namespace FazaBoa_API.Models
{
    public class Register
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsDependent { get; set; }
        public string? MasterUserId { get; set; }
    }
}
