using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Corvax.TTS;
using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _broadPhase = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private float _volume = 0.0f;
    private int _fileIdx = 0;

    private readonly HashSet<AudioStream> _currentStreams = new();
    private readonly Dictionary<EntityUid, Queue<AudioStream>> _entityQueues = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _resourceCache.AddRoot(Prefix, _contentRoot);
        _cfg.OnValueChanged(CCVars.TTSVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.TTSVolume, OnTtsVolumeChanged);
        _contentRoot.Dispose();
    }

    // Little bit of duplication logic from AudioSystem
    public override void FrameUpdate(float frameTime)
    {
        var streamToRemove = new HashSet<AudioStream>();

        var ourPos = _eye.CurrentEye.Position.Position;
        foreach (var stream in _currentStreams)
        {
            if (!stream.Source.IsPlaying ||
                !_entity.TryGetComponent<MetaDataComponent>(stream.Uid, out var meta) ||
                Deleted(stream.Uid, meta) ||
                !_entity.TryGetComponent<TransformComponent>(stream.Uid, out var xform))
            {
                stream.Source.Dispose();
                streamToRemove.Add(stream);
                continue;
            }

            var mapPos = xform.MapPosition;
            if (mapPos.MapId != MapId.Nullspace)
            {
                if (!stream.Source.SetPosition(mapPos.Position))
                {
                    _sawmill.Warning("Can't set position for audio stream, stop stream.");
                    stream.Source.StopPlaying();
                }
            }

            if (mapPos.MapId == _eye.CurrentMap)
            {
                var collisionMask = (int) CollisionGroup.Impassable;
                var sourceRelative = ourPos - mapPos.Position;
                var occlusion = 0f;
                if (sourceRelative.Length() > 0)
                {
                    occlusion = _broadPhase.IntersectRayPenetration(mapPos.MapId,
                        new CollisionRay(mapPos.Position, sourceRelative.Normalized(), collisionMask),
                        sourceRelative.Length(), stream.Uid);
                }
                stream.Source.SetOcclusion(occlusion);
            }
        }

        foreach (var audioStream in streamToRemove)
        {
            _currentStreams.Remove(audioStream);
            ProcessEntityQueue(audioStream.Uid);
        }
    }

    public void RequestGlobalTTS(string text, string voiceId)
    {
        RaiseNetworkEvent(new RequestGlobalTTSEvent(text, voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Debug($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var volume = _volume;
        if (ev.IsWhisper)
            volume -= 4;

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioParams = AudioParams.Default.WithVolume(volume);
        var soundPath = new SoundPathSpecifier(Prefix / filePath, audioParams);
        if (ev.SourceUid != null)
        {
            var sourceUid = GetEntity(ev.SourceUid.Value);
            _audio.PlayEntity(soundPath, new EntityUid(), sourceUid); // recipient arg ignored on client
        }
        else
        {
            _audio.PlayGlobal(soundPath, Filter.Local(), false);
        }

        _contentRoot.RemoveFile(filePath);
    }

    private void AddEntityStreamToQueue(AudioStream stream)
    {
        if (_entityQueues.TryGetValue(stream.Uid, out var queue))
        {
            queue.Enqueue(stream);
        }
        else
        {
            _entityQueues.Add(stream.Uid, new Queue<AudioStream>(new[] { stream }));

            if (!IsEntityCurrentlyPlayStream(stream.Uid))
                ProcessEntityQueue(stream.Uid);
        }
    }

    private bool IsEntityCurrentlyPlayStream(EntityUid uid)
    {
        return _currentStreams.Any(s => s.Uid == uid);
    }

    private void ProcessEntityQueue(EntityUid uid)
    {
        if (TryTakeEntityStreamFromQueue(uid, out var stream))
            PlayEntity(stream);
    }

    private bool TryTakeEntityStreamFromQueue(EntityUid uid, [NotNullWhen(true)] out AudioStream? stream)
    {
        if (_entityQueues.TryGetValue(uid, out var queue))
        {
            stream = queue.Dequeue();
            if (queue.Count == 0)
                _entityQueues.Remove(uid);
            return true;
        }

        stream = null;
        return false;
    }

    private void PlayEntity(AudioStream stream)
    {
        if (!_entity.TryGetComponent<TransformComponent>(stream.Uid, out var xform) ||
            !stream.Source.SetPosition(xform.WorldPosition))
            return;

        stream.Source.StartPlaying();
        _currentStreams.Add(stream);
    }

    private void EndStreams()
    {
        foreach (var stream in _currentStreams)
        {
            stream.Source.StopPlaying();
            stream.Source.Dispose();
        }

        _currentStreams.Clear();
        _entityQueues.Clear();
    }

    // ReSharper disable once InconsistentNaming
    private sealed class AudioStream
    {
        public EntityUid Uid { get; }
        public IClydeAudioSource Source { get; }

        public AudioStream(EntityUid uid, IClydeAudioSource source)
        {
            Uid = uid;
            Source = source;
        }
    }
}
