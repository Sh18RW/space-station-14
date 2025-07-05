using Content.Shared.Humanoid;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP.TTS;

/// <summary>
/// Prototype represent available TTS voices
/// </summary>
[Prototype("ttsVoice")]
// ReSharper disable once InconsistentNaming
public sealed class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; } = string.Empty;

    [DataField("sex", required: true)]
    public Sex Sex { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("speaker", required: true)]
    public string Speaker { get; } = string.Empty;

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField("roundStart")]
    public bool RoundStart { get; } = true;

    [DataField("effects")]
    public TTSEffects Effects { get; }

    /// <summary>
    ///     Tags that allows to sort voices.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = [];
}
