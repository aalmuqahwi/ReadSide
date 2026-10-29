namespace ReadSide.Tests.Models;

public class Book
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? ReturnedAt { get; set; }
}