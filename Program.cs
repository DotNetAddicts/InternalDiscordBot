using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetAddicts.InternalDiscordBot
{
    using Components;
    using Entities;

    internal class Program
    {
        internal const string DynamicSettingsJson = "dynamic.json";

        internal const string StaticSettingsJson = "static.json";

        internal static SocketRole AuthorizedRole { get; private set; }

        internal static SocketGuild CurrentGuild { get; private set; }

        internal static DiscordSocketClient DiscordClient { get; private set; }

        internal static DynamicSettings DynamicSettings { get; private set; }

        internal static HttpClient GitHubClient { get; private set; }

        internal static StaticSettings StaticSettings { get; private set; }

        internal static async Task Main(string[] args)
        {
            if (File.Exists(DynamicSettingsJson))
            using (var reader = File.OpenText(DynamicSettingsJson))
            {
                DynamicSettings = JsonConvert.DeserializeObject<DynamicSettings>(await reader.ReadToEndAsync());
            }
            else
            {
                DynamicSettings = new DynamicSettings()
                {
                    DiscordToGitHub = new Dictionary<ulong, ulong>()
                };
                await File.WriteAllTextAsync(DynamicSettingsJson, JsonConvert.SerializeObject(DynamicSettings, Formatting.Indented));
            }

            if (File.Exists(StaticSettingsJson))
            using (var reader = File.OpenText(StaticSettingsJson))
            {
                StaticSettings = JsonConvert.DeserializeObject<StaticSettings>(await reader.ReadToEndAsync());
            }
            else
            {
                StaticSettings = new StaticSettings();
                await File.WriteAllTextAsync(StaticSettingsJson, JsonConvert.SerializeObject(StaticSettings, Formatting.Indented));
                return;
            }

            GitHubClient = new HttpClient()
            {
                BaseAddress = GitHubApi.BaseUrl,
                Timeout = Timeout.InfiniteTimeSpan
            };

            DiscordClient = new DiscordSocketClient();
            DiscordClient.MessageReceived += OnMessageReceivedAsync;
            DiscordClient.Ready += OnReadyAsync;
            await DiscordClient.LoginAsync(TokenType.Bot, StaticSettings.DiscordToken);
            await DiscordClient.StartAsync();

            await Task.Delay(-1);
        }

        internal static bool IsValidTokenFormat(string token)
        {
            if (token.Length != 40) return false;
            for (int i = 0; i < 40; i++)
            {
                var c = token[i];
                if ((('0' > c) || (c > '9')) && (('a' > c) || (c > 'f'))) return false;
            }
            return true;
        }

        internal static async Task OnMessageReceivedAsync(SocketMessage message)
        {
            try
            {
                var target = CurrentGuild.Users.FirstOrDefault(predicate => predicate.Id == message.Author.Id);
                var context = message.Content.Trim().ToLower();
                if (target != null && message.Channel.Id == StaticSettings.VerificationChannel && IsValidTokenFormat(context))
                {
                    await message.DeleteAsync();
                    GitHubClient.DefaultRequestHeaders.Accept.Clear();
                    GitHubClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                    GitHubClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", context);
                    GitHubClient.DefaultRequestHeaders.UserAgent.Clear();
                    GitHubClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DotNetAddicts.InternalDiscordBot", "1.0.0"));
                    using (var response = await GitHubClient.GetAsync(GitHubApi.UserUrl))
                    using (var content = response.Content)
                    if (response.IsSuccessStatusCode)
                    {
                        var user = JsonConvert.DeserializeObject<GitHubUser>(await content.ReadAsStringAsync());
                        var isUpdate = DynamicSettings.DiscordToGitHub.Remove(message.Author.Id);
                        DynamicSettings.DiscordToGitHub.Add(message.Author.Id, user.Id);
                        await File.WriteAllTextAsync(DynamicSettingsJson, JsonConvert.SerializeObject(DynamicSettings, Formatting.Indented));
                        await message.Channel.SendMessageAsync(string.Empty, embed: new EmbedBuilder()
                        {
                            Title = "認証成功",
                            Description = $"{message.Author.Username} を `{user.Login}` と紐付けました",
                            Url = user.Url,
                            Timestamp = DateTimeOffset.UtcNow,
                            Color = Colors.Green,
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = "GitHub",
                                IconUrl = Assets.FluidIcon
                            },
                            ThumbnailUrl = user.AvatarUrl,
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = message.Author.Username,
                                IconUrl = message.Author.GetAvatarUrl()
                            }
                        });
                        await target.AddRoleAsync(AuthorizedRole);
                    }
                    else
                    {
                        var result = await content.ReadAsStringAsync();
                        var error = JsonConvert.DeserializeObject<Error>(result);
                        await message.Channel.SendMessageAsync(string.Empty, embed: new EmbedBuilder()
                        {
                            Title = "認証失敗",
                            Description = "認証に失敗しました",
                            Timestamp = DateTimeOffset.UtcNow,
                            Color = Colors.Red,
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = "GitHub",
                                IconUrl = Assets.FluidIcon
                            },
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = message.Author.Username,
                                IconUrl = message.Author.GetAvatarUrl()
                            },
                            Fields = new List<EmbedFieldBuilder>()
                            {
                                new EmbedFieldBuilder()
                                {
                                    Name = "Status",
                                    Value = response.StatusCode,
                                    IsInline = true
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await message.Channel.SendMessageAsync(string.Empty, embed: new EmbedBuilder()
                {
                    Title = "認証失敗",
                    Description = "予期せぬエラーが発生しました",
                    Timestamp = DateTimeOffset.UtcNow,
                    Color = Colors.Red,
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = DiscordClient.CurrentUser.Username,
                        IconUrl = DiscordClient.CurrentUser.GetAvatarUrl()
                    },
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = message.Author.Username,
                        IconUrl = message.Author.GetAvatarUrl()
                    },
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Status",
                            Value = ex.HResult.ToString("X8"),
                            IsInline = true
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Source",
                            Value = ex.Source,
                            IsInline = true
                        }
                    }
                });
            }
        }

        public static Task OnReadyAsync()
        {
            CurrentGuild = DiscordClient.GetGuild(StaticSettings.CurrentGuild);
            AuthorizedRole = CurrentGuild.Roles.FirstOrDefault(predicate => predicate.Id == StaticSettings.AuthorizedRole);
            return Task.CompletedTask;
        }
    }
}
