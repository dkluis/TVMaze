using System;
using System.Text.Json.Serialization;

using static Web_Lib.DTOs.TvmazeDto;

namespace Web_Lib.DTOs;

public class ShowDto
{
    [JsonPropertyName("id")]             public int         Id             { get; set; }
    [JsonPropertyName("url")]            public string?     Url            { get; set; }
    [JsonPropertyName("name")]           public string?     Name           { get; set; }
    [JsonPropertyName("type")]           public string?     Type           { get; set; }
    [JsonPropertyName("language")]       public string?     Language       { get; set; }
    [JsonPropertyName("genres")]         public string[]?   Genres         { get; set; }
    [JsonPropertyName("status")]         public string?     Status         { get; set; }
    [JsonPropertyName("runtime")]        public int?        Runtime        { get; set; }
    [JsonPropertyName("averageRuntime")] public int?        AverageRuntime { get; set; }
    [JsonPropertyName("premiered")]      public DateTime?   Premiered      { get; set; }
    [JsonPropertyName("ended")]          public DateTime?   Ended          { get; set; }
    [JsonPropertyName("officialSite")]   public string?     OfficialSite   { get; set; }
    [JsonPropertyName("schedule")]       public Schedule?   Schedule       { get; set; }
    [JsonPropertyName("rating")]         public Rating?     Rating         { get; set; }
    [JsonPropertyName("weight")]         public int?        Weight         { get; set; }
    [JsonPropertyName("network")]        public Network?    Network        { get; set; }
    [JsonPropertyName("webChannel")]     public WebChannel? WebChannel     { get; set; }
    [JsonPropertyName("dvdCountry")]     public Country?    DvdCountry     { get; set; }
    [JsonPropertyName("externals")]      public Externals?  Externals      { get; set; }
    [JsonPropertyName("image")]          public Image?      Image          { get; set; }
    [JsonPropertyName("summary")]        public string?     Summary        { get; set; }
    [JsonPropertyName("updated")]        public int?        Updated        { get; set; }
    [JsonPropertyName("links")]          public Links?      Links          { get; set; }
}
