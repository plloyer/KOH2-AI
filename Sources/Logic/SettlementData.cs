using System;
using System.Collections.Generic;

namespace Logic;

public class RealmData
{
	public class SettlementData
	{
		public DT.Field field;

		public Settlement.Def def;

		public string desired_type;

		public Point position;

		public bool IsAssostatedFeature(string feature_id)
		{
			if (def == null)
			{
				return false;
			}
			if (def.enable_features == null || def.enable_features.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < def.enable_features.Length; i++)
			{
				if (def.enable_features[i] == feature_id)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool locked;

	public Realm realm;

	public bool isCoastal;

	public bool hasDistantPort;

	public Settlement.Def castle;

	public List<SettlementData> settlementData = new List<SettlementData>();

	public RealmFeaturesPreset.Def desired_preset;

	public List<string> desired_features = new List<string>();

	public int max_features_per_realm;

	public int demand_level;

	public Point castle_world_pos;

	public RealmData(Realm r)
	{
		realm = r;
		isCoastal = r.def.GetBool("has_coastal_city");
		hasDistantPort = r.def.GetBool("has_distant_port");
	}

	public override string ToString()
	{
		return "RealmData(" + realm?.name + ")";
	}

	public bool CanMeetPresetRequirements(Game game, RealmFeaturesPreset.Def preset, out string error)
	{
		if (preset == null)
		{
			error = "missing_preset";
			return false;
		}
		if (preset.requre_costal && !isCoastal)
		{
			error = "is_costal_not_met";
			return false;
		}
		if (preset.required_settlements != null && preset.required_settlements.Count > 0 && !CanHaveSettlements(preset.required_settlements))
		{
			error = "can_have_settlements_not_met";
			return false;
		}
		if (preset.required_province_features != null && preset.required_province_features.Count > 0)
		{
			for (int i = 0; i < preset.required_province_features.Count; i++)
			{
				string id = preset.required_province_features[i];
				ProvinceFeature.Def pfDef = game.defs.Get<ProvinceFeature.Def>(id);
				if (!CanHaveProviceFeature(game, pfDef, out var error2))
				{
					error = "can_have_features_not_met: " + error2;
					return false;
				}
			}
		}
		error = string.Empty;
		return true;
	}

	public bool CanHaveSettlement(string set_type)
	{
		for (int i = 0; i < this.settlementData.Count; i++)
		{
			if (this.settlementData[i].field.based_on.key == set_type)
			{
				return true;
			}
		}
		for (int j = 0; j < this.settlementData.Count; j++)
		{
			SettlementData settlementData = this.settlementData[j];
			if (settlementData.field.based_on.key == "Settlement" && string.IsNullOrEmpty(settlementData.desired_type))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanHaveSettlements(List<(string, int)> types)
	{
		if (types == null || types.Count == 0)
		{
			return false;
		}
		List<(string, int)> list = new List<(string, int)>(types);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < this.settlementData.Count; i++)
			{
				SettlementData obj = this.settlementData[i];
				(string, int) value = list[num];
				if (obj.field.based_on.key == value.Item1)
				{
					value.Item2--;
					list[num] = value;
					if (value.Item2 == 0)
					{
						list.RemoveAt(num);
						break;
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return true;
		}
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			for (int j = 0; j < this.settlementData.Count; j++)
			{
				SettlementData settlementData = this.settlementData[j];
				(string, int) value2 = list[num2];
				if (settlementData.field.based_on.key == "Settlement" && string.IsNullOrEmpty(settlementData.desired_type))
				{
					value2.Item2--;
					list[num2] = value2;
					if (value2.Item2 == 0)
					{
						list.RemoveAt(num2);
						break;
					}
				}
			}
		}
		return list.Count == 0;
	}

	public bool CanHaveProviceFeature(Game game, ProvinceFeature.Def pfDef, out string error, bool use_desired_settlement = false)
	{
		if (pfDef.requre_costal && !isCoastal)
		{
			error = "_not_costal";
			return false;
		}
		if (pfDef.requre_distant_port && !hasDistantPort)
		{
			error = "_no_distant_port";
			return false;
		}
		if (castle != null && !pfDef.ValidateArena(castle_world_pos, game.world_size))
		{
			error = "_not_in_valid_area";
			return false;
		}
		Dictionary<string, Settlement.Def> featureToSettlementMap = RealmFeaturesPreset.GetFeatureToSettlementMap(game);
		if (featureToSettlementMap.ContainsKey(pfDef.field.key))
		{
			string key = featureToSettlementMap[pfDef.field.key].field.key;
			if (key != "Castle")
			{
				if (use_desired_settlement)
				{
					bool flag = false;
					for (int i = 0; i < settlementData.Count; i++)
					{
						if (settlementData[i].desired_type == key)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						error = "_cant_have_desared_settelemnts";
						return false;
					}
				}
				else if (!CanHaveSettlement(key))
				{
					error = "_cant_have_requred_settelemnts";
					return false;
				}
			}
		}
		error = "";
		return true;
	}

	public bool CheckFeatureArena(Settlement.Def settlement_def, Point world_pos, Point world_size)
	{
		string[] enable_features = settlement_def.enable_features;
		if (enable_features == null)
		{
			return true;
		}
		foreach (string text in enable_features)
		{
			if (!string.IsNullOrEmpty(text))
			{
				ProvinceFeature.Def def = realm.game.defs.Get<ProvinceFeature.Def>(text);
				if (def != null && !def.ValidateArena(world_pos, world_size))
				{
					return false;
				}
			}
		}
		return true;
	}

	public void FillDefaults()
	{
		if (desired_features == null || desired_features.Count >= max_features_per_realm)
		{
			return;
		}
		int val = max_features_per_realm - desired_features.Count;
		HashSet<string> hashSet = new HashSet<string>();
		for (int i = 0; i < this.settlementData.Count; i++)
		{
			SettlementData settlementData = this.settlementData[i];
			if (settlementData != null && settlementData.def.default_features != null && settlementData.def.default_features.Count != 0)
			{
				for (int j = 0; j < settlementData.def.default_features.Count; j++)
				{
					hashSet.Add(settlementData.def.default_features[j]);
				}
			}
		}
		Random random = new Random();
		int num = Math.Min(val, hashSet.Count);
		for (int k = 0; k < num; k++)
		{
			int index = random.Next(0, desired_features.Count);
			string item = desired_features[index];
			desired_features.Add(item);
			desired_features.RemoveAt(index);
		}
	}

	public string Dump()
	{
		if (this.settlementData == null || this.settlementData.Count == 0)
		{
			return "Empty";
		}
		string text = (isCoastal ? "Coastal" : "Inland");
		text += " ";
		for (int i = 0; i < this.settlementData.Count; i++)
		{
			SettlementData settlementData = this.settlementData[i];
			text += settlementData.def.field.key;
			if (!string.IsNullOrEmpty(settlementData.desired_type))
			{
				text = text + "(" + settlementData.desired_type + ")";
			}
			text += ", ";
		}
		return text;
	}

	public bool ValidateSettlements(Game game, out string reason)
	{
		int num = 0;
		int num2 = 0;
		DT.Field field = game.dt.Find("SettlementsRandomizationWeights");
		int num3 = game.defs.GetBase<Settlement.Def>()?.field?.FindChild("max_specal_settlements_per_realm")?.Int(null, 3) ?? 3;
		for (int i = 0; i < this.settlementData.Count; i++)
		{
			SettlementData settlementData = this.settlementData[i];
			if (!(settlementData.desired_type == "Empty"))
			{
				if (field.FindChild(settlementData.desired_type) != null)
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
		}
		if (num <= 0)
		{
			reason = "base_settlement_count is " + num;
			return false;
		}
		if (num2 > num3)
		{
			reason = $"special_settlement_count ({num2}) is over limit {num3}";
			return false;
		}
		int num4 = num + num2;
		Settlement.Def def = game.defs.GetBase<Settlement.Def>();
		if (num4 < def.min_restriction_settlements && this.settlementData.Count >= def.min_restriction_settlements)
		{
			reason = $"total count ({num4}) is under limit {def.min_restriction_settlements}";
			return false;
		}
		if (num4 > def.max_restriction_settlements)
		{
			reason = $"total count ({num4}) is over limit {def.max_restriction_settlements}";
			return false;
		}
		reason = string.Empty;
		return true;
	}

	public bool ValidateFeatures(Game game)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		int num = 0;
		int num2 = 0;
		if (desired_features == null || desired_features.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < this.settlementData.Count; i++)
		{
			SettlementData settlementData = this.settlementData[i];
			if (!dictionary.ContainsKey(settlementData.def.id))
			{
				dictionary.Add(settlementData.def.id, 0);
			}
			dictionary[settlementData.def.id]++;
			for (int j = 0; j < desired_features.Count; j++)
			{
				string feature_id = desired_features[j];
				if (settlementData.IsAssostatedFeature(feature_id))
				{
					if (!dictionary2.ContainsKey(settlementData.def.id))
					{
						dictionary2.Add(settlementData.def.id, 0);
					}
					dictionary2[settlementData.def.id]++;
				}
			}
		}
		for (int k = 0; k < desired_features.Count; k++)
		{
			ProvinceFeature.Def pfDef = game.defs.Get<ProvinceFeature.Def>(desired_features[k]);
			if (!CanHaveProviceFeature(game, pfDef, out var _, use_desired_settlement: true))
			{
				num2++;
			}
		}
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			if (dictionary2.ContainsKey(item.Key) && dictionary2[item.Key] > item.Value)
			{
				num++;
			}
		}
		if (num2 <= 0)
		{
			return num <= 0;
		}
		return false;
	}

	public void SortSettlementsByDistance()
	{
		settlementData.Sort((SettlementData x, SettlementData y) => x.position.SqrDist(castle_world_pos).CompareTo(y.position.SqrDist(castle_world_pos)));
	}
}
