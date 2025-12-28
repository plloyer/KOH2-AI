using System.Collections.Generic;

namespace Logic;

public class RealmFeaturesPreset
{
	public class Def : Logic.Def
	{
		public int occurances_min = 4;

		public int occurances_max = 4;

		public int occurances_curr = 4;

		public bool requre_costal;

		public List<string> required_province_features = new List<string>();

		public List<(string, int)> required_settlements = new List<(string, int)>();

		public int preferred_min_distance = 2;

		public List<(string, float)> desired_in_realms = new List<(string, float)>();

		public List<string> features = new List<string>();

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			required_province_features.Clear();
			required_settlements.Clear();
			desired_in_realms.Clear();
			features.Clear();
			LoadOccurances(field.FindChild("occurances"), game);
			requre_costal = field.GetBool("requre_costal", null, requre_costal);
			preferred_min_distance = field.GetInt("preferred_min_distance ", null, preferred_min_distance);
			LoadProviceFeatures(field.FindChild("required_province_fetures"), game);
			LoadFeatures(field.FindChild("features"), game);
			LoadSettlements(field.FindChild("required_settlements"), game);
			LoadDesiredRealms(field.FindChild("desired_in_realms"), game);
			return true;
		}

		private void LoadOccurances(DT.Field field, Game game)
		{
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			if (num != 0)
			{
				if (num == 1)
				{
					occurances_curr = (occurances_min = (occurances_max = field.Int(0, null, occurances_min)));
				}
				else if (num > 1)
				{
					occurances_min = field.Int(0, null, occurances_min);
					occurances_max = field.Int(1, null, occurances_max);
					occurances_curr = game.Random(occurances_min, occurances_max + 1);
				}
			}
		}

