using System;
using System.Collections.Generic;

namespace Logic;

public class ProvinceFeatureDistribution
{
	private struct SettlementCountMap
	{
		public Settlement.Def def;

		public float weight;

		public Dictionary<string, float> mod_weight;

		public float settlement_repeating_chance_mul;

		public int assigned;

		public void Add(int cnt)
		{
			assigned += cnt;
		}

		public float GetWeight(RealmData rd)
		{
			float num = weight;
			if (rd == null)
			{
				return num;
			}
			if (mod_weight != null)
			{
				foreach (KeyValuePair<string, float> item in mod_weight)
				{
					if (rd.realm.def.GetBool(item.Key))
					{
						num += item.Value;
					}
				}
			}
			if (settlement_repeating_chance_mul != 1f)
			{
				for (int i = 0; i < rd.settlementData.Count; i++)
				{
					if (rd.settlementData[i].desired_type == def.field.key)
					{
						num *= settlement_repeating_chance_mul;
						break;
					}
				}
			}
			return num;
		}

		public override string ToString()
		{
			return $"SettlementCountMap({def?.id}, {weight}, {assigned})";
		}
	}

	private const int default_max_specal_settlements_per_realm = 3;

	private static Dictionary<Game, ProvinceFeatureDistribution> distributions = new Dictionary<Game, ProvinceFeatureDistribution>();

	public Dictionary<Realm, RealmData> data;

	public Dictionary<RealmFeaturesPreset.Def, List<RealmData>> presets;

	private static int maxDepth = 3;

	private static Dictionary<Game, int[,]> realmDistanceMap = new Dictionary<Game, int[,]>();

	public static ProvinceFeatureDistribution GetDistribution(Game game)
	{
		if (distributions.ContainsKey(game))
		{
			return distributions[game];
		}
		return null;
	}

	public static void ClearDistribution()
	{
		distributions.Clear();
	}

	public static bool ShowWarrings(Game game)
	{
		return game.defs_map == "europe";
	}

