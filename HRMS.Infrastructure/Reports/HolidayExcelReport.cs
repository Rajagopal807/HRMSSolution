using ClosedXML.Excel;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using System.IO;

namespace HRMS.Infrastructure.Reports
{
    public class HolidayExcelReport : IReportGenerator<HolidayReportDto>
    {
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public string GetFileName(HolidayReportDto data)
            => string.Format("HolidayRegister_{0:ddMMyyyy}_{1:ddMMyyyy}.xlsx", data.FromDate, data.ToDate);

        public byte[] Generate(HolidayReportDto data)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Holidays");
                BuildSheet(ws, data);

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void BuildSheet(IXLWorksheet ws, HolidayReportDto data)
        {
            ws.Cell(1, 1).Value = data.CompanyName;
            ws.Range(1, 1, 1, 5).Merge();
            ws.Cell(1, 1).Style.Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = string.Format("Holiday Register For the period {0:dd/MM/yyyy} To {1:dd/MM/yyyy}", data.FromDate, data.ToDate);
            ws.Range(2, 1, 2, 5).Merge();
            ws.Cell(2, 1).Style.Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 5).Value = string.Format("Printed On: {0:dd-MMM-yyyy}", data.PrintedOn);
            ws.Cell(3, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Font.SetFontSize(9);

            string[] headers = { "S.No", "Holiday Date", "Holiday Name", "Description", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(5, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(200, 200, 200))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            int row = 6;
            int index = 1;
            foreach (var holiday in data.Rows)
            {
                ws.Cell(row, 1).Value = index++;
                ws.Cell(row, 2).Value = holiday.HolidayDate;
                ws.Cell(row, 2).Style.DateFormat.Format = "dd-MMM-yyyy";
                ws.Cell(row, 3).Value = holiday.HolidayName;
                ws.Cell(row, 4).Value = holiday.Description;
                ws.Cell(row, 5).Value = holiday.IsActive ? "Active" : "Inactive";

                var range = ws.Range(row, 1, row, 5);
                range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                range.Style.Fill.SetBackgroundColor(row % 2 == 0 ? XLColor.White : XLColor.FromArgb(245, 245, 245));
                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;
            }

            if (data.Rows.Count == 0)
            {
                ws.Cell(row, 1).Value = "No holidays found for the selected period.";
                ws.Range(row, 1, row, 5).Merge();
                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Font.SetItalic(true);
            }

            ws.Columns().AdjustToContents();
            ws.Column(4).Width = 36;
            ws.Column(4).Style.Alignment.SetWrapText(true);
            ws.SheetView.FreezeRows(5);
            ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.FitToPages(1, 0);
        }
    }
}
