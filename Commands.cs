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
            await FollowupAsync($"nickname: {target.Nickname}, globalName: {target.DisplayName}, username: {target.Username}");
            if (nicknames.Count > 0)
            {
                string mostVotesNickname = nicknames.MaxBy(kvp => kvp.Value).Key;
                if (!target.Username.Equals(mostVotesNickname))
                {
                    await FollowupAsync($"Most voted username for {target.Mention} is now {nickname}");
                    if (target.Hierarchy <= Context.Guild.CurrentUser.Hierarchy)
                    { 
                        await target.ModifyAsync(u => u.Nickname = nickname);
                    }
                    else
                    {
                        await FollowupAsync($"Could not set nickname for {target.Mention}");
                    }
                }
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
                votesDAO.setBannedStatus(target.Id, banned);
                string text = banned ? "B" : "Unb";
                await RespondAsync($"{text}anned {target.Mention} from suggesting nicknames");
                Dictionary<ulong, string> targets = votesDAO.getAllResultsWhereUserVoted(target.Id);
                foreach (ulong targetId in targets.Keys)
                {
                    IGuildUser votedUser = Context.Guild.GetUser(targetId);
                    if (targets[targetId].Length == 0)
                    {
                        await FollowupAsync($"All votes for {votedUser.Mention} have been removed. Changing name to {votedUser.Username}");
                        targets[targetId] = votedUser.Username;
                    }
                    else if (!votedUser.DisplayName.Equals(targets[targetId]))
                    {
                        await FollowupAsync($"Most voted username for {votedUser.Mention} is now {targets[targetId]}");
                    }
                    if (votedUser.Hierarchy <= Context.Guild.CurrentUser.Hierarchy)
                    { 
                        await votedUser.ModifyAsync(u => u.Nickname = targets[targetId]);
                    }
                    else
                    {
                        await FollowupAsync($"Could not set nickname for {votedUser.Mention}");
                    }
                }
            }

            [SlashCommand("ban", "Bans the target user from suggesting nicknames and unvalidates all their votes")]
            public async Task BanUserFromSuggestions(IGuildUser target)
            {
                await ToggleBan(target, true);
            }

            [SlashCommand("unban", "Unbans the target user from suggesting nicknames and validates all their votes")]
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

            [SlashCommand("validate", "Validates the suggested nickname")]
            public async Task UninvalidateNicknameSuggestion(String nickname)
            {
                votesDAO.setInvalidateStatus(nickname, false);
                await RespondAsync($"Validated nickname {nickname}");
            }

        }

    }
}
