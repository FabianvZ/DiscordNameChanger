using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNameChanger
{
    [Group("nickname", "Commands for controlling nicknames")]
    public class Commands : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly VotesDAO votesDAO = new();

        [SlashCommand("suggest", "Suggests a nickname for a user. Changes their name if it has the most votes")]
        public async Task suggestNickname(IUser target, string nickname)
        {

        }

        [SlashCommand("show", "Description")]
        public async Task getNicknameSuggestions(IUser target)
        {

            // EMBED
        }

        [RequireRole(423101126171426816)]
        public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
        {

            [SlashCommand("ban", "Bans the target user from suggesting nicknames")]
            public async Task BanUserFromSuggestions(IUser target)
            {

            }

            [SlashCommand("unban", "Unbans the target user from suggesting nicknames")]
            public async Task UnbanUserFromSuggestions(IUser target)
            {

            }

            [SlashCommand("invalidate", "Invalidates the suggested nickname")]
            public async Task InvalidateNicknameSuggestion(String nickname)
            {

            }

        }

    }
}
