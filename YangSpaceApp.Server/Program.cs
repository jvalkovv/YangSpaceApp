using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using YangSpaceApp.Server.Data.Extension;
using YangSpaceApp.Server.Data.SeedData;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext and Identity services and AutoMapper
builder.Services.AddYangSpaceDbContext(builder.Configuration);
builder.Services.AddRegisterIdentity();

// Add Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://yangspace.azurewebsites.net", "https://yangspace.gleeze.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure JWT authentication
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
    };
});

// Register services
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IServiceService, ServicesService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IImageService, ImageService>();

// Initialize Seed Data
builder.Services.AddTransient<Seed>();  // Register Seed service
var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    var scope = app.Services.CreateScope();
    var seedDataLoader = scope.ServiceProvider.GetRequiredService<Seed>();
    await seedDataLoader.SeedCategories(); // Seed the categories
    await seedDataLoader.SeedUsers(); // Seed the users
}

// Apply CORS before routing
app.UseCors("AllowAngularApp");

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

// Use Authentication and Authorization
app.UseAuthentication(); 
app.UseAuthorization();

app.MapFallbackToFile("index.html");

// Map controllers
app.MapControllers();
app.Run();
