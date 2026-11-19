
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// Information required for a patient to book
    /// a dental appointment.
    /// </summary>
    public class CreateAppointmentDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid dentist.")]
        public int DoctorId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid dental service.")]
        public int ServiceId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [StringLength(1000)]
        public string? AdditionalInfo { get; set; }
    }
}
