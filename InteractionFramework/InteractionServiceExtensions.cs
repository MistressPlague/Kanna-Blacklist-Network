using Discord.Interactions;
using InteractionFramework;
using Kanna_Blacklist_Network;

namespace Discord.Net.Extensions.Interactions;

public static class InteractionServiceExtensions
{
    /// <summary>
    ///    Registers <see cref="InteractionModuleBase{T}"/> modules globally and to guilds set
    ///    with <see cref="GuildModuleAttribute"/>.
    /// </summary>
    /// <param name="service"></param>
    /// <param name="deleteMissing"></param>
    /// <returns></returns>
    public static async Task RegisterCommandsAsync(this InteractionService service, bool deleteMissing = true)
    {
        var AllCommands = service.SlashCommands.ToArray().Cast<ICommandInfo>().Concat(service.ContextCommands).Distinct();

        var GlobalCommands = AllCommands.Where(x => !x.Attributes.Any(attr => attr is RequireGuilds) && !x.Attributes.Any(attr => attr is DontAutoRegisterAttribute));

        var GuildCommands = AllCommands.Where(x => x.Attributes.Any(attr => attr is RequireGuilds) && !x.Attributes.Any(attr => attr is DontAutoRegisterAttribute)).Select(command => (command, command.Attributes.First(attr => attr is RequireGuilds) as RequireGuilds));

        var guildcmds = new Dictionary<ulong, List<ICommandInfo>>();

        foreach (var command in GuildCommands)
        {
            foreach (var guild in command.Item2.GuildsIds)
            {
                if (Program._client.GetGuild(guild) == null)
                {
                    continue;
                }

                if (!guildcmds.ContainsKey(guild))
                {
                    guildcmds[guild] = new List<ICommandInfo>();
                }

                if (!guildcmds[guild].Contains(command.command))
                {
                    guildcmds[guild].Add(command.command);
                }
            }
        }

        var globals = GlobalCommands.Cast<IApplicationCommandInfo>().ToArray();
        await service.AddCommandsGloballyAsync(deleteMissing, globals);
        foreach (var cmd in globals)
        {
            Program.SendLog(LogSeverity.Info, "CMD", $"Registered /{cmd.Name} Globally.");
        }

        foreach (var cmd in guildcmds)
        {
            await service.AddCommandsToGuildAsync(cmd.Key, deleteMissing, cmd.Value.ToArray());

            foreach (var gcmd in cmd.Value)
            {
                Program.SendLog(LogSeverity.Info, "CMD", $"Registered /{gcmd.Name} To Guild: {Program._client.GetGuild(cmd.Key).Name}.");
            }
        }
    }
}