using Newtonsoft.Json;

namespace DotNetAddicts.InternalDiscordBot.Entities
{
    [JsonObject]
    internal class Error
    {
        [JsonProperty("message")]
        internal string Message { get; set; }

        [JsonProperty("documentation_url")]
        internal string DocumentationUrl { get; set; }
    }
}