using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Data;

public sealed class AppDbContext : DbContext
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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.TokenHashed).IsUnique();
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasMany(u => u.Tags).WithMany();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasMany(g => g.Tags).WithMany();
        });

        modelBuilder.Entity<Chat>()
            .HasOne(c => c.Recruiter)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Chat>()
            .HasOne(c => c.Responser)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Response>()
            .HasOne(r => r.Responder)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Recruiter)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Recruitment)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Chat)
            .WithOne()
            .HasForeignKey<Response>(r => r.ChatId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
