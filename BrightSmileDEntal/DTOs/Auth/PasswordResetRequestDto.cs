namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// Email address used to request a password reset.
    /// </summary>
    public class PasswordResetRequestDto
    {
        /// <summary>
        /// Account email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
