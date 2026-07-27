using FaydamPDKS.Core.Interfaces;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FaydamPDKS.Data;

public sealed class DiyanetPublicHolidayProvider : IPublicHolidayProvider
{
    public const string SourceUrl = "https://mobil.diyanet.gov.tr/mobile/dinigunler/dinigunler.html";
    private const string OfficialSource = "2429 sayılı Kanun ve T.C. Diyanet İşleri Başkanlığı";
    private const int MaximumResponseBytes = 2 * 1024 * 1024;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly IReadOnlyDictionary<string, int> TurkishMonths =
        new Dictionary<string, int>(StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true))
        {
            ["Ocak"] = 1,
            ["Şubat"] = 2,
            ["Mart"] = 3,
            ["Nisan"] = 4,
            ["Mayıs"] = 5,
            ["Haziran"] = 6,
            ["Temmuz"] = 7,
            ["Ağustos"] = 8,
            ["Eylül"] = 9,
            ["Ekim"] = 10,
            ["Kasım"] = 11,
            ["Aralık"] = 12
        };

    private readonly HttpClient httpClient;
    private readonly ConcurrentDictionary<int, CachedCalendar> cache = new();

    public DiyanetPublicHolidayProvider()
        : this(CreateHttpClient())
    {
    }

    public DiyanetPublicHolidayProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<OfficialHolidayCalendar> GetTurkeyCalendarAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 2200)
            throw new ArgumentOutOfRangeException(nameof(year), "Takvim yılı 2000 ile 2200 arasında olmalıdır.");

        if (cache.TryGetValue(year, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return cached.Calendar;

        OfficialHolidayCalendar calendar;
        try
        {
            var html = await DownloadOfficialCalendarAsync(cancellationToken);
            var religiousHolidays = ParseReligiousHolidays(html, year);
            if (religiousHolidays.Count != 9)
                throw new InvalidDataException($"{year} yılı için beklenen Ramazan ve Kurban Bayramı günleri bulunamadı.");

            calendar = BuildCalendar(year, religiousHolidays, usedFallback: false, warning: null);
        }
        catch (Exception ex) when (
            !cancellationToken.IsCancellationRequested
            && ex is HttpRequestException or TaskCanceledException or IOException or InvalidDataException or RegexMatchTimeoutException)
        {
            var fallback = ReadFallbackReligiousHolidays(year);
            if (fallback.Count == 9)
            {
                calendar = BuildCalendar(
                    year,
                    fallback,
                    usedFallback: true,
                    warning: $"Diyanet takvimine şu anda ulaşılamadı. {year} yılı için doğrulanmış yerel yedek kullanılıyor.");
            }
            else
            {
                calendar = BuildCalendar(
                    year,
                    [],
                    usedFallback: false,
                    warning: $"Diyanet takvimine ulaşılamadı ve {year} yılı için yerel yedek bulunamadı. Dini bayramlar otomatik eklenemedi.");
            }
        }

        var cacheDuration = calendar.UsedFallback || !calendar.IsComplete ? FailureCacheDuration : CacheDuration;
        cache[year] = new CachedCalendar(DateTimeOffset.UtcNow.Add(cacheDuration), calendar);
        return calendar;
    }

    internal static IReadOnlyList<OfficialHoliday> ParseReligiousHolidays(string html, int year)
    {
        var tablePattern = $@"<table\b[^>]*\bid\s*=\s*[""']icerik_{year}[""'][^>]*>(?<body>.*?)</table>";
        var tableMatch = Regex.Match(html, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeout);
        if (!tableMatch.Success)
            throw new InvalidDataException($"{year} yılı Diyanet tablosu bulunamadı.");

        DateOnly? ramadanEve = null;
        DateOnly? sacrificeEve = null;
        foreach (Match rowMatch in Regex.Matches(
                     tableMatch.Groups["body"].Value,
                     @"<tr\b[^>]*>(?<row>.*?)</tr>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline,
                     RegexTimeout))
        {
            var row = rowMatch.Groups["row"].Value;
            if (row.Contains("ramazan_arefesi.html", StringComparison.OrdinalIgnoreCase))
                ramadanEve = ParseRowDate(row, year);
            else if (row.Contains("kurban_arefesi.html", StringComparison.OrdinalIgnoreCase))
                sacrificeEve = ParseRowDate(row, year);
        }

        if (!ramadanEve.HasValue || !sacrificeEve.HasValue)
            throw new InvalidDataException($"{year} yılı dini bayram arife tarihleri bulunamadı.");

        return BuildReligiousHolidays(ramadanEve.Value, sacrificeEve.Value);
    }

    private async Task<string> DownloadOfficialCalendarAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            SourceUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaximumResponseBytes)
            throw new InvalidDataException("Diyanet yanıtı beklenen boyutu aşıyor.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (output.Length + read > MaximumResponseBytes)
                throw new InvalidDataException("Diyanet yanıtı beklenen boyutu aşıyor.");
            output.Write(buffer, 0, read);
        }

        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static DateOnly ParseRowDate(string rowHtml, int year)
    {
        var text = WebUtility.HtmlDecode(Regex.Replace(rowHtml, "<[^>]+>", " ", RegexOptions.None, RegexTimeout));
        text = Regex.Replace(text, @"\s+", " ", RegexOptions.None, RegexTimeout).Trim();
        var monthNames = string.Join("|", TurkishMonths.Keys.Select(Regex.Escape));
        var dayMatch = Regex.Match(
            rowHtml,
            @"<strong\b[^>]*>\s*(?<day>\d{1,2})\s*</strong>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            RegexTimeout);
        var monthMatch = Regex.Match(
            text,
            $@"(?<month>{monthNames})\s+{year}\b",
            RegexOptions.IgnoreCase,
            RegexTimeout);
        if (!dayMatch.Success
            || !monthMatch.Success
            || !int.TryParse(dayMatch.Groups["day"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var day)
            || !TurkishMonths.TryGetValue(monthMatch.Groups["month"].Value, out var month))
            throw new InvalidDataException($"Diyanet tarih satırı okunamadı: {text}");

        return new DateOnly(year, month, day);
    }

    private static IReadOnlyList<OfficialHoliday> ReadFallbackReligiousHolidays(int year)
    {
        var assembly = typeof(DiyanetPublicHolidayProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith("tr-public-holidays-fallback.json", StringComparison.Ordinal));
        if (resourceName is null) return [];

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return [];
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.GetProperty("years").TryGetProperty(year.ToString(CultureInfo.InvariantCulture), out var yearData)
            || !DateOnly.TryParseExact(yearData.GetProperty("ramadanEve").GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ramadanEve)
            || !DateOnly.TryParseExact(yearData.GetProperty("sacrificeEve").GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sacrificeEve))
            return [];

        return BuildReligiousHolidays(ramadanEve, sacrificeEve);
    }

    private static IReadOnlyList<OfficialHoliday> BuildReligiousHolidays(DateOnly ramadanEve, DateOnly sacrificeEve)
    {
        var holidays = new List<OfficialHoliday>
        {
            new(ramadanEve, "Ramazan Bayramı Arifesi", true, OfficialSource)
        };
        for (var day = 1; day <= 3; day++)
            holidays.Add(new(ramadanEve.AddDays(day), $"Ramazan Bayramı {day}. Gün", false, OfficialSource));

        holidays.Add(new(sacrificeEve, "Kurban Bayramı Arifesi", true, OfficialSource));
        for (var day = 1; day <= 4; day++)
            holidays.Add(new(sacrificeEve.AddDays(day), $"Kurban Bayramı {day}. Gün", false, OfficialSource));
        return holidays;
    }

    private static OfficialHolidayCalendar BuildCalendar(
        int year,
        IReadOnlyList<OfficialHoliday> religiousHolidays,
        bool usedFallback,
        string? warning)
    {
        var holidays = BuildFixedHolidays(year)
            .Concat(religiousHolidays)
            .OrderBy(x => x.Date)
            .ToArray();
        return new(year, holidays, religiousHolidays.Count == 9, usedFallback, warning);
    }

    private static IReadOnlyList<OfficialHoliday> BuildFixedHolidays(int year) =>
    [
        new(new DateOnly(year, 1, 1), "Yılbaşı", false, OfficialSource),
        new(new DateOnly(year, 4, 23), "Ulusal Egemenlik ve Çocuk Bayramı", false, OfficialSource),
        new(new DateOnly(year, 5, 1), "Emek ve Dayanışma Günü", false, OfficialSource),
        new(new DateOnly(year, 5, 19), "Atatürk'ü Anma, Gençlik ve Spor Bayramı", false, OfficialSource),
        new(new DateOnly(year, 7, 15), "Demokrasi ve Millî Birlik Günü", false, OfficialSource),
        new(new DateOnly(year, 8, 30), "Zafer Bayramı", false, OfficialSource),
        new(new DateOnly(year, 10, 28), "Cumhuriyet Bayramı Arifesi", true, OfficialSource),
        new(new DateOnly(year, 10, 29), "Cumhuriyet Bayramı", false, OfficialSource)
    ];

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FaydamPDKS/1.0 (+official-holiday-sync)");
        return client;
    }

    private sealed record CachedCalendar(DateTimeOffset ExpiresAt, OfficialHolidayCalendar Calendar);
}
