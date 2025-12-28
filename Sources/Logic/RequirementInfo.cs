using System;
using System.Collections.Generic;

namespace Logic;

public class Building : BaseObject, IVars
{
	public class Def : Logic.Def
	{
		public class PerSettlementModidfiers
		{
			public List<StatModifier.Def> realm_mods;

			public List<StatModifier.Def> kingdom_mods;

			public void AddMod(StatModifier.Def mod_def)
			{
				List<StatModifier.Def> list;
				if (mod_def?.field?.type == "kingdom_mod")
				{
					if (kingdom_mods == null)
					{
						kingdom_mods = new List<StatModifier.Def>();
					}
					list = kingdom_mods;
				}
				else
				{
					if (realm_mods == null)
					{
						realm_mods = new List<StatModifier.Def>();
					}
					list = realm_mods;
				}
				for (int i = 0; i < list.Count; i++)
				{
					StatModifier.Def def = list[i];
					if (def.stat_name == mod_def.stat_name && def.settlement_type == mod_def.settlement_type && def.condition == mod_def.condition && def.type == mod_def.type)
					{
						def.value += mod_def.value;
						return;
					}
				}
				list.Add(mod_def.Copy());
			}

			public void AddMods(List<StatModifier.Def> mods)
			{
				if (mods != null)
				{
					for (int i = 0; i < mods.Count; i++)
					{
						StatModifier.Def mod_def = mods[i];
						AddMod(mod_def);
					}
				}
			}

			public void AddMods(PerSettlementModidfiers mods)
			{
				AddMods(mods?.realm_mods);
				AddMods(mods?.kingdom_mods);
			}
		}

		public struct ProducedResource
		{
			public string type;

			public string resource;

			public ResourceInfo.Availability availability;

			public override string ToString()
			{
				return $"[{availability}][{type}] {resource}";
			}
		}

		public class Bonuses
		{
			public Def building_def;

			public string location;

			public bool flat;

			public DT.Field field;

			public int min_level;

			public int at_level;

			public Dictionary<string, List<ConditionalProduction.Def>> productions;

			public Dictionary<string, PerSettlementModidfiers> stat_mods;

			public Bonuses per_level;

			public Bonuses region;

			public void AddProduction(int line_number, string type, Resource production, DT.Field condition)
			{
				if (productions == null)
				{
					productions = new Dictionary<string, List<ConditionalProduction.Def>>();
				}
				if (!productions.TryGetValue(type, out var value))
				{
					value = new List<ConditionalProduction.Def>();
					productions.Add(type, value);
				}
				for (int i = 0; i < value.Count; i++)
				{
					ConditionalProduction.Def def = value[i];
					if (def.settlement_type == type && def.condition == condition)
					{
						def.resources.Add(production, 1f);
						return;
					}
				}
				value.Add(new ConditionalProduction.Def
				{
					line_number = line_number,
					bonuses = this,
					settlement_type = type,
					resources = production.Copy(),
					condition = condition
				});
			}

			public void AddProductions(string type, List<ConditionalProduction.Def> productions)
			{
				for (int i = 0; i < productions.Count; i++)
				{
					ConditionalProduction.Def def = productions[i];
					AddProduction(def.line_number, type, def.resources, def.condition);
				}
			}

			public void AddProductions(Dictionary<string, List<ConditionalProduction.Def>> productions)
			{
				if (productions == null)
				{
					return;
				}
				foreach (KeyValuePair<string, List<ConditionalProduction.Def>> production in productions)
				{
					string key = production.Key;
					List<ConditionalProduction.Def> value = production.Value;
					AddProductions(key, value);
				}
			}

			public void AddProductions(Bonuses b)
			{
				AddProductions(b.productions);
			}

			public void AddMods(string type, PerSettlementModidfiers mods)
			{
				if (mods != null)
				{
					if (stat_mods == null)
					{
						stat_mods = new Dictionary<string, PerSettlementModidfiers>();
					}
					if (!stat_mods.TryGetValue(type, out var value))
					{
						value = new PerSettlementModidfiers();
						stat_mods.Add(type, value);
					}
					value.AddMods(mods);
				}
			}

			public void AddMods(Dictionary<string, PerSettlementModidfiers> mods)
			{
				if (mods == null)
				{
					return;
				}
				foreach (KeyValuePair<string, PerSettlementModidfiers> mod in mods)
				{
					string key = mod.Key;
					PerSettlementModidfiers value = mod.Value;
					AddMods(key, value);
				}
			}

			public void AddMods(Bonuses b)
			{
				AddMods(b.stat_mods);
			}

			public void AddBonuses(Bonuses b)
			{
				AddProductions(b);
				AddMods(b);
				_ = b.per_level;
				_ = b.region;
			}
		}

		public class RequirementInfo
		{
			public DT.Field field;

			public int amount;

			public string type;

			public DT.Field def;

			public string key => field.key;

			public RequirementInfo(DT.Field field)
			{
				this.field = field;
				amount = field.Int(null, 1);
			}

			public override string ToString()
			{
				return $"{type} {key}: {amount}";
			}
		}

		public static bool SOFT_RESOURCE_REQUIREMENTS = false;

		public List<District.Def> districts;

		public District.Def upgrades;

		public List<Def> variants;

		public Def variant_of;

		public List<RequirementInfo> requires;

		public List<RequirementInfo> requires_or;

		public List<ProducedResource> produces;

		public List<ProducedResource> produces_completed;

		public string piety_type;

		public List<Realm.RegionPopInfModifier> regPopInfMods;

		public Resource cost;

		public float upkeep;

		public float progressive_gold_cost_for_building;

		public float progressive_gold_cost_for_upgrades;

		private int max_num_buildings_for_progressive_upgrades_cost;

		private float progressive_building_add_cost_ssum;

		private float progressive_upgrades_add_cost_ssum;

		public float add_cost_per_missing_good_perc;

		public List<Bonuses> bonuses;

		public bool has_conditional_bonuses;

		public int skills_min;

		public int skills_max;

		public List<Skill.Def> skills_pool;

		public float siege_defense;

		public int min_distance_between = -1;

		public float refund_percentage = 50f;

		public string on_activate;

		public string on_deactivate;

		public bool buildable;

		public int always_available = -1;

		public int max_instances = 1;

		public DT.Field ai_eval_field;

		public KingdomAI.Expense.Category ai_category;

		public DT.Field ai_urgent_field;

		public string battleview_type;

		public int slots_base = 5;

		public int slots_per_tier = 1;

		public DT.Field slots_expand_cost;

		public bool hide_upgrades_if_completed;

		private static Resource tmp_upkeep = new Resource();

		private static TokenParser token_parser = new TokenParser("");

		private int validated;

		public District.Def district
		{
			get
			{
				if (districts != null && districts.Count == 1)
				{
					return districts[0];
				}
				return null;
			}
		}

		public Dictionary<string, List<ConditionalProduction.Def>> productions => GetBonuses()?.productions;

