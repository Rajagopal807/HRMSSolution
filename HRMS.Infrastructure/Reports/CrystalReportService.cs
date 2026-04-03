using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Interfaces;

namespace HRMS.Infrastructure.Reports
{
    /// <summary>
    /// OCP: To add a new report type (e.g. PayrollReport), implement IReportService
    /// or extend this class — don't modify existing methods.
    /// </summary>
    public class CrystalReportService : IReportService
    {
        private readonly IUnitOfWork _uow;
        private readonly string _reportBasePath;

        public CrystalReportService(IUnitOfWork uow, string reportBasePath)
        {
            _uow            = uow;
            _reportBasePath = reportBasePath;
        }

        public byte[] GenerateEmployeeReport(string department = null, string status = null)
        {
            var employees = _uow.Employees.GetAll().ToList();

            if (!string.IsNullOrEmpty(department) && department != "All")
                employees = employees.Where(e => e.Department.ToString() == department).ToList();
            if (!string.IsNullOrEmpty(status) && status != "All")
                employees = employees.Where(e => e.Status.ToString() == status).ToList();

            var reportData = employees.Select(e => new EmployeeReportRow
            {
                EmployeeCode = e.EmployeeCode,
                FullName     = e.FullName,
                Department   = e.Department.ToString(),
                Designation  = e.Designation,
                Email        = e.Email,
                Phone        = e.Phone,
                BasicSalary  = e.BasicSalary,
                Allowances   = e.Allowances,
                GrossSalary  = e.GrossSalary,
                JoiningDate  = e.JoiningDate,
                Status       = e.Status.ToString()
            }).ToList();

            return ExportReport("EmployeeReport.rpt", reportData);
        }

        public byte[] GenerateLeaveReport(DateTime? from = null, DateTime? to = null)
        {
            var leaves = from.HasValue && to.HasValue
                ? _uow.Leaves.GetByDateRange(from.Value, to.Value).ToList()
                : _uow.Leaves.GetAll().ToList();

            var reportData = leaves.Select(l => new LeaveReportRow
            {
                EmployeeName = l.Employee?.FullName ?? "Unknown",
                Department   = l.Employee?.Department.ToString() ?? "",
                LeaveType    = l.LeaveType.ToString(),
                FromDate     = l.FromDate,
                ToDate       = l.ToDate,
                TotalDays    = l.TotalDays,
                Reason       = l.Reason,
                Status       = l.Status.ToString()
            }).ToList();

            return ExportReport("LeaveReport.rpt", reportData);
        }

        public byte[] GeneratePayrollReport(int month, int year)
        {
            var employees = _uow.Employees.GetActiveEmployees().ToList();
            var reportData = employees.Select(e => new PayrollReportRow
            {
                EmployeeCode = e.EmployeeCode,
                FullName     = e.FullName,
                Department   = e.Department.ToString(),
                Designation  = e.Designation,
                BasicSalary  = e.BasicSalary,
                Allowances   = e.Allowances,
                GrossSalary  = e.GrossSalary,
                Month        = new DateTime(year, month, 1).ToString("MMMM yyyy")
            }).ToList();

            return ExportReport("PayrollReport.rpt", reportData);
        }

        // ─── Private helper ────────────────────────────────────────────────────
        private byte[] ExportReport<T>(string reportFileName, List<T> data)
        {
            var reportPath = Path.Combine(_reportBasePath, reportFileName);
            using (var doc = new ReportDocument())
            {
                doc.Load(reportPath);
                doc.SetDataSource(data);
                using (var stream = doc.ExportToStream(ExportFormatType.PortableDocFormat))
                {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    return bytes;
                }
            }
        }
    }

    // ─── Report Row DTOs (match Crystal Report .rpt DataSource schema) ─────────
    public class EmployeeReportRow
    {
        public string   EmployeeCode { get; set; }
        public string   FullName     { get; set; }
        public string   Department   { get; set; }
        public string   Designation  { get; set; }
        public string   Email        { get; set; }
        public string   Phone        { get; set; }
        public decimal  BasicSalary  { get; set; }
        public decimal  Allowances   { get; set; }
        public decimal  GrossSalary  { get; set; }
        public DateTime JoiningDate  { get; set; }
        public string   Status       { get; set; }
    }

    public class LeaveReportRow
    {
        public string   EmployeeName { get; set; }
        public string   Department   { get; set; }
        public string   LeaveType    { get; set; }
        public DateTime FromDate     { get; set; }
        public DateTime ToDate       { get; set; }
        public int      TotalDays    { get; set; }
        public string   Reason       { get; set; }
        public string   Status       { get; set; }
    }

    public class PayrollReportRow
    {
        public string  EmployeeCode { get; set; }
        public string  FullName     { get; set; }
        public string  Department   { get; set; }
        public string  Designation  { get; set; }
        public decimal BasicSalary  { get; set; }
        public decimal Allowances   { get; set; }
        public decimal GrossSalary  { get; set; }
        public string  Month        { get; set; }
    }
}
