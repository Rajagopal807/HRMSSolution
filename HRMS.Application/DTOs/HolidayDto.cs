using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs
{
    public class HolidayDto
    {
        public int Id { get; set; }
        public DateTime HolidayDate { get; set; }
        public string HolidayName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveHolidayDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Holiday date is required.")]
        [DataType(DataType.Date)]
        public DateTime HolidayDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Holiday name is required.")]
        [StringLength(100, ErrorMessage = "Max 100 characters.")]
        public string HolidayName { get; set; }

        [StringLength(250, ErrorMessage = "Max 250 characters.")]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class HolidayReportRowDto
    {
        public DateTime HolidayDate { get; set; }
        public string HolidayName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class HolidayReportDto
    {
        public string CompanyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime PrintedOn { get; set; }
        public List<HolidayReportRowDto> Rows { get; set; } = new List<HolidayReportRowDto>();
    }
}
