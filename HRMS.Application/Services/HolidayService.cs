using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HRMS.Application.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly IUnitOfWork _uow;

        public HolidayService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IEnumerable<HolidayDto> GetAll()
            => _uow.Holidays.GetAllHolidays().Select(Map);

        public HolidayDto GetById(int id)
        {
            var holiday = _uow.Holidays.GetById(id);
            return holiday == null ? null : Map(holiday);
        }

        public int Create(SaveHolidayDto dto)
        {
            var date = dto.HolidayDate.Date;
            if (_uow.Holidays.DateExists(date))
                throw new InvalidOperationException($"Holiday already exists for {date:dd-MMM-yyyy}.");

            var holiday = new Holiday
            {
                HolidayDate = date,
                HolidayName = dto.HolidayName.Trim(),
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _uow.Holidays.Add(holiday);
            _uow.SaveChanges();
            _uow.Log.Log("Create", "Holiday", $"Created holiday '{holiday.HolidayName}' on {holiday.HolidayDate:dd-MMM-yyyy}.");

            return holiday.Id;
        }

        public void Update(int id, SaveHolidayDto dto)
        {
            var holiday = _uow.Holidays.GetById(id);
            if (holiday == null) throw new KeyNotFoundException($"Holiday {id} not found.");

            var date = dto.HolidayDate.Date;
            if (_uow.Holidays.DateExists(date, id))
                throw new InvalidOperationException($"Holiday already exists for {date:dd-MMM-yyyy}.");

            holiday.HolidayDate = date;
            holiday.HolidayName = dto.HolidayName.Trim();
            holiday.Description = dto.Description;
            holiday.IsActive = dto.IsActive;
            holiday.UpdatedAt = DateTime.UtcNow;

            _uow.Holidays.Update(holiday);
            _uow.SaveChanges();
            _uow.Log.Log("Update", "Holiday", $"Updated holiday '{holiday.HolidayName}' on {holiday.HolidayDate:dd-MMM-yyyy}.");
        }

        public void Delete(int id)
        {
            var holiday = _uow.Holidays.GetById(id);
            _uow.Holidays.Delete(id);
            _uow.SaveChanges();
            _uow.Log.Log("Delete", "Holiday",
                holiday == null
                    ? $"Holiday '{id}' delete requested."
                    : $"Deleted holiday '{holiday.HolidayName}' on {holiday.HolidayDate:dd-MMM-yyyy}.");
        }

        public bool DateExists(DateTime holidayDate, int excludeId = 0)
            => _uow.Holidays.DateExists(holidayDate.Date, excludeId);

        private static HolidayDto Map(Holiday holiday)
            => new HolidayDto
            {
                Id = holiday.Id,
                HolidayDate = holiday.HolidayDate,
                HolidayName = holiday.HolidayName,
                Description = holiday.Description,
                IsActive = holiday.IsActive
            };
    }
}
