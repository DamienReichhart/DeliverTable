using DeliverTableInfrastructure.Models;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace DeliverTableTests.Global.Helpers;

/// <summary>
///     Creates properly configured <see cref="UserManager{TUser}"/> substitutes.
///     <see cref="UserManager{TUser}"/> has many constructor dependencies;
///     this helper encapsulates the boilerplate so each test class stays focused.
/// </summary>
public static class UserManagerMockHelper
{
    /// <summary>
    ///     Creates a NSubstitute mock of <see cref="UserManager{User}"/>
    ///     with all required constructor dependencies satisfied by fakes.
    /// </summary>
    public static UserManager<User> Create()
    {
        IUserStore<User> store = Substitute.For<IUserStore<User>>();

        return Substitute.For<UserManager<User>>(
            store,
            /* IOptions<IdentityOptions> */ null,
            /* IPasswordHasher<User> */ null,
            /* IEnumerable<IUserValidator<User>> */ null,
            /* IEnumerable<IPasswordValidator<User>> */ null,
            /* ILookupNormalizer */ null,
            /* IdentityErrorDescriber */ null,
            /* IServiceProvider */ null,
            /* ILogger<UserManager<User>> */ null
        );
    }
}
