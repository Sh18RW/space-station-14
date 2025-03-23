using System.Linq;
using Content.Server._CP.TTS.Events;
using Content.Server._CP.TTS.Systems;
using Content.Server.Administration;
using Content.Shared._CP.TTS;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CP.TTS.Commands;

[AdminCommand(AdminFlags.Fun)]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Command => "playtts";
    public string Description => Loc.GetString("play-tts-command-description");
    public string Help => Loc.GetString("play-tts-command-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("play-tts-command-error-too-low-args"));
            return;
        }

        var voiceProtoId = args[0];

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceProtoId, out var voicePrototype))
        {
            shell.WriteLine(Loc.GetString("play-tts-command-error-voice", ("voice", voiceProtoId)));
            return;
        }

        var text = args[1];
        var mode = args[2];

        switch (mode)
        {
            case "global":
                _entManager.System<TTSSystem>()
                    .OnPlayTTSRequest(
                        new PlayTTSRequestEvent(text,
                            voicePrototype,
                            Filter.Empty().AddAllPlayers()));
                return;
            case "sourced":
            {
                var sources = new List<EntityUid>();
                for (var i = 3; i < args.Length; i++)
                {
                    if (!NetEntity.TryParse(args[i], out var netEntity))
                    {
                        shell.WriteError(Loc.GetString("play-tts-command-error-entity-not-found", ("ent", args[i])));
                        continue;
                    }

                    if (_entManager.TryGetEntity(netEntity, out var ent))
                    {
                        sources.Add(ent.Value);
                    }
                }

                _entManager.System<TTSSystem>()
                    .OnPlayTTSRequest(
                        new PlayTTSRequestEvent(text,
                            voicePrototype,
                            null,
                            TTSEffects.Default,
                            sources));
                return;
            }
            case "local":
            {
                var receivers = new List<ICommonSession>();

                for (var i = 3; i < args.Length; i++)
                {
                    var playerLocator = await _locator.LookupIdByNameOrIdAsync(args[i]);

                    if (playerLocator == null)
                    {
                        shell.WriteError(Loc.GetString("play-tts-command-error-player-not-found", ("player", args[i])));
                        continue;
                    }

                    if (_playerManager.TryGetSessionById(playerLocator.UserId, out var player))
                    {
                        receivers.Add(player);
                    }
                }

                _entManager.System<TTSSystem>()
                    .OnPlayTTSRequest(
                        new PlayTTSRequestEvent(text,
                            voicePrototype,
                            Filter.Empty().AddPlayers(receivers)));
                return;
            }
            default:
                shell.WriteError(Loc.GetString("play-tts-command-error-mode-not-found", ("mode", mode)));
                break;
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var voices = _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>()
                .Select(v => new CompletionOption(v.ID, Loc.GetString(v.Name)));
            return CompletionResult.FromHintOptions(voices, Loc.GetString("play-tts-command-hint-voice"));
        }

        if (args.Length <= 2)
        {
            return CompletionResult.FromHint(Loc.GetString("play-tts-command-error-text"));
        }

        if (args.Length <= 3)
        {
            return CompletionResult.FromHintOptions(["sourced", "global", "local"],
                Loc.GetString("play-tts-command-hint-mode"));
        }

        var mode = args[2];

        switch (mode)
        {
            case "sourced":
            {
                var included = new List<EntityUid>();

                for (var i = 3; i < args.Length; i++)
                {
                    if (NetEntity.TryParse(args[i], out var netEntity) && _entManager.TryGetEntity(netEntity, out var ent))
                    {
                        included.Add(ent.Value);
                    }
                }

                var entities = _entManager.GetEntities()
                    .Except(included)
                    .Select(c => new CompletionOption(c.ToString(), _entManager.EnsureComponent<MetaDataComponent>(c).EntityName));
                return CompletionResult.FromHintOptions(entities, Loc.GetString("play-tts-command-hint-mode-sourced"));
            }
            case "local":
            {
                // ignore checking for already added players, as it can be slow.
                var options = _playerManager.Sessions
                    .Select(c => c.Name)
                    .OrderBy(c => c);
                return CompletionResult.FromHintOptions(options, Loc.GetString("play-tts-command-hint-mode-sourced"));
            }
            case "global":
                return CompletionResult.Empty;
            default:
                return CompletionResult.FromHint(Loc.GetString("play-tts-command-error-wrong"));
        }
    }
}
