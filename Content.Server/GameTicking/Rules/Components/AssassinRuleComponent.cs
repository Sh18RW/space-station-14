using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, Access(typeof(AssassinRuleSystem))]
public sealed partial class AssassinRuleComponent : Component
{
    public readonly List<EntityUid> Assassins = new();
    public readonly List<ICommonSession> MakeTargetAssassins = new();

    public readonly string KillObjectivePrototypeId = "AssassinKillRandomHeadObjective";
    public readonly string EscapeObjectivePrototypeId = "AssassinEscapeShuttleObjective";

    [DataField("assassinPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string AssassinPrototypeId = "Assassin";

    [DataField("assassinTargetsCount")]
    public int TargetsCount = 2;

    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
    [DataField("newTargetSound")]
    public SoundSpecifier NewTargetSound = new SoundPathSpecifier("/Audio/Ambience/Antag/assassin_new_target.ogg");

    public Dictionary<ICommonSession, HumanoidCharacterProfile> Candidates = new();
    public int NextAssassinPlayerAmount = 0;
}
