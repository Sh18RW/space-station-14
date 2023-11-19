using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed class RequestRadioTTSEvent : EntityEventArgs
{
    public byte[]? Audio { get; set; }
    public INetChannel Receiver { get; set; }

    public RequestRadioTTSEvent(byte[]? audio, INetChannel receiver)
    {
        Audio = audio;
        Receiver = receiver;
    }
}
