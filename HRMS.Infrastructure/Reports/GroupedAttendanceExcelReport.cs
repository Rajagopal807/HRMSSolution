using System;
using System.IO;
using ClosedXML.Excel;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Reports
{
    /// <summary>
    /// OCP: New class implementing IReportGenerator&lt;GroupedAttendanceReportDto&gt;.
    /// AttendanceExcelReport (employee-wise) is never modified.
    ///
    /// Sheet layout per group:
    ///   Row 1   : Company name (merged, centred)
    ///   Row 2   : Report title (merged, centred)
    ///   Row 3   : Print date / Page No (right-aligned)
    ///   Row 4   : blank spacer
    ///   For each group:
    ///     GroupHeader row  – dark navy fill, white bold text
    ///     ColumnHeader row – grey fill
    ///     Data rows        – alternating white/light grey
    ///     Subtotal row     – light blue fill, bold
    ///   Grand total row at the bottom – medium blue fill, bold
    /// </summary>
    public class GroupedAttendanceExcelReport : IReportGenerator<GroupedAttendanceReportDto>
    {
        public string ContentType =>
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public string GetFileName(GroupedAttendanceReportDto data)
        {
            return string.Format("AttendanceRegister_{0}_{1:MMMMyyyy}.xlsx",
                data.GroupingLabel, data.FromDate);
        }

        public byte[] Generate(GroupedAttendanceReportDto data)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(data.GroupingLabel + " Wise");
                BuildSheet(ws, data);

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        // ── Sheet builder ─────────────────────────────────────────────────────
        private static void BuildSheet(IXLWorksheet ws, GroupedAttendanceReportDto data)
        {
            int totalCols = 34; // EmpID + Name + Day1-31 + WorkDays + pad
            int days = data.DaysInMonth;

            // ── Row 1: Company name ───────────────────────────────────────────
            ws.Cell(1, 1).Value = data.CompanyName;
            ws.Range(1, 1, 1, totalCols - 1).Merge();
            StyleTitle(ws.Cell(1, 1), 13);

            // ── Row 2: Report title ───────────────────────────────────────────
            ws.Cell(2, 1).Value = string.Format(
                "Attendance Register ({0} Wise) For the period {1:dd/MM/yyyy} To {2:dd/MM/yyyy}",
                data.GroupingLabel, data.FromDate, data.ToDate);
            ws.Range(2, 1, 2, totalCols - 1).Merge();
            StyleTitle(ws.Cell(2, 1), 10);

            // ── Row 3: Print date ─────────────────────────────────────────────
            ws.Cell(3, totalCols - 1).Value =
                data.PrintedOn.ToString("dd-MMM-yyyy") + "   Page No : 1";
            ws.Cell(3, totalCols - 1).Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Font.SetFontSize(8);

            // ── Freeze first two data columns ─────────────────────────────────
            ws.SheetView.FreezeColumns(2);

            int currentRow = 5;  // start after header rows + one blank

            int grandWorkDays = 0;
            int grandEmpCount = 0;

            foreach (var group in data.Groups)
            {
                // ── Group header row ──────────────────────────────────────────
                var ghRange = ws.Range(currentRow, 1, currentRow, totalCols - 1).Merge();
                ghRange.Value = string.Format("  {0}: {1}   —   {2} Employee{3}",
                    data.GroupingLabel, group.GroupName,
                    group.EmployeeCount, group.EmployeeCount == 1 ? "" : "s");
                ghRange.Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(9)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(51, 71, 121))
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                ws.Row(currentRow).Height = 18;
                currentRow++;

                // ── Column header row ─────────────────────────────────────────
                SetColHeader(ws.Cell(currentRow, 1), "Emp ID");
                SetColHeader(ws.Cell(currentRow, 2), "Employee Name");
                for (int d = 1; d <= 31; d++)
                {
                    var hCell = ws.Cell(currentRow, 2 + d);
                    hCell.Value = d;
                    hCell.Style
                        .Font.SetBold(true).Font.SetFontSize(7)
                        .Fill.SetBackgroundColor(d <= days
                            ? XLColor.FromArgb(200, 200, 200)
                            : XLColor.FromArgb(235, 235, 235))
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.Gray)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                }
                //SetColHeader(ws.Cell(currentRow, 34), "Work Days");
                ws.Row(currentRow).Height = 16;
                currentRow++;

                // ── Data rows ─────────────────────────────────────────────────
                bool alt = false;
                foreach (var row in group.Rows)
                {
                    var rowBg = alt
                        ? XLColor.FromArgb(245, 245, 245)
                        : XLColor.White;
                    alt = !alt;

                    // Emp ID
                    StyleDataCell(ws.Cell(currentRow, 1), row.EmployeeCode, rowBg,
                        bold: true, align: XLAlignmentHorizontalValues.Left);

                    // Name
                    StyleDataCell(ws.Cell(currentRow, 2), row.EmployeeName, rowBg,
                        bold: false, align: XLAlignmentHorizontalValues.Left);

                    // Days 1–31
                    for (int d = 0; d < 31; d++)
                    {
                        var day = row.Days[d];
                        var cell = ws.Cell(currentRow, 3 + d);
                        var bg = day.IsPadDay ? XLColor.FromArgb(230, 230, 230) : rowBg;

                        cell.Style
                            .Fill.SetBackgroundColor(bg)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                            .Border.SetOutsideBorderColor(XLColor.LightGray)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                            .Alignment.SetWrapText(true)
                            .Font.SetFontSize(7);

                        if (!day.IsPadDay)
                        {
                            if (!string.IsNullOrEmpty(day.AttId)
                                && day.AttId != "00"
                                && string.IsNullOrEmpty(day.FirstIn))
                            {
                                cell.Value = day.AttId;
                                cell.Style.Font.SetBold(true)
                                         .Font.SetFontColor(XLColor.DarkBlue);
                            }
                            else if (!string.IsNullOrEmpty(day.FirstIn))
                            {
                                cell.Value = day.FirstIn;
                                cell.Value += "\n";
                                cell.Value += !string.IsNullOrEmpty(day.Lastout) ? day.Lastout : "--:--";
                            }
                            else
                            {
                                cell.Value = "00";
                                cell.Style.Font.SetFontColor(XLColor.Gray);
                            }
                        }
                    }

                    // Work Days
                    //StyleDataCell(ws.Cell(currentRow, 34), row.WorkDays.ToString(),
                    //    rowBg, bold: true,
                    //    align: XLAlignmentHorizontalValues.Center);

                    ws.Row(currentRow).Height = 22;
                    currentRow++;
                }

                // ── Group subtotal row ────────────────────────────────────────
                var stRange = ws.Range(currentRow, 1, currentRow, 33).Merge();
                stRange.Value = string.Format("  Subtotal (No.Of.Employees) : {0}", group.EmployeeCount);
                stRange.Style
                    .Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(220, 230, 245))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.Gray)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                //var stWd = ws.Cell(currentRow, 34);
                //stWd.Value = group.TotalWorkDays;
                //stWd.Style
                //    .Font.SetBold(true).Font.SetFontSize(8)
                //    .Fill.SetBackgroundColor(XLColor.FromArgb(220, 230, 245))
                //    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                //    .Border.SetOutsideBorderColor(XLColor.Gray)
                //    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                //    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                ws.Row(currentRow).Height = 16;
                currentRow++;

                //grandWorkDays += group.TotalWorkDays;
                grandEmpCount += group.EmployeeCount;
            }

            // ── Grand total row ───────────────────────────────────────────────
            var gtRange = ws.Range(currentRow, 1, currentRow, 33).Merge();
            gtRange.Value = string.Format("  Grand Total    Groups: {0}    Total Employees: {1}", data.Groups.Count, grandEmpCount);
            gtRange.Style
                .Font.SetBold(true).Font.SetFontSize(9)
                .Fill.SetBackgroundColor(XLColor.FromArgb(180, 200, 230))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            //var gtWd = ws.Cell(currentRow, 34);
            //gtWd.Value = grandWorkDays;
            //gtWd.Style
            //    .Font.SetBold(true).Font.SetFontSize(9)
            //    .Fill.SetBackgroundColor(XLColor.FromArgb(180, 200, 230))
            //    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            //    .Border.SetOutsideBorderColor(XLColor.Gray)
            //    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            //    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(currentRow).Height = 18;

            // ── Column widths ─────────────────────────────────────────────────
            ws.Column(1).Width = 13;   // Emp ID
            ws.Column(2).Width = 24;   // Name
            for (int d = 1; d <= 31; d++)
                ws.Column(2 + d).Width = 7;
            //ws.Column(34).Width = 10;  // Work Days

            // ── Print settings ────────────────────────────────────────────────
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A3Paper;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.SetRowsToRepeatAtTop(1, 6);
        }

        // ── Style helpers ─────────────────────────────────────────────────────
        private static void StyleTitle(IXLCell cell, int size)
        {
            cell.Style
                .Font.SetBold(true).Font.SetFontSize(size)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        private static void SetColHeader(IXLCell cell, string text)
        {
            cell.Value = text;
            cell.Style
                .Font.SetBold(true).Font.SetFontSize(8)
                .Fill.SetBackgroundColor(XLColor.FromArgb(200, 200, 200))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
        }

        private static void StyleDataCell(IXLCell cell, string text,
            XLColor bg, bool bold, XLAlignmentHorizontalValues align)
        {
            cell.Value = text;
            cell.Style
                .Font.SetBold(bold).Font.SetFontSize(8)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetHorizontal(align)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }
    }
}
