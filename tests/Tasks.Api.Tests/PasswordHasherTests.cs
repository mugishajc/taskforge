using Tasks.Api.Auth;
using Xunit;

namespace Tasks.Api.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _h = new();

    [Fact]
    public void HashAndVerify_RoundTrip()
    {
        var stored = _h.Hash("correct horse battery staple");
        Assert.True(_h.Verify("correct horse battery staple", stored));
    }

    [Fact]
    public void WrongPasswordFails()
    {
        var stored = _h.Hash("real-pass");
        Assert.False(_h.Verify("other-pass", stored));
    }

    [Fact]
    public void TamperedHashFails()
    {
        var stored = _h.Hash("hello");
        var tampered = stored[..^2] + "AA";
        Assert.False(_h.Verify("hello", tampered));
    }

    [Fact]
    public void MalformedStoredHashFails()
    {
        Assert.False(_h.Verify("anything", "not-a-valid-hash"));
    }
}
