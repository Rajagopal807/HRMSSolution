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
    public class DesignationService : IDesignationService
    {
        private readonly IUnitOfWork _uow;

        public DesignationService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public string Create(DesignationDto dto)
        {
            var designation = new Designation
            {
                DesignationID = dto.DesignationID,
                DesignationName = dto.DesignationName,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _uow.Designations.Add(designation);
            _uow.SaveChanges();
            return designation.DesignationID;
        }

        public void Delete(string id)
        {
            _uow.Designations.DeleteDesignationID(id);
            _uow.SaveChanges();
        }

        public IEnumerable<DesignationDto> GetActive()
            => _uow.Designations.GetActiveDesignations().Select(Map);

        public IEnumerable<DesignationDto> GetAll()
            => _uow.Designations.GetAll().Select(Map);

        public DesignationDto GetById(string id)
        {
            var d = _uow.Designations.GetByDesignationID(id);
            return d == null ? null : Map(d);
        }

        public IEnumerable<DesignationDto> Search(string query, string designation, string status)
        {
            var all = _uow.Designations.GetAll();
            if (!string.IsNullOrWhiteSpace(query))
                all = all.Where(e => e.DesignationID.Contains(query)
                                  || e.DesignationID.Contains(query));
            if (!string.IsNullOrWhiteSpace(designation) && designation != "All")
                all = all.Where(e => e.DesignationID.ToString() == designation);
            return all.Select(Map);
        }

        public void Update(string id, DesignationDto dto)
        {
            var desig = _uow.Designations.GetByDesignationID(id);
            if (desig == null) throw new KeyNotFoundException($"Designation {id} not found.");
            desig.DesignationID = dto.DesignationID;
            desig.DesignationName = dto.DesignationName;
            desig.IsActive = true;
            desig.UpdatedAt = DateTime.UtcNow;
            _uow.Designations.Update(desig);
            _uow.SaveChanges();
        }

        private static DesignationDto Map(Designation e) => new DesignationDto
        {
            DesignationID = e.DesignationID,
            DesignationName = e.DesignationName,
            CreatedAt = e.CreatedAt
        };
    }
}
