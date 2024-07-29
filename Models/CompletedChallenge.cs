namespace FazaBoa_API.Models
{
    public class CompletedChallenge
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public virtual Challenge Challenge { get; set; } = default!;
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;
        public DateTime CompletedDate { get; set; }
        public bool IsValidated { get; set; }
    }
}
