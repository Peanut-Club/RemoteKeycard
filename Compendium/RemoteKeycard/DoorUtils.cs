using System.Linq;
using Compendium.RemoteKeycard.Enums;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MapGeneration.Distributors;
using UnityEngine;

namespace Compendium.RemoteKeycard;

public static class DoorUtils
{
	public static bool IsDoor(this InteractableCategory category)
	{
		return category == InteractableCategory.EzDoor || category == InteractableCategory.HczDoor || category == InteractableCategory.LczDoor || category == InteractableCategory.SurfaceDoor;
	}

	public static bool IsLocker(this InteractableCategory category)
	{
		return category == InteractableCategory.WallGunLocker || category == InteractableCategory.Locker || category == InteractableCategory.GunLocker;
	}

	public static InteractableCategory GetCategory(this DoorVariant door)
	{
		RoomIdentifier roomIdentifier = door.Rooms.First();
		if (roomIdentifier.Zone == FacilityZone.Entrance)
		{
			return InteractableCategory.EzDoor;
		}
		if (roomIdentifier.Zone == FacilityZone.HeavyContainment)
		{
			return InteractableCategory.HczDoor;
		}
		if (roomIdentifier.Zone == FacilityZone.LightContainment)
		{
			return InteractableCategory.LczDoor;
		}
		return InteractableCategory.SurfaceDoor;
	}

	public static InteractableCategory GetCategory(this LockerChamber locker)
	{
		GameObject gameObject = locker.transform.parent.gameObject;
		if (gameObject.name.Contains("LargeGunLockerStructure"))
		{
			return InteractableCategory.GunLocker;
		}
		if (locker.name.Contains("MiscLocker"))
		{
			return InteractableCategory.Locker;
		}
		return InteractableCategory.WallGunLocker;
	}
}
