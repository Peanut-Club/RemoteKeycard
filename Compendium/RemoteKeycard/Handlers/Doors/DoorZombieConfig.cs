using System.Collections.Generic;
using Compendium.Messages;
using Compendium.RemoteKeycard.Enums;

namespace Compendium.RemoteKeycard.Handlers.Doors;

public class DoorZombieConfig
{
	public List<InteractableCategory> AllowedCategories { get; set; } = new List<InteractableCategory>
	{
		InteractableCategory.EzDoor,
		InteractableCategory.HczDoor,
		InteractableCategory.LczDoor,
		InteractableCategory.SurfaceDoor
	};


	public int LastInteractionInterval { get; set; } = 10000;


	public bool Enabled { get; set; } = true;


	public float StartingHealth { get; set; } = 150f;


	public float DamagePerPlayer { get; set; } = 5f;


	public float RegenHealth { get; set; } = 5f;


	public float RegenSpeed { get; set; } = 1500f;


	public HintMessage InteractionHint { get; set; } = HintMessage.Create("<b><color=#33FFA5>Tyto dveře mají ještě <color=#FF0000>%hp%</color> HP!<b></color>\n<i><color=#90FF33>Tip:</color> sežeň si pár dalších zombie pro větší damage! (aktuální damage: %damage% HP / hit)</color></i>", 5.0);

}
