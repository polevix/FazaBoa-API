namespace FazaBoa_API.Models
{
    public class Register
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsDependent { get; set; }
        public string? MasterUserId { get; set; }
    }
}
