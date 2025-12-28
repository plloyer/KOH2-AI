using System;
using System.Collections.Generic;

namespace Logic;

public class Cultures
{
	public class Def : Logic.Def
	{
		public float pop_majority_update_interval = 240f;

		public float pop_majority_convert_threshold = 50f;

		public float pop_majority_min_attack_strength = 0.1f;

		public float pop_majority_max_local_authority = 5f;

		public float pop_majority_max_influence = 4f;

		public float pop_majority_land_realms_mul = 1f;

		public float pop_majority_sea_realms_mul = 0.5f;

		public float pop_majority_max_realms_mul = 3f;

		public float pop_majority_marriage_mul = 1.5f;

		public float pop_majority_king_cleric_level_mul = 0.1f;

		public float pop_majority_separated_own_mul = 0.5f;

		public float pop_majority_bolster_cleric_base = 0.5f;

		public float pop_majority_bolster_cleric_level_mul = 0.1f;

		public Dictionary<string, string> map_culture_to_group = new Dictionary<string, string>();

		public Dictionary<string, int> map_culture_ingroup_index = new Dictionary<string, int>();

		public Dictionary<string, int> map_culture_group_subcultures_count = new Dictionary<string, int>();

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			LoadPopConvertParams(field.FindChild("pop_majority_convert"));
			LoadGroups(field.FindChild("groups"));
			return true;
		}

		private void LoadPopConvertParams(DT.Field f)
		{
			if (f != null)
			{
				pop_majority_update_interval = f.GetFloat("update_interval", null, pop_majority_update_interval);
				pop_majority_convert_threshold = f.GetFloat("convert_threshold", null, pop_majority_convert_threshold);
				pop_majority_min_attack_strength = f.GetFloat("min_attack_strength", null, pop_majority_min_attack_strength);
				pop_majority_max_local_authority = f.GetFloat("max_local_authority", null, pop_majority_max_local_authority);
				pop_majority_max_influence = f.GetFloat("max_influence", null, pop_majority_max_influence);
				pop_majority_land_realms_mul = f.GetFloat("land_realms_mul", null, pop_majority_land_realms_mul);
				pop_majority_sea_realms_mul = f.GetFloat("sea_realms_mul", null, pop_majority_sea_realms_mul);
				pop_majority_max_realms_mul = f.GetFloat("max_realms_mul", null, pop_majority_max_realms_mul);
				pop_majority_marriage_mul = f.GetFloat("marriage_mul", null, pop_majority_marriage_mul);
				pop_majority_king_cleric_level_mul = f.GetFloat("king_cleric_level_mul", null, pop_majority_king_cleric_level_mul);
				pop_majority_separated_own_mul = f.GetFloat("separated_own_mul", null, pop_majority_separated_own_mul);
				pop_majority_bolster_cleric_base = f.GetFloat("bolstering_cleric_base", null, pop_majority_bolster_cleric_base);
				pop_majority_bolster_cleric_level_mul = f.GetFloat("bolstering_cleric_level_mul", null, pop_majority_bolster_cleric_level_mul);
			}
		}

		private void LoadGroups(DT.Field gsf)
		{
			map_culture_to_group.Clear();
			map_culture_ingroup_index.Clear();
			map_culture_group_subcultures_count.Clear();
			if (gsf != null && gsf.children != null)
			{
				for (int i = 0; i < gsf.children.Count; i++)
				{
					DT.Field gf = gsf.children[i];
					LoadGroup(gf);
				}
			}
		}

