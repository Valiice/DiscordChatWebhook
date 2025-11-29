using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using DiscordChatWebhook.Services;
using System.Numerics;

namespace DiscordChatWebhook.UI;

internal class PluginUI : IDisposable
{
    private readonly Configuration config;
    private bool visible = false;

    private readonly XivChatType[] validTypes =
    {
        XivChatType.Say, XivChatType.Shout, XivChatType.Yell,
        XivChatType.Party, XivChatType.FreeCompany, XivChatType.Alliance,
        XivChatType.TellIncoming, XivChatType.Echo
    };

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

    private void Draw()
    {
        if (!Visible) return;

        ImGui.SetNextWindowSize(new Vector2(400, 350), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Discord Chat Webhook", ref this.visible))
        {
            // Master Switch
            bool enabled = this.config.Enabled;
            if (ImGui.Checkbox("Enable Plugin", ref enabled))
            {
                this.config.Enabled = enabled;
                this.config.Save();
            }
            ImGui.Separator();

            string url = this.config.WebhookUrl;
            ImGui.Text("Discord Webhook URL:");
            if (ImGui.InputText("##webhookurl", ref url, 200))
            {
                this.config.WebhookUrl = url;
                this.config.Save();
            }

            ImGui.Spacing();
            bool dfNotify = this.config.DutyFinderNotify;
            if (ImGui.Checkbox("Notify when Duty Finder Pops", ref dfNotify))
            {
                this.config.DutyFinderNotify = dfNotify;
                this.config.Save();
            }

            ImGui.Separator();
            ImGui.Text("Select Chat Channels to Forward:");

            foreach (var type in validTypes)
            {
                int typeInt = (int)type;
                bool enabledType = this.config.AllowedChatTypes.Contains(typeInt);

                if (ImGui.Checkbox(type.ToString(), ref enabledType))
                {
                    if (enabledType) this.config.AllowedChatTypes.Add(typeInt);
                    else this.config.AllowedChatTypes.Remove(typeInt);
                    this.config.Save();
                }
            }
            ImGui.End();
        }
    }

    public void Dispose()
    {
        Service.Interface.UiBuilder.Draw -= Draw;
        Service.Interface.UiBuilder.OpenConfigUi -= OnOpenUi;
        Service.Interface.UiBuilder.OpenMainUi -= OnOpenUi;
    }
}