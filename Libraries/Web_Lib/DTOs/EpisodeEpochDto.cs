using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Web_Lib.DTOs;

public class ShowEpochDto
{
    [JsonExtensionData] public Dictionary<string, long>? ShowEpoch { get; set; }
}
