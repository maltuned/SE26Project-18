using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IUserService
{
    Task<UserDto?> LoginAsync(string username, string password);
    Task<UserDto?> GetUserByIdAsync(long id);
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto> RegisterAsync(string username, string password);
    Task<UserDto?> UpdateUserAsync(long id, Dictionary<string, object> data);
}
