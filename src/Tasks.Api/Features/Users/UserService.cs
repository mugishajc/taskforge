using Microsoft.EntityFrameworkCore;
using Tasks.Api.Auth;
using Tasks.Api.Data;
using Tasks.Api.Domain;

namespace Tasks.Api.Features.Users;

public record RegisterRequest(string Email, string Password, string? Name);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, int UserId, string Email);
public record UserProfileDto(int Id, string Email, string Name, DateTime CreatedAt);
public record UpdateProfileRequest(string Name);

public class UserService(AppDbContext db, IPasswordHasher hasher, IJwtIssuer jwt)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            Email = email,
            Name = req.Name?.Trim() ?? "",
            PasswordHash = hasher.Hash(req.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = jwt.IssueAccessToken(user.Id, user.Email);
        return new AuthResponse(token, user.Id, user.Email);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return null;
        if (!hasher.Verify(req.Password, user.PasswordHash)) return null;
        var token = jwt.IssueAccessToken(user.Id, user.Email);
        return new AuthResponse(token, user.Id, user.Email);
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        return user is null ? null : new UserProfileDto(user.Id, user.Email, user.Name, user.CreatedAt);
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest req, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return null;
        user.Name = req.Name.Trim();
        await db.SaveChangesAsync(ct);
        return new UserProfileDto(user.Id, user.Email, user.Name, user.CreatedAt);
    }

    public async Task<List<UserProfileDto>> ListUsersAsync(CancellationToken ct) =>
        await db.Users.OrderBy(u => u.Id)
            .Select(u => new UserProfileDto(u.Id, u.Email, u.Name, u.CreatedAt))
            .ToListAsync(ct);
}
