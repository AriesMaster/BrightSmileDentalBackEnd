
namespace BrightSmileDEntal.DTOs.Profile
{
    /// <summary>
    /// Represents the profile information exposed to authenticated users.
    /// </summary>
    public class ProfileDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;
    }
}
