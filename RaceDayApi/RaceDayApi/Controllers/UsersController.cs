using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Extensions;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // any logged-in user (Organiser or Participant)
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the profile details of the logged-in user.
    /// </summary>
    /// <remarks>
    /// Requires login (any role). The id in the route must be the id of the logged-in user.
    /// </remarks>
    /// <param name="id">The id of the logged-in user.</param>
    /// <response code="200">The user object.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetProfile(int id)
    {
        if (User.GetUserId() != id)
        {
            return Forbid();
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(UserResponse.From(user));
    }

    /// <summary>
    /// Updates the logged-in user's own profile (name and email).
    /// </summary>
    /// <remarks>
    /// Requires login (any role). Expects: name and email. The role cannot be changed here.
    /// </remarks>
    /// <param name="id">The id of the logged-in user.</param>
    /// <param name="request">The new name and email.</param>
    /// <response code="200">The updated user object.</response>
    /// <response code="400">Invalid data, or the email is already used by another account.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateProfile(int id, UpdateProfileRequest request)
    {
        if (User.GetUserId() != id)
        {
            return Forbid();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id);

        if (user is null)
        {
            return NotFound();
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserID != id))
        {
            return BadRequest(new { message = "Email already in use." });
        }

        user.Name = request.Name.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(UserResponse.From(user));
    }
}