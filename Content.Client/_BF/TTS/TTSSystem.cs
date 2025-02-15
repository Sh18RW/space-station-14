using Content.Shared._BF.CCVars;
using Content.Shared._BF.TTS;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._BF.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 4f;

    /// <summary>
    /// The volume at which the TTS sound will not be heard.
    /// </summary>
    private const float MinimalVolume = -10f;

    private float _volume;
    private bool _isEnabled;
    private int _fileIdx;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(Prefix, _contentRoot);

        _cfg.OnValueChanged(BFCCVars.TTSClientEnabled, OnTTSEnabledChanged, true);
        _cfg.OnValueChanged(BFCCVars.TTSVolume, OnTTSVolumeChanged, true);

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(BFCCVars.TTSClientEnabled, OnTTSEnabledChanged);
        _cfg.UnsubValueChanged(BFCCVars.TTSVolume, OnTTSVolumeChanged);

        _contentRoot.Dispose();
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId, TTSEffects.Default));
    }

    private void OnTTSEnabledChanged(bool value)
    {
        _isEnabled = value;
    }

    private void OnTTSVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        if (!_isEnabled)
        {
            return;
        }

        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, Prefix / filePath);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(ev.Effects.HasFlag(TTSEffects.Whisper)))
            .WithMaxDistance(AdjustDistance(ev.Effects.HasFlag(TTSEffects.Whisper)));

        if (ev.SourceUid != null)
        {
            var sourceUid = GetEntity(ev.SourceUid.Value);
            _audio.PlayEntity(audioResource.AudioStream, sourceUid, audioParams);
        }
        else
        {
            _audio.PlayGlobal(audioResource.AudioStream, audioParams);
        }

        _contentRoot.RemoveFile(filePath);
    }

    private float AdjustVolume(bool isWhisper)
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }

        return volume;
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }
}
