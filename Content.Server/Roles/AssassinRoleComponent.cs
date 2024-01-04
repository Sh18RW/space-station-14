using Content.Shared.Roles;

namespace Content.Server.Roles;

[RegisterComponent]
public sealed partial class AssassinRoleComponent : AntagonistRoleComponent
{
    [DataField("targets")]
    public List<EntityUid> Targets = new();
}
