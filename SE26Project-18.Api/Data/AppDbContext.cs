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

    public DbSet<Response> Responses => Set<Response>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserTag> UserTags => Set<UserTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Chat -> User (Recruiter / Responser)
        modelBuilder.Entity<Chat>()
            .HasOne(c => c.Recruiter)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Chat>()
            .HasOne(c => c.Responser)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Response -> User (Responder / Recruiter)
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Responder)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Response>()
            .HasOne(r => r.Recruiter)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Response -> Recruitment
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Recruitment)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Response -> Chat（接受后创建，一对一，FK 在 Response）
        modelBuilder.Entity<Response>()
            .HasOne(r => r.Chat)
            .WithOne()
            .HasForeignKey<Response>(r => r.ChatId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
