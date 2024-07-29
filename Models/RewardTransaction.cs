using System;

namespace FazaBoa_API.Models
{
    public class RewardTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;
        public int RewardId { get; set; }
        public virtual Reward Reward { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
