using Content.Shared._CP.TTS;
using Content.Shared._CP.TTS.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Client._CP.TTS;

// ReSharper disable once InconsistentNaming
public partial class TTSSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    // TODO: make it with normal station announcement system.
    private readonly Queue<PlayTTSAudioData> _publicAudioQueue = [];
    private TimeSpan _endTime = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_endTime < _timing.CurTime)
        {
            if (_publicAudioQueue.TryDequeue(out var audioData))
            {
                PlayGlobalTTSAudio(audioData);
            }
        }

        var queue = EntityQueryEnumerator<TTSComponent>();

        while (queue.MoveNext(out var uid, out var component))
        {
            if (component.EndTime > _timing.CurTime)
            {
                continue;
            }

            if (component.Queue.TryDequeue(out var data))
            {
                PlayTTSAudio(uid, component, data);
            }
        }
    }

    private void QueueAudio(EntityUid? source, PlayTTSAudioData data)
    {
        if (source == null)
        {
            _sawmill.Verbose($"Add to queue public TTS audio.");
            _publicAudioQueue.Enqueue(data);
            return;
        }
        if (!TryComp<TTSComponent>(source, out var ttsComponent))
        {
            _sawmill.Warning($"Ignore TTS on {source} because it doesn't have TTS component!");
            return;
        }
        _sawmill.Verbose($"Add to queue TTS audio from {source} entity.");

        ttsComponent.Queue.Enqueue(data);
    }


    // ReSharper disable once InconsistentNaming
    private void PlayTTSAudio(EntityUid source, TTSComponent component, PlayTTSAudioData data)
    {
        if (TryComp<MobStateComponent>(source, out var mobStateComponent))
        {
            if (mobStateComponent.CurrentState != MobState.Alive)
            {
                return;
            }
        }

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, Prefix / data.Path);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(data.Effects.HasFlag(TTSEffects.Whisper)))
            .WithMaxDistance(AdjustDistance(data.Effects.HasFlag(TTSEffects.Whisper)));

        component.EndTime = _timing.CurTime + audioResource.AudioStream.Length;
        var audioEntry = _audio.PlayEntity(audioResource.AudioStream, source, audioParams);

        if (audioEntry != null)
        {
            component.AudioComponent = audioEntry.Value.Component;
        }

        _contentRoot.RemoveFile(data.Path);
    }

    // ReSharper disable once InconsistentNaming
    private void PlayGlobalTTSAudio(PlayTTSAudioData data)
    {
        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, Prefix / data.Path);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(data.Effects.HasFlag(TTSEffects.Whisper)))
            .WithMaxDistance(AdjustDistance(data.Effects.HasFlag(TTSEffects.Whisper)));

        _endTime = _timing.CurTime + audioResource.AudioStream.Length;
        _audio.PlayGlobal(audioResource.AudioStream, audioParams);

        _contentRoot.RemoveFile(data.Path);
    }

    private void OnMobStateChanged(EntityUid uid, TTSComponent component, MobStateChangedEvent args)
    {
        if (args.Component.CurrentState == MobState.Alive)
            return;

        _audio.Stop(null, component.AudioComponent);
        component.Queue.Clear();
    }
}
