using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Application.Services
{
    public class DepartmentService : IDepartmentService
    {

        private readonly IUnitOfWork _uow;

        public DepartmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public string Create(DepartmentDto dto)
        {
            var department = new Department
            {
                DepartmentId = dto.DepartmentID,
                DepartmentName = dto.DepartmentName,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _uow.Departments.Add(department);
            _uow.SaveChanges();
            return department.DepartmentId;
        }

        public void Delete(string id)
        {
            var department = _uow.Departments.GetByDepartmentID(id);
            _uow.Departments.DeleteDepartmentID(id);
            _uow.SaveChanges();
            _uow.Log.Log("Delete", "Department",
                department == null
                    ? $"Department '{id}' delete requested."
                    : $"Deleted department '{department.DepartmentId}' - '{department.DepartmentName}'.");
        }

        public IEnumerable<DepartmentDto> GetActive()
            => _uow.Departments.GetActiveDepartments().Select(Map);

        public IEnumerable<DepartmentDto> GetAll() 
            => _uow.Departments.GetAll().Select(Map);

        public DepartmentDto GetById(string id)
        {
            var d = _uow.Departments.GetByDepartmentID(id);
            return d == null ? null : Map(d);
        }

        public IEnumerable<DepartmentDto> Search(string query, string department, string status)
        {
            var all = _uow.Departments.GetAll();
            if (!string.IsNullOrWhiteSpace(query))
                all = all.Where(e => e.DepartmentId.Contains(query)
                                  || e.DepartmentId.Contains(query));
            if (!string.IsNullOrWhiteSpace(department) && department != "All")
                all = all.Where(e => e.DepartmentId.ToString() == department);
            return all.Select(Map);
        }

        public void Update(string id, DepartmentDto dto)
        {
            var dept = _uow.Departments.GetByDepartmentID(id);
            if (dept == null) throw new KeyNotFoundException($"Department {id} not found.");
            dept.DepartmentId = dto.DepartmentID;
            dept.DepartmentName = dto.DepartmentName;
            dept.IsActive = true;
            dept.UpdatedAt = DateTime.UtcNow;
            _uow.Departments.Update(dept);
            _uow.SaveChanges();
        }

        private static DepartmentDto Map(Department e) => new DepartmentDto
        {
            DepartmentID = e.DepartmentId,
            DepartmentName = e.DepartmentName,
            CreatedAt = e.CreatedAt
        };
    }
}
