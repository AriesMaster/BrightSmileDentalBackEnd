namespace BrightSmileDEntal.DTOs.Auth
{
    /// <summary>
    /// Credentials submitted to authenticate an account.
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Account email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>

        /// Account password.

        /// </summary>

        public string Password { get; set; } = string.Empty;
    }
}
