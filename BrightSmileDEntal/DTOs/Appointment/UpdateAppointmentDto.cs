
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
        /// <summary>
    /// Details used to reschedule an appointment.
    /// </summary>
    public class UpdateAppointmentDto
    {
        [Required]
        /// <summary>
        /// New appointment date and time; scheduling rules are validated by the API.
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        [StringLength(1000)]
        /// <summary>
        /// Optional appointment notes (maximum 1,000 characters where validated).
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }
}
