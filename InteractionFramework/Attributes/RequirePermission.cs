using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanna_Blacklist_Network;

namespace InteractionFramework.Attributes
{
    public class RequirePermission : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            switch (context.Client.TokenType)
            {
                case TokenType.Bot:
                    if (!Program.Config.InternalConfig.CommandsPermitted.Contains(context.User.Id))
                        return PreconditionResult.FromError(ErrorMessage ?? "Command can only be run by permitted users.");
                    return PreconditionResult.FromSuccess();
                default:
                    return PreconditionResult.FromError($"{nameof(RequireOwnerAttribute)} is not supported by this {nameof(TokenType)}.");
            }
        }
    }
}
