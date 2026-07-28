using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameTag> GameTags => Set<GameTag>();
    public DbSet<RecruitmentTag> RecruitmentTags => Set<RecruitmentTag>();
    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Admin> Admins => Set<Admin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();

            entity.HasMany(u => u.Recruitments)
                .WithOne(r => r.Publisher)
                .HasForeignKey(r => r.PublisherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.SentResponses)
                .WithOne(r => r.Responser)
                .HasForeignKey(r => r.ResponserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Chats)
                .WithOne(c => c.Recruiter)
                .HasForeignKey(c => c.RecruiterId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Game <-> GameTag (M2M with custom join table)
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Tags)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "game_game_tags",
                j => j.HasOne<GameTag>().WithMany().HasForeignKey("game_tag_id"),
                j => j.HasOne<Game>().WithMany().HasForeignKey("game_id"));

        // Recruitment
        modelBuilder.Entity<Recruitment>(entity =>
        {
            entity.HasOne(r => r.Publisher)
                .WithMany(u => u.Recruitments)
                .HasForeignKey(r => r.PublisherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Game)
                .WithMany()
                .HasForeignKey(r => r.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.GameTags)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "recruitment_game_tags",
                    j => j.HasOne<GameTag>().WithMany().HasForeignKey("game_tag_id"),
                    j => j.HasOne<Recruitment>().WithMany().HasForeignKey("recruitment_id"));

            entity.HasMany(r => r.RecruitmentTags)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "recruitment_recruitment_tags",
                    j => j.HasOne<RecruitmentTag>().WithMany().HasForeignKey("recruitment_tag_id"),
                    j => j.HasOne<Recruitment>().WithMany().HasForeignKey("recruitment_id"));

            entity.HasMany(r => r.Responses)
                .WithOne(r => r.Recruitment)
                .HasForeignKey(r => r.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Chats)
                .WithOne(c => c.Recruitment)
                .HasForeignKey(c => c.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Response
        modelBuilder.Entity<Response>(entity =>
        {
            entity.HasOne(r => r.Recruitment)
                .WithMany(r => r.Responses)
                .HasForeignKey(r => r.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Responser)
                .WithMany(u => u.SentResponses)
                .HasForeignKey(r => r.ResponserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Chat
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasOne(c => c.Recruitment)
                .WithMany(r => r.Chats)
                .HasForeignKey(c => c.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Recruiter)
                .WithMany(u => u.Chats)
                .HasForeignKey(c => c.RecruiterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Responser)
                .WithMany()
                .HasForeignKey(c => c.ResponserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Chat)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.TokenHashed).IsUnique();

            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Feedback
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Admin
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasIndex(a => a.Username).IsUnique();
        });
    }
}