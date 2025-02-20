using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._BF.TTS.Components;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent, NetworkedComponent]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice", customTypeSerializer: typeof(PrototypeIdSerializer<TTSVoicePrototype>))]
    public string? VoicePrototypeId { get; set; }

    /// <summary>
    /// Effects of used voice for TTS.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("effects")]
    public TTSEffects Effects { get; set; } = TTSEffects.Default;

    [DataField]
    public TimeSpan EndTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Used on client to play audio queued on one entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public Queue<PlayTTSAudioData> Queue { get; set; } = new();
}
