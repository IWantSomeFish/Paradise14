using Content.Shared.Actions;
using Content.Shared.Modsuits.Components;
using Content.Shared.Modsuits.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared.Modsuits.System;

/// <summary>
/// System for handling modsuit actions and events, such as equipping, unequipping, and deploying the modsuit.
/// This system controls any actions with the modsuit.
/// </summary>
public sealed partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModsuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ModsuitComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ModsuitComponent, DeployModsuit>(OnAction);
    }
    /// <summary>
    /// Give deploy, power on and status panel access actions when the modsuit is equipped
    /// </summary>
    private void OnEquipped(EntityUid uid, ModsuitComponent component, GotEquippedEvent args)
    {
        _actions.AddAction(args.EquipTarget, ref component.ActionEntity, component.Action, uid);
    }
    /// <summary>
    /// Remove deploy, power on and status panel access actions when the modsuit is unequipped
    /// </summary>
    private void OnUnequipped(EntityUid uid, ModsuitComponent component, GotUnequippedEvent args)
    {
        _actions.RemoveAction(args.EquipTarget, component.ActionEntity);
        component.ActionEntity = null;
    }
    private void OnAction(EntityUid uid, ModsuitComponent component, DeployModsuit args)
    {
        args.Handled = true;
        _popup.PopupEntity($"Interacted", args.Performer);
    }
}
