using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DutyTime.Windows;

using Lumina.Excel.Sheets;

namespace DutyTime;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] private static IChatGui Chat { get; set; } = null!;

    private const string CommandName = "/dutytime";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("DutyTime");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    private readonly IClientState clientState;
    private string dutyName = "";
    
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    
    public Plugin(IClientState clientState)
    {
        this.clientState = clientState;
        this.clientState.CfPop += OnCfPop;
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "The default MainWindow that came with the SampleProject\n/dutytime config -> Opens the DutyTime settings"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [DutyTime] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public async Task<string?> SendDiscordAsync(string message, string mentionUserIds = "")
    {
        if (string.IsNullOrWhiteSpace(Configuration.WebhookUrl))
        {
            return "No webhook set";
        }
        
        try
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["content"] = message,
                ["allowedMentions"] = new Dictionary<string, string[]> { ["parse"] = Array.Empty<string>() },
            });

            Log.Info(payload);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var resp = await Http.PostAsync(Configuration.WebhookUrl, content);

            if (!resp.IsSuccessStatusCode)
            {
                return $"Discord returned {(int)resp.StatusCode}.";
            } 
            return null;
        }
        catch (Exception e)
        { 
            Log.Error(e, "Webhook failed");
            return "Request failed, see /xllog";
        }
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        clientState.CfPop -= OnCfPop;

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        if (args == "config")
        {
            ConfigWindow.Toggle();    
        }
        else
        {
            MainWindow.Toggle();
        }
    }

    private void OnCfPop(ContentFinderCondition duty)
    {
        // why is the first letter not capitalized? who decided that
        dutyName = duty.Name.ToString();
        char firstLetter = dutyName[0];
        firstLetter = char.ToUpper(firstLetter);
        dutyName = firstLetter + dutyName.Remove(0, 1);

        if (Configuration.NotifyInDiscord)
        {
            var mention = $"<@{Configuration.DiscordUserId}>";
            var message = $"{mention}, {dutyName} is ready!";
            _ = Task.Run(() => SendDiscordAsync(message));
        }
        else
        {
            if (Configuration.NoPingWarning){
                Notify($"{dutyName} is ready, however pings are disabled. You can this warning in \"/dutytime\" under \"Warn when pings is disabled\".");
            }
        }
        
    }
    
    private static void Notify(string message)
    {
        Chat.Print(message);
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
