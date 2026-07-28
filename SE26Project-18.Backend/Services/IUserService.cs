using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(long id);
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto?> UpdateUserAsync(long id, Dictionary<string, object> data);
    Task<List<UserDto>> SearchUsersAsync(string query);
    Task<UserDto?> UpdateUserStatusAsync(long id, UserStatus status);
    Task<UserDto?> ClearUserProfileAsync(long id);
}
