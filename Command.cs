using Discord;
using Discord.Interactions;

namespace DiscordNameChanger
{
    public class Command : InteractionModuleBase<SocketInteractionContext>
    {

        public async Task UpdateUser(ulong userId)
        {
            await UpdateUser(Context.Guild.GetUser(userId));
        }

        public async Task UpdateUser(IGuildUser user)
        {
            Dictionary<string, int> nicknames = VotesDAO.GetAllSuggestions(user.Id);
            //await FollowupAsync($"nickname: {user.Nickname}, globalName: {user.DisplayName}, username: {user.Username}");
            string mostVotesNickname = (nicknames.Count > 0) ? nicknames.MaxBy(kvp => kvp.Value).Key : user.GlobalName;
            if (user.Nickname == null || !nicknames.TryGetValue(user.Nickname, out int value) || value < nicknames[mostVotesNickname])
            {
                await FollowupAsync($"Most voted username for {user.Mention} has changed from {user.Nickname ?? user.DisplayName ?? user.GlobalName} to {mostVotesNickname}");
                if (user.Hierarchy <= Context.Guild.CurrentUser.Hierarchy)
                {
                    await user.ModifyAsync(u => u.Nickname = mostVotesNickname);
                }
                else
                {
                    await FollowupAsync($"Could not set nickname for {user.Mention}");
                }
            }
        }

    }
}
