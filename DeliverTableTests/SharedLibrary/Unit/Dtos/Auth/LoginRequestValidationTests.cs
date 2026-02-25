using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Global.Helpers;
using DeliverTableTests.SharedLibrary.Factories;

namespace DeliverTableTests.SharedLibrary.Unit.Dtos.Auth;

/// <summary>
///     Validation tests for <see cref="LoginRequest" />.
///     Each test starts from a factory-built valid instance and mutates a single field,
///     isolating the rule under test.
/// </summary>
[TestFixture]
[Category("Validation")]
public class LoginRequestValidationTests
{
    [Test]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        ValidationTestHelper.AssertValid(request);
    }

    #region Email

    [Test]
    public void Email_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Email = "";

        ValidationTestHelper.AssertHasError(request, nameof(LoginRequest.Email));
    }

    [TestCase("plaintext")]
    [TestCase("@missing-local.com")]
    [TestCase("missing-domain@")]
    public void Email_WhenInvalidFormat_ShouldFail(string invalidEmail)
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Email = invalidEmail;

        ValidationTestHelper.AssertHasError(request, nameof(LoginRequest.Email));
    }

    [Test]
    public void Email_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Email = new string('a', 91) + "@test.com"; // 100 chars

        ValidationTestHelper.AssertNoError(request, nameof(LoginRequest.Email));
    }

    [Test]
    public void Email_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Email = new string('a', 92) + "@test.com"; // 101 chars

        ValidationTestHelper.AssertHasError(request, nameof(LoginRequest.Email));
    }

    #endregion

    #region Password

    [Test]
    public void Password_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Password = "";

        ValidationTestHelper.AssertHasError(request, nameof(LoginRequest.Password));
    }

    [Test]
    public void Password_BelowMinLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Password = new string('x', 11); // 11 < 12

        ValidationTestHelper.AssertHasError(request, nameof(LoginRequest.Password));
    }

    [Test]
    public void Password_AtMinLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidLoginRequest();
        request.Password = new string('x', 12);

        ValidationTestHelper.AssertNoError(request, nameof(LoginRequest.Password));
    }

    #endregion
}
