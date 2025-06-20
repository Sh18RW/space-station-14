using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Chat.Managers;

public interface IChatSanitizationManager
{
    public void Initialize();

    public void SanitizeEmoteShorthands(string input,
        EntityUid speaker,
        out string sanitized,
        bool punctuateEmotes = false);

    public bool TrySanitizeEmoteShorthands(string input,
        EntityUid speaker,
        out string sanitized,
        [NotNullWhen(true)] out string? emote);
}
