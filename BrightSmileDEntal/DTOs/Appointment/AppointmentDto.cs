
namespace BrightSmileDEntal.DTOs.Appointment
{
        /// <summary>
    /// Appointment details returned by the API.
    /// </summary>
    public class AppointmentDto
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identifier of the patient.
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// Display name of the patient.
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the dentist.
        /// </summary>
        public int DoctorId { get; set; }

        /// <summary>
        /// Display name of the dentist.
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the dental service.
        /// </summary>
        public int ServiceId { get; set; }

        /// <summary>
        /// Name of the dental service.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Service price in South African rand (ZAR).
        /// </summary>
        public decimal ServicePrice { get; set; }

        /// <summary>
        /// Scheduled appointment date and time.
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Current appointment status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional appointment notes (maximum 1,000 characters where validated).
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }
}
