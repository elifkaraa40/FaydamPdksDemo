using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class MobileAuthServiceTests
{
    private const string DeviceA = "device-a-installation-id";
    private const string DeviceB = "device-b-installation-id";

    [Fact]
    public async Task Login_and_refresh_rotate_refresh_token()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User
        {
            Id = Guid.NewGuid(), Name = "Test Personel", Email = "test@faydam.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"), RoleId = role.Id, Role = role
        };
        context.AddRange(role, user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var login = await service.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceA, "Test Phone"));
        Assert.NotNull(login);

        var refreshed = await service.RefreshAsync(login.RefreshToken, DeviceA);
        Assert.NotNull(refreshed);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Null(await service.RefreshAsync(login.RefreshToken, DeviceA));
    }

    [Fact]
    public async Task Login_rejects_invalid_password()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        context.AddRange(role, new User
        {
            Id = Guid.NewGuid(), Name = "Test", Email = "test@faydam.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!"), RoleId = role.Id, Role = role
        });
        await context.SaveChangesAsync();

        var response = await CreateService(context).LoginAsync(
            new MobileLoginRequest("test@faydam.com", "WrongPassword!", DeviceA));
        Assert.Null(response);
    }

    [Fact]
    public async Task Login_rejects_inactive_employee()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        context.AddRange(role, new User
        {
            Id = Guid.NewGuid(), Name = "Inactive", Email = "inactive@faydam.com", EmployeeNumber = "PER-0099",
            IsActive = false, PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!"), RoleId = role.Id, Role = role
        });
        await context.SaveChangesAsync();

        var response = await CreateService(context).LoginAsync(
            new MobileLoginRequest("inactive@faydam.com", "CorrectPassword!", DeviceA));
        Assert.Null(response);
    }

    [Fact]
    public async Task Personnel_login_from_second_device_revokes_first_device_session()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test Personel",
            Email = "device-test@faydam.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            RoleId = role.Id,
            Role = role
        };
        context.AddRange(role, user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var first = await service.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceA, "Telefon A"));
        var second = await service.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceB, "Telefon B"));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(second.PreviousDeviceSessionRevoked);
        Assert.Null(await service.RefreshAsync(first.RefreshToken, DeviceA));
        Assert.NotNull(await service.RefreshAsync(second.RefreshToken, DeviceB));
        var firstValidation = await service.ValidateDeviceSessionAsync(
            user.Id, first.DeviceSessionId, DeviceA);
        Assert.False(firstValidation.IsValid);
        Assert.Equal("DEVICE_SESSION_REVOKED", firstValidation.ErrorCode);
    }

    [Fact]
    public async Task Logout_all_revokes_every_device_and_refresh_token()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Yonetici", NormalizedName = "YONETICI" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test Yönetici",
            Email = "manager@faydam.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            RoleId = role.Id,
            Role = role
        };
        context.AddRange(role, user);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var first = await service.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceA, "Telefon A"));
        var second = await service.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceB, "Telefon B"));

        await service.RevokeAllAsync(user.Id);

        Assert.Null(await service.RefreshAsync(first!.RefreshToken, DeviceA));
        Assert.Null(await service.RefreshAsync(second!.RefreshToken, DeviceB));
        Assert.All(context.DeviceSessions, session => Assert.NotNull(session.RevokedAt));
    }

    private static MobileTokenService CreateService(AppDbContext context)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-key-with-more-than-32-characters-long",
            ["Jwt:Issuer"] = "FaydamPDKS.Tests",
            ["Jwt:Audience"] = "FaydamPDKS.Mobile.Tests",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "30"
        };
        return new MobileTokenService(
            new UserRepository(context), new RefreshTokenRepository(context), new UnitOfWork(context),
            context,
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 8, 0, 0, TimeSpan.Zero)));
    }
}
