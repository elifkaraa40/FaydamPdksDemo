using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using System.Net;
using System.Text;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class WorkCalendarTests
{
    [Fact]
    public async Task Workplace_rule_overrides_global_holiday_and_weekend_default()
    {
        await using var context = TestInfrastructure.CreateContext();
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "IST", Name = "İstanbul", TimeZoneId = "Europe/Istanbul", IsActive = true };
        var employee = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-1", Name = "Test", Email = "t@f.com", WorkplaceId = workplace.Id, Workplace = workplace, IsActive = true };
        var saturday = new DateOnly(2026, 7, 18);
        context.AddRange(workplace, employee,
            new WorkCalendarDay { Id = Guid.NewGuid(), Date = saturday, Name = "Genel tatil", DayType = CalendarDayType.Holiday },
            new WorkCalendarDay { Id = Guid.NewGuid(), WorkplaceId = workplace.Id, Date = saturday, Name = "Telafi çalışması", DayType = CalendarDayType.WorkingDayOverride });
        await context.SaveChangesAsync();

        var result = await new WorkCalendarResolver(context).ResolveAsync(employee.Id, saturday);
        Assert.True(result.IsWorkingDay);
        Assert.Equal("Telafi çalışması", result.Name);
    }

    [Fact]
    public async Task Half_day_is_a_half_workday_and_ends_at_13()
    {
        await using var context = TestInfrastructure.CreateContext();
        var employee = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-2", Name = "Test", Email = "half@f.com", IsActive = true };
        var date = new DateOnly(2026, 10, 28);
        context.AddRange(employee, new WorkCalendarDay
        {
            Id = Guid.NewGuid(),
            Date = date,
            Name = "Cumhuriyet Bayramı Arifesi",
            DayType = CalendarDayType.Holiday,
            IsHalfDay = true,
            IsSystemGenerated = true
        });
        await context.SaveChangesAsync();

        var result = await new WorkCalendarResolver(context).ResolveAsync(employee.Id, date);

        Assert.True(result.IsWorkingDay);
        Assert.Equal(.5, result.WorkdayWeight);
        Assert.Equal(new TimeOnly(13, 0), result.WorkingUntil);
    }

    [Fact]
    public async Task Admin_can_replace_an_existing_rule_in_the_same_scope()
    {
        await using var context = TestInfrastructure.CreateContext();
        var service = new WebWorkCalendarAdminService(new WorkCalendarRepository(context), new OrganizationRepository(context), new UnitOfWork(context));
        var request = new CreateWorkCalendarDayDto { Date = new DateOnly(2026, 10, 29), Name = "Cumhuriyet Bayramı", DayType = CalendarDayType.Holiday };
        await service.CreateAsync(request);
        request.Name = "Özel çalışma";
        request.DayType = CalendarDayType.WorkingDayOverride;
        await service.CreateAsync(request);

        var saved = Assert.Single(context.WorkCalendarDays);
        Assert.Equal("Özel çalışma", saved.Name);
        Assert.Equal(CalendarDayType.WorkingDayOverride, saved.DayType);
        Assert.False(saved.IsSystemGenerated);
        Assert.Equal("Yönetici", saved.Source);
    }

    [Fact]
    public async Task Diyanet_provider_reads_only_religious_holiday_rows_and_adds_fixed_days()
    {
        const string html = """
            <html><body>
            <table id="icerik_2026">
              <tr><td><a href="kandil.html">Kandil</a><strong>02</strong></td><td><span>Şubat<br>2026</span></td></tr>
              <tr><td><strong>19</strong><br>Perşembe</td><td><a href="ramazan_arefesi.html">Ramazan Bayramı Arefesi</a><br>29 Ramazan</td><td><span>Mart<br>2026</span></td></tr>
              <tr><td><strong>26</strong><br>Salı</td><td><a href="kurban_arefesi.html">Kurban Bayramı Arefesi</a><br>9 Zilhicce</td><td><span>Mayıs<br>2026</span></td></tr>
            </table>
            </body></html>
            """;
        using var httpClient = new HttpClient(new StubHttpHandler(HttpStatusCode.OK, html));
        var result = await new DiyanetPublicHolidayProvider(httpClient).GetTurkeyCalendarAsync(2026);

        Assert.True(result.IsComplete);
        Assert.False(result.UsedFallback);
        Assert.Equal(17, result.Holidays.Count);
        Assert.Contains(result.Holidays, x => x.Date == new DateOnly(2026, 3, 19) && x.IsHalfDay);
        Assert.Contains(result.Holidays, x => x.Date == new DateOnly(2026, 3, 22) && x.Name == "Ramazan Bayramı 3. Gün");
        Assert.Contains(result.Holidays, x => x.Date == new DateOnly(2026, 5, 30) && x.Name == "Kurban Bayramı 4. Gün");
        Assert.DoesNotContain(result.Holidays, x => x.Date == new DateOnly(2026, 2, 2));
    }

    [Fact]
    public async Task Diyanet_provider_uses_verified_json_when_official_source_is_unavailable()
    {
        using var httpClient = new HttpClient(new StubHttpHandler(HttpStatusCode.ServiceUnavailable, ""));
        var result = await new DiyanetPublicHolidayProvider(httpClient).GetTurkeyCalendarAsync(2027);

        Assert.True(result.IsComplete);
        Assert.True(result.UsedFallback);
        Assert.NotNull(result.Warning);
        Assert.Equal(17, result.Holidays.Count);
        Assert.Contains(result.Holidays, x => x.Date == new DateOnly(2027, 3, 8) && x.IsHalfDay);
    }

    [Fact]
    public async Task Diyanet_provider_uses_fallback_instead_of_showing_parser_error()
    {
        const string changedHtml = """
            <table id="icerik_2026">
              <tr><td><a href="ramazan_arefesi.html">Okunamayan satır</a></td></tr>
              <tr><td><a href="kurban_arefesi.html">Okunamayan satır</a></td></tr>
            </table>
            """;
        using var httpClient = new HttpClient(new StubHttpHandler(HttpStatusCode.OK, changedHtml));

        var result = await new DiyanetPublicHolidayProvider(httpClient).GetTurkeyCalendarAsync(2026);

        Assert.True(result.IsComplete);
        Assert.True(result.UsedFallback);
        Assert.NotNull(result.Warning);
        Assert.Equal(17, result.Holidays.Count);
    }

    [Fact]
    public void Half_day_ends_at_break_start_when_lunch_overlaps_13()
    {
        var shift = new ShiftDefinition(
            new TimeOnly(8, 30),
            new TimeOnly(18, 0),
            breakMinutes: 60,
            scheduledBreakStart: new TimeOnly(12, 30),
            scheduledBreakEnd: new TimeOnly(13, 30));

        var halfDayShift = shift.ShortenForHoliday(new TimeOnly(13, 0));

        Assert.Equal(new TimeOnly(12, 30), halfDayShift.EndsAt);
        Assert.Equal(0, halfDayShift.BreakMinutes);
        Assert.Equal(240, (halfDayShift.EndsAt - halfDayShift.StartsAt).TotalMinutes);
    }

    [Fact]
    public async Task Sync_keeps_manual_override_and_removes_only_stale_automatic_rows()
    {
        await using var context = TestInfrastructure.CreateContext();
        var manualDate = new DateOnly(2026, 10, 29);
        var automaticDate = new DateOnly(2026, 5, 19);
        context.WorkCalendarDays.AddRange(
            new WorkCalendarDay
            {
                Id = Guid.NewGuid(),
                Date = manualDate,
                Name = "Şirket açık",
                DayType = CalendarDayType.WorkingDayOverride,
                IsSystemGenerated = false,
                Source = "Yönetici"
            },
            new WorkCalendarDay
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2026, 1, 2),
                Name = "Eski otomatik kayıt",
                DayType = CalendarDayType.Holiday,
                IsSystemGenerated = true
            });
        await context.SaveChangesAsync();
        var provider = new StaticHolidayProvider(new OfficialHolidayCalendar(
            2026,
            [
                new(manualDate, "Cumhuriyet Bayramı", false, "Resmî kaynak"),
                new(automaticDate, "Atatürk'ü Anma, Gençlik ve Spor Bayramı", false, "Resmî kaynak")
            ],
            true,
            false));
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

        var result = await new PublicHolidaySyncService(context, provider, new TestTimeProvider(now)).SyncYearAsync(2026);

        Assert.True(result.IsComplete);
        Assert.Equal(1, result.Added);
        Assert.Contains(context.WorkCalendarDays, x => x.Date == manualDate && x.Name == "Şirket açık" && !x.IsSystemGenerated);
        Assert.Contains(context.WorkCalendarDays, x => x.Date == automaticDate && x.IsSystemGenerated);
        Assert.DoesNotContain(context.WorkCalendarDays, x => x.Date == new DateOnly(2026, 1, 2));
        var state = Assert.Single(context.HolidayCalendarSyncStates);
        Assert.Equal(now, state.LastSuccessfulAt);
    }

    private sealed class StubHttpHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/html")
            });
    }

    private sealed class StaticHolidayProvider(OfficialHolidayCalendar calendar) : IPublicHolidayProvider
    {
        public Task<OfficialHolidayCalendar> GetTurkeyCalendarAsync(int year, CancellationToken cancellationToken = default) =>
            Task.FromResult(calendar);
    }
}
