using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IResponseService
{
    Task<ResponseResponse> CreateAsync(long userId, CreateResponseRequest request);

    Task<ResponseResponse> GetByIdAsync(long responseId, long userId);

    Task<ResponseResponse> AcceptAsync(long responseId, long recruiterId);

    Task<ResponseResponse> RejectAsync(long responseId, long recruiterId);
}
