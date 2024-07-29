using System.Collections.Generic;

namespace FazaBoa_API.Models
{
    public class Reward
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int RequiredCoins { get; set; }
        public int GroupId { get; set; }
        public virtual Group Group { get; set; } = default!;
        public virtual ICollection<RewardTransaction> RewardTransactions { get; set; } = new List<RewardTransaction>();
    }
}
