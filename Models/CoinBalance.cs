namespace FazaBoa_API.Models
{
    public class CoinBalance
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;
        public int GroupId { get; set; }
        public virtual Group Group { get; set; } = default!;
        public int Balance { get; set; }
    }
}
