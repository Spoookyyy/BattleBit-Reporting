using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Text.Json;
using System.Text;

using CommunityServerAPI.Handler;

class Program
{
    static async Task Main(string[] args)
    {
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.Start(29294);
        await Task.Delay(-1);
    }
}

public class MyPlayer : Player<MyPlayer>
{
    public bool IsMuted { get; set; }
    public int Strikes { get; set; }
    public int ReportedAmount { get; set; }
}

class MyGameServer : GameServer<MyPlayer>
{
    public const int MaxStrikes = 5;
    public const int MaxReports = 5;
    
    private static readonly BlacklistedStrings blacklistedStrings = new BlacklistedStrings();
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string DiscordWebhookUrl = "https://";

    public override async Task OnPlayerReported(MyPlayer reporter, MyPlayer reportedPlayer, ReportReason reason, string description)
    {
        reportedPlayer.ReportedAmount++;

        if (reportedPlayer.ReportedAmount >= MaxReports)
        {
            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    content = $"[{reporter.Name}](https://steamcommunity.com/profiles/{reportedPlayer.SteamID}) has been reported for {MaxReports} times"
                }),
                Encoding.UTF8,
                "application/json");

            await httpClient.PostAsync(DiscordWebhookUrl, jsonContent);
        }
    }

    public override async Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        messageHandler.handleMessage(player, channel, msg);
        
        if (player.IsMuted) return false;

        var containsBlacklistedString = blacklistedStrings.Strings.Any(s => msg.ToLower().Contains(s.ToLower()));

        if (containsBlacklistedString)
        {
            player.Strikes++;

            if (player.Strikes >= MyGameServer.MaxStrikes)
            {
                player.IsMuted = true;
                player.Message(("You have been muted for repeated offences"));
                await Console.Out.WriteLineAsync($"Player {player.Name} has been muted.");
                return false;
            }
            
            player.Message($"You are currently at {player.Strikes} strikes, if you continue you will be muted.");
            
            return false;
        }

        return true;
    }

}

