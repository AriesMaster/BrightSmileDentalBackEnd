
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Dentist
{
    /// <summary>
    /// Details required by an administrator to create a dentist account.
    /// </summary>
    public class CreateDentistDto
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        /// <summary>
        /// Account email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(12)]
        /// <summary>
        /// Account password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

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
        public string? PhoneNumber { get; set; }
    }
}
