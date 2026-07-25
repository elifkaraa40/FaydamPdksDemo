using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class MobileProfileServiceTests
{
    [Fact]
    public async Task Updates_account_settings_and_returns_complete_profile()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "MERKEZ", Name = "Merkez", TimeZoneId = "Europe/Istanbul" };
        var department = new Department { Id = Guid.NewGuid(), Code = "BT", Name = "Bilgi Teknolojileri" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Profil Test",
            Email = "profile@faydam.com",
            EmployeeNumber = "PER-700",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Current123!"),
            RoleId = role.Id,
            Role = role,
            WorkplaceId = workplace.Id,
            Workplace = workplace,
            DepartmentId = department.Id,
            Department = department,
            HireDate = new DateOnly(2025, 1, 15),
            ProfileImageUrl = "https://example.com/profile.jpg"
        };
        context.AddRange(role, workplace, department, user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var updated = await service.UpdateAsync(user.Id, new UpdateMobileProfileDto(
            "+90 555 111 22 33", false, true));

        Assert.NotNull(updated);
        Assert.Equal("+905551112233", updated.PhoneNumber);
        Assert.False(updated.IsEmailNotificationEnabled);
        Assert.True(updated.IsSmsNotificationEnabled);
        Assert.Equal("Bilgi Teknolojileri", updated.DepartmentName);
        Assert.Equal("Merkez", updated.WorkplaceName);
        Assert.Equal("https://example.com/profile.jpg", updated.ProfileImageUrl);
    }

    [Fact]
    public async Task Changes_password_only_when_current_password_and_confirmation_are_valid()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Password Test",
            Email = "password@faydam.com",
            EmployeeNumber = "PER-701",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Current123!"),
            RoleId = role.Id,
            Role = role
        };
        context.AddRange(role, user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var changed = await service.ChangePasswordAsync(user.Id,
            new ChangeMobilePasswordDto("Current123!", "NewPassword456!", "NewPassword456!"));

        Assert.True(changed);
        var stored = await context.Users.FindAsync(user.Id);
        Assert.NotNull(stored);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword456!", stored.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("Current123!", stored.PasswordHash));
    }

    [Theory]
    [InlineData("WrongPassword!", "NewPassword456!", "NewPassword456!", "CURRENT_PASSWORD_INVALID")]
    [InlineData("Current123!", "NewPassword456!", "Different456!", "PASSWORD_CONFIRMATION_MISMATCH")]
    [InlineData("Current123!", "Current123!", "Current123!", "PASSWORD_REUSE_NOT_ALLOWED")]
    public async Task Rejects_invalid_password_change(
        string currentPassword,
        string newPassword,
        string confirmation,
        string expectedCode)
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Password Reject",
            Email = "password-reject@faydam.com",
            EmployeeNumber = "PER-702",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Current123!"),
            RoleId = role.Id,
            Role = role
        };
        context.AddRange(role, user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync(user.Id,
                new ChangeMobilePasswordDto(currentPassword, newPassword, confirmation)));

        Assert.Equal(expectedCode, error.Message);
    }

    private static MobileProfileService CreateService(AppDbContext context) =>
        new(new UserRepository(context), new UnitOfWork(context));
}
