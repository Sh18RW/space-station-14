using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.Managers;

/// <summary>
///     Sanitizes messages!
///     It currently ony removes the shorthands for emotes (like "lol" or "^-^") from a chat message and returns the last
///     emote in their message
/// </summary>
public sealed class ChatSanitizationManager : IChatSanitizationManager
{
    private static readonly List<EmoteShorthand> ShorthandToEmote =
    [
        // Corvax-Localization-Start
        new("хд", "chatsan-laughs"),
        new("о-о", "chatsan-wide-eyed"), // cyrillic о
        new("о.о", "chatsan-wide-eyed"), // cyrillic о
        new("0_о", "chatsan-wide-eyed"), // cyrillic о
        new("о/", "chatsan-waves"), // cyrillic о
        new("о7", "chatsan-salutes"), // cyrillic о
        new("0_o", "chatsan-wide-eyed"),
        new("лмао", "chatsan-laughs"),
        new("рофл", "chatsan-laughs"),
        new("яхз", "chatsan-shrugs"),
        new(":0", "chatsan-surprised"),
        new(":р", "chatsan-stick-out-tongue"), // cyrillic р
        new("кек", "chatsan-laughs"),
        new("T_T", "chatsan-cries"),
        new("Т_Т", "chatsan-cries"), // cyrillic T
        new("=_(", "chatsan-cries"),
        new("!с", "chatsan-laughs"),
        new("!в", "chatsan-sighs"),
        new("!х", "chatsan-claps"),
        new("!щ", "chatsan-snaps"),
        new("))", "chatsan-smiles-widely"),
        new(")", "chatsan-smiles"),
        new("((", "chatsan-frowns-deeply"),
        // Corvax-Localization-End
        new("(", "chatsan-frowns"),
        new(":)", "chatsan-smiles"),
        new(":]", "chatsan-smiles"),
        new("=)", "chatsan-smiles"),
        new("=]", "chatsan-smiles"),
        new("(:", "chatsan-smiles"),
        new("[:", "chatsan-smiles"),
        new("(=", "chatsan-smiles"),
        new("[=", "chatsan-smiles"),
        new("^^", "chatsan-smiles"),
        new("^-^", "chatsan-smiles"),
        new(":(", "chatsan-frowns"),
        new(":[", "chatsan-frowns"),
        new("=(", "chatsan-frowns"),
        new("=[", "chatsan-frowns"),
        new("):", "chatsan-frowns"),
        new(")=", "chatsan-frowns"),
        new("]:", "chatsan-frowns"),
        new("]=", "chatsan-frowns"),
        new(":D", "chatsan-smiles-widely"),
        new("D:", "chatsan-frowns-deeply"),
        new(":O", "chatsan-surprised"),
        new(":3", "chatsan-smiles"),
        new(":S", "chatsan-uncertain"),
        new(":>", "chatsan-grins"),
        new(":<", "chatsan-pouts"),
        new("xD", "chatsan-laughs"),
        new(":'(", "chatsan-cries"),
        new(":'[", "chatsan-cries"),
        new("='(", "chatsan-cries"),
        new("='[", "chatsan-cries"),
        new(")':", "chatsan-cries"),
        new("]':", "chatsan-cries"),
        new(")='", "chatsan-cries"),
        new("]='", "chatsan-cries"),
        new(";-;", "chatsan-cries"),
        new(";_;", "chatsan-cries"),
        new("qwq", "chatsan-cries"),
        new(":u", "chatsan-smiles-smugly"),
        new(":v", "chatsan-smiles-smugly"),
        new(">:i", "chatsan-annoyed"),
        new(":i", "chatsan-sighs"),
        new(":|", "chatsan-sighs"),
        new(":p", "chatsan-stick-out-tongue"),
        new(";p", "chatsan-stick-out-tongue"),
        new(":b", "chatsan-stick-out-tongue"),
        new("0-0", "chatsan-wide-eyed"),
        new("o-o", "chatsan-wide-eyed"),
        new("o.o", "chatsan-wide-eyed"),
        new("._.", "chatsan-surprised"),
        new(".-.", "chatsan-confused"),
        new("-_-", "chatsan-unimpressed"),
        new("smh", "chatsan-unimpressed"),
        new("o/", "chatsan-waves"),
        new("^^/", "chatsan-waves"),
        new(":/", "chatsan-uncertain"),
        new(":\\", "chatsan-uncertain"),
        new("lmao", "chatsan-laughs"),
        new("lmfao", "chatsan-laughs"),
        new("lol", "chatsan-laughs"),
        new("lel", "chatsan-laughs"),
        new("kek", "chatsan-laughs"),
        new("rofl", "chatsan-laughs"),
        new("o7", "chatsan-salutes"),
        new(";_;7", "chatsan-tearfully-salutes"),
        new("idk", "chatsan-shrugs"),
        new(";)", "chatsan-winks"),
        new(";]", "chatsan-winks"),
        new("(;", "chatsan-winks"),
        new("[;", "chatsan-winks"),
        new(":')", "chatsan-tearfully-smiles"),
        new(":']", "chatsan-tearfully-smiles"),
        new("=')", "chatsan-tearfully-smiles"),
        new("=']", "chatsan-tearfully-smiles"),
        new("(':", "chatsan-tearfully-smiles"),
        new("[':", "chatsan-tearfully-smiles"),
        new("('=", "chatsan-tearfully-smiles"),
        new("['=", "chatsan-tearfully-smiles"),
    ];



    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private bool _doSanitize;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.ChatSanitizerEnabled, x => _doSanitize = x, true);
    }

    /// <summary>
    ///     Remove the shorthands from the message, returning the last one found as the emote
    /// </summary>
    /// <param name="message">The pre-sanitized message</param>
    /// <param name="speaker">The speaker</param>
    /// <param name="sanitized">The sanitized message with shorthands removed</param>
    /// <param name="punctuateEmotes">If it is true, replaces shorthands with *emote*</param>
    /// <returns>True if emote has been sanitized out</returns>
    public void SanitizeEmoteShorthands(string message,
        EntityUid speaker,
        out string sanitized,
        bool punctuateEmotes = false)
    {
        sanitized = message;

        if (!_doSanitize)
        {
            return;
        }

        foreach (var shorthand in ShorthandToEmote)
        {
            var replacement = punctuateEmotes
                ? $"*{_loc.GetString(shorthand.EmoteName, ("ent", speaker))}*"
                : _loc.GetString(shorthand.EmoteName, ("ent", speaker));
            sanitized = shorthand.Shorthand.Replace(sanitized, replacement);
        }
        sanitized = message.Trim();
    }

    /// <summary>
    ///     Remove the shorthands from the message, returning the last one found as the emote
    /// </summary>
    /// <param name="message">The pre-sanitized message</param>
    /// <param name="speaker">The speaker</param>
    /// <param name="sanitized">The sanitized message with shorthands removed</param>
    /// <param name="emote">The localized emote</param>
    /// <returns>True if emote has been sanitized out</returns>
    public bool TrySanitizeEmoteShorthands(string message,
        EntityUid speaker,
        out string sanitized,
        [NotNullWhen(true)] out string? emote)
    {
        emote = null;
        sanitized = message;

        if (!_doSanitize)
            return false;

        // -1 is just a canary for nothing found yet
        var lastEmoteIndex = -1;

        foreach (var shorthandToEmote in ShorthandToEmote)
        {
            // We have to escape it because shorthands like ":)" or "-_-" would break the regex otherwise.
            // So there are 2 cases:
            // - If there is whitespace before it and after it is either punctuation, whitespace, or the end of the line
            //   Delete the word and the whitespace before
            // - If it is at the start of the string and is followed by punctuation, whitespace, or the end of the line
            //   Delete the word and the punctuation if it exists.
            // We're using sanitized as the original message until the end so that we can make sure the indices of
            // the emotes are accurate.
            var lastMatch = shorthandToEmote.Shorthand.Match(sanitized);

            if (!lastMatch.Success)
                continue;

            if (lastMatch.Index > lastEmoteIndex)
            {
                lastEmoteIndex = lastMatch.Index;
                emote = _loc.GetString(shorthandToEmote.EmoteName, ("ent", speaker));
            }

            message = shorthandToEmote.Shorthand.Replace(message, string.Empty);
        }

        sanitized = message.Trim();
        return emote is not null;
    }

    private sealed class EmoteShorthand
    {
        public Regex Shorthand { get; }
        public string EmoteName { get; }

        public EmoteShorthand(string shorthand, string emoteName)
        {
            var escaped = Regex.Escape(shorthand);
            Shorthand = new Regex($@"\s{escaped}(?=\p{{P}}|\s|$)|^{escaped}(?:\p{{P}}|(?=\s|$))");
            EmoteName = emoteName;
        }
    }
}
