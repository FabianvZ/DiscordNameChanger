using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DiscordNameChanger
{
    [Group("nickname", "Commands for controlling nicknames")]
    public class Commands : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly VotesDAO votesDAO = new();

        [SlashCommand("suggest", "Suggests a nickname for a user. Changes their name if it has the most votes")]
        public async Task SuggestNickname(IGuildUser target, string nickname)
        {
            votesDAO.SetVote(Context.User.Id, target.Id, nickname);
            await RespondAsync($"{Context.User.Mention} voted for {target.Mention} to be nicknamed {nickname}");
            Dictionary<string, int> nicknames = votesDAO.GetAllSuggestions(target.Id);
            //await FollowupAsync($"nickname: {target.Nickname}, globalName: {target.DisplayName}, username: {target.Username}");
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
        public async Task GetNicknameSuggestions(IUser target)
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

        [DefaultMemberPermissions(GuildPermission.BanMembers)]
        public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
        {

            private readonly VotesDAO votesDAO = new();

            private async Task CheckUsernamesWhereUserVoted(IGuildUser voter)
            {
                Dictionary<ulong, string> targets = votesDAO.GetAllResultsWhereUserVoted(voter.Id);
                foreach (ulong targetId in targets.Keys)
                {
                    await CheckUser(targetId, targets[targetId]);
                }
            }

            private async Task CheckUsernamesWithUsername(string username)
            {
                Dictionary<ulong, string> targets = votesDAO.GetAllResultsWhereUserHasNicknameVoted(username);
                foreach (ulong targetId in targets.Keys)
                {
                    await CheckUser(targetId, targets[targetId]);
                }
            }

            private async Task CheckUser(ulong targetId, string nickname)
            {
                SocketGuildUser votedUser = Context.Guild.GetUser(targetId);
                if (nickname.Length == 0)
                {
                    await FollowupAsync($"All votes for {votedUser.Mention} have been removed. Changing name to {votedUser.Username}");
                    nickname = votedUser.Username;
                }
                else if (!votedUser.DisplayName.Equals(nickname))
                {
                    await FollowupAsync($"Most voted username for {votedUser.Mention} is now {nickname}");
                }
                if (votedUser.Hierarchy <= Context.Guild.CurrentUser.Hierarchy)
                {
                    await votedUser.ModifyAsync(u => u.Nickname = nickname);
                }
                else
                {
                    await FollowupAsync($"Could not set nickname for {votedUser.Mention}");
                }
            }

            [SlashCommand("ban", "Bans the target user from suggesting nicknames and unvalidates all their votes")]
            public async Task BanUserFromSuggestions(IGuildUser target)
            {
                votesDAO.SetBannedStatus(target.Id, true);
                await RespondAsync($"Banned {target.Mention} from suggesting nicknames and invalidated all their votes");
                await CheckUsernamesWhereUserVoted(target);
            }

            [SlashCommand("unban", "Unbans the target user from suggesting nicknames and validates all their votes")]
            public async Task UnbanUserFromSuggestions(IGuildUser target)
            {
                votesDAO.SetBannedStatus(target.Id, false);
                await RespondAsync($"Unbanned {target.Mention} from suggesting nicknames and validated all their votes");
                await CheckUsernamesWhereUserVoted(target);
            }

            [SlashCommand("invalidate", "Invalidates the suggested nickname")]
            public async Task InvalidateNicknameSuggestion(String nickname)
            {
                votesDAO.SetInvalidateStatus(nickname, true);
                await RespondAsync($"Invalidated nickname {nickname}");
                await CheckUsernamesWithUsername(nickname);
            }

            [SlashCommand("validate", "Validates the suggested nickname")]
            public async Task UninvalidateNicknameSuggestion(String nickname)
            {
                votesDAO.SetInvalidateStatus(nickname, false);
                await RespondAsync($"Validated nickname {nickname}");
                await CheckUsernamesWithUsername(nickname);
            }

        }

    }
}