
namespace BrightSmileDEntal.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Patient";

        // Token used for password reset flows. Stored as plain string (random token).
        // This field is optional and is set when a password reset is requested.
        public string? ResetToken { get; set; }

        // Expiration time for the reset token. The token should be considered invalid
        // after this timestamp.
        public DateTime? ResetTokenExpiry { get; set; }

        public Profile? Profile { get; set; }
    }
}