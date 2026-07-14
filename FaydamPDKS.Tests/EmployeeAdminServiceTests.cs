using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class EmployeeAdminServiceTests
{
    [Fact]
    public async Task Create_normalizes_fields_and_rejects_duplicate_email()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.CreateAsync(new CreateEmployeeDto
        {
            EmployeeNumber = " per-0042 ", FullName = "  Elif Test  ", Email = " ELIF@FAYDAM.COM ",
            RoleId = role.Id, TemporaryPassword = "StrongPassword123!"
        }, Guid.NewGuid());

        var created = Assert.Single(context.Users);
        Assert.Equal("PER-0042", created.EmployeeNumber);
        Assert.Equal("elif@faydam.com", created.Email);
        Assert.True(created.IsActive);
        var audit = Assert.Single(context.AuditLogs);
        Assert.Equal("Employee.Created", audit.Action);
        Assert.DoesNotContain("Password", audit.NewValuesJson, StringComparison.OrdinalIgnoreCase);

        var duplicate = new CreateEmployeeDto
        {
            EmployeeNumber = "PER-0043", FullName = "Baska Personel", Email = "elif@faydam.com",
            RoleId = role.Id, TemporaryPassword = "StrongPassword123!"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(duplicate, Guid.NewGuid()));
    }

    [Fact]
    public async Task SetActive_prevents_manager_from_deactivating_self()
    {
        await using var context = TestInfrastructure.CreateContext();
        var actorId = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(context).SetActiveAsync(actorId, false, actorId));
    }

    private static WebEmployeeAdminService CreateService(AppDbContext context) => new(
        new UserRepository(context), new RoleRepository(context), new OrganizationRepository(context), new AuditTrail(context, TimeProvider.System), new UnitOfWork(context));
}
