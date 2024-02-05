using System;
using System.Text.Json.Serialization;

using static Web_Lib.DTOs.TvmazeDto;

namespace Web_Lib.DTOs;

public class EpisodeDto
{
    [JsonPropertyName("id")]       public int               Id       { get; set; }
    [JsonPropertyName("url")]      public string?           Url      { get; set; }
    [JsonPropertyName("name")]     public string?           Name     { get; set; }
    [JsonPropertyName("season")]   public int?              Season   { get; set; }
    [JsonPropertyName("number")]   public int?              Number   { get; set; }
    [JsonPropertyName("type")]     public string?           Type     { get; set; }
    [JsonPropertyName("airdate")]  public DateTime?         Airdate  { get; set; }
    [JsonPropertyName("airtime")]  public string?           Airtime  { get; set; }
    [JsonPropertyName("airstamp")] public DateTime?         AirStamp { get; set; }
    [JsonPropertyName("runtime")]  public int?              Runtime  { get; set; }
    [JsonPropertyName("rating")]   public Rating? Rating   { get; set; }
    [JsonPropertyName("image")]    public Image?  Image    { get; set; }
    [JsonPropertyName("summary")]  public string?           Summary  { get; set; }
    [JsonPropertyName("links")]    public Links?  Links    { get; set; }
}
