using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantAccountControllerTests
{
    private IRestaurantAccountService _accountService = null!;
    private RestaurantAccountController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = Substitute.For<IRestaurantAccountService>();
        _sut = new RestaurantAccountController(_accountService);
    }

    [Test]
    public async Task GetAccount_WithValidOwner_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        var accountDto = new RestaurantAccountDto
        {
            Balance = 360m,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            }
        };
        _accountService.GetAccountAsync(1, 5, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Success(accountDto));

        var result = await _sut.GetAccount(1, new TransactionQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAccount_WhenUnauthorized_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);

        var result = await _sut.GetAccount(1, new TransactionQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Withdraw_WithValidRequest_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        var accountDto = new RestaurantAccountDto
        {
            Balance = 300m,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            }
        };
        _accountService.WithdrawAsync(1, 5, Arg.Any<WithdrawRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Success(accountDto));

        var result = await _sut.Withdraw(1, new WithdrawRequest { Amount = 200 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Withdraw_WhenServiceFails_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        _accountService.WithdrawAsync(1, 5, Arg.Any<WithdrawRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Failure(new ServiceError("Solde insuffisant", 400)));

        var result = await _sut.Withdraw(1, new WithdrawRequest { Amount = 9999 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
    }
}
