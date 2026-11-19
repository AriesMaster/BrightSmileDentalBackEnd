
namespace BrightSmileDEntal.DTOs.Appointment
{
    /// <summary>
    /// Represents appointment information returned
    /// by the API.
    /// </summary>
    public class AppointmentDto
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public int DoctorId { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public decimal ServicePrice { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? AdditionalInfo { get; set; }
    }
}
