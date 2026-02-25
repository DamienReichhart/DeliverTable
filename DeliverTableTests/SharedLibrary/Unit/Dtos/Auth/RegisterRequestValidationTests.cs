using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Global.Helpers;
using DeliverTableTests.SharedLibrary.Factories;

namespace DeliverTableTests.SharedLibrary.Unit.Dtos.Auth;

/// <summary>
///     Validation tests for <see cref="RegisterRequest" />.
///     Covers every DataAnnotation rule: Required, MaxLength, MinLength, EmailAddress, Compare.
/// </summary>
[TestFixture]
[Category("Validation")]
public class RegisterRequestValidationTests
{
    [Test]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        ValidationTestHelper.AssertValid(request);
    }

    #region FirstName

    [Test]
    public void FirstName_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.FirstName = "";

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.FirstName));
    }

    [Test]
    public void FirstName_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.FirstName = new string('A', 50);

        ValidationTestHelper.AssertNoError(request, nameof(RegisterRequest.FirstName));
    }

    [Test]
    public void FirstName_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.FirstName = new string('A', 51);

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.FirstName));
    }

    #endregion

    #region LastName

    [Test]
    public void LastName_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.LastName = "";

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.LastName));
    }

    [Test]
    public void LastName_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.LastName = new string('B', 100);

        ValidationTestHelper.AssertNoError(request, nameof(RegisterRequest.LastName));
    }

    [Test]
    public void LastName_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.LastName = new string('B', 101);

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.LastName));
    }

    #endregion

    #region Email

    [Test]
    public void Email_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Email = "";

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.Email));
    }

    [TestCase("not-an-email")]
    [TestCase("@no-local.com")]
    [TestCase("no-domain@")]
    public void Email_WhenInvalidFormat_ShouldFail(string invalidEmail)
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Email = invalidEmail;

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.Email));
    }

    [Test]
    public void Email_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Email = new string('a', 91) + "@test.com"; // 100 chars

        ValidationTestHelper.AssertNoError(request, nameof(RegisterRequest.Email));
    }

    [Test]
    public void Email_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Email = new string('a', 92) + "@test.com"; // 101 chars

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.Email));
    }

    #endregion

    #region Password

    [Test]
    public void Password_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Password = "";

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.Password));
    }

    [Test]
    public void Password_BelowMinLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Password = new string('x', 11);
        request.ConfirmPassword = request.Password;

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.Password));
    }

    [Test]
    public void Password_AtMinLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Password = new string('x', 12);
        request.ConfirmPassword = request.Password;

        ValidationTestHelper.AssertNoError(request, nameof(RegisterRequest.Password));
    }

    #endregion

    #region ConfirmPassword

    [Test]
    public void ConfirmPassword_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.ConfirmPassword = "";

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.ConfirmPassword));
    }

    [Test]
    public void ConfirmPassword_WhenMismatch_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.ConfirmPassword = "DifferentPass12!";

        ValidationTestHelper.AssertHasError(request, nameof(RegisterRequest.ConfirmPassword));
    }

    [Test]
    public void ConfirmPassword_WhenMatching_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRegisterRequest();
        request.Password = "MatchingPass12!";
        request.ConfirmPassword = "MatchingPass12!";

        ValidationTestHelper.AssertNoError(request, nameof(RegisterRequest.ConfirmPassword));
    }

    #endregion
}
