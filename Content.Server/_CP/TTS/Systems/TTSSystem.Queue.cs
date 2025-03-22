using Content.Shared._CP.TTS.Events;
using Robust.Shared.Player;

namespace Content.Server._CP.TTS.Systems;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    public void ClearQueue(List<NetEntity> sources)
    {
        RaiseNetworkEvent(new ClearTTSQueueEvent(sources), Filter.Empty().AddAllPlayers());
    }
}
