
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// Details required to request a dental appointment.
    /// </summary>
    public class CreateAppointmentDto
    {
        /// <summary>
        /// Dentist identifier.
        /// </summary>
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid dentist.")]
        public int DoctorId { get; set; }

        /// <summary>
        /// Dental service identifier.
        /// </summary>
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid dental service.")]
        public int ServiceId { get; set; }

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
}    /// <summary>

