namespace UrlShortenerWithRedis.Models
{
    public class UrlShortenerViewModel
    {
        public string? Url { get; set; }
        public string? ShortenedUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string>? UrlKvps { get; set; }
    }
}
