using System.Collections.Generic;
using System.Linq;
using Compendium.RemoteKeycard.Handlers;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;
using MapGeneration.Distributors;

namespace Compendium.RemoteKeycard;

public static class AccessUtils
{
	public static bool CanAccessWarhead(ReferenceHub player)
	{
		return player.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> x) => x.Value != null && x.Value is KeycardItem keycardItem && keycardItem.Permissions.HasFlagFast(KeycardPermissions.AlphaWarhead)) && !RoundSwitches.IsRemote;
	}

	public static bool CanAccessChamber(LockerChamber chamber, ReferenceHub player)
	{
		return chamber.RequiredPermissions == KeycardPermissions.None || (player.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> x) => x.Value != null && x.Value is KeycardItem keycardItem && keycardItem.Permissions.HasFlagFast(chamber.RequiredPermissions)) && !RoundSwitches.IsRemote);
	}

	public static bool CanAccessGenerator(Scp079Generator generator, ReferenceHub player)
	{
		return player.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> x) => x.Value != null && x.Value is KeycardItem keycardItem && keycardItem.Permissions.HasFlagFast(generator._requiredPermission)) && !RoundSwitches.IsRemote;
	}

	public static bool CanAccessDoor(DoorVariant door, ReferenceHub player)
	{
		return player.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> x) => x.Value != null && x.Value is KeycardItem item && door.RequiredPermissions.CheckPermissions(item, player)) && !RoundSwitches.IsRemote;
	}
}
