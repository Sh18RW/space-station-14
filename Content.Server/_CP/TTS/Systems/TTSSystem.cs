using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared._BF.CCVars;
using Content.Shared._BF.TTS;
using Content.Shared._BF.TTS.Components;
using Content.Shared._BF.TTS.Events;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._BF.TTS.Systems;

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
        "Eat this delicious piece of cake and enjoy a warm cup of tea!",
        "What a breathtaking sunset! The sky is painted in hues of orange and pink.",
        "That’s an interesting question. Let me take a moment to think about it.",
        "Help me! There’s a strange clown telling the funniest joke, and I can’t stop laughing!",
        "The weather today is perfect for a walk in the park.",
        "I can’t believe how fast time flies when you’re having fun.",
        "Could you please pass me the salt?",
        "The sound of the ocean waves is so calming and peaceful.",
        "I’m really looking forward to the weekend. Any plans?",
        "This book is absolutely fascinating! I can’t put it down.",
        "The aroma of freshly baked bread fills the air.",
        "I’m so grateful for all the support I’ve received.",
        "The stars tonight are incredibly bright and beautiful.",
        "I think we should take a different approach to solve this problem.",
        "The laughter of children playing is such a joyful sound.",
        "I’m feeling a bit tired today. Maybe I should get some rest.",
        "The city lights at night are truly mesmerizing.",
        "I’ve never seen such a stunning view before!",
        "Let’s make the most of this wonderful day.",
        "The smell of coffee in the morning is the best way to start the day.",
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
        SubscribeLocalEvent<PlayTTSRequestEvent>(OnPlayTTSRequest);

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
        var soundData = (await GenerateTTS(previewText, protoVoice.Speaker, ev.Effects | protoVoice.Effects)).audio;
        if (soundData is null)
        {
            return;
        }

        RaiseNetworkEvent(new PlayTTSEvent(soundData, ev.Effects), Filter.SinglePlayer(args.SenderSession));
    }

    private void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
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
        effects |= voiceEv.Effects;

        if (voiceId == null)
        {
            _sawmill.Warning($"Voice id on {uid} is null, ignore TTS request.");
            return;
        }

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
        {
            return;
        }

        effects |= protoVoice.Effects;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice, effects);
            return;
        }

        HandleSay(uid, args.Message, protoVoice, effects);
    }

    private async void OnPlayTTSRequest(PlayTTSRequestEvent ev)
    {
        if (!_isEnabled)
        {
            return;
        }

        var effects = ev.Effects;
        var speaker = SharedHumanoidAppearanceSystem.DefaultVoice;
        if (_prototypeManager.TryIndex(ev.Voice, out var voiceProto))
        {
            effects |= voiceProto.Effects;
            speaker = voiceProto.Speaker;
        }

        if (!(ev.Cache != null && _ttsManager.TryGetAudio(ev.Cache, out var audio)))
        {
            if (ev.Message.Length > _maxMessageLength)
            {
                _sawmill.Warning($"Too much long announce message, ignore TTS request.");
                return;
            }

            audio = (await GenerateTTS(ev.Message, speaker, effects)).audio;
        }

        if (audio != null)
        {
            RaiseNetworkEvent(new PlayTTSEvent(audio, effects, GetNetEntity(ev.Source)), ev.ReceiversFilter);
        }
    }

    private async void HandleSay(EntityUid uid, string message, ProtoId<TTSVoicePrototype> voice, TTSEffects effects)
    {
        RaiseLocalEvent(new PlayTTSRequestEvent(message, voice, Filter.Pvs(uid), effects, uid));
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, ProtoId<TTSVoicePrototype> speaker, TTSEffects effects)
    {
        effects |= TTSEffects.Whisper;

        // ReSharper disable once InconsistentNaming
        var fullTTSEventRequest = new PlayTTSRequestEvent(message, speaker, Filter.Empty(), effects, uid);
        // ReSharper disable once InconsistentNaming
        var obfTTSEventRequest = new PlayTTSRequestEvent(obfMessage, speaker, Filter.Empty(), effects, uid);

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

            if (distance > ChatSystem.WhisperClearRange)
            {
                obfTTSEventRequest.ReceiversFilter = Filter.SinglePlayer(session);
                RaiseLocalEvent(obfTTSEventRequest);
            }
            else
            {
                fullTTSEventRequest.ReceiversFilter = Filter.SinglePlayer(session);
                RaiseLocalEvent(fullTTSEventRequest);
            }
        }
    }

    public async Task<string?> GenerateCachedTTS(string message, ProtoId<TTSVoicePrototype> voiceId, TTSEffects effects)
    {
        return !_prototypeManager.TryIndex(voiceId, out var voiceProto) ? null : (await GenerateTTS(message, voiceProto.Speaker, effects)).cacheKey;
    }

    // ReSharper disable once InconsistentNaming
    private async Task<(byte[]? audio, string? cacheKey)> GenerateTTS(string text, string speaker, TTSEffects effects)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
        {
            return (null, null);
        }

        if (char.IsLetter(textSanitized[^1]))
        {
            textSanitized += ".";
        }

        var ssmlTraits = TTSSystem.SoundTraits.RateFast;

        if (effects.HasFlag(TTSEffects.Whisper))
        {
            ssmlTraits = TTSSystem.SoundTraits.PitchVeryLow;
        }

        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml, effects);
    }
}
