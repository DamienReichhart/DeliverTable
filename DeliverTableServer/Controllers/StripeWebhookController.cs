using DeliverTableInfrastructure.Payments;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.StripeWebhook.Base)]
public class StripeWebhookController(
    IPaymentService paymentService,
    IStripeGateway stripe,
    AppEnvironment env) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost(ApiRoutes.StripeWebhook.WebhookRoute)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["Stripe-Signature"].ToString();

        Stripe.Event stripeEvent;
        try
        {
            stripeEvent = stripe.ConstructWebhookEvent(payload, signature, env.StripeWebhookSecret);
        }
        catch (Stripe.StripeException)
        {
            return BadRequest(new { error = ErrorMessages.WebhookSignatureInvalid });
        }

        var result = await paymentService.HandleStripeEventAsync(stripeEvent, ct);
        return result.IsSuccess ? Ok() : StatusCode(500, new { error = result.Error!.Message });
    }
}
