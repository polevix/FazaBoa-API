namespace FazaBoa_API.Models
{
    public class CoinTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;
        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        public int Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
