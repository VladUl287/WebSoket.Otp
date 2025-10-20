namespace WebSockets.Otp.Api.Database.Models;

public sealed class User
{
    public long Id { get; init; }
    public required string Name { get; set; }
    public required string Password { get; set; }
}
