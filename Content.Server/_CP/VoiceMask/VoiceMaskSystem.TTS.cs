using Content.Server._BF.TTS;
using Content.Server._BF.TTS.Components;
using Content.Shared._BF.TTS;
using Content.Shared._BF.TTS.Systems;
using Content.Shared.Inventory;
using Content.Shared.VoiceMask;

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
