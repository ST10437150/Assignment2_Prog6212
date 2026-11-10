using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private static readonly string[] ValidRoles = { "Organiser", "Participant" };

    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public AuthController(RaceDayDbContext db, IPasswordService passwords, ITokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    /// <summary>
    /// Creates a new user account as either an Organiser or a Participant.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Expects: name, email, password (minimum 8 characters) and role
    /// ("Organiser" or "Participant"). The password is hashed before it is stored.
    /// </remarks>
    /// <param name="request">The new account details.</param>
    /// <response code="201">Account created. Returns the user object (without the password).</response>
    /// <response code="400">Invalid data, or a role other than Organiser / Participant.</response>
    /// <response code="409">The email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        var role = ValidRoles.FirstOrDefault(r =>
        r.Equals(request.Role.Trim(), StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "Email already registered." });
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created($"/api/users/{user.UserID}", UserResponse.From(user));
    }

    /// <summary>
    /// Logs a user in and returns a token to use on all later requests.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Expects: email and password. Copy the returned token and paste it
    /// into the Authorize button of Swagger to call protected endpoints.
    /// </remarks>
    /// <param name="request">The login credentials.</param>
    /// <response code="200">Login successful. Returns the JWT token and the user object.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var (token, expires) = _tokens.CreateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            User = UserResponse.From(user)
        });
    }
}