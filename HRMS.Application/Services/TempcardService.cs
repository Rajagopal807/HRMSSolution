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
    public class TempcardService : ITempCardService
    {
        private readonly IUnitOfWork _uow;

        public TempcardService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public (string Id, string Error) Create(SaveTempCardDto dto)
        {
            if(_uow.TempCard.TempCardIdExists(dto.TempCardId))
                return ("0", $"Temp Card ID '{dto.TempCardId}' already exists.");

            if(_uow.TempCard.EmployeeAlreadyHasCard(dto.EmployeeId))
                return ("0", "This employee already has an active temp card assigned.");

            var card = new TempCard
            {
                TempCardNo = dto.TempCardId.Trim(),
                EmpId = dto.EmployeeId
            };

            _uow.TempCard.Add(card);
            _uow.SaveChanges();
            return (card.TempCardNo, null);


        }

        public void Delete(string id)
        {
            var card = _uow.TempCard.GetByTempCard(id);
            _uow.TempCard.DeleteByTempcard(id);
            _uow.SaveChanges();
            _uow.Log.Log("Delete", "Temp Card",
                card == null
                    ? $"Temp card '{id}' delete requested."
                    : $"Deleted temp card '{card.TempCardNo}' assigned to employee '{card.EmpId}'.");
        }

        public bool EmployeeAlreadyHasCard(string employeeId, string excludeId = "0")
            => _uow.TempCard.EmployeeAlreadyHasCard(employeeId, excludeId);

        public IEnumerable<TempCardDto> GetAll()
            => _uow.TempCard.GetAllTempCards().Select(Map);

        public TempCardDto GetById(string id)
        {
            var card = _uow.TempCard.GetByTempCard(id);
            return card == null ? null : Map(card);
        }

        public IEnumerable<EmployeeDropdownDto> GetEmployees()
            => _uow.Employees.GetActiveEmployees()
                   .OrderBy(e => e.EmployeeId)
                   .Select(e => new EmployeeDropdownDto
                   {
                       EmployeeId = e.EmployeeId,
                       EmployeeName = e.EmployeeName,
                       DepartmentName = e.Department.DepartmentName.ToString()
                   });

        public bool TempCardIdExists(string tempCardId, string excludeId = "0") 
            => _uow.TempCard.TempCardIdExists(tempCardId, excludeId);

        public string Update(string id, SaveTempCardDto dto)
        {
            var card = _uow.TempCard.GetByTempCard(id);
            if (card == null) return "Temp card not found.";

            if (_uow.TempCard.TempCardIdExists(dto.TempCardId, excludeId: id))
                return $"Temp Card ID '{dto.TempCardId}' is already used by another record.";

            if (_uow.TempCard.EmployeeAlreadyHasCard(dto.EmployeeId, excludeId: id))
                return "This employee already has an active temp card assigned.";

            card.TempCardNo = dto.TempCardId.Trim();
            card.EmpId = dto.EmployeeId;
            _uow.TempCard.Update(card);
            _uow.SaveChanges();
            return null;
        }

        private static TempCardDto Map(TempCard t) => new TempCardDto
        {
            TempCardId = t.TempCardNo,
            EmployeeId = t.EmpId,
            EmployeeName = t.Employee?.EmployeeName ?? t.EmpId,
            Department = t.Employee?.Department.ToString() ?? ""
        };
    }
}
