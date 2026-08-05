using Content.Shared.Actions;
using Content.Shared.Modsuits.Components;
using Content.Shared.Modsuits.Events;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Content.Shared.Inventory;
using System.Linq;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Content.Shared.Toggleable;
using Content.Shared.DoAfter;

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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModsuitComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ModsuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ModsuitComponent, GotUnequippedEvent>(OnUnequipped);
        /// events for UI handle
        SubscribeLocalEvent<ModsuitComponent, DeployModsuit>(OpenRadialUI);
        SubscribeLocalEvent<ModsuitComponent, ModsuitSystemMessage>(OnSystemMessage);
        SubscribeLocalEvent<ModsuitComponent, PowerModsuit>(OnActivate);
        // events for modsuit state change
        SubscribeLocalEvent<ModsuitComponent, DeployPartEvent>(DeployPart);
        SubscribeLocalEvent<ModsuitComponent, RetractPartEvent>(RetractPart);
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
            Dirty(uid, component);
        }
    }
    /// <summary>
    /// Give deploy, power on and status panel access actions when the modsuit is equipped
    /// </summary>
    private void OnEquipped(EntityUid uid, ModsuitComponent component, GotEquippedEvent args)
    {
        foreach (var action in component.ActionEndpoints)
        {
            EntityUid? entity = null;
            if (_actions.AddAction(args.EquipTarget, ref entity, out _, action, uid))
                component.ActionEntities.Add(entity.Value);
            Dirty(uid, component);
        }
    }
    /// <summary>
    /// Remove deploy, power on and status panel access actions when the modsuit is unequipped
    /// </summary>
    private void OnUnequipped(EntityUid uid, ModsuitComponent component, GotUnequippedEvent args)
    {
        foreach (var action in component.ActionEntities)
        {
            _actions.RemoveAction(args.EquipTarget, action);
            Dirty(uid, component);
        }

        component.ActionEntities.Clear();
    }
    private void OnActivate(EntityUid uid, ModsuitComponent component, PowerModsuit args)
    {
        if (_net.IsClient)
            return;
        args.Handled = true;
        var delay = 0;
        foreach (var partKey in component.SpawnedParts.Keys)
        {
            if (!component.DeployedParts[partKey])
                continue;
            var currentDelay = delay;
            Timer.Spawn(TimeSpan.FromSeconds(currentDelay), () =>
            {
                var part = component.SpawnedParts[partKey];
                _appearance.SetData(part, ToggleableVisuals.Enabled, component.PowerOn);
                _audio.PlayPvs(component.DeploySound, uid, AudioParams.Default.WithVolume(-2f));
            });
            delay += component.ActivateDelay;
        }
        Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                _appearance.SetData(uid, ToggleableVisuals.Enabled, component.PowerOn);
                _audio.PlayPvs(component.PowerOnSound, uid, AudioParams.Default.WithVolume(-2f));
            });
        component.PowerOn = !component.PowerOn;
        Dirty(uid, component);
    }
    private void OpenRadialUI(EntityUid uid, ModsuitComponent component, DeployModsuit args)
    {
        args.Handled = true;
        _ui.OpenUi(uid, ModsuitUiKey.Radial, args.Performer);
    }
    private void OnSystemMessage(EntityUid uid, ModsuitComponent component, ModsuitSystemMessage args)
    {
        DoAfterEvent doAfter;
        if (component.DeployedParts.TryGetValue(args.Part, out var deployed) && deployed)
        {
            doAfter = new RetractPartEvent(GetNetEntity(args.Actor), GetNetEntity(uid), args.Part);
        }
        else
            doAfter = new DeployPartEvent(GetNetEntity(args.Actor), GetNetEntity(uid), args.Part);
        var doAfterArgs = new DoAfterArgs(EntityManager, args.Actor, TimeSpan.FromSeconds(component.PowerOn ? component.ActivateDelay : 0), doAfter, uid);
        _doAfter.TryStartDoAfter(doAfterArgs);
    }
    private void DeployPart(EntityUid uid, ModsuitComponent component, DeployPartEvent args)
    {
        if (_net.IsClient)
            return;
        if (!component.SpawnedParts.TryGetValue(args.Part, out var entity))
            return;
        var perfomer = GetEntity(args.Performer);
        var slot = ModsuitContainers.GetInventorySlot(args.Part);
        if (ModsuitContainers.TryGetStorageContainer(args.Part, out var storageName))
        {
            if (_inventory.TryGetSlotEntity(perfomer, slot, out var oldItem) && oldItem != null)
            {
                if (_inventory.TryUnequip(perfomer, perfomer, slot, force: true))
                {
                    if (_container.TryGetContainer(uid, storageName, out var storage))
                        _container.Insert(oldItem.Value, storage);
                }
            }
        }

        if (!_inventory.TryEquip(perfomer, entity, slot, force: true))
        {
            if (_inventory.TryGetSlotEntity(perfomer, slot, out var oldItem) && oldItem != null)
            {
                _audio.PlayPvs(component.ErrorSound, uid, AudioParams.Default.WithVolume(-2f));
            }
            return;
        }

        _audio.PlayPvs(component.DeploySound, uid, AudioParams.Default.WithVolume(-2f));
        component.DeployedParts[args.Part] = true;
        EnsureComp<UnremoveableComponent>(entity);

        if (!HasComp<UnremoveableComponent>(uid))
        {
            EnsureComp<UnremoveableComponent>(uid);
        }
    }
    private void RetractPart(EntityUid uid, ModsuitComponent component, RetractPartEvent args)
    {
        if (_net.IsClient)
            return;
        if (!component.SpawnedParts.TryGetValue(args.Part, out var entity))
            return;
        var perfomer = GetEntity(args.Performer);
        RemComp<UnremoveableComponent>(entity);

        var slot = ModsuitContainers.GetInventorySlot(args.Part);

        if (!_inventory.TryUnequip(uid, perfomer, slot, force: true))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(uid, ModsuitContainers.GetPartContainer(args.Part));

        if (!_container.Insert(entity, container))
            return;

        if (ModsuitContainers.TryGetStorageContainer(args.Part, out var storageName))
        {
            if (!_container.TryGetContainer(uid, storageName, out var baseContainer))
                return;
            if (baseContainer is not ContainerSlot storage)
                return;
            if (storage.ContainedEntity != null)
            {
                var oldItem = storage.ContainedEntity.Value;
                _container.Remove(oldItem, storage);
                _inventory.TryEquip(perfomer, oldItem, slot, force: true);
            }
        }
        _audio.PlayPvs(component.DeploySound, perfomer, AudioParams.Default.WithVolume(-2f));
        component.DeployedParts[args.Part] = false;

        if (!component.DeployedParts.Values.Any(x => x))
            RemComp<UnremoveableComponent>(uid);
    }
}
