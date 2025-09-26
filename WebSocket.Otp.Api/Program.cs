using System.Reflection;
using WebSockets.Otp.Core;
using WebSockets.Otp.AspNet.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddLogging();

    builder.Services.AddWsFramework(Assembly.GetExecutingAssembly());

    builder.Services.AddOpenApi();
}

var app = builder.Build();
{
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.InitializeWs();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseWebSockets();
    app.UseOtpWebSockets((opt) =>
    {
        opt.Path = "/ws";
    });

    app.Run();
}