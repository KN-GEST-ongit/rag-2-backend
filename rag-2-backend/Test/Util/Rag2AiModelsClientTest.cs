#region

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using rag_2_backend.Infrastructure.Util;
using Xunit;

#endregion

namespace rag_2_backend.Test.Util;

public class Rag2AiModelsClientTest
{
    [Fact]
    public async Task GetRoutesAsync_ShouldFetchPerGame_WhenRouteGamesConfigured()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/ws/flappybird/routes/" => JsonResponse(
                    """[{"path":"/ws/flappybird/flappybird-ppo/","name":"PPO"}]"""
                ),
                "/ws/pong/routes/" => JsonResponse(
                    """[{"path":"/ws/pong/pong-dqn/","name":"DQN"}]"""
                ),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        var client = CreateClient(handler, new Dictionary<string, string?>
        {
            ["Rag2Ai:Enabled"] = "true",
            ["Rag2Ai:BaseUrl"] = "http://ai.test",
            ["Rag2Ai:RouteGames:0"] = "flappybird",
            ["Rag2Ai:RouteGames:1"] = "pong"
        });

        var routes = await client.GetRoutesAsync();

        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, r => r.Path.Contains("flappybird-ppo"));
        Assert.Contains(routes, r => r.Path.Contains("pong-dqn"));
    }

    [Fact]
    public async Task GetRoutesAsync_ShouldUseRoutesPath_WhenRouteGamesMissing()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal("/all-routes", request.RequestUri!.AbsolutePath);
            return JsonResponse("""[{"path":"/ws/pong/pong-ppo/","name":"PPO"}]""");
        });

        var client = CreateClient(handler, new Dictionary<string, string?>
        {
            ["Rag2Ai:Enabled"] = "true",
            ["Rag2Ai:BaseUrl"] = "http://ai.test",
            ["Rag2Ai:RoutesPath"] = "/all-routes"
        });

        var routes = await client.GetRoutesAsync();

        Assert.Single(routes);
        Assert.Equal("PPO", routes[0].Name);
    }

    [Fact]
    public async Task GetRoutesAsync_ShouldReturnEmpty_WhenDisabled()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP should not be called")
        );

        var client = CreateClient(handler, new Dictionary<string, string?>
        {
            ["Rag2Ai:Enabled"] = "false",
            ["Rag2Ai:BaseUrl"] = "http://ai.test",
            ["Rag2Ai:RouteGames:0"] = "flappybird"
        });

        var routes = await client.GetRoutesAsync();

        Assert.Empty(routes);
    }

    [Fact]
    public async Task GetRoutesAsync_ShouldSkipFailedGames_AndReturnSuccessfulOnes()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/ws/flappybird/routes/" => JsonResponse(
                    """[{"path":"/ws/flappybird/flappybird-ppo/","name":"PPO"}]"""
                ),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        var client = CreateClient(handler, new Dictionary<string, string?>
        {
            ["Rag2Ai:Enabled"] = "true",
            ["Rag2Ai:BaseUrl"] = "http://ai.test",
            ["Rag2Ai:RouteGames:0"] = "flappybird",
            ["Rag2Ai:RouteGames:1"] = "missing-game"
        });

        var routes = await client.GetRoutesAsync();

        Assert.Single(routes);
        Assert.Contains("flappybird-ppo", routes[0].Path);
    }

    private static Rag2AiModelsClient CreateClient(
        HttpMessageHandler handler,
        Dictionary<string, string?> settings
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new Rag2AiModelsClient(
            new HttpClient(handler),
            configuration,
            NullLogger<Rag2AiModelsClient>.Instance
        );
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(responder(request));
    }
}
