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
            return string.Format("AttendanceRegister_{0:MMMMyyyy}.xlsx", data.FromDate);
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
            int days = data.DaysInMonth;
            int totalCols = 2 + 31 + 1;   // EmpID + Name + 31 days + WorkDays

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

            SetHeader(ws.Cell(headerRow, 1), "Emp ID");
            SetHeader(ws.Cell(headerRow, 2), "Employee Name");
            for (int d = 1; d <= 31; d++)
                SetHeader(ws.Cell(headerRow, 2 + d), d.ToString());
            SetHeader(ws.Cell(headerRow, totalCols), "Work Days");

            // Freeze panes: keep EmpID + Name visible when scrolling right
            ws.SheetView.FreezeColumns(2);
            ws.SheetView.FreezeRows(headerRow);

            // ── Data rows ─────────────────────────────────────────────────────
            int currentRow = headerRow + 1;

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
                for (int d = 0; d < 31; d++)
                {
                    var dayDto = row.Days[d];
                    int col = 3 + d;
                    var cell = ws.Cell(currentRow, col);

                    cell.Style
                        .Fill.SetBackgroundColor(rowBg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.LightGray)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                        .Alignment.SetWrapText(true)
                        .Font.SetFontSize(7);

                    if (dayDto.IsPadDay)
                    {
                        cell.Value = "";
                        cell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(230, 230, 230));
                        continue;
                    }

                    // AA / HH / WO
                    if (!string.IsNullOrEmpty(dayDto.AttId)
                        && dayDto.AttId != "00"
                        && !string.IsNullOrEmpty(dayDto.FirstIn))
                    {
                        cell.Value = dayDto.AttId;
                        cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.DarkBlue);
                        continue;
                    }

                    // In/Out times
                    if (string.IsNullOrEmpty(dayDto.FirstIn))
                    {
                        string inStr = dayDto.FirstIn;
                        string outStr = string.IsNullOrEmpty(dayDto.Lastout)
                            ? dayDto.Lastout
                            : "--:--";
                        cell.Value = inStr + "\n" + outStr;
                        continue;
                    }

                    // Absent
                    cell.Value = "00";
                    cell.Style.Font.SetFontColor(XLColor.Gray);
                }

                // Work Days
                var wdCell = ws.Cell(currentRow, totalCols);
                wdCell.Value = row.WorkDays;
                wdCell.Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(rowBg)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                // Row height to accommodate two-line time cells
                ws.Row(currentRow).Height = 22;

                currentRow++;
            }

            // ── Column widths ─────────────────────────────────────────────────
            ws.Column(1).Width = 12;   // Emp ID
            ws.Column(2).Width = 22;   // Employee Name
            for (int d = 1; d <= 31; d++)
                ws.Column(2 + d).Width = 7;   // Day columns
            ws.Column(totalCols).Width = 9;   // Work Days

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
    }
}
