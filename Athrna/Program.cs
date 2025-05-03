using Microsoft.EntityFrameworkCore;
using Athrna.Data ;
using Athrna.Services;
using Athrna.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication with custom claim transformation
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;

        // Set default expiration (short-lived) when "Remember Me" is not checked
        options.ExpireTimeSpan = TimeSpan.FromHours(2);

        // Set sliding expiration to false by default - cookies will expire after fixed time
        options.SlidingExpiration = false;

        // Add the role level claim to transform from claim to Role
        options.Events.OnValidatePrincipal = async context =>
        {
            // Ensure we have a user identity
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                // Check if the user is an administrator
                if (identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrator"))
                {
                    // Check if they have a role level claim
                    var roleLevelClaim = identity.FindFirst("AdminRoleLevel");
                    if (roleLevelClaim != null && int.TryParse(roleLevelClaim.Value, out int roleLevel))
                    {
                        // Add specific role claims based on the admin role level
                        if (roleLevel <= 3) // Level 3 or higher
                        {
                            identity.AddClaim(new Claim("AdminPermission", "ContentManagement"));
                        }

                        if (roleLevel <= 4) // Level 4 or higher
                        {
                            identity.AddClaim(new Claim("AdminPermission", "UserManagement"));
                        }

                        if (roleLevel == 1) // Only level 1
                        {
                            identity.AddClaim(new Claim("AdminPermission", "AdminManagement"));
                        }
                    }
                }
            }
        };
    });

// Add authorization policies for specific admin permissions
builder.Services.AddAuthorization(options =>
{
    // Policy for content management (level 3 or higher)
    options.AddPolicy("ContentManagement", policy =>
        policy.RequireClaim("AdminPermission", "ContentManagement"));

    // Policy for user management (level 4 or higher)
    options.AddPolicy("UserManagement", policy =>
        policy.RequireClaim("AdminPermission", "UserManagement"));

    // Policy for admin management (only level 1)
    options.AddPolicy("AdminManagement", policy =>
        policy.RequireClaim("AdminPermission", "AdminManagement"));
});

// Register DataProtection for token generation
builder.Services.AddDataProtection();

// Register email services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<PasswordResetTokenService>();

builder.Services.AddScoped<TranslationService>();

var app = builder.Build();

// Ensure database exists with correct schema
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Add custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseMiddleware<TranslationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure email templates exist
await EmailTemplateSetupHelper.EnsureEmailTemplateExists(app.Environment);
await EmailVerificationHelper.EnsureEmailVerificationTemplateExists(app.Environment);

app.Run();