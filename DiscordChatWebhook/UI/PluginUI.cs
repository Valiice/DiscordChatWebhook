using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Discord;
using Discord.Webhook;
using DiscordChatWebhook.Services;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace DiscordChatWebhook.UI;

internal class PluginUI : IDisposable
{
    private readonly Configuration config;
    private bool visible = false;
    private bool _wasVisible = false;

    private static readonly (string Group, XivChatType[] Types)[] _channelGroups =
    [
        ("Basic", [XivChatType.Say, XivChatType.Shout, XivChatType.Yell, XivChatType.Echo]),
        ("Group", [XivChatType.Party, XivChatType.CrossParty, XivChatType.Alliance, XivChatType.FreeCompany]),
        ("Tells", [XivChatType.TellIncoming, XivChatType.TellOutgoing]),
        ("Networks", [XivChatType.NoviceNetwork]),
        ("Linkshells", [XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8]),
        ("Cross-World Linkshells", [XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8]),
    ];

    public bool Visible
    {
        get => this.visible;
        set => this.visible = value;
    }

    public PluginUI(Configuration config)
    {
        this.config = config;

        Service.Interface.UiBuilder.Draw += Draw;
        Service.Interface.UiBuilder.OpenConfigUi += OnOpenUi;
        Service.Interface.UiBuilder.OpenMainUi += OnOpenUi;
    }

    private void OnOpenUi()
    {
        this.Visible = true;
    }

    private static bool IsValidWebhookUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               uri.Host.EndsWith("discord.com") &&
               uri.AbsolutePath.Contains("/api/webhooks/");
    }

    private void Draw()
    {
        // Debounce: save config only when the window closes
        if (this._wasVisible && !this.visible)
        {
            this.config.Save();
        }
        this._wasVisible = this.visible;

        if (!Visible) return;

        ImGui.SetNextWindowSize(new Vector2(400, 500), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Discord Chat Webhook", ref this.visible))
        {
            bool enabled = this.config.Enabled;
            if (ImGui.Checkbox("Enable Plugin", ref enabled))
            {
                this.config.Enabled = enabled;
            }
            ImGui.Separator();

            string url = this.config.WebhookUrl;
            ImGui.Text("Discord Webhook URL:");
            ImGui.SetNextItemWidth(-55);
            if (ImGui.InputText("##webhookurl", ref url, 200))
            {
                this.config.WebhookUrl = url;
            }
            ImGui.SameLine();
            if (ImGui.Button("Test") && !string.IsNullOrEmpty(url) && IsValidWebhookUrl(url))
            {
                var testUrl = url;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var testClient = new DiscordWebhookClient(testUrl);
                        var embed = new EmbedBuilder()
                            .WithDescription("Webhook connection successful!")
                            .WithColor(0x2ECC71)
                            .WithCurrentTimestamp()
                            .Build();
                        await testClient.SendMessageAsync(embeds: [embed], username: "FFXIV Chat");
                        Service.Chat.Print("[DiscordBridge] Test message sent successfully!");
                    }
                    catch (Exception ex)
                    {
                        Service.Chat.PrintError($"[DiscordBridge] Test failed: {ex.Message}");
                    }
                });
            }

            // URL validation warning
            if (!string.IsNullOrEmpty(url) && !IsValidWebhookUrl(url))
            {
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Invalid webhook URL. Expected: https://discord.com/api/webhooks/...");
            }

            ImGui.Spacing();
            bool dfNotify = this.config.DutyFinderNotify;
            if (ImGui.Checkbox("Notify when Duty Finder Pops", ref dfNotify))
            {
                this.config.DutyFinderNotify = dfNotify;
            }

            ImGui.Separator();
            ImGui.Text("Select Chat Channels to Forward:");

            foreach (var (group, types) in _channelGroups)
            {
                if (ImGui.CollapsingHeader(group, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var type in types)
                    {
                        int typeInt = (int)type;
                        bool enabledType = this.config.AllowedChatTypes.Contains(typeInt);

                        if (ImGui.Checkbox(type.ToString(), ref enabledType))
                        {
                            if (enabledType) this.config.AllowedChatTypes.Add(typeInt);
                            else this.config.AllowedChatTypes.Remove(typeInt);
                        }
                    }
                }
            }
        }
        ImGui.End();
    }

    public void Dispose()
    {
        Service.Interface.UiBuilder.Draw -= Draw;
        Service.Interface.UiBuilder.OpenConfigUi -= OnOpenUi;
        Service.Interface.UiBuilder.OpenMainUi -= OnOpenUi;
    }
}
