using Content.Shared.Actions;
using Content.Shared.Modsuits.Components;
using Content.Shared.Modsuits.Events;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Content.Shared.Inventory;
using System.Linq;
using Content.Shared.Interaction.Components;

namespace Content.Shared.Modsuits;

/// <summary>
/// System for handling modsuit actions and events, such as equipping, unequipping, and deploying the modsuit.
/// This system controls any actions with the modsuit.
/// </summary>
public sealed partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModsuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ModsuitComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ModsuitComponent, DeployModsuit>(OnAction);
        SubscribeLocalEvent<ModsuitComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Spawns the modsuit parts into their respective containers when the modsuit is initialized on the map.
    /// </summary>
    private void OnMapInit(EntityUid uid, ModsuitComponent component, MapInitEvent args)
    {
        foreach (var (part, prototype) in component.Parts)
        {
            var container = _container.EnsureContainer<ContainerSlot>(
                uid,
                ModsuitContainers.GetPartContainer(part));

            if (container.ContainedEntity != null)
            {
                component.SpawnedParts[part] = container.ContainedEntity.Value;
                component.DeployedParts[part] = false;
                continue;
            }

            var entity = Spawn(prototype.ToString(), Transform(uid).Coordinates);

            if (!_container.Insert(entity, container))
            {
                Del(entity);
                continue;
            }

            component.SpawnedParts.TryAdd(part, entity);
            component.DeployedParts.TryAdd(part, false);
        }
    }
    /// <summary>
    /// Give deploy, power on and status panel access actions when the modsuit is equipped
    /// </summary>
    private void OnEquipped(EntityUid uid, ModsuitComponent component, GotEquippedEvent args)
    {
        _actions.AddAction(args.EquipTarget, ref component.ActionEntity, component.DeployAction, uid);
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
        Log.Info($"Deploying modsuit for {args.Performer}");
        args.Handled = true;
        foreach (var part in component.Parts.Keys.ToList())
        {

            if (!component.DeployedParts.TryGetValue(part, out var deployed))
                continue;

            if (deployed)
                RetractPart(args.Performer, uid, part, component);
            else
                DeployPart(args.Performer, uid, part, component);
        }
    }
    private void DeployPart(EntityUid wearer, EntityUid modsuit, ModsuitPartType part, ModsuitComponent component)
    {
        if (!component.SpawnedParts.TryGetValue(part, out var entity))
            return;

        var slot = ModsuitContainers.GetInventorySlot(part);

        if (!_inventory.TryEquip(wearer, entity, slot, force: true))
            return;

        component.DeployedParts[part] = true;
        EnsureComp<UnremoveableComponent>(entity);
        if (!HasComp<UnremoveableComponent>(modsuit))
        {
            EnsureComp<UnremoveableComponent>(modsuit);
        }
    }
    private void RetractPart(EntityUid wearer, EntityUid modsuit, ModsuitPartType part, ModsuitComponent component)
    {
        if (!component.SpawnedParts.TryGetValue(part, out var entity))
            return;

        RemComp<UnremoveableComponent>(entity);

        var slot = ModsuitContainers.GetInventorySlot(part);

        if (!_inventory.TryUnequip(modsuit, wearer, slot, force: true))
            return;
        var container = _container.EnsureContainer<ContainerSlot>(modsuit, ModsuitContainers.GetPartContainer(part));
        if (!_container.Insert(entity, container))
            return;

        component.DeployedParts[part] = false;

        if (!component.DeployedParts.Values.Any(x => x))
            RemComp<UnremoveableComponent>(modsuit);
    }
}
