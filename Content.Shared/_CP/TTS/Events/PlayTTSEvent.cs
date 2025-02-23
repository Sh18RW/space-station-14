using Robust.Shared.Serialization;

namespace Content.Shared._CP.TTS.Events;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent(byte[] data, TTSEffects effects, NetEntity? sourceUid = null)
    : EntityEventArgs
{
    public byte[] Data { get; } = data;
    public TTSEffects Effects { get; } = effects;
    public NetEntity? SourceUid { get; } = sourceUid;
}
