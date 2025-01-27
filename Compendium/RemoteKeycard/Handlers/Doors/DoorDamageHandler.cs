using System;
using System.Collections.Generic;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.RemoteKeycard.Enums;
using Compendium.Updating;
using helpers;
using helpers.Configuration;
using helpers.Random;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles;
using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers.Doors;

[ConfigCategory(Name = "Door Damage")]
public static class DoorDamageHandler
{
	private static readonly Dictionary<DoorVariant, DoorDamageData> _damage = new Dictionary<DoorVariant, DoorDamageData>();

	private static readonly Dictionary<DoorVariant, DoorZombieStatus> _zombies = new Dictionary<DoorVariant, DoorZombieStatus>();

	[Config(Name = "Enabled", Description = "Whether or not to allow players to damage doors.")]
	public static bool IsEnabled { get; set; } = true;


	[Config(Name = "Health", Description = "Health of each door type.")]
	public static Dictionary<InteractableCategory, float> DoorHealth { get; set; } = new Dictionary<InteractableCategory, float>
	{
		[InteractableCategory.EzDoor] = 100f,
		[InteractableCategory.EzGate] = 300f,
		[InteractableCategory.SurfaceDoor] = 100f,
		[InteractableCategory.SurfaceGate] = 300f,
		[InteractableCategory.LczDoor] = 150f,
		[InteractableCategory.LczGate] = 300f,
		[InteractableCategory.HczDoor] = 200f,
		[InteractableCategory.HczGate] = 300f
	};


	[Config(Name = "Destroy Status", Description = "The door status to use for a destroyed door.")]
	public static DoorDamageStatus DestroyStatus { get; set; } = DoorDamageStatus.Unusable;


	[Config(Name = "Zombies", Description = "Config for SCP-049-2 attackers.")]
	public static DoorZombieConfig Zombies { get; set; } = new DoorZombieConfig();


	public static void DoDamage(ReferenceHub player, float damage, DoorVariant target, DoorDamageSource source)
	{
		if (!IsEnabled)
		{
			return;
		}
		if (source == DoorDamageSource.Firearm)
		{
			if (_damage.TryGetValue(target, out var value) && value.Status == DoorDamageStatus.Usable)
			{
				value.Health -= damage;
				if (value.Health <= 0f)
				{
					value.Status = DestroyStatus;
					Door.Open(target);
				}
			}
		}
		else
		{
			if (!_zombies.TryGetValue(target, out var value2) || value2.Broken || HubRoleExtensions.RoleId(player) != RoleTypeId.Scp0492 || target.NetworkTargetState)
			{
				return;
			}
			DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)target.ActiveLocks);
			if ((!mode.HasFlagFast(DoorLockMode.FullLock) || mode.HasFlagFast(DoorLockMode.ScpOverride) || mode.HasFlagFast(DoorLockMode.CanOpen)) && target.RequiredPermissions.RequiredPermissions != 0)
			{
				if (!value2.CurrentInteractions.Contains(player))
				{
					value2.CurrentInteractions.Add(player);
				}
				value2.LastInteraction = DateTime.Now;
				value2.ActiveInteraction = true;
				value2.RemainingHealth -= value2.Damage;
				if (value2.RemainingHealth <= 0f)
				{
					value2.RemainingHealth = 0f;
					value2.ActiveInteraction = false;
					value2.Broken = true;
					value2.CurrentInteractions.Clear();
					value2.LastInteraction = DateTime.Now;
					Door.Destroy(target);
				}
				else if (Zombies.InteractionHint != null && Zombies.InteractionHint.IsValid)
				{
					HubWorldExtensions.Broadcast(player, Zombies.InteractionHint.Value.Replace("%hp%", Mathf.RoundToInt(value2.RemainingHealth).ToString()).Replace("%damage%", Mathf.RoundToInt(value2.Damage).ToString()), (int)Zombies.InteractionHint.Duration);
				}
			}
		}
	}

	public static bool ProcessDamage(ReferenceHub player, DoorVariant door)
	{
		if (!IsEnabled)
		{
			return true;
		}
		if (!_damage.TryGetValue(door, out var value))
		{
			return true;
		}
		if (value.Status != 0)
		{
			if (value.Status == DoorDamageStatus.Unusable)
			{
				return false;
			}
			if (value.Status == DoorDamageStatus.UsableChance)
			{
				if (DoorHandler.UsableChance <= 0)
				{
					return false;
				}
				return WeightedRandomGeneration.Default.GetBool(DoorHandler.UsableChance);
			}
		}
		return true;
	}

	public static void DamageAction(ReferenceHub player, DoorVariant door)
	{
		if (IsEnabled && DoorHandler.FailureHint != null && DoorHandler.FailureHint.IsValid)
		{
			DoorHandler.FailureHint.Send(player);
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.InProgress })]
	private static void OnRoundStart()
	{
		_damage.Clear();
		_zombies.Clear();
		DoorVariant.AllDoors.ForEach(delegate(DoorVariant d)
		{
			InteractableCategory category = d.GetCategory();
			if (DoorHealth.TryGetValue(category, out var value))
			{
				_damage[d] = new DoorDamageData
				{
					Health = value,
					MaxHealth = value,
					Status = DoorDamageStatus.Usable
				};
			}
			_zombies[d] = new DoorZombieStatus
			{
				ActiveInteraction = false,
				Broken = false,
				DamagePerPlayer = Zombies.DamagePerPlayer,
				RegenHealth = Zombies.RegenHealth,
				RegenSpeed = Zombies.RegenSpeed,
				LastInteraction = DateTime.Now,
				LastRegen = DateTime.Now
			};
			if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ArmoryLevelOne))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 15f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ArmoryLevelTwo))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 30f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ArmoryLevelThree))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 60f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.AlphaWarhead))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 100f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.Intercom))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 10f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ContainmentLevelOne))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 5f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ContainmentLevelTwo))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 30f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ContainmentLevelThree))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 100f;
			}
			else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ExitGates))
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth + 2000f;
			}
			else
			{
				_zombies[d].StartingHealth = Zombies.StartingHealth;
			}
			_zombies[d].RemainingHealth = _zombies[d].StartingHealth;
		});
	}

	[Update(Delay = 200)]
	private static void UpdateZombieProgress()
	{
		_zombies.PoolableModifyAct(delegate(DoorVariant door, IDictionary<DoorVariant, DoorZombieStatus> dict)
		{
			if (dict.TryGetValue(door, out var value))
			{
				if (value.ActiveInteraction && (DateTime.Now - value.LastInteraction).TotalMilliseconds >= (double)Zombies.LastInteractionInterval)
				{
					value.CurrentInteractions.Clear();
					value.ActiveInteraction = false;
				}
				else if (value.RemainingHealth > 0f && value.RemainingHealth < value.StartingHealth && (DateTime.Now - value.LastRegen).TotalMilliseconds >= (double)Zombies.RegenSpeed)
				{
					value.RemainingHealth += value.RegenHealth;
					if (value.RemainingHealth > value.StartingHealth)
					{
						value.RemainingHealth = value.StartingHealth;
					}
					value.LastRegen = DateTime.Now;
				}
			}
		});
	}
}
