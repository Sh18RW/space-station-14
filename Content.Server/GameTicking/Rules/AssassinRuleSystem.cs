using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

public sealed class AssassinRuleSystem : GameRuleSystem<AssassinRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);

        SubscribeLocalEvent<AssassinRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<AssassinRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var assassinRule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            List<ICommonSession> toDelete = new();

            foreach (var assassin in assassinRule.MakeTargetAssassins)
            {
                if (!_mindSystem.TryGetMind(assassin, out var mindId, out var mind))
                {
                    assassinRule.MakeTargetAssassins.Remove(assassin);
                    return;
                }

                if (!TryComp<AssassinRoleComponent>(mindId, out var assassinRole))
                {
                    Log.Error("Player doesn't assassin!");
                    assassinRule.MakeTargetAssassins.Remove(assassin);
                    return;
                }

                if (AddTarget(mindId, mind, ref assassinRule, ref assassinRole))
                {
                    _roleSystem.MindPlaySound(mindId, assassinRule.NewTargetSound, mind);
                }

                if (assassinRole.Targets.Count >= assassinRule.TargetsCount)
                    toDelete.Add(assassin);
            }

            foreach (var assassin in toDelete)
            {
                assassinRule.MakeTargetAssassins.Remove(assassin);
            }
        }
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, AssassinRuleComponent component, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = component.Assassins;
        args.AgentName = Loc.GetString("assassin-round-end-agent-name");
    }

    public void MakeAssassin(ICommonSession assassin)
    {
        var assassinRule = EntityQuery<AssassinRuleComponent>().FirstOrDefault();
        if (assassinRule == null)
        {
            GameTicker.StartGameRule("Assassin", out var rule);
            assassinRule = Comp<AssassinRuleComponent>(rule);
        }

        if (!_mindSystem.TryGetMind(assassin, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked assassin.");
            return;
        }

        if (HasComp<AssassinRoleComponent>(mindId))
        {
            Log.Error($"Player {assassin.Name} is already an assassin!");
            return;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Log.Error("Mind picked for assassin did not have an attached entity.");
            return;
        }

        var assassinRole = new AssassinRoleComponent
        {
            PrototypeId = assassinRule.AssassinPrototypeId,
        };

        _roleSystem.MindAddRole(mindId, assassinRole, mind);

        _roleSystem.MindPlaySound(mindId, assassinRule.GreetSoundNotification, mind);
        SendAssassinBriefing(mindId);
        assassinRule.Assassins.Add(mindId);

        _npcFaction.RemoveFaction(entity, "NanoTrasen");
        _npcFaction.AddFaction(entity, "Syndicate");

        for (var picks = 0; picks < assassinRule.TargetsCount; picks++)
        {
            AddTarget(mindId, mind, ref assassinRule, ref assassinRole);
        }

        if (assassinRole.Targets.Count != assassinRule.TargetsCount)
        {
            assassinRule.MakeTargetAssassins.Add(assassin);
        }

        var escapeObjective =
            _objectives.GetObjectiveByPrototypeId(mindId, mind, assassinRule.EscapeObjectivePrototypeId);
        if (escapeObjective != null)
        {
            _mindSystem.AddObjective(mindId, mind, escapeObjective.Value);
        }
    }

    private void SendAssassinBriefing(EntityUid mind)
    {
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        _chatManager.DispatchServerMessage(session, Loc.GetString("assassin-role-greeting"));
    }

    private bool AddTarget(EntityUid mindId,
        MindComponent mind,
        ref AssassinRuleComponent assassinRule,
        ref AssassinRoleComponent assassinRole)
    {
        var objective = _objectives.GetObjectiveByPrototypeId(mindId, mind, assassinRule.KillObjectivePrototypeId, assassinRole.Targets);
        if (objective == null)
            return false;

        _mindSystem.AddObjective(mindId, mind, objective.Value);
        return true;
    }
}
