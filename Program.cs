using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SmartRent.Data;
using SmartRent.Interface;
using SmartRent.Service;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// 1️⃣ Services Configuration
// -----------------------------
builder.Services.AddControllers()
  .AddJsonOptions(options =>
  {
      // Prevent circular reference issues in JSON serialization
      options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
  });

builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration with JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your token: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
  {
    {
      new Microsoft.OpenApi.Models.OpenApiSecurityScheme
      {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
          Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
          Id = "Bearer"
        }
      },
      new string[] {}
    }
  });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
        ClockSkew = TimeSpan.Zero
    };
});

// -----------------------------
// 2️⃣ Database Configuration
// -----------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
          maxRetryCount: 5,
          maxRetryDelay: TimeSpan.FromSeconds(5),
          errorCodesToAdd: null);
    });
});

// Scoped Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IImageService, ImageService>();

// -----------------------------
// 3️⃣ Configure Kestrel
// -----------------------------
// Listen on all interfaces (important for VM/public IP)
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

// -----------------------------
// 4️⃣ Database Migration (Docker/VM)
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database Migration Error");
    }
}

// -----------------------------
// 5️⃣ Middleware Pipeline
// -----------------------------

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartRent API V1");
    c.RoutePrefix = "swagger";
});

// Static Files
var env = app.Services.GetRequiredService<IWebHostEnvironment>();

// Ensure wwwroot/uploads exists
var uploadsPath = Path.Combine(env.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

// Serve wwwroot normally
app.UseStaticFiles();

// Explicitly serve /uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Run
app.Run();