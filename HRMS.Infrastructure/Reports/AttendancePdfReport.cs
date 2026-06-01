using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Reports
{
    /// <summary>
    /// OCP: Implements IReportGenerator&lt;AttendanceReportDto&gt; for PDF output.
    /// Uses iTextSharp (free, .NET 4.8 compatible).
    /// NuGet: Install-Package iTextSharp -Version 5.5.13.3
    /// </summary>
    public class AttendancePdfReport : IReportGenerator<AttendanceReportDto>
    {
        // ── Fonts & Colours ───────────────────────────────────────────────────
        private static readonly BaseFont _baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont _boldFont = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);

        private static readonly Font _fontTitle = new Font(_boldFont, 11f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fontSubTitle = new Font(_boldFont, 9f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fontHeader = new Font(_boldFont, 6f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fontCell = new Font(_baseFont, 5.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fontCellBold = new Font(_boldFont, 5.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fontSmall = new Font(_baseFont, 5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font _fontPageInfo = new Font(_baseFont, 7f, Font.NORMAL, BaseColor.BLACK);

        private static readonly BaseColor _headerBg = new BaseColor(220, 220, 220);
        private static readonly BaseColor _altRowBg = new BaseColor(245, 245, 245);

        public string ContentType => "application/pdf";

        public string GetFileName(AttendanceReportDto data)
        {
            return string.Format("AttendanceRegister_{0:MMMMyyyy}.pdf", data.FromDate);
        }

        public byte[] Generate(AttendanceReportDto data)
        {
            using (var ms = new MemoryStream())
            {
                // A3 landscape for 31 day columns + ID + Name + WorkDays
                var pageSize = new Rectangle(PageSize.A3.Height, PageSize.A3.Width); // landscape
                var doc = new Document(pageSize, 20f, 20f, 60f, 20f);
                var writer = PdfWriter.GetInstance(doc, ms);

                // Page event for header/footer on every page
                writer.PageEvent = new AttendancePageEvent(data);

                doc.Open();
                BuildContent(doc, writer, data);
                doc.Close();

                return ms.ToArray();
            }
        }

        // ── Content ───────────────────────────────────────────────────────────
        private void BuildContent(Document doc, PdfWriter writer, AttendanceReportDto data)
        {
            int totalDays = Convert.ToInt32((data.ToDate.AddDays(1) - data.FromDate).TotalDays);
            int days = totalDays;

            int totalCols = 2 + (totalDays * 2);
            float[] widths = new float[totalCols];
            widths[0] = 40f;   // Emp ID
            widths[1] = 70f;   // Employee Name

            int colIndex = 2;
            for (int d = 0; d < totalDays; d++)
            {
                widths[colIndex++] = 18f; // In
                widths[colIndex++] = 18f; // Out
            }

            var table = new PdfPTable(totalCols)
            {
                WidthPercentage = 100f,
                SpacingBefore = 4f,
                HeaderRows = 1,
                KeepTogether = false
            };
            table.SetWidths(widths);

            // ── Header Row ────────────────────────────────────────────────────
            AddHeaderCell(table, "Emp ID", 2);
            AddHeaderCell(table, "Employee Name", 2);
            for (int d = 1; d <= days; d++)
            {
                var cell = new PdfPCell(new Phrase(d.ToString(), _fontHeader))
                {
                    Colspan = 2,
                    BackgroundColor = _headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.BOX,
                    BorderColor = new BaseColor(160, 160, 160),
                    BorderWidth = 0.5f,
                    Padding = 2.5f
                };
                table.AddCell(cell);
            }

            //AddHeaderCell(table, "");
            //AddHeaderCell(table, "");

            for (int d = 1; d <= days; d++)
            {
                AddHeaderCell(table, "In");
                AddHeaderCell(table, "Out");
            }

            table.HeaderRows = 2;


            // ── Data Rows ─────────────────────────────────────────────────────
            bool alternate = false;
            foreach (var row in data.Rows)
            {
                var rowBg = alternate ? _altRowBg : BaseColor.WHITE;
                alternate = !alternate;

                AddDataCell(table, row.EmployeeCode, _fontCellBold, rowBg, Element.ALIGN_LEFT);
                AddDataCell(table, row.EmployeeName, _fontCell, rowBg, Element.ALIGN_LEFT);

                for (int d = 0; d < days; d++)
                {
                    var day = row.Days[d];

                    string inVal = "";
                    string outVal = "";

                    if (day.IsPadDay)
                    {
                        AddDataCell(table, "", _fontCell, rowBg, Element.ALIGN_CENTER);
                        AddDataCell(table, "", _fontCell, rowBg, Element.ALIGN_CENTER);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(day.AttId) && day.AttId != "00"
                        && string.IsNullOrEmpty(day.FirstIn))
                    {
                        var attCell = new PdfPCell(new Phrase(day.AttId, _fontCellBold))
                        {
                            Colspan = 2,
                            BackgroundColor = _headerBg,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Border = Rectangle.BOX,
                            BorderColor = new BaseColor(160, 160, 160),
                            BorderWidth = 0.5f,
                            Padding = 2.5f
                        };

                        table.AddCell(attCell);
                        continue;
                    }
                    else if (!string.IsNullOrEmpty(day.FirstIn))
                    {
                        inVal = day.FirstIn;
                        outVal = !string.IsNullOrEmpty(day.Lastout) ? day.Lastout : "--:--";
                    }
                    else
                    {
                        var attCell = new PdfPCell(new Phrase("00", _fontCellBold))
                        {
                            Colspan = 2,
                            BackgroundColor = rowBg,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Border = Rectangle.BOX,
                            BorderColor = new BaseColor(160, 160, 160),
                            BorderWidth = 0.5f,
                            Padding = 2.5f
                        };

                        table.AddCell(attCell);
                        continue;
                    }


                    AddDataCell(table, inVal, _fontSmall, rowBg, Element.ALIGN_CENTER);
                    AddDataCell(table, outVal, _fontSmall, rowBg, Element.ALIGN_CENTER);
                }
            }

            doc.Add(table);
            AddLeaveTypeFooter(doc, data);
        }

        private static void AddLeaveTypeFooter(Document doc, AttendanceReportDto data)
        {
            if (data.LeaveTypes == null || data.LeaveTypes.Count == 0)
                return;

            var table = new PdfPTable(2)
            {
                WidthPercentage = 35f,
                HorizontalAlignment = Element.ALIGN_LEFT,
                SpacingBefore = 8f,
                KeepTogether = true
            };
            table.SetWidths(new[] { 45f, 140f });

            var titleCell = new PdfPCell(new Phrase("Leave Types", _fontCellBold))
            {
                Colspan = 2,
                BackgroundColor = _headerBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(160, 160, 160),
                BorderWidth = 0.5f,
                Padding = 3f
            };
            table.AddCell(titleCell);

            AddFooterHeaderCell(table, "LeaveType ID");
            AddFooterHeaderCell(table, "Leave Description");

            foreach (var leaveType in data.LeaveTypes)
            {
                AddDataCell(table, leaveType.LeaveTypeID, _fontCellBold, BaseColor.WHITE, Element.ALIGN_LEFT);
                AddDataCell(table, leaveType.Description, _fontCell, BaseColor.WHITE, Element.ALIGN_LEFT);
            }

            doc.Add(table);
        }

        // ── Cell Helpers ──────────────────────────────────────────────────────
        private static void AddHeaderCell(PdfPTable table, string text, int rowSpan = 0)
        {
            PdfPCell cell = null;
            if(rowSpan !=0 )
            {
                cell = new PdfPCell(new Phrase(text, _fontHeader))
                {
                    Rowspan = rowSpan,
                    BackgroundColor = _headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.BOX,
                    BorderColor = new BaseColor(160, 160, 160),
                    BorderWidth = 0.5f,
                    Padding = 2.5f
                };
            }
            else
            {
                cell = new PdfPCell(new Phrase(text, _fontHeader))
                {
                    BackgroundColor = _headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.BOX,
                    BorderColor = new BaseColor(160, 160, 160),
                    BorderWidth = 0.5f,
                    Padding = 2.5f
                };
            }

            table.AddCell(cell);
        }

        private static void AddDataCell(PdfPTable table, string text,
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

        private static void AddFooterHeaderCell(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text, _fontHeader))
            {
                BackgroundColor = _headerBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(160, 160, 160),
                BorderWidth = 0.5f,
                Padding = 2.5f
            };
            table.AddCell(cell);
        }
    }

    // ── Page Header / Footer Event ─────────────────────────────────────────────
    internal class AttendancePageEvent : PdfPageEventHelper
    {
        private readonly AttendanceReportDto _data;
        private static readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont _bfB = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);
        private static readonly Font _fTitle = new Font(_bfB, 12f);
        private static readonly Font _fSub = new Font(_bfB, 9f);
        private static readonly Font _fInfo = new Font(_bf, 8f);

        public AttendancePageEvent(AttendanceReportDto data) { _data = data; }

        public override void OnStartPage(PdfWriter writer, Document document)
        {
            var cb = writer.DirectContent;

            // ── Company name — centred ────────────────────────────────────────
            ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                new Phrase(_data.CompanyName, _fTitle),
                document.PageSize.Width / 2f,
                document.PageSize.Height - 18f, 0);

            // ── Report title ──────────────────────────────────────────────────
            string title = string.Format(
                "Attendance Register For the period {0:dd/MM/yyyy} To {1:dd/MM/yyyy}",
                _data.FromDate, _data.ToDate);

            ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                new Phrase(title, _fSub),
                document.PageSize.Width / 2f,
                document.PageSize.Height - 30f, 0);

            // ── Top-right: date + page number ─────────────────────────────────
            float rightX = document.PageSize.Width - document.RightMargin;
            float topY = document.PageSize.Height - 18f;

            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase(_data.PrintedOn.ToString("dd-MMM-yyyy"), _fInfo),
                rightX, topY, 0);

            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase("Page No : " + writer.PageNumber, _fInfo),
                rightX, topY - 12f, 0);
        }
    }
}
