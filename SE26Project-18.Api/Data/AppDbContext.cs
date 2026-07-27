using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Data;

internal sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameTag> GameTags => Set<GameTag>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserTag> UserTags => Set<UserTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("chats");
            entity.HasKey(c => c.Id);
            entity
                .HasOne(c => c.Recruitment)
                .WithMany()
                .HasForeignKey("RecruitmentId")
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(c => c.User1)
                .WithMany(u => u.ChatsAsUser1)
                .HasForeignKey("User1Id")
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(c => c.User2)
                .WithMany(u => u.ChatsAsUser2)
                .HasForeignKey("User2Id")
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey("ChatId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex("User1Id", "User2Id").IsUnique();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(g => g.Id);
            entity.HasMany(g => g.Tags).WithMany();
        });

        modelBuilder.Entity<GameTag>(entity =>
        {
            entity.ToTable("game_tags");
            entity.HasKey(t => t.Id);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(m => m.Id);
            entity
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey("SenderId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Recruitment>(entity =>
        {
            entity.ToTable("recruitments");
            entity.HasKey(r => r.Id);
            entity
                .HasOne(r => r.Game)
                .WithMany()
                .HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(r => r.Recruiter)
                .WithMany(u => u.Recruitments)
                .HasForeignKey("RecruiterId")
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasMany(r => r.Responses)
                .WithOne()
                .HasForeignKey("RecruitmentId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.TokenHashed).IsUnique();
            entity
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Response>(entity =>
        {
            entity.ToTable("responses");
            entity.HasKey(r => r.Id);
            entity
                .HasOne(r => r.Responder)
                .WithMany()
                .HasForeignKey("ResponderId")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex("RecruitmentId", "ResponderId").IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasMany(u => u.Tags).WithMany();
        });

        modelBuilder.Entity<UserTag>(entity =>
        {
            entity.ToTable("user_tags");
            entity.HasKey(t => t.Id);
        });
    }
}
