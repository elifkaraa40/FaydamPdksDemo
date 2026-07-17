using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace FaydamPDKS.Web.Controllers;

public sealed class HomeController(
    IUserRepository users,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IPersonalDataExportService personalDataExport,
    IWebHostEnvironment environment,
    IDashboardQueryService dashboard,
    ILogger<HomeController> logger) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Yonetici"))
            return RedirectToAction("Index", "MyWork");

        return View(await dashboard.GetAsync(cancellationToken));
    }

    [Authorize(Roles = "Yonetici")]
    [HttpGet]
    public async Task<IActionResult> Search(string? q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var matches = await users.SearchAsync(q, cancellationToken: cancellationToken);
        return Json(matches.Select(item => new
        {
            title = item.Name,
            description = $"{item.EmployeeNumber} · {item.Department?.Name ?? item.DepartmentLegacy ?? "Bölüm belirtilmemiş"}",
            category = "Personel",
            url = Url.Action("Edit", "Employees", new { id = item.Id })
        }));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Account(string section = "personal", CancellationToken cancellationToken = default)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId)) return Challenge();

        var user = await users.GetByIdWithRoleAsync(userId, asTracking: false, cancellationToken);
        if (user is null) return NotFound();

        var userNotifications = await notifications.GetForUserAsync(userId, 50, cancellationToken);
        var allowedSections = new[] { "personal", "appearance", "security", "notifications", "privacy" };
        if (!allowedSections.Contains(section, StringComparer.OrdinalIgnoreCase)) section = "personal";

        return View(new AccountViewModel(
            section.ToLowerInvariant(),
            user.Id,
            user.Name,
            user.Email,
            user.EmployeeNumber,
            user.Role?.Name ?? "Personel",
            user.ProfileImageUrl,
            user.PhoneNumber,
            user.Department?.Name ?? user.DepartmentLegacy,
            user.Workplace?.Name,
            user.HireDate,
            user.IsEmailNotificationEnabled,
            user.IsSmsNotificationEnabled,
            Request.Cookies["Faydam.Theme"] is "dark" ? "dark" : "light",
            Request.Cookies["Faydam.Language"] is "en" ? "en" : "tr",
            userNotifications.Select(item => new AccountNotificationItem(item.Title, item.Message, item.CreatedAt, item.ReadAt.HasValue)).ToArray()));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateProfile(UpdateOwnProfileModel model, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ad soyad ve telefon bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Account), new { section = "personal" });
        }

        var user = await users.GetByIdWithRoleAsync(userId, asTracking: true, cancellationToken);
        if (user is null) return NotFound();
        user.Name = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await SignInUserAsync(user, isPersistent: true);
        TempData["Success"] = "Kişisel bilgileriniz güncellendi.";
        return RedirectToAction(nameof(Account), new { section = "personal" });
    }

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UpdateProfilePhoto(IFormFile? photo, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        if (photo is null || photo.Length == 0 || photo.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "En fazla 5 MB boyutunda bir profil fotoğrafı seçin.";
            return RedirectToAction(nameof(Account), new { section = "personal" });
        }

        var extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };
        if (!extensions.TryGetValue(photo.ContentType, out var extension))
        {
            TempData["Error"] = "Yalnızca JPG, PNG veya WebP görselleri yüklenebilir.";
            return RedirectToAction(nameof(Account), new { section = "personal" });
        }

        await using (var validationStream = photo.OpenReadStream())
        {
            var header = new byte[12];
            var bytesRead = await validationStream.ReadAsync(header, cancellationToken);
            if (!IsSupportedImageHeader(header, bytesRead, extension))
            {
                TempData["Error"] = "Seçilen dosya geçerli bir görsel değil.";
                return RedirectToAction(nameof(Account), new { section = "personal" });
            }
        }

        var user = await users.GetByIdWithRoleAsync(userId, asTracking: true, cancellationToken);
        if (user is null) return NotFound();

        var directory = Path.Combine(environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(directory);
        var fileName = $"{userId:N}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await photo.CopyToAsync(stream, cancellationToken);

        user.ProfileImageUrl = $"/uploads/profiles/{fileName}";
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await SignInUserAsync(user, isPersistent: true);
        TempData["Success"] = "Profil fotoğrafınız güncellendi.";
        return RedirectToAction(nameof(Account), new { section = "personal" });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangeOwnPasswordModel model, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var user = await users.GetByIdWithRoleAsync(userId, asTracking: true, cancellationToken);
        if (user is null) return NotFound();

        if (!ModelState.IsValid || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            TempData["Error"] = "Mevcut parola veya yeni parola bilgileri geçersiz.";
            return RedirectToAction(nameof(Account), new { section = "security" });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Parolanız güvenli biçimde değiştirildi.";
        return RedirectToAction(nameof(Account), new { section = "security" });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdatePreferences(UpdateOwnPreferencesModel model, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var user = await users.GetByIdWithRoleAsync(userId, asTracking: true, cancellationToken);
        if (user is null) return NotFound();

        var theme = model.Theme is "dark" ? "dark" : "light";
        var language = model.Language is "en" ? "en" : "tr";
        Response.Cookies.Append("Faydam.Theme", theme, new CookieOptions { IsEssential = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddYears(1) });
        Response.Cookies.Append("Faydam.Language", language, new CookieOptions { IsEssential = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddYears(1) });
        user.IsEmailNotificationEnabled = model.EmailNotifications;
        user.IsSmsNotificationEnabled = model.SmsNotifications;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["Success"] = language == "en" ? "Preferences saved." : "Tercihleriniz kaydedildi.";
        var returnSection = model.ReturnSection == "notifications" ? "notifications" : "appearance";
        return RedirectToAction(nameof(Account), new { section = returnSection });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> MarkNotificationRead(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var notification = await notifications.GetForUserByIdAsync(userId, id, cancellationToken);
        if (notification is null) return NotFound();
        if (!notification.ReadAt.HasValue)
        {
            notification.ReadAt = TimeProvider.System.GetUtcNow();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return IsLocal(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public IActionResult Help() => View();

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ExportMyData(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var export = await personalDataExport.ExportAsync(userId, cancellationToken);
        if (export is null) return NotFound();

        var json = JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions { WriteIndented = true });
        return File(json, "application/json", $"faydam-kisisel-veriler-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        return View(new LoginViewModel { ReturnUrl = IsLocal(returnUrl) ? returnUrl : null });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await users.GetByEmailWithRoleAsync(model.Email.Trim().ToUpperInvariant(), cancellationToken);
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            logger.LogWarning("Başarısız web oturum açma denemesi. TraceId: {TraceId}", HttpContext.TraceIdentifier);
            ModelState.AddModelError(string.Empty, "E-posta veya parola hatalı.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);

        return IsLocal(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private bool TryUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private Task SignInUserAsync(FaydamPDKS.Core.Models.User user, bool isPersistent)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "Personel"),
            new Claim("profile_image", user.ProfileImageUrl ?? string.Empty)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = isPersistent, AllowRefresh = true });
    }

    private static bool IsSupportedImageHeader(byte[] header, int length, string extension) => extension switch
    {
        ".jpg" => length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        ".png" => length >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".webp" => length >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private bool IsLocal(string? url) => !string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url);
}
