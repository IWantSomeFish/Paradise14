using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modsuits.Components;

public sealed partial class ModsuitPartData : Component
{
    [DataField]
    public EntProtoId ProtoID = default!;
    public bool Deployed = false;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ModsuitPartsComponent : Component
{
    [DataField(required: true)]
    public ModsuitPartData Helmet = default!;

    [DataField(required: true)]
    public ModsuitPartData Chest = default!;

    [DataField(required: true)]
    public ModsuitPartData Gloves = default!;

    [DataField(required: true)]
    public ModsuitPartData Boots = default!;
}
