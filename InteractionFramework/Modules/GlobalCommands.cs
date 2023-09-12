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

        [SlashCommand("reportuser", "Report a user to the owner of this bot. Whether they get blacklisted is up to the bot owner only.", false, RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReportUser(ulong userid, string reason)
        {
            await RespondAsync("Sent!", ephemeral: true);

            Program.SendLog(LogSeverity.Warning, "Reported User", $"{Context.User} Sent A Report For UserID: {userid}: {reason}");
        }

        [SlashCommand("blacklistuser", "Blacklists a user on your Kanna Blacklist Network. This will effect all servers this is in.", false, RunMode.Async)]
        [Attributes.RequireOwner]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task BlacklistUser()
        {
            await RespondAsync("Please proceed in the console.", ephemeral: true);

            Program.SendLog(LogSeverity.Warning, "BlacklistUser", "You Just Triggered The BlacklistUser Command. Enter The UserID To Blacklist Now.");
            var userid = ulong.Parse(Console.ReadLine());

            if (!Program.Config.InternalConfig.BlacklistedUsers.Contains(userid))
            {
                Program.Config.InternalConfig.BlacklistedUsers.Add(userid);
            }

            foreach (var guild in Program._client.Guilds)
            {
                try
                {
                    if (guild.GetUser(userid) is var user && user != null && user.GuildPermissions.Has(GuildPermission.Administrator))
                    {
                        continue; // Ignore blacklisted user if admin, as we likely can't ban them anyway. We also can't assume the morals of said server in this rare case.
                    }

                    await guild.AddBanAsync(userid, 1, "Banned Via Kanna Blacklist Network By Bot Owner.");
                }
                catch // Ignore any and all errors, blindly.
                {
                }
            }

            Program.SendLog(LogSeverity.Warning, "BlacklistUser", "Done!");
        }
    }
}