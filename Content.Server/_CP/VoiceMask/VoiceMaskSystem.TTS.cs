using Content.Server._CP.TTS;
using Content.Server._CP.TTS.Components;
using Content.Shared._CP.TTS;
using Content.Shared._CP.TTS.Systems;
using Content.Shared.Inventory;
using Content.Shared.VoiceMask;
using TransformsSpeakerVoiceComponent = Content.Server._CP.TTS.Components.TransformsSpeakerVoiceComponent;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void OnChangeVoice(Entity<VoiceMaskComponent> entity, ref VoiceMaskChangeVoiceMessage msg)
    {
        if (!TryComp<TransformsSpeakerVoiceComponent>(entity.Owner, out var transformVoiceComponent))
        {
            return;
        }

        if (msg.Voice is { } id && !_proto.HasIndex<TTSVoicePrototype>(id))
        {
            return;
        }

        transformVoiceComponent.Voice = msg.Voice;

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), entity);

        UpdateUI(entity);
    }
}
