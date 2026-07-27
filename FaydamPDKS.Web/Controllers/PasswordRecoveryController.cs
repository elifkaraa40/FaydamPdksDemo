using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FaydamPDKS.Web.Controllers;

[AllowAnonymous]
public sealed class PasswordRecoveryController(
    IPasswordResetService passwordResets,
    IWebPasswordResetEmailSender emailSender,
    ILogger<PasswordRecoveryController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new PasswordRecoveryRequestModel
    {
        EmailResetAvailable = emailSender.IsConfigured
    });

    [HttpPost]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> Email(PasswordRecoveryRequestModel model, CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured)
        {
            TempData["Error"] = "E-postayla sıfırlama henüz yapılandırılmadı. Yöneticiden sıfırlama isteyebilirsiniz.";
            return RedirectToAction(nameof(Index));
        }
        if (!ModelState.IsValid)
        {
            model.EmailResetAvailable = true;
            return View(nameof(Index), model);
        }

        var ticket = await passwordResets.CreateEmailResetAsync(model.Email, cancellationToken);
        if (ticket is not null)
        {
            try
            {
                var url = Url.Action(nameof(Reset), "PasswordRecovery", new { token = ticket.RawToken }, Request.Scheme)!;
                await emailSender.SendAsync(ticket.RecipientEmail, ticket.RecipientName, url, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Şifre sıfırlama e-postası gönderilemedi. TraceId: {TraceId}", HttpContext.TraceIdentifier);
            }
        }
        TempData["Success"] = "Hesap bulunursa şifre sıfırlama bağlantısı e-posta adresine gönderilecektir.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> Manager(PasswordRecoveryRequestModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.EmailResetAvailable = emailSender.IsConfigured;
            return View(nameof(Index), model);
        }
        await passwordResets.RequestManagerResetAsync(model.Email, cancellationToken);
        TempData["Success"] = "Hesap bulunursa şifre sıfırlama talebi yöneticilere iletilecektir.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Reset(string? token) =>
        string.IsNullOrWhiteSpace(token) ? RedirectToAction(nameof(Index)) : View(new ResetPasswordModel { Token = token });

    [HttpPost]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> Reset(ResetPasswordModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await passwordResets.ResetWithTokenAsync(model.Token, model.NewPassword, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Bağlantı geçersiz, süresi dolmuş veya yeni parola kullanılamıyor.");
            return View(model);
        }
        TempData["Success"] = "Şifreniz yenilendi. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToAction("Login", "Home");
    }
}
