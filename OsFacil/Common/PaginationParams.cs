namespace OsFacil.Common;

public class PaginationParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 1 : value;
    }

    public string? OrderBy { get; set; }
    public string OrderDir { get; set; } = "asc";
    public string? Filter { get; set; }
}
