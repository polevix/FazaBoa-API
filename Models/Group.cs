using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FazaBoa_API.Models
{
    public class Group
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = "/profile-photos/default-group.png";
        public string Description { get; set; } = string.Empty;
        public bool HasUniqueRewards { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        [ForeignKey(nameof(CreatedById))]
        public virtual ApplicationUser CreatedBy { get; set; } = default!;

        public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
        public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();
        public Group()
        {
            PhotoUrl = "/profile-photos/default-group.png";
        }
    }
}
