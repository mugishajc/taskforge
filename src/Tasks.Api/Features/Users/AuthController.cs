using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Tasks.Api.Features.Users;

[ApiController]
[Route("api/auth")]
public class AuthController(UserService users) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var auth = await users.RegisterAsync(req, ct);
            return Created($"/api/users/{auth.UserId}", auth);
        }
        catch (InvalidOperationException e)
        {
            return Conflict(new { error = e.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var auth = await users.LoginAsync(req, ct);
        return auth is null ? Unauthorized(new { error = "invalid credentials" }) : Ok(auth);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
