namespace BloodDonationSystem.Helpers
{
    public static class SessionHelper
    {
        public const string UserId = "UserId";
        public const string UserName = "UserName";
        public const string UserEmail = "UserEmail";
        public const string UserRole = "UserRole";

        public static void SetUserSession(this ISession session, int userId, string userName, string email, string role)
        {
            session.SetInt32(UserId, userId);
            session.SetString(UserName, userName);
            session.SetString(UserEmail, email);
            session.SetString(UserRole, role);
        }

        public static void ClearUserSession(this ISession session)
        {
            session.Clear();
        }

        public static bool IsLoggedIn(this ISession session)
        {
            return session.GetInt32(UserId).HasValue;
        }

        public static int? GetUserId(this ISession session)
        {
            return session.GetInt32(UserId);
        }

        public static string? GetUserRole(this ISession session)
        {
            return session.GetString(UserRole);
        }
    }
}
