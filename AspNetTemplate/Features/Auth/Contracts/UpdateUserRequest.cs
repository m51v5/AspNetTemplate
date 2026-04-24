using FluentValidation;
using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Features.Auth.Data;

namespace AspNetTemplate.Features.Auth.Contracts;

public class UpdateUserRequest : IBaseUpdateDto<User>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }

    public Task UpdateRecord(User entity)
    {
        if (FirstName is not null) entity.FirstName = FirstName;
        if (LastName is not null)  entity.LastName  = LastName;
        if (Username is not null)  entity.Username  = Username;
        if (Email is not null)     entity.Email     = Email;
        if (IsActive.HasValue)     entity.IsActive  = IsActive.Value;
        return Task.CompletedTask;
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.Username).MaximumLength(50).When(x => x.Username is not null);
        RuleFor(x => x.Email).MaximumLength(320).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.NewPassword).MinimumLength(8).When(x => x.NewPassword is not null);
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("CurrentPassword is required when changing password.")
            .When(x => x.NewPassword is not null);
    }
}
