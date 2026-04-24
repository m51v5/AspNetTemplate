using FluentValidation;
using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Features.Auth.Data;

namespace AspNetTemplate.Features.Auth.Contracts;

public class CreateUserRequest : IBaseCreateDto<User>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }

    public Task<User> CreateRecord() => Task.FromResult(new User
    {
        FirstName    = FirstName,
        LastName     = LastName,
        Username     = Username,
        Email        = Email,
        PasswordHash = Password.AsHashedPassword(),
    });
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
