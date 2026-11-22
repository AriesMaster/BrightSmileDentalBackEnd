
using System;
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// Details required to register a patient account.
    /// </summary>
    public class RegisterDto
    {

        /// <summary>
        /// Account email address.
        /// </summary>
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Account password.
        /// </summary>

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(
            8,
            ErrorMessage = "Password must contain at least 8 characters."
        )]
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// Given name.
        /// </summary>

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "First name must contain between 2 and 100 characters."
        )]
        public string FirstName { get; set; } = string.Empty;
        /// <summary>
        /// Family name.
        /// </summary>

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Last name must contain between 2 and 100 characters."
        )]
        public string LastName { get; set; } = string.Empty;
        /// <summary>
        /// Contact phone number.
        /// </summary>

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(
            20,
            ErrorMessage = "Phone number cannot exceed 20 characters."
        )]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Date of birth, if provided.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        /// <summary>
        /// Gender, if provided.
        /// </summary>

        [StringLength(
            30,
            ErrorMessage = "Gender cannot exceed 30 characters."
        )]
        public string Gender { get; set; } = string.Empty;
    }
}
