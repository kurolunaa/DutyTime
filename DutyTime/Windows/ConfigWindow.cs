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

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("DutyTime Configuration Window###With a constant ID")
    {
        // Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
        
        this.plugin = plugin;
        configuration = plugin.Configuration;
        webhookUrl = configuration.WebhookUrl;
        userId = plugin.Configuration.DiscordUserId;

    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        // if (configuration.IsConfigWindowMovable)
        // {
        //     Flags &= ~ImGuiWindowFlags.NoMove;
        // }
        // else
        // {
        //     Flags |= ImGuiWindowFlags.NoMove;
        // }
    }

    public override void Draw()
    {
        // Can't ref a property, so use a local copy
        // var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
        // if (ImGui.Checkbox("Random Config Bool", ref configValue))
        // {
        //     configuration.SomePropertyToBeSavedAndWithADefault = configValue;
        //     // Can save immediately on change if you don't want to provide a "Save and Close" button
        //     configuration.Save();
        // }
        
        // var movable = configuration.IsConfigWindowMovable;
        // if (ImGui.Checkbox("Movable Config Window", ref movable))
        // {
        //     configuration.IsConfigWindowMovable = movable;
        //     configuration.Save();
        // }
        
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
        
        // discord webhook shenanigans
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##webhook", "https://discord.com/api/webhooks/...", ref webhookUrl, 300, ImGuiInputTextFlags.Password);
        
        // who to ping (userID), you will need dev mode enabled for discord (look it up)
        ImGui.TextUnformatted("Discord ID to ping: ");
        ImGui.SetNextItemWidth(-1);
        // i looked it up and apparently discord IDs are 18 digits, but it increased to 19? i don't care enough, so 50 it is
        ImGui.InputTextWithHint("##userID", "123456789012345678", ref userId, 50);
        
        if (ImGui.Button("Save"))
        {
            if (IsValidWebhook(webhookUrl))
            {
                configuration.WebhookUrl = webhookUrl.Trim();
                // configuration.DiscordUserId = userID.Trim();
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
