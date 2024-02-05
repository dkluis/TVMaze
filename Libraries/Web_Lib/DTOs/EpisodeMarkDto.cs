using System;
using System.Text.Json.Serialization;

namespace Web_Lib.DTOs;

public class EpisodeMarkDto
{
    [JsonPropertyName("episode_id")] public int  EpisodeId { get; set; }
    [JsonPropertyName("type")]       public int? Type      { get; set; }
    [JsonPropertyName("marked_at")]  public int? MarkedAt  { get; set; }
}
