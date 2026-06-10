using System;
using System.IO;
using ClosedXML.Excel;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Reports
{
    /// <summary>
    /// OCP: Implements IReportGenerator&lt;AttendanceReportDto&gt; for Excel output.
    /// Uses ClosedXML (free, MIT licence).
    /// NuGet: Install-Package ClosedXML -Version 0.102.2
    /// </summary>
    public class AttendanceExcelReport : IReportGenerator<AttendanceReportDto>
    {
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public string GetFileName(AttendanceReportDto data)
        {
            return string.Format("AttendanceRegister_{0:ddMMMyyyy}_to_{1:ddMMMyyyy}.xlsx",
                data.FromDate, data.ToDate);
        }

        public byte[] Generate(AttendanceReportDto data)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Attendance Register");
                BuildSheet(ws, data);

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        // ── Sheet Builder ─────────────────────────────────────────────────────
        private static void BuildSheet(IXLWorksheet ws, AttendanceReportDto data)
        {
            int totalDays = data.DaysInRange;
            int days = totalDays; //data.DaysInMonth;
            int totalCols = 2 + (days * 2); // EmpID + Name + 31 days * 2(In/Out)

            // ── Row 1: Company name ───────────────────────────────────────────
            ws.Cell(1, 1).Value = data.CompanyName;
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ── Row 2: Report title ───────────────────────────────────────────
            string title = string.Format(
                "Attendance Register For the period {0:dd/MM/yyyy} To {1:dd/MM/yyyy}",
                data.FromDate, data.ToDate);

            ws.Cell(2, 1).Value = title;
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(10)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ── Row 3: Print date + Page No (right-aligned in last column) ────
            ws.Cell(3, totalCols).Value =
                string.Format("{0:dd-MMM-yyyy}    Page No : {1}",
                              data.PrintedOn, data.PageNo);
            ws.Cell(3, totalCols).Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Font.SetFontSize(8);

            // ── Row 5: Column headers ─────────────────────────────────────────
            int headerRow = 5;
            int subHeaderRow = headerRow + 1;

            // ── FULL HEADER RANGE (apply border once)
            var fullHeader = ws.Range(headerRow, 1, subHeaderRow, totalCols);
            fullHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            fullHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;


            // ── Emp ID (MERGED VERTICALLY)
            var empRange = ws.Range(headerRow, 1, subHeaderRow, 1);
            empRange.Merge();
            ws.Cell(headerRow, 1).Value = "Emp ID";
            SetHeader(ws.Cell(headerRow, 1));

            // 🔥 FIX: remove right border to avoid double line
            empRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            empRange.Style.Border.RightBorderColor = XLColor.Gray;


            // ── Employee Name (MERGED VERTICALLY)
            var nameRange = ws.Range(headerRow, 2, subHeaderRow, 2);
            nameRange.Merge();
            ws.Cell(headerRow, 2).Value = "Employee Name";
            SetHeader(ws.Cell(headerRow, 2));

            // 🔥 FIX: remove left border to avoid double line
            nameRange.Style.Border.LeftBorder = XLBorderStyleValues.None;


            // ── Day Headers
            int col = 3;
            bool crossesMonth = data.FromDate.Month != data.ToDate.Month || data.FromDate.Year != data.ToDate.Year;

            for (int d = 0; d < days; d++)
            {
                var date = data.FromDate.Date.AddDays(d);
                var dayRange = ws.Range(headerRow, col, headerRow, col + 1);
                dayRange.Merge();

                SetHeader(ws.Cell(headerRow, col), crossesMonth ? date.ToString("dd/MM") : date.Day.ToString());

                // 🔥 FIX: avoid double line between header & subheader
                dayRange.Style.Border.BottomBorder = XLBorderStyleValues.None;
                dayRange.Style.Border.TopBorderColor = XLColor.Gray;
                dayRange.Style.Border.RightBorderColor = XLColor.Gray;

                col += 2;
            }


            // ── Sub Header (In / Out)
            col = 3;

            for (int d = 1; d <= days; d++)
            {
                var inCell = ws.Cell(subHeaderRow, col++);
                var outCell = ws.Cell(subHeaderRow, col++);

                SetHeader(inCell, "In");
                SetHeader(outCell, "Out");

                // Ensure proper border (only here)
                inCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                outCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            }


            // Freeze panes
            ws.SheetView.FreezeColumns(2);
            ws.SheetView.FreezeRows(subHeaderRow);

            // ── Data rows ─────────────────────────────────────────────────────
            int currentRow = headerRow + 2;

            foreach (var row in data.Rows)
            {
                bool isAlt = (currentRow % 2 == 0);
                var rowBg = isAlt
                    ? XLColor.FromArgb(245, 245, 245)
                    : XLColor.White;

                // Emp ID
                var idCell = ws.Cell(currentRow, 1);
                idCell.Value = row.EmployeeCode;
                idCell.Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(rowBg)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.Gray)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                // Employee Name
                var nameCell = ws.Cell(currentRow, 2);
                nameCell.Value = row.EmployeeName;
                nameCell.Style
                    .Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(rowBg)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.Gray)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                // Day columns 1–31
                for (int d = 0; d < days; d++)
                {
                    if (d >= row.Days.Count)
                    {
                        continue;
                    }

                    var dayDto = row.Days[d];

                    col = 3 + (d * 2);
                    var inCell = ws.Cell(currentRow, col);
                    var outCell = ws.Cell(currentRow, col + 1);

                    var range = ws.Range(currentRow, col, currentRow, col + 1);

                    // Style
                    range.Style
                        .Fill.SetBackgroundColor(rowBg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.LightGray)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                        .Alignment.SetWrapText(true)
                        .Font.SetFontSize(7);

                    if (dayDto.IsPadDay)
                    {
                        range.Merge();
                        inCell.Value = "";
                        range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(230, 230, 230));
                        continue;
                    }

                    // AA / HH / WO
                    if (!string.IsNullOrEmpty(dayDto.AttId)
                        && dayDto.AttId != "00"
                        && string.IsNullOrEmpty(dayDto.FirstIn))
                    {
                        range.Merge();
                        inCell.Value = dayDto.AttId;
                        range.Style.Font.SetBold(true).Font.SetFontColor(XLColor.DarkBlue);
                        range.Style.Border.RightBorderColor = XLColor.Gray;
                        continue;
                    }

                    // In/Out times
                    if (!string.IsNullOrEmpty(dayDto.FirstIn))
                    {
                        inCell.Value = dayDto.FirstIn;
                        outCell.Value = !string.IsNullOrEmpty(dayDto.Lastout)
                            ? dayDto.Lastout
                            : "--:--";
                        range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        range.Style.Border.RightBorderColor = XLColor.Gray;
                        continue;
                    }

                    // Absent
                    range.Merge();
                    inCell.Value = "00";
                    inCell.Style.Font.SetFontColor(XLColor.Gray);
                    inCell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    inCell.Style.Border.RightBorderColor = XLColor.Gray;
                }
                ws.Row(currentRow).Height = 26;

                currentRow++;
            }

            AddLeaveTypeFooter(ws, data, currentRow + 1);

            // ── Column widths ─────────────────────────────────────────────────
            ws.Column(1).Width = 12;   // Emp ID
            ws.Column(2).Width = 22;   // Employee Name

            int colIndex = 3;
            for (int d = 0; d < days; d++)
            {
                ws.Column(colIndex++).Width = 7; // In
                ws.Column(colIndex++).Width = 7; // Out
            }

            // ── Print settings ────────────────────────────────────────────────
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A3Paper;
            ws.PageSetup.FitToPages(1, 0);   // Fit all columns to 1 page wide
            ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
        }

        // ── Helper: styled header cell ────────────────────────────────────────
        private static void SetHeader(IXLCell cell, string text)
        {
            cell.Value = text;
            cell.Style
                .Font.SetBold(true)
                .Font.SetFontSize(8)
                .Fill.SetBackgroundColor(XLColor.FromArgb(200, 200, 200))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
        }

        private static void SetHeader(IXLCell cell)
        {
            cell.Style
                .Font.SetBold(true)
                .Font.SetFontSize(8)
                .Fill.SetBackgroundColor(XLColor.FromArgb(200, 200, 200))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
        }

        private static void AddLeaveTypeFooter(IXLWorksheet ws, AttendanceReportDto data, int startRow)
        {
            if (data.LeaveTypes == null || data.LeaveTypes.Count == 0)
                return;

            ws.Cell(startRow, 1).Value = "Leave Types";
            ws.Range(startRow, 1, startRow, 2).Merge();
            ws.Cell(startRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(8)
                .Fill.SetBackgroundColor(XLColor.FromArgb(200, 200, 200))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            SetHeader(ws.Cell(startRow + 1, 1), "LeaveType ID");
            SetHeader(ws.Cell(startRow + 1, 2), "Leave Description");

            int row = startRow + 2;
            foreach (var leaveType in data.LeaveTypes)
            {
                ws.Cell(row, 1).Value = leaveType.LeaveTypeID;
                ws.Cell(row, 2).Value = leaveType.Description;

                var range = ws.Range(row, 1, row, 2);
                range.Style
                    .Font.SetFontSize(8)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.Gray)
                    .Border.SetInsideBorderColor(XLColor.LightGray)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                row++;
            }
        }
    }
}
