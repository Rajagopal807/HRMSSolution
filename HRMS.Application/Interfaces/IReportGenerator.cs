using System;

namespace HRMS.Application.Interfaces
{
    // ══════════════════════════════════════════════════════════════════════════
    // OPEN / CLOSED PRINCIPLE — REPORT GENERATION
    //
    // IReportGenerator<TData> is the stable abstraction.
    // Every new report type (PDF, Excel, CSV, HTML…) is a new class that
    // implements this interface.  No existing class is ever modified.
    //
    // Hierarchy:
    //
    //   IReportGenerator<TData>
    //     │
    //     ├── AttendancePdfReport     : IReportGenerator<AttendanceReportDto>
    //     ├── AttendanceExcelReport   : IReportGenerator<AttendanceReportDto>
    //     │
    //     ├── EmployeePdfReport       : IReportGenerator<EmployeeReportDto>   (future)
    //     └── PayrollExcelReport      : IReportGenerator<PayrollReportDto>    (future)
    //
    // Adding a new format (e.g. CSV):
    //   → Create AttendanceCsvReport : IReportGenerator<AttendanceReportDto>
    //   → Register in AutofacConfig
    //   → Done.  Zero existing files touched.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Core OCP contract: closed for modification, open for extension.
    /// </summary>
    /// <typeparam name="TData">The DTO that carries report data.</typeparam>
    public interface IReportGenerator<TData>
    {
        /// <summary>MIME type of the generated output (e.g. "application/pdf").</summary>
        string ContentType { get; }

        /// <summary>Suggested file name (e.g. "AttendanceRegister_March2026.pdf").</summary>
        string GetFileName(TData data);

        /// <summary>Generates the report and returns the raw bytes.</summary>
        byte[] Generate(TData data);
    }

    // ── Attendance-specific service contract ──────────────────────────────────
    public interface IAttendanceReportService
    {
        /// <summary>Builds the attendance data DTO from the database.</summary>
        HRMS.Application.DTOs.AttendanceReportDto GetReportData(
            DateTime from, DateTime to, string companyName = "HRMS Company");

        /// <summary>Generates and returns PDF bytes.</summary>
        byte[] GeneratePdf(DateTime from, DateTime to);

        /// <summary>Generates and returns Excel bytes.</summary>
        byte[] GenerateExcel(DateTime from, DateTime to);
    }
}
