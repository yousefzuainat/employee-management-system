namespace YousefZuaianatAPI.Models
{
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
    }
}
