
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
        public class CreateAppointmentDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid dentist.")]
        /// <summary>
        /// Identifier of the dentist.
        /// </summary>
        /// <summary>
        /// Identifier of the selected dentist.
        /// </summary>
        public int DoctorId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid dental service.")]
        /// <summary>
        /// Identifier of the dental service.
        /// </summary>
        /// <summary>
        /// Identifier of the selected dental service.
        /// </summary>
        public int ServiceId { get; set; }

        [Required]
        /// <summary>
        /// Requested appointment date and time; scheduling rules are validated by the API.
        /// </summary>
        /// <summary>
        /// Requested appointment date and time, subject to scheduling rules.
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        [StringLength(1000)]
        /// <summary>
        /// Optional appointment notes (maximum 1,000 characters where validated).
        /// </summary>
        /// <summary>
        /// Optional appointment notes.
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }
}    /// <summary>
    /// Details required to request a dental appointment.
    /// </summary>

