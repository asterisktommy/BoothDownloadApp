using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BoothDownloadApp.Core;

public class BoothLibrary
{
    [JsonPropertyName("library")]
    public List<BoothItem> Library { get; set; } = new();
}
