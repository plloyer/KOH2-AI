using System.Collections.Generic;

namespace Logic;

public class ModCompatibilityInfo
{
	public string save_string;

	public string display_name;

	public string checksum;

	public bool texts_only;

	public Mod mod;

	public bool active;

	public ModCompatibilityInfo(Mod mod)
	{
		save_string = mod.GetSaveString();
		display_name = mod.DisplayName;
		checksum = mod.checksum;
		texts_only = mod.IsTextsOnly;
		this.mod = mod;
		ModManager modManager = ModManager.Get();
		if (modManager != null)
		{
			active = modManager.IsActiveMod(mod);
		}
	}

	public ModCompatibilityInfo(string save_string)
	{
		this.save_string = save_string;
		int num = save_string.IndexOf('/');
		if (num >= 0)
		{
			display_name = save_string.Substring(0, num);
			checksum = save_string.Substring(num + 1);
		}
		else
		{
			display_name = save_string;
		}
		texts_only = checksum == "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";
		ModManager modManager = ModManager.Get();
		mod = modManager?.GetModBySaveString(save_string);
		if (mod != null)
		{
			display_name = mod.DisplayName;
			active = modManager.IsActiveMod(mod);
		}
	}

	public static ModCompatibilityInfo Find(List<ModCompatibilityInfo> mods, string checksum)
	{
		if (mods == null)
		{
			return null;
		}
		for (int i = 0; i < mods.Count; i++)
		{
			ModCompatibilityInfo modCompatibilityInfo = mods[i];
			if (modCompatibilityInfo.checksum == checksum)
			{
				return modCompatibilityInfo;
			}
		}
		return null;
	}

	public override string ToString()
	{
		return save_string;
	}
}
