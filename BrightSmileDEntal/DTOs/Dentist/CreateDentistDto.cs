
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Dentist
{
    /// <summary>
    /// Details required to create a dentist account.
    /// </summary>
    public class CreateDentistDto
    {
        /// <summary>
        /// Account email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Initial account password.
        /// </summary>
        [Required]
        [MinLength(12)]
        public string Password { get; set; } = string.Empty;

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
        public string? PhoneNumber { get; set; }
    }
}
