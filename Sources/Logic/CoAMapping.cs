using System;
using System.Collections.Generic;

namespace Logic;

public class CoAMapping
{
	public class KingdomColors
	{
		public int map_color = -1;

		public int primary_army_color = -1;

		public int secondary_army_color = -1;
	}

	public Dictionary<string, int> mapping = new Dictionary<string, int>();

	public Dictionary<string, KingdomColors> colors = new Dictionary<string, KingdomColors>();

	private static Dictionary<string, CoAMapping> mappings = new Dictionary<string, CoAMapping>();

	public static void ClearAll()
	{
		mappings.Clear();
	}

	public static CoAMapping GetMapping(string map_name, bool create_if_not_found = true)
	{
		if (string.IsNullOrEmpty(map_name))
		{
			return null;
		}
		if (mappings.TryGetValue(map_name, out var value))
		{
			return value;
		}
		if (!create_if_not_found)
		{
			return null;
		}
		value = new CoAMapping();
		value.Load(map_name);
		mappings.Add(map_name, value);
		return value;
	}

	public static int GetCoAIndex(string map_name, string period, string kingdom_name)
	{
		if (string.IsNullOrEmpty(kingdom_name))
		{
			return -2;
		}
		return GetMapping(map_name)?.Get(kingdom_name, period) ?? (-1);
	}

	public int Get(string key)
	{
		mapping.TryGetValue(key, out var value);
		if (value > 0)
		{
			return value;
		}
		int num = key.LastIndexOf('.');
		if (num < 0)
		{
			return 0;
		}
		string key2 = key.Substring(0, num);
		mapping.TryGetValue(key2, out value);
		return value;
	}

	public void Set(string key, int idx)
	{
		mapping[key] = idx;
	}

	public int Get(string name, string period)
	{
		if (string.IsNullOrEmpty(name))
		{
			return -2;
		}
		int value;
		if (!string.IsNullOrEmpty(period))
		{
			string key = name + "." + period;
			mapping.TryGetValue(key, out value);
			if (value > 0)
			{
				return value;
			}
		}
		mapping.TryGetValue(name, out value);
		return value;
	}

	public KingdomColors GetColors(string key)
	{
		colors.TryGetValue(key, out var value);
		if (value != null)
		{
			return value;
		}
		int num = key.LastIndexOf('.');
		if (num < 0)
		{
			return null;
		}
		string key2 = key.Substring(0, num);
		colors.TryGetValue(key2, out value);
		return value;
	}

	public void SetColors(string key, KingdomColors new_colors)
	{
		colors[key] = new_colors;
	}

	public KingdomColors GetColors(string name, string period)
	{
		string key = name + "." + period;
		colors.TryGetValue(key, out var value);
		if (value != null && (value.map_color >= 0 || value.primary_army_color >= 0 || value.secondary_army_color >= 0))
		{
			return value;
		}
		colors.TryGetValue(name, out value);
		return value;
	}

	public void Load(string map_name)
	{
		mapping.Clear();
		colors.Clear();
		ImportCSV(map_name, "CoA", error_on_duplicates: true);
	}

	private void ImportCSV(string map_name, string filename, bool error_on_duplicates)
	{
		ImportCSVInternal(Game.maps_path, map_name, filename, error_on_duplicates);
		ModManager modManager = ModManager.Get();
		if (modManager == null)
		{
			return;
		}
		foreach (Mod activeMod in modManager.GetActiveMods())
		{
			if (activeMod.has_maps)
			{
				ModManager.LoadingMod = activeMod;
				ImportCSVInternal(activeMod.maps_path, map_name, filename, error_on_duplicates);
				ModManager.LoadingMod = null;
			}
		}
	}

	private void ImportCSVInternal(string maps_path, string map_name, string filename, bool error_on_duplicates)
	{
		string text = maps_path + map_name + "/" + filename + ".csv";
		Table table = Table.FromFile(text);
		if (table == null || table.NumRows < 2)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < table.NumCols; i++)
		{
			if (table.Get(0, i) == "CoA")
			{
				num = i;
				break;
			}
		}
		if (num >= 0)
		{
			for (int j = 1; j < table.NumRows; j++)
			{
				string text2 = table.Get(j, 0);
				if (string.IsNullOrEmpty(text2) || text2.StartsWith("//", StringComparison.Ordinal))
				{
					continue;
				}
				int.TryParse(table.Get(j, num), out var result);
				if (!mapping.TryGetValue(text2, out var value))
				{
					mapping.Add(text2, result);
					continue;
				}
				if (error_on_duplicates && !ModManager.IsLoadingMod())
				{
					Game.Log($"{text}({j + 1}): Duplicated key: '{text2}'", Game.LogType.Error);
				}
				if (value == 0)
				{
					mapping[text2] = result;
				}
			}
		}
		int num2 = -1;
		for (int k = 0; k < table.NumCols; k++)
		{
			if (table.Get(0, k) == "map_color")
			{
				num2 = k;
				break;
			}
		}
		if (num2 < 0 || table.NumCols < num2 + 2)
		{
			return;
		}
		for (int l = 1; l < table.NumRows; l++)
		{
			string text3 = table.Get(l, 0);
			if (string.IsNullOrEmpty(text3) || text3.StartsWith("//", StringComparison.Ordinal))
			{
				continue;
			}
			string s = table.Get(l, num2);
			string s2 = table.Get(l, num2 + 1);
			string s3 = table.Get(l, num2 + 2);
			int.TryParse(s, out var result2);
			int.TryParse(s2, out var result3);
			int.TryParse(s3, out var result4);
			KingdomColors kingdomColors = new KingdomColors();
			kingdomColors.map_color = result2;
			kingdomColors.primary_army_color = result3;
			kingdomColors.secondary_army_color = result4;
			if (!colors.TryGetValue(text3, out var value2))
			{
				colors.Add(text3, kingdomColors);
				continue;
			}
			if (error_on_duplicates)
			{
				Game.Log($"{text}({l + 1}): Duplicated key: '{text3}'", Game.LogType.Error);
			}
			if (value2 == null)
			{
				colors[text3] = kingdomColors;
			}
		}
	}
}
