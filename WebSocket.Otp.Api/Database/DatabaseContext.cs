using Microsoft.EntityFrameworkCore;
using WebSockets.Otp.Api.Database.Models;

namespace WebSockets.Otp.Api.Database;

public sealed class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
        //Database.EnsureCreated();
    }

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChatUser> ChatsUsers => Set<ChatUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData([
            new User
            {
                Id = 1,
                Name = "first user",
                Password = "password"
            },
            new User
            {
                Id = 2,
                Name = "second user",
                Password = "password"
            }
        ]);

        var firstChatId = Guid.CreateVersion7();
        var secondChatId = Guid.CreateVersion7();
        modelBuilder.Entity<Chat>().HasData([
            new Chat
            {
                Id = firstChatId,
                Name = "first chat",
            },
            new Chat
            {
                Id = secondChatId,
                Name = "second chat",
            },
        ]);

        modelBuilder.Entity<ChatUser>().HasData([
            new ChatUser
            {
                ChatId = firstChatId,
                UserId = 1,
            },
            new ChatUser
            {
                ChatId = firstChatId,
                UserId = 2,
            },
            new ChatUser
            {
                ChatId = secondChatId,
                UserId = 1,
            }
        ]);
    }
}
