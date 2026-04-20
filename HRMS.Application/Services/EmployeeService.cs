using System;
using System.Collections.Generic;
using System.Linq;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _uow;

        public EmployeeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IEnumerable<EmployeeDto> GetActive()
        {
            return _uow.Employees.GetActiveEmployees()
                .Select(e => new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    Email = e.Email,
                    Phone = e.Phone,
                    JoiningDate = e.DateOfJoining,
                    DateOfBirth = e.DateOfBirth,
                    DepartmentID = e.DepartmentId,
                    DepartmentName = e.Department.DepartmentName,
                    DesignationID = e.DesignationId,
                    DesignationName = e.Designation.DesignationName,
                    Weekoff1 = e.Weekoff1,
                    Weekoff2 = e.Weekoff2,  
                    DateofLeft = e.DateofLeft,  
                    Status = e.Status.ToString(),
                    CreatedAt = e.CreatedAt
                })
                .ToList();
        }

        public IEnumerable<EmployeeDto> GetByDepartment(string departmentId)
        {
            return _uow.Employees.GetAll()
                .Where(e => e.DepartmentId == departmentId)
                .Select(e => new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    Email = e.Email,
                    Phone = e.Phone,
                    JoiningDate = e.DateOfJoining,
                    DepartmentID = e.DepartmentId,
                    DepartmentName = e.Department.DepartmentName, 
                    DesignationID = e.DesignationId,
                    DesignationName = e.Designation.DesignationName,
                    Status = e.Status.ToString(),
                    CreatedAt = e.CreatedAt
                })
                .ToList();
        }

        public string Create(CreateEmployeeDto dto)
        {
            //int count = _uow.Employees.Count() + 1;
            var emp = new Employee
            {
                EmployeeId = dto.EmployeeID,
                EmployeeName = dto.EmployeeName,
                Email = dto.Email,
                Phone = dto.Phone,
                DateOfBirth = dto.DateOfBirth,
                DateOfJoining = dto.JoiningDate,
                DepartmentId = dto.DepartmentID,
                DesignationId = dto.DesignationID,
                Status = EmployeeStatus.Active,
                Weekoff1 = dto.Weekoff1,
                Weekoff2= dto.Weekoff2,
                CreatedAt = DateTime.UtcNow
            };
            _uow.Employees.Add(emp);
            _uow.SaveChanges();
            return emp.EmployeeId;
        }

        public void Update(string id, CreateEmployeeDto dto)
        {
            var emp = _uow.Employees.GetByEmployeeID(id);
            if (emp == null) throw new KeyNotFoundException($"Employee {id} not found.");
            emp.EmployeeName = dto.EmployeeName;
            emp.Email = dto.Email;
            emp.Phone = dto.Phone;
            emp.DateOfBirth = dto.DateOfBirth;
            emp.DepartmentId = dto.DepartmentID;
            emp.DesignationId = dto.DesignationID;
            emp.Weekoff1 = dto.Weekoff1;
            emp.Weekoff2 = dto.Weekoff2;
            if(dto.IsInactive == true && emp.IsActive == true)
            {
                emp.Status = EmployeeStatus.Inactive;
                emp.IsActive = false;
                emp.DateofLeft = DateTime.UtcNow;
            }
            else if (dto.IsInactive == false && emp.Status == EmployeeStatus.Inactive)
            {
                emp.Status = EmployeeStatus.Active;
                emp.IsActive = true;
                emp.DateofLeft = null;
            }
            emp.UpdatedAt = DateTime.UtcNow;
            _uow.Employees.Update(emp);
            _uow.SaveChanges();
        }

        public void Delete(string id)
        {
            _uow.Employees.DeleteEmployeeID(id);
            _uow.SaveChanges();
        }

        public IEnumerable<EmployeeDto> Search(string query, string department, string status)
        {
            var data = _uow.Employees.GetAll();

            if (!string.IsNullOrWhiteSpace(query))
            {
                data = data.Where(e => e.EmployeeName.Contains(query)
                                    || e.Email.Contains(query)
                                    || e.EmployeeId.Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(department) && department != "All")
            {
                data = data.Where(e => e.DepartmentId == department);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                data = data.Where(e => e.Status.ToString() == status);
            }

            return data.Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.EmployeeName,
                Email = e.Email,
                Phone = e.Phone,
                JoiningDate = e.DateOfJoining,
                DepartmentID = e.DepartmentId,
                DepartmentName = e.Department.DepartmentName,
                DesignationID = e.DesignationId,
                DesignationName = e.Designation.DesignationName,
                Status = e.Status.ToString(),
                CreatedAt = e.CreatedAt
            }).ToList();
        }

        public EmployeeDto GetById(string id)
        {
            var emp = _uow.Employees.GetAll().Where(e => e.EmployeeId == id).FirstOrDefault();
            EmployeeDto employeeDto = new EmployeeDto();
            employeeDto.EmployeeId = emp.EmployeeId;
            employeeDto.EmployeeName = emp.EmployeeName;
            employeeDto.Email = emp.Email;
            employeeDto.Phone = emp.Phone;
            employeeDto.JoiningDate = emp.DateOfJoining;
            employeeDto.DateOfBirth = emp.DateOfBirth;
            employeeDto.DepartmentID = emp.DepartmentId;
            employeeDto.DepartmentName = emp.Department.DepartmentName;
            employeeDto.DesignationID = emp.DesignationId;
            employeeDto.DesignationName = emp.Designation.DesignationName;
            employeeDto.Weekoff1 = emp.Weekoff1;
            employeeDto.Weekoff1Name = GetWeekoffName(emp.Weekoff1);
            employeeDto.Weekoff2 = emp.Weekoff2;
            employeeDto.Weekoff2Name = GetWeekoffName(emp.Weekoff2);
            employeeDto.Status = emp.Status.ToString();
            employeeDto.CreatedAt = emp.CreatedAt;
            employeeDto.IsInactive = !emp.IsActive;
            employeeDto.DateofLeft = emp.DateofLeft;
            return employeeDto;
        }

        public IEnumerable<EmployeeDto> GetAll()
        {
            return _uow.Employees.GetAll()
                    .Select(e => new EmployeeDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeName = e.EmployeeName,
                        Email = e.Email,
                        Phone = e.Phone,
                        JoiningDate = e.DateOfJoining,
                        DepartmentID = e.DepartmentId,
                        DepartmentName = e.Department.DepartmentName,  
                        DesignationID = e.DesignationId,
                        DesignationName = e.Designation.DesignationName, 
                        Status = e.Status.ToString(),
                        CreatedAt = e.CreatedAt
                    })
                    .ToList();
        }

        private string GetWeekoffName(int day)
        {
            switch (day)
            {
                case 1: return "Sunday";
                case 2: return "Monday";
                case 3: return "Tuesday";
                case 4: return "Wednesday";
                case 5: return "Thursday";
                case 6: return "Friday";
                case 7: return "Saturday";
                default: return "Invalid";
            }
        }
    }
}
