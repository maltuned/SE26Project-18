namespace SE26Project_18.Api.Dtos.Response;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages => PageSize > 0
        ? Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize))
        : 0;

    public bool HasPrev => Page > 1;

    public bool HasNext => Page < TotalPages;

    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
