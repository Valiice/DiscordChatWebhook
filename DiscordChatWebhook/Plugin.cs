using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using DiscordChatWebhook.Discord;
using DiscordChatWebhook.Services;
using DiscordChatWebhook.UI;
using Lumina.Excel.Sheets;
using System.Linq;

namespace DiscordChatWebhook;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "DiscordChatWebhook";
    private const string _commandName = "/dcw";

    private readonly Configuration _configuration;
    private readonly PluginUI _pluginUi;
    private readonly WebhookSender _sender;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        this._configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this._configuration.Initialize(pluginInterface);

        this._sender = new WebhookSender(this._configuration);
        this._pluginUi = new PluginUI(this._configuration);

        Service.Chat.ChatMessage += OnChatMessage;
        Service.ClientState.CfPop += OnDutyPop;

        Service.CommandManager.AddHandler(_commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the DiscordChatWebhook configuration."
        });
    }

    private void OnDutyPop(ContentFinderCondition condition)
    {
        if (!this._configuration.Enabled || !this._configuration.DutyFinderNotify) return;

        string dutyName = condition.Name.ExtractText();
        this._sender.EnqueueMessage("Duty Finder", "System", $"**{dutyName}** is ready! Commencing...", XivChatType.Notice);
    }

    private void OnChatMessage(IHandleableChatMessage chatMsg)
    {
        if (!this._configuration.Enabled) return;

        int typeId = (int)chatMsg.LogKind & 0x7F;

        if (this._configuration.AllowedChatTypes.Contains(typeId))
        {
            string senderName = chatMsg.Sender.TextValue;
            string worldName = "";

            if (chatMsg.Sender.Payloads.FirstOrDefault(p => p is PlayerPayload) is PlayerPayload playerPayload)
            {
                senderName = playerPayload.PlayerName;
                worldName = playerPayload.World.Value.Name.ToString() ?? "";
            }
            else if (senderName.Contains('@'))
            {
                var parts = senderName.Split('@');
                senderName = parts[0];
                worldName = parts[1];
            }

            this._sender.EnqueueMessage(senderName, worldName, chatMsg.Message.TextValue, chatMsg.LogKind);
        }
    }

    private void OnCommand(string command, string args)
    {
        this._pluginUi.Visible = true;
    }

    public void Dispose()
    {
        Service.Chat.ChatMessage -= OnChatMessage;
        Service.ClientState.CfPop -= OnDutyPop;
        Service.CommandManager.RemoveHandler(_commandName);
        this._pluginUi.Dispose();
        this._sender.Dispose();
    }
}
