using System;
using System.Collections.Generic;

namespace FazaBoa_API.Models
{
    public class Challenge
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CoinValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsDaily { get; set; }

        public int GroupId { get; set; }
        public virtual Group Group { get; set; } = default!;

        public string CreatedById { get; set; } = string.Empty;
        public virtual ApplicationUser CreatedBy { get; set; } = default!;

        public virtual ICollection<ApplicationUser> AssignedUsers { get; set; } = new List<ApplicationUser>();
    }
}