		public Dictionary<string, PerSettlementModidfiers> per_settlement_modifiers => GetBonuses()?.stat_mods;

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"id" => "#" + base.id, 
				"id_key" => base.id, 
				"is_upgrade" => IsUpgrade(), 
				"upgrade_of" => new Value(GetUpgradeOf()), 
				"districts" => new Value(districts), 
				"districts_text" => DistrictsText(), 
				"district" => district, 
				"upgrades" => upgrades, 
				"variants" => new Value(variants), 
				"variant_of" => variant_of, 
				"requires" => new Value(requires), 
				"requires_or" => new Value(requires_or), 
				"produces" => new Value(produces), 
				"produces_completed" => new Value(produces_completed), 
				"bonuses" => new Value(bonuses), 
				"buildable" => buildable, 
				"always_available" => IsAlwaysAvailable(), 
				"base_cost" => cost, 
				"cost" => GetCost(vars), 
				"upkeep" => upkeep, 
				"alt_tooltips" => alt_tooltips, 
				_ => base.GetVar(key, vars, as_value), 
			};
		}

		public string DistrictsText()
		{
			if (districts == null)
			{
				return "null";
			}
			string text = "";
			if (districts.Count != 1)
			{
				text += $"{districts.Count}: ";
			}
			for (int i = 0; i < districts.Count; i++)
			{
				District.Def def = districts[i];
				if (i > 0)
				{
					text += ", ";
				}
				text += def.id;
			}
			return text;
		}

		public float CalcAddPercFromMissingGoods(Kingdom k)
		{
			if (k == null)
			{
				return 0f;
			}
			if (!SOFT_RESOURCE_REQUIREMENTS)
			{
				return 0f;
			}
			float num = 0f;
			if (requires != null)
			{
				for (int i = 0; i < requires.Count; i++)
				{
					RequirementInfo requirementInfo = requires[i];
					if (!(requirementInfo.type != "Resource") && k.GetRealmTag(requirementInfo.key) < requirementInfo.amount)
					{
						num += add_cost_per_missing_good_perc;
					}
				}
			}
			return num;
		}

		public Resource GetCost(IVars vars)
		{
			if (cost == null)
			{
				return null;
			}
			Kingdom kingdom = vars.GetVar("kingdom").Get<Kingdom>();
			Realm realm = vars.GetVar("realm").Get<Realm>();
			if (realm == null)
			{
				realm = vars.GetVar("castle").Get<Castle>()?.GetRealm();
			}
			return GetCost(realm, kingdom);
		}

		public Resource GetCost(Realm realm, Kingdom kingdom = null)
		{
			if (cost == null)
			{
				return null;
			}
			if (kingdom == null)
			{
				kingdom = realm?.GetKingdom();
				if (kingdom == null)
				{
					return cost;
				}
			}
			float num = CalcProgressiveGoldCost(kingdom);
			float num2 = 0f;
			num2 = (IsUpgrade() ? kingdom.GetStat(Stats.ks_upgrade_cost_discount_perc) : (realm?.GetStat(Stats.rs_build_cost_discount_perc) ?? kingdom.GetStat(Stats.ks_build_cost_discount_perc)));
			num2 -= CalcAddPercFromMissingGoods(kingdom);
			if (num2 == 0f && num == 0f)
			{
				return cost;
			}
			Resource resource = new Resource(cost);
			resource.Add(ResourceType.Gold, num);
			Discount(resource, ResourceType.Gold, num2);
			float val = resource.Get(ResourceType.Gold);
			val = kingdom.game.economy.def.RoundCost(val);
			resource.Set(ResourceType.Gold, val);
			return resource;
		}

		public float CalcProgressiveGoldCost(Kingdom kingdom)
		{
			if (IsUpgrade())
			{
				if (progressive_gold_cost_for_upgrades <= 0f)
				{
					return 0f;
				}
				Def def = variant_of ?? this;
				if (def.districts == null)
				{
					return 0f;
				}
				int num = 0;
				for (int i = 0; i < def.districts.Count; i++)
				{
					Def def2 = def.districts[i]?.GetParent();
					if (def2 != null)
					{
						int realmTag = kingdom.GetRealmTag(def2.id);
						realmTag += kingdom.GetBuildingsCurrentlyBeingBuilt(def2);
						num += realmTag;
					}
				}
				if (num <= 1)
				{
					return 0f;
				}
				if (max_num_buildings_for_progressive_upgrades_cost > 0 && num > max_num_buildings_for_progressive_upgrades_cost)
				{
					num = max_num_buildings_for_progressive_upgrades_cost;
				}
				float num2 = cost[ResourceType.Gold] * (float)(num - 1) * progressive_gold_cost_for_upgrades * 0.01f;
				if (progressive_upgrades_add_cost_ssum > 0f)
				{
					float sSum = kingdom.GetSSum(progressive_upgrades_add_cost_ssum);
					if (num2 > sSum)
					{
						num2 = sSum;
					}
				}
				return (float)Math.Floor(num2);
			}
			if (progressive_gold_cost_for_building <= 0f)
			{
				return 0f;
			}
			if (upgrades?.buildings == null)
			{
				return 0f;
			}
			float num3 = 1f + kingdom.GetStat(Stats.ks_upgrade_cost_discount_perc) * 0.01f;
			float num4 = 0f;
			for (int j = 0; j < upgrades.buildings.Count; j++)
			{
				Def def3 = upgrades.buildings[j].def;
				if (kingdom.HasBuildingUpgrade(def3) || kingdom.IsUpgrading(def3))
				{
					float num5 = def3.cost[ResourceType.Gold];
					num5 *= num3;
					num4 += num5;
				}
			}
			if (num4 <= 0f)
			{
				return 0f;
			}
			float num6 = num4 * progressive_gold_cost_for_building * 0.01f;
			if (progressive_building_add_cost_ssum > 0f)
			{
				float sSum2 = kingdom.GetSSum(progressive_building_add_cost_ssum);
				if (num6 > sSum2)
				{
					num6 = sSum2;
				}
			}
			return (float)Math.Floor(num6);
		}

		public float CalcCommerceUpkeep(Kingdom k)
		{
			if (!SOFT_RESOURCE_REQUIREMENTS)
			{
				return 0f;
			}
			if (k == null)
			{
				return 0f;
			}
			if (requires == null)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < requires.Count; i++)
			{
				RequirementInfo requirementInfo = requires[i];
				if (!(requirementInfo.type != "Resource") && k.GetRealmTag(requirementInfo.key) < requirementInfo.amount)
				{
					Resource.Def def = Logic.Def.Get<Resource.Def>(requirementInfo.def);
					if (def != null)
					{
						num += def.building_commerce_upkeep;
					}
				}
			}
			return num;
		}

		public Resource CalcUpkeep(Kingdom k)
		{
			if (k == null)
			{
				return null;
			}
			float num = CalcCommerceUpkeep(k);
			if (num == 0f && upkeep == 0f)
			{
				return null;
			}
			tmp_upkeep.Set(ResourceType.Gold, upkeep);
			tmp_upkeep.Set(ResourceType.Trade, num);
			return tmp_upkeep;
		}

		public Resource GetBuildRefunds()
		{
			return GetBuildRefunds(null);
		}

		public Resource GetBuildRefunds(Realm realm, Kingdom kingdom = null)
		{
			Resource resource = new Resource(GetCost(realm, kingdom));
			resource.Mul(refund_percentage / 100f);
			resource[ResourceType.Hammers] = 0f;
			return resource;
		}

		public bool IsCommonBuilding()
		{
			if (districts != null && districts.Count == 1)
			{
				return districts[0].IsCommon();
			}
			return false;
		}

		public bool IsAlwaysAvailable()
		{
			if (always_available >= 0)
			{
				return always_available != 0;
			}
			if (requires != null || requires_or != null)
			{
				always_available = 0;
				return false;
			}
			always_available = 1;
			if (districts == null)
			{
				return true;
			}
			for (int i = 0; i < districts.Count; i++)
			{
				District.Def def = districts[i];
				if (!def.IsAlwaysAvailable())
				{
					always_available = 0;
					return false;
				}
				List<Def> prerequisites = GetPrerequisites(def);
				if (prerequisites != null)
				{
					for (int j = 0; j < prerequisites.Count; j++)
					{
						if (!prerequisites[j].IsAlwaysAvailable())
						{
							always_available = 0;
							return false;
						}
					}
				}
				List<Def> prerequisitesOr = GetPrerequisitesOr(def);
				if (prerequisitesOr == null)
				{
					continue;
				}
				bool flag = false;
				for (int k = 0; k < prerequisitesOr.Count; k++)
				{
					if (prerequisitesOr[k].IsAlwaysAvailable())
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					always_available = 0;
					return false;
				}
			}
			return true;
		}

		public bool IsUpgrade()
		{
			if (districts == null)
			{
				return false;
			}
			for (int i = 0; i < districts.Count; i++)
			{
				if (districts[i].IsUpgrades())
				{
					return true;
				}
			}
			return false;
		}

		public List<Def> GetUpgradeOf()
		{
			if (districts == null)
			{
				return null;
			}
			List<Def> list = new List<Def>();
			for (int i = 0; i < districts.Count; i++)
			{
				Def parent = districts[i].GetParent();
				if (parent != null)
				{
					list.Add(parent);
				}
			}
			return list;
		}

		public Def GetFirstUpgradeOf()
		{
			if (districts != null)
			{
				for (int i = 0; i < districts.Count; i++)
				{
					Def parent = districts[i].GetParent();
					if (parent != null)
					{
						return parent;
					}
				}
			}
			if (variants != null)
			{
				for (int j = 0; j < variants.Count; j++)
				{
					Def firstUpgradeOf = variants[j].GetFirstUpgradeOf();
					if (firstUpgradeOf != null)
					{
						return firstUpgradeOf;
					}
				}
			}
			return null;
		}

		public bool HasLevels()
		{
			if (bonuses == null || bonuses.Count <= 0)
			{
				return false;
			}
			if (bonuses.Count == 1 && bonuses[0].min_level == 1)
			{
				return false;
			}
			return true;
		}

		public Bonuses GetBonuses()
		{
			if (this.bonuses == null || this.bonuses.Count < 1)
			{
				return null;
			}
			Bonuses bonuses = this.bonuses[0];
			if (bonuses.min_level != 1)
			{
				return null;
			}
			return bonuses;
		}

		public int CalcLevel(Castle castle)
		{
			if (castle == null)
			{
				return 0;
			}
			Building b = castle.FindBuilding(this);
			return CalcLevel(b);
		}

		public int CalcLevel(Building b)
		{
			if (b == null)
			{
				return 0;
			}
			if (!b.IsWorking(use_temp_state: false))
			{
				return 0;
			}
			if (!b.IsFullyFunctional())
			{
				return 1;
			}
			if (!b.CalcCompleted())
			{
				return 2;
			}
			return 3;
		}

		public List<Def> GetPrerequisites(District.Def district)
		{
			if (district == null && districts != null && districts.Count == 1)
			{
				district = districts[0];
			}
			return district?.FindBuilding(this)?.prerequisites;
		}

		public List<Def> GetPrerequisitesOr(District.Def district)
		{
			if (district == null && districts != null && districts.Count == 1)
			{
				district = districts[0];
			}
			return district?.FindBuilding(this)?.prerequisites_or;
		}

		public static bool ResolveRequirement(RequirementInfo req, DT dt)
		{
			return ResolveRequirement(req.key, dt, out req.type, out req.def);
		}

		public static bool ResolveRequirement(string req_key, DT dt, out string req_type, out DT.Field req_def)
		{
			req_type = null;
			req_def = null;
			if (string.IsNullOrEmpty(req_key))
			{
				return false;
			}
			if (req_key == "Citadel")
			{
				req_type = "Citadel";
				return true;
			}
			req_def = dt.Find(req_key);
			if (req_def == null)
			{
				return false;
			}
			if (req_key == "Coastal")
			{
				req_type = "ProvinceFeature";
				return true;
			}
			DT.Field field = req_def.BaseRoot();
			if (field == null || field.def == null)
			{
				return false;
			}
			req_type = field.key;
			return true;
		}

		public bool HasPFRequirement(bool count_settlements)
		{
			if (HasPFRequirement(requires, count_settlements))
			{
				return true;
			}
			if (HasPFRequirement(requires_or, count_settlements))
			{
				return true;
			}
			return false;
		}

		private static bool HasPFRequirement(List<RequirementInfo> reqs, bool count_settlements)
		{
			if (reqs == null)
			{
				return false;
			}
			for (int i = 0; i < reqs.Count; i++)
			{
				RequirementInfo requirementInfo = reqs[i];
				if (requirementInfo.type == "ProvinceFeature")
				{
					return true;
				}
				if (count_settlements && requirementInfo.type == "Settlement")
				{
					return true;
				}
			}
			return false;
		}

		public bool HasReligionRequirement()
		{
			if (HasReligionRequirement(requires))
			{
				return true;
			}
			if (HasReligionRequirement(requires_or))
			{
				return true;
			}
			return false;
		}

		private static bool HasReligionRequirement(List<RequirementInfo> reqs)
		{
			if (reqs == null)
			{
				return false;
			}
			for (int i = 0; i < reqs.Count; i++)
			{
				if (reqs[i].type == "Religion")
				{
					return true;
				}
			}
			return false;
		}

		public bool CheckReligionRequirements(Kingdom k)
		{
			if (k == null)
			{
				return true;
			}
			if (k.religion == null)
			{
				return false;
			}
			if (requires != null)
			{
				for (int i = 0; i < requires.Count; i++)
				{
					RequirementInfo requirementInfo = requires[i];
					if (!(requirementInfo.type != "Religion") && !k.religion.HasTag(requirementInfo.key))
					{
						return false;
					}
				}
			}
			if (requires_or != null)
			{
				for (int j = 0; j < requires_or.Count; j++)
				{
					RequirementInfo requirementInfo2 = requires_or[j];
					if (!(requirementInfo2.type != "Religion") && k.religion.HasTag(requirementInfo2.key))
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}

		public static int FindReqIdx(List<RequirementInfo> lst, string key)
		{
			if (lst == null)
			{
				return -1;
			}
			for (int i = 0; i < lst.Count; i++)
			{
				if (lst[i].key == key)
				{
					return i;
				}
			}
			return -1;
		}

		public static void AddRequirements(ref List<RequirementInfo> lst, List<RequirementInfo> add, params string[] types)
		{
			if (add == null)
			{
				return;
			}
			for (int i = 0; i < add.Count; i++)
			{
				RequirementInfo requirementInfo = add[i];
				if (types.Length != 0 && Array.IndexOf(types, requirementInfo.type) < 0)
				{
					continue;
				}
				int num = FindReqIdx(lst, requirementInfo.key);
				if (num >= 0)
				{
					if (lst[num].amount < requirementInfo.amount)
					{
						lst[num] = requirementInfo;
					}
					continue;
				}
				if (lst == null)
				{
					lst = new List<RequirementInfo>();
				}
				lst.Add(requirementInfo);
			}
		}

		public List<RequirementInfo> GetIndirectResourceRequirements()
		{
			List<RequirementInfo> lst = null;
			AddRequirements(ref lst, requires, "Resource");
			return lst;
		}

		public int CalcRequiresPrerequisite(string def_id, Castle castle, bool hard_requirements_only = false)
		{
			if (districts == null)
			{
				return 0;
			}
			for (int i = 0; i < districts.Count; i++)
			{
				District.Def def = districts[i];
				int num = CalcRequires(def_id, castle, GetPrerequisites(def), hard_requirements_only);
				if (num != 0)
				{
					return num;
				}
				num = CalcRequires(def_id, castle, GetPrerequisitesOr(def), hard_requirements_only);
				if (num != 0)
				{
					return num;
				}
			}
			return 0;
		}

		public int CalcRequires(string def_id, Castle castle, bool hard_requirements_only = false)
		{
			if (FindReqIdx(requires, def_id) >= 0)
			{
				return 1;
			}
			if (FindReqIdx(requires_or, def_id) >= 0)
			{
				return 1;
			}
			int num = CalcRequiresPrerequisite(def_id, castle, hard_requirements_only);
			if (num != 0)
			{
				return num;
			}
			return 0;
		}

		private int CalcRequires(string def_id, Castle castle, List<Def> preqs, bool hard_requirements_only)
		{
			if (preqs == null)
			{
				return 0;
			}
			for (int i = 0; i < preqs.Count; i++)
			{
				Def def = preqs[i];
				if (def.id == def_id)
				{
					return 1;
				}
				int num = def.CalcRequires(def_id, castle, hard_requirements_only);
				if (num != 0)
				{
					return num + 1;
				}
			}
			return 0;
		}

		public Resource CalcUpgradeTotalProduction(Resource res, Kingdom kingdom, int level = 1, bool check_condition = true, float immediate_mul = 1f, float near_future_mul = 0f, float far_future_mul = 0f, int add_goods_produced = 0)
		{
			if (bonuses == null)
			{
				return res;
			}
			if (kingdom == null)
			{
				return res;
			}
			if (!IsUpgrade())
			{
				return res;
			}
			if (variant_of != null)
			{
				return variant_of.CalcUpgradeTotalProduction(res, kingdom, level, check_condition, immediate_mul, near_future_mul, far_future_mul);
			}
			if (districts == null || districts.Count == 0)
			{
				return res;
			}
			if (res == null)
			{
				res = new Resource();
			}
			Resource resource = new Resource();
			float num = 1f;
			float taxMul = kingdom.GetTaxMul();
			for (int i = 0; i < kingdom.realms.Count; i++)
			{
				Realm realm = kingdom.realms[i];
				if (realm?.castle == null)
				{
					continue;
				}
				float num2 = 0f;
				for (int j = 0; j < districts.Count; j++)
				{
					Def def = districts[j]?.GetParent();
					if (def != null)
					{
						if (realm.castle.HasWorkingBuilding(def))
						{
							num2 += immediate_mul;
						}
						else if (near_future_mul != 0f && realm.castle.CheckBuildingRequirements(def))
						{
							num2 += near_future_mul;
						}
						else if (far_future_mul != 0f && realm.castle.MayBuildBuilding(def))
						{
							num2 += far_future_mul;
						}
					}
				}
				if (!(num2 <= 0f))
				{
					resource.Clear();
					CalcProduction(resource, realm.castle, level, check_condition, immediate_mul, near_future_mul, far_future_mul);
					num2 = ((realm.castle.governor != null) ? (num2 * num) : (num2 * taxMul));
					res.Add(resource, num2);
				}
			}
			CalcProducedGoods(res, kingdom, add_goods_produced, immediate_mul);
			return res;
		}

		public Resource CalcProduction(Resource res, Castle castle, int level = 1, bool check_condition = true, float immediate_mul = 1f, float near_future_mul = 0f, float far_future_mul = 0f, int add_goods_produced = 0)
		{
			if (this.bonuses == null)
			{
				return res;
			}
			if (immediate_mul == 0f && near_future_mul == 0f && far_future_mul == 0f)
			{
				return res;
			}
			Kingdom kingdom = castle?.GetKingdom();
			if (kingdom == null)
			{
				return res;
			}
			if (!CheckReligionRequirements(kingdom))
			{
				return res;
			}
			if (res == null)
			{
				res = new Resource();
			}
			bool flag = true;
			bool flag2 = true;
			if (upgrades?.buildings != null)
			{
				int num = add_goods_produced;
				if (num >= 2 && kingdom.GetRealmTag(base.id) switch
				{
					0 => 1, 
					1 => castle.HasBuilding(this) ? 1 : 0, 
					_ => 0, 
				} == 0)
				{
					num = 1;
				}
				for (int i = 0; i < upgrades.buildings.Count; i++)
				{
					District.Def.BuildingInfo buildingInfo = upgrades.buildings[i];
					int num2 = num;
					float immediate_mul2;
					if (kingdom.HasBuildingUpgrade(buildingInfo.def))
					{
						immediate_mul2 = immediate_mul;
					}
					else
					{
						if (level < 3)
						{
							continue;
						}
						flag = false;
						if (near_future_mul != far_future_mul)
						{
							if (!castle.CheckBuildingRequirements(buildingInfo.def))
							{
								immediate_mul2 = far_future_mul;
								flag2 = false;
							}
							else
							{
								immediate_mul2 = near_future_mul;
								if (num2 == 2)
								{
									num2 = 3;
								}
							}
						}
						else
						{
							immediate_mul2 = near_future_mul;
						}
					}
					buildingInfo.def?.CalcProduction(res, castle, level, check_condition, immediate_mul2, 0f, 0f, num2);
				}
			}
			for (int j = 0; j < this.bonuses.Count; j++)
			{
				Bonuses bonuses = this.bonuses[j];
				if (bonuses.min_level > level || bonuses.productions == null)
				{
					continue;
				}
				float mul = ((bonuses.min_level < 3 || flag) ? immediate_mul : ((!flag2) ? far_future_mul : near_future_mul));
				foreach (KeyValuePair<string, List<ConditionalProduction.Def>> production in bonuses.productions)
				{
					_ = production.Key;
					List<ConditionalProduction.Def> value = production.Value;
					for (int k = 0; k < value.Count; k++)
					{
						value[k].AddValue(res, castle, level, per_settlement: false, check_condition, mul);
					}
				}
			}
			CalcProducedGoods(res, kingdom, add_goods_produced, immediate_mul);
			return res;
		}

		public Resource CalcProducedGoods(Resource res, Kingdom k, int add_goods_produced, float mul)
		{
			if (add_goods_produced <= 0)
			{
				return res;
			}
			if (mul == 0f)
			{
				return res;
			}
			if (produces == null)
			{
				return res;
			}
			if (res == null)
			{
				res = new Resource();
			}
			for (int i = 0; i < produces.Count; i++)
			{
				ProducedResource producedResource = produces[i];
				if (producedResource.type == "tag")
				{
					continue;
				}
				if (k != null && add_goods_produced >= 2 && k.GetProducedGoodDef(producedResource.resource) == null)
				{
					float amount = ((add_goods_produced >= 3) ? mul : 1f);
					Resource.Def def = k.game.defs.Get<Resource.Def>(producedResource.resource);
					if (def.ai_eval_field != null)
					{
						amount = def.ai_eval_field.Float(k, 1f);
					}
					res.Add(ResourceType.Rebels, amount);
				}
				else
				{
					res.Add(ResourceType.RebelsSlots, mul);
				}
			}
			return res;
		}

		public Bonuses CalcCurrentBonuses(Kingdom k)
		{
			Bonuses bonuses = GetBonuses();
			if (k == null)
			{
				return bonuses;
			}
			if (upgrades?.buildings == null)
			{
				return bonuses;
			}
			Bonuses bonuses2 = null;
			for (int i = 0; i < upgrades.buildings.Count; i++)
			{
				District.Def.BuildingInfo buildingInfo = upgrades.buildings[i];
				if (!k.HasBuildingUpgrade(buildingInfo.def))
				{
					continue;
				}
				Bonuses bonuses3 = buildingInfo.def.GetBonuses();
				if (bonuses3 != null)
				{
					if (bonuses2 == null)
					{
						bonuses2 = new Bonuses();
						bonuses2.building_def = bonuses.building_def;
						bonuses2.location = bonuses.location;
						bonuses2.flat = bonuses.flat;
						bonuses2.field = bonuses.field;
						bonuses2.min_level = bonuses.min_level;
						bonuses2.at_level = bonuses.at_level;
						bonuses2.AddBonuses(bonuses);
					}
					bonuses2.AddBonuses(bonuses3);
				}
			}
			if (bonuses2 == null)
			{
				return bonuses;
			}
			return bonuses2;
		}

		public int FindProducesIndex(string resource)
		{
			if (produces == null)
			{
				return -1;
			}
			for (int i = 0; i < produces.Count; i++)
			{
				if (produces[i].resource == resource)
				{
					return i;
				}
			}
			return -1;
		}

		public int FindProducesCompletedIndex(string resource)
		{
			if (produces_completed == null)
			{
				return -1;
			}
			for (int i = 0; i < produces_completed.Count; i++)
			{
				if (produces_completed[i].resource == resource)
				{
					return i;
				}
			}
			return -1;
		}

		public bool Produces(string resource)
		{
			return FindProducesIndex(resource) >= 0;
		}

		public bool ProducesCompleted(string resource)
		{
			return FindProducesCompletedIndex(resource) >= 0;
		}

		public bool ProducesResources()
		{
			if (produces != null)
			{
				for (int i = 0; i < produces.Count; i++)
				{
					if (produces[i].type != "tag")
					{
						return true;
					}
				}
			}
			if (produces_completed != null)
			{
				for (int j = 0; j < produces_completed.Count; j++)
				{
					if (produces_completed[j].type != "tag")
					{
						return true;
					}
				}
			}
			return false;
		}

		public int GetProducesAmount(string resource)
		{
			if (produces == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < produces.Count; i++)
			{
				if (produces[i].resource == resource)
				{
					num++;
				}
			}
			return num;
		}

		public int GetProducesCompletedAmount(string resource)
		{
			if (produces_completed == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < produces_completed.Count; i++)
			{
				if (produces_completed[i].resource == resource)
				{
					num++;
				}
			}
			return num;
		}

		public string GetRelation(Game game, string def_id, Castle castle)
		{
			if (game == null || def_id == null)
			{
				return null;
			}
			if (def_id == base.id)
			{
				return "this";
			}
			int num = CalcRequires(def_id, castle);
			if (num == 1)
			{
				return "requires";
			}
			if (num > 1)
			{
				return "requires_indirect";
			}
			Def def = game.defs.Find<Def>(def_id);
			if (def == null)
			{
				return null;
			}
			num = def.CalcRequires(base.id, castle);
			if (num == 1)
			{
				return "enables";
			}
			if (num > 1)
			{
				return "enables_indirect";
			}
			return null;
		}

		private void Discount(Resource cost, ResourceType rt, float bonus)
		{
			if (bonus != 0f)
			{
				float num = cost[rt];
				if (!(num <= 0f))
				{
					float num2 = num * (100f - bonus) / 100f;
					num2 = (float)Math.Ceiling(num2);
					cost[rt] = num2;
				}
			}
		}

		public override void Unload(Game game)
		{
			base.Unload(game);
			districts = null;
			upgrades = null;
			variants = null;
			variant_of = null;
		}

		public override bool Load(Game game)
		{
			has_conditional_bonuses = false;
			validated = 0;
			always_available = -1;
			DT.Field field = base.field;
			if (field == null)
			{
				return false;
			}
			if (IsBase())
			{
				SOFT_RESOURCE_REQUIREMENTS = field.GetBool("soft_resource_requirements", null, SOFT_RESOURCE_REQUIREMENTS);
				alt_tooltips = field.GetBool("alt_tooltips", null, alt_tooltips);
			}
			LoadCSV();
			LoadVariantOf(game);
			LoadUpgrades(game);
			requires = LoadRequirements(game, field.FindChild("requires"));
			requires_or = LoadRequirements(game, field.FindChild("requires_or"));
			max_instances = field.GetInt("max_instances", null, max_instances);
			cost = Resource.Parse(field.FindChild("cost"), this);
			if (cost != null)
			{
				cost[ResourceType.Gold] *= game.GetDevSettingsFloat("building_cost_gold_mod", 1f);
				cost[ResourceType.Hammers] *= game.GetDevSettingsFloat("building_cost_hammers_mod", 1f);
			}
			progressive_gold_cost_for_building = field.GetFloat("progressive_gold_cost_for_building", null, progressive_gold_cost_for_building);
			progressive_gold_cost_for_upgrades = field.GetFloat("progressive_gold_cost_for_upgrades", null, progressive_gold_cost_for_upgrades);
			progressive_building_add_cost_ssum = field.GetFloat("progressive_building_add_cost_ssum", null, progressive_building_add_cost_ssum);
			progressive_upgrades_add_cost_ssum = field.GetFloat("progressive_upgrades_add_cost_ssum", null, progressive_upgrades_add_cost_ssum);
			add_cost_per_missing_good_perc = field.GetFloat("add_cost_per_missing_good_perc", null, add_cost_per_missing_good_perc);
			max_num_buildings_for_progressive_upgrades_cost = field.GetInt("max_num_buildings_for_progressive_upgrades_cost", null, max_num_buildings_for_progressive_upgrades_cost);
			upkeep = field.GetFloat("GoldUpkeep");
			piety_type = field.GetString("piety_type", null, null);
			LoadSkills(game, field);
			LoadProduces(game, field, ref produces);
			LoadProduces(game, field.FindChild("completion_bonuses"), ref produces_completed);
			LoadBonuses(game, field);
			LoadRegPopInfl(game, field);
			if (IsBase())
			{
				min_distance_between = field.GetInt("min_distance_between");
			}
			siege_defense = field.GetFloat("siege_defense");
			buildable = field.GetBool("buildable", null, def_val: true);
			refund_percentage = field.GetFloat("refund_percentage", null, refund_percentage);
			on_activate = field.GetString("on_activate", null, on_activate);
			on_deactivate = field.GetString("on_deactivate", null, on_deactivate);
			ai_eval_field = field.FindChild("ai_eval");
			Enum.TryParse<KingdomAI.Expense.Category>(field.GetString("ai_category", null, "Economy"), out ai_category);
			if (ai_category == KingdomAI.Expense.Category.None)
			{
				ai_category = KingdomAI.Expense.Category.Economy;
			}
			ai_urgent_field = field.FindChild("ai_urgent");
			battleview_type = field.GetString("battleview_type", null, battleview_type);
			slots_base = field.GetInt("slots_base", null, slots_base);
			hide_upgrades_if_completed = field.GetBool("hide_upgrades_if_completed", null, hide_upgrades_if_completed);
			slots_expand_cost = field.FindChild("slots_expand_cost");
			return true;
		}

		private void LoadCSV()
		{
			using (Game.Profile("Building.Def.LoadCSV"))
			{
				LoadCSVBonuses("Bonuses");
				LoadCSVBonuses("CompletionBonuses", "completion_bonuses");
				LoadCSVRequires();
			}
		}

		private void LoadCSVBonuses(string csv_key, string def_key = null)
		{
			string text = base.field.GetString(csv_key);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			DT.Field field = base.field;
			if (def_key != null)
			{
				field = field.CreateChild(def_key);
			}
			token_parser.Init(text);
			if (token_parser.cur_token.type <= TokenParser.Token.Type.Comment)
			{
				token_parser.MoveNext();
			}
			while (!token_parser.AtEOF())
			{
				if (!LoadCSVBonus(field))
				{
					Game.Log(field.Path(include_file: true) + ": Invalid bonus: " + token_parser.CurTokenLineText, Game.LogType.Error);
					break;
				}
			}
		}

		private bool LoadCSVBonus(DT.Field field)
		{
			string curTokenText;
			if (token_parser.Match(token_parser.cur_token, "produces"))
			{
				token_parser.MoveNext();
				if (token_parser.cur_token.type != TokenParser.Token.Type.Identifier)
				{
					return false;
				}
				curTokenText = token_parser.CurTokenText;
				token_parser.MoveNext();
				string type = "";
				if (curTokenText == "tag")
				{
					type = curTokenText;
					if (token_parser.cur_token.type != TokenParser.Token.Type.Identifier)
					{
						return false;
					}
					curTokenText = token_parser.CurTokenText;
					token_parser.MoveNext();
				}
				field.CreateChild("produces").CreateChild(curTokenText).type = type;
				return true;
			}
			if (token_parser.cur_token.type != TokenParser.Token.Type.Number)
			{
				return false;
			}
			string curTokenText2 = token_parser.CurTokenText;
			token_parser.MoveNext();
			if (token_parser.cur_token.type != TokenParser.Token.Type.Identifier)
			{
				return false;
			}
			curTokenText = token_parser.CurTokenText;
			token_parser.MoveNext();
			string text = null;
			if (token_parser.Match(token_parser.cur_token, "in"))
			{
				token_parser.MoveNext();
				if (token_parser.cur_token.type != TokenParser.Token.Type.Identifier)
				{
					return false;
				}
				text = token_parser.CurTokenText;
				token_parser.MoveNext();
			}
			string condition = null;
			if (token_parser.Match(token_parser.cur_token, "if"))
			{
				token_parser.MoveNext();
				if (token_parser.cur_token.type != TokenParser.Token.Type.Identifier)
				{
					return false;
				}
				condition = token_parser.CurTokenText;
				token_parser.MoveNext();
			}
			if (curTokenText.StartsWith("rs_", StringComparison.Ordinal))
			{
				CreateStatModField(field, "mod", curTokenText, curTokenText2, text, condition);
				return true;
			}
			if (curTokenText.StartsWith("ks_", StringComparison.Ordinal))
			{
				CreateStatModField(field, "kingdom_mod", curTokenText, curTokenText2, text, condition);
				return true;
			}
			if (text == null)
			{
				return false;
			}
			CreateProductionField(field, curTokenText, curTokenText2, text, condition);
			return true;
		}

		private void CreateStatModField(DT.Field field, string mod_type, string key, string val, string location, string condition)
		{
			field = field.CreateChild("stat_modifiers");
			if (condition != null)
			{
				field = field.CreateChild(condition);
				field.type = "if";
			}
			if (location != null)
			{
				field = field.CreateChild(location);
			}
			field = field.CreateChild(key);
			field.type = mod_type;
			field.value_str = val;
			if (DT.ParseFloat(val, out var f))
			{
				field.value = f;
			}
		}

		private void CreateProductionField(DT.Field field, string key, string val, string location, string condition)
		{
			field = field.CreateChild("production");
			field = field.CreateChild(location);
			if (condition != null)
			{
				field = field.CreateChild(condition);
				field.type = "if";
			}
			field = field.CreateChild(key);
			field.value_str = val;
			if (DT.ParseFloat(val, out var f))
			{
				field.value = f;
			}
		}

		private void LoadCSVRequires()
		{
			string text = base.field.GetString("Requires");
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			DT.Field field = base.field.CreateChild("requires");
			token_parser.Init(text);
			while (!token_parser.AtEOF())
			{
				if (token_parser.cur_token.type == TokenParser.Token.Type.Identifier)
				{
					string curTokenText = token_parser.CurTokenText;
					field.CreateChild(curTokenText);
				}
				token_parser.MoveNext();
			}
		}

		private void LoadVariantOf(Game game)
		{
			variant_of = null;
			if (IsBase())
			{
				return;
			}
			DT.Field field = base.field?.based_on;
			if (field == null)
			{
				Game.Log(base.field?.Path(include_file: true) + ": Building def is not based on anything", Game.LogType.Error);
			}
			else
			{
				if (field.key == "Building" || field.key == "Upgrade")
				{
					return;
				}
				variant_of = Logic.Def.Get<Def>(field);
				if (variant_of == null)
				{
					Game.Log(base.field?.Path(include_file: true) + ": Could not resolve base '" + field.base_path + "' as building def", Game.LogType.Error);
					return;
				}
				if (variant_of.variants == null)
				{
					variant_of.variants = new List<Def>();
				}
				variant_of.variants.Add(this);
			}
		}

		private void LoadUpgrades(Game game)
		{
			upgrades = null;
			if (base.field.FindChild("upgrades.buildings") != null)
			{
				upgrades = game.defs.Get<District.Def>(base.id + ".upgrades");
			}
		}

		public static List<RequirementInfo> LoadRequirements(Game game, DT.Field f)
		{
			List<DT.Field> list = f?.Children();
			if (list == null)
			{
				return null;
			}
			int count = list.Count;
			if (count <= 0)
			{
				return null;
			}
			List<RequirementInfo> list2 = new List<RequirementInfo>(count);
			for (int i = 0; i < count; i++)
			{
				DT.Field field = list[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					RequirementInfo item = new RequirementInfo(field);
					list2.Add(item);
				}
			}
			return list2;
		}

		private void LoadSkills(Game game, DT.Field f)
		{
			skills_min = 0;
			skills_max = 0;
			skills_pool = null;
			f = f?.FindChild("skills");
			if (f == null)
			{
				return;
			}
			skills_min = f.Int(0, null, 1);
			skills_max = f.Int(1, null, skills_min);
			if (f.children == null)
			{
				return;
			}
			skills_pool = new List<Skill.Def>(f.children.Count);
			for (int i = 0; i < f.children.Count; i++)
			{
				DT.Field field = f.children[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					Skill.Def item = game.defs.Get<Skill.Def>(field.key);
					skills_pool.Add(item);
				}
			}
		}

		private void AddProduces(ref List<ProducedResource> produces, string type, string resource, ResourceInfo.Availability availability = ResourceInfo.Availability.Available)
		{
			if (produces == null)
			{
				produces = new List<ProducedResource>();
			}
			int num = FindProducesIndex(resource);
			if (num < 0)
			{
				produces.Add(new ProducedResource
				{
					type = type,
					resource = resource,
					availability = availability
				});
				return;
			}
			ProducedResource value = produces[num];
			if (availability < value.availability)
			{
				value.availability = availability;
				produces[num] = value;
			}
		}

		private void LoadProduces(Game game, DT.Field f, ref List<ProducedResource> produces)
		{
			produces = null;
			f = f?.FindChild("produces");
			if (f == null)
			{
				return;
			}
			int num = f.NumValues();
			for (int i = 0; i < num; i++)
			{
				string text = f.String(i);
				if (!string.IsNullOrEmpty(text))
				{
					AddProduces(ref produces, f.Type(), text);
				}
			}
			if (f.children == null)
			{
				return;
			}
			for (int j = 0; j < f.children.Count; j++)
			{
				DT.Field field = f.children[j];
				if (!string.IsNullOrEmpty(field.key))
				{
					AddProduces(ref produces, field.Type(), field.key);
				}
			}
		}

		private void LoadBonuses(Game game, DT.Field f)
		{
			this.bonuses = null;
			Bonuses bonuses = LoadBonuses(game, f, 1, 1);
			if (bonuses != null)
			{
				this.bonuses = new List<Bonuses>();
				this.bonuses.Add(bonuses);
			}
			Bonuses bonuses2 = LoadBonuses(game, f.FindChild("res_bonuses"), 2, 2);
			if (bonuses2 != null)
			{
				Game.Log(bonuses2.field.Path(include_file: true) + ": res_bonuses are no longer supported!", Game.LogType.Error);
			}
			Bonuses bonuses3 = LoadBonuses(game, f.FindChild("completion_bonuses"), 3, 3);
			if (bonuses3 != null)
			{
				Game.Log(bonuses3.field.Path(include_file: true) + ": completion_bonuses are no longer supported!", Game.LogType.Error);
			}
		}

		private Bonuses LoadBonuses(Game game, DT.Field f, int min_level, int at_level, string location = "realm", bool flat = true)
		{
			if (f == null)
			{
				return null;
			}
			Bonuses bonuses = new Bonuses();
			bonuses.building_def = this;
			bonuses.location = location;
			bonuses.flat = flat;
			bonuses.field = f;
			bonuses.min_level = min_level;
			bonuses.at_level = at_level;
			LoadProductions(game, f, bonuses);
			LoadMods(game, f, bonuses);
			if (flat)
			{
				DT.Field field = f.FindChild("per_level");
				if (field != null)
				{
					Game.Log(field.Path(include_file: true) + ": per_level bonuses are no longer supported!", Game.LogType.Error);
				}
			}
			if (flat && location == "realm")
			{
				DT.Field field2 = f.FindChild("region");
				if (field2 != null)
				{
					Game.Log(field2.Path(include_file: true) + ": region bonuses are no longer supported!", Game.LogType.Error);
				}
			}
			return bonuses;
		}

		private void LoadProductions(Game game, DT.Field f, Bonuses bonuses)
		{
			bonuses.productions = null;
			DT.Field field = f.FindChild("production");
			if (field != null)
			{
				List<string> list = field.Keys();
				for (int i = 0; i < list.Count; i++)
				{
					string path = list[i];
					DT.Field f2 = field.FindChild(path);
					AddProduction(f2, bonuses);
				}
			}
		}

		private void AddProduction(DT.Field f, Bonuses bonuses)
		{
			string key = f.key;
			string text = f.key;
			if (text != "All" && text != "CoastalSettlement" && text != "CoastalVillage" && text != "ReligiousSettlement" && text != "KingdomReligiousSettlement" && text != "MonasteryOrMosque")
			{
				text = Settlement.ParseType(f.dt, key);
			}
			if (text != null)
			{
				Parse(text, f, bonuses);
			}
			else
			{
				Game.Log(f.Path(include_file: true) + ": invalid settlement type '" + key + "'", Game.LogType.Error);
			}
			void Parse(string type, DT.Field field, Bonuses b)
			{
				Resource resource = Resource.Parse(field);
				if (resource != null)
				{
					AddProduction(field.line, type, resource, null, b);
				}
				List<string> list = field.Keys();
				for (int i = 0; i < list.Count; i++)
				{
					string path = list[i];
					DT.Field field2 = field.FindChild(path);
					if (!(field2.type != "if"))
					{
						Resource resource2 = Resource.Parse(field2, null, no_null: false, parse_value: false);
						if (resource2 != null)
						{
							AddProduction(field2.line, type, resource2, field2, b);
						}
					}
				}
			}
		}

		private void AddProduction(int line_number, string type, Resource production, DT.Field condition, Bonuses b)
		{
			if (condition != null)
			{
				DT.Field obj = base.field.BaseRoot().FindChild("conditions." + condition.key);
				if (obj == null)
				{
					Game.Log(condition.Path(include_file: true) + ": Unknown condition", Game.LogType.Error);
				}
				condition = obj;
			}
			if (condition != null)
			{
				has_conditional_bonuses = true;
			}
			b.AddProduction(line_number, type, production, condition);
		}

		private void LoadMods(Game game, DT.Field f, Bonuses bonuses)
		{
			bonuses.stat_mods = null;
			DT.Field field = f?.FindChild("stat_modifiers");
			if (field == null)
			{
				return;
			}
			LoadMods(game, field, bonuses, "Castle");
			List<string> list = field.Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string text = list[i];
				DT.Field field2 = field.FindChild(text);
				if (field2 != null && !(field2.type == "mod") && !(field2.type == "kingdom_mod") && !(field2.type == "if"))
				{
					LoadMods(game, field2, bonuses, text);
				}
			}
		}

		public int FindRegPopInfMod(Realm.RegionPopInfModifier.Type type, string key)
		{
			if (regPopInfMods == null)
			{
				return -1;
			}
			for (int i = 0; i < regPopInfMods.Count; i++)
			{
				Realm.RegionPopInfModifier regionPopInfModifier = regPopInfMods[i];
				if (regionPopInfModifier.type == type && regionPopInfModifier.key == key)
				{
					return i;
				}
			}
			return -1;
		}

		private void AddRegPopInfMod(Realm.RegionPopInfModifier.Type type, float strengthLand, float strengthThroughSea, string key = "")
		{
			if (regPopInfMods == null)
			{
				regPopInfMods = new List<Realm.RegionPopInfModifier>();
			}
			int num = FindRegPopInfMod(type, key);
			if (num < 0)
			{
				regPopInfMods.Add(new Realm.RegionPopInfModifier
				{
					type = type,
					neighborStrength = strengthLand,
					neighborThroughSeaStrength = strengthThroughSea,
					key = key
				});
			}
			else
			{
				Realm.RegionPopInfModifier value = regPopInfMods[num];
				value.neighborStrength += strengthLand;
				value.neighborThroughSeaStrength += strengthThroughSea;
				regPopInfMods[num] = value;
			}
		}

		private void LoadRegPopInfl(Game game, DT.Field f)
		{
			regPopInfMods = null;
			f = f.FindChild("region_pop_inf");
			if (f == null)
			{
				return;
			}
			if (f.NumValues() > 0)
			{
				float num = f.Value(0);
				AddRegPopInfMod(Realm.RegionPopInfModifier.Type.Raw, num, num);
			}
			DT.Field field = f.FindChild("per_resource_type");
			if (field?.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!string.IsNullOrEmpty(field2.key))
				{
					DT.Field field3 = field2.FindChild("Neighbor");
					float num2 = 0f;
					num2 = ((field3 == null) ? ((float)field2.Value()) : ((float)field3.Value()));
					DT.Field field4 = field2.FindChild("NeighborThroughSea");
					float num3 = 0f;
					num3 = ((field4 == null) ? num2 : ((float)field4.Value()));
					AddRegPopInfMod(Realm.RegionPopInfModifier.Type.Resource, num2, num3, field2.key);
				}
			}
		}

		private void LoadMods(Game game, DT.Field f, Bonuses bonuses, string settlement_type, DT.Field condition = null)
		{
			if (condition != null)
			{
				DT.Field obj = base.field.BaseRoot().FindChild("conditions." + condition.key);
				if (obj == null)
				{
					Game.Log(condition.Path(include_file: true) + ": Unknown condition", Game.LogType.Error);
				}
				condition = obj;
			}
			if (condition != null)
			{
				has_conditional_bonuses = true;
			}
			if (bonuses.stat_mods == null)
			{
				bonuses.stat_mods = new Dictionary<string, PerSettlementModidfiers>();
			}
			PerSettlementModidfiers value = null;
			if (!bonuses.stat_mods.TryGetValue(settlement_type, out value))
			{
				value = new PerSettlementModidfiers();
				bonuses.stat_mods.Add(settlement_type, value);
			}
			List<string> list = f.Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				DT.Field field = f.FindChild(path);
				if (field.type == "mod" || field.type == "kingdom_mod")
				{
					StatModifier.Def def = new StatModifier.Def();
					if (def.Load(game, this, field))
					{
						def.bonuses = bonuses;
						def.settlement_type = settlement_type;
						def.condition = condition;
						value.AddMod(def);
					}
				}
				else if (field.type == "if")
				{
					LoadMods(game, field, bonuses, settlement_type, field);
				}
			}
		}

		public override bool Validate(Game game)
		{
			if (validated > 0)
			{
				return true;
			}
			if (validated < 0)
			{
				Game.Log("Infinite loop during building def validation: " + base.id, Game.LogType.Error);
				return false;
			}
			if (base.field == null)
			{
				validated = 1;
				return false;
			}
			validated = -1;
			ValidateDistricts(game);
			ValidateRequirements(game, requires, (variant_of ?? this).id);
			ValidateRequirements(game, requires_or, (variant_of ?? this).id);
			ValidateMods(game);
			ValidateProduces(game);
			validated = 1;
			return true;
		}

		private void ValidateDistricts(Game game)
		{
			if (variants != null)
			{
				for (int i = 0; i < variants.Count; i++)
				{
					Def def = variants[i];
					if (def?.districts == null)
					{
						continue;
					}
					for (int j = 0; j < def.districts.Count; j++)
					{
						District.Def item = def.districts[j];
						if (districts == null)
						{
							districts = new List<District.Def>();
						}
						else if (districts.Contains(item))
						{
							continue;
						}
						districts.Add(item);
					}
				}
			}
			if (IsBase())
			{
				if (districts != null)
				{
					Game.Log(base.field?.Path(include_file: true) + ": Base building def has disricts: " + DistrictsText(), Game.LogType.Error);
				}
			}
			else if (districts == null || districts.Count == 0)
			{
				Game.Log(base.field?.Path(include_file: true) + ": Building has no districts, did you forget to add it to a district def?", Game.LogType.Warning);
			}
			else if (districts.Count > 1 && variants == null)
			{
				Game.Log(base.field?.Path(include_file: true) + ": Building has multiple districts (" + DistrictsText() + "), please use building variants instead", Game.LogType.Warning);
			}
		}

		public static void ValidateRequirements(Game game, List<RequirementInfo> requirements, string required_for_id)
		{
			if (requirements == null)
			{
				return;
			}
			for (int i = 0; i < requirements.Count; i++)
			{
				RequirementInfo requirementInfo = requirements[i];
				if (!ResolveRequirement(requirementInfo, game.dt))
				{
					Game.Log(requirementInfo.field.Path(include_file: true) + ": invalid requirement '" + requirementInfo.key + "'", Game.LogType.Warning);
					requirements.RemoveAt(i);
					i--;
				}
				else if (requirementInfo.type == "Resource" && !string.IsNullOrEmpty(required_for_id))
				{
					Resource.Def def = Logic.Def.Get<Resource.Def>(requirementInfo.def);
					if (def != null)
					{
						Container.AddUnique_Class(ref def.required_for, required_for_id);
					}
				}
			}
		}

		private void ValidateMods(Game game)
		{
			if (per_settlement_modifiers == null)
			{
				return;
			}
			foreach (KeyValuePair<string, PerSettlementModidfiers> per_settlement_modifier in per_settlement_modifiers)
			{
				PerSettlementModidfiers value = per_settlement_modifier.Value;
				if (value.realm_mods != null)
				{
					Stats.Def def = game.defs.Get<Stats.Def>("RealmStats");
					for (int i = 0; i < value.realm_mods.Count; i++)
					{
						StatModifier.Def def2 = value.realm_mods[i];
						if (!def.HasStat(def2.stat_name))
						{
							Game.Log(def2.field.Path(include_file: true) + ": Unknown Realm stat: " + base.id + "." + def2.stat_name, Game.LogType.Error);
						}
					}
				}
				if (value.kingdom_mods == null)
				{
					continue;
				}
				Stats.Def def3 = game.defs.Get<Stats.Def>("KingdomStats");
				for (int j = 0; j < value.kingdom_mods.Count; j++)
				{
					StatModifier.Def def4 = value.kingdom_mods[j];
					if (!def3.HasStat(def4.stat_name))
					{
						Game.Log(def4.field.Path(include_file: true) + ": Unknown Kingdom stat: " + base.id + "." + def4.stat_name, Game.LogType.Error);
					}
				}
			}
		}

		private void AddProducedRequirementsInResourceInfo(Resource.Def rdef)
		{
			if (requires == null)
			{
				return;
			}
			for (int i = 0; i < requires.Count; i++)
			{
				Logic.Def def = requires[i]?.def?.def?.def;
				Logic.Def def2 = def;
				if (def2 == null)
				{
					continue;
				}
				if (!(def2 is ProvinceFeature.Def def3))
				{
					if (!(def2 is Settlement.Def))
					{
						continue;
					}
					string[] array = (def as Settlement.Def)?.enable_features;
					if (array != null)
					{
						for (int j = 0; j < array.Length; j++)
						{
							Container.AddUnique_Class(ref rdef.province_features, array[j]);
						}
					}
				}
				else
				{
					ProvinceFeature.Def def5 = def3;
					Container.AddUnique_Class(ref rdef.province_features, def5.id);
				}
			}
		}

		private void AddProducedInResourceInfo(Resource.Def rdef)
		{
			Container.AddUnique_Class(ref rdef.produced_in, this);
			AddProducedRequirementsInResourceInfo(rdef);
			for (int i = 0; i < districts.Count; i++)
			{
				(districts[i]?.GetParent())?.AddProducedRequirementsInResourceInfo(rdef);
			}
		}

		private void ValidateProduces(Game game)
		{
			Def def = variant_of ?? this;
			if (produces != null)
			{
				for (int i = 0; i < produces.Count; i++)
				{
					ProducedResource producedResource = produces[i];
					if (!(producedResource.type == "tag"))
					{
						Resource.Def def2 = game.defs.Find<Resource.Def>(producedResource.resource);
						if (def2 == null)
						{
							Game.Log("Invalid resource '" + producedResource.resource + "' produced by building '" + base.id + "'", Game.LogType.Error);
						}
						else
						{
							def.AddProducedInResourceInfo(def2);
						}
					}
				}
			}
			if (produces_completed == null)
			{
				return;
			}
			for (int j = 0; j < produces_completed.Count; j++)
			{
				ProducedResource producedResource2 = produces_completed[j];
				if (!(producedResource2.type == "tag"))
				{
					Resource.Def def3 = game.defs.Find<Resource.Def>(producedResource2.resource);
					if (def3 == null)
					{
						Game.Log("Invalid resource '" + producedResource2.resource + "' produced by building '" + base.id + "'", Game.LogType.Error);
					}
					else
					{
						def.AddProducedInResourceInfo(def3);
					}
				}
			}
		}
	}

	public class ConditionalProduction
	{
		public class Def
		{
			public int line_number;

			public Building.Def.Bonuses bonuses;

			public string settlement_type;

			public Resource resources;

			public DT.Field condition;

			public Building.Def building_def => bonuses?.building_def;

			public bool CheckCondition(Castle castle, int level)
			{
				if (level < bonuses.min_level)
				{
					return false;
				}
				if (condition == null)
				{
					return true;
				}
				if (condition.NumValues() == 0 || condition.value.type == Value.Type.Int)
				{
					int min_amount = ((condition.NumValues() == 0) ? 1 : condition.Int());
					return castle.GetRealm().HasTag(condition.key, min_amount);
				}
				Value value = condition.Value(castle);
				if (value.type == Value.Type.Int)
				{
					return value.Int() != 0;
				}
				return false;
			}

			public void AddValue(Resource value, Castle castle, int level, bool per_settlement, bool check_condition = true, float mul = 1f)
			{
				if (check_condition && !CheckCondition(castle, level))
				{
					return;
				}
				mul *= CalcMultiplier(castle, level, per_settlement);
				if (mul != 0f)
				{
					if (Religion.MatchPietyType(building_def?.piety_type, castle?.GetKingdom()))
					{
						value.Add(resources, mul);
						return;
					}
					value.Add(resources, mul, ResourceType.Piety);
					value.Add(ResourceType.Piety, resources[ResourceType.Piety] * mul * -1f);
				}
			}

			public float CalcMultiplier(Castle castle, int level, bool per_settlement)
			{
				int num = CalcPerLevelMul(level, bonuses.min_level, bonuses.flat);
				if (num == 0)
				{
					return 0f;
				}
				if (per_settlement && bonuses.location == "realm")
				{
					return num;
				}
				int num2 = CalcSetllementTypeMul(castle?.GetRealm(), settlement_type, bonuses.location);
				return num * num2;
			}

			public override string ToString()
			{
				string text = resources.ToString();
				text = (bonuses.flat ? (text + $" at level {bonuses.min_level}") : (text + $" per level {bonuses.min_level}+"));
				if (bonuses.location == "realm")
				{
					text += " in ";
					if (settlement_type == "Castle" || settlement_type == "Town")
					{
						return text + settlement_type;
					}
					if (settlement_type == "All")
					{
						return text + "ALL settlements";
					}
					return text + " all " + settlement_type + "s";
				}
				return text + " per " + settlement_type + " in " + bonuses.location;
			}
		}

		public Def def;

		private Resource value;

		public ConditionalProduction(Def def)
		{
			this.def = def;
		}

		public Resource GetValue(Castle castle, int level, string piety_type)
		{
			if (value == null || level >= 0)
			{
				if (value == null)
				{
					value = new Resource();
				}
				else
				{
					value.Clear();
				}
				def.AddValue(value, castle, level, per_settlement: true);
			}
			return value;
		}

		public override string ToString()
		{
			return def.ToString() + " -> " + value;
		}
	}

	public class StatModifier : Stat.Modifier
	{
		public class Def
		{
			public DT.Field field;

			public Building.Def building_def;

			public string stat_name;

			public Building.Def.Bonuses bonuses;

			public string settlement_type;

			public float value;

			public Type type;

			public DT.Field condition;

			public bool Load(Game game, Building.Def building_def, DT.Field field)
			{
				if (field.children != null)
				{
					Game.Log(field.Path(include_file: true) + ": stat mod has children", Game.LogType.Warning);
				}
				this.field = field;
				this.building_def = building_def;
				stat_name = field.key;
				value = field.Float();
				if (field.FindChild("perc") != null)
				{
					type = Type.Perc;
				}
				else if (field.FindChild("base") != null)
				{
					type = Type.Base;
				}
				else if (field.FindChild("unscaled") != null)
				{
					type = Type.Unscaled;
				}
				return true;
			}

			public Def Copy()
			{
				return new Def
				{
					field = field,
					building_def = building_def,
					stat_name = stat_name,
					bonuses = bonuses,
					settlement_type = settlement_type,
					value = value,
					type = type,
					condition = condition
				};
			}

			public float CalcValue(Castle castle, int level)
			{
				Realm realm = castle?.GetRealm();
				if (realm == null)
				{
					return 0f;
				}
				if (this.value == 0f)
				{
					return 0f;
				}
				int num = CalcPerLevelMul(level, bonuses.min_level, bonuses.flat);
				if (num == 0)
				{
					return 0f;
				}
				int num2 = CalcSetllementTypeMul(realm, settlement_type, bonuses.location);
				if (num2 == 0)
				{
					return 0f;
				}
				if (condition != null)
				{
					bool flag = false;
					if (condition.NumValues() == 0 || condition.value.type == Value.Type.Int)
					{
						int min_amount = ((condition.NumValues() == 0) ? 1 : condition.Int());
						flag = realm.HasTag(condition.key, min_amount);
					}
					else
					{
						Value value = condition.Value(castle);
						flag = value.type == Value.Type.Int && value.Int() != 0;
					}
					if (!flag)
					{
						return 0f;
					}
				}
				return this.value * (float)num * (float)num2;
			}
		}

		public Def def;

		public StatModifier(Def def)
		{
			this.def = def;
			type = def.type;
		}

		public override DT.Field GetField()
		{
			return def.field;
		}

		public override DT.Field GetNameField()
		{
			return def.building_def?.field;
		}

		public override bool IsConst()
		{
			return true;
		}

		public void UpdateValue(Castle castle, int level)
		{
			float num = def.CalcValue(castle, level);
			if (num != value)
			{
				Stat stat = base.stat;
				if (stat == null)
				{
					value = num;
					return;
				}
				stat.DelModifier(this, notify_changed: false);
				value = num;
				stat.AddModifier(this);
			}
		}
	}

	public enum State
	{
		Invalid,
		Planned,
		Building,
		Damaged,
		Repairing,
		Abandoned,
		Stalled,
		TemporaryDeactivated,
		Working
	}

	public class RefData : Data
	{
		public NID castle_nid;

		public int slot_id;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Building #" + slot_id + " in " + castle_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Building { castle: not null } building))
			{
				return false;
			}
			castle_nid = building.castle;
			slot_id = building.castle.FindBuildingIdx(building);
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(castle_nid, "castle_nid");
			ser.Write7BitSigned(slot_id, "slot_id");
		}

		public override void Load(Serialization.IReader ser)
		{
			castle_nid = ser.ReadNID("castle_nid");
			slot_id = ser.Read7BitSigned("slot_id");
		}

		public override object GetObject(Game game)
		{
			return ((Castle)castle_nid.GetObj(game))?.GetBuilding(slot_id);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			Castle castle = (Castle)castle_nid.GetObj(game);
			if (castle == null)
			{
				return false;
			}
			return castle.GetBuilding(slot_id) != null;
		}
	}

	public const int MAX_TIERS = 3;

	public static bool global_resources = true;

	public static bool alt_tooltips = false;

	public Def def;

	public Castle castle;

	public State temp_state;

	public int temp_state_version;

	public List<StatModifier> realm_mods;

	public List<StatModifier> kingdom_mods;

	public List<ConditionalProduction> productions;

	public Incomes incomes;

	public int applied_level;

	private static Resource tmp_production = new Resource();

	public State state { get; private set; }

	public Building(Castle castle, Def def, State state)
	{
		this.castle = castle;
		this.def = def;
		CreateMods();
		CreateProductions();
		Incomes.CreateForBuilding(this);
		SetState(state);
	}

	public bool IsWorking(bool use_temp_state = true)
	{
		State state = (use_temp_state ? GetState() : this.state);
		if (state == State.Working)
		{
			return true;
		}
		if (!Def.SOFT_RESOURCE_REQUIREMENTS)
		{
			return false;
		}
		if (state == State.Stalled)
		{
			return true;
		}
		return false;
	}

	public bool IsFullyFunctional()
	{
		if (GetState() == State.Working)
		{
			return true;
		}
		return false;
	}

	public bool IsBuilt()
	{
		return GetState() > State.Building;
	}

	public bool IsUnderConstruction()
	{
		if (castle?.structure_build == null)
		{
			return false;
		}
		return castle.structure_build.current_building_def == def;
	}

	public bool IsPlanned()
	{
		return GetState() == State.Planned;
	}

	public bool CalcCompleted()
	{
		if (def?.upgrades?.buildings == null)
		{
			return false;
		}
		if (castle == null)
		{
			return false;
		}
		Kingdom kingdom = castle.GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (!kingdom.CheckReligionRequirements(def))
		{
			return false;
		}
		for (int i = 0; i < def.upgrades.buildings.Count; i++)
		{
			District.Def.BuildingInfo buildingInfo = def.upgrades.buildings[i];
			Building building = castle.FindBuilding(buildingInfo.def);
			if (building != null && Kingdom.in_RecalcBuildingStates)
			{
				kingdom.RecalcBuildingState(building);
			}
			if ((building == null || !building.IsFullyFunctional()) && kingdom.CheckReligionRequirements(buildingInfo.def))
			{
				return false;
			}
		}
		return true;
	}

	public bool GetTempState(out State state)
	{
		if (temp_state_version == 0 || temp_state_version != Kingdom.building_state_recalcs)
		{
			state = State.Invalid;
			return false;
		}
		state = temp_state;
		return true;
	}

	public void SetTempState(State state)
	{
		temp_state = state;
		temp_state_version = Kingdom.building_state_recalcs;
	}

	public State GetState()
	{
		if (!Kingdom.in_RecalcBuildingStates)
		{
			return this.state;
		}
		if (castle?.GetKingdom() == null)
		{
			return this.state;
		}
		if (!GetTempState(out var state))
		{
			return this.state;
		}
		if (state == State.Invalid)
		{
			return this.state;
		}
		return state;
	}

	public void SetState(State state)
	{
		this.state = state;
		int num = def.CalcLevel(castle);
		if (num != applied_level)
		{
			Deactivate();
			Activate(num);
		}
	}

	public float GetRepairCost()
	{
		if (castle == null || castle.sacking_comp == null)
		{
			return 0f;
		}
		return def.cost.Get(ResourceType.Hammers) * castle.sacking_comp.def.sacking_repair_coef;
	}

	public void OnDestroy()
	{
		if (incomes != null)
		{
			incomes.Destroy();
			incomes = null;
		}
		if (castle.burned_buildings.Contains(this))
		{
			castle.burned_buildings.Remove(this);
			if (castle.structure_build.current_building_repair == this)
			{
				castle.sack_damage -= (1f - castle.structure_build.current_progress) * GetRepairCost();
				castle.structure_build.CancelBuild(send_state: true, check_keep_progress: false);
				castle.sacking_comp.Begin();
			}
			else
			{
				castle.sack_damage -= GetRepairCost();
			}
		}
		else
		{
			Deactivate();
		}
		castle = null;
	}

	private void CreateMods()
	{
		realm_mods = new List<StatModifier>();
		kingdom_mods = new List<StatModifier>();
		if (def.bonuses != null)
		{
			for (int i = 0; i < def.bonuses.Count; i++)
			{
				Def.Bonuses bonuses = def.bonuses[i];
				CreateMods(bonuses, realm_mods, kingdom_mods);
			}
		}
	}

	private void CreateMods(Def.Bonuses bonuses, List<StatModifier> realm_mods, List<StatModifier> kingdom_mods)
	{
		if (bonuses == null)
		{
			return;
		}
		if (bonuses.stat_mods != null)
		{
			foreach (KeyValuePair<string, Def.PerSettlementModidfiers> stat_mod in bonuses.stat_mods)
			{
				Def.PerSettlementModidfiers value = stat_mod.Value;
				if (value.realm_mods != null)
				{
					for (int i = 0; i < value.realm_mods.Count; i++)
					{
						StatModifier item = new StatModifier(value.realm_mods[i]);
						realm_mods.Add(item);
					}
				}
				if (value.kingdom_mods != null)
				{
					for (int j = 0; j < value.kingdom_mods.Count; j++)
					{
						StatModifier item2 = new StatModifier(value.kingdom_mods[j]);
						kingdom_mods.Add(item2);
					}
				}
			}
		}
		CreateMods(bonuses.per_level, realm_mods, kingdom_mods);
		CreateMods(bonuses.region, realm_mods, kingdom_mods);
	}

	public static int CalcSetllementTypeMul(Realm r, string settlement_type, string location)
	{
		return r?.GetSettlementCount(settlement_type, location) ?? 0;
	}

	public static int CalcPerLevelMul(int level, int min_level, bool flat)
	{
		if (level < min_level)
		{
			return 0;
		}
		if (flat)
		{
			return 1;
		}
		return level - min_level + 1;
	}

	private void UpdateMods(int level)
	{
		if (realm_mods != null)
		{
			for (int i = 0; i < realm_mods.Count; i++)
			{
				realm_mods[i].UpdateValue(castle, level);
			}
		}
		if (kingdom_mods != null)
		{
			for (int j = 0; j < kingdom_mods.Count; j++)
			{
				kingdom_mods[j].UpdateValue(castle, level);
			}
		}
	}

	private void ApplyMods()
	{
		if (castle == null)
		{
			return;
		}
		Realm realm = castle.GetRealm();
		if (realm == null)
		{
			return;
		}
		Kingdom kingdom = castle.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		if (realm_mods != null)
		{
			for (int i = 0; i < realm_mods.Count; i++)
			{
				StatModifier statModifier = realm_mods[i];
				realm.stats.AddModifier(statModifier.def.stat_name, statModifier);
			}
		}
		if (kingdom_mods != null)
		{
			for (int j = 0; j < kingdom_mods.Count; j++)
			{
				StatModifier statModifier2 = kingdom_mods[j];
				kingdom.stats.AddModifier(statModifier2.def.stat_name, statModifier2);
			}
		}
	}

	private void RevertMods()
	{
		if (castle == null)
		{
			return;
		}
		Realm realm = castle.GetRealm();
		if (realm == null)
		{
			return;
		}
		Kingdom kingdom = castle.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		if (realm_mods != null)
		{
			for (int i = 0; i < realm_mods.Count; i++)
			{
				StatModifier statModifier = realm_mods[i];
				realm.stats.DelModifier(statModifier.def.stat_name, statModifier);
			}
		}
		if (kingdom_mods != null)
		{
			for (int j = 0; j < kingdom_mods.Count; j++)
			{
				StatModifier statModifier2 = kingdom_mods[j];
				kingdom.stats.DelModifier(statModifier2.def.stat_name, statModifier2);
			}
		}
	}

	private void ApplyUpkeep()
	{
		if (!(def.upkeep <= 0f))
		{
			Kingdom kingdom = castle?.GetKingdom();
			if (kingdom != null)
			{
				kingdom.upkeepBuildings += def.upkeep;
			}
		}
	}

	private void RevertUpkeep()
	{
		if (!(def.upkeep <= 0f))
		{
			Kingdom kingdom = castle?.GetKingdom();
			if (kingdom != null)
			{
				kingdom.upkeepBuildings -= def.upkeep;
			}
		}
	}

	private void CreateProductions()
	{
		productions = new List<ConditionalProduction>();
		if (def.bonuses != null)
		{
			for (int i = 0; i < def.bonuses.Count; i++)
			{
				Def.Bonuses bonuses = def.bonuses[i];
				CreateProductions(bonuses);
			}
		}
	}

	private void CreateProductions(Def.Bonuses bonuses)
	{
		if (bonuses == null)
		{
			return;
		}
		if (bonuses.productions != null)
		{
			foreach (KeyValuePair<string, List<ConditionalProduction.Def>> production in bonuses.productions)
			{
				List<ConditionalProduction.Def> value = production.Value;
				for (int i = 0; i < value.Count; i++)
				{
					ConditionalProduction item = new ConditionalProduction(value[i]);
					productions.Add(item);
				}
			}
		}
		CreateProductions(bonuses.per_level);
		CreateProductions(bonuses.region);
	}

	private void ApplyProduction(int level)
	{
		AddProductions(productions, level);
	}

	private void RevertProduction()
	{
		AddProductions(productions, -1);
	}

	public bool FixPiety()
	{
		if (string.IsNullOrEmpty(def?.piety_type))
		{
			return false;
		}
		if (applied_level <= 0)
		{
			return false;
		}
		RevertProduction();
		ApplyProduction(applied_level);
		return true;
	}

	private void AddProductions(List<ConditionalProduction> productions, int level)
	{
		if (castle == null || productions == null)
		{
			return;
		}
		Realm realm = castle.GetRealm();
		if (realm == null || realm.settlements == null)
		{
			return;
		}
		int num = ((level >= 0) ? 1 : (-1));
		for (int i = 0; i < realm.settlements.Count; i++)
		{
			Settlement settlement = realm.settlements[i];
			if (settlement == null || !settlement.IsActiveSettlement())
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < productions.Count; j++)
			{
				ConditionalProduction conditionalProduction = productions[j];
				if (conditionalProduction?.def != null)
				{
					string type_filter = ((conditionalProduction.def.bonuses.location != "realm") ? "Castle" : conditionalProduction.def.settlement_type);
					if (settlement.MatchType(type_filter))
					{
						Resource value = conditionalProduction.GetValue(castle, level, def.piety_type);
						settlement.production_from_buildings.Add(value, num);
						flag = true;
					}
				}
			}
			if (flag)
			{
				settlement.NotifyListeners("production_changed");
			}
		}
	}

	private void ApplyRegPopInfl(bool add = true)
	{
		if (def?.regPopInfMods == null)
		{
			return;
		}
		Realm realm = castle.GetRealm();
		if (realm != null)
		{
			float valueMultiplier = (add ? 1 : (-1));
			for (int i = 0; i < def.regPopInfMods.Count; i++)
			{
				Realm.RegionPopInfModifier mod = def.regPopInfMods[i];
				realm.AddRegPopInfMod(mod, valueMultiplier);
			}
		}
	}

	private void RevertRegPopInfl()
	{
		ApplyRegPopInfl(add: false);
	}

	private void Activate(int level)
	{
		if (level >= 1)
		{
			applied_level = level;
			UpdateMods(level);
			ApplyMods();
			ApplyUpkeep();
			ApplyProduction(level);
			ApplyRegPopInfl();
			if (!string.IsNullOrEmpty(def.on_activate))
			{
				castle?.GetKingdom().NotifyListeners(def.on_activate, this);
			}
		}
	}

	public void Deactivate(bool force = false)
	{
		if (applied_level <= 0)
		{
			if (!force)
			{
				return;
			}
			Game.Log($"Trying to deactivate already inactive building {this} ", Game.LogType.Warning);
		}
		RevertProduction();
		RevertRegPopInfl();
		RevertMods();
		RevertUpkeep();
		applied_level = 0;
		if (!string.IsNullOrEmpty(def.on_deactivate))
		{
			castle?.GetKingdom().NotifyListeners(def.on_deactivate, this);
		}
	}

	public float GetHappinessMod()
	{
		float num = 0f;
		foreach (StatModifier realm_mod in realm_mods)
		{
			if (realm_mod.def.stat_name == "rs_happiness")
			{
				num += realm_mod.value;
			}
		}
		return num;
	}

	public Resource CalcProduction(Resource res = null, bool check_condition = true)
	{
		return def.CalcProduction(res, castle, applied_level, check_condition);
	}

	public Resource CalcMaxProduction(Resource res = null, bool check_condition = true)
	{
		return def.CalcProduction(res, castle, 3, check_condition);
	}

	public float GetCommerceMod()
	{
		tmp_production.Clear();
		CalcProduction(tmp_production);
		return tmp_production[ResourceType.Trade];
	}

	public int CalcLevel()
	{
		return def.CalcLevel(castle);
	}

	public float CalcUpgradedRatio(out int num_upgrades, out int max_upgrades, bool exclude_unbuildable = true)
	{
		num_upgrades = (max_upgrades = 0);
		Kingdom kingdom = castle?.GetKingdom();
		if (kingdom == null)
		{
			return 0f;
		}
		if (this.def?.upgrades?.buildings == null)
		{
			return 1f;
		}
		for (int i = 0; i < this.def.upgrades.buildings.Count; i++)
		{
			Def def = this.def.upgrades.buildings[i].def;
			if (kingdom.CheckReligionRequirements(def) && (!exclude_unbuildable || castle.CheckBuildingRequirements(def)))
			{
				max_upgrades++;
				if (kingdom.HasBuildingUpgrade(def))
				{
					num_upgrades++;
				}
			}
		}
		if (max_upgrades == 0)
		{
			return 0f;
		}
		return (float)num_upgrades / (float)max_upgrades;
	}

	public float CalcUpgradedRatio()
	{
		int num_upgrades;
		int max_upgrades;
		return CalcUpgradedRatio(out num_upgrades, out max_upgrades);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "id":
			return "#" + def.id;
		case "id_key":
			return def.id;
		case "def":
			return def;
		case "castle":
			return castle;
		case "realm":
			return castle?.GetRealm();
		case "kingdom":
			return castle?.GetKingdom();
		case "index":
			if (castle != null)
			{
				return new Value(castle.FindBuildingIdx(this));
			}
			return Value.Null;
		case "upkeep":
			if (!(def.upkeep > 0f))
			{
				return Value.Null;
			}
			return new Value(def.upkeep);
		case "state":
			return state.ToString();
		case "is_built":
			return IsBuilt();
		case "is_planned":
			return IsPlanned();
		case "name":
			return def.field.key;
		case "happiness":
			return GetHappinessMod();
		case "production":
			return CalcProduction();
		case "max_production":
			return CalcMaxProduction();
		case "commerce":
			return GetCommerceMod();
		case "level":
			return CalcLevel();
		case "num_upgrades":
		{
			CalcUpgradedRatio(out var num_upgrades2, out var _);
			return num_upgrades2;
		}
		case "max_upgrades":
		{
			CalcUpgradedRatio(out var _, out var max_upgrades);
			return max_upgrades;
		}
		case "upgraded_ratio":
			return CalcUpgradedRatio();
		default:
			if (castle != null)
			{
				Value var = castle.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			return Value.Unknown;
		}
	}

	public override string ToString()
	{
		return $"[{state}] {def?.id}";
	}
}
