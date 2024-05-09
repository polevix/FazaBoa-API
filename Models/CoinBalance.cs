namespace FazaBoa_API.Models
{
    public class CoinBalance
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }
        public int Balance { get; set; }
    }
}
