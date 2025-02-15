using Content.Server._BF.TTS;
using Content.Shared._BF.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{

    public readonly ProtoId<TTSVoicePrototype> _announcerVoice = "Announcer";

    // ReSharper disable InconsistentNaming
    private async void SendAnnounceTTS(string message, Filter filter)
    {
        RaiseLocalEvent(new PlayTTSRequestEvent(message, _announcerVoice, filter));
    }
}
