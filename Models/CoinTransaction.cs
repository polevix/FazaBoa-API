namespace FazaBoa_API.Models
{
    public class CoinTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }
        public string Description { get; set; }
        public int Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