		private void LoadGroup(DT.Field gf)
		{
			if (gf == null || gf.children == null)
			{
				return;
			}
			string key = gf.key;
			if (string.IsNullOrEmpty(key))
			{
				return;
			}
			int num = 0;
			map_culture_to_group.Add(key, key);
			for (int i = 0; i < gf.children.Count; i++)
			{
				string key2 = gf.children[i].key;
				if (string.IsNullOrEmpty(key2))
				{
					return;
				}
				map_culture_to_group.Add(key2, key);
				map_culture_ingroup_index.Add(key2, num);
				num++;
			}
			map_culture_group_subcultures_count.Add(key, num);
		}
	}

	public class Defaults
	{
		public string NobilityTitles;

		public string NobilityNames;

		public Character.Ethnicity Ethnicity;

		public string UnitsSet;
	}

	public Game game;

	public Def def;

	private Dictionary<string, Defaults> defaults;

	public Cultures(Game game)
	{
		this.game = game;
		def = game.defs.GetBase<Def>();
	}

	public void OnDestroy()
	{
		game = null;
	}

	public bool IsValid(string culture)
	{
		return def.map_culture_to_group.ContainsKey(culture);
	}

	public string GetGroup(string culture)
	{
		if (culture == null)
		{
			return null;
		}
		def.map_culture_to_group.TryGetValue(culture, out var value);
		return value;
	}

	public int GetIngroupIndex(string culture)
	{
		if (culture == null)
		{
			return -1;
		}
		if (!def.map_culture_ingroup_index.TryGetValue(culture, out var value))
		{
			return -1;
		}
		return value;
	}

	public int GetGroupSubculturesCount(string culture)
	{
		if (culture == null)
		{
			return -1;
		}
		if (!def.map_culture_group_subcultures_count.TryGetValue(culture, out var value) && !def.map_culture_group_subcultures_count.TryGetValue(GetGroup(culture) ?? "", out value))
		{
			return -1;
		}
		return value;
	}

	public int Dist(string culture1, string culture2)
	{
		if (string.IsNullOrEmpty(culture1))
		{
			return 2;
		}
		if (string.IsNullOrEmpty(culture2))
		{
			return 2;
		}
		if (culture1 == culture2)
		{
			return 0;
		}
		string text = GetGroup(culture1);
		if (string.IsNullOrEmpty(text))
		{
			return 2;
		}
		string text2 = GetGroup(culture2);
		if (text == text2)
		{
			return 1;
		}
		return 2;
	}

	public Defaults GetDefaults(string culture)
	{
		if (defaults == null)
		{
			return null;
		}
		if (!defaults.TryGetValue(culture, out var value))
		{
			return null;
		}
		return value;
	}

	public void LoadDefaults(string map_name)
	{
		DT.Field field = DT.ReadMapsCsv(null, map_name + "/cultures.csv");
		if (field?.children == null)
		{
			this.defaults = null;
			return;
		}
		this.defaults = new Dictionary<string, Defaults>();
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			string text = field2.key;
			if (text.StartsWith("Name_", StringComparison.Ordinal))
			{
				text = text.Substring(5);
			}
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			if (this.defaults.ContainsKey(text))
			{
				Game.Log(field2.Path(include_file: true) + ": duplicated key", Game.LogType.Error);
				continue;
			}
			if (!IsValid(text))
			{
				Game.Log(field2.Path(include_file: true) + ": unknown culture: '" + text + "'", Game.LogType.Warning);
			}
			Defaults defaults = new Defaults();
			defaults.NobilityNames = field2.GetString("NobilityNames");
			if (string.IsNullOrEmpty(defaults.NobilityNames))
			{
				Game.Log(field2.Path(include_file: true) + ": Culture: '" + text + "' has no NobilityNames", Game.LogType.Warning);
			}
			defaults.NobilityTitles = field2.GetString("NobilityTitles");
			if (string.IsNullOrEmpty(defaults.NobilityTitles))
			{
				Game.Log(field2.Path(include_file: true) + ": Culture: '" + text + "' has no NobilityTitles", Game.LogType.Warning);
			}
			if (!Enum.TryParse<Character.Ethnicity>(field2.GetString("Ethnicity"), out defaults.Ethnicity))
			{
				defaults.Ethnicity = Character.Ethnicity.European;
			}
			defaults.UnitsSet = field2.GetString("UnitsSet");
			game.ValidateDefID(defaults.UnitsSet, "AvailableUnits", field2, "UnitsSet");
			this.defaults.Add(text, defaults);
		}
	}

	public string GetNameKey(IVars vars = null, string form = "")
	{
		return "culture_" + def.field.key;
	}
}
