using Microsoft.AspNetCore.DataProtection;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Infrastructure.Services;

/// <summary>ASP.NET Data Protection; the key ring is persisted in the database (see AppDb.DataProtectionKeys).</summary>
public class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("MailSettings.Password");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
