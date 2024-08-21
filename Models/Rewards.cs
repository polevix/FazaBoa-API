using System.Collections.Generic;

namespace FazaBoa_API.Models
{
    public class Reward
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Description { get; set; } = string.Empty;
        public int RequiredCoins { get; set; }
        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = default!;
        public virtual ICollection<RewardTransaction> RewardTransactions { get; set; } = new List<RewardTransaction>();
    }
}
