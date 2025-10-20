using System.Reflection;
using WebSockets.Otp.AspNet.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddLogging();

    builder.Services.AddWsEndpoints(Assembly.GetExecutingAssembly());

    builder.Services.AddOpenApi();
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseWebSockets();
    app.UseWsEndpoints((opt) =>
    {
        opt.RequestPath = "/ws";
        opt.Authorization = new()
        {
            RequireAuthorization = true,
        };
        opt.OnConnected = (connection) =>
        {
            Console.WriteLine($"Connection created {connection.Id}");
            return Task.CompletedTask;
        };
        opt.OnDisconnected = (connection) =>
        {
            Console.WriteLine($"Connection deleted {connection.Id}");
            return Task.CompletedTask;
        };
    });

    app.Run();
}