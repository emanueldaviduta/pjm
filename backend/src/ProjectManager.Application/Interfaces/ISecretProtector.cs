namespace ProjectManager.Application.Interfaces;

/// <summary>Encrypts secrets (such as the mailbox password) before they are stored.</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
