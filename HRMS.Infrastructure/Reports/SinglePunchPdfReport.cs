using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace HRMS.Infrastructure.Reports
{
    public class SinglePunchPdfReport : IReportGenerator<SinglePunchReportDto>
    {
        private static readonly BaseFont BaseFontRegular = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont BaseFontBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);
        private static readonly Font TitleFont = new Font(BaseFontBold, 13f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font SubTitleFont = new Font(BaseFontBold, 10f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font HeaderFont = new Font(BaseFontBold, 7.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font CellFont = new Font(BaseFontRegular, 7.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font CellBoldFont = new Font(BaseFontBold, 7.5f, Font.NORMAL, BaseColor.BLACK);
        private static readonly BaseColor HeaderBg = new BaseColor(220, 220, 220);
        private static readonly BaseColor AltRowBg = new BaseColor(245, 245, 245);

        public string ContentType => "application/pdf";

        public string GetFileName(SinglePunchReportDto data)
            => string.Format("SinglePunchReport_{0:ddMMyyyy}_{1:ddMMyyyy}.pdf", data.FromDate, data.ToDate);

        public byte[] Generate(SinglePunchReportDto data)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4.Rotate(), 24f, 24f, 28f, 24f);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();
                BuildContent(doc, data);
                doc.Close();
                return ms.ToArray();
            }
        }

        private static void BuildContent(Document doc, SinglePunchReportDto data)
        {
            doc.Add(new Paragraph(data.CompanyName, TitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 4f
            });

            doc.Add(new Paragraph(
                string.Format("Single Punch Report For the period {0:dd/MM/yyyy} To {1:dd/MM/yyyy}", data.FromDate, data.ToDate),
                SubTitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 2f
            });

            doc.Add(new Paragraph(string.Format("Printed On: {0:dd-MMM-yyyy}", data.PrintedOn), CellFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingAfter = 8f
            });

            var table = new PdfPTable(9)
            {
                WidthPercentage = 100f,
                HeaderRows = 1
            };
            table.SetWidths(new[] { 30f, 75f, 130f, 100f, 75f, 65f, 42f, 80f, 130f });

            AddHeader(table, "S.No");
            AddHeader(table, "Emp ID");
            AddHeader(table, "Employee Name");
            AddHeader(table, "Department");
            AddHeader(table, "Date");
            AddHeader(table, "Punch Time");
            AddHeader(table, "I/O");
            AddHeader(table, "Reader");
            AddHeader(table, "Remarks");

            int index = 1;
            bool alternate = false;
            foreach (var row in data.Rows)
            {
                var bg = alternate ? AltRowBg : BaseColor.WHITE;
                alternate = !alternate;

                AddCell(table, index++.ToString(), CellFont, bg, Element.ALIGN_CENTER);
                AddCell(table, row.EmployeeId, CellBoldFont, bg, Element.ALIGN_LEFT);
                AddCell(table, row.EmployeeName, CellFont, bg, Element.ALIGN_LEFT);
                AddCell(table, row.DepartmentName, CellFont, bg, Element.ALIGN_LEFT);
                AddCell(table, row.AttendanceDate.ToString("dd-MMM-yyyy"), CellBoldFont, bg, Element.ALIGN_CENTER);
                AddCell(table, row.PunchTime.ToString("HH:mm"), CellBoldFont, bg, Element.ALIGN_CENTER);
                AddCell(table, row.IOFlag, CellFont, bg, Element.ALIGN_CENTER);
                AddCell(table, row.BadgeReaderNo, CellFont, bg, Element.ALIGN_CENTER);
                AddCell(table, row.Remarks, CellFont, bg, Element.ALIGN_LEFT);
            }

            if (data.Rows.Count == 0)
            {
                table.AddCell(new PdfPCell(new Phrase("No single punch records found for the selected period.", CellFont))
                {
                    Colspan = 9,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8f,
                    Border = Rectangle.BOX,
                    BorderColor = BaseColor.LIGHT_GRAY
                });
            }

            doc.Add(table);
        }

        private static void AddHeader(PdfPTable table, string text)
        {
            table.AddCell(new PdfPCell(new Phrase(text, HeaderFont))
            {
                BackgroundColor = HeaderBg,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(160, 160, 160),
                Padding = 5f
            });
        }

        private static void AddCell(PdfPTable table, string text, Font font, BaseColor bg, int align)
        {
            table.AddCell(new PdfPCell(new Phrase(text ?? "", font))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = BaseColor.LIGHT_GRAY,
                Padding = 4f
            });
        }
    }
}
