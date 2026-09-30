using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using WebSockets.Otp.Api;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Redis.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddLogging();

    builder.Services.AddControllers();

    builder.Services.AddDbContext<DatabaseContext>(op =>
    {
        op.UseNpgsql("Host=localhost;Port=5432;Database=chatdb;Username=postgres;Password=qwerty");
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("test", policy => policy.RequireClaim("scope", "ws.chat"));
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secretsecretsecretsecretsecretsecretsecretsecretsecret")),
                ValidateIssuer = false,
                ValidateAudience = false,
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.Equals("/ws"))
                        context.Token = accessToken;

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddSignalR();

    builder.Services.AddAuthorization();

    builder.Services.AddWsEndpoints();

    //builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect("localhost:6379"));
    //builder.Services.AddRedisManager();

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

    app.MapEndpoints(
        "/ws",
        (opt) =>
        {
            opt.OnConnected = async (context) =>
            {
                await context.Groups.AddAsync("general-chat", context.ConnectionId);
            };
            opt.OnDisconnected = async (context) =>
            {
                await context.Groups.RemoveAsync("general-chat", context.ConnectionId);
            };
        });

    app.MapControllers();

    app.Run();
}