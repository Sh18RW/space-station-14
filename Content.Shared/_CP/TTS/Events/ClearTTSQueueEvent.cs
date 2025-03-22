using Robust.Shared.Serialization;

namespace Content.Shared._CP.TTS.Events;

[Serializable, NetSerializable]
// ReSharper disable InconsistentNaming
public sealed class ClearTTSQueueEvent(List<NetEntity> sources) : EntityEventArgs
{
    public readonly List<NetEntity> Sources = sources;
}
