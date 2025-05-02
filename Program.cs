using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestApi.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text.Json;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.Load();

// Fetch environment variables
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
var Key = Environment.GetEnvironmentVariable("JWT_KEY");
var ServerEndpoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
var ClientEndpoint = Environment.GetEnvironmentVariable("CLIENT_ENDPOINT");

string[] allowedOrigins;

builder.Configuration.AddEnvironmentVariables();
// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.IsNullOrEmpty(dbServer) || string.IsNullOrEmpty(dbName) ||
        string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(dbPassword))
    {
        throw new InvalidOperationException("Database connection information is missing from environment variables.");
    }
    string connectionString = $"Server=tcp:{dbServer},1433;" +
                              $"Initial Catalog={dbName};" +
                              "Persist Security Info=False;" +
                              $"User ID={dbUser};" +
                              $"Password={dbPassword};" +
                              "MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    options.UseSqlServer(connectionString);
});

// Register Identity services
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// CORS policy based on environment
if (builder.Environment.IsDevelopment())
{
    allowedOrigins = new string[]
    {
        "https://localhost:5173",
        "https://localhost:4173",
        "https://localhost:5151",
        "https://localhost:7158",
        "http://localhost:7158",
        "http://localhost:5173",
        "http://localhost:5173/"
    };
}
else
{
    allowedOrigins = new string[] { ServerEndpoint ?? "" ,
    ClientEndpoint ?? "",
    };

}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add HTTP client
builder.Services.AddHttpClient();

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.IncludeFields = true;
    });

// Swagger-related services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add JWT Authentication and Google Authentication with Cookies
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.Name = ".AspNetCore.Correlation";
})
.AddGoogle(options =>
{
    options.ClientId = ClientId;
    options.ClientSecret = ClientSecret;
    options.CallbackPath = "/api/Auth/google-callback";
    options.SaveTokens = true;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key))
    };
});

// Redirect HTTP to HTTPS (in production)
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7158; // Ensure this matches your HTTPS port
});

// Add Authorization
builder.Services.AddAuthorization();

// Logging configuration
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Trace);
});

var app = builder.Build();

// Set the URL for production environment
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("https://0.0.0.0:5000/"); // Set URL only in production
}

// Use Swagger UI for testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty;
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Apply CORS policy
app.UseCors("AllowAllOrigins");

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();