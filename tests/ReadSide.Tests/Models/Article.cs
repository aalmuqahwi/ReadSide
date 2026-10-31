namespace ReadSide.Tests.Models;

public class Article
{
    public int Id { get; set; }
    public string? Headline { get; set; }

    // Computed in C#. It is not a column in the database.
    public string Label => $"#{Id} {Headline}";
}
