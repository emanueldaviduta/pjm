using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Application.Tests;

public class CloudinaryFileStorageTests
{
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("cloud", null, null)]
    [InlineData("cloud", "key", "")]
    [InlineData("cloud", " ", "secret")]
    public async Task MissingOrPartialSettings_AreNotConfigured(string? cloud, string? key, string? secret)
    {
        var storage = new CloudinaryFileStorage(new CloudinaryOptions { CloudName = cloud, ApiKey = key, ApiSecret = secret });

        Assert.False(storage.IsConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.UploadAsync(Stream.Null, "a.txt", "text/plain", "pjm/tasks"));
    }

    [Fact]
    public void CompleteSettings_AreConfigured()
    {
        var storage = new CloudinaryFileStorage(new CloudinaryOptions { CloudName = "cloud", ApiKey = "key", ApiSecret = "secret" });

        Assert.True(storage.IsConfigured);
    }
}
