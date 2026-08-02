using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
/// <summary>
/// Component that holds modsuit specific data, such as the action entity and the action prototype ID.
/// Modular components can be added to the modsuit entity to provide additional functionality, such as deploying the modsuit or accessing the status panel.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ModsuitComponent : Component
{
    [DataField]
    public EntProtoId DeployAction = "ActionModsuitWheel";

    [DataField]
    public Dictionary<ModsuitPartType, EntProtoId> Parts = new();
    [ViewVariables]
    public Dictionary<ModsuitPartType, bool> DeployedParts = new();
    [ViewVariables]
    public Dictionary<ModsuitPartType, EntityUid> SpawnedParts = new();

    public EntityUid? ActionEntity;
    public bool PowerOn = false;
}
