using System;

namespace DotNetAddicts.InternalDiscordBot.Components
{
    internal static class GitHubApi
    {
        internal static Uri BaseUrl { get; } = new Uri("https://api.github.com/", UriKind.Absolute);

        internal static Uri UserUrl { get; } = new Uri("user", UriKind.Relative);
    }
}