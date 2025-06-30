using System.Linq;
using Content.Shared._CP.TTS;
using Content.Shared._CP.TTS.Components;
using Content.Shared._CP.TTS.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._CP.TTS;

// ReSharper disable once InconsistentNaming
public partial class TTSSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    // TODO: make it with a station announcement system.
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

        if (!_entityManager.EntityExists(source))
        {
            _sawmill.Verbose($"Ignore TTS because {source} entity doesn't exist.");
            _contentRoot.RemoveFile(data.Path);
            return;
        }

        if (!TryComp<TTSComponent>(source, out var ttsComponent))
        {
            _sawmill.Warning($"Ignore TTS on {source} because it doesn't have TTS component!");
            _contentRoot.RemoveFile(data.Path);
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

        var soundPath = new SoundPathSpecifier(Prefix / data.Path);
        // TODO: make resolving sound path
        // var resolvedAudio = _audio.ResolveSound(soundPath); Ups, for some reason _audio.ResolveSound does not exist

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(data.Effects.HasFlag(TTSEffects.Whisper)))
            .WithMaxDistance(AdjustDistance(data.Effects.HasFlag(TTSEffects.Whisper)));

        component.EndTime = _timing.CurTime + _audio.GetAudioLength(soundPath.Path.CanonPath);
        var audioEntry =
            _audio.PlayEntity(soundPath, Filter.Local(), source, true, audioParams);

        if (audioEntry != null)
        {
            component.Audio = (audioEntry.Value.Entity, audioEntry.Value.Component);
        }

        _contentRoot.RemoveFile(data.Path);
    }

    // ReSharper disable once InconsistentNaming
    private void PlayGlobalTTSAudio(PlayTTSAudioData data)
    {
        var soundPath = new SoundPathSpecifier(Prefix / data.Path);

        // TODO: make resolving sound path
        // var resolvedAudio = _audio.ResolveSound(soundPath);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(data.Effects.HasFlag(TTSEffects.Whisper)))
            .WithMaxDistance(AdjustDistance(data.Effects.HasFlag(TTSEffects.Whisper)));

        _endTime = _timing.CurTime + _audio.GetAudioLength(soundPath.Path.CanonPath);
        _audio.PlayGlobal(soundPath, Filter.Local(), true, audioParams);

        _contentRoot.RemoveFile(data.Path);
    }

    private void OnMobStateChanged(EntityUid uid, TTSComponent component, MobStateChangedEvent args)
    {
        if (args.Component.CurrentState == MobState.Alive)
            return;

        _audio.Stop(null, component.Audio?.component);
        component.Queue.Clear();
    }

    private void OnClearTTSQueue(ClearTTSQueueEvent ev)
    {
        if (ev.Sources.Count != 0)
        {
            foreach (var entityUid in ev.Sources.Select(netEntity => _entityManager.GetEntity(netEntity)))
            {
                if (!TryComp<TTSComponent>(entityUid, out var component))
                    continue;

                _audio.Stop(null, component.Audio?.component);
                component.Queue.Clear();
            }

            return;
        }

        var queue = EntityQueryEnumerator<TTSComponent>();

        while (queue.MoveNext(out _, out var component))
        {
            _audio.Stop(null, component.Audio?.component);
            component.Queue.Clear();
        }
    }
}
