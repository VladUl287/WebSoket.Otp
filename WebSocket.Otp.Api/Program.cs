using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebSockets.Otp.Api;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Core.Extensions;

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

    builder.Services.AddAuthorization();

    builder.Services.AddWsEndpoints();

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
                var userId = context.Context.User.GetUserId<long>();
                await context.Groups.AddAsync(userId.ToString(), context.ConnectionId);
            };
            opt.OnDisconnected = async (context) =>
            {
                var userId = context.Context.User.GetUserId<long>();
                await context.Groups.RemoveAsync(userId.ToString(), context.ConnectionId);
            };
        });

    app.MapControllers();

    app.Run();
}