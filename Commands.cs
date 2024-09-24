using Discord;
using Discord.Interactions;

namespace DiscordNameChanger
{

    [Group("nickname", "Commands for controlling nicknames")]
    public class Commands : Command
    {

        [SlashCommand("suggest", "Suggests a nickname for a user. Changes their name if it has the most votes")]
        public async Task SuggestNickname(IGuildUser target, string nickname)
        {
            VotesDAO.SetVote(Context.User.Id, target.Id, nickname);
            await RespondAsync($"{Context.User.Mention} voted for {target.Mention} to be nicknamed {nickname}");
            await UpdateUser(target);
        }

        [SlashCommand("show", "Description")]
        public async Task GetNicknameSuggestions(IUser target)
        {
            Dictionary<string, int> nicknames = VotesDAO.GetAllSuggestions(target.Id);
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
        public class AdminCommands : Command
        {

            private async Task CheckUsernamesWhereUserVoted(IGuildUser voter)
            {
                foreach (ulong targetId in VotesDAO.GetAllTargetsWhereUserVoted(voter.Id))
                {
                    await UpdateUser(targetId);
                }
            }

            private async Task CheckUsernamesWithUsername(string username)
            {
                foreach (ulong targetId in VotesDAO.GetAllResultsWhereUserHasNicknameVoted(username))
                {
                    await UpdateUser(targetId);
                }
            }

            [SlashCommand("ban", "Bans the target user from suggesting nicknames and unvalidates all their votes")]
            public async Task BanUserFromSuggestions(IGuildUser target)
            {
                VotesDAO.SetBannedStatus(target.Id, true);
                await RespondAsync($"Banned {target.Mention} from suggesting nicknames and invalidated all their votes");
                await CheckUsernamesWhereUserVoted(target);
            }

            [SlashCommand("unban", "Unbans the target user from suggesting nicknames and validates all their votes")]
            public async Task UnbanUserFromSuggestions(IGuildUser target)
            {
                VotesDAO.SetBannedStatus(target.Id, false);
                await RespondAsync($"Unbanned {target.Mention} from suggesting nicknames and validated all their votes");
                await CheckUsernamesWhereUserVoted(target);
            }

            [SlashCommand("invalidate", "Invalidates the suggested nickname")]
            public async Task InvalidateNicknameSuggestion(String nickname)
            {
                VotesDAO.SetInvalidateStatus(nickname, true);
                await RespondAsync($"Invalidated nickname {nickname}");
                await CheckUsernamesWithUsername(nickname);
            }

            [SlashCommand("validate", "Validates the suggested nickname")]
            public async Task UninvalidateNicknameSuggestion(String nickname)
            {
                VotesDAO.SetInvalidateStatus(nickname, false);
                await RespondAsync($"Validated nickname {nickname}");
                await CheckUsernamesWithUsername(nickname);
            }

        }
    }
}
