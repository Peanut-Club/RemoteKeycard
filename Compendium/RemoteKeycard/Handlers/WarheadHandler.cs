using System;
using Compendium.Attributes;
using Compendium.Enums;
using helpers.Configuration;
using helpers.Patching;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;
using Respawning;
using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers;

[ConfigCategory(Name = "Warhead")]
public static class WarheadHandler
{
	public static AlphaWarheadOutsitePanel Panel;

	public static GameObject Script;

	[Config(Name = "Toggleable", Description = "Whether or not to allow players with sufficient perms to toggle the alpha warhead keycard button.")]
	public static bool IsToggleable { get; set; } = true;


	[Config(Name = "Permission", Description = "The permission required to access the alpha warhead button.")]
	public static KeycardPermissions Permission { get; set; } = KeycardPermissions.AlphaWarhead;


	[Patch(typeof(PlayerInteract), "UserCode_CmdSwitchAWButton", PatchType.Prefix, new Type[] { })]
	private static bool WarheadButtonReplacement(PlayerInteract __instance)
	{
		if (RoundSwitches.IsWarheadDisabled)
		{
			return false;
		}
		try
		{
			if (!__instance.CanInteract)
			{
				return false;
			}
			if ((object)Script == null || (object)Panel == null)
			{
				return false;
			}
			if (!__instance.ChckDis(Script.transform.position))
			{
				return false;
			}
			if (IsToggleable && Panel.NetworkkeycardEntered)
			{
				Panel.NetworkkeycardEntered = false;
				__instance.OnInteract();
				return false;
			}
			if (!__instance._sr.BypassMode)
			{
				bool flag = false;
				if (__instance._inv._curInstance != null && __instance._inv._curInstance is KeycardItem keycardItem && keycardItem.Permissions.HasFlagFast(Permission))
				{
					flag = true;
				}
				if (AccessUtils.CanAccessWarhead(__instance._hub))
				{
					flag = true;
				}
				if (!flag)
				{
					return false;
				}
			}
			__instance.OnInteract();
			Panel.NetworkkeycardEntered = !IsToggleable || !Panel.NetworkkeycardEntered;
			/*
			SpawnableTeamType spawnableTeamType = default(SpawnableTeamType);
			if (RespawnTokensManager.TryGetAssignedSpawnableTeam(__instance._hub, ref spawnableTeamType))
			{
				RespawnTokensManager.GrantTokens(spawnableTeamType, 1f);
			}*/
			return false;
		}
		catch (Exception arg)
		{
			Plugin.Error($"Caught an exception in the warhead patch!\n{arg}");
			return true;
		}
	}

	[RoundStateChanged(new RoundState[] { RoundState.WaitingForPlayers })]
	private static void OnRoundWaiting()
	{
		Panel = null;
		Script = null;
	}

	[RoundStateChanged(new RoundState[] { RoundState.InProgress })]
	private static void OnRoundStart()
	{
		Script = GameObject.Find("OutsitePanelScript");
		Panel = Script.GetComponentInParent<AlphaWarheadOutsitePanel>();
	}
}
