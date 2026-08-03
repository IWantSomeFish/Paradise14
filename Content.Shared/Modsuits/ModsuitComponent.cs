using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Modsuits.Components;

/// <summary>
/// Enum representing the different parts of a modsuit. Each part can have its own functionality and can be deployed or retracted independently.
/// </summary>
public enum ModsuitPartType
{
    Helmet,
    Chest,
    Gloves,
    Boots
}

[Serializable, NetSerializable]
public enum ModsuitUiKey
{
    Radial
}

/// <summary>
/// Component that holds modsuit specific data, such as the action entity and the action prototype ID.
/// Modular components can be added to the modsuit entity to provide additional functionality, such as deploying the modsuit or accessing the status panel.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitComponent : Component
{
    [DataField]
    public EntProtoId DeployAction = "ActionModsuitWheel";
    [DataField]
    public Dictionary<ModsuitPartType, EntProtoId> Parts = new();
    [DataField]
    public SoundSpecifier DeploySound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");
    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
    [ViewVariables, AutoNetworkedField]
    public Dictionary<ModsuitPartType, bool> DeployedParts = new();
    [ViewVariables, AutoNetworkedField]
    public Dictionary<ModsuitPartType, EntityUid> SpawnedParts = new();
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ActionEntity;
    [ViewVariables, AutoNetworkedField]
    public bool PowerOn = false;
}
