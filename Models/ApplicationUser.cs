using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace FazaBoa_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }= string.Empty;
        public bool IsDependent { get; set; }

        [ForeignKey(nameof(MasterUser))]
        public string? MasterUserId { get; set; }
        public virtual ApplicationUser? MasterUser { get; set; }

        public string? ProfilePhotoUrl { get; set; }
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

        public string? RefreshToken { get; set; }  // Armazena o refresh token
        public DateTime RefreshTokenExpiryTime { get; set; }  // Armazena a data de expiração do refresh token
    }
}
