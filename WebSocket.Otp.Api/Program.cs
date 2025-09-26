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
        opt.Path = "/ws";
    });

    app.Run();
}