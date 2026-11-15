namespace BrightSmileDEntal.Constants
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Dentist = "Dentist";
        public const string Patient = "Patient";

        public static readonly string[] All =
        [
            Admin,
            Dentist,
            Patient
        ];
    }
}