using FluentValidation;

namespace AspNetTemplate.Features.Auth.Contracts;

public record ResetUserPasswordRequest(Guid UserId, string NewPassword);

public class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
