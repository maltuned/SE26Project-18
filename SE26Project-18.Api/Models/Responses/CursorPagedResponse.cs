namespace SE26Project_18.Api.Models.Responses;

public sealed record CursorPagedResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore
);
