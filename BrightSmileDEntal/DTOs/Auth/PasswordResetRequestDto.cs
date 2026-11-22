namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// The email address used to request a password reset.
    /// </summary>
    public class PasswordResetRequestDto
    {
        /// <summary>
        /// Account email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
