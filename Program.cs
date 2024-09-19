using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace InteractionFramework;

public class Program
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;

    public Program()
    {
        _client = new(new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true,
        }
        );
        _handler = new(_client);
        _client.Log += LogAsync;
    }

    public static async Task Main(string[] args)
    {
        await new Program().Start();
    }

    public async Task Start()
    {
        await InitializeAsync();
        await _client.LoginAsync(TokenType.Bot, "NjkwNjg3NDAwNjczODA0MzE5.GrlL3W.V1CoRk-sgZOYwHRUK83rQUXkSzX-HWRWCu12F8");
        await _client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    public async Task InitializeAsync()
    {
        _client.Ready += ReadyAsync;
        _handler.Log += LogAsync;

        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), null);

        _client.InteractionCreated += HandleInteraction;
    }

    private async Task ReadyAsync()
    {
        await _handler.RegisterCommandsGloballyAsync();
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var result = await _handler.ExecuteCommandAsync(new SocketInteractionContext(_client, interaction), null);
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    private Task LogAsync(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }
}
