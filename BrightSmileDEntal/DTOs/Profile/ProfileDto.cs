
namespace BrightSmileDEntal.DTOs.Profile
{
    /// <summary>
    /// Profile details returned to an authenticated user.
    /// </summary>
    public class ProfileDto
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>

        /// Identifier of the associated user account.

        /// </summary>

        public int UserId { get; set; }

        /// <summary>

        /// Given name.

        /// </summary>

        public string FirstName { get; set; } = string.Empty;

        /// <summary>

        /// Family name.

        /// </summary>

        public string LastName { get; set; } = string.Empty;

        /// <summary>

        /// Contact phone number.

        /// </summary>

        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>

        /// Date of birth, if provided.

        /// </summary>

        public DateTime? DateOfBirth { get; set; }

        /// <summary>

        /// Gender, if provided.

        /// </summary>

        public string Gender { get; set; } = string.Empty;
    }
}
