
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// Status change request for an existing appointment.
    /// </summary>
    public class UpdateAppointmentStatusDto
    {
        /// <summary>
        /// Appointment status.
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
