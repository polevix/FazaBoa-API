namespace FazaBoa_API.Models
{
    public class DependentLogin
    {
        public string MasterUserEmail { get; set; }
        public string MasterUserPassword { get; set; }
        public string DependentEmail { get; set; }
    }
}
