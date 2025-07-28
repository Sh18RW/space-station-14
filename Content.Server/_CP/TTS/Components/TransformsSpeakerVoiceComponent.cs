using Content.Shared._CP.TTS;
using Robust.Shared.Prototypes;

namespace Content.Server._CP.TTS.Components;

[RegisterComponent]
public sealed partial class TransformsSpeakerVoiceComponent : Component
{
    [DataField]
    public ProtoId<TTSVoicePrototype>? Voice { get; set; }

    [DataField]
    public TTSEffects Effects { get; set; } = TTSEffects.Default;

    [DataField]
    public bool ReplaceEffects { get; set; }
}
