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
        public async Task suggestNickname(IGuildUser target, string nickname)
        {
            votesDAO.SetVote(Context.User.Id, target.Id, nickname);
            await RespondAsync($"{Context.User.Mention} voted for {target.Mention} to be nicknamed {nickname}");
            Dictionary<string, int> nicknames = votesDAO.GetAllSuggestions(target.Id);
            string mostVotesNickname = nicknames.MaxBy(kvp => kvp.Value).Key;
            if (!target.Username.Equals(mostVotesNickname))
            {
                await FollowupAsync($"Most voted username for {target.Mention} is now {nickname}");
                await target.ModifyAsync(user => user.Nickname = mostVotesNickname);
            }
        }

        [SlashCommand("show", "Description")]
        public async Task getNicknameSuggestions(IUser target)
        {
            Dictionary<string, int> nicknames = votesDAO.GetAllSuggestions(target.Id);
            string result = "";
            if (nicknames.Count == 0)
            {
                result = $"Noone has voted for {target.Mention} yet. Use /nickname suggest and be the first!";
            }
            foreach (KeyValuePair<string, int> votes in nicknames)
            {
                result += $"{votes.Key}: {votes.Value}\n";
            }
            await RespondAsync(embed: new EmbedBuilder()
            .WithAuthor("Nickname changer")
            .WithTitle($"Nicknames for {target.GlobalName}")
            .AddField("Votes", result).Build());
        }

        //[RequireRole(423101126171426816)]
        public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
        {

            private readonly VotesDAO votesDAO = new();

            private async Task ToggleBan(IGuildUser target, Boolean banned)
            {
                Dictionary<ulong, string> before = votesDAO.getAllResultsWhereUserVoted(target.Id), after;
                votesDAO.setBannedStatus(target.Id, banned);
                string text = banned ? "B" : "Unb";
                await RespondAsync($"{text}anned {target.Mention} from suggesting nicknames");
                after = votesDAO.getAllResultsWhereUserVoted(target.Id);
                foreach (ulong targetId in before.Keys)
                {
                    if (!after.ContainsKey(targetId) || after[targetId] == null)
                    {
                        await FollowupAsync($"All votes for {target.Mention} have been removed. Changing name to {target.GlobalName}");
                        await target.ModifyAsync(user => user.Nickname = target.GlobalName);
                    }
                    else if (!before[targetId].Equals(after[targetId]))
                    {
                        await FollowupAsync($"Most voted username for {target.Mention} is now {after[targetId]}");
                        await target.ModifyAsync(user => user.Nickname = after[targetId]);
                    }
                }
            }

            [SlashCommand("ban", "Bans the target user from suggesting nicknames")]
            public async Task BanUserFromSuggestions(IGuildUser target)
            {
                await ToggleBan(target, true);
            }

            [SlashCommand("unban", "Unbans the target user from suggesting nicknames and invalidates all their votes")]
            public async Task UnbanUserFromSuggestions(IGuildUser target)
            {
                await ToggleBan(target, false);
            }

            [SlashCommand("invalidate", "Invalidates the suggested nickname")]
            public async Task InvalidateNicknameSuggestion(String nickname)
            {
                votesDAO.setInvalidateStatus(nickname, true);
                await RespondAsync($"Invalidated nickname {nickname}");
            }

            [SlashCommand("uninvalidate", "Invalidates the suggested nickname")]
            public async Task UninvalidateNicknameSuggestion(String nickname)
            {
                votesDAO.setInvalidateStatus(nickname, false);
                await RespondAsync($"Validated nickname {nickname}");
            }

        }

    }
}
