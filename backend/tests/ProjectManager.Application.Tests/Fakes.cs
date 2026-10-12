using ProjectManager.Application.Interfaces;

namespace ProjectManager.Application.Tests;

/// <summary>Reversible and visibly different from the input, so tests can tell protected from plain values.</summary>
internal sealed class FakeProtector : ISecretProtector
{
    public string Protect(string plaintext) => "enc:" + new string(plaintext.Reverse().ToArray());
    public string Unprotect(string protectedValue) => new(protectedValue["enc:".Length..].Reverse().ToArray());
}

/// <summary>An in-memory IMAP folder: messages get increasing UIDs; FetchNewAsync follows the IMailboxClient contract.</summary>
internal sealed class FakeMailbox : IMailboxClient
{
    private readonly List<MailboxMessage> _messages = [];

    public long UidValidity { get; set; } = 1;
    public int FetchCalls { get; private set; }
    public List<MailboxConnection> Tests { get; } = [];
    public MailboxTestResult TestResult { get; set; } = new(true, "Connected.");
    /// <summary>When set, the next fetch throws this and the field is cleared.</summary>
    public Exception? FailNextFetch { get; set; }

    public MailboxMessage Add(string from, string subject = "Subject", string body = "Body",
        string? messageId = null, params MailboxAttachment[] attachments)
    {
        var uid = (_messages.Count == 0 ? 0 : _messages[^1].Uid) + 1;
        var message = new MailboxMessage(uid, messageId ?? $"<{uid}@test>", from, subject,
            new DateTime(2026, 10, 12, 9, 30, 0, DateTimeKind.Utc), body, attachments);
        _messages.Add(message);
        return message;
    }

    public Task<MailboxTestResult> TestAsync(MailboxConnection connection, CancellationToken cancellationToken = default)
    {
        Tests.Add(connection);
        return Task.FromResult(TestResult);
    }

    public Task<MailboxFetchResult> FetchNewAsync(MailboxConnection connection, long? uidValidity, long? lastUid,
        CancellationToken cancellationToken = default)
    {
        FetchCalls++;
        if (FailNextFetch is { } failure)
        {
            FailNextFetch = null;
            throw failure;
        }

        var highest = _messages.Count == 0 ? 0 : _messages[^1].Uid;
        IReadOnlyList<MailboxMessage> newer = uidValidity == UidValidity && lastUid != null
            ? _messages.Where(m => m.Uid > lastUid).ToList()
            : [];
        return Task.FromResult(new MailboxFetchResult(UidValidity, highest, newer));
    }
}

/// <summary>Records uploads; can be switched off (not configured) or made to fail for a given file name.</summary>
internal sealed class FakeFileStorage : IFileStorage
{
    public bool IsConfigured { get; set; } = true;
    public List<string> Uploaded { get; } = [];
    public HashSet<string> FailFor { get; } = [];

    public async Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("Upload attempted while storage is not configured.");
        if (FailFor.Contains(fileName)) throw new IOException("Upload rejected by storage.");

        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        Uploaded.Add(fileName);
        var publicId = $"{folder}/{Uploaded.Count}-{fileName}";
        return new StoredFile(publicId, $"https://files.test/{publicId}", copy.Length);
    }
}
