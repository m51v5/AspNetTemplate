using System.Net;
using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Core.Infra.Helpers;
using AspNetTemplate.Data;
using AspNetTemplate.Features.Auth.Contracts;
using AspNetTemplate.Features.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Features.Auth.Services;

public interface IUserService
{
    Task<IApiResponse> CreateAsync(CreateUserRequest req, IFormFile? image, IErrorEventCollector errorEvents, CancellationToken ct);
    Task<IApiResponse> GetAllAsync(UserFilter filter, CancellationToken ct);
    Task<IApiResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IApiResponse> UpdateAsync(Guid id, UpdateUserRequest req, IFormFile? image, IErrorEventCollector errorEvents, CancellationToken ct);
    Task<IApiResponse> DeleteAsync(Guid id, CancellationToken ct);
    Task<IApiResponse> RestoreAsync(Guid id, CancellationToken ct);
    Task<IApiResponse> PermanentDeleteAsync(Guid id, CancellationToken ct);
}

[AutoRegister(typeof(IUserService))]
public class UserService(AppData context, ILogger<UserService> logger) : BaseService(context), IUserService
{
    private static readonly string RootPath = Path.Combine(AppContext.BaseDirectory, AppState.UploadsPath);
    private const string FolderName = "users";

    public async Task<IApiResponse> CreateAsync(
        CreateUserRequest req, IFormFile? image, IErrorEventCollector errorEvents, CancellationToken ct)
    {
        if (await _context.Users.AnyAsync(u => u.Username == req.Username && !u.IsDeleted, ct))
            return new FailureResponse($"Username '{req.Username}' is already taken.", HttpStatusCode.BadRequest);

        if (await _context.Users.AnyAsync(u => u.Email == req.Email && !u.IsDeleted, ct))
            return new FailureResponse($"Email '{req.Email}' is already registered.", HttpStatusCode.BadRequest);

        var user = await req.CreateRecord();

        var uploadError = await FileHelper.TryUploadOptionalAsync(
            RootPath, errorEvents, logger, _context, user, image, FolderName, ct);

        if (uploadError is not null)
            return new FailureResponse(uploadError, HttpStatusCode.BadRequest);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(user).Reference(u => u.File).LoadAsync(ct);

        return new SuccessResponse<UserResponse>(new UserResponse(user));
    }

    public async Task<IApiResponse> GetAllAsync(UserFilter filter, CancellationToken ct)
    {
        var query = _context.Users
            .OrderByDescending(u => u.Id)
            .AsNoTracking();

        query = (IQueryable<User>)filter.ApplyFilters(query);

        var result = await PagedList<UserResponse>.Create(
            query.Select(UserResponse.Projection),
            filter.PageNumber, filter.PageSize, ct);

        return new SuccessResponse<PagedList<UserResponse>>(result);
    }

    public async Task<IApiResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id && !u.IsDeleted)
            .Select(UserResponse.Projection)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return new FailureResponse("User not found.", HttpStatusCode.NotFound);

        return new SuccessResponse<UserResponse>(user);
    }

    public async Task<IApiResponse> UpdateAsync(
        Guid id, UpdateUserRequest req, IFormFile? image, IErrorEventCollector errorEvents, CancellationToken ct)
    {
        var user = await _context.Users
            .Include(u => u.File)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

        if (user is null)
            return new FailureResponse("User not found.", HttpStatusCode.NotFound);

        if (user.IsAdmin)
            return new FailureResponse("The admin account cannot be modified via this endpoint.", HttpStatusCode.BadRequest);

        if (req.Username is not null &&
            await _context.Users.AnyAsync(u => u.Id != id && u.Username == req.Username && !u.IsDeleted, ct))
            return new FailureResponse($"Username '{req.Username}' is already taken.", HttpStatusCode.BadRequest);

        if (req.Email is not null &&
            await _context.Users.AnyAsync(u => u.Id != id && u.Email == req.Email && !u.IsDeleted, ct))
            return new FailureResponse($"Email '{req.Email}' is already registered.", HttpStatusCode.BadRequest);

        if (req.NewPassword is not null)
        {
            if (req.CurrentPassword is null || !req.CurrentPassword.VerifyHashedPassword(user.PasswordHash))
                return new FailureResponse("Current password is incorrect.", HttpStatusCode.BadRequest);

            user.PasswordHash = req.NewPassword.AsHashedPassword();
        }

        var uploadError = await FileHelper.TryUploadOptionalAsync(
            RootPath, errorEvents, logger, _context, user, image, FolderName, ct);

        if (uploadError is not null)
            return new FailureResponse(uploadError, HttpStatusCode.BadRequest);

        await req.UpdateRecord(user);
        await _context.SaveChangesAsync(ct);

        return new SuccessResponse<UserResponse>(new UserResponse(user));
    }

    public async Task<IApiResponse> DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

        if (user is null)
            return new FailureResponse("User not found.", HttpStatusCode.NotFound);

        if (user.IsAdmin)
            return new FailureResponse("The admin account cannot be deleted.", HttpStatusCode.BadRequest);

        await user.SoftDelete();
        await _context.SaveChangesAsync(ct);

        return new EmptySuccessResponse();
    }

    public async Task<IApiResponse> RestoreAsync(Guid id, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted, ct);

        if (user is null)
            return new FailureResponse("User not found or is not deleted.", HttpStatusCode.NotFound);

        await user.UnSoftDelete();
        await _context.SaveChangesAsync(ct);

        return new EmptySuccessResponse();
    }

    public async Task<IApiResponse> PermanentDeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return new FailureResponse("User not found.", HttpStatusCode.NotFound);

        if (user.IsAdmin)
            return new FailureResponse("The admin account cannot be deleted.", HttpStatusCode.BadRequest);

        if (!user.IsDeleted)
            return new FailureResponse("User must be soft-deleted before permanent deletion.", HttpStatusCode.BadRequest);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(ct);

        return new EmptySuccessResponse();
    }
}
