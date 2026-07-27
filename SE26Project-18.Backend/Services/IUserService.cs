using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(long id);
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto?> UpdateUserAsync(long id, Dictionary<string, object> data);
}