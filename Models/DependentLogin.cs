namespace FazaBoa_API.Models
{
    public class DependentLogin
    {
        public string MasterUserEmail { get; set; }= string.Empty;
        public string MasterUserPassword { get; set; }= string.Empty;
        public string DependentEmail { get; set; }= string.Empty;
    }
}
