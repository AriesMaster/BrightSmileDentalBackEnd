
using System;
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// Details required to register a patient account.
    /// </summary>
    public class RegisterDto
    {

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        /// <summary>
        /// Account email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(
            8,
            ErrorMessage = "Password must contain at least 8 characters."
        )]
        /// <summary>
        /// Account password.
        /// </summary>
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "First name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "First name must contain between 2 and 100 characters."
        )]
        /// <summary>
        /// Given name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Last name must contain between 2 and 100 characters."
        )]
        /// <summary>
        /// Family name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;


        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(
            20,
            ErrorMessage = "Phone number cannot exceed 20 characters."
        )]
        /// <summary>
        /// Contact phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;


        [DataType(DataType.Date)]
        /// <summary>
        /// Date of birth, if provided.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        [StringLength(
            30,
            ErrorMessage = "Gender cannot exceed 30 characters."
        )]
        /// <summary>
        /// Gender, if provided.
        /// </summary>
        public string Gender { get; set; } = string.Empty;
    }
}
