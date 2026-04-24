using FluentValidation;

namespace AspNetTemplate.Features.Auth.Contracts;

// TODO : login email and phone number
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
