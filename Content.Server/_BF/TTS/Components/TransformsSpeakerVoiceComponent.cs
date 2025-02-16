using Content.Shared._BF.TTS;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server._BF.TTS.Components;

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
