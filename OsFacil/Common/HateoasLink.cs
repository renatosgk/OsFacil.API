namespace OsFacil.Common;

public record HateoasLink(string Href, string Rel, string Method);

public class HateoasResponse<T>
{
    public T Data { get; set; } = default!;
    public List<HateoasLink> Links { get; set; } = new();

    public HateoasResponse() { }

    public HateoasResponse(T data) => Data = data;

    public HateoasResponse<T> AddLink(string href, string rel, string method = "GET")
    {
        Links.Add(new HateoasLink(href, rel, method));
        return this;
    }
}
