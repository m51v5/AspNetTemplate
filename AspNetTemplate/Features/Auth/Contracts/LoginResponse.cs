namespace AspNetTemplate.Features.Auth.Contracts;

public class LoginResponse
{
    public required string AccessToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required string Role { get; set; }
    public required LoginUserDto User { get; set; }
}

public class LoginUserDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}
