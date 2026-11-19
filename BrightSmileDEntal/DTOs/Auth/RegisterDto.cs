
using System;
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// Represents the information required to
    /// register a new patient account.
    ///
    /// Public registration always creates a
    /// user with the Patient role.
    /// </summary>
    public class RegisterDto
    {
        // ==========================================
        // ACCOUNT INFORMATION
        // ==========================================

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(
            8,
            ErrorMessage = "Password must contain at least 8 characters."
        )]
        public string Password { get; set; } = string.Empty;

        // ==========================================
        // PERSONAL INFORMATION
        // ==========================================

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "First name must contain between 2 and 100 characters."
        )]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Last name must contain between 2 and 100 characters."
        )]
        public string LastName { get; set; } = string.Empty;

        // ==========================================
        // CONTACT INFORMATION
        // ==========================================

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(
            20,
            ErrorMessage = "Phone number cannot exceed 20 characters."
        )]
        public string PhoneNumber { get; set; } = string.Empty;

        // ==========================================
        // ADDITIONAL PROFILE INFORMATION
        // ==========================================

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(
            30,
            ErrorMessage = "Gender cannot exceed 30 characters."
        )]
        public string Gender { get; set; } = string.Empty;
    }
}
