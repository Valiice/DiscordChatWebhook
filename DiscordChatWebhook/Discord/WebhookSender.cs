using Dalamud.Game.Text;
using Discord;
using Discord.Webhook;
using DiscordChatWebhook.Services;
using System.Collections.Concurrent;

namespace DiscordChatWebhook.Discord;

public struct ChatMessage
{
    public string Sender;
    public string World;
    public string Content;
    public XivChatType Type;
}

public class WebhookSender : IDisposable
{
    private readonly Configuration _config;
    private readonly ConcurrentQueue<ChatMessage> _messageQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;
    private readonly LodestoneScraper _scraper = new();

    // Default "Silhouette" image from Lodestone for fallback
    private const string _defaultAvatarUrl = "https://img2.finalfantasyxiv.com/h/e/erPIM9iFmQ90lD179r_s8f65kM.jpg";

    public WebhookSender(Configuration config)
    {
        this._config = config;
        this._workerTask = Task.Run(ProcessQueueAsync);
        Service.Logger.Debug("[WebhookSender] Service started.");
    }

    public void EnqueueMessage(string sender, string world, string message, XivChatType type)
    {
        Service.Logger.Verbose($"[WebhookSender] Enqueuing message from {sender}...");
        this._messageQueue.Enqueue(new ChatMessage
        {
            Sender = sender,
            World = world,
            Content = message,
            Type = type
        });
    }

    private async Task ProcessQueueAsync()
    {
        while (!this._cts.Token.IsCancellationRequested)
        {
            if (string.IsNullOrEmpty(this._config.WebhookUrl))
            {
                await Task.Delay(2000);
                continue;
            }

            if (this._messageQueue.TryDequeue(out var msg))
            {
                try
                {
                    Service.Logger.Verbose($"[WebhookSender] Processing message: {msg.Content}");

                    // 1. Fetch Avatar
                    string avatarUrl = await this._scraper.GetAvatarAsync(msg.Sender, msg.World);

                    // 2. Use Fallback if empty
                    if (string.IsNullOrEmpty(avatarUrl))
                    {
                        avatarUrl = _defaultAvatarUrl;
                    }

                    var style = GetChatStyle(msg.Type);

                    // 3. Build Embed
                    var embed = new EmbedBuilder()
                        .WithAuthor($"{msg.Sender} @ {msg.World}", iconUrl: avatarUrl)
                        .WithDescription(msg.Content)
                        .WithColor(style.Color)
                        .WithFooter($"{style.Emoji}  {style.Label}")
                        .WithCurrentTimestamp()
                        .Build();

                    // 4. Send
                    using var client = new DiscordWebhookClient(this._config.WebhookUrl);
                    await client.SendMessageAsync(embeds: [embed], username: "FFXIV Chat");

                    Service.Logger.Verbose($"[WebhookSender] Sent successfully!");
                }
                catch (Exception ex)
                {
                    Service.Logger.Error(ex, "[WebhookSender] Failed to send webhook.");
                    // ERROR NOTIFICATION: Prints to in-game chat in red
                    Service.Chat.PrintError($"[DiscordBridge] Failed to send message: {ex.Message}");
                }
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }

    private static (uint Color, string Emoji, string Label) GetChatStyle(XivChatType type)
    {
        return (int)type switch
        {
            (int)XivChatType.TellIncoming => (0xFF7EB9, "📨", "Tell Incoming"),
            (int)XivChatType.TellOutgoing => (0xFF7EB9, "📤", "Tell Outgoing"),
            (int)XivChatType.Party => (0x66CCFF, "🛡️", "Party"),
            (int)XivChatType.CrossParty => (0x66CCFF, "⚔️", "Cross-World Party"),
            (int)XivChatType.Alliance => (0xFF7F00, "🚩", "Alliance"),
            (int)XivChatType.FreeCompany => (0xADD8E6, "🏠", "Free Company"),
            (int)XivChatType.Say => (0xFFFFFF, "💬", "Say"),
            (int)XivChatType.Yell => (0xFFFF00, "📢", "Yell"),
            (int)XivChatType.Shout => (0xFFA07A, "🔥", "Shout"),
            (int)XivChatType.NoviceNetwork => (0x2B922F, "🌱", "Novice Network"),
            >= (int)XivChatType.Ls1 and <= (int)XivChatType.Ls8 => (0x98FB98, "🟢", "Linkshell"),
            >= (int)XivChatType.CrossLinkShell1 and <= (int)XivChatType.CrossLinkShell8 => (0xD7FF00, "🌐", "CWLS"),
            _ => (0x95A5A6, "📝", type.ToString())
        };
    }

    public void Dispose()
    {
        this._cts.Cancel();
        try { this._workerTask.Wait(1000); } catch { }
        this._cts.Dispose();
        Service.Logger.Debug("[WebhookSender] Disposed.");

        GC.SuppressFinalize(this);
    }
}