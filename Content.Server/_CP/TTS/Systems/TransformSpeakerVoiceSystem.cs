using Content.Server._CP.TTS.Components;
using Content.Shared._CP.TTS;
using Content.Shared._CP.TTS.Events;
using Content.Shared.Inventory;
using TransformsSpeakerVoiceComponent = Content.Server._CP.TTS.Components.TransformsSpeakerVoiceComponent;

namespace Content.Server._CP.TTS.Systems;

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
