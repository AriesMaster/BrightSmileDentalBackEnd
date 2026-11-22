
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// Details used to reschedule an appointment.
    /// </summary>
    public class UpdateAppointmentDto
    {
        /// <summary>
        /// Appointment date and time.
        /// </summary>
        [Required]
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Optional appointment notes.
        /// </summary>
        [StringLength(1000)]
        public string? AdditionalInfo { get; set; }
    }
}
