using Content.Shared.Actions;
using Content.Shared.Modsuits.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Modsuits.Events;

public sealed partial class DeployModsuit : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed class ModsuitSystemMessage(ModsuitPartType part) : BoundUserInterfaceMessage
{
    public ModsuitPartType Part = part;
}
