using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace DutyTime.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;
    private string webhookUrl;
    private string status = "";
    private string userId;

    public ConfigWindow(Plugin plugin) : base("DutyTime Configuration Window###With a constant ID")
    {
        Size = new Vector2(300, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
        
        this.plugin = plugin;
        configuration = plugin.Configuration;
        webhookUrl = configuration.WebhookUrl;
        userId = plugin.Configuration.DiscordUserId;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var pingsEnabled = configuration.NotifyInDiscord;
        if (ImGui.Checkbox("Enable Discord pings", ref pingsEnabled))
        {
            configuration.NotifyInDiscord = pingsEnabled;
            if (!configuration.NotifyInDiscord)
            {
                status = "Discord pings disabled.";
            }
            else
            {
                status = "Discord pings enabled.";
            }
            configuration.Save();
        }
        
        var pingWarning = configuration.NoPingWarning;
        if (ImGui.Checkbox("Warn when pings is disabled", ref pingWarning))
        {
            configuration.NoPingWarning = pingWarning;
            if (configuration.NoPingWarning)
            {
                status = "You will be reminded in chat when pings are disabled.";
            }
            else
            {
                status = "You will NOT be reminded in chat when pings are disabled.";
            }
            configuration.Save();
        }
        
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##webhook", "https://discord.com/api/webhooks/...", ref webhookUrl, 300, ImGuiInputTextFlags.Password);
        ImGui.TextUnformatted("Discord ID to ping: ");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##userID", "123456789012345678", ref userId, 50);
        
        if (ImGui.Button("Save"))
        {
            if (IsValidWebhook(webhookUrl))
            {
                configuration.WebhookUrl = webhookUrl.Trim();
                configuration.Save();
                status = "Settings saved!";
            }
            else
            {
                status = "Webhook URL is invalid.";
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Send test message to webhook"))
        {
            status = "Sending test message...";
                _ = Task.Run(async () =>
                {
                    var error = await plugin.SendDiscordAsync(
                                    // i've decided this will not be a debug feature. if you see this when you wanted to, congrats. if not, you will be laughed it. i will laugh at you.
                                    "Test notification from DutyTime plugin, if you see this it worked and you can move on with your life. If you see this when you didn't want to see it, uh oh");
                    status = error ?? "Test sent.";
                });
        }

        if (status.Length > 0)
        {
            ImGui.TextWrapped(status);
        }
    }
    
    private static bool IsValidWebhook(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                                                      uri.Scheme == Uri.UriSchemeHttps &&
                                                      (uri.Host is "discord.com" or "discordapp.com") &&
                                                      uri.AbsolutePath.StartsWith("/api/webhooks/");
}
