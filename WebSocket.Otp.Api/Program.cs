using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Api;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Api.Services;
using WebSockets.Otp.Api.Services.Contracts;
using WebSockets.Otp.AspNet.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddLogging();

    builder.Services.AddControllers();

    builder.Services.AddDbContext<DatabaseContext>(op =>
    {
        op.UseNpgsql("Host=localhost;Port=5432;Database=chatdb;Username=postgres;Password=qwerty");
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secretsecretsecretsecretsecretsecretsecretsecretsecret")),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<IStorage<long>, InMemoryUserConnectionMapStorage>();

    builder.Services.AddWsEndpoints(Assembly.GetExecutingAssembly());

    builder.Services.AddOpenApi();
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors(opt =>
    {
        opt.AllowAnyOrigin();
        opt.AllowAnyHeader();
        opt.AllowAnyMethod();
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseWebSockets();
    app.UseWsEndpoints((opt) =>
    {
        opt.RequestPath = "/ws";
        opt.Authorization = new()
        {
            RequireAuthorization = true,
        };
        opt.OnConnected = async (connection) =>
        {
            var userId = connection.Context.User.GetUserId<long>();
            var storage = connection.Context.RequestServices.GetRequiredService<IStorage<long>>();
            await storage.Add(userId, connection.Id);
        };
        opt.OnDisconnected = async (connection) =>
        {
            var userId = connection.Context.User.GetUserId<long>();
            var storage = connection.Context.RequestServices.GetRequiredService<IStorage<long>>();
            await storage.Delete(userId, connection.Id);
        };
    });

    app.MapControllers();

    app.Run();
}