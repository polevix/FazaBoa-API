using System;
using System.Collections.Generic;

namespace FazaBoa_API.Models
{
    public class Challenge
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CoinValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsDaily { get; set; }

        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        public string CreatedById { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }

        public virtual ICollection<ApplicationUser> AssignedUsers { get; set; } = new List<ApplicationUser>();
    }
}
