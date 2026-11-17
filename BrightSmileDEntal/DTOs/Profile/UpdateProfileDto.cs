
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Profile
{
    /// <summary>
    /// Defines the personal information an authenticated user can update.
    /// </summary>
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [StringLength(30)]
        public string Gender { get; set; } = string.Empty;
    }
}
