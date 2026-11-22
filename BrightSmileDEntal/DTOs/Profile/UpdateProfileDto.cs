
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Profile
{
    /// <summary>
    /// Editable personal profile details.
    /// </summary>
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        /// <summary>
        /// Given name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        /// <summary>
        /// Family name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        /// <summary>
        /// Contact phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>

        /// Date of birth, if provided.

        /// </summary>

        public DateTime? DateOfBirth { get; set; }

        [StringLength(30)]
        /// <summary>
        /// Gender, if provided.
        /// </summary>
        public string Gender { get; set; } = string.Empty;
    }
}
