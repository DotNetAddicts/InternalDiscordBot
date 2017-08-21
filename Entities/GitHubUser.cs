using Newtonsoft.Json;

namespace DotNetAddicts.InternalDiscordBot.Entities
{
    [JsonObject]
    internal class GitHubUser
    {
        [JsonProperty("login")]
        internal string Login { get; set; }
        
        [JsonProperty("id")]
        internal ulong Id { get; set; }
        
        [JsonProperty("avatar_url")]
        internal string AvatarUrl { get; set; }
        
        [JsonProperty("url")]
        internal string Url { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }
    }
}