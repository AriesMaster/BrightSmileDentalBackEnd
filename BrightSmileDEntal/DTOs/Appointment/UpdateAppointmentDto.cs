
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// Defines the information that can be updated
    /// when rescheduling an appointment.
    /// </summary>
    public class UpdateAppointmentDto
    {
        [Required]
        public DateTime AppointmentDate { get; set; }

        [StringLength(1000)]
        public string? AdditionalInfo { get; set; }
    }
}
