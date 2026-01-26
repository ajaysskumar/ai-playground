using System.Collections.Generic;

namespace AIDemos.Models
{
    public class MovieDto
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public string Category { get; set; } // TV show or Movie
        public List<string> Directors { get; set; }
        public List<string> Actors { get; set; }
        public string Plot { get; set; }
        public string Genre { get; set; }
        public string Rating { get; set; }
    }
}