using Robust.Shared.Serialization;

namespace Content.Shared.Ame;

[Virtual]
public partial class SharedAmeControllerComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class AmeControllerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasPower;
    public readonly bool IsMaster;
    public readonly bool Injecting;
    public readonly bool HasFuelJar;
    public readonly bool IsSecureInjecting;
    public readonly bool IsUnlimitedFuel;
    public readonly int FuelAmount;
    public readonly int InjectionAmount;
    public readonly int CoreCount;
    public readonly float CurrentPowerSupply;
    public readonly float TargetedPowerSupply;

    public AmeControllerBoundUserInterfaceState(bool hasPower, bool isMaster, bool injecting, bool hasFuelJar, bool isSecureInjecting, bool isUnlimitedFuel, int fuelAmount, int injectionAmount, int coreCount, float currentPowerSupply, float targetedPowerSupply)
    {
        HasPower = hasPower;
        IsMaster = isMaster;
        Injecting = injecting;
        HasFuelJar = hasFuelJar;
        IsSecureInjecting = isSecureInjecting;
        IsUnlimitedFuel = isUnlimitedFuel;
        FuelAmount = fuelAmount;
        InjectionAmount = isSecureInjecting ? coreCount * 2 : injectionAmount;
        CoreCount = coreCount;
        CurrentPowerSupply = currentPowerSupply;
        TargetedPowerSupply = targetedPowerSupply;
    }
}

[Serializable, NetSerializable]
public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly UiButton Button;

    public UiButtonPressedMessage(UiButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public enum AmeControllerUiKey
{
    Key
}

public enum UiButton
{
    Eject,
    ToggleInjection,
    IncreaseFuel,
    DecreaseFuel,
}

[Serializable, NetSerializable]
public enum AmeControllerVisuals
{
    DisplayState,
}

[Serializable, NetSerializable]
public enum AmeControllerState
{
    On,
    Critical,
    Fuck,
    Off,
}
