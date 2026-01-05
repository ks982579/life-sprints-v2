using LifeSprint.Infrastructure.Data;
using LifeSprint.Infrastructure.Services;
using LifeSprint.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// HttpClient for external API calls
builder.Services.AddHttpClient();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Authentication services
builder.Services.AddScoped<IAuthService, GitHubAuthService>();

// Activity management services
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<IActivityService, ActivityService>();

// Cookie policy for authentication
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});

// CORS configuration
var corsOrigins = builder.Configuration["CorsOrigins"]?.Split(',') ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Development-specific middleware can go here
}

app.UseCookiePolicy();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
