using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modsuits.Components;

/// <summary>
/// Component that holds modsuit specific data, such as the action entity and the action prototype ID.
/// Modular components can be added to the modsuit entity to provide additional functionality, such as deploying the modsuit or accessing the status panel.
/// </summary>
[RegisterComponent]
public sealed partial class ModsuitComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionModsuitWheel";

    public EntityUid? ActionEntity;
}
