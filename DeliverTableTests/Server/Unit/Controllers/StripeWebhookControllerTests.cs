using DeliverTableInfrastructure.Payments;
using DeliverTableServer.Configuration;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableTests.Global.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Text;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class StripeWebhookControllerTests
{
    private IPaymentService _service = null!;
    private IStripeGateway _stripe = null!;
    private AppEnvironment _env = null!;
    private StripeWebhookController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IPaymentService>();
        _stripe = Substitute.For<IStripeGateway>();
        _env = TestEnvironmentFactory.Create();
        _sut = new StripeWebhookController(_service, _stripe, _env);
    }

    private void SetBody(string payload, string signature)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = payload.Length;
        context.Request.Headers["Stripe-Signature"] = signature;
        _sut.ControllerContext = new ControllerContext { HttpContext = context };
    }

    [Test]
    public async Task Receive_ValidSignature_DispatchesAndReturns200()
    {
        SetBody("{\"id\":\"evt_1\"}", "sig");
        var evt = new Stripe.Event { Id = "evt_1", Type = "payment_intent.succeeded" };
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), "sig", Arg.Any<string>()).Returns(evt);
        _service.HandleStripeEventAsync(evt, Arg.Any<CancellationToken>())
                .Returns(DeliverTableServer.Common.ServiceResult.Success());

        var result = await _sut.Receive(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Receive_InvalidSignature_Returns400()
    {
        SetBody("{}", "bad");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), "bad", Arg.Any<string>())
               .Throws(new Stripe.StripeException("bad sig"));

        var result = await _sut.Receive(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
