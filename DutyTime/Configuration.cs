using Dalamud.Configuration;
using System;

namespace DutyTime;


[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // public bool IsConfigWindowMovable { get; set; } = true;
    // public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    
    // setup discord webhook settings for config
    public bool NotifyInDiscord { get; set; } = true;
    public bool NoPingWarning { get; set; } = true;
    public string WebhookUrl { get; set; } = "";
    public string DiscordUserId { get; set; } = "";


    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
