namespace PoolHub.Shared;

public class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

    [System.Text.Json.Serialization.JsonIgnore]
    public int TotalCount
    {
        get => TotalItems;
        set => TotalItems = value;
    }
}
