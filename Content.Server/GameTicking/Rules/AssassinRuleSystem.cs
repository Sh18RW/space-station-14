using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class AssassinRuleSystem : GameRuleSystem<AssassinRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EntityManager _ent = default!;

    private int PlayerPerAssassin => _cfg.GetCVar(CCVars.AssassinDifficulty);
    private int MaxAssassins => _cfg.GetCVar(CCVars.AssassinMaxCount);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);

        SubscribeLocalEvent<AssassinRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<AssassinRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var assassin, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                assassin.Candidates[player] = ev.Profiles[player.UserId];
            }

            TryMakeAssassin(assassin);
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<AssassinRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var assassin, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cfg.GetCVar(CCVars.AssassinMinPlayerCount);

            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                // TODO: localization
                _chatManager.SendAdminAnnouncement("Min players count is not ready to start assassins!");
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement("No player is ready to start assassins!");
                ev.Cancel();
            }
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<AssassinRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var assassinRule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            // Try make new assassin

            if (assassinRule.Assassins.Count < MaxAssassins)
            {
                if (ev.LateJoin
                    && ev.Profile.AntagPreferences.Contains(assassinRule.AssassinPrototypeId)
                    && ev.JobId != null
                    && _prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job)
                    && job.CanBeAntag)
                {
                    MakeAssassin(ev.Player);

                    break;
                }
            }

            List<ICommonSession> toDelete = new();

            foreach (var assassin in  assassinRule.MakeTargetAssassins)
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

    private List<ICommonSession> FindPotentialAssassin(
        in Dictionary<ICommonSession, HumanoidCharacterProfile> candidates, AssassinRuleComponent component)
    {
        var list = new List<ICommonSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            if (!_jobs.CanBeAntag(player))
            {
                continue;
            }

            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
            {
                continue;
            }

            list.Add(player);
        }
        var prefList = new List<ICommonSession>();

        foreach (var player in list)
        {
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(component.AssassinPrototypeId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            Log.Info("Insufficient preferred assassin, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    public void TryMakeAssassin(AssassinRuleComponent component)
    {
        if (!component.Candidates.Any())
        {
            Log.Error("Nobody in Candidates dict to make assassin.");
            return;
        }

        var assassinCount = MathHelper.Clamp(component.Candidates.Count / PlayerPerAssassin, 1, MaxAssassins);
        var assassinPool = FindPotentialAssassin(component.Candidates, component);

        component.NextAssassinPlayerAmount = component.Candidates.Count - PlayerPerAssassin * assassinCount;

        for (var i = 0; i < assassinCount; i++)
        {
            var player = _random.PickAndTake(assassinPool);
            MakeAssassin(player);
        }
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

        var implantSystem = _ent.System<SharedSubdermalImplantSystem>();
        implantSystem.AddImplants(entity, assassinRule.Implants);

        _tag.AddTag(entity, assassinRule.AssassinCraftTag);

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
