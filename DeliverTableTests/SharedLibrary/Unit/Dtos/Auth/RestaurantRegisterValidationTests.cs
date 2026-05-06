using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableTests.Global.Helpers;
using DeliverTableTests.SharedLibrary.Factories;

namespace DeliverTableTests.SharedLibrary.Unit.Dtos.Auth;

/// <summary>
///     Validation tests for <see cref="RestaurantRegister" />.
///     Covers Required, MinLength, MaxLength, EmailAddress, and Compare constraints.
/// </summary>
[TestFixture]
[Category("Validation")]
public class RestaurantRegisterValidationTests
{
    [Test]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        ValidationTestHelper.AssertValid(request);
    }

    #region FirstName

    [Test]
    public void FirstName_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.FirstName = "";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.FirstName));
    }

    [Test]
    public void FirstName_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.FirstName = new string('A', 50);

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.FirstName));
    }

    [Test]
    public void FirstName_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.FirstName = new string('A', 51);

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.FirstName));
    }

    #endregion

    #region LastName

    [Test]
    public void LastName_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.LastName = "";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.LastName));
    }

    [Test]
    public void LastName_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.LastName = new string('B', 100);

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.LastName));
    }

    [Test]
    public void LastName_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.LastName = new string('B', 101);

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.LastName));
    }

    #endregion

    #region ContactPhoneNumber

    [Test]
    public void ContactPhoneNumber_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ContactPhoneNumber = "";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.ContactPhoneNumber));
    }

    [Test]
    public void ContactPhoneNumber_BelowMinLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ContactPhoneNumber = new string('0', 9);

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.ContactPhoneNumber));
    }

    [Test]
    public void ContactPhoneNumber_AtMinLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ContactPhoneNumber = new string('0', 10);

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.ContactPhoneNumber));
    }

    [Test]
    public void ContactPhoneNumber_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ContactPhoneNumber = new string('0', 20);

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.ContactPhoneNumber));
    }

    [Test]
    public void ContactPhoneNumber_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ContactPhoneNumber = new string('0', 21);

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.ContactPhoneNumber));
    }

    #endregion

    #region Email

    [Test]
    public void Email_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Email = "";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.Email));
    }

    [TestCase("not-an-email")]
    [TestCase("@no-local.com")]
    [TestCase("no-domain@")]
    public void Email_WhenInvalidFormat_ShouldFail(string invalidEmail)
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Email = invalidEmail;

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.Email));
    }

    [Test]
    public void Email_AtMaxLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Email = new string('a', 41) + "@test.com"; // 50 chars

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.Email));
    }

    [Test]
    public void Email_ExceedingMaxLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Email = new string('a', 42) + "@test.com"; // 51 chars

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.Email));
    }

    #endregion

    #region Password

    [Test]
    public void Password_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Password = "";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.Password));
    }

    [Test]
    public void Password_BelowMinLength_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Password = new string('x', 11);
        request.ConfirmPassword = request.Password;

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.Password));
    }

    [Test]
    public void Password_AtMinLength_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Password = new string('x', 12);
        request.ConfirmPassword = request.Password;

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.Password));
    }

    #endregion

    #region ConfirmPassword

    [Test]
    public void ConfirmPassword_WhenEmpty_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ConfirmPassword = "";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.ConfirmPassword));
    }

    [Test]
    public void ConfirmPassword_WhenMismatch_ShouldFail()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.ConfirmPassword = "DifferentPass12!";

        ValidationTestHelper.AssertHasError(request, nameof(RestaurantRegister.ConfirmPassword));
    }

    [Test]
    public void ConfirmPassword_WhenMatching_ShouldPass()
    {
        var request = SharedLibraryDtoFactory.CreateValidRestaurantRegister();
        request.Password = "MatchingPass12!";
        request.ConfirmPassword = "MatchingPass12!";

        ValidationTestHelper.AssertNoError(request, nameof(RestaurantRegister.ConfirmPassword));
    }

    #endregion
}
