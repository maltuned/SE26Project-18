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
}
