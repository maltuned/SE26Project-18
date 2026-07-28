using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public interface IAdminService
{
    Task<(string Token, Admin Admin)> LoginAsync(string username, string password);
    Task<int[]> GetPendingCountAsync();
}
