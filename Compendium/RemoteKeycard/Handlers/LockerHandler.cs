using System;
using helpers.Configuration;
using helpers.Patching;
using Interactables.Interobjects.DoorUtils;
using MapGeneration.Distributors;
using PluginAPI.Events;
using static InventorySystem.Items.Firearms.Modules.CylinderAmmoModule;

namespace Compendium.RemoteKeycard.Handlers;

#if true
[ConfigCategory(Name = "Locker")]
public static class LockerHandler {
	[Patch(typeof(Locker), nameof(Locker.ServerInteract), PatchType.Prefix)]
	private static bool LockerInteractionReplacement(Locker __instance, ReferenceHub ply, byte colliderId) {
        if (RoundSwitches.IsLockerDisabled) {
            return true;
        }

        if (!__instance.Chambers.TryGet(colliderId, out var element) || !element.CanInteract) {
            return false;
        }

        //bool hasPermissions = __instance.CheckTogglePerms(colliderId, ply) || ply.serverRoles.BypassMode;
        bool hasPermissions = AccessUtils.CanAccessChamber(__instance.Chambers[colliderId], ply) || ply.serverRoles.BypassMode;

        if (EventManager.ExecuteEvent(new PlayerInteractLockerEvent(ply, __instance, element, hasPermissions))) {
            if (hasPermissions) {
                element.SetDoor(!element.IsOpen, __instance._grantedBeep);
                __instance.RefreshOpenedSyncvar();
            } else {
                __instance.RpcPlayDenied(colliderId);
            }
        }
		return false;

        /*
		try {
			if (colliderId >= __instance.Chambers.Length || !__instance.Chambers[colliderId].CanInteract) {
				return false;
			}
			bool flag = false;
			if (ply.serverRoles.BypassMode) {
				flag = true;
			}
			if (__instance.Chambers[colliderId].RequiredPermissions == KeycardPermissions.None) {
				flag = true;
			}
			if (!flag) {
				flag = __instance.CheckPerms(__instance.Chambers[colliderId].RequiredPermissions, ply);
			}
			if (!flag) {
				flag = AccessUtils.CanAccessChamber(__instance.Chambers[colliderId], ply);
			}
			if (!EventManager.ExecuteEvent(new PlayerInteractLockerEvent(ply, __instance, __instance.Chambers[colliderId], flag)))
			{
				return false;
			}
			if (!flag)
			{
				__instance.RpcPlayDenied(colliderId);
				return false;
			}
			__instance.Chambers[colliderId].SetDoor(!__instance.Chambers[colliderId].IsOpen, __instance._grantedBeep);
			__instance.RefreshOpenedSyncvar();
			return false;
		}
		catch (Exception arg)
		{
			Plugin.Error($"Caught an exception in the locker patch!\n{arg}");
			return true;
		}
		*/
    }
}
#endif