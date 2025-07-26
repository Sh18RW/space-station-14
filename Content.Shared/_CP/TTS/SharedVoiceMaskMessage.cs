using Robust.Shared.Serialization;

namespace Content.Shared._CP.TTS.Systems;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage(string voice) : BoundUserInterfaceMessage
{
    public string Voice = voice;
}
