using System.Text.Json;

namespace Fhi.ClientCredentialsUsingSecrets.Tests;

public class Tests
{
    [Test]
    public void TestSerialization()
    {
        var sut = new ClientCredentialsConfiguration();
        var json = JsonSerializer.Serialize(sut);
        TestContext.Out.WriteLine(json);
        Assert.That(json, Is.Not.Null);
        var api = new ApiKonfigurasjon();
        var json2 = JsonSerializer.Serialize(api);
        TestContext.Out.WriteLine(json2);
        Assert.That(json2, Is.Not.Null);
    }
}
