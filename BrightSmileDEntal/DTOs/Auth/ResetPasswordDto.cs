namespace BrightSmileDEntal.DTOs.Auth
{
    // DTO used when submitting a new password together with the reset token
    public class ResetPasswordDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
