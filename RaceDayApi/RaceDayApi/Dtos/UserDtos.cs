using System.ComponentModel.DataAnnotations;
using RaceDayApi.Models;

namespace RaceDayApi.Dtos;

/// <summary>User details returned by the API. The password hash is never included.</summary>
public class UserResponse
{
    public int UserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public static UserResponse From(User user) => new()
    {
        UserID = user.UserID,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role
    };
}

/// <summary>Data a user can change on their own profile. The role cannot be changed.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}