using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public enum VoiceMaskUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class VoiceMaskBuiState(string name, string voice, string? verb) : BoundUserInterfaceState
{
    public readonly string Name = name;
    public readonly string? Verb = verb;
    public readonly string Voice = voice; // CP-TTS
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeNameMessage(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}

/// <summary>
/// Change the speech verb prototype to override, or null to use the user's verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVerbMessage(string? verb) : BoundUserInterfaceMessage
{
    public readonly string? Verb = verb;
}
