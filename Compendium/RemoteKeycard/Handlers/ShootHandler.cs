using System;
using CustomPlayerEffects;
using helpers.Patching;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using PlayerStatsSystem;
using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers;

public static class ShootHandler
{
	public static event Func<ReferenceHub, GameObject, bool> OnHit;

	[Patch(typeof(HitscanHitregModuleBase), nameof(HitscanHitregModuleBase.ServerPerformHitscan), PatchType.Prefix)]
	private static bool Prefix(HitscanHitregModuleBase __instance, Ray targetRay, out float targetDamage, ref bool __result) {
		targetDamage = 0f;
        __result = false;
        float maxDistance = __instance.DamageFalloffDistance + __instance.FullDamageDistance;

        if (RoundSwitches.IsShotDisabled) {
			return true;
		}

		if (Physics.Raycast(targetRay, out var hitInfo, maxDistance, Physics.DefaultRaycastLayers) &&
            ShootHandler.OnHit != null &&
            ShootHandler.OnHit(__instance.Owner, hitInfo.collider.gameObject)) {
			return false;
		}

		return true;
		/*
        __instance.ServerLastDamagedTargets.Clear();
        targetDamage = 0f;
        if (!Physics.Raycast(targetRay, out var hitInfo, maxDistance, __instance.HitregMask)) {
            return false;
        }
        if (hitInfo.collider.TryGetComponent<IDestructible>(out var component)) {
            if (!__instance.ValidateTarget(component)) {
                return false;
            }
            targetDamage = __instance.ServerProcessTargetHit(component, hitInfo);
        } else {
            targetDamage = __instance.ServerProcessObstacleHit(hitInfo);
        }
        if (!__instance.Firearm.TryGetModule<ImpactEffectsModule>(out var module)) {
            return true;
        }
        module.ServerProcessHit(hitInfo, targetRay.origin, targetDamage > 0f);
        return true;



        if (hit.collider.TryGetComponent<IDestructible>(out var component) && __instance.CheckInaccurateFriendlyFire(component))
		{
			FirearmBaseStats baseStats = ((StandardHitregBase)__instance).Firearm.BaseStats;
			float num = ((FirearmBaseStats)(ref baseStats)).DamageAtDistance(((StandardHitregBase)__instance).Firearm, hit.distance);
			if (component.Damage(num, new FirearmDamageHandler(((StandardHitregBase)__instance).Firearm, num, true), hit.point))
			{
				if (!ReferenceHub.TryGetHubNetID(component.NetworkId, out var hub) || !hub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
				{
					Hitmarker.SendHitmarkerDirectly(((StandardHitregBase)__instance).Conn, 1f);
				}
				((StandardHitregBase)__instance).ShowHitIndicator(component.NetworkId, num, ray.origin);
				((StandardHitregBase)__instance).PlaceBloodDecal(ray, hit, component);
			}
		}
		else
		{
			((StandardHitregBase)__instance).PlaceBulletholeDecal(ray, hit);
		}
		return false;
		*/
	}
}
