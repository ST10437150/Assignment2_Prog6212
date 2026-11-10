using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Extensions;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // any logged-in user (Organiser or Participant)
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
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

    /// <summary>
    /// Uploads (or replaces) the logged-in user's profile picture. The image is stored in Azure Blob Storage.
    /// </summary>
    /// <param name="id">The id of the logged-in user.</param>
    /// <param name="file">The image file (JPG, PNG, GIF or WEBP, up to 5 MB).</param>
    /// <response code="200">The updated user object.</response>
    /// <response code="400">Not a valid image.</response>
    /// <response code="403">The id belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    /// <response code="503">Image storage is unavailable.</response>
    [HttpPost("{id:int}/picture")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UserResponse>> UploadPicture(int id, IFormFile file)
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

        var problem = _blobs.ValidateImage(file);
        if (problem is not null)
        {
            return BadRequest(new { message = problem });
        }

        string url;
        try
        {
            url = await _blobs.UploadImageAsync(file, "profiles");
        }
        catch (StorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }

        var old = user.ProfilePictureUrl;
        user.ProfilePictureUrl = url;
        await _db.SaveChangesAsync();
        await _blobs.DeleteAsync(old);

        return Ok(UserResponse.From(user));
    }
}