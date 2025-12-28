using System.Collections.Generic;

namespace Logic;

public class ModsCompatibility
{
	private string saved_mod_id;

	public List<ModCompatibilityInfo> saved_mods = new List<ModCompatibilityInfo>();

	public List<ModCompatibilityInfo> available_mods = new List<ModCompatibilityInfo>();

	public ModCompatibilityState state;

	public ModsCompatibility()
	{
	}

	public ModsCompatibility(string saved_mod_id, string available_mod_id = null)
	{
		PopulateSavedMods(saved_mod_id);
		PopulateAvailableMods(available_mod_id);
		state = DecideState();
	}

	public override string ToString()
	{
		return $"[{state} {saved_mod_id}";
	}

	public static ModCompatibilityState Get(string saved_mod_id, string available_mod_id = null)
	{
		return new ModsCompatibility(saved_mod_id, available_mod_id).state;
	}

	public static bool ValidateJoinOrEnter(string saved_mod_id)
	{
		return new ModsCompatibility(saved_mod_id).state <= ModCompatibilityState.Green;
	}

	public static string GetErrorKey(string saved_mod_id, out Vars vars, string available_mod_id = null)
	{
		return new ModsCompatibility(saved_mod_id, available_mod_id).GetErrorKey(out vars);
	}

	public string GetErrorKey(out Vars vars)
	{
		vars = null;
		return state switch
		{
			ModCompatibilityState.Hidden => null, 
			ModCompatibilityState.Green => "", 
			ModCompatibilityState.Yellow => "MultiplayerMenu.campaign_different_mod_tooltip", 
			_ => "MultiplayerMenu.campaign_unknown_mod_tooltip", 
		};
	}

	public void PopulateAvailableMods(string available_mod_id = null)
	{
		if (available_mod_id != null)
		{
			PopulateSavedMods(available_mods, available_mod_id, active: true);
			return;
		}
		available_mods.Clear();
		ModManager modManager = ModManager.Get();
		if (modManager != null)
		{
			List<Mod> allMods = modManager.GetAllMods();
			for (int i = 0; i < allMods.Count; i++)
			{
				ModCompatibilityInfo item = new ModCompatibilityInfo(allMods[i]);
				available_mods.Add(item);
			}
		}
	}

	private void PopulateSavedMods(List<ModCompatibilityInfo> saved_mods, string saved_mod_id, bool active = false)
	{
		saved_mods.Clear();
		if (string.IsNullOrEmpty(saved_mod_id))
		{
			return;
		}
		int num = 0;
		while (num < saved_mod_id.Length)
		{
			int num2 = saved_mod_id.IndexOf(';', num);
			if (num2 < 0)
			{
				num2 = saved_mod_id.Length;
			}
			ModCompatibilityInfo modCompatibilityInfo = new ModCompatibilityInfo(saved_mod_id.Substring(num, num2 - num));
			modCompatibilityInfo.active = active;
			saved_mods.Add(modCompatibilityInfo);
			num = num2 + 1;
		}
	}

	public void PopulateSavedMods(string saved_mod_id)
	{
		this.saved_mod_id = saved_mod_id;
		PopulateSavedMods(saved_mods, saved_mod_id);
	}

	public ModCompatibilityState DecideModState(ModCompatibilityInfo mi, bool is_saved)
	{
		if (!is_saved && !mi.active)
		{
			return ModCompatibilityState.Hidden;
		}
		if (mi.texts_only)
		{
			return ModCompatibilityState.Green;
		}
		if (ModCompatibilityInfo.Find(is_saved ? available_mods : saved_mods, mi.checksum) == null)
		{
			if (is_saved)
			{
				return ModCompatibilityState.Red;
			}
			return ModCompatibilityState.Yellow;
		}
		return ModCompatibilityState.Green;
	}

	public ModCompatibilityState DecideState()
	{
		ModCompatibilityState modCompatibilityState = ModCompatibilityState.Hidden;
		for (int i = 0; i < saved_mods.Count; i++)
		{
			ModCompatibilityInfo mi = saved_mods[i];
			ModCompatibilityState modCompatibilityState2 = DecideModState(mi, is_saved: true);
			if (modCompatibilityState2 > modCompatibilityState)
			{
				modCompatibilityState = modCompatibilityState2;
			}
		}
		for (int j = 0; j < available_mods.Count; j++)
		{
			ModCompatibilityInfo modCompatibilityInfo = available_mods[j];
			if (!modCompatibilityInfo.texts_only)
			{
				ModCompatibilityState modCompatibilityState3 = DecideModState(modCompatibilityInfo, is_saved: false);
				if (modCompatibilityState3 > modCompatibilityState)
				{
					modCompatibilityState = modCompatibilityState3;
				}
			}
		}
		return modCompatibilityState;
	}
}
