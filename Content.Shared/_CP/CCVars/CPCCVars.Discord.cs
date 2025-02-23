using Robust.Shared.Configuration;

namespace Content.Shared._CP.CCVars;

// ReSharper disable once InconsistentNaming
public sealed partial class CPCCVars
{
    public static readonly CVarDef<string> DiscordBanNotificationWebhook =
        CVarDef.Create("discord.ban_notification_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordBanNotificationName =
        CVarDef.Create("discord.ban_notification_name", "Jandarma Office", CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordBanNotificationAvatarUrl =
        CVarDef.Create("discord.ban_notification_icon_url", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordBanNotificationFooterIconUrl =
        CVarDef.Create("discord.ban_notification_footer_icon_url", string.Empty, CVar.SERVERONLY);
}
