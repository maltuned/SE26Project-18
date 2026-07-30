using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Data;

internal sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<EmbeddingSyncOutboxMessage> EmbeddingSyncOutbox =>
        Set<EmbeddingSyncOutboxMessage>();

    public DbSet<Game> Games => Set<Game>();

    public DbSet<GameTag> GameTags => Set<GameTag>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Recruitment> Recruitments => Set<Recruitment>();

    public DbSet<RecruitmentTag> RecruitmentTags => Set<RecruitmentTag>();

    public DbSet<RecruitmentView> RecruitmentViews => Set<RecruitmentView>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Response> Responses => Set<Response>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserTag> UserTags => Set<UserTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4", DelegationModes.ApplyToDatabases);
        modelBuilder.UseCollation("utf8mb4_unicode_ci");

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("chats");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Status).HasConversion<byte>();
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
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex("User1Id", "User2Id").IsUnique();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(200);
            entity.Property(g => g.Description).HasMaxLength(4_000);
            entity.HasIndex(g => g.Name).IsUnique();
            entity.HasMany(g => g.Tags).WithMany();
        });

        modelBuilder.Entity<EmbeddingSyncOutboxMessage>(entity =>
        {
            entity.ToTable("embedding_sync_outbox");
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => new { message.PublishedAt, message.LeaseExpiresAt });
            entity.Property(message => message.Target).HasConversion<byte>();
            entity.Property(message => message.CreatedAt).HasPrecision(6);
            entity.Property(message => message.PublishedAt).HasPrecision(6);
            entity.Property(message => message.LeaseExpiresAt).HasPrecision(6);
            entity.Property(message => message.LastError).HasMaxLength(2_000);
        });

        modelBuilder.Entity<GameTag>(entity =>
        {
            entity.ToTable("game_tags");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).HasMaxLength(4_000);
            entity.Property(m => m.SentAt).HasPrecision(6);
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
            entity.Property(r => r.Title).HasMaxLength(200);
            entity.Property(r => r.Description).HasMaxLength(4_000);
            entity.Property(r => r.Status).HasConversion<byte>();
            entity.Property(r => r.ExpiresAt).HasPrecision(6);
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
                .WithOne(response => response.Recruitment)
                .HasForeignKey(response => response.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(r => r.Tags).WithMany();
        });

        modelBuilder.Entity<RecruitmentTag>(entity =>
        {
            entity.ToTable("recruitment_tags");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<RecruitmentView>(entity =>
        {
            entity.ToTable("recruitment_views");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.LastViewedAt).HasPrecision(6);
            entity
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(v => v.Recruitment)
                .WithMany()
                .HasForeignKey(v => v.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(v => new { v.UserId, v.RecruitmentId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity
                .Property(rt => rt.TokenHashed)
                .HasMaxLength(44)
                .IsFixedLength()
                .HasCharSet("ascii")
                .UseCollation("ascii_bin");
            entity.Property(rt => rt.ExpiresAt).HasPrecision(6);
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
            entity.Property(r => r.Type).HasConversion<byte>();
            entity
                .HasOne(r => r.Responder)
                .WithMany()
                .HasForeignKey(r => r.ResponderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.RecruitmentId, r.ResponderId }).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).HasMaxLength(50);
            entity
                .Property(u => u.PasswordHashed)
                .HasMaxLength(60)
                .IsFixedLength()
                .HasCharSet("ascii")
                .UseCollation("ascii_bin");
            entity.Property(u => u.Nickname).HasMaxLength(100);
            entity.Property(u => u.Signature).HasMaxLength(500);
            entity.Property(u => u.Gender).HasConversion<byte>();
            entity.Property(u => u.Status).HasConversion<byte>();
            entity.Property(u => u.Role).HasConversion<byte>();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasMany(u => u.Tags).WithMany();
        });

        modelBuilder.Entity<UserTag>(entity =>
        {
            entity.ToTable("user_tags");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
        });
    }
}
