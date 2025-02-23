using Content.Shared._CP.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CP.TTS.Events;

// ReSharper disable once InconsistentNaming
public sealed class PlayTTSRequestEvent(string message, ProtoId<TTSVoicePrototype> voice, Filter receiversFilter, TTSEffects effects = TTSEffects.Default, EntityUid? source = null, string? cache = null) : EntityEventArgs
{
    public Filter ReceiversFilter = receiversFilter;
    public readonly string Message = message;
    public readonly EntityUid? Source = source;
    public readonly TTSEffects Effects = effects;
    public readonly ProtoId<TTSVoicePrototype> Voice = voice;
    public readonly string? Cache = cache;
}
