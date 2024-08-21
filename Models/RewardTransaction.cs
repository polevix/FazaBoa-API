using System;

namespace FazaBoa_API.Models
{
    public class RewardTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;
        public Guid RewardId { get; set; }
        public virtual Reward Reward { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
