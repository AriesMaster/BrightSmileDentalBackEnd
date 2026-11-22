
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// The status to assign to an existing appointment.
    /// </summary>
    public class UpdateAppointmentStatusDto
    {
        [Required]
        /// <summary>
        /// Requested appointment status.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
