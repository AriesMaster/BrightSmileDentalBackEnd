
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Profile
{
    /// <summary>
    /// Editable personal profile details.
    /// </summary>
    public class UpdateProfileDto
    {
        /// <summary>
        /// Given name.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Family name.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Contact phone number.
        /// </summary>
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Date of birth, if provided.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Gender, if provided.
        /// </summary>
        [StringLength(30)]
        public string Gender { get; set; } = string.Empty;
    }
}
