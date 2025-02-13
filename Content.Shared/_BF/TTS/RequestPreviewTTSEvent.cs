using Robust.Shared.Serialization;

namespace Content.Shared._BF.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class RequestPreviewTTSEvent(string voiceId, TTSEffects effects) : EntityEventArgs
{
    public string VoiceId { get; } = voiceId;
    public TTSEffects Effects { get; } = effects;
}
