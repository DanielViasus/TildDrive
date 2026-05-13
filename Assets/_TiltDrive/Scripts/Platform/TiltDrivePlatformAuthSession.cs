namespace TiltDrive.Platform
{
    public static class TiltDrivePlatformAuthSession
    {
        public static string Token { get; private set; } = string.Empty;
        public static string UserId { get; private set; } = string.Empty;
        public static string Email { get; private set; } = string.Empty;
        public static string FullName { get; private set; } = string.Empty;
        public static string LinkedProfileId { get; private set; } = string.Empty;
        public static string Role { get; private set; } = string.Empty;

        public static bool IsAuthenticated => !string.IsNullOrEmpty(Token);
        public static bool CanAccessSimulator => Role == "instructor" || Role == "admin";

        public static void Set(string token, string userId, string email, string fullName, string linkedProfileId, string role)
        {
            Token = token ?? string.Empty;
            UserId = userId ?? string.Empty;
            Email = email ?? string.Empty;
            FullName = fullName ?? string.Empty;
            LinkedProfileId = linkedProfileId ?? string.Empty;
            Role = role ?? string.Empty;
        }

        public static void Clear()
        {
            Token = string.Empty;
            UserId = string.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            LinkedProfileId = string.Empty;
            Role = string.Empty;
        }
    }
}
