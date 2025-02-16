using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Shared._BF.TTS;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._BF.TTS;

/// <summary>
/// Uses for radio tts.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSHeadsetSystem : EntitySystem
{
    /// <inheritdoc cref="Robust.Shared.GameObjects.EntitySystem" />
    [Dependency] private readonly TTSSystem _ttsSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayRadioTTSEvent>(OnPlayRadioTTS);
        _sawmill = LogManager.GetSawmill("TTS Radio");
    }

    private async void OnPlayRadioTTS(PlayRadioTTSEvent ev)
    {
        var effects = ev.Effects | TTSEffects.Radio;
        var cache = await _ttsSystem.GenerateCachedTTS(ev.Message, ev.Voice, effects);

        if (cache == null)
        {
            _sawmill.Warning("Cache for TTS message is null. Ignore TTS generation.");
            return;
        }

        foreach (var receiver in ev.Receivers)
        {
            if (TryComp<RadioSpeakerComponent>(receiver, out var speaker))
            {
                if (!speaker.Enabled)
                {
                    continue;
                }

                RaiseLocalEvent(new PlayTTSRequestEvent(ev.Message, ev.Voice, Filter.Pvs(receiver), effects, receiver, cache));

                continue;
            }

            if (!TryComp(Transform(receiver).ParentUid, out ActorComponent? actor))
            {
                continue;
            }

            RaiseLocalEvent(new PlayTTSRequestEvent(ev.Message, ev.Voice, Filter.SinglePlayer(actor.PlayerSession), cache: cache));
        }
    }

    // ReSharper disable once InconsistentNaming
    public record PlayRadioTTSEvent(
        string Voice,
        string Message,
        TTSEffects Effects,
        List<EntityUid> Receivers);
}
