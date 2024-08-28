using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MosqApp1.ApiService.EntityFramework;
using MosqApp1.ApiService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<WorkoutRecordsDbContext>(options =>
    options.UseSqlite("Data Source=workoutRecords.db"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddSingleton(jwtSettings);

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
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<WorkoutRecordsDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
        AdditionalUserClaimsPrincipalFactory>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

#region Endpoints
// Get all workout records
app.MapGet("/workoutrecords", async (WorkoutRecordsDbContext db) =>
    await db.WorkoutRecords.ToListAsync()).RequireAuthorization();

// Get a specific workout record by ID
app.MapGet("/workoutrecords/{id}", async (int id, WorkoutRecordsDbContext db) =>
    await db.WorkoutRecords.FindAsync(id) is WorkoutRecord record
        ? Results.Ok(record)
        : Results.NotFound()).RequireAuthorization();

// Create a new workout record
app.MapPost("/workoutrecords", async (WorkoutRecord record, WorkoutRecordsDbContext db, HttpContext httpContext) =>
{
    record.UserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    record.UserDisplayName = httpContext.User.FindFirstValue("DisplayName");
    db.WorkoutRecords.Add(record);
    await db.SaveChangesAsync();
    return Results.Created($"/workoutrecords/{record.Id}", record);
}).RequireAuthorization();

// Update an existing workout record
app.MapPut("/workoutrecords/{id}", async (int id, WorkoutRecord inputRecord, WorkoutRecordsDbContext db, HttpContext httpContext) =>
{
    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var record = await db.WorkoutRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    if (record is null) return Results.Forbid();

    record.Description = inputRecord.Description;
    record.DateUpdated = DateTime.Now;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// Delete a workout record
app.MapDelete("/workoutrecords/{id}", async (int id, WorkoutRecordsDbContext db, HttpContext httpContext) =>
{
    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var record = await db.WorkoutRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    if (record is null) return Results.Forbid();

    db.WorkoutRecords.Remove(record);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/register", async (RegisterModel model, UserManager<ApplicationUser> userManager) =>
{
    var user = new ApplicationUser { UserName = model.Username, DisplayName = model.DisplayName };
    var result = await userManager.CreateAsync(user, model.Password);

    return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
});

app.MapPost("/login", async (LoginModel model, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, JwtSettings jwtSettings) =>
{
    var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        var user = await userManager.FindByNameAsync(model.Username);

        // Generate the JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            // Add other claims if necessary
        ]),
            Expires = DateTime.UtcNow.AddMonths(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings.Issuer,
            Audience = jwtSettings.Audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Return the token in the response
        return Results.Ok(tokenString);
    }
    return Results.Unauthorized();
});

app.MapGet("/userinfo", (HttpContext httpContext) =>
{
    var userClaims = httpContext.User.Claims;

    var userInfo = new
    {
        Username = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
        DisplayName = userClaims.FirstOrDefault(c => c.Type == "DisplayName")?.Value,
        UserId = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
        Roles = userClaims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList()
    };

    // Return the user info as JSON
    return Results.Ok(userInfo);
}).RequireAuthorization();

app.MapDefaultEndpoints();
#endregion

// Apply database migrations and run the app
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutRecordsDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

public record RegisterModel(string Username, string DisplayName, string Password);
public record LoginModel(string Username, string Password);
public record JwtSettings(string Issuer, string Audience, string SecretKey);
internal class AdditionalUserClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    public async override Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity;

        var claims = new List<Claim>
        {
            new("DisplayName", user.DisplayName)
        };

        identity.AddClaims(claims);
        return principal;
    }
}