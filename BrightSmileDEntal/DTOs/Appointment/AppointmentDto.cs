
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
        /// Patient identifier.
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// Patient name.
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// Dentist identifier.
        /// </summary>
        public int DoctorId { get; set; }

        /// <summary>
        /// Dentist name.
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// Dental service identifier.
        /// </summary>
        public int ServiceId { get; set; }

        /// <summary>
        /// Dental service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Service price in ZAR.
        /// </summary>
        public decimal ServicePrice { get; set; }

        /// <summary>
        /// Appointment date and time.
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Appointment status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional appointment notes.
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }
}
