using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService;

    [HttpGet(ApiRoutes.Admin.UsersRoute)]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var result = await _adminService.GetAllUsersAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.UserByIdRoute)]
    public async Task<IActionResult> GetUserById(int id, CancellationToken ct)
    {
        var result = await _adminService.GetUserByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.UsersRoute)]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request, CancellationToken ct)
    {
        var result = await _adminService.CreateUserAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUserById), new { id = result.Value!.Id }, result.Value)
            : result.Error!.ToErrorResult();
    }

    [HttpPut(ApiRoutes.Admin.UserByIdRoute)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct)
    {
        var result = await _adminService.UpdateUserAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.UserByIdRoute)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var result = await _adminService.DeleteUserAsync(id, ct);
        return result.ToNoContentResult();
    }

    [HttpPut(ApiRoutes.Admin.UserByIdRoleRoute)]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        var result = await _adminService.UpdateUserRoleAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Admin.UserByIdStatusRoute)]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        var result = await _adminService.UpdateUserStatusAsync(id, request, ct);
        return result.ToOkResult();
    }
}
