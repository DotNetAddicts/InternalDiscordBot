using Newtonsoft.Json;
using System.Collections.Generic;

namespace DotNetAddicts.InternalDiscordBot.Entities
{
    [JsonObject]
    internal class DynamicSettings
    {
        [JsonProperty("discord_to_github")]
        internal Dictionary<ulong, ulong> DiscordToGitHub { get; set; }
    }
}