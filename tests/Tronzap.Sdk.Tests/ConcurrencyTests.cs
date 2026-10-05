using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Tronzap.Sdk.Responses;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class ConcurrencyTests
{
    [Fact]
    public async Task OneClientServesParallelRequests()
    {
        await using var server = TestServer.Start(request =>
        {
            string address = request.Json!["address"]!.GetValue<string>();
            var result = new JsonObject { ["resources"] = new JsonObject { ["energy"] = int.Parse(address[5..], System.Globalization.CultureInfo.InvariantCulture) } };
            return Reply.Ok(result.ToJsonString());
        });
        TronzapClient client = Client(server);
        string[] addresses = Enumerable.Range(0, 64).Select(i => $"TAddr{i}").ToArray();

        AddressInfo[] results = await Task.WhenAll(addresses.Select(a => Task.Run(() => client.GetAddressInfoAsync(a, Ct), Ct)));

        for (int i = 0; i < addresses.Length; i++)
        {
            Assert.Equal(i, results[i].Resources.Energy);
        }

        Assert.Equal(addresses.Length, server.Requests.Count);
        Assert.All(server.Requests, r => Assert.Equal(ExpectedSignature(r.Body), r.Header("X-Signature")));
    }
}
