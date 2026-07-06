namespace BrightSmileDEntal.DTOs.Auth
{
    // DTO used when a user requests a password reset (provides email only)
    public class PasswordResetRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
