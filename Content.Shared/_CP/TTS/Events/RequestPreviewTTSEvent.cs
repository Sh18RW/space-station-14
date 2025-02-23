using Robust.Shared.Serialization;

namespace Content.Shared._CP.TTS.Events;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class RequestPreviewTTSEvent(string voiceId, TTSEffects effects) : EntityEventArgs
{
    public string VoiceId { get; } = voiceId;
    public TTSEffects Effects { get; } = effects;
}
