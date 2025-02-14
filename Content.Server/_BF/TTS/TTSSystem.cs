using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared._BF.CCVars;
using Content.Shared._BF.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using TTSComponent = Content.Shared._BF.TTS.TTSComponent;

namespace Content.Server._BF.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly List<string> _sampleText =
    [
        "Eat this beauty piece of the cake and drink a cup of tea!",
        "What a beautiful sunset!",
        "That’s a good question. Let me think.",
        "Help me! There is a strange clown telling the funniest joke! I'm dying of laughter.",
    ];

    private int _maxMessageLength;
    private bool _isEnabled;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _cfg.OnValueChanged(BFCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(BFCCVars.TTSMessageMaxLength, v => _maxMessageLength = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();

        _sawmill = _logManager.GetSawmill("TTS System");
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled)
        {
            return;
        }

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
        {
            return;
        }

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
        {
            return;
        }

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker, ev.Effects);
        if (soundData is null)
        {
            return;
        }

        RaiseNetworkEvent(new PlayTTSEvent(soundData, ev.Effects), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled)
        {
            return;
        }

        if (args.Message.Length > _maxMessageLength)
        {
            _sawmill.Warning($"Too much long message from {uid}, ignore TTS request.");
            return;
        }

        var voiceId = component.VoicePrototypeId;
        var effects = component.Effects;

        // if something changes voice and effects.
        var voiceEv = new TransformSpeakerVoiceEvent(voiceId, effects);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;
        effects = voiceEv.Effects;

        if (voiceId == null)
        {
            _sawmill.Warning($"Voice id on {uid} is null, ignore TTS request.");
            return;
        }

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
        {
            return;
        }

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker, effects);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker, effects);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker, TTSEffects effects)
    {
        var soundData = await GenerateTTS(message, speaker, effects);
        if (soundData is null)
        {
            return;
        }
        RaiseNetworkEvent(new PlayTTSEvent(soundData, effects, GetNetEntity(uid)), Filter.Pvs(uid));
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker, TTSEffects effects)
    {
        effects |= TTSEffects.Whisper;

        var fullSoundData = await GenerateTTS(message, speaker, effects);
        if (fullSoundData is null)
        {
            return;
        }

        var obfSoundData = await GenerateTTS(obfMessage, speaker, effects);
        if (obfSoundData is null)
        {
            return;
        }

        var fullTtsEvent = new PlayTTSEvent(fullSoundData, effects, GetNetEntity(uid));
        var obfTtsEvent = new PlayTTSEvent(obfSoundData, effects, GetNetEntity(uid));

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;
        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue)
            {
                continue;
            }
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.VoiceRange * ChatSystem.VoiceRange)
            {
                continue;
            }

            RaiseNetworkEvent(distance > ChatSystem.WhisperClearRange ? obfTtsEvent : fullTtsEvent, session);
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, TTSEffects effects)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
        {
            return null;
        }
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var ssmlTraits = SoundTraits.RateFast;
        if (effects.HasFlag(TTSEffects.Whisper))
        {
            ssmlTraits = SoundTraits.PitchVeryLow;
        }

        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml, effects);
    }
}

public sealed class TransformSpeakerVoiceEvent(string? voiceId, TTSEffects effects) : EntityEventArgs
{
    public string? VoiceId = voiceId;
    public TTSEffects Effects = effects;
}
