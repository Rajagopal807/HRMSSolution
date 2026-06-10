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
        // Column layout constants
        private const int EmpIdCol = 1;
        private const int EmpNameCol = 2;
        private const int FirstDayCol = 3;   // Day 1 "In" column

        // Returns the "In" column index for a given 1-based day number
        private static int InCol(int day) => FirstDayCol + (day - 1) * 2;
        private static int OutCol(int day) => InCol(day) + 1;
        private static int TotalCols(int days) { return 2 + (days * 2); }

        public string ContentType =>
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public string GetFileName(GroupedAttendanceReportDto data)
        {
            return string.Format("AttendanceRegister_{0}_{1:ddMMMyyyy}_to_{2:ddMMMyyyy}.xlsx",
                data.GroupingLabel, data.FromDate, data.ToDate);
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
            int days = data.DaysInRange;
            int totalCols = TotalCols(days);

            // ── Row 1: Company name ───────────────────────────────────────────
            ws.Cell(1, 1).Value = data.CompanyName;
            ws.Range(1, 1, 1, totalCols).Merge();
            StyleTitle(ws.Cell(1, 1), 13);

            // ── Row 2: Report title ───────────────────────────────────────────
            ws.Cell(2, 1).Value = string.Format(
                "Attendance Register ({0} Wise) For the period {1:dd/MM/yyyy} To {2:dd/MM/yyyy}",
                data.GroupingLabel, data.FromDate, data.ToDate);
            ws.Range(2, 1, 2, totalCols).Merge();
            StyleTitle(ws.Cell(2, 1), 10);

            // ── Row 3: Print date ─────────────────────────────────────────────
            ws.Cell(3, totalCols).Value =
                data.PrintedOn.ToString("dd-MMM-yyyy") + "   Page No : 1";
            ws.Cell(3, totalCols).Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Font.SetFontSize(8);

            // ── Freeze first two data columns ─────────────────────────────────
            ws.SheetView.FreezeColumns(2);

            int currentRow = 5; // start after header rows + one blank
            int grandEmpCount = 0;

            foreach (var group in data.Groups)
            {
                // ── Group header row ──────────────────────────────────────────
                var ghRange = ws.Range(currentRow, 1, currentRow, totalCols).Merge();
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

                // ── Day-number header row (merged In+Out per day) ─────────────
                // "Emp ID" cell — merged across two column-header rows
                ws.Range(currentRow, EmpIdCol, currentRow + 1, EmpIdCol).Merge();
                SetColHeader(ws.Cell(currentRow, EmpIdCol), "Emp ID");

                // "Employee Name" cell — merged across two column-header rows
                ws.Range(currentRow, EmpNameCol, currentRow + 1, EmpNameCol).Merge();
                SetColHeader(ws.Cell(currentRow, EmpNameCol), "Employee Name");

                bool crossesMonth = data.FromDate.Month != data.ToDate.Month || data.FromDate.Year != data.ToDate.Year;
                for (int d = 1; d <= days; d++)
                {
                    int inCol = InCol(d);
                    int outCol = OutCol(d);
                    var date = data.FromDate.Date.AddDays(d - 1);

                    var dayBg = XLColor.FromArgb(200, 200, 200);

                    // Merge In+Out columns for the day number label
                    var dayMerge = ws.Range(currentRow, inCol, currentRow, outCol).Merge();
                    dayMerge.Value = crossesMonth ? date.ToString("dd/MM") : date.Day.ToString();
                    dayMerge.Style
                        .Font.SetBold(true).Font.SetFontSize(7)
                        .Fill.SetBackgroundColor(dayBg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.Gray)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                }
                ws.Row(currentRow).Height = 14;
                currentRow++;

                // ── In / Out sub-header row ───────────────────────────────────
                for (int d = 1; d <= days; d++)
                {
                    var subBg = XLColor.FromArgb(210, 210, 210);

                    SetSubHeader(ws.Cell(currentRow, InCol(d)), "In", subBg);
                    SetSubHeader(ws.Cell(currentRow, OutCol(d)), "Out", subBg);
                }
                ws.Row(currentRow).Height = 13;
                currentRow++;

                // ── Data rows ─────────────────────────────────────────────────
                bool alt = false;
                foreach (var row in group.Rows)
                {
                    var rowBg = alt
                        ? XLColor.FromArgb(245, 245, 245)
                        : XLColor.White;
                    alt = !alt;

                    StyleDataCell(ws.Cell(currentRow, EmpIdCol),
                        row.EmployeeCode, rowBg, bold: true,
                        align: XLAlignmentHorizontalValues.Left);

                    StyleDataCell(ws.Cell(currentRow, EmpNameCol),
                        row.EmployeeName, rowBg, bold: false,
                        align: XLAlignmentHorizontalValues.Left);

                    for (int d = 0; d < days; d++)
                    {
                        if (d >= row.Days.Count)
                        {
                            continue;
                        }

                        int dayNumber = d + 1;
                        var day = row.Days[d];
                        var padBg = day.IsPadDay ? XLColor.FromArgb(230, 230, 230) : rowBg;

                        var inCell = ws.Cell(currentRow, InCol(dayNumber));
                        var outCell = ws.Cell(currentRow, OutCol(dayNumber));

                        // Common style for both cells
                        ApplyTimeCellStyle(inCell, padBg);
                        ApplyTimeCellStyle(outCell, padBg);

                        if (!day.IsPadDay)
                        {
                            if (!string.IsNullOrEmpty(day.AttId)
                                && day.AttId != "00"
                                && string.IsNullOrEmpty(day.FirstIn))
                            {
                                // Abbreviation (e.g. WW, A, H) — merge In+Out and centre
                                var abbrvRange = ws.Range(currentRow, InCol(dayNumber),
                                                          currentRow, OutCol(dayNumber)).Merge();
                                abbrvRange.Value = day.AttId;
                                abbrvRange.Style
                                    .Fill.SetBackgroundColor(padBg)
                                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                    .Border.SetOutsideBorderColor(XLColor.LightGray)
                                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                                    .Font.SetBold(true)
                                    .Font.SetFontSize(7)
                                    .Font.SetFontColor(XLColor.DarkBlue);
                            }
                            else if (!string.IsNullOrEmpty(day.FirstIn))
                            {
                                inCell.Value = day.FirstIn;
                                outCell.Value = !string.IsNullOrEmpty(day.Lastout)
                                    ? day.Lastout
                                    : "--:--";
                            }
                            else
                            {
                                inCell.Value = "00";
                                outCell.Value = "00";
                                inCell.Style.Font.SetFontColor(XLColor.Gray);
                                outCell.Style.Font.SetFontColor(XLColor.Gray);
                            }
                        }
                    }

                    ws.Row(currentRow).Height = 16;
                    currentRow++;
                }

                // ── Group subtotal row ────────────────────────────────────────
                var stRange = ws.Range(currentRow, 1, currentRow, totalCols).Merge();
                stRange.Value = string.Format("  Subtotal (No.Of.Employees) : {0}",
                    group.EmployeeCount);
                stRange.Style
                    .Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(220, 230, 245))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.Gray)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                ws.Row(currentRow).Height = 16;
                currentRow++;

                grandEmpCount += group.EmployeeCount;
            }

            // ── Grand total row ───────────────────────────────────────────────
            var gtRange = ws.Range(currentRow, 1, currentRow, totalCols).Merge();
            gtRange.Value = string.Format(
                "  Grand Total    Groups: {0}    Total Employees: {1}",
                data.Groups.Count, grandEmpCount);
            gtRange.Style
                .Font.SetBold(true).Font.SetFontSize(9)
                .Fill.SetBackgroundColor(XLColor.FromArgb(180, 200, 230))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Row(currentRow).Height = 18;
            currentRow += 2;

            AddLeaveTypeFooter(ws, data, currentRow);

            // ── Column widths ─────────────────────────────────────────────────
            ws.Column(EmpIdCol).Width = 13;
            ws.Column(EmpNameCol).Width = 24;
            for (int d = 1; d <= days; d++)
            {
                ws.Column(InCol(d)).Width = 6;
                ws.Column(OutCol(d)).Width = 6;
            }

            // ── Print settings ────────────────────────────────────────────────
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A3Paper;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.SetRowsToRepeatAtTop(1, 7); // includes both header rows
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

        private static void SetSubHeader(IXLCell cell, string text, XLColor bg)
        {
            cell.Value = text;
            cell.Style
                .Font.SetBold(true).Font.SetFontSize(7)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.Gray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        private static void ApplyTimeCellStyle(IXLCell cell, XLColor bg)
        {
            cell.Style
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.LightGray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Font.SetFontSize(7);
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

        private static void AddLeaveTypeFooter(IXLWorksheet ws, GroupedAttendanceReportDto data, int startRow)
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

            SetColHeader(ws.Cell(startRow + 1, 1), "LeaveType ID");
            SetColHeader(ws.Cell(startRow + 1, 2), "Leave Description");

            int row = startRow + 2;
            foreach (var leaveType in data.LeaveTypes)
            {
                StyleDataCell(ws.Cell(row, 1), leaveType.LeaveTypeID, XLColor.White, bold: true,
                    align: XLAlignmentHorizontalValues.Left);
                StyleDataCell(ws.Cell(row, 2), leaveType.Description, XLColor.White, bold: false,
                    align: XLAlignmentHorizontalValues.Left);
                row++;
            }
        }
    }
}
