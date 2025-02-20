using Content.Shared.Inventory;

namespace Content.Shared._BF.TTS.Events;

public sealed class TransformSpeakerVoiceEvent(string? voiceId, TTSEffects effects) : EntityEventArgs, IInventoryRelayEvent
{
    public string? VoiceId = voiceId;
    public TTSEffects Effects = effects;
    public SlotFlags TargetSlots => SlotFlags.All;
}
