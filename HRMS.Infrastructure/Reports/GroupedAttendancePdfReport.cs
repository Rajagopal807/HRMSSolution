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

        private static readonly Font _fTitle = new Font(_bfB, 11f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fSubTitle = new Font(_bfB, 9f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fColHeader = new Font(_bfB, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fGroupHdr = new Font(_bfB, 7f, Font.NORMAL, BaseColor.WHITE);
        private static readonly Font _fCell = new Font(_bf, 5.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fCellBold = new Font(_bfB, 5.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fSmall = new Font(_bf, 5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fSubtotal = new Font(_bfB, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fPageInfo = new Font(_bf, 8f, Font.NORMAL, BaseColor.BLACK);

        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly BaseColor _colHeaderBg = new BaseColor(200, 200, 200);
        private static readonly BaseColor _groupHdrBg = new BaseColor(51, 71, 121);  // dark navy
        private static readonly BaseColor _subtotalBg = new BaseColor(220, 230, 245);  // light blue
        private static readonly BaseColor _grandTotalBg = new BaseColor(180, 200, 230);
        private static readonly BaseColor _altRowBg = new BaseColor(245, 245, 245);
        private static readonly BaseColor _borderColor = new BaseColor(180, 180, 180);

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
                var doc = new Document(pageSize, 20f, 20f, 38f, 20f);
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
            int totalCols = 34;  // EmpID(1) + Name(1) + Day1-31(31) + WorkDays(1) + padding(1) = 35

            float[] widths = new float[totalCols];
            widths[0] = 42f;   // Emp ID
            widths[1] = 72f;   // Name
            for (int i = 2; i <= 32; i++) widths[i] = 17.5f;  // Day 1–31
            //widths[33] = 24f;  // Work Days
            widths[33] = 0.1f; // invisible padding col

            int grandTotal = 0;
            int grandEmpCount = 0;
            bool alternate = false;

            foreach (var group in data.Groups)
            {
                var table = new PdfPTable(totalCols)
                {
                    WidthPercentage = 100f,
                    SpacingBefore = 0f,
                    SpacingAfter = 4f,
                    HeaderRows = 2,   // col-header row repeated on new page
                    KeepTogether = false
                };
                table.SetWidths(widths);

                // ── Group header row (full-width, dark navy) ──────────────────
                AddGroupHeaderRow(table, group, data, totalCols);

                // ── Column header row ─────────────────────────────────────────
                AddColumnHeaderRow(table, data.DaysInMonth);

                // ── Employee data rows ────────────────────────────────────────
                alternate = false;
                foreach (var row in group.Rows)
                {
                    var bg = alternate ? _altRowBg : BaseColor.WHITE;
                    alternate = !alternate;
                    AddDataRow(table, row, bg, data.DaysInMonth);
                }

                // ── Group subtotal row ────────────────────────────────────────
                AddSubtotalRow(table, group, totalCols);

                doc.Add(table);

                grandTotal += group.TotalWorkDays;
                grandEmpCount += group.EmployeeCount;
            }

            // ── Grand total row ───────────────────────────────────────────────
            var grandTable = new PdfPTable(totalCols) { WidthPercentage = 100f };
            grandTable.SetWidths(widths);
            AddGrandTotalRow(grandTable, grandTotal, grandEmpCount, totalCols, data.Groups.Count);
            doc.Add(grandTable);
        }

        // ── Group header ──────────────────────────────────────────────────────
        private void AddGroupHeaderRow(PdfPTable table, AttendanceGroupDto group,
            GroupedAttendanceReportDto data, int totalCols)
        {
            string text = string.Format("{0}: {1}   ({2} Employee{3})",
                data.GroupingLabel,
                group.GroupName,
                group.EmployeeCount,
                group.EmployeeCount == 1 ? "" : "s");

            var cell = new PdfPCell(new Phrase(text, _fGroupHdr))
            {
                BackgroundColor = _groupHdrBg,
                Colspan = totalCols,
                Border = Rectangle.NO_BORDER,
                Padding = 5f,
                PaddingLeft = 8f,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            table.AddCell(cell);
        }

        // ── Column header ─────────────────────────────────────────────────────
        private void AddColumnHeaderRow(PdfPTable table, int daysInMonth)
        {
            AddHCell(table, "Emp ID");
            AddHCell(table, "Employee Name");
            for (int d = 1; d <= 31; d++)
            {
                var c = new PdfPCell(new Phrase(d.ToString(), _fColHeader))
                {
                    BackgroundColor = d <= daysInMonth ? _colHeaderBg
                                                           : new BaseColor(235, 235, 235),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.BOX,
                    BorderColor = _borderColor,
                    BorderWidth = 0.4f,
                    Padding = 2.5f
                };
                table.AddCell(c);
            }
            //AddHCell(table, "Work\nDays");
            AddHCell(table, "");  // padding col
        }

        // ── Data row ─────────────────────────────────────────────────────────
        private void AddDataRow(PdfPTable table, AttendanceRowDto row,
            BaseColor bg, int daysInMonth)
        {
            AddDCell(table, row.EmployeeCode, _fCellBold, bg, Element.ALIGN_LEFT);
            AddDCell(table, row.EmployeeName, _fCell, bg, Element.ALIGN_LEFT);

            for (int d = 0; d < 31; d++)
            {
                var day = row.Days[d];
                var cell = new PdfPCell
                {
                    BackgroundColor = day.IsPadDay ? new BaseColor(235, 235, 235) : bg,
                    Border = Rectangle.BOX,
                    BorderColor = _borderColor,
                    BorderWidth = 0.3f,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 1.5f
                };

                if (!day.IsPadDay)
                {
                    string line1, line2 = "";
                    if (!string.IsNullOrEmpty(day.AttId) && day.AttId != "00" && string.IsNullOrEmpty(day.FirstIn))
                        line1 = day.AttId;
                    else if (!string.IsNullOrEmpty(day.FirstIn))
                    {
                        line1 = day.FirstIn;
                        line2 = !string.IsNullOrEmpty(day.Lastout) ? day.Lastout : "--:--";
                    }
                    else
                        line1 = "00";

                    var phrase = new Phrase();
                    phrase.Add(new Chunk(line1, _fSmall));
                    if (!string.IsNullOrEmpty(line2))
                        phrase.Add(new Chunk("\n" + line2, _fSmall));
                    cell.AddElement(new Paragraph(phrase) { Alignment = Element.ALIGN_CENTER });
                }

                table.AddCell(cell);
            }

            //AddDCell(table, row.WorkDays.ToString(), _fCellBold, bg, Element.ALIGN_CENTER);
            AddDCell(table, "", _fCell, bg, Element.ALIGN_CENTER);  // padding
        }

        // ── Group subtotal ────────────────────────────────────────────────────
        private void AddSubtotalRow(PdfPTable table, AttendanceGroupDto group, int totalCols)
        {
            string label = string.Format("  Subtotal (No.Of.Employees) : {0}", group.EmployeeCount);

            var labelCell = new PdfPCell(new Phrase(label, _fSubtotal))
            {
                BackgroundColor = _subtotalBg,
                Colspan = totalCols - 1,
                Border = Rectangle.BOX,
                BorderColor = _borderColor,
                BorderWidth = 0.5f,
                Padding = 3f,
                PaddingLeft = 8f,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            table.AddCell(labelCell);

            //var wdCell = new PdfPCell(
            //    new Phrase("", _fSubtotal))
            //{
            //    BackgroundColor = _subtotalBg,
            //    Colspan = totalCols - 2,
            //    Border = Rectangle.BOX,
            //    BorderColor = _borderColor,
            //    BorderWidth = 0.5f,
            //    Padding = 3f,
            //    HorizontalAlignment = Element.ALIGN_CENTER
            //};
            //table.AddCell(wdCell);

            // padding col
            table.AddCell(new PdfPCell(new Phrase("", _fCell))
            { BackgroundColor = _subtotalBg, Border = Rectangle.NO_BORDER });
        }

        // ── Grand total ───────────────────────────────────────────────────────
        private void AddGrandTotalRow(PdfPTable table, int total,
            int empCount, int totalCols, int groupCount)
        {
            string label = string.Format(
                "  Grand Total    Groups: {0}    Total Employees: {1}",
                groupCount, empCount);  // group count not tracked here; just show emp count

            var labelCell = new PdfPCell(new Phrase(label, _fSubtotal))
            {
                BackgroundColor = _grandTotalBg,
                Colspan = totalCols - 1,
                Border = Rectangle.BOX,
                BorderColor = _borderColor,
                BorderWidth = 0.6f,
                Padding = 4f,
                PaddingLeft = 8f,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            table.AddCell(labelCell);

            //var wdCell = new PdfPCell(new Phrase("", _fSubtotal))
            //{
            //    BackgroundColor = _grandTotalBg,
            //    //Border = Rectangle.BOX,
            //    //BorderColor = _borderColor,
            //    BorderWidth = 0.6f,
            //    Padding = 4f,
            //    HorizontalAlignment = Element.ALIGN_CENTER
            //};
            //table.AddCell(wdCell);

            table.AddCell(new PdfPCell(new Phrase("", _fCell))
            { BackgroundColor = _grandTotalBg, Border = Rectangle.NO_BORDER });
        }

        // ── Cell helpers ──────────────────────────────────────────────────────
        private static void AddHCell(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text, new Font(_bfB, 6f)))
            {
                BackgroundColor = _colHeaderBg,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(160, 160, 160),
                BorderWidth = 0.5f,
                Padding = 2.5f
            };
            table.AddCell(cell);
        }

        private static void AddDCell(PdfPTable table, string text,
            Font font, BaseColor bg, int align)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(200, 200, 200),
                BorderWidth = 0.3f,
                Padding = 2f
            };
            table.AddCell(cell);
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
        private static readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont _bfB = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);

        public GroupedPageEvent(GroupedAttendanceReportDto data) { _data = data; }

        public override void OnStartPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent;

            // Company name
            ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                new Phrase(_data.CompanyName, new Font(_bfB, 12f)),
                doc.PageSize.Width / 2f,
                doc.PageSize.Height - 18f, 0);

            // Report title
            string title = string.Format(
                "Attendance Register ({0} Wise) For the period {1:dd/MM/yyyy} To {2:dd/MM/yyyy}",
                _data.GroupingLabel, _data.FromDate, _data.ToDate);

            ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                new Phrase(title, new Font(_bfB, 9f)),
                doc.PageSize.Width / 2f,
                doc.PageSize.Height - 30f, 0);

            // Top-right: date + page
            float rx = doc.PageSize.Width - doc.RightMargin;
            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase(_data.PrintedOn.ToString("dd-MMM-yyyy"), new Font(_bf, 8f)),
                rx, doc.PageSize.Height - 18f, 0);
            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase("Page No : " + writer.PageNumber, new Font(_bf, 8f)),
                rx, doc.PageSize.Height - 30f, 0);
        }
    }
}
