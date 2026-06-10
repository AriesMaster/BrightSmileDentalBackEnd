namespace BrightSmileDEntal.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int ServiceId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Status { get; set; } = "Pending";
        public string? AdditionalInfo { get; set; }

        public User Patient { get; set; } = null!;
        public User Doctor { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}
