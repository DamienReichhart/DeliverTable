using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class PaymentControllerTests
{
    private IPaymentService _service = null!;
    private PaymentController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IPaymentService>();
        _sut = new PaymentController(_service);
    }

    [Test]
    public async Task Cancel_Authenticated_ReturnsNoContent()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service.CancelAuthorizationAsync(42, 7, Arg.Any<CancellationToken>())
                .Returns(ServiceResult.Success());

        var result = await _sut.Cancel(42, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Cancel_ServiceError_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service.CancelAuthorizationAsync(42, 7, Arg.Any<CancellationToken>())
                .Returns(ServiceResult.Failure(new ServiceError("Annulation impossible", 400)));

        var result = await _sut.Cancel(42, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Cancel_OrderNotOwnedByCustomer_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service.CancelAuthorizationAsync(42, 7, Arg.Any<CancellationToken>())
                .Returns(ServiceResult.Failure(new ServiceError("Vous n'êtes pas autorisé à modifier cette commande", 403)));

        var result = await _sut.Cancel(42, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<NoContentResult>());
    }
}