		private void LoadDesiredRealms(DT.Field field, Game game)
		{
			if (field == null)
			{
				return;
			}
			float num = 100f;
			if (field.value_str != string.Empty)
			{
				num = field.Float();
			}
			List<DT.Field> list = field.Children();
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field2 = list[i];
				if (field2 != null)
				{
					float item = num;
					if (field2.value_str != string.Empty)
					{
						item = field2.Float();
					}
					desired_in_realms.Add((field2.key, item));
				}
			}
		}

		private void LoadProviceFeatures(DT.Field field, Game game)
		{
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					Value value = field.Value(i, null, calc_expression: false);
					if (value.is_object && value.obj_val is DT.Field field2)
					{
						if (field2.base_path != "ProvinceFeature")
						{
							Game.Log(field.Path(include_file: true) + ": Unknown Settlement type " + field2.key, Game.LogType.Warning);
						}
						else
						{
							required_province_features.Add(field2.key);
						}
					}
				}
				return;
			}
			List<DT.Field> list = field.Children();
			if (list == null)
			{
				return;
			}
			for (int j = 0; j < list.Count; j++)
			{
				DT.Field field3 = list[j];
				if (field3 != null)
				{
					DT.Def def = game.dt.FindDef(field3.key);
					if (def == null)
					{
						Game.Log(field.Path(include_file: true) + ": Unknown Settlement type " + field3.key, Game.LogType.Warning);
					}
					else if (def.field.base_path != "ProvinceFeature")
					{
						Game.Log(field.Path(include_file: true) + ": Unknown Settlement type " + field3.key, Game.LogType.Warning);
					}
					else
					{
						required_province_features.Add(field3.key);
					}
				}
			}
		}

		private void LoadFeatures(DT.Field field, Game game)
		{
			if (field == null)
			{
				return;
			}
			int count = field.children.Count;
			if (count > 0)
			{
				for (int i = 0; i < count; i++)
				{
					features.Add(field.children[i].key);
				}
			}
		}

		private void LoadSettlements(DT.Field field, Game game)
		{
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					Value value = field.Value(i, null, calc_expression: false);
					if (value.is_object && value.obj_val is DT.Field field2)
					{
						if (field2.BaseRoot()?.key != "Settlement")
						{
							Game.Log(field.Path(include_file: true) + ": Unknown Settlement type " + field2.key, Game.LogType.Warning);
						}
						else
						{
							required_settlements.Add((field2.key, 1));
						}
					}
				}
				return;
			}
			List<DT.Field> list = field.Children();
			if (list == null)
			{
				return;
			}
			for (int j = 0; j < list.Count; j++)
			{
				DT.Field field3 = list[j];
				if (field3 != null)
				{
					DT.Def def = game.dt.FindDef(field3.key);
					if (def == null)
					{
						Game.Log(field.Path(include_file: true) + ": Unknown Settlement type " + field3.key, Game.LogType.Warning);
					}
					else if (def.field.BaseRoot()?.key != "Settlement")
					{
						Game.Log(field.Path(include_file: true) + ": Unknown Settlement type " + field3.key, Game.LogType.Warning);
					}
					else
					{
						required_settlements.Add((field3.key, field3.Int(null, 1)));
					}
				}
			}
		}

		public override bool Validate(Game game)
		{
			int max_identical_settlement_per_realm = game.defs.GetBase<Settlement.Def>().max_identical_settlement_per_realm;
			if (required_settlements != null && required_settlements.Count >= max_identical_settlement_per_realm)
			{
				Game.Log($"{base.field.Path(include_file: true)}:Exceeded the maximum number of settlement types per realm {required_settlements.Count} from {max_identical_settlement_per_realm}. Overflow will be ommited ", Game.LogType.Warning);
				while (required_settlements.Count > max_identical_settlement_per_realm)
				{
					required_settlements.RemoveAt(required_settlements.Count - 1);
				}
			}
			List<string> list = new List<string>();
			if (required_province_features != null)
			{
				Dictionary<string, Settlement.Def> featureToSettlementMap = GetFeatureToSettlementMap(game);
				Dictionary<Settlement.Def, int> dictionary = new Dictionary<Settlement.Def, int>();
				for (int i = 0; i < required_province_features.Count; i++)
				{
					string text = required_province_features[i];
					if (!featureToSettlementMap.ContainsKey(text))
					{
						list.Add(text);
						continue;
					}
					Settlement.Def def = featureToSettlementMap[text];
					if (featureToSettlementMap.ContainsKey(text))
					{
						if (!dictionary.ContainsKey(def))
						{
							dictionary.Add(def, 0);
						}
						dictionary[def]++;
						if (dictionary[def] > def.max_allowed_features_per_settlement_type)
						{
							list.Add(text);
						}
					}
				}
			}
			if (list.Count > 0)
			{
				for (int j = 0; j < list.Count; j++)
				{
					required_province_features.Remove(list[j]);
					Game.Log(base.field.Path(include_file: true) + ":Exceeded the maximum number of province features per settlement type. " + list[j] + " will be ommited ", Game.LogType.Warning);
				}
			}
			return true;
		}

		public bool IsDesiredRealm(RealmData realmData, out float chance)
		{
			if (desired_in_realms == null || desired_in_realms.Count == 0)
			{
				chance = 0f;
				return false;
			}
			for (int i = 0; i < desired_in_realms.Count; i++)
			{
				(string, float) tuple = desired_in_realms[i];
				if (tuple.Item1 == realmData.realm.name)
				{
					chance = tuple.Item2;
					return true;
				}
			}
			chance = 0f;
			return false;
		}

		public void GetRequredFeatures(Settlement.Def sdef)
		{
			Game.Log("implement me ", Game.LogType.Warning);
		}

		private void Dump()
		{
			Game.Log(string.Concat(string.Concat(string.Concat(string.Concat("RealmSets(" + base.field.key + ")", $"\n occurances = {occurances_min}/{occurances_max}"), "\n required_province_fetures = ", ListToString(required_province_features)), "\n required_settlements = ", ListToString(required_settlements)), "\n desired_in_realms = ", ListToString(desired_in_realms)), Game.LogType.Message);
		}
	}

	private static Dictionary<string, Settlement.Def> sm_feature_map;

	private static string ListToString<T>(List<T> lst)
	{
		if (lst == null || lst.Count == 0)
		{
			return "Empty";
		}
		string text = "";
		for (int i = 0; i < lst.Count; i++)
		{
			text = string.Concat(text, lst[i], (i < lst.Count - 1) ? ", " : "");
		}
		return text;
	}

	public static Dictionary<string, Settlement.Def> GetFeatureToSettlementMap(Game game)
	{
		if (sm_feature_map != null)
		{
			return sm_feature_map;
		}
		sm_feature_map = new Dictionary<string, Settlement.Def>();
		game.defs.GetBase<Settlement.Def>();
		List<Settlement.Def> defs = game.defs.GetDefs<Settlement.Def>();
		List<Settlement.Def> list = new List<Settlement.Def>();
		for (int i = 0; i < defs.Count; i++)
		{
			if (!defs[i].dt_def.path.Contains("Settlements."))
			{
				list.Add(defs[i]);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			Settlement.Def def = list[j];
			string[] enable_features = def.enable_features;
			if (enable_features == null)
			{
				continue;
			}
			foreach (string text in enable_features)
			{
				if (!string.IsNullOrEmpty(text) && game.defs.Get<ProvinceFeature.Def>(text) != null)
				{
					if (sm_feature_map.ContainsKey(text))
					{
						Game.Log("Province feature " + text + " is enabled by more than one settlement type!Ignoring excess", Game.LogType.Warning);
					}
					else
					{
						sm_feature_map.Add(text, def);
					}
				}
			}
		}
		return sm_feature_map;
	}

	private static void DumpBindings(Dictionary<Realm, RealmData> bindData)
	{
		string text = "";
		foreach (KeyValuePair<Realm, RealmData> bindDatum in bindData)
		{
			text = text + "Realm: " + bindDatum.Key.ToString();
			text += "\n";
			text += bindDatum.Value.Dump();
			text += "\n";
		}
		Game.Log(text, Game.LogType.Message);
	}
}
