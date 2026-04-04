using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Enums;
using HRMS.Web.Filters;
using HRMS.Web.ViewModels;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HRMS.Web.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IDesignationService _designationService;

        public EmployeeController(IEmployeeService employeeService, IDepartmentService departmentService, IDesignationService designationService)
        {
            _employeeService = employeeService;
            _departmentService = departmentService;
            _designationService = designationService;
        }

        // GET: /Employee
        public ActionResult Index(string search = null, string department = "All", string status = "All")
        {
            var employees = _employeeService.Search(search, department, status);
            ViewBag.Search = search;
            ViewBag.Department = department;
            ViewBag.Status = status;
            return View(employees.Select(MapToVm));
        }

        // GET: /Employee/Details/5
        public ActionResult Details(string id)
        {
            var emp = _employeeService.GetById(id);
            if (emp == null) return HttpNotFound();
            return View(MapToVm(emp));
        }

        // GET: /Employee/Create
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Create()
        {

            var vm = new CreateEmployeeViewModel
            {
                DepartmentOptions = _departmentService.GetAll().Select(d => new SelectListItem
                {
                    Value = d.DepartmentID,
                    Text = d.DepartmentName
                }).ToList(),

                DesignationOptions = _designationService.GetAll().Select(de => new SelectListItem
                {
                    Value = de.DesignationID,
                    Text = de.DesignationName

                }).ToList()
            };

            return View(vm);
        }

        // POST: /Employee/Create
        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Create(CreateEmployeeViewModel vm)
        {
            if (!ModelState.IsValid)
            {

                vm.DepartmentOptions = _departmentService.GetAll().Select(d => new SelectListItem
                {
                    Value = d.DepartmentID,
                    Text = d.DepartmentName
                }).ToList();

                vm.DesignationOptions = _designationService.GetAll().Select(de => new SelectListItem
                {
                    Value = de.DesignationID,
                    Text = de.DesignationName
                }).ToList();


                return View(vm);
            }

            CreateEmployeeDto createEmployeeDto = new CreateEmployeeDto();
            createEmployeeDto.EmployeeID = vm.EmployeeID;
            createEmployeeDto.EmployeeName = vm.EmployeeName;
            createEmployeeDto.Email = vm.Email;
            createEmployeeDto.Phone = vm.Phone;
            createEmployeeDto.DateOfBirth = vm.DateOfBirth;
            createEmployeeDto.JoiningDate = vm.JoiningDate;
            createEmployeeDto.DepartmentID = vm.Department;
            createEmployeeDto.DesignationID = vm.Designation;

            _employeeService.Create(createEmployeeDto);

            TempData["Success"] = "Employee created successfully.";
            return RedirectToAction("Index");
        }

        // GET: /Employee/Edit/5
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Edit(string id)
        {

            var emp = _employeeService.GetById(id);
            if (emp == null) return HttpNotFound();

            var vm = new CreateEmployeeViewModel();

            vm.EmployeeID = emp.EmployeeId;
            vm.EmployeeName = emp.EmployeeName;
            vm.Email = emp.Email;
            vm.Phone = emp.Phone;
            vm.Address = emp.Address;
            vm.DateOfBirth = emp.DateOfBirth;
            vm.JoiningDate = emp.JoiningDate;
            vm.Department = emp.DepartmentID?.Trim();
            vm.Designation = emp.DesignationID?.Trim();
            vm.DepartmentOptions = _departmentService.GetAll().Select(d => new SelectListItem
            {
                Value = d.DepartmentID.Trim(),
                Text = d.DepartmentName
            }).ToList();
            vm.DesignationOptions = _designationService.GetAll().Select(d => new SelectListItem
            {
                Value = d.DesignationID.Trim(),
                Text = d.DesignationName
            }).ToList();

            ViewBag.EmployeeId = emp.EmployeeId;

            return View(vm);
        }

        // POST: /Employee/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Edit(string id, CreateEmployeeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var departments = _departmentService.GetAll();

                vm.DepartmentOptions = departments.Select(d => new SelectListItem
                {
                    Value = d.DepartmentID,
                    Text = d.DepartmentName
                }).ToList();

                return View(vm);
            }

            _employeeService.Update(id, new CreateEmployeeDto
            {
                EmployeeName = vm.EmployeeName,
                Email = vm.Email,
                Phone = vm.Phone,
                DateOfBirth = vm.DateOfBirth,
                JoiningDate = vm.JoiningDate,
                DepartmentID = vm.Department,
                DesignationID = vm.Designation
            });

            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction("Details", new { id });
        }

        // POST: /Employee/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]
        public ActionResult Delete(string id)
        {
            _employeeService.Delete(id);
            TempData["Success"] = "Employee removed.";
            return RedirectToAction("Index");
        }


        // ================= PDF EXPORT =================
        public ActionResult ExportToPdf(string search, string department, string status, bool groupBy = false)
        {
            string departmentName = "All";

            var employees = _employeeService.Search(search, department, status);

            if(department .ToUpper() != "ALL")
                departmentName = _departmentService.GetById(department).DepartmentName;

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);

                writer.PageEvent = new PdfHeaderFooter();
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var groupFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

                // ===== HEADER =====
                doc.Add(new Paragraph("AMS HRMS - Employee Report", titleFont));
                doc.Add(new Paragraph($"Generated on: {DateTime.Now:dd MMM yyyy}"));
                doc.Add(new Paragraph($"Department: {departmentName ?? "All"}"));
                doc.Add(new Paragraph("\n"));

                // ===== NO DATA =====
                if (!employees.Any())
                {
                    doc.Add(new Paragraph("No data available for selected filters."));
                    doc.Close();
                    return File(ms.ToArray(), "application/pdf", "Empty_Report.pdf");
                }

                // ===== GROUPING CONDITION =====
                if (groupBy)
                {
                    var deptGroups = employees
                        .GroupBy(e => e.DepartmentName)
                        .OrderBy(g => g.Key);

                    foreach (var dept in deptGroups)
                    {
                        Paragraph deptHeader = new Paragraph($"Department: {dept.Key}", groupFont);
                        deptHeader.SpacingBefore = 10f;   // space above
                        deptHeader.SpacingAfter = 8f;     // space below (IMPORTANT)
                        doc.Add(deptHeader);

                        AddEmployeeTable(doc, dept, true);
                    }
                }
                else
                {
                    // ✅ NORMAL TABLE (NO GROUPING)
                    AddEmployeeTable(doc, employees);
                }



                doc.Close();

                string fileName = BuildFileName(search, department, status, "PDF");

                return File(ms.ToArray(), "application/pdf", fileName);
            }
        }

        public ActionResult ExportToExcel(string search, string department, string status, bool groupBy = false)
        {
            var employees = _employeeService.Search(search, department, status);

            using (var wb = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Employees");

                int row = 1;

                if (groupBy)
                {
                    var grouped = employees.GroupBy(x => x.DepartmentName);

                    foreach (var dept in grouped)
                    {
                        // Department Header
                        ws.Cell(row, 1).Value = dept.Key;
                        ws.Range(row, 1, row, 6).Merge().Style
                            .Font.SetBold()
                            .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.LightGray);

                        row++;

                        // Table Header
                        ws.Cell(row, 1).Value = "Employee";
                        ws.Cell(row, 2).Value = "Code";
                        ws.Cell(row, 3).Value = "Designation";
                        ws.Cell(row, 4).Value = "Joined";
                        ws.Cell(row, 5).Value = "Status";

                        ws.Range(row, 1, row, 5).Style.Font.SetBold();
                        row++;

                        foreach (var e in dept)
                        {
                            ws.Cell(row, 1).Value = e.EmployeeName;
                            ws.Cell(row, 2).Value = e.EmployeeId;
                            ws.Cell(row, 3).Value = e.DesignationName;
                            ws.Cell(row, 4).Value = e.JoiningDate.ToString("dd MMM yyyy");
                            ws.Cell(row, 5).Value = e.Status;
                            row++;
                        }

                        row++; // space between groups
                    }
                }
                else
                {
                    // Header
                    ws.Cell(1, 1).Value = "Employee";
                    ws.Cell(1, 2).Value = "Code";
                    ws.Cell(1, 3).Value = "Department";
                    ws.Cell(1, 4).Value = "Designation";
                    ws.Cell(1, 5).Value = "Joined";
                    ws.Cell(1, 6).Value = "Status";

                    ws.Range(1, 1, 1, 6).Style.Font.SetBold();

                    int rowData = 2;

                    foreach (var e in employees)
                    {
                        ws.Cell(rowData, 1).Value = e.EmployeeName;
                        ws.Cell(rowData, 2).Value = e.EmployeeId;
                        ws.Cell(rowData, 3).Value = e.DepartmentName;
                        ws.Cell(rowData, 4).Value = e.DesignationName;
                        ws.Cell(rowData, 5).Value = e.JoiningDate.ToString("dd MMM yyyy");
                        ws.Cell(rowData, 6).Value = e.Status;
                        rowData++;
                    }
                }

                ws.Columns().AdjustToContents();

                string fileName = BuildFileName(search, department, status, "EXCEL");

                using (var stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        private void AddEmployeeTable(Document doc, IEnumerable<EmployeeDto> employees, bool groupBy = false)
        {

            PdfPTable table = groupBy ? new PdfPTable(5) : new PdfPTable(6);
            table.WidthPercentage = 100;
            if (groupBy)
                table.SetWidths(new float[] { 2, 5, 4, 4, 3 });
            else
                table.SetWidths(new float[] { 2, 5, 4, 4, 3, 3 });

            AddHeaderCell(table, "Employee ID");
            AddHeaderCell(table, "Employee Name");
            if(!groupBy)
                AddHeaderCell(table, "Department");
            AddHeaderCell(table, "Designation");
            AddHeaderCell(table, "Joining Date");
            AddHeaderCell(table, "Status");

            bool alt = false;

            foreach (var emp in employees)
            {
                var bg = alt ? new BaseColor(245, 247, 255) : BaseColor.WHITE;

                AddCell(table, emp.EmployeeId, bg);
                AddCell(table, emp.EmployeeName, bg);
                if (!groupBy)
                    AddCell(table, emp.DepartmentName, bg);
                AddCell(table, emp.DesignationName, bg);
                AddCell(table, emp.JoiningDate.ToString("dd MMM yyyy"), bg);
                AddCell(table, emp.Status, bg);

                alt = !alt;
            }

            doc.Add(table);
        }

        // ================= HEADER CELL =================
        private void AddHeaderCell(PdfPTable table, string text)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);

            PdfPCell cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = new BaseColor(63, 81, 181), // darker blue
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 10,
                BorderWidth = 1.0f
            };

            table.AddCell(cell);
        }

        // ================= DATA CELL =================
        private void AddCell(PdfPTable table, string text, BaseColor bgColor)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font))
            {
                BackgroundColor = bgColor,
                Padding = 8,
                BorderColor = new BaseColor(220, 220, 220)
            };

            table.AddCell(cell);
        }

        // ================= FILE NAME BUILDER =================
        private string BuildFileName(string search, string department, string status, string exporttype)
        {
            string file = "Employees";

            string fileType = exporttype.ToUpper() == "PDF" ? "pdf" : "xlsx";

            if (!string.IsNullOrWhiteSpace(department) && department != "All")
                file += $"_{department}";

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                file += $"_{status}";

            if (!string.IsNullOrWhiteSpace(search))
                file += "_Search";

            file += $"_{DateTime.Now:yyyyMMdd}.{fileType}";

            return file.Replace(" ", "_");
        }


        private static EmployeeViewModel MapToVm(EmployeeDto e) => new EmployeeViewModel
        {
            EmployeeId = e.EmployeeId,
            EmployeeName = e.EmployeeName,
            Email = e.Email,
            Phone = e.Phone,
            Address = e.Address,
            DateOfBirth = e.DateOfBirth,
            JoiningDate = e.JoiningDate,
            DepartmentID = e.DepartmentID,
            DepartmentName = e.DepartmentName,
            DesignationID = e.DesignationID,
            DesignationName = e.DesignationName,
            Status = e.Status
        };
    }

    public class PdfHeaderFooter : PdfPageEventHelper
    {
        private Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
        private Font subFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.GRAY);

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfPTable header = new PdfPTable(2);
            header.TotalWidth = document.PageSize.Width - 40;

            header.SetWidths(new float[] { 3, 1 });

            // LEFT SIDE (Title)
            header.AddCell(new PdfPCell(new Phrase("AMS HRMS - Employee Report", headerFont))
            {
                Border = 0,
                HorizontalAlignment = Element.ALIGN_LEFT
            });

            // RIGHT SIDE (Date)
            header.AddCell(new PdfPCell(new Phrase(DateTime.Now.ToString("dd MMM yyyy"), subFont))
            {
                Border = 0,
                HorizontalAlignment = Element.ALIGN_RIGHT
            });

            // Position Header (TOP)
            header.WriteSelectedRows(0, -1, 20, document.PageSize.Height - 10, writer.DirectContent);

            // ===== FOOTER =====
            PdfPTable footer = new PdfPTable(2);
            footer.TotalWidth = document.PageSize.Width - 40;

            footer.AddCell(new PdfPCell(new Phrase("AMS HRMS", subFont))
            {
                Border = 0,
                HorizontalAlignment = Element.ALIGN_LEFT
            });

            footer.AddCell(new PdfPCell(new Phrase("Page " + writer.PageNumber, subFont))
            {
                Border = 0,
                HorizontalAlignment = Element.ALIGN_RIGHT
            });

            footer.WriteSelectedRows(0, -1, 20, 30, writer.DirectContent);
        }
    }
}
