using Discord;
using Discord.Interactions;
using Discord.Net.Extensions.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace InteractionFramework.Modules
{
    // Interaction modules must be public and inherit from an IInteractionModuleBase
    public class GuildCommands : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private InteractionHandler _handler;

        // Constructor injection is also a valid way to access the dependencies
        public GuildCommands(InteractionHandler handler)
        {
            _handler = handler;
        }
    }
}
