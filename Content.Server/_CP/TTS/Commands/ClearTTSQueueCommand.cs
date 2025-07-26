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
        var entities = new List<NetEntity>();
        foreach (var uidString in args)
        {
            if (!NetEntity.TryParse(uidString, out var netEntity))
            {
                shell.WriteError(Loc.GetString("clear-tts-queue-command-entity-not-found", ("ent", uidString)));
                continue;
            }

            entities.Add(netEntity);
        }

        _entManager.System<TTSSystem>().ClearQueue(entities);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var entities = _entManager.GetEntities()
            .Select(c => new CompletionOption(c.ToString(), _entManager.EnsureComponent<MetaDataComponent>(c).EntityName));
        return CompletionResult.FromHintOptions(entities, Loc.GetString("clear-tts-queue-command-arg-entityn", ("entity", args.Length)));
    }
}
