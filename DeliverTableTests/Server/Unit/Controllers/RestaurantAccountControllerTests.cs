using System.Security.Claims;
using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.AspNetCore.Http;
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

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, nameof(UserRole.RestaurantOwner))
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Test]
    public async Task GetAccount_WithValidOwner_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var accountDto = new RestaurantAccountDto
        {
            Balance = 360m,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 20
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
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _sut.GetAccount(1, new TransactionQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Withdraw_WithValidRequest_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var accountDto = new RestaurantAccountDto
        {
            Balance = 300m,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 20
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
        SetupAuthenticatedUser("5");
        _accountService.WithdrawAsync(1, 5, Arg.Any<WithdrawRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantAccountDto>.Failure(new ServiceError("Solde insuffisant", 400)));

        var result = await _sut.Withdraw(1, new WithdrawRequest { Amount = 9999 }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
    }
}
