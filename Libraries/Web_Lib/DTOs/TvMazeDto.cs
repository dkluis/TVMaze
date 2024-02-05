using System.Text.Json.Serialization;

namespace Web_Lib.DTOs;

public class TvmazeDto
{
    public class Rating
    {
        [JsonPropertyName("average")] public double? Average { get; set; }
    }

    public class Image
    {
        [JsonPropertyName("medium")]   public string? Medium   { get; set; }
        [JsonPropertyName("original")] public string? Original { get; set; }
    }

    public class Links
    {
        [JsonPropertyName("self")] public Link? Self { get; set; }
        [JsonPropertyName("show")] public Link? Show { get; set; }
    }

    public class Link
    {
        [JsonPropertyName("href")] public string? Href { get; set; }
    }

    public class Schedule
    {
        [JsonPropertyName("time")] public string?   Time { get; set; }
        [JsonPropertyName("days")] public string[]? Days { get; set; }
    }

    public class WebChannel
    {
        [JsonPropertyName("id")]           public int?     Id           { get; set; }
        [JsonPropertyName("name")]         public string?  Name         { get; set; }
        [JsonPropertyName("country")]      public Country? Country      { get; set; }
        [JsonPropertyName("officialSite")] public string?  OfficialSite { get; set; }
    }

    public class Country
    {
        [JsonPropertyName("name")]     public string? Name     { get; set; }
        [JsonPropertyName("code")]     public string? Code     { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
    }

    public class Externals
    {
        [JsonPropertyName("tvrage")]  public object? TvRage  { get; set; }
        [JsonPropertyName("thetvdb")] public int?    TheTvDb { get; set; }
        [JsonPropertyName("imdb")]    public string? Imdb    { get; set; }
    }

    public class Network
    {
        [JsonPropertyName("id")]           public int      Id           { get; set; }
        [JsonPropertyName("name")]         public string?  Name         { get; set; }
        [JsonPropertyName("country")]      public Country? Country      { get; set; }
        [JsonPropertyName("officialSite")] public string?  OfficialSite { get; set; }
    }
}
