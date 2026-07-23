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

        modelBuilder.Entity<User>()
            .Ignore(user => user.Chats);

        modelBuilder.Entity<Chat>()
            .HasOne(chat => chat.Recruitment)
            .WithMany()
            .HasForeignKey(chat => chat.RecruitmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Chat>()
            .HasOne(chat => chat.Recruiter)
            .WithMany()
            .HasForeignKey(chat => chat.RecruiterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Chat>()
            .HasOne(chat => chat.Responser)
            .WithMany()
            .HasForeignKey(chat => chat.ResponserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(message => message.Chat)
            .WithMany(chat => chat.Messages)
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(message => message.Sender)
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
