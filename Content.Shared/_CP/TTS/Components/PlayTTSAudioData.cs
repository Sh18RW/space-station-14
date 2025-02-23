using Robust.Shared.Utility;

namespace Content.Shared._CP.TTS.Components;

// ReSharper disable once InconsistentNaming
public sealed class PlayTTSAudioData(ResPath path, TTSEffects effects)
{
    public readonly ResPath Path = path;
    public readonly TTSEffects Effects = effects;
}