	public static ProvinceFeatureDistribution AnalyzeRealms(Game game, string map_name)
	{
		ClearDistribution();
		if (game == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(map_name))
		{
			return null;
		}
		DT.Field field = game.dt.Find("Settlements." + map_name);
		if (field == null || field.children == null)
		{
			Game.Log("Missing Settlements data for map " + map_name + "! Search path:Settlements." + map_name, Game.LogType.Error);
			return null;
		}
		Dictionary<Realm, RealmData> dictionary = new Dictionary<Realm, RealmData>(game.realms.Count);
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (!realm.IsSeaRealm())
			{
				RealmData value = new RealmData(realm);
				dictionary.Add(realm, value);
			}
		}
		BindSettlements(game, dictionary, field.children);
		Dictionary<RealmFeaturesPreset.Def, List<RealmData>> presetMap = new Dictionary<RealmFeaturesPreset.Def, List<RealmData>>();
		ExtractValidPresets(game, presetMap, dictionary, map_name);
		AllocateGeographicalFeatures(game, dictionary);
		AllocatePresets(game, presetMap, prefred_only: true);
		AllocatePresets(game, presetMap);
		ApplyPresets(game, dictionary);
		AllocateRandomSettlements(game, dictionary);
		AllocateRandomFeatures(game, dictionary);
		ValidateSettlements(game, dictionary);
		ValidateFeatures(game, dictionary);
		Sort(game, dictionary);
		ProvinceFeatureDistribution provinceFeatureDistribution = new ProvinceFeatureDistribution
		{
			data = dictionary,
			presets = presetMap
		};
		distributions.Add(game, provinceFeatureDistribution);
		return provinceFeatureDistribution;
	}

	private static void Sort(Game game, Dictionary<Realm, RealmData> realmMap)
	{
		foreach (KeyValuePair<Realm, RealmData> item in realmMap)
		{
			RealmData value = item.Value;
			if (value != null && value.desired_features != null && value.desired_features.Count != 0)
			{
				value.desired_features.Sort(delegate(string x, string y)
				{
					ProvinceFeature.Def def = game.defs.Get<ProvinceFeature.Def>(x);
					ProvinceFeature.Def def2 = game.defs.Get<ProvinceFeature.Def>(y);
					return def.sort_id.CompareTo(def2.sort_id);
				});
			}
		}
	}

	private static void ExtractValidPresets(Game game, Dictionary<RealmFeaturesPreset.Def, List<RealmData>> presetMap, Dictionary<Realm, RealmData> realmMap, string map_name)
	{
		List<RealmFeaturesPreset.Def> defs = game.defs.GetDefs<RealmFeaturesPreset.Def>();
		if (defs == null)
		{
			return;
		}
		for (int i = 0; i < defs.Count; i++)
		{
			RealmFeaturesPreset.Def def = defs[i];
			if (!def.dt_def.path.Contains("RealmFeaturesPresets." + map_name))
			{
				continue;
			}
			foreach (KeyValuePair<Realm, RealmData> item in realmMap)
			{
				RealmData value = item.Value;
				float chance;
				if (value.CanMeetPresetRequirements(game, def, out var error))
				{
					if (!presetMap.ContainsKey(def))
					{
						presetMap.Add(def, new List<RealmData>());
					}
					presetMap[def].Add(value);
					value.demand_level++;
				}
				else if (ShowWarrings(game) && def.IsDesiredRealm(value, out chance))
				{
					Game.Log($"Realm {value.realm} is marked as a desired realm for feature preset {def.id}, but can not meet the requirements! Error: {error}!", Game.LogType.Warning);
				}
			}
		}
	}

	private static void DumpPresetToRealmData(RealmFeaturesPreset.Def def, List<RealmData> realms)
	{
		string text = "preset :" + def.id + "\n";
		if (realms == null || realms.Count == 0)
		{
			text += "Empty";
		}
		for (int i = 0; i < realms.Count; i++)
		{
			text = text + realms[i].realm.name + ((i < realms.Count - 1) ? ", " : "");
		}
		Game.Log(text, Game.LogType.Message);
	}

	private static void AllocatePresets(Game game, Dictionary<RealmFeaturesPreset.Def, List<RealmData>> presetMap, bool prefred_only = false)
	{
		List<RealmFeaturesPreset.Def> list = new List<RealmFeaturesPreset.Def>(presetMap.Keys);
		list.Sort((RealmFeaturesPreset.Def x, RealmFeaturesPreset.Def y) => presetMap[x].Count.CompareTo(presetMap[y].Count));
		bool flag = true;
		for (int num = 0; num < list.Count; num++)
		{
			RealmFeaturesPreset.Def def = list[num];
			List<RealmData> collection = presetMap[def];
			int num2 = 0;
			while (true)
			{
				int num3 = def.preferred_min_distance + num2;
				if (num3 < 0)
				{
					flag = false;
					break;
				}
				if (AllocatePreset(game, def, new List<RealmData>(collection), num3, prefred_only) <= 0)
				{
					break;
				}
				num2--;
			}
		}
		if (ShowWarrings(game) && !flag)
		{
			Game.Log("Could not distribute all province features", Game.LogType.Warning);
		}
	}

	private static int AllocatePreset(Game game, RealmFeaturesPreset.Def preset, List<RealmData> available, int min_dist, bool prefred_only = false)
	{
		int occurances_curr = preset.occurances_curr;
		int num = 0;
		if (available == null || available.Count == 0)
		{
			return 0;
		}
		for (int num2 = available.Count - 1; num2 >= 0; num2--)
		{
			RealmFeaturesPreset.Def desired_preset = available[num2].desired_preset;
			if (desired_preset != null)
			{
				available.RemoveAt(num2);
			}
			if (desired_preset == preset)
			{
				num++;
			}
		}
		if (num >= occurances_curr)
		{
			return 0;
		}
		occurances_curr -= num;
		occurances_curr = Math.Min(occurances_curr, available.Count);
		List<RealmData> list = new List<RealmData>(occurances_curr);
		if (prefred_only)
		{
			List<RealmData> list2 = new List<RealmData>();
			for (int num3 = available.Count - 1; num3 >= 0; num3--)
			{
				RealmData realmData = available[num3];
				if (preset.IsDesiredRealm(realmData, out var chance) && (float)game.Random(0, 100) < chance)
				{
					list2.Add(realmData);
					available.Remove(realmData);
				}
			}
			game.Shuffle(list2);
			for (int i = 0; i < list2.Count && i < occurances_curr; i++)
			{
				list2[i].desired_preset = preset;
				list.Add(list2[i]);
				num++;
			}
			return 0;
		}
		while (occurances_curr > 0)
		{
			bool flag = false;
			RealmData realmData2 = null;
			while (!flag && available.Count > 0)
			{
				int index = game.Random(0, available.Count);
				realmData2 = available[index];
				if (CheckPorximity(game, realmData2, list, min_dist) > 0)
				{
					available.Remove(realmData2);
					continue;
				}
				realmData2.desired_preset = preset;
				available.Remove(realmData2);
				list.Add(realmData2);
				num++;
				flag = true;
			}
			occurances_curr--;
		}
		return preset.occurances_curr - num;
	}

	private static void BindSettlements(Game game, Dictionary<Realm, RealmData> realmDataMap, List<DT.Field> settlement_defs)
	{
		if (game == null)
		{
			return;
		}
		for (int i = 0; i < settlement_defs.Count; i++)
		{
			DT.Field field = settlement_defs[i];
			if (string.IsNullOrEmpty(field.key) || Settlement.Validate(game, field, supress_warrings: true) != "ok")
			{
				continue;
			}
			Point point = field.GetPoint("position");
			int nearbyLandRealm = game.GetNearbyLandRealm(point);
			Realm realm = game.GetRealm(nearbyLandRealm);
			if (realm != null && !realm.IsSeaRealm())
			{
				RealmData.SettlementData settlementData = new RealmData.SettlementData();
				settlementData.field = field;
				settlementData.def = game.defs.Get<Settlement.Def>(field.base_path);
				settlementData.position = point;
				string base_path = field.base_path;
				string text = Settlement.ParseType(game.dt, base_path);
				if (text != null && text.Equals("Castle", StringComparison.OrdinalIgnoreCase))
				{
					realmDataMap[realm].castle = settlementData.def;
					realmDataMap[realm].castle_world_pos = point;
				}
				else
				{
					realmDataMap[realm].settlementData.Add(settlementData);
				}
			}
		}
		foreach (KeyValuePair<Realm, RealmData> item in realmDataMap)
		{
			item.Value.SortSettlementsByDistance();
		}
	}

	private static void ApplyPresets(Game game, Dictionary<Realm, RealmData> realmData)
	{
		foreach (KeyValuePair<Realm, RealmData> realmDatum in realmData)
		{
			RealmData value = realmDatum.Value;
			if (value != null && value.desired_preset != null)
			{
				ApplyPreset(game, realmDatum.Value);
			}
		}
	}

	private static void ApplyPreset(Game game, RealmData realmData)
	{
		RealmFeaturesPreset.Def desired_preset = realmData.desired_preset;
		if (desired_preset == null)
		{
			return;
		}
		List<(string, int)> list = new List<(string, int)>(desired_preset.required_settlements);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			(string, int) tuple = list[num];
			for (int i = 0; i < realmData.settlementData.Count; i++)
			{
				if (realmData.settlementData[i].desired_type == tuple.Item1)
				{
					tuple.Item2--;
					list[num] = tuple;
					if (tuple.Item2 == 0)
					{
						break;
					}
				}
			}
			if (tuple.Item2 > 0)
			{
				for (int j = 0; j < realmData.settlementData.Count; j++)
				{
					RealmData.SettlementData settlementData = realmData.settlementData[j];
					if (string.IsNullOrEmpty(settlementData.desired_type))
					{
						(settlementData.desired_type, _) = tuple;
						tuple.Item2--;
						list[num] = tuple;
						if (tuple.Item2 == 0)
						{
							break;
						}
					}
				}
			}
			list.RemoveAt(num);
		}
		realmData.desired_features.AddRange(desired_preset.required_province_features);
	}

	private static float GetWeight(DT.Field w_field, Settlement.Def s_def)
	{
		return w_field.FindChild(s_def.name)?.Float() ?? 0f;
	}

	private static Dictionary<string, float> GetOverrideWeights(DT.Field w_field, Settlement.Def s_def)
	{
		DT.Field field = w_field.FindChild(s_def.name);
		if (field == null)
		{
			return null;
		}
		List<DT.Field> list = field.Children();
		if (list == null || list.Count == 0)
		{
			return null;
		}
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		for (int i = 0; i < list.Count; i++)
		{
			DT.Field field2 = list[i];
			dictionary.Add(field2.key, field2.Float());
		}
		return dictionary;
	}

	private static bool TryGetRandomSettlementType(Game game, RealmData realmData, List<SettlementCountMap> settlementTypes, out SettlementCountMap result)
	{
		if (settlementTypes == null || settlementTypes.Count == 0)
		{
			result = default(SettlementCountMap);
			return false;
		}
		float num = 0f;
		for (int i = 0; i < settlementTypes.Count; i++)
		{
			num += settlementTypes[i].GetWeight(realmData);
		}
		float num2 = game.Random(0f, num);
		for (int j = 0; j < settlementTypes.Count; j++)
		{
			SettlementCountMap settlementCountMap = settlementTypes[j];
			if (num2 < settlementCountMap.GetWeight(realmData))
			{
				result = settlementCountMap;
				return true;
			}
			num2 -= settlementCountMap.GetWeight(realmData);
		}
		result = default(SettlementCountMap);
		return false;
	}

	private static void AllocateRandomSettlements(Game game, Dictionary<Realm, RealmData> realmMap)
	{
		Settlement.Def def = game.defs.GetBase<Settlement.Def>();
		List<Settlement.Def> defs = game.defs.GetDefs<Settlement.Def>();
		DT.Field field = game.dt.Find("SettlementsRandomizationWeights");
		DT.Field field2 = game.dt.Find("SpecialSettlementsRandomizationWeights");
		List<SettlementCountMap> list = new List<SettlementCountMap>();
		List<SettlementCountMap> list2 = new List<SettlementCountMap>();
		for (int i = 0; i < defs.Count; i++)
		{
			string path = defs[i].dt_def.path;
			if (!path.Contains("Settlements.") && !path.Contains("Castle"))
			{
				bool flag = field.FindChild(defs[i].field.key) != null;
				bool flag2 = field2.FindChild(defs[i].field.key) != null;
				SettlementCountMap item = new SettlementCountMap
				{
					def = defs[i],
					weight = GetWeight(flag ? field : field2, defs[i]),
					mod_weight = GetOverrideWeights(flag ? field : field2, defs[i]),
					settlement_repeating_chance_mul = (flag ? def.settlement_repeating_chance_mul : 1f),
					assigned = 0
				};
				if (flag)
				{
					list.Add(item);
				}
				else if (flag2)
				{
					list2.Add(item);
				}
			}
		}
		if (list.Count + list2.Count <= 1)
		{
			return;
		}
		int max_types = Math.Min(field.value, list.Count - 1);
		int max_types2 = Math.Min(field2.value, list2.Count - 1);
		int num = def.field.FindChild("max_specal_settlements_per_realm")?.Int(null, 3) ?? 3;
		foreach (KeyValuePair<Realm, RealmData> item4 in realmMap)
		{
			RealmData value = item4.Value;
			if (value == null || value.settlementData == null || value.settlementData.Count == 0)
			{
				continue;
			}
			int num2 = value.settlementData.Count;
			if (num2 > def.min_restriction_settlements)
			{
				num2 = game.Random(def.min_restriction_settlements, Math.Min(def.max_restriction_settlements, num2) + 1);
			}
			int num3 = game.Random(0, num + 1);
			if (num3 >= value.settlementData.Count)
			{
				num3 = value.settlementData.Count - 1;
			}
			List<SettlementCountMap> list3 = new List<SettlementCountMap>(list2);
			for (int num4 = list3.Count - 1; num4 >= 0; num4--)
			{
				if (!value.CheckFeatureArena(list3[num4].def, value.castle_world_pos, game.world_size))
				{
					list3.RemoveAt(num4);
				}
			}
			int num5 = PopulateOptions(value, list3, num3, max_types2);
			List<SettlementCountMap> list4 = new List<SettlementCountMap>(list);
			for (int num6 = list4.Count - 1; num6 >= 0; num6--)
			{
				if (!value.CheckFeatureArena(list4[num6].def, value.castle_world_pos, game.world_size))
				{
					list4.RemoveAt(num6);
				}
			}
			PopulateOptions(value, list4, num2 - num5, max_types);
			for (int j = 0; j < value.settlementData.Count; j++)
			{
				if (string.IsNullOrEmpty(value.settlementData[j].desired_type))
				{
					value.settlementData[j].desired_type = "Empty";
				}
			}
		}
		int PopulateOptions(RealmData realmData, List<SettlementCountMap> options, int max_picks, int num10)
		{
			if (max_picks <= 0)
			{
				return 0;
			}
			int num7 = 0;
			for (int k = 0; k < realmData.settlementData.Count; k++)
			{
				RealmData.SettlementData settlementData = realmData.settlementData[k];
				if (settlementData != null)
				{
					if (!string.IsNullOrEmpty(settlementData.desired_type))
					{
						for (int l = 0; l < options.Count; l++)
						{
							SettlementCountMap value2 = options[l];
							if (value2.def.id == settlementData.desired_type)
							{
								value2.assigned++;
								options[l] = value2;
								num7++;
							}
						}
					}
					else
					{
						if (options == null || options.Count == 0)
						{
							continue;
						}
						if (!TryGetRandomSettlementType(game, realmData, options, out var result))
						{
							result = options[game.Random(0, options.Count)];
						}
						num7++;
						settlementData.desired_type = result.def.id;
						result.assigned++;
						for (int m = 0; m < options.Count; m++)
						{
							if (options[m].def.id == result.def.id)
							{
								options[m] = result;
							}
						}
					}
					int num8 = 0;
					for (int num9 = options.Count - 1; num9 >= 0; num9--)
					{
						SettlementCountMap item2 = options[num9];
						if (item2.assigned > 0)
						{
							num8++;
						}
						if (item2.assigned >= item2.def.max_identical_settlement_per_realm)
						{
							options.Remove(item2);
						}
					}
					if (num8 >= num10)
					{
						for (int num11 = options.Count - 1; num11 >= 0; num11--)
						{
							SettlementCountMap item3 = options[num11];
							if (item3.assigned == 0)
							{
								options.Remove(item3);
							}
						}
					}
					if (num7 >= max_picks)
					{
						break;
					}
				}
			}
			return num7;
		}
	}

	private static void ValidateSettlements(Game game, Dictionary<Realm, RealmData> realmMap)
	{
		string text = "Invalid Settlements: ";
		bool flag = false;
		foreach (KeyValuePair<Realm, RealmData> item in realmMap)
		{
			RealmData value = item.Value;
			if (!value.ValidateSettlements(game, out var reason))
			{
				text = text + "\n" + value.realm?.name + " Reason: " + reason + ", ";
				flag = true;
			}
		}
		if (flag)
		{
			Game.Log(text, Game.LogType.Message);
		}
	}

	private static void ValidateFeatures(Game game, Dictionary<Realm, RealmData> realmMap)
	{
		string text = "Invalid PFs: ";
		bool flag = false;
		foreach (KeyValuePair<Realm, RealmData> item in realmMap)
		{
			RealmData value = item.Value;
			if (!value.ValidateFeatures(game))
			{
				text = text + value.realm?.name + ", ";
				flag = true;
			}
		}
		if (flag)
		{
			Game.Log(text, Game.LogType.Message);
		}
	}

	private static void AllocateGeographicalFeatures(Game game, Dictionary<Realm, RealmData> realmMap)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<Realm, RealmData> item2 in realmMap)
		{
			list.Clear();
			RealmData value = item2.Value;
			if (value.realm.def.GetBool("has_river_city"))
			{
				list.Add("Rivers");
			}
			if (value.realm.def.GetBool("has_mountains"))
			{
				list.Add("Mountain");
			}
			if (value.realm.def.GetBool("has_coastal_city"))
			{
				list.Add("Coastal");
			}
			if (value.realm.def.GetBool("has_distant_port"))
			{
				list.Add("Pirates");
			}
			for (int i = 0; i < list.Count; i++)
			{
				string item = list[i];
				ProvinceFeature.Def def = game.defs.Get<ProvinceFeature.Def>(list[i]);
				if (def != null && def.GetSpawnChance(item2.Key) > (float)game.Random(0, 100))
				{
					value.desired_features.Add(item);
				}
			}
		}
	}

	private static void AllocateRandomFeatures(Game game, Dictionary<Realm, RealmData> realmMap)
	{
		Dictionary<string, Settlement.Def> baseSettlementTypes = GetBaseSettlementTypes(game);
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		int max_PF_per_province = game.defs.GetBase<ProvinceFeature.Def>().max_PF_per_province;
		foreach (KeyValuePair<Realm, RealmData> item6 in realmMap)
		{
			RealmData value = item6.Value;
			if (value == null)
			{
				continue;
			}
			dictionary.Clear();
			int num = ((value.desired_features != null) ? value.desired_features.Count : 0);
			if (num >= max_PF_per_province)
			{
				continue;
			}
			for (int i = 0; i < value.settlementData.Count; i++)
			{
				RealmData.SettlementData settlementData = value.settlementData[i];
				if (settlementData.desired_type != null && baseSettlementTypes.ContainsKey(settlementData.desired_type))
				{
					Settlement.Def def = baseSettlementTypes[settlementData.desired_type];
					if (def.enable_features != null && def.enable_features.Length != 0)
					{
						int value2 = 0;
						dictionary.TryGetValue(def.id, out value2);
						dictionary[def.id] = value2 + 1;
					}
				}
			}
			if (value.castle != null && baseSettlementTypes.ContainsKey("Castle"))
			{
				Settlement.Def def2 = baseSettlementTypes["Castle"];
				if (def2.enable_features != null && def2.enable_features.Length != 0)
				{
					int value3 = 0;
					dictionary.TryGetValue(def2.id, out value3);
					dictionary[def2.id] = value3 + 1;
				}
			}
			foreach (KeyValuePair<string, int> item7 in dictionary)
			{
				string key = item7.Key;
				int num2 = item7.Value;
				Settlement.Def def3 = baseSettlementTypes[key];
				int num3 = 0;
				if (def3.field.key == "Castle")
				{
					num2 = def3.max_allowed_features_per_settlement_type;
				}
				List<string> list = new List<string>();
				for (int j = 0; j < def3.enable_features.Length; j++)
				{
					string item = def3.enable_features[j];
					if (((!(def3?.secoundary_features?.Contains(item))) ?? true) && !list.Contains(item))
					{
						list.Add(item);
					}
				}
				game.Shuffle(list);
				if (value.desired_features != null && value.desired_features.Count > 0)
				{
					for (int num4 = list.Count - 1; num4 >= 0; num4--)
					{
						if (value.desired_features.Contains(list[num4]))
						{
							num3++;
							num2--;
							list.RemoveAt(num4);
						}
					}
					if (def3.exclusive_features != null && def3.exclusive_features.Count > 0)
					{
						int k = 0;
						for (int count = value.desired_features.Count; k < count; k++)
						{
							string item2 = value.desired_features[k];
							if (def3.exclusive_features.Contains(item2))
							{
								for (int l = 0; l < def3.exclusive_features.Count; l++)
								{
									string item3 = def3.exclusive_features[l];
									list.Remove(item3);
								}
							}
						}
					}
				}
				if (def3.secoundary_features != null && def3.secoundary_features.Count > 0)
				{
					game.Shuffle(def3.secoundary_features);
					for (int m = 0; m < def3.secoundary_features.Count; m++)
					{
						string text = def3.secoundary_features[m];
						if (!value.desired_features.Contains(text))
						{
							ProvinceFeature.Def def4 = game.defs.Get<ProvinceFeature.Def>(text);
							if (def4 != null && def4.GetSpawnChance(item6.Key) > (float)game.Random(0, 100))
							{
								value.desired_features.Add(text);
								break;
							}
						}
					}
				}
				if (num2 <= 0)
				{
					continue;
				}
				int num5 = 0;
				while (list.Count > 0)
				{
					int index = game.Random(0, list.Count);
					string text2 = list[index];
					list.RemoveAt(index);
					ProvinceFeature.Def def5 = game.defs.Get<ProvinceFeature.Def>(text2);
					if (def5 == null || (def5.requre_costal && !value.isCoastal) || (def5.requre_distant_port && !value.hasDistantPort) || !def5.ValidateArena(value.castle_world_pos, game.world_size) || value.desired_features.Contains(text2) || game.Random(0f, 100f) >= def5.GetSpawnChance(item6.Key) || (def5.special && num5 >= max_PF_per_province))
					{
						continue;
					}
					if (text2 == "Brine" && value.desired_features.Contains("RockSalt"))
					{
						if (game.Random(0f, 100f) >= 50f)
						{
							continue;
						}
						value.desired_features.Remove("RockSalt");
					}
					value.desired_features.Add(text2);
					if (def5.special)
					{
						num5++;
					}
					if (++num3 >= def3.max_allowed_features_per_settlement_type || num3 >= num2 || num3 + num >= max_PF_per_province)
					{
						break;
					}
					if (def3.exclusive_features != null && def3.exclusive_features.Contains(text2))
					{
						for (int n = 0; n < def3.exclusive_features.Count; n++)
						{
							string item4 = def3.exclusive_features[n];
							list.Remove(item4);
						}
					}
				}
				if (num3 <= 0 && def3.default_features != null && def3.default_features.Count > 0)
				{
					int index2 = game.Random(0, def3.default_features.Count);
					string item5 = def3.default_features[index2];
					if (!value.desired_features.Contains(item5))
					{
						value.desired_features.Add(item5);
					}
				}
			}
		}
	}

	private static Dictionary<string, Settlement.Def> GetBaseSettlementTypes(Game game)
	{
		game.defs.GetBase<Settlement.Def>();
		List<Settlement.Def> defs = game.defs.GetDefs<Settlement.Def>();
		new Dictionary<string, int>();
		Dictionary<string, Settlement.Def> dictionary = new Dictionary<string, Settlement.Def>();
		for (int i = 0; i < defs.Count; i++)
		{
			string path = defs[i].dt_def.path;
			if (!path.Contains("Settlements."))
			{
				dictionary.Add(path, defs[i]);
			}
		}
		return dictionary;
	}

	private static int CheckPorximity(Game game, RealmData realm_data, List<RealmData> relevant_neighbors, int min_range)
	{
		if (realm_data == null)
		{
			return 0;
		}
		if (relevant_neighbors == null || relevant_neighbors.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < relevant_neighbors.Count; i++)
		{
			RealmData realmData = relevant_neighbors[i];
			if (realmData != null && realmData != realm_data && GetDist(game, realm_data.realm, realmData.realm) <= min_range)
			{
				num++;
			}
		}
		return num;
	}

	private static int GetDist(Game game, Realm r1, Realm r2)
	{
		if (!realmDistanceMap.ContainsKey(game) || realmDistanceMap[game] == null)
		{
			BuildRealmNeigborMap(game);
		}
		if (realmDistanceMap[game].GetLength(0) <= r1.id)
		{
			return int.MaxValue;
		}
		if (realmDistanceMap[game].GetLength(1) <= r2.id)
		{
			return int.MaxValue;
		}
		return realmDistanceMap[game][r1.id, r2.id];
	}

	private static void BuildRealmNeigborMap(Game game)
	{
		if (game == null || game.realms == null)
		{
			if (realmDistanceMap.ContainsKey(game))
			{
				realmDistanceMap[game] = null;
			}
			return;
		}
		if (!realmDistanceMap.ContainsKey(game))
		{
			realmDistanceMap.Add(game, new int[game.realms.Count + 1, game.realms.Count + 1]);
		}
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (realm.IsSeaRealm())
			{
				continue;
			}
			for (int j = 0; j < game.realms.Count; j++)
			{
				Realm realm2 = game.realms[j];
				if (!realm2.IsSeaRealm())
				{
					realmDistanceMap[game][realm.id - 1, realm2.id - 1] = game.RealmDistance(realm.id, realm2.id, goThroughSeas: false, useLogicNeighbors: false, maxDepth);
					if (realmDistanceMap[game][realm.id - 1, realm2.id - 1] == -1)
					{
						realmDistanceMap[game][realm.id - 1, realm2.id - 1] = maxDepth;
					}
				}
			}
		}
	}

	private static void DumpDefs(Dictionary<Realm, RealmData> realmData)
	{
		string text = "";
		foreach (KeyValuePair<Realm, RealmData> realmDatum in realmData)
		{
			RealmData value = realmDatum.Value;
			if (value.settlementData == null || value.settlementData.Count == 0)
			{
				continue;
			}
			for (int i = 0; i < value.settlementData.Count; i++)
			{
				RealmData.SettlementData settlementData = value.settlementData[i];
				if (settlementData.def == null)
				{
					text += "Settlement has no def ...? ";
					continue;
				}
				text += $"Settlement {settlementData.def} features: ";
				if (settlementData.def.enable_features != null)
				{
					for (int j = 0; j < settlementData.def.enable_features.Length; j++)
					{
						text = text + settlementData.def.enable_features[j] + "\n";
					}
				}
			}
			text += "\n";
		}
		Game.Log(text, Game.LogType.Message);
	}

	private static void DumpRealms(Dictionary<Realm, RealmData> realmData)
	{
		string text = "";
		foreach (KeyValuePair<Realm, RealmData> realmDatum in realmData)
		{
			RealmData value = realmDatum.Value;
			text += $"{realmDatum.Key} > desired_preset : {value.desired_preset} \n";
			text += "Desired features ";
			if (value.desired_features != null && value.desired_features.Count == 0)
			{
				for (int i = 0; i < value.desired_features.Count; i++)
				{
					text = text + value.desired_features[i] + ", ";
				}
			}
			text += "\n";
			text += value.Dump();
			text += "\n";
			text += "\n";
		}
		Game.Log(text, Game.LogType.Message);
	}

	public string GetPrefredSettlementType(DT.Field sf)
	{
		foreach (KeyValuePair<Realm, RealmData> datum in data)
		{
			RealmData value = datum.Value;
			for (int i = 0; i < value.settlementData.Count; i++)
			{
				RealmData.SettlementData settlementData = value.settlementData[i];
				if (settlementData.field == sf && !string.IsNullOrEmpty(settlementData.desired_type))
				{
					return settlementData.desired_type;
				}
			}
		}
		return sf.base_path;
	}

	private static void DumpPresetMap(Dictionary<RealmFeaturesPreset.Def, List<RealmData>> presetMap, bool show_chosen_only = false)
	{
		string text = "";
		foreach (KeyValuePair<RealmFeaturesPreset.Def, List<RealmData>> item in presetMap)
		{
			List<RealmData> value = item.Value;
			RealmFeaturesPreset.Def key = item.Key;
			if (show_chosen_only)
			{
				text = text + "THE Chosen Realms for Preset : " + key.ToString() + " are ";
				for (int i = 0; i < value.Count; i++)
				{
					RealmData realmData = value[i];
					if (realmData.desired_preset != null && realmData.desired_preset == key)
					{
						text = text + value[i].realm.name + ", ";
					}
				}
				text += "\n";
			}
			else
			{
				text = text + "Preset : " + key.ToString();
				text += "\n";
				text += $"has {item.Value.Count} valid candidates: ";
				for (int j = 0; j < value.Count; j++)
				{
					text = text + value[j].realm.name + ", ";
				}
				text += "\n";
			}
		}
		Game.Log(text, Game.LogType.Message);
	}

	public List<string> GetDesiredProvinceFeaturesType(Realm r)
	{
		if (data.ContainsKey(r))
		{
			return data[r].desired_features;
		}
		return null;
	}
}
