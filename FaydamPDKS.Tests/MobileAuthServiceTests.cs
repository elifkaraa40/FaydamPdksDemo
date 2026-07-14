using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class MobileAuthServiceTests
{
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
        var login = await service.LoginAsync(new MobileLoginRequest(user.Email, "StrongPassword123!", "Test Phone"));
        Assert.NotNull(login);

        var refreshed = await service.RefreshAsync(login.RefreshToken);
        Assert.NotNull(refreshed);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Null(await service.RefreshAsync(login.RefreshToken));
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

        var response = await CreateService(context).LoginAsync(new MobileLoginRequest("test@faydam.com", "WrongPassword!"));
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

        var response = await CreateService(context).LoginAsync(new MobileLoginRequest("inactive@faydam.com", "CorrectPassword!"));
        Assert.Null(response);
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
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 8, 0, 0, TimeSpan.Zero)));
    }
}
