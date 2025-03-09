using Robust.Shared.Serialization;

namespace Content.Shared._CP.TTS.Events;

[Serializable, NetSerializable]
// ReSharper disable InconsistentNaming
public sealed class ClearTTSQueueEvent(List<EntityUid> sources) : EntityEventArgs
{
    public readonly List<EntityUid> Sources = sources;
}
