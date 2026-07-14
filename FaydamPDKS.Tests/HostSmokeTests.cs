using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class HostSmokeTests
{
    [Fact]
    public async Task Web_login_renders_with_security_and_noindex_headers()
    {
        await using var factory = new WebApplicationFactory<FaydamPdksWebMarker>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/Home/Login");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("noindex, nofollow", html);
        Assert.Contains("<html lang=\"tr\">", html);
    }

    [Fact]
    public async Task Api_live_endpoint_works_and_validation_uses_standard_error_contract()
    {
        await using var factory = new WebApplicationFactory<FaydamPdksApiMarker>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var live = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "invalid", password = "" });
        var body = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 but received {(int)invalid.StatusCode}: {body}");
        Assert.Contains("VALIDATION_ERROR", body);
        Assert.Contains("traceId", body);
        Assert.Equal("no-store", invalid.Headers.CacheControl?.ToString());
    }
}
