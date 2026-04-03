using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Data;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Core.ExcelPackage;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<IGrouping<string, ReportDto>> GetAttendanceReport(DateTime? from, DateTime? to)
    {
        //var query = from a in _db.Attendances
        //            join e in _db.Employees on a.EmployeeId equals e.Id
        //            select new ReportDto
        //            {
        //                Department = e.Department,
        //                EmployeeName = e.FullName,
        //                Date = a.Date,
        //                IsPresent = a.IsPresent
        //            };

        //if (from.HasValue)
        //    query = query.Where(x => x.Date >= from.Value);

        //if (to.HasValue)
        //    query = query.Where(x => x.Date <= to.Value);

        //return query.ToList().GroupBy(x => x.Department.ToString()).ToList();
        return new List<IGrouping<string, ReportDto>>();
    }

    // ================= PDF =================
    public byte[] ExportPdf(DateTime? from, DateTime? to)
    {
        var data = GetAttendanceReport(from, to);

        using (var ms = new MemoryStream())
        {
            Document doc = new Document();
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            doc.Add(new Paragraph("Attendance Report\n\n"));

            foreach (var group in data)
            {
                doc.Add(new Paragraph("Department: " + group.Key));

                PdfPTable table = new PdfPTable(3);
                table.AddCell("Employee");
                table.AddCell("Date");
                table.AddCell("Status");

                foreach (var item in group)
                {
                    table.AddCell(item.EmployeeName);
                    table.AddCell(item.Date.ToShortDateString());
                    table.AddCell(item.IsPresent ? "Present" : "Absent");
                }

                doc.Add(table);
                doc.Add(new Paragraph("Total: " + group.Count()));
                doc.Add(new Paragraph("\n"));
            }

            doc.Close();
            return ms.ToArray();
        }
    }

    // ================= EXCEL =================
    public byte[] ExportExcel(DateTime? from, DateTime? to)
    {
        using (var ms = new MemoryStream())
        {
            using (var package = new ExcelPackage(ms))
            {
                var sheet = package.Workbook.Worksheets.Add("Attendance");

                int row = 1;
                var data = GetAttendanceReport(from, to);

                foreach (var group in data)
                {
                    sheet.Cell(row, 1).Value = "Department: " + group.Key;
                    //sheet.Cell(row, 1).Style.f.Bold = true;
                    row++;

                    sheet.Cell(row, 1).Value = "Employee";
                    sheet.Cell(row, 2).Value = "Date";
                    sheet.Cell(row, 3).Value = "Status";
                    row++;

                    foreach (var item in group)
                    {
                        sheet.Cell(row, 1).Value = item.EmployeeName;
                        sheet.Cell(row, 2).Value = item.Date.ToShortDateString();
                        sheet.Cell(row, 3).Value = item.IsPresent ? "Present" : "Absent";
                        row++;
                    }

                    sheet.Cell(row, 1).Value = "Total: " + group.Count();
                    row += 2;
                }

                //return package.GetAsByteArray();
            }
            return ms.ToArray();
        }
        
    }
}