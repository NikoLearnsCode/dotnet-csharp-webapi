namespace dotnet_backend_2.DTOs;

public class CursorPagedList<T>
{
    public List<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}   