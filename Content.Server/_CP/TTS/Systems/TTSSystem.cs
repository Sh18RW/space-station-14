using System.Linq;
using System.Threading.Tasks;
using Content.Server._CP.TTS.Events;
using Content.Server.Chat.Systems;
using Content.Shared._CP.CCVars;
using Content.Shared._CP.TTS;
using Content.Shared._CP.TTS.Components;
using Content.Shared._CP.TTS.Events;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CP.TTS.Systems;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly List<string> _sampleText =
    [
        "Съешьте этот вкусный кусок торта и насладитесь чашкой горячего чая!",
        "Какой потрясающий закат! Небо раскрашено в оттенки оранжевого и розового.",
        "Это интересный вопрос. Дайте мне немного времени подумать над ним.",
        "Помогите мне! Тут какой-то странный клоун рассказывает самый смешной анекдот, и я не могу перестать смеяться!",
        "Сегодня погода идеально подходит для прогулки в парке.",
        "Я не могу поверить, как быстро летит время, когда тебе весело.",
        "Можете ли вы, пожалуйста, передать мне соль?",
        "Звук волн на море так спокоен и умиротворяет.",
        "Я очень жду выходных. У вас есть планы?",
        "Эта книга невероятно увлекательна! Я не могу её бросить.",
        "Аромат свежеиспеченного хлеба наполняет воздух.",
        "Я благодарен за всю поддержку, которую получил.",
        "Звёзды сегодня невероятно яркие и красивые.",
        "Я думаю, нам стоит принять другой подход, чтобы решить эту проблему.",
        "Смех детей, играющих, — это такой радостный звук.",
        "Сегодня я чувствую себя немного уставшим. Возможно, мне стоит отдохнуть.",
        "Городские огни ночью действительно завораживают.",
        "Я никогда раньше не видел такой потрясающий вид!",
        "Давайте максимально используем этот прекрасный день.",
        "Запах кофе утром — это лучший способ начать день.",
    ];


    private int _maxMessageLength;
    private bool _isEnabled;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CPCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CPCCVars.TTSMessageMaxLength, v => _maxMessageLength = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PlayTTSRequestEvent>(OnPlayTTSRequest);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
        InitializeSanitizer();

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

    public async void OnPlayTTSRequest(PlayTTSRequestEvent ev)
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

        if (audio == null)
        {
            return;
        }

        if (ev.Sources == null)
        {
            RaiseNetworkEvent(new PlayTTSEvent(audio, effects), ev.ReceiversFilter ?? Filter.Empty().AddAllPlayers());
            return;
        }

        foreach (var ent in ev.Sources.Where(ent => _entityManager.EntityExists(ent)))
        {
            RaiseNetworkEvent(new PlayTTSEvent(audio, effects, GetNetEntity(ent)), ev.ReceiversFilter ?? Filter.Pvs(ent));
        }
    }

    private async void HandleSay(EntityUid uid, string message, ProtoId<TTSVoicePrototype> voice, TTSEffects effects)
    {
        RaiseLocalEvent(new PlayTTSRequestEvent(message, voice, Filter.Pvs(uid), effects, [uid]));
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, ProtoId<TTSVoicePrototype> speaker, TTSEffects effects)
    {
        effects |= TTSEffects.Whisper;

        // ReSharper disable once InconsistentNaming
        var fullTTSEventRequest = new PlayTTSRequestEvent(message, speaker, Filter.Empty(), effects, [uid]);
        // ReSharper disable once InconsistentNaming
        var obfTTSEventRequest = new PlayTTSRequestEvent(obfMessage, speaker, Filter.Empty(), effects, [uid]);

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

    // ReSharper disable once InconsistentNaming
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

        var ssmlTraits = SoundTraits.RateFast;

        if (effects.HasFlag(TTSEffects.Whisper))
        {
            ssmlTraits = SoundTraits.PitchVeryLow;
        }

        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml, effects);
    }
}
