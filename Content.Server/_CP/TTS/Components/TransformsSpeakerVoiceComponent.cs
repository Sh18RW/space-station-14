using Content.Shared._CP.TTS;
using Robust.Shared.Prototypes;

namespace Content.Server._CP.TTS.Components;

[RegisterComponent]
public sealed partial class TransformsSpeakerVoiceComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<TTSVoicePrototype>? Voice { get; set; } = null;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TTSEffects Effects { get; set; } = TTSEffects.Default;
}
