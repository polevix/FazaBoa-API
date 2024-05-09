using System;

namespace FazaBoa_API.Models
{
    public class RewardTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int RewardId { get; set; }
        public virtual Reward Reward { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
