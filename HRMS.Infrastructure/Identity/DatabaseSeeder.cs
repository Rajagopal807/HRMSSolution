using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace HRMS.Infrastructure.Identity
{
    public class DatabaseSeeder : CreateDatabaseIfNotExists<ApplicationDbContext>
    {

        protected override void Seed(ApplicationDbContext ctx)
        {
            SeedDepartments(ctx);
            ctx.SaveChanges();
            SeedDesignations(ctx);
            ctx.SaveChanges();
            SeedRoles(ctx);
            ctx.SaveChanges();

            List<Employee> employees = SeedEmployees(ctx);
            ctx.SaveChanges();

            SeedUsers(ctx, employees);
            ctx.SaveChanges();
        }

        private void SeedDepartments(ApplicationDbContext ctx)
        {
            if (ctx.Departments.Any()) return;

            var departments = new[]
            {
                new Department { DepartmentId = "01", DepartmentName = "Management", IsActive = true },
                new Department { DepartmentId = "02", DepartmentName = "Development", IsActive = true },
                new Department { DepartmentId = "03", DepartmentName = "Administration", IsActive = true },
                new Department { DepartmentId = "04", DepartmentName = "Business Development", IsActive = true },
                new Department { DepartmentId = "05", DepartmentName = "IT Support", IsActive = true },
                new Department { DepartmentId = "06", DepartmentName = "Quality Team", IsActive = true },
                new Department { DepartmentId = "07", DepartmentName = "BD Support", IsActive = true }
            };

            ctx.Departments.AddRange(departments);
        }

        private void SeedDesignations(ApplicationDbContext ctx)
        {
            if (ctx.Designations.Any()) return;

            var designations = new[]
            {
                new Designation { DesignationID = "01", DesignationName = "STAFF", IsActive = true },
                new Designation { DesignationID = "02", DesignationName = "President", IsActive = true },
                new Designation { DesignationID = "03", DesignationName = "Project Manager", IsActive = true },
                new Designation { DesignationID = "04", DesignationName = "Asst Mgr - Admin", IsActive = true },
                new Designation { DesignationID = "05", DesignationName = "Manager Finance", IsActive = true },
                new Designation { DesignationID = "06", DesignationName = "Asst Mgr - Pre - Sales", IsActive = true },
                new Designation { DesignationID = "07", DesignationName = "Front Office Executive", IsActive = true },
                new Designation { DesignationID = "08", DesignationName = "Team Leader", IsActive = true },
                new Designation { DesignationID = "09", DesignationName = "System Engineer", IsActive = true },
                new Designation { DesignationID = "10", DesignationName = "Product Manager", IsActive = true },
                new Designation { DesignationID = "11", DesignationName = "QC Manager", IsActive = true },
                new Designation { DesignationID = "12", DesignationName = "Sr. QC Engineer", IsActive = true },
                new Designation { DesignationID = "13", DesignationName = "Support Engineer", IsActive = true },
                new Designation { DesignationID = "14", DesignationName = "Project Leader", IsActive = true },
                new Designation { DesignationID = "15", DesignationName = "Vice President - Sales", IsActive = true },
                new Designation { DesignationID = "16", DesignationName = "System Executive", IsActive = true },
                new Designation { DesignationID = "17", DesignationName = "Senior QC Engineer", IsActive = true },
                new Designation { DesignationID = "18", DesignationName = "Software Engineer", IsActive = true },
                new Designation { DesignationID = "19", DesignationName = "Softwr Eng UI/UX Desig", IsActive = true }
            };

            ctx.Designations.AddRange(designations);
        }

        private void SeedRoles(ApplicationDbContext ctx)
        {
            var roleStore = new RoleStore<ApplicationRole>(ctx);
            var roleManager = new RoleManager<ApplicationRole>(roleStore);
            foreach (var role in new[] { "Admin", "SuperAdmin" })
                if (!roleManager.RoleExists(role))
                    roleManager.Create(new ApplicationRole(role));
        }

        private List<Employee> SeedEmployees(ApplicationDbContext ctx)
        {
            if (ctx.Employees.Any())
                return ctx.Employees.ToList();

            var employees = new List<Employee>
            {
                new Employee{
                    EmployeeId = "00000012134",
                    EmployeeName = "DIVYA BHARATHI",
                    Email = "divyab@zenithsoft.com",
                    DepartmentId = "03",
                    DesignationId = "07",
                    DateOfJoining = DateTime.Now,
                    DateOfBirth = DateTime.Now.AddYears(-30),
                    Status = Domain.Enums.EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee{
                    EmployeeId = "00000011255",
                    EmployeeName = "RAJESH SUNDARAMURTHY ARCOT",
                    Email = "rajeshs@zenithsoft.com",
                    DepartmentId = "01",
                    DesignationId = "05",
                    DateOfJoining = DateTime.Now,
                    DateOfBirth = DateTime.Now.AddYears(-30),
                    Status = Domain.Enums.EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }
        };


            ctx.Employees.AddRange(employees);
            return employees;
        }

        private void SeedUsers(ApplicationDbContext ctx, List<Employee> employees)
        {
            var userStore = new UserStore<ApplicationUser>(ctx);
            var userManager = new UserManager<ApplicationUser>(userStore);

            var users = new[]
            {
                new { EmpId="00000011255", Userid="superadmin", Name = "SuperAdmin", Role = "Admin", Password="Superadmin@123!", AppRole = UserRole.SuperAdmin },
                new { EmpId="00000012134", Userid="admin", Name = "Admin", Role = "Admin", Password="Admin@123!",  AppRole = UserRole.Admin }
            };
            foreach (var u in users)
            {
                if (userManager.FindById(u.Userid) == null)
                {
                    var emp = employees.First(e => e.EmployeeId == u.EmpId);

                    var user = new ApplicationUser
                    {
                        UserID = u.Userid.ToUpper(),
                        UserName = u.Name,
                        EmployeeId = emp.EmployeeId,
                        FullName = emp.EmployeeName,
                        Email = emp.Email,
                        Role = u.AppRole,
                        IsActive = true
                    };
                    var result = userManager.Create(user, u.Password);
                    if (result.Succeeded)
                    {
                        userManager.AddToRole(user.Id, u.Role);
                    }
                }
            }
        }
    }
}
