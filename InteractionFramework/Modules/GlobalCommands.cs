using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.Net.Extensions.Interactions;
using InteractionFramework.Attributes;
using Kanna_Blacklist_Network;

namespace InteractionFramework.Modules
{
    // Interaction modules must be public and inherit from an IInteractionModuleBase
    public class GlobalCommands : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private InteractionHandler _handler;

        // Constructor injection is also a valid way to access the dependencies
        public GlobalCommands(InteractionHandler handler)
        {
            _handler = handler;
        }

        // You can use a number of parameter types in you Slash Command handlers (string, int, double, bool, IUser, IChannel, IMentionable, IRole, Enums) by default. Optionally,
        // you can implement your own TypeConverters to support a wider range of parameter types. For more information, refer to the library documentation.
        // Optional method parameters(parameters with a default value) also will be displayed as optional on Discord.

        // [Summary] lets you customize the name and the description of a parameter
        [SlashCommand("help", "Shows all commands supported by this bot.", false, RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task Help()
        {
            var Commands = new List<string>();

            foreach (var GuildCommand in InteractionHandler._handler.SlashCommands.Where(o => (o.Module.Attributes.FirstOrDefault(a => a is RequireGuilds) as RequireGuilds)?.GuildsIds.Contains(Context.Guild.Id) ?? false))
            {
                Commands.Add($"[Guild] `/{GuildCommand.Name}`: {GuildCommand.Description}");
            }

            foreach (var GlobalCommand in InteractionHandler._handler.SlashCommands.Where(o => o.Module.Attributes.All(a => a is not RequireGuilds)))
            {
                Commands.Add($"[Global] `/{GlobalCommand.Name}`: {GlobalCommand.Description}");
            }

            await RespondAsync(string.Join("\r\n", Commands), ephemeral: true);
        }

        [SlashCommand("donate", "Donation Link", false, RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task Donate()
        {
            await RespondAsync("To Donate To The Developer Of This Bot, Use https://paypal.me/KannaVR - Note Only \"Friends And Family\" Payments Are Accepted. Goods And Services Are Auto-Refunded Within 24h.");
        }

        [SlashCommand("permittousecommands", "Permits a specified user ID to use commands such as isblacklisted.", false, RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [Attributes.RequireOwner]
        public async Task PermitToUseCommands(ulong userid)
        {
            if (!Program.Config.InternalConfig.CommandsPermitted.Contains(userid))
            {
                Program.Config.InternalConfig.CommandsPermitted.Add(userid);

                await RespondAsync("Permission Given!", ephemeral: true);
            }
            else
            {
                Program.Config.InternalConfig.CommandsPermitted.Remove(userid);

                await RespondAsync("Permission Removed!", ephemeral: true);
            }
        }

        [SlashCommand("isblacklisted", "Checks if a specified user ID is blacklisted.", false, RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [Attributes.RequirePermission]
        public async Task IsBlacklisted(ulong userid)
        {
            var match = Program.Config.InternalConfig.BlacklistedUsers.FirstOrDefault(o => o.UserID == userid);

            if (match == null)
            {
                var report = Program.Config.InternalConfig.ReportedUsers.FirstOrDefault(o => o.UserID == userid);
                
                if (report != null)
                {
                    await RespondAsync($"Nope! Not Blacklisted, But They Were Reported! - Time First Reported: {report.Time}, Reports: {string.Join("\r\n", report.ReportedReasons.Select(a => $"[{a.Time} {a.ReportedBy}: {a.Reason}]"))}", ephemeral: true);
                }
                else
                {
                    await RespondAsync("Nope! Not Blacklisted Or Reported!", ephemeral: true);
                }
            }
            else
            {
                await RespondAsync($"Yep! They Are Blacklisted! - Time: {match.Time}, Reason: {match.Reason}", ephemeral: true);
            }
        }

        [SlashCommand("reportuser", "Report a user to the owner of this bot. Whether they get blacklisted is up to the bot owner only.", false, RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task ReportUser(ulong userid, string reason)
        {
            await RespondAsync("Sent!", ephemeral: true);

            Program.SendLog(LogSeverity.Warning, "Reported User", $"{Context.User} Sent A Report For UserID: {userid}: {reason}");

            if (Program.Config.InternalConfig.ReportedUsers.FindIndex(o => o.UserID == userid) is var index && index != -1)
            {
                if (Program.Config.InternalConfig.ReportedUsers[index].ReportedReasons.All(p => p.Reason != reason))
                {
                    Program.Config.InternalConfig.ReportedUsers[index].ReportedReasons.Add(new Program.Report() { Time = Program.GenerateTimestamp(), ReportedBy = Context.User.ToString(), Reason = reason });
                }
            }
            else
            {
                Program.Config.InternalConfig.ReportedUsers.Add(new Program.ReportedUser() { Time = Program.GenerateTimestamp(), UserID = userid, ReportedReasons = new List<Program.Report> { new Program.Report() { Time = Program.GenerateTimestamp(), ReportedBy = Context.User.ToString(), Reason = reason } } });
            }
        }

        [SlashCommand("blacklistuser", "Blacklists a user on your Kanna Blacklist Network. This will effect all servers this is in.", false, RunMode.Async)]
        [Attributes.RequireOwner]
        [RequireContext(ContextType.Guild)]
        public async Task BlacklistUser()
        {
            await RespondAsync("Please proceed in the console.", ephemeral: true);
            
            Retry:
            Program.SendLog(LogSeverity.Warning, "BlacklistUser", "You Just Triggered The BlacklistUser Command. Enter The UserIDs To Blacklist Now. IDs Can Be Serparated With \";\", With Reasons After A \":\"; For Example DiscordID1;DiscordID2 Works, So Would DiscordID1:reason here;DiscordID2:other reason here.");

            var text = Console.ReadLine();

            if (!Regex.IsMatch(text, @"^\d{18,}(?::(\w| )+)?(?:;\d+(?::(\w| )+)?)*$"))
            {
                Program.SendLog(LogSeverity.Error, "BlacklistUser", "Please Type Using The Correct Format.");
                goto Retry;
            }

            var splits = text.Split(";");

            var users = new List<(ulong, string)>();

            foreach (var entry in splits)
            {
                var split = entry.Split(':');

                var id = ulong.Parse(split[0]);

                var reason = "";

                if (split.Length > 1)
                {
                    reason = split[1];
                }

                users.Add((id, reason));
            }

            // Accepts id, id, id or id:reasonhere,id:reasonhere

            foreach (var user in users)
            {
                if (Program.Config.InternalConfig.BlacklistedUsers.All(p => p.UserID != user.Item1))
                {
                    Program.Config.InternalConfig.BlacklistedUsers.Add(new Program.BlacklistedUser() { Time = Program.GenerateTimestamp(), UserID = user.Item1, Reason = user.Item2});
                }

                foreach (var guild in Program._client.Guilds)
                {
                    try
                    {
                        if (guild.GetUser(user.Item1) is var founduser && founduser != null && founduser.GuildPermissions.Has(GuildPermission.Administrator))
                        {
                            continue; // Ignore blacklisted user if admin, as we likely can't ban them anyway. We also can't assume the morals of said server in this rare case.
                        }

                        await guild.AddBanAsync(user.Item1, 1, $"Banned Via Kanna Blacklist Network ({Program._client.CurrentUser.Username}) By Bot Owner. - {user.Item2}");
                    }
                    catch // Ignore any and all errors, blindly.
                    {
                    }
                }
            }

            Program.SendLog(LogSeverity.Warning, "BlacklistUser", "Done!");
        }
    }
}