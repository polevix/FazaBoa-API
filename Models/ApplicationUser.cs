using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace FazaBoa_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public bool IsDependent { get; set; }
        public string MasterUserId { get; set; }
        public virtual ApplicationUser MasterUser { get; set; }

        public string ProfilePhotoUrl { get; set; }
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}
