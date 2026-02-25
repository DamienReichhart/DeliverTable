using System.Net;
using Microsoft.JSInterop;
using NSubstitute;

namespace DeliverTableTests.Client.Unit.Services.Auth;

/// <summary>
///     Tests for <see cref="JwtInterceptor" />.
///     Verifies that the delegating handler correctly attaches (or omits)
///     the JWT Bearer token on outgoing HTTP requests.
/// </summary>
[TestFixture]
public class JwtInterceptorTests
{
    private IJSRuntime _jsRuntime = null!;
    private JwtInterceptor _sut = null!;
    private HttpMessageInvoker _invoker = null!;

    [SetUp]
    public void SetUp()
    {
        _jsRuntime = Substitute.For<IJSRuntime>();

        _sut = new JwtInterceptor(_jsRuntime)
        {
            InnerHandler = new StubInnerHandler()
        };

        _invoker = new HttpMessageInvoker(_sut);
    }

    [TearDown]
    public void TearDown()
    {
        _invoker.Dispose();
        _sut.Dispose();
    }

    #region SendAsync

    [Test]
    public async Task SendAsync_WithToken_AddsAuthorizationHeader()
    {
        ConfigureStoredToken("my-jwt-token");

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        await _invoker.SendAsync(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.Authorization, Is.Not.Null);
            Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("my-jwt-token"));
        });
    }

    [Test]
    public async Task SendAsync_WithoutToken_DoesNotAddAuthorizationHeader()
    {
        ConfigureStoredToken(null);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        await _invoker.SendAsync(request, CancellationToken.None);

        Assert.That(request.Headers.Authorization, Is.Null);
    }

    [Test]
    public async Task SendAsync_WithEmptyToken_DoesNotAddAuthorizationHeader()
    {
        ConfigureStoredToken("");

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        await _invoker.SendAsync(request, CancellationToken.None);

        Assert.That(request.Headers.Authorization, Is.Null);
    }

    [Test]
    public async Task SendAsync_ForwardsRequestToInnerHandler()
    {
        ConfigureStoredToken(null);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        var response = await _invoker.SendAsync(request, CancellationToken.None);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    private void ConfigureStoredToken(string? token)
    {
        _jsRuntime.InvokeAsync<string>("localStorage.getItem",
                Arg.Is<object[]>(a => a.Length == 1 && (string)a[0] == "authToken"))
            .Returns(new ValueTask<string>(token!));
    }

    /// <summary>Minimal inner handler that returns 200 OK for every request.</summary>
    private sealed class StubInnerHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
