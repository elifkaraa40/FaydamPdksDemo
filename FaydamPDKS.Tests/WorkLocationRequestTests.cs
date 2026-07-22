using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class WorkLocationRequestTests
{
    [Fact]
    public async Task Field_and_remote_requests_validate_overlap_cancel_and_manager_notification()
    {
        await using var db = TestInfrastructure.CreateContext();
        var personnelRole = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var managerRole = new Role { Id = Guid.NewGuid(), Name = "Yonetici", NormalizedName = "YONETICI" };
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "MER", Name = "Merkez" };
        var personnel = new User { Id = Guid.NewGuid(), Role = personnelRole, RoleId = personnelRole.Id, Workplace = workplace, WorkplaceId = workplace.Id,
            Name = "Personel", Email = "personel@test.local", EmployeeNumber = "PER-1", IsActive = true, AccountStatus = AccountStatus.Active };
        var manager = new User { Id = Guid.NewGuid(), Role = managerRole, RoleId = managerRole.Id, Workplace = workplace, WorkplaceId = workplace.Id,
            Name = "Yönetici", Email = "manager@test.local", EmployeeNumber = "YON-1", IsActive = true, AccountStatus = AccountStatus.Active };
        db.AddRange(personnelRole, managerRole, workplace, personnel, manager);
        await db.SaveChangesAsync();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:WorkLocations"] = "true" }).Build();
        var service = new WorkLocationService(db, config, clock, new ManagerNotificationService(db, clock), new NotificationRepository(db));

        var remote = new CreateFieldWorkRequestDto { LocationType = WorkLocationType.Remote, StartDate = new(2026, 7, 23), EndDate = new(2026, 7, 24), Reason = "Evden planlı çalışma" };
        await service.CreateFieldRequestAsync(personnel.Id, remote);
        var request = Assert.Single(await service.GetMyRequestsAsync(personnel.Id));
        Assert.Equal(WorkLocationType.Remote, request.LocationType);
        Assert.Contains(db.Notifications, x => x.UserId == manager.Id && x.RelatedEntityId == request.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFieldRequestAsync(personnel.Id, remote));
        Assert.True(await service.CancelFieldRequestAsync(request.Id, personnel.Id));
        Assert.False(await service.CancelFieldRequestAsync(request.Id, personnel.Id));

        var field = new CreateFieldWorkRequestDto { LocationType = WorkLocationType.Field, StartDate = new(2026, 7, 25), EndDate = new(2026, 7, 25),
            ProjectName = "Kurulum", FieldAddress = "Müşteri tesisi", Reason = "Saha cihaz kurulumu" };
        await service.CreateFieldRequestAsync(personnel.Id, field);
        var fieldRequest = (await service.GetMyRequestsAsync(personnel.Id)).Single(x => x.Status == WorkLocationRequestStatus.Pending);
        Assert.True(await service.ReviewFieldRequestAsync(fieldRequest.Id, manager.Id, true, "Onaylandı"));
        Assert.Contains(db.Notifications, x => x.UserId == personnel.Id && x.Type == NotificationType.FieldWorkRequestApproved && x.RelatedEntityId == fieldRequest.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewFieldRequestAsync(fieldRequest.Id, manager.Id, false, null));
    }

    [Fact]
    public async Task Start_after_end_is_rejected()
    {
        await using var db = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User { Id = Guid.NewGuid(), Role = role, RoleId = role.Id, Name = "Personel", Email = "p@test.local", EmployeeNumber = "PER-2", IsActive = true };
        db.AddRange(role, user); await db.SaveChangesAsync();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:WorkLocations"] = "true" }).Build();
        var service = new WorkLocationService(db, config, clock, new ManagerNotificationService(db, clock), new NotificationRepository(db));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFieldRequestAsync(user.Id, new CreateFieldWorkRequestDto
            { LocationType = WorkLocationType.Remote, StartDate = new(2026, 7, 25), EndDate = new(2026, 7, 24), Reason = "Geçersiz tarih aralığı" }));
    }
}
