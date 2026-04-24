using System.Net;
using FluentValidation;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Core.Infra.Helpers;
using AspNetTemplate.Data;
using AspNetTemplate.Features.Auth.Contracts;
using AspNetTemplate.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetTemplate.Features.Auth;

[ApiController]
[Route("api/users")]
[RoleAuthorize(Enums.AppRoles.Admin)]
public class UsersController(
    IUserService userService,
    IErrorEventCollector errorEvents,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SuccessResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] CreateUserRequest req, IFormFile? image, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(new FailureResponse(validation.ToString(), HttpStatusCode.BadRequest));

        var result = await userService.CreateAsync(req, image, errorEvents, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SuccessResponse<PagedList<UserResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] UserFilter filter, CancellationToken ct)
    {
        var result = await userService.GetAllAsync(filter, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SuccessResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(id, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SuccessResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateUserRequest req, IFormFile? image, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(new FailureResponse(validation.ToString(), HttpStatusCode.BadRequest));

        var result = await userService.UpdateAsync(id, req, image, errorEvents, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}/soft")]
    [ProducesResponseType(typeof(EmptySuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
    {
        var result = await userService.DeleteAsync(id, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPatch("{id}/soft")]
    [ProducesResponseType(typeof(EmptySuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await userService.RestoreAsync(id, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(EmptySuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await userService.PermanentDeleteAsync(id, ct);
        return StatusCode((int)result.StatusCode, result);
    }
}
