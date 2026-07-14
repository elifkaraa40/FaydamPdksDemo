using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class OrganizationServiceTests
{
    [Fact]
    public async Task Creates_workplace_and_scoped_department_and_rejects_duplicate_code()
    {
        await using var context = TestInfrastructure.CreateContext();
        var service = new WebOrganizationAdminService(new OrganizationRepository(context), new UnitOfWork(context));
        await service.CreateWorkplaceAsync(new CreateWorkplaceDto { Code = " ist-01 ", Name = "İstanbul Merkez", TimeZoneId = "Europe/Istanbul" });
        var workplace = Assert.Single(context.Workplaces);
        Assert.Equal("IST-01", workplace.Code);
        await service.CreateDepartmentAsync(new CreateDepartmentDto { WorkplaceId = workplace.Id, Code = " yaz ", Name = "Yazılım" });
        Assert.Equal("YAZ", Assert.Single(context.Departments).Code);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDepartmentAsync(new CreateDepartmentDto { WorkplaceId = workplace.Id, Code = "YAZ", Name = "Başka" }));
    }

    [Fact]
    public async Task Employee_assignment_derives_workplace_from_department()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "ANK", Name = "Ankara", TimeZoneId = "Europe/Istanbul", IsActive = true };
        var department = new Department { Id = Guid.NewGuid(), WorkplaceId = workplace.Id, Workplace = workplace, Code = "OPS", Name = "Operasyon", IsActive = true };
        context.AddRange(role, workplace, department);
        await context.SaveChangesAsync();
        var service = new WebEmployeeAdminService(new UserRepository(context), new RoleRepository(context), new OrganizationRepository(context), new AuditTrail(context, TimeProvider.System), new UnitOfWork(context));

        await service.CreateAsync(new CreateEmployeeDto { EmployeeNumber = "PER-0400", FullName = "Organizasyon Test", Email = "org@faydam.com", DepartmentId = department.Id, RoleId = role.Id, TemporaryPassword = "StrongPassword123!" }, Guid.NewGuid());

        var employee = Assert.Single(context.Users);
        Assert.Equal(workplace.Id, employee.WorkplaceId);
        Assert.Equal(department.Id, employee.DepartmentId);
    }
}
