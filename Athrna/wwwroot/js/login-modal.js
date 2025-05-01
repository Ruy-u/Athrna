[HttpPost]
[ValidateAntiForgeryToken]
public async Task < IActionResult > LoginAjax(LoginViewModel model)
{
    if (!ModelState.IsValid) {
        return Json(new { success = false, message = "Invalid form submission" });
    }

    await Task.Delay(200); // Prevent timing attacks

    var client = await _context.Client.FirstOrDefaultAsync(c => c.Username == model.Username);
    if (client == null || client.EncryptedPassword != model.Password) {
        return Json(new { success = false, message = "Invalid username or password" });
    }

    if (client.IsBanned) {
        _logger.LogWarning("AJAX login attempt for banned account: {Username}", model.Username);
        return Json(new
            {
                success = false,
                message = "Your account has been suspended. Please contact support for assistance.",
                isBanned = true
            });
    }

    if (!client.IsEmailVerified) {
        _logger.LogWarning("AJAX login attempt with unverified email for user: {Username}", model.Username);
        var verificationUrl = Url.Action("ResendVerificationEmail", "Account", new { email = client.Email });
        return Json(new
            {
                success = false,
                message = "Please verify your email address before logging in.",
                requireVerification = true,
                email = client.Email,
                verificationUrl
            });
    }

    // Create claims for the client
    var claims = new List < Claim >
    {
        new Claim(ClaimTypes.Name, client.Username),
        new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
        new Claim(ClaimTypes.Email, client.Email)
    };

    var isAdmin = false;
    var isGuide = false;
    int adminRoleLevel = 0;
    string redirectUrl = "/";

    // Check if client is a guide
    var guide = await _context.Guide.FirstOrDefaultAsync(g => g.Email == client.Email);
    if (guide != null) {
        isGuide = true;
        claims.Add(new Claim(ClaimTypes.Role, "Guide"));
    }

    // Check if client is an administrator
    var admin = await _context.Administrator.FirstOrDefaultAsync(a => a.ClientId == client.Id);
    if (admin != null) {
        isAdmin = true;
        adminRoleLevel = admin.RoleLevel;
        claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        claims.Add(new Claim("AdminRoleLevel", admin.RoleLevel.ToString()));

        _logger.LogInformation("Admin user {Username} logged in with role level {RoleLevel}", client.Username, admin.RoleLevel);
    }

    // Determine redirect URL
    if (isAdmin)
        redirectUrl = "/Admin";
    else if (isGuide)
        redirectUrl = "/GuideDashboard";

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
            AllowRefresh = model.RememberMe
    };

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(claimsIdentity),
        authProperties);

    _logger.LogInformation("User {Username} logged in successfully via AJAX. Remember Me: {RememberMe}", model.Username, model.RememberMe);

    return Json(new
        {
            success = true,
            redirectUrl,
            isAdmin,
            isGuide,
            adminRoleLevel
        });
}
