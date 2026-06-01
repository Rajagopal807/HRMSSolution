using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace HRMS.Infrastructure.Reports
{
    public class HolidayPdfReport : IReportGenerator<HolidayReportDto>
    {
        private static readonly BaseFont BaseFontRegular = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        private static readonly BaseFont BaseFontBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false);
        private static readonly Font TitleFont = new Font(BaseFontBold, 13f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font SubTitleFont = new Font(BaseFontBold, 10f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font HeaderFont = new Font(BaseFontBold, 8f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font CellFont = new Font(BaseFontRegular, 8f, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font CellBoldFont = new Font(BaseFontBold, 8f, Font.NORMAL, BaseColor.BLACK);
        private static readonly BaseColor HeaderBg = new BaseColor(220, 220, 220);
        private static readonly BaseColor AltRowBg = new BaseColor(245, 245, 245);

        public string ContentType => "application/pdf";

        public string GetFileName(HolidayReportDto data)
            => string.Format("HolidayRegister_{0:ddMMyyyy}_{1:ddMMyyyy}.pdf", data.FromDate, data.ToDate);

        public byte[] Generate(HolidayReportDto data)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 28f, 28f, 28f, 28f);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();
                BuildContent(doc, data);
                doc.Close();
                return ms.ToArray();
            }
        }

        private static void BuildContent(Document doc, HolidayReportDto data)
        {
            var title = new Paragraph(data.CompanyName, TitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 4f
            };
            doc.Add(title);

            var subTitle = new Paragraph(
                string.Format("Holiday Register For the period {0:dd/MM/yyyy} To {1:dd/MM/yyyy}", data.FromDate, data.ToDate),
                SubTitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 2f
            };
            doc.Add(subTitle);

            var printed = new Paragraph(string.Format("Printed On: {0:dd-MMM-yyyy}", data.PrintedOn), CellFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingAfter = 8f
            };
            doc.Add(printed);

            var table = new PdfPTable(5)
            {
                WidthPercentage = 100f,
                HeaderRows = 1
            };
            table.SetWidths(new[] { 32f, 85f, 150f, 220f, 70f });

            AddHeader(table, "S.No");
            AddHeader(table, "Holiday Date");
            AddHeader(table, "Holiday Name");
            AddHeader(table, "Description");
            AddHeader(table, "Status");

            int index = 1;
            bool alternate = false;
            foreach (var holiday in data.Rows)
            {
                var bg = alternate ? AltRowBg : BaseColor.WHITE;
                alternate = !alternate;

                AddCell(table, index++.ToString(), CellFont, bg, Element.ALIGN_CENTER);
                AddCell(table, holiday.HolidayDate.ToString("dd-MMM-yyyy"), CellBoldFont, bg, Element.ALIGN_CENTER);
                AddCell(table, holiday.HolidayName, CellBoldFont, bg, Element.ALIGN_LEFT);
                AddCell(table, holiday.Description ?? "", CellFont, bg, Element.ALIGN_LEFT);
                AddCell(table, holiday.IsActive ? "Active" : "Inactive", CellFont, bg, Element.ALIGN_CENTER);
            }

            if (data.Rows.Count == 0)
            {
                var emptyCell = new PdfPCell(new Phrase("No holidays found for the selected period.", CellFont))
                {
                    Colspan = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8f,
                    Border = Rectangle.BOX,
                    BorderColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(emptyCell);
            }

            doc.Add(table);
        }

        private static void AddHeader(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text, HeaderFont))
            {
                BackgroundColor = HeaderBg,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = new BaseColor(160, 160, 160),
                Padding = 5f
            };
            table.AddCell(cell);
        }

        private static void AddCell(PdfPTable table, string text, Font font, BaseColor bg, int align)
        {
            var cell = new PdfPCell(new Phrase(text ?? "", font))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.BOX,
                BorderColor = BaseColor.LIGHT_GRAY,
                Padding = 4f
            };
            table.AddCell(cell);
        }
    }
}
