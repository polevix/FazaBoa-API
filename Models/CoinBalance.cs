namespace FazaBoa_API.Models
{
    public class CoinBalance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;
        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = default!;
        public int Balance { get; set; }
    }
}
