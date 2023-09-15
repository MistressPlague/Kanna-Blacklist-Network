using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using InteractionFramework;
using Libraries;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Reflection;
using TextCopy;

namespace Kanna_Blacklist_Network
{
    internal class Program
    {
        public class Configuration
        {
            public List<BlacklistedUser> BlacklistedUsers = new List<BlacklistedUser>();
            public List<ReportedUser> ReportedUsers = new List<ReportedUser>();

            public List<ulong> CommandsPermitted = new List<ulong>();
        }

        public class ConfigEntry
        {
            public string Time;
        }

        public class BlacklistedUser : ConfigEntry
        {
            public ulong UserID;
            public string Reason;
        }

        public class ReportedUser : ConfigEntry
        {
            public ulong UserID;
            public List<Report> ReportedReasons = new List<Report>();
        }

        public class Report
        {
            public string Time;
            public string ReportedBy;
            public string Reason;
        }

        public static string GenerateTimestamp()
        {
            return DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
        }

        public static ConfigLib<Configuration> Config = new ConfigLib<Configuration>(Environment.CurrentDirectory + "\\NewConfig.json"); // File name changed to prevent error on startup due to config type changes

        public static DiscordSocketClient _client;

        private IServiceProvider _services;

        public static async Task Main(string[] args)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            await new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            var cfg = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged,
                AlwaysDownloadUsers = true
            };

            _services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(cfg)
                .BuildServiceProvider();

            // Set Up Bot
            _client = _services.GetRequiredService<DiscordSocketClient>();

            _client.Log += Log;
            _client.Ready += ClientOnReady;
            _client.JoinedGuild += ClientOnJoinedGuild;
            _client.UserJoined += ClientOnUserJoined;

            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            string token;

            if (!File.Exists("Token.txt"))
            {
                SendLog(LogSeverity.Warning, "Startup", "No Token.txt Detected! Please Enter A Discord Bot Token Now.");

                token = ReadHiddenInput();

                while (string.IsNullOrWhiteSpace(token) || token.Length < 16)
                {
                    SendLog(LogSeverity.Warning, "Startup", "Invalid Token. Please Enter A Valid Token.");

                    token = ReadHiddenInput();
                }

                await File.WriteAllTextAsync("Token.txt", token);
            }
            else
            {
                token = await File.ReadAllTextAsync("Token.txt");
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(Timeout.Infinite);
        }

        static string ReadHiddenInput()
        {
            var input = "";

            var clip = new Clipboard();

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input.Remove(input.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    input += keyInfo.KeyChar;
                    Console.Write('*');
                }
                else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && keyInfo.Key == ConsoleKey.V)
                {
                    // Handle Ctrl+V for paste
                    var clipboardText = clip.GetText();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        input += clipboardText;
                        Console.Write(new string('*', clipboardText.Length));
                    }
                }
            } while (true);

            Console.WriteLine(); // Move to the next line

            return input;
        }

        private async Task ClientOnUserJoined(SocketGuildUser arg)
        {
            if (Config.InternalConfig.BlacklistedUsers.FirstOrDefault(o => o.UserID == arg.Id) is var user && user != null)
            {
                try
                {
                    await arg.Guild.AddBanAsync(arg.Id, 1, $"Banned Via Kanna Blacklist Network ({Program._client.CurrentUser.Username}) By Bot Owner. - {user.Reason}");
                }
                catch // Ignore any and all errors, blindly.
                {
                }
            }
        }

        private async Task ClientOnJoinedGuild(SocketGuild guild)
        {
            foreach (var user in Config.InternalConfig.BlacklistedUsers)
            {
                try
                {
                    if (guild.GetUser(user.UserID) is var founduser && founduser != null && founduser.GuildPermissions.Has(GuildPermission.Administrator))
                    {
                        continue; // Ignore blacklisted user if admin, as we likely can't ban them anyway. We also can't assume the morals of said server in this rare case.
                    }

                    await guild.AddBanAsync(user.UserID, 1, $"Banned Via Kanna Blacklist Network ({Program._client.CurrentUser.Username}) By Bot Owner. - {user.Reason}");
                }
                catch // Ignore any and all errors, blindly.
                {
                }
            }
        }

        private static async Task ClientOnReady()
        {
            Console.Clear();

            Console.Title = $"Kanna Blacklist Network: {_client.CurrentUser.GlobalName ?? _client.CurrentUser.Username}";

            SendLog(LogSeverity.Info, "Ready", $"Welcome To Kanna Blacklist Network - {_client.CurrentUser.GlobalName ?? _client.CurrentUser.Username}, You Currently Have {Config.InternalConfig.BlacklistedUsers.Count} Blacklisted Users.");

            SendLog(LogSeverity.Info, "Ready", $"Your Instance Is In {_client.Guilds.Count} Servers: {string.Join(", ", _client.Guilds.Select(o => o.Name))}");

            Console.WriteLine("Created By Kanna. Donate: https://paypal.me/KannaVR");

            await _client.SetCustomStatusAsync("Kanna Blacklist Network");
        }

        private static async Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
            }

            Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff tt}] [{msg.Source}]: {msg.Message}{msg.Exception}");

            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void SendLog(LogSeverity Severity, string Source, string Text)
        {
            Log(new LogMessage(Severity, Source, Text));
        }
    }
}