using Xunit;

namespace Blobatar.Tests;

/// <summary>
/// Opt-in: compares this port against the deployed upstream endpoint. Needs the network, so it is
/// excluded from the default run; select it with <c>--filter Category=Network</c>.
/// </summary>
[Trait("Category", "Network")]
public class LiveEndpointTests
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("https://blobatar.dev/") };

    [Theory]
    [InlineData("alain", "", "")]
    [InlineData("alain@example.com", "?size=64", "size")]
    [InlineData("Team Rocket 7", "?background=squircle", "squircle")]
    [InlineData("user-42", "?expression=happy", "happy")]
    [InlineData("user-42", "?expression=mad&hue=210&tone=0.5", "mad-hue-tone")]
    public async Task The_deployed_endpoint_renders_the_same_markup(string name, string query, string label)
    {
        var options = label switch
        {
            "size" => new BlobatarOptions { Size = 64 },
            "squircle" => new BlobatarOptions { Background = BlobatarBackground.Squircle },
            "happy" => new BlobatarOptions { Expression = BlobatarExpression.Happy },
            "mad-hue-tone" => new BlobatarOptions { Expression = BlobatarExpression.Mad, Hue = 210, Tone = 0.5 },
            _ => new BlobatarOptions(),
        };

        var remote = await Http.GetStringAsync(new Uri("avatar/" + Uri.EscapeDataString(name) + query + (query.Length == 0 ? "?gen=2" : "&gen=2"), UriKind.Relative));
        Assert.Equal(remote.Trim(), BlobatarRenderer.Render(name, options));
    }
}
