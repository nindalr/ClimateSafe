using System;

namespace ClimateSafe
{
    internal static class Session
    {
        public static UserAccount CurrentUser;

        public static string DisplayName =>
            CurrentUser == null ? "Tamu" : CurrentUser.Name;

        public static string RoleText =>
            CurrentUser == null ? "Tamu" : CurrentUser.Role.ToString();

        public static string FirstName
        {
            get
            {
                string[] p = DisplayName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return p.Length > 0 ? p[0] : DisplayName;
            }
        }

        public static string Initials
        {
            get
            {
                string[] p = DisplayName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0) return "?";
                if (p.Length == 1) return p[0].Substring(0, 1).ToUpper();
                return (p[0].Substring(0, 1) + p[1].Substring(0, 1)).ToUpper();
            }
        }
    }
}