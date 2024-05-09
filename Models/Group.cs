using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FazaBoa_API.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhotoUrl { get; set; }

        public string CreatedById { get; set; }
        [ForeignKey(nameof(CreatedById))]
        public virtual ApplicationUser CreatedBy { get; set; }

        public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
        public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();
    }
}
