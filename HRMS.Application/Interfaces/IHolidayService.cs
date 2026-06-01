using System;
using System.Collections.Generic;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    public interface IHolidayService
    {
        IEnumerable<HolidayDto> GetAll();
        HolidayDto GetById(int id);
        int Create(SaveHolidayDto dto);
        void Update(int id, SaveHolidayDto dto);
        void Delete(int id);
        bool DateExists(DateTime holidayDate, int excludeId = 0);
    }
}
