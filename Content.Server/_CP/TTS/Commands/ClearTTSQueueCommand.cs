using System.Linq;
using Content.Server._CP.TTS.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CP.TTS.Commands;

[AdminCommand(AdminFlags.Moderator)]
// ReSharper disable once InconsistentNaming
public sealed class ClearTTSQueueCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    public string Command => "clearttsqueue";
    public string Description => Loc.GetString("clear-tts-queue-command-description");
    public string Help => Loc.GetString("clear-tts-queue-command-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entities = new List<EntityUid>();
        foreach (var uidString in args)
        {
            if (!(NetEntity.TryParse(uidString, out var netEntity) && _entManager.TryGetEntity(netEntity, out var entity)))
            {
                shell.WriteError(Loc.GetString("clear-tts-queue-command-entity-not-found", ("ent", uidString)));
                continue;
            }

            entities.Add(entity.Value);
        }

        _entManager.System<TTSSystem>().ClearQueue(entities);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var entities = _entManager.GetEntities().Select(c => c.ToString());
        return CompletionResult.FromHintOptions(entities, Loc.GetString("clear-tts-queue-command-arg-entityn", ("entity", args.Length)));
    }
}
