using FazaBoa_API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FazaBoa_API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Group> Groups { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<CoinBalance> CoinBalances { get; set; }
        public DbSet<CoinTransaction> CoinTransactions { get; set; }
        public DbSet<RewardTransaction> RewardTransactions { get; set; }
        public DbSet<CompletedChallenge> CompletedChallenges { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relação muitos-para-muitos entre ApplicationUser e Group
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Groups)
                .WithMany(g => g.Members)
                .UsingEntity(j => j.ToTable("UserGroups"));

            // Autoincremental
            builder.Entity<Group>()
                .Property(g => g.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<Challenge>()
                .Property(c => c.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<CoinBalance>()
                .Property(cb => cb.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<CoinTransaction>()
                .Property(ct => ct.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<RewardTransaction>()
                .Property(rt => rt.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<Reward>()
                .Property(r => r.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<CompletedChallenge>()
                .Property(cc => cc.Id)
                .ValueGeneratedOnAdd();

            // Relacionamentos
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.MasterUser)
                .WithMany()
                .HasForeignKey(u => u.MasterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Group>()
                .HasOne(g => g.CreatedBy)
                .WithMany()
                .HasForeignKey(g => g.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Challenge>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Challenge>()
                .HasOne(c => c.Group)
                .WithMany(g => g.Challenges)
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CoinBalance>()
                .HasOne(cb => cb.Group)
                .WithMany()
                .HasForeignKey(cb => cb.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CoinBalance>()
                .HasOne(cb => cb.User)
                .WithMany()
                .HasForeignKey(cb => cb.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CoinTransaction>()
                .HasOne(ct => ct.Group)
                .WithMany()
                .HasForeignKey(ct => ct.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CoinTransaction>()
                .HasOne(ct => ct.User)
                .WithMany()
                .HasForeignKey(ct => ct.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CompletedChallenge>()
                .HasOne(cc => cc.Challenge)
                .WithMany()
                .HasForeignKey(cc => cc.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CompletedChallenge>()
                .HasOne(cc => cc.User)
                .WithMany()
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reward>()
                .HasOne(r => r.Group)
                .WithMany(g => g.Rewards)
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RewardTransaction>()
                .HasOne(rt => rt.Reward)
                .WithMany(r => r.RewardTransactions)
                .HasForeignKey(rt => rt.RewardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RewardTransaction>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
