using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Exceptions;
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
        var overlap = await Assert.ThrowsAsync<WorkLocationOverlapException>(
            () => service.CreateFieldRequestAsync(personnel.Id, remote));
        Assert.Equal(new DateOnly(2026, 7, 23), overlap.ConflictingStartDate);
        Assert.Equal(new DateOnly(2026, 7, 24), overlap.ConflictingEndDate);
        Assert.Equal("WorkLocation", overlap.ConflictingRecordType);
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
        var tooLong = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateFieldRequestAsync(user.Id, new CreateFieldWorkRequestDto
            {
                LocationType = WorkLocationType.Remote,
                StartDate = new(2026, 7, 25),
                EndDate = new(2026, 10, 23),
                Reason = "Doksan günden uzun tarih aralığı"
            }));
        Assert.Equal("Çalışma konumu tarih aralığı en fazla 90 gün olabilir.", tooLong.Message);
    }

    [Fact]
    public async Task Active_leave_returns_its_dates_as_work_location_conflict()
    {
        await using var db = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Role = role,
            RoleId = role.Id,
            Name = "Personel",
            Email = "leave-conflict@test.local",
            EmployeeNumber = "PER-3",
            IsActive = true
        };
        db.AddRange(role, user, new LeaveRequest
        {
            Id = Guid.NewGuid(),
            User = user,
            UserId = user.Id,
            LeaveType = LeaveType.Annual,
            StartDate = new(2026, 7, 27),
            EndDate = new(2026, 7, 29),
            Reason = "Onay bekleyen yıllık izin",
            Status = LeaveRequestStatus.Pending
        });
        await db.SaveChangesAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Features:WorkLocations"] = "true" })
            .Build();
        var service = new WorkLocationService(
            db,
            config,
            clock,
            new ManagerNotificationService(db, clock),
            new NotificationRepository(db));

        var overlap = await Assert.ThrowsAsync<WorkLocationOverlapException>(() =>
            service.CreateFieldRequestAsync(user.Id, new CreateFieldWorkRequestDto
            {
                LocationType = WorkLocationType.Remote,
                StartDate = new(2026, 7, 28),
                EndDate = new(2026, 7, 30),
                Reason = "İzinle çakışan uzaktan çalışma"
            }));

        Assert.Equal(new DateOnly(2026, 7, 27), overlap.ConflictingStartDate);
        Assert.Equal(new DateOnly(2026, 7, 29), overlap.ConflictingEndDate);
        Assert.Equal("Leave", overlap.ConflictingRecordType);
    }
}
