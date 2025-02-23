using Content.Server._BF.TTS.Components;
using Content.Shared._BF.TTS;
using Content.Shared._BF.TTS.Events;
using Content.Shared.Inventory;

namespace Content.Server._BF.TTS.Systems;

/// <summary>
///
/// </summary>
public sealed class TransformSpeakerVoiceSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TransformsSpeakerVoiceComponent, InventoryRelayedEvent<TransformSpeakerVoiceEvent>>(OnTransformSpeakerVoice);
    }

    private void OnTransformSpeakerVoice(Entity<TransformsSpeakerVoiceComponent> ent, ref InventoryRelayedEvent<TransformSpeakerVoiceEvent> args)
    {
        if (ent.Comp.Voice != null)
        {
            args.Args.VoiceId = ent.Comp.Voice;
        }

        args.Args.Effects |= ent.Comp.Effects;
    }
}
