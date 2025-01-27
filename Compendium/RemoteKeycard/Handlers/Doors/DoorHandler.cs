using System;
using System.Collections.Generic;
using System.Linq;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Extensions;
using Compendium.Features;
using Compendium.Messages;
using helpers;
using helpers.Attributes;
using helpers.Configuration;
using helpers.Random;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PluginAPI.Events;
using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers.Doors;

[ConfigCategory(Name = "Door")]
public static class DoorHandler
{
	public const int Mask = -73729;

	[Config(Name = "Enabled", Description = "Whether or not to enable remote keycard interactions for doors.")]
	public static bool IsEnabled { get; set; }

	[Config(Name = "Shot", Description = "Whether or not to allow opening doors by shooting them.")]
	public static bool AllowShot { get; set; }

	[Config(Name = "Failure Chance", Description = "The chance for a remote keycard interaction to fail.")]
	public static int FailureChance { get; set; }

	[Config(Name = "Usable Chance", Description = "The chance for a door to work after being destroyed.")]
	public static int UsableChance { get; set; }

	[Config(Name = "Base Damage", Description = "Base damage that firearm deal to doors.")]
	public static float BaseDamage { get; set; }

	[Config(Name = "Failure Hint", Description = "The hint to display when a door interaction fails.")]
	public static HintMessage FailureHint { get; set; }

	public static event Func<ReferenceHub, GameObject, bool> OnRaycast;

	public static event Func<ReferenceHub, bool> OnZombieAttack;

	[Load]
	private static void Load()
	{
		ShootHandler.OnHit += OnHit;
		OnZombieAttack += OnZombieAttackHandler;
	}

	[Unload]
	private static void Unload()
	{
		ShootHandler.OnHit -= OnHit;
		OnZombieAttack -= OnZombieAttackHandler;
	}

	private static bool CanInteractOverride(DoorVariant target, ReferenceHub ply)
	{
		if (!RoundSwitches.IsDoorDisabled)
		{
			if (!DoorDamageHandler.ProcessDamage(ply, target) && !ply.serverRoles.BypassMode)
			{
				DoorDamageHandler.DamageAction(ply, target);
				return false;
			}
			if (FailureChance > 0 && WeightedRandomGeneration.Default.GetBool(FailureChance))
			{
				if (FailureHint != null && FailureHint.IsValid)
				{
					FailureHint.Send(ply);
				}
				return false;
			}
		}
		if (target.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
		{
			DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)target.ActiveLocks);
			if ((!mode.HasFlagFast(DoorLockMode.CanClose) || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP()) && (mode == DoorLockMode.FullLock || (target.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) || (!target.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
			{
				if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, target, canOpen: false)))
				{
					return false;
				}
				target.LockBypassDenied(ply, 0);
				return false;
			}
		}
		if (!target.AllowInteracting(ply, 0))
		{
			return false;
		}
		bool flag = ply.GetRoleId() == RoleTypeId.Scp079 || target.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply);
		if (!RoundSwitches.IsDoorDisabled && !flag)
		{
			flag = AccessUtils.CanAccessDoor(target, ply);
		}
		return EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, target, flag)) && flag;
	}

	[RoundStateChanged(new RoundState[] { RoundState.InProgress })]
	private static void OnRoundStart()
	{
		if (IsEnabled)
		{
			DoorVariant.AllDoors.ForEach(delegate(DoorVariant d)
			{
				Door.Override(d, CanInteractOverride);
			});
		}
	}

	private static bool OnZombieAttackHandler(ReferenceHub ply)
	{
		try
		{
			if (!Physics.Raycast(ply.PlayerCameraReference.position, ply.PlayerCameraReference.forward, out var hitInfo, 20f, -73729))
			{
				return true;
			}
			Transform transform = hitInfo.transform;
			GameObject gameObject;
			if (transform == null)
			{
				gameObject = null;
			}
			else
			{
				Transform parent = transform.parent;
				gameObject = ((parent != null) ? parent.gameObject : null);
			}
			GameObject gameObject2 = gameObject ?? hitInfo.transform.gameObject;
			if (gameObject2 == null)
			{
				return true;
			}
			if (UnityExtensions.TryGet<DoorVariant>(gameObject2, out var result) || (UnityExtensions.TryGet<RegularDoorButton>(gameObject2, out var result2) && (result = result2.Target as DoorVariant) != null))
			{
				DoorDamageHandler.DoDamage(ply, 0f, result, DoorDamageSource.Zombie);
			}
		}
		catch (StackOverflowException)
		{
		}
		catch (Exception message)
		{
			Plugin.Error(message);
			return false;
		}
		return true;
	}

	//return true if interracted with door or elevator
	private static bool OnHit(ReferenceHub ply, GameObject target)
	{
		if (!IsEnabled || !AllowShot)
			return false;
			/*
			if (!Physics.Raycast(ply.PlayerCameraReference.position, ply.PlayerCameraReference.forward, out var hitInfo, 70f, -73729)
			  || hitInfo.collider == null) {
				return true;
			}
			*/

        Transform transform = target.transform;
		if (transform == null || transform.gameObject == null) {
			return false;
		}
		DoorVariant door;

        if ((transform.TryGetComponent<RegularDoorButton>(out var doorButton) || (transform.parent != null && transform.parent.TryGetComponent(out doorButton))) &&
			(door = doorButton.GetComponentInParent<DoorVariant>()) != null)
		{
			if (!Door.IsInteractable(door)) {
				return true;
			}
			/* if (BaseDamage > 0f) {
				DoorDamageHandler.DoDamage(ply, BaseDamage / UnityExtensions.DistanceSquared(ply, door) * 10f, door, DoorDamageSource.Firearm);
			} */
			door.ServerInteract(ply, 0);
			return true;
		} else if (transform.TryGetComponent<ElevatorPanel>(out var panel) &&
			panel._assignedChamber != null &&
			panel._assignedChamber.IsReady &&
			ElevatorDoor.AllElevatorDoors.TryGetValue(panel._assignedChamber.AssignedGroup, out var doors) &&
			(int)DoorLockUtils.GetMode(panel._assignedChamber.ActiveLocksAllDoors) > 0)
		{
			if (DecontaminationController.Singleton != null &&
				DecontaminationController.Singleton._decontaminationBegun &&
				doors != null &&
				doors.Any((ElevatorDoor d) => d.IsInZone(FacilityZone.LightContainment)))
			{
				return true;
			}

			if (AlphaWarheadController.Detonated) {
				return true;
			}
			panel._assignedChamber.ServerSetDestination(panel._assignedChamber.NextLevel, true);
			// NetworkServer.SendToReady<ElevatorSyncMsg>(new ElevatorSyncMsg(component2.AssignedChamber.AssignedGroup, component2.AssignedChamber.CurrentLevel), 0);
			// ElevatorManager.SyncedDestinations[component2.AssignedChamber.AssignedGroup] = component2.AssignedChamber.CurrentLevel;
			return true;
		}

		/* if (DoorHandler.OnRaycast != null) {
			return DoorHandler.OnRaycast(ply, transform.gameObject);
		} */

		return false;
	}

	static DoorHandler()
	{
		IsEnabled = true;
		AllowShot = true;
		FailureChance = 5;
		UsableChance = 60;
		BaseDamage = 50f;
		FailureHint = HintMessage.Create("\n\n\n<b><color=#33FFA5>Z nějakého důvodu tyto dveře <color=#FF0000>nefungují</color> ..</color></b>", 5.0);
	}
}
