using Microsoft.EntityFrameworkCore;
using Athrna.Data;
using Athrna.Services;
using Athrna.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
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

app.Run();