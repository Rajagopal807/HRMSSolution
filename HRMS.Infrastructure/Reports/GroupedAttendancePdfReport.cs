using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Reports
{
    /// <summary>
    /// OCP: New class implementing IReportGenerator&lt;GroupedAttendanceReportDto&gt;.
    /// AttendancePdfReport (employee-wise) is never modified.
    ///
    /// Layout:
    ///   Company header + report title on every page.
    ///   Each group (dept / cadre) gets:
    ///     – A full-width group header row (dark background, group name + employee count)
    ///     – Employee rows (same 31-column grid as the base report)
    ///     – A group subtotal row (total work days for the group)
    ///   Grand total row at the very end.
    /// </summary>
    public class GroupedAttendancePdfReport : IReportGenerator<GroupedAttendanceReportDto>
    {
        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont _bfB = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);

        private static readonly Font _fPageHdr = new Font(_bfB, 11f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fPageSub = new Font(_bfB, 8f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fDayNum = new Font(_bfB, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fInOut = new Font(_bf, 5.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fEmpId = new Font(_bfB, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fEmpName = new Font(_bf, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fTime = new Font(_bf, 5.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fSpecial = new Font(_bfB, 6f, Font.NORMAL, BaseColor.BLACK);  // WW / AA / HH
        private static readonly Font _fGrpHdr = new Font(_bfB, 7f, Font.NORMAL, BaseColor.WHITE);
        private static readonly Font _fSubtotal = new Font(_bfB, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fGrandTotal = new Font(_bfB, 7f, Font.NORMAL, BaseColor.BLACK);

        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly BaseColor _bgColHeader = new BaseColor(220, 220, 220);
        private static readonly BaseColor _bgInOut = new BaseColor(235, 235, 235);
        private static readonly BaseColor _bgGroupHdr = new BaseColor(40, 60, 110);   // dark navy
        private static readonly BaseColor _bgSubtotal = new BaseColor(215, 228, 248);   // soft blue
        private static readonly BaseColor _bgGrandTotal = new BaseColor(180, 200, 235);
        private static readonly BaseColor _bgAlt = new BaseColor(248, 248, 248);
        private static readonly BaseColor _borderLight = new BaseColor(190, 190, 190);
        private static readonly BaseColor _borderDark = new BaseColor(140, 140, 140);

        // ── Column width constants ─────────────────────────────────────────────
        // EmpID + Name + (31 days × 2 sub-cols)
        // Total columns in table = 2 + 62 = 64
        private const int TOTAL_COLS = 64;
        private const float W_EMPID = 48f;
        private const float W_NAME = 70f;
        private const float W_IN_OUT = 14f;   // each In / Out sub-column

        public string ContentType => "application/pdf";

        public string GetFileName(GroupedAttendanceReportDto data)
        {
            return string.Format("AttendanceRegister_{0}_{1:MMMMyyyy}.pdf",
                data.GroupingLabel, data.FromDate);
        }

        public byte[] Generate(GroupedAttendanceReportDto data)
        {
            using (var ms = new MemoryStream())
            {
                var pageSize = new Rectangle(PageSize.A3.Height, PageSize.A3.Width);
                var doc = new Document(pageSize, 20f, 20f, 45f, 20f);
                var writer = PdfWriter.GetInstance(doc, ms);
                writer.PageEvent = new GroupedPageEvent(data);

                doc.Open();
                BuildContent(doc, data);
                doc.Close();

                return ms.ToArray();
            }
        }

        // ── Content ───────────────────────────────────────────────────────────
        private void BuildContent(Document doc, GroupedAttendanceReportDto data)
        {
            int days = data.DaysInMonth;
            int grandWD = 0;
            int grandEmp = 0;
            bool alternate = false;

            foreach (var group in data.Groups)
            {
                // Each group gets its own PdfPTable so iTextSharp can start a new
                // page mid-group if needed.  HeaderRows=2 repeats the column headers.
                var table = BuildTable(days);

                // ── Group header row ──────────────────────────────────────────
                AddGroupHeaderRow(table, group, data);

                // ── Two-row column header ─────────────────────────────────────
                AddColumnHeaderRow1(table, days);  // Day numbers
                AddColumnHeaderRow2(table, days);  // In / Out labels

                // ── Employee data rows ────────────────────────────────────────
                alternate = false;
                foreach (var row in group.Rows)
                {
                    var bg = alternate ? _bgAlt : BaseColor.WHITE;
                    alternate = !alternate;
                    AddDataRow(table, row, bg, days);
                }

                // ── Group subtotal ────────────────────────────────────────────
                AddSubtotalRow(table, group);

                doc.Add(table);

                grandWD += group.TotalWorkDays;
                grandEmp += group.EmployeeCount;
            }

            // ── Grand total ───────────────────────────────────────────────────
            var grandTable = BuildTable(data.DaysInMonth);
            AddGrandTotalRow(grandTable, data.Groups.Count, grandEmp, grandWD);
            doc.Add(grandTable);
        }

        // ── Table factory ─────────────────────────────────────────────────────
        private static PdfPTable BuildTable(int days)
        {
            var table = new PdfPTable(TOTAL_COLS)
            {
                WidthPercentage = 100f,
                SpacingBefore = 0f,
                SpacingAfter = 6f,
                HeaderRows = 3,     // group header (1) + col header row1 (1) + col header row2 (1)
                KeepTogether = false
            };

            var widths = new float[TOTAL_COLS];
            widths[0] = W_EMPID;
            widths[1] = W_NAME;
            for (int i = 2; i < TOTAL_COLS; i++)
                widths[i] = W_IN_OUT;

            table.SetWidths(widths);
            return table;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Group header row — full width, dark navy background
        // ════════════════════════════════════════════════════════════════════════
        private void AddGroupHeaderRow(PdfPTable table,
            AttendanceGroupDto group, GroupedAttendanceReportDto data)
        {
            string text = string.Format("  {0}: {1}   ({2} Employee{3})",
                data.GroupingLabel, group.GroupName,
                group.EmployeeCount, group.EmployeeCount == 1 ? "" : "s");

            var cell = new PdfPCell(new Phrase(text, _fGrpHdr))
            {
                Colspan = TOTAL_COLS,
                BackgroundColor = _bgGroupHdr,
                Border = Rectangle.NO_BORDER,
                Padding = 5f,
                PaddingLeft = 10f,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            table.AddCell(cell);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Column header — Row 1: "Emp ID" | "Employee Name" | "1"(×2) | … | "31"(×2)
        // ════════════════════════════════════════════════════════════════════════
        private void AddColumnHeaderRow1(PdfPTable table, int days)
        {
            // Emp ID — rowspan 2
            var empIdCell = MakeHCell("Emp ID", _fDayNum, _bgColHeader, 1, 2);
            table.AddCell(empIdCell);

            // Employee Name — rowspan 2
            var nameCell = MakeHCell("Employee Name", _fDayNum, _bgColHeader, 1, 2);
            table.AddCell(nameCell);

            // Day numbers — each spans 2 columns (In + Out)
            for (int d = 1; d <= 31; d++)
            {
                string label = d <= days ? d.ToString() : "";
                var bg = d <= days ? _bgColHeader : new BaseColor(235, 235, 235);
                var cell = MakeHCell(label, _fDayNum, bg, 2, 1);
                table.AddCell(cell);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Column header — Row 2: "In" | "Out" under each day number
        // ════════════════════════════════════════════════════════════════════════
        private void AddColumnHeaderRow2(PdfPTable table, int days)
        {
            // EmpID and Name cells were already rowspan=2, so skip them here.
            // iTextSharp handles rowspan automatically — we only add the In/Out cells.

            for (int d = 1; d <= 31; d++)
            {
                var bg = d <= days ? _bgInOut : new BaseColor(235, 235, 235);

                // In
                var inCell = new PdfPCell(new Phrase(d <= days ? "In" : "", _fInOut))
                {
                    BackgroundColor = bg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.BOX,
                    BorderColor = _borderLight,
                    BorderWidth = 0.3f,
                    Padding = 1.5f
                };
                table.AddCell(inCell);

                // Out
                var outCell = new PdfPCell(new Phrase(d <= days ? "Out" : "", _fInOut))
                {
                    BackgroundColor = bg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.BOX,
                    BorderColor = _borderLight,
                    BorderWidth = 0.3f,
                    Padding = 1.5f
                };
                table.AddCell(outCell);
            }
        }

        // ── Data row ─────────────────────────────────────────────────────────
        private void AddDataRow(PdfPTable table, AttendanceRowDto row,
            BaseColor bg, int daysInMonth)
        {
            // Emp ID
            table.AddCell(MakeDataCell(row.EmployeeCode, _fEmpId, bg, Element.ALIGN_LEFT));

            // Employee Name (may wrap — allow it)
            var nameCell = new PdfPCell(new Phrase(row.EmployeeName, _fEmpName))
            {
                BackgroundColor = bg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = _borderLight,
                BorderWidth = 0.3f,
                Padding = 2f,
                NoWrap = false
            };
            table.AddCell(nameCell);

            // Day columns
            for (int d = 0; d < 31; d++)
            {
                var day = row.Days[d];

                if (day.IsPadDay)
                {
                    // Pad columns — grey, no content, span 2
                    var padCell = new PdfPCell(new Phrase("", _fTime))
                    {
                        Colspan = 2,
                        BackgroundColor = new BaseColor(235, 235, 235),
                        Border = Rectangle.BOX,
                        BorderColor = _borderLight,
                        BorderWidth = 0.3f,
                        Padding = 1f
                    };
                    table.AddCell(padCell);
                    continue;
                }

                // Check if this is a special marker (WW / AA / HH / WO)
                bool isSpecial = !string.IsNullOrEmpty(day.AttId)
                                 && day.AttId != "00"
                                 && string.IsNullOrEmpty(day.FirstIn);

                if (isSpecial)
                {
                    // Special marker spans both In + Out columns
                    var specCell = new PdfPCell(new Phrase(day.AttId, _fSpecial))
                    {
                        Colspan = 2,
                        BackgroundColor = bg,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Border = Rectangle.BOX,
                        BorderColor = _borderLight,
                        BorderWidth = 0.3f,
                        Padding = 1.5f
                    };
                    table.AddCell(specCell);
                    continue;
                }

                // Normal In/Out times
                string inStr = !string.IsNullOrEmpty(day.FirstIn)
                    ? DateTime.Parse(day.FirstIn).ToString(@"hh\:mm")
                    : "--";
                string outStr = !string.IsNullOrEmpty(day.Lastout)
                    ? DateTime.Parse(day.Lastout).ToString(@"hh\:mm")
                    : (!string.IsNullOrEmpty(day.FirstIn) ? "--:--" : "--");

                // If absent (no in time, no special marker) → "--" in both
                if (!!string.IsNullOrEmpty(day.FirstIn))
                {
                    inStr = "--";
                    outStr = "--";
                }

                table.AddCell(MakeDataCell(inStr, _fTime, bg, Element.ALIGN_CENTER));
                table.AddCell(MakeDataCell(outStr, _fTime, bg, Element.ALIGN_CENTER));
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Subtotal row
        // ════════════════════════════════════════════════════════════════════════
        private void AddSubtotalRow(PdfPTable table, AttendanceGroupDto group)
        {
            string label = string.Format(
                "  Subtotal — {0}    Employees: {1}    Total Work Days: {2}",
                group.GroupName, group.EmployeeCount, group.TotalWorkDays);

            var cell = new PdfPCell(new Phrase(label, _fSubtotal))
            {
                Colspan = TOTAL_COLS,
                BackgroundColor = _bgSubtotal,
                Border = Rectangle.BOX,
                BorderColor = _borderDark,
                BorderWidth = 0.5f,
                Padding = 4f,
                PaddingLeft = 10f,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            table.AddCell(cell);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Grand total row
        // ════════════════════════════════════════════════════════════════════════
        private void AddGrandTotalRow(PdfPTable table,
            int groupCount, int empCount, int totalWorkDays)
        {
            string label = string.Format(
                "  Grand Total    Groups: {0}    Total Employees: {1} ",
                groupCount, empCount);

            var cell = new PdfPCell(new Phrase(label, _fGrandTotal))
            {
                Colspan = TOTAL_COLS,
                BackgroundColor = _bgGrandTotal,
                Border = Rectangle.BOX,
                BorderColor = _borderDark,
                BorderWidth = 0.6f,
                Padding = 5f,
                PaddingLeft = 10f,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            table.AddCell(cell);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Cell factory helpers
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Header cell with optional colspan / rowspan.</summary>
        private static PdfPCell MakeHCell(string text, Font font,
            BaseColor bg, int colspan, int rowspan)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Colspan = colspan,
                Rowspan = rowspan,
                BackgroundColor = bg,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(150, 150, 150),
                BorderWidth = 0.5f,
                Padding = 2.5f
            };
            return cell;
        }

        /// <summary>Standard data cell.</summary>
        private static PdfPCell MakeDataCell(string text, Font font,
            BaseColor bg, int align)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(200, 200, 200),
                BorderWidth = 0.3f,
                Padding = 1.5f,
                NoWrap = true
            };
        }


    private static readonly BaseFont _bfStatic =
            BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont _bfBStatic =
            BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);
    }

    // ── Page event: company name + report title + date/page on every page ──────
    internal class GroupedPageEvent : PdfPageEventHelper
    {
        private readonly GroupedAttendanceReportDto _data;

        private static readonly BaseFont _bf =
            BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont _bfB =
            BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);

        public GroupedPageEvent(GroupedAttendanceReportDto data) { _data = data; }

        public override void OnStartPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent;

            float centerX = doc.PageSize.Width / 2;
            float topY = doc.PageSize.Height - 12f;

            // ── Company Name (CENTER) ─────────────────────────
            ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                new Phrase(_data.CompanyName, new Font(_bfB, 12f, Font.BOLD)),
                centerX, topY, 0);

            // ── Report Title (CENTER) ─────────────────────────
            string title = string.Format(
                "Attendance Register ({0} Wise) For the period {1:dd/MM/yyyy} To {2:dd/MM/yyyy}",
                _data.GroupingLabel, _data.FromDate, _data.ToDate);

            ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                new Phrase(title, new Font(_bfB, 8f, Font.BOLD)),
                centerX, topY - 14f, 0);

            // ── Right side (date + page) ──────────────────────
            float rightX = doc.PageSize.Width - doc.RightMargin;

            string info = string.Format("{0:dd-MMM-yyyy}    Page {1}",
                _data.PrintedOn, writer.PageNumber);

            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase(info, new Font(_bf, 7f)),
                rightX, topY - 26f, 0);
        }
    }
}
