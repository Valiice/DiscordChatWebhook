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
using System.Text.RegularExpressions;

namespace DiscordChatWebhook;

public sealed partial class Plugin : IDalamudPlugin
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

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!this._configuration.Enabled) return;

        int typeId = (int)type & 0x7F;

        if (this._configuration.AllowedChatTypes.Contains(typeId))
        {
            string senderName = sender.TextValue;
            string worldName = "";


            if (sender.Payloads.FirstOrDefault(p => p is PlayerPayload) is PlayerPayload playerPayload)
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
            else
            {
                // If the name has weird characters (like "Yuuki YangZodiark"), try to clean it
                // This regex splits on non-standard characters sometimes found in cross-world names
                if (NonAscii().IsMatch(senderName))
                {
                    // This is a "dirty" fix for mashed names if payload fails, 
                    // but usually PlayerPayload handles this case correctly.
                }

                if (Service.ClientState.LocalPlayer != null)
                {
                    worldName = Service.ClientState.LocalPlayer.HomeWorld.Value.Name.ToString() ?? "";
                }
            }

            this._sender.EnqueueMessage(senderName, worldName, message.TextValue, type);
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

    [GeneratedRegex(@"[^\u0000-\u007F]+")]
    private static partial Regex NonAscii();
}