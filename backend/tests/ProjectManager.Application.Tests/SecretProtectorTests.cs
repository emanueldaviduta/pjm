using Microsoft.AspNetCore.DataProtection;
using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Application.Tests;

public class SecretProtectorTests
{
    [Fact]
    public void Protect_ThenUnprotect_RoundTrips_AndHidesThePlaintext()
    {
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());

        var stored = protector.Protect("app-password-123");

        Assert.NotEqual("app-password-123", stored);
        Assert.DoesNotContain("app-password-123", stored);
        Assert.Equal("app-password-123", protector.Unprotect(stored));
    }
}
