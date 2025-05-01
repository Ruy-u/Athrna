using Microsoft.EntityFrameworkCore;
using Athrna.Data;
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

// Add translation dictionary
builder.Services.AddSingleton<Dictionary<string, Dictionary<string, string>>>(serviceProvider => {
    // Initialize with some basic translations
    var translations = new Dictionary<string, Dictionary<string, string>>();

    // Arabic translations
    translations["ar"] = new Dictionary<string, string>
    {
        { "Home", "الرئيسية" },
        { "Search historical sites...", "البحث عن المواقع التاريخية..." },
        { "Login", "تسجيل الدخول" },
        { "Register", "التسجيل" },
        { "My Dashboard", "لوحة التحكم الخاصة بي" },
        { "My Bookmarks", "إشاراتي المرجعية" },
        { "My Ratings", "تقييماتي" },
        { "My Profile", "ملفي الشخصي" },
        { "Logout", "تسجيل الخروج" },
        { "Password", "كلمة المرور" },
        { "Username", "اسم المستخدم" },
        { "Remember me", "تذكرني" },
        { "Forgot password?", "نسيت كلمة المرور؟" },
        { "Don't have an account?", "ليس لديك حساب؟" },
        { "Register here", "سجل هنا" },
        { "Explore", "استكشف" },
        { "Madinah", "المدينة المنورة" },
        { "Riyadh", "الرياض" },
        { "AlUla", "العلا" }
    };

    // French translations
    translations["fr"] = new Dictionary<string, string>
    {
        { "Home", "Accueil" },
        { "Search historical sites...", "Rechercher des sites historiques..." },
        { "Login", "Connexion" },
        { "Register", "S'inscrire" },
        { "My Dashboard", "Mon tableau de bord" },
        { "My Bookmarks", "Mes favoris" },
        { "My Ratings", "Mes évaluations" },
        { "My Profile", "Mon profil" },
        { "Logout", "Déconnexion" },
        { "Password", "Mot de passe" },
        { "Username", "Nom d'utilisateur" },
        { "Remember me", "Se souvenir de moi" },
        { "Forgot password?", "Mot de passe oublié?" },
        { "Don't have an account?", "Vous n'avez pas de compte?" },
        { "Register here", "Inscrivez-vous ici" },
        { "Explore", "Explorer" },
        { "Madinah", "Médine" },
        { "Riyadh", "Riyad" },
        { "AlUla", "AlUla" }
    };

    // Spanish translations
    translations["es"] = new Dictionary<string, string>
    {
        { "Home", "Inicio" },
        { "Search historical sites...", "Buscar sitios históricos..." },
        { "Login", "Iniciar sesión" },
        { "Register", "Registrarse" },
        { "My Dashboard", "Mi panel" },
        { "My Bookmarks", "Mis marcadores" },
        { "My Ratings", "Mis calificaciones" },
        { "My Profile", "Mi perfil" },
        { "Logout", "Cerrar sesión" },
        { "Password", "Contraseña" },
        { "Username", "Nombre de usuario" },
        { "Remember me", "Recordarme" },
        { "Forgot password?", "¿Olvidó su contraseña?" },
        { "Don't have an account?", "¿No tiene una cuenta?" },
        { "Register here", "Regístrese aquí" },
        { "Explore", "Explorar" },
        { "Madinah", "Medina" },
        { "Riyadh", "Riad" },
        { "AlUla", "AlUla" }
    };

    return translations;
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure email templates exist
await EmailTemplateSetupHelper.EnsureEmailTemplateExists(app.Environment);
await EmailVerificationHelper.EnsureEmailVerificationTemplateExists(app.Environment);

// Setup audio directories and sample files
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await AudioSetupHelper.EnsureAudioSetup(app.Environment, logger);
}

app.Run();