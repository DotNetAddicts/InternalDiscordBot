using Newtonsoft.Json;

namespace DotNetAddicts.InternalDiscordBot.Entities
{
    [JsonObject]
    internal class StaticSettings
    {
        [JsonProperty("authorized_role")]
        internal ulong AuthorizedRole { get; set; }

        [JsonProperty("current_guild")]
        internal ulong CurrentGuild { get; set; }

        [JsonProperty("discord_token")]
        internal string DiscordToken { get; set; }

        [JsonProperty("verification_channel")]
        internal ulong VerificationChannel { get; set; }
    }
}