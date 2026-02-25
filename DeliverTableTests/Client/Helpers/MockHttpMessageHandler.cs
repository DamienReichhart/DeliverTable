using System.Net;
using System.Net.Http.Json;

namespace DeliverTableTests.Client.Helpers;

/// <summary>
///     Fake <see cref="HttpMessageHandler" /> that lets tests control HTTP responses
///     without touching the network. Responses are consumed in FIFO order; when the
///     queue is empty the <see cref="DefaultResponse" /> is returned.
///     All outgoing requests are captured in <see cref="SentRequests" /> for verification.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _queuedResponses = new();

    /// <summary>Every request that passed through <see cref="SendAsync" />, in order.</summary>
    public List<HttpRequestMessage> SentRequests { get; } = [];

    /// <summary>Returned when the queue is empty. Defaults to <c>200 OK</c>.</summary>
    public HttpResponseMessage DefaultResponse { get; set; } = new(HttpStatusCode.OK);

    /// <summary>Enqueue a raw <see cref="HttpResponseMessage" /> to be returned by the next request.</summary>
    public void QueueResponse(HttpResponseMessage response) =>
        _queuedResponses.Enqueue(response);

    /// <summary>
    ///     Enqueue a JSON-serialized response with the given status code.
    ///     Convenient for simulating typed API responses.
    /// </summary>
    public void QueueJsonResponse<T>(T content, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        _queuedResponses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(content)
        });

    /// <summary>Enqueue an error response with a plain-text body.</summary>
    public void QueueErrorResponse(HttpStatusCode statusCode, string body = "") =>
        _queuedResponses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        });

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        SentRequests.Add(request);

        var response = _queuedResponses.TryDequeue(out var queued)
            ? queued
            : DefaultResponse;

        return Task.FromResult(response);
    }
}
