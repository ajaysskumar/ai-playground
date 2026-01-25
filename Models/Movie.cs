namespace AIDemos.Models;

public class Movie
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? ReleaseDate { get; set; }
    public List<string> Directors { get; set; } = new();
    public List<string> Actors { get; set; } = new();
}

public class MovieDetailsResponse
{
    public List<Movie> Movies { get; set; } = new();
}