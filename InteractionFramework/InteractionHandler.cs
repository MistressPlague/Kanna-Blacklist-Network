using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Discord.Net.Extensions.Interactions;
using Kanna_Blacklist_Network;

namespace InteractionFramework
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        public static InteractionService _handler;
        private readonly IServiceProvider _services;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services)
        {
            _client = client;
            _handler = handler;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            // Process when the client is ready, so we can register our commands.
            _client.Ready += ReadyAsync;

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
        }

        private async Task ReadyAsync()
        {
            // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
            await _handler.RegisterCommandsAsync(true);
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                if (interaction is IModalInteraction)
                {
                    return;
                }

                // Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    switch (result.Error) // in case of implementing specific ones in the future.
                    {
                        default:
                            Program.SendLog(LogSeverity.Error, "CMD", ($"{result.Error} {result.ErrorReason}"));
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
