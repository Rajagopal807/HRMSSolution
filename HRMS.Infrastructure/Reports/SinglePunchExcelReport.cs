using ClosedXML.Excel;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using System.IO;

namespace HRMS.Infrastructure.Reports
{
    public class SinglePunchExcelReport : IReportGenerator<SinglePunchReportDto>
    {
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public string GetFileName(SinglePunchReportDto data)
            => string.Format("SinglePunchReport_{0:ddMMyyyy}_{1:ddMMyyyy}.xlsx", data.FromDate, data.ToDate);

        public byte[] Generate(SinglePunchReportDto data)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Single Punch");
                BuildSheet(ws, data);

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void BuildSheet(IXLWorksheet ws, SinglePunchReportDto data)
        {
            ws.Cell(1, 1).Value = data.CompanyName;
            ws.Range(1, 1, 1, 9).Merge();
            ws.Cell(1, 1).Style.Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = string.Format("Single Punch Report For the period {0:dd/MM/yyyy} To {1:dd/MM/yyyy}", data.FromDate, data.ToDate);
            ws.Range(2, 1, 2, 9).Merge();
            ws.Cell(2, 1).Style.Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 9).Value = string.Format("Printed On: {0:dd-MMM-yyyy}", data.PrintedOn);
            ws.Cell(3, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Font.SetFontSize(9);

            string[] headers = { "S.No", "Emp ID", "Employee Name", "Department", "Date", "Punch Time", "I/O", "Reader", "Remarks" };
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
            foreach (var punch in data.Rows)
            {
                ws.Cell(row, 1).Value = index++;
                ws.Cell(row, 2).Value = punch.EmployeeId;
                ws.Cell(row, 3).Value = punch.EmployeeName;
                ws.Cell(row, 4).Value = punch.DepartmentName;
                ws.Cell(row, 5).Value = punch.AttendanceDate;
                ws.Cell(row, 5).Style.DateFormat.Format = "dd-MMM-yyyy";
                ws.Cell(row, 6).Value = punch.PunchTime;
                ws.Cell(row, 6).Style.DateFormat.Format = "HH:mm";
                ws.Cell(row, 7).Value = punch.IOFlag;
                ws.Cell(row, 8).Value = punch.BadgeReaderNo;
                ws.Cell(row, 9).Value = punch.Remarks;

                var range = ws.Range(row, 1, row, 9);
                range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                range.Style.Fill.SetBackgroundColor(row % 2 == 0 ? XLColor.White : XLColor.FromArgb(245, 245, 245));
                ws.Range(row, 1, row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range(row, 5, row, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;
            }

            if (data.Rows.Count == 0)
            {
                ws.Cell(row, 1).Value = "No single punch records found for the selected period.";
                ws.Range(row, 1, row, 9).Merge();
                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Font.SetItalic(true);
            }

            ws.Columns().AdjustToContents();
            ws.Column(9).Width = 34;
            ws.Column(9).Style.Alignment.SetWrapText(true);
            ws.SheetView.FreezeRows(5);
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.FitToPages(1, 0);
        }
    }
}
