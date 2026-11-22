namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// Password reset token and replacement password.
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>
        /// Password reset token.
        /// </summary>
        public string Token { get; set; } = string.Empty;
        /// <summary>
        /// Replacement password.
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
