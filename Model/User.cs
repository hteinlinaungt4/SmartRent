namespace SmartRent.Model
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // --- ထပ်ဖြည့်ရမည့် အပိုင်း ---
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public bool IsVerified { get; set; } = false;
        public int TrustScore { get; set; } = 90; // Default score

        // Relationship: User တစ်ယောက်က Post တွေအများကြီး တင်နိုင်သည်
        public List<Property> Properties { get; set; } = new();
        // -------------------------

        // Refresh Token Properties
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}