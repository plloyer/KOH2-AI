using System;
using System.Collections.Generic;

namespace Logic;

public class RebellionRisk : Component
{
	public class CategoryInfo
	{
		public float value;

		public string stat_type;

		public Stat.Modifier stat_mod;

		public override string ToString()
		{
			return $"Category {stat_type}: {value}";
		}
	}

	public class Def : Logic.Def
	{
		public float low_stability_in_realm_message_threshold = -20f;

		public int max_rebels_per_realm = 3;

		public int max_migrants_per_realm = 3;

		public int max_migrants_per_kingdom = 15;

		public int max_migrants_per_game = 300;

		public int rp_min_immigrants = 1;

		public int rp_max_immigrants = 3;

		public float risk_rebellion_zone = -5f;

		public float risk_disloyal_population = -30f;

		public float risk_occupied_by_rebels;

		public float risk_nearby_occupied_by_rebels;

		public float risk_nearby_occupied_by_our_loyalists;

		public float risk_rebel_leader_base;

		public float risk_rebel_leader_per_general;

		public float risk_rebel_leader_per_rebel;

		public float risk_rebel_armies_in_neigbors_multiplier = 0.5f;

		public float risk_religios_differences_multiplier = 1f;

		public float risk_cultural_differences_multiplier = 1f;

		public float risk_same_culture_family = -2f;

		public float risk_different_culture_family = -8f;

		public float risk_army_presence_multiplier = 1f;

		public float risk_sacked = -10f;

		public float risk_dead_king = 5f;

		public float dead_king_risk_period = 600f;

		public float rebel_risk_base_period = 120f;

		public float rebel_risk_variable_period = 60f;

		public float create_rebel_pop_mod = 1f;

		public float max_create_rebel_pop = 75f;

		public float calm_rebel_pop_mod = 2.5f;

		public float max_calm_rebel_pop = 75f;

		public float risk_per_occupied_realm_base = 3f;

		public float risk_per_occupied_realm_other_subreligion = 2f;

		public float risk_per_occupied_realm_other_mainreligion = 5f;

		public float risk_per_occupied_realm_other_subculture = 1f;

		public float risk_per_occupied_realm_other_mainculture = 3f;

		public float risk_per_occupied_realm_controller_army_in_town = 5f;

		public float risk_per_occupied_realm_other_per_controller_garrison = 0.5f;

		public float risk_per_occupied_realm_min;

		public float risk_per_occupied_realm_max = 10000f;

		public float loyalist_spawn_chance_mod = 0.5f;

		public PerLevelValues chance_rebel_pop_take_action;

		public float chance_to_reinforce = 30f;

		public float chance_to_rebel = 30f;

		public float chance_to_migrate = 40f;

		public float desirability_rebel_risk_mod = -1f;

		public float desirability_same_religion = 20f;

		public float desirability_gold_mod = 1f;

		public int min_reinforce = 1;

		public int max_reinforce = 6;

		public int reinforce_gold = 100;

		public int min_rebelling = 3;

		public int max_rebelling = 6;

		public int rp_min_for_rebellion_spawn = 3;

		public float rebellious_weight_base = 1f;

		public float rebellios_weight_per_rebel_population = 2f;

		public float rebellious_weght_governed = 3f;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			low_stability_in_realm_message_threshold = field.GetFloat("low_stability_in_realm_message_threshold", null, low_stability_in_realm_message_threshold);
			max_rebels_per_realm = field.GetInt("max_rebels_per_realm", null, max_rebels_per_realm);
			max_migrants_per_realm = field.GetInt("max_migrants_per_realm", null, max_migrants_per_realm);
			max_migrants_per_kingdom = field.GetInt("max_migrants_per_kingdom", null, max_migrants_per_kingdom);
			max_migrants_per_game = field.GetInt("max_migrants_per_game", null, max_migrants_per_game);
			rp_min_immigrants = field.GetInt("rp_min_immigrants", null, rp_min_immigrants);
			rp_max_immigrants = field.GetInt("rp_max_immigrants", null, rp_max_immigrants);
			risk_rebellion_zone = field.GetFloat("risk_rebellion_zone", null, risk_rebellion_zone);
			risk_disloyal_population = field.GetFloat("risk_disloyal_population", null, risk_disloyal_population);
			risk_occupied_by_rebels = field.GetFloat("risk_occupied_by_rebels", null, risk_occupied_by_rebels);
			risk_nearby_occupied_by_rebels = field.GetFloat("risk_nearby_occupied_by_rebels", null, risk_nearby_occupied_by_rebels);
			risk_nearby_occupied_by_our_loyalists = field.GetFloat("risk_nearby_occupied_by_our_loyalists", null, risk_nearby_occupied_by_our_loyalists);
			risk_religios_differences_multiplier = field.GetFloat("risk_religios_differences_multiplier", null, risk_religios_differences_multiplier);
			risk_cultural_differences_multiplier = field.GetFloat("risk_cultural_differences_multiplier", null, risk_cultural_differences_multiplier);
			risk_same_culture_family = field.GetFloat("risk_same_culture_family", null, risk_same_culture_family);
			risk_different_culture_family = field.GetFloat("risk_different_culture_family", null, risk_different_culture_family);
			risk_army_presence_multiplier = field.GetFloat("risk_risk_army_presence_multiplier", null, risk_army_presence_multiplier);
			risk_rebel_leader_base = field.GetFloat("risk_rebel_leader_base", null, risk_rebel_leader_base);
			risk_rebel_leader_per_general = field.GetFloat("risk_rebel_leader_per_general", null, risk_rebel_leader_per_general);
			risk_rebel_leader_per_rebel = field.GetFloat("risk_rebel_leader_per_rebel", null, risk_rebel_leader_per_rebel);
			risk_rebel_armies_in_neigbors_multiplier = field.GetFloat("risk_rebel_armies_in_neigbors_multiplier", null, risk_rebel_armies_in_neigbors_multiplier);
			risk_sacked = field.GetFloat("risk_sacked", null, risk_sacked);
			risk_dead_king = field.GetFloat("risk_dead_king", null, risk_dead_king);
			dead_king_risk_period = field.GetFloat("dead_king_risk_period", null, dead_king_risk_period);
			rebel_risk_base_period = field.GetFloat("rebel_risk_base_period", null, rebel_risk_base_period);
			rebel_risk_variable_period = field.GetFloat("rebel_risk_variable_period", null, rebel_risk_variable_period);
			create_rebel_pop_mod = field.GetFloat("create_rebel_pop_mod", null, create_rebel_pop_mod);
			max_create_rebel_pop = field.GetFloat("max_create_rebel_pop", null, max_create_rebel_pop);
			calm_rebel_pop_mod = field.GetFloat("calm_rebel_pop_mod", null, calm_rebel_pop_mod);
			max_calm_rebel_pop = field.GetFloat("max_calm_rebel_pop", null, max_calm_rebel_pop);
			risk_per_occupied_realm_base = field.GetFloat("risk_per_occupied_realm", null, risk_per_occupied_realm_base);
			risk_per_occupied_realm_other_subreligion = field.GetFloat("risk_per_occupied_realm_other_subreligion", null, risk_per_occupied_realm_other_subreligion);
			risk_per_occupied_realm_other_mainreligion = field.GetFloat("risk_per_occupied_realm_other_mainreligion", null, risk_per_occupied_realm_other_mainreligion);
			risk_per_occupied_realm_other_subculture = field.GetFloat("risk_per_occupied_realm_other_subculture", null, risk_per_occupied_realm_other_subculture);
			risk_per_occupied_realm_other_mainculture = field.GetFloat("risk_per_occupied_realm_other_mainculture", null, risk_per_occupied_realm_other_mainculture);
			risk_per_occupied_realm_controller_army_in_town = field.GetFloat("risk_per_occupied_realm_controller_army_in_town", null, risk_per_occupied_realm_controller_army_in_town);
			risk_per_occupied_realm_other_per_controller_garrison = field.GetFloat("risk_per_occupied_realm_other_per_controller_garrison", null, risk_per_occupied_realm_other_per_controller_garrison);
			risk_per_occupied_realm_min = field.GetFloat("risk_per_occupied_realms_min", null, risk_per_occupied_realm_min);
			risk_per_occupied_realm_max = field.GetFloat("risk_per_occupied_realms_max", null, risk_per_occupied_realm_max);
			loyalist_spawn_chance_mod = field.GetFloat("loyalist_spawn_chance_mod", null, loyalist_spawn_chance_mod);
			chance_rebel_pop_take_action = PerLevelValues.Parse<float>(field.FindChild("chance_rebel_pop_take_action"));
			chance_to_reinforce = field.GetFloat("chance_to_reinforce", null, chance_to_reinforce);
			chance_to_rebel = field.GetFloat("chance_to_rebel", null, chance_to_rebel);
			chance_to_migrate = field.GetFloat("chance_to_migrate", null, chance_to_migrate);
			desirability_rebel_risk_mod = field.GetFloat("desirability_rebel_risk_mod", null, desirability_rebel_risk_mod);
			desirability_same_religion = field.GetFloat("desirability_same_religion", null, desirability_same_religion);
			desirability_gold_mod = field.GetFloat("desirability_gold_mod", null, desirability_gold_mod);
			min_reinforce = field.GetInt("min_reinforce", null, min_reinforce);
			max_reinforce = field.GetInt("max_reinforce", null, max_reinforce);
			reinforce_gold = field.GetInt("reinforce_gold", null, reinforce_gold);
			min_rebelling = field.GetInt("min_rebelling", null, min_rebelling);
			max_rebelling = field.GetInt("max_rebelling", null, max_rebelling);
			rebellious_weight_base = field.GetFloat("rebellious_weight_base", null, rebellious_weight_base);
			rebellios_weight_per_rebel_population = field.GetFloat("rebellios_weight_per_rebel_population", null, rebellios_weight_per_rebel_population);
			rebellious_weght_governed = field.GetFloat("rebellious_weght_governed", null, rebellious_weght_governed);
			rp_min_for_rebellion_spawn = field.GetInt("rp_min_for_rebellion_spawn ", null, rp_min_for_rebellion_spawn);
			return true;
		}
	}

	public static bool enabled = true;

	public Def def;

	public Realm realm;

	public Stat local_stats;

	public RebelSpawnCondition.Def[] conditions;

	private CategoryInfo[] rebellion_risk_per_category;

	private int happiness_category_id = -1;

	public float value;

	private static List<Kingdom> rebel_kingdoms = new List<Kingdom>();

	public int NumCategories()
	{
		if (rebellion_risk_per_category == null)
		{
			return 0;
		}
		return rebellion_risk_per_category.Length;
	}

	public void SetCategory(float val, int i)
	{
		rebellion_risk_per_category[i].value = val;
	}

	public RebellionRisk(Realm realm)
		: base(realm)
	{
		this.realm = realm;
		def = base.game.defs.GetBase<Def>();
		Build();
		UpdateAfter(def.rebel_risk_base_period + base.game.Random(0f, def.rebel_risk_variable_period));
	}

	public override void OnUpdate()
	{
		if (realm == null || !realm.IsValid())
		{
			Destroy();
			return;
		}
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom != null)
		{
			UpdateAfter(def.rebel_risk_base_period + base.game.Random(0f, def.rebel_risk_variable_period));
			if (!kingdom.IsDefeated())
			{
				Recalc(think_rebel_pop: true);
				CheckDespawn();
			}
		}
	}

	public void CheckDespawn()
	{
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom?.stability == null)
		{
			return;
		}
		int max_rebellions_per_game = kingdom.stability.def.max_rebellions_per_game;
		List<Rebellion> rebellions = base.game.rebellions;
		int num = 0;
		for (int i = 0; i < rebellions.Count; i++)
		{
			Rebellion rebellion = rebellions[i];
			if (rebellion != null && rebellion.rebels.Count != 0 && rebellion.leader != null)
			{
				num++;
			}
		}
		if (num <= max_rebellions_per_game)
		{
			return;
		}
		rebellions.Sort((Rebellion x, Rebellion y) => x.DespawnScore().CompareTo(y.DespawnScore()));
		int num2 = num - max_rebellions_per_game;
		for (int num3 = rebellions.Count - 1; num3 >= 0; num3--)
		{
			Rebellion rebellion2 = rebellions[num3];
			if (rebellion2 != null && rebellion2.rebels.Count != 0 && rebellion2.leader != null)
			{
				RebellionIndependence component = rebellion2.GetComponent<RebellionIndependence>();
				if (component == null || !component.DeclareIndependence())
				{
					rebellion2.defeatedByKingdom = rebellion2.GetOriginKingdom();
					rebellion2.Disband();
				}
				num2--;
				rebellions.Remove(rebellion2);
				if (num2 <= 0)
				{
					break;
				}
			}
		}
	}

	private void Build()
	{
		List<RebelSpawnCondition.Def> defs = base.game.defs.GetDefs<RebelSpawnCondition.Def>();
		if (defs == null)
		{
			return;
		}
		List<RebelSpawnCondition.Def> list = new List<RebelSpawnCondition.Def>();
		for (int i = 0; i < defs.Count; i++)
		{
			RebelSpawnCondition.Def def = defs[i];
			if (def.periodic)
			{
				list.Add(def);
			}
		}
		conditions = list.ToArray();
		local_stats = realm.stats.Find(Stats.rs_stability);
		rebellion_risk_per_category = BuildInfo(local_stats, base.game, global: false);
		happiness_category_id = -1;
		for (int j = 0; j < rebellion_risk_per_category.Length; j++)
		{
			if (rebellion_risk_per_category[j].stat_type == "happiness")
			{
				happiness_category_id = j;
				break;
			}
		}
	}

	public static CategoryInfo[] BuildInfo(Stat stat, Game game, bool global)
	{
		List<RebellionRiskCategory.Def> categories = RebellionRiskCategory.GetCategories(game);
		List<CategoryInfo> list = new List<CategoryInfo>();
		for (int i = 0; i < categories.Count; i++)
		{
			if (categories[i].isGlobal != global)
			{
				continue;
			}
			CategoryInfo categoryInfo = new CategoryInfo();
			categoryInfo.stat_type = categories[i].stat_name;
			categoryInfo.value = 0f;
			if (stat.all_mods != null)
			{
				for (int j = 0; j < stat.all_mods.Count; j++)
				{
					if (stat.all_mods[j].GetField().key == categoryInfo.stat_type)
					{
						categoryInfo.stat_mod = stat.all_mods[j];
						break;
					}
				}
			}
			for (int k = 0; k < stat.def.global_mods.Count; k++)
			{
				if (stat.def.global_mods[k].field.key == categoryInfo.stat_type)
				{
					categoryInfo.stat_mod = stat.def.global_mods[k];
					break;
				}
			}
			categories[i].index = list.Count;
			list.Add(categoryInfo);
		}
		return list.ToArray();
	}

	public float Round(float val)
	{
		val = (float)Math.Round(val, MidpointRounding.AwayFromZero);
		return val;
	}

	public void OnKingdomChanged()
	{
		Recalc(think_rebel_pop: false, allow_rebel_spawn: false);
	}

	public string Recalc(bool think_rebel_pop = false, bool allow_rebel_spawn = true)
	{
		if (!realm.IsAuthority())
		{
			return string.Empty;
		}
		if (realm == null)
		{
			return string.Empty;
		}
		value = (float)Math.Floor(realm.GetStat(Stats.rs_stability));
		for (int i = 0; i < rebellion_risk_per_category.Length; i++)
		{
			rebellion_risk_per_category[i].value = rebellion_risk_per_category[i].stat_mod.value;
		}
		CheckStability();
		realm.SendState<Realm.StabilityState>();
		if (think_rebel_pop && enabled && realm.castle?.population != null && realm.castle.battle == null)
		{
			return ThinkRebeliousPopulation(allow_rebel_spawn);
		}
		return string.Empty;
	}

	public void CheckStability()
	{
		if (!base.game.IsPaused() && realm.GetTotalRebellionRisk() < def.low_stability_in_realm_message_threshold)
		{
			Kingdom kingdom = realm.GetKingdom();
			if (kingdom?.rebellions != null && kingdom.rebellions.Count == 0)
			{
				kingdom.NotifyListeners("low_stability_in_realm");
			}
		}
	}

	public float risk_from_rebel_armies(Realm realm)
	{
		float num = 0f;
		for (int i = 0; i < realm.armies.Count; i++)
		{
			Rebel rebel = realm.armies[i].rebel;
			if (rebel == null)
			{
				continue;
			}
			float num2 = 0f;
			if (!rebel.IsLeader())
			{
				num2 = ((!rebel.IsGeneral()) ? (num2 + def.risk_rebel_leader_per_rebel) : (num2 + def.risk_rebel_leader_per_general));
			}
			else
			{
				num2 += def.risk_rebel_leader_base;
				if (rebel.character != null)
				{
					num2 -= (float)rebel.character.GetClassLevel();
				}
			}
			num = Math.Min(num, num2);
		}
		return num;
	}

	private void AttemptConvert()
	{
		if (!realm.castle.IsAuthority())
		{
			return;
		}
		int rebelion_risk = realm.castle.rebelion_risk;
		float totalRebellionRisk = realm.GetTotalRebellionRisk();
		int num = realm.castle.population.workers + realm.castle.population.rebels;
		if (totalRebellionRisk < 0f)
		{
			if (realm.castle.game.session_time.minutes < realm.castle.game.GetMinRebelPopTime())
			{
				return;
			}
			float num2 = 0.008f * (float)Math.Pow(Math.Abs(totalRebellionRisk) * def.create_rebel_pop_mod, 0.6000000238418579);
			realm.castle.population.rebelion_acc = Math.Min(realm.castle.population.rebelion_acc + num2, 1f);
			if (realm.castle.population.rebelion_acc + num2 * 5f >= 0.75f)
			{
				realm.castle.rebelion_risk = 2;
			}
			else if (realm.castle.population.rebelion_acc + num2 * 30f >= 0.75f)
			{
				realm.castle.rebelion_risk = 1;
			}
			else
			{
				realm.castle.rebelion_risk = 0;
			}
		}
		else
		{
			float num3 = 0.016f * (float)Math.Pow(Math.Abs(totalRebellionRisk) * def.calm_rebel_pop_mod, 0.6000000238418579);
			realm.castle.population.rebelion_acc = Math.Max(realm.castle.population.rebelion_acc - num3, 0f);
			if (realm.castle.population.rebelion_acc > 0.75f)
			{
				realm.castle.rebelion_risk = 2;
			}
			else
			{
				realm.castle.rebelion_risk = 0;
			}
		}
		int num4 = (int)Math.Ceiling(realm.castle.population.rebelion_acc * (float)num);
		if (num4 > realm.castle.population.rebels)
		{
			realm.castle.population.ConvertToRebel(1 + (num4 - realm.castle.population.rebels) / 4, send_state: false);
		}
		else if (num4 < realm.castle.population.rebels)
		{
			realm.castle.population.ConvertToWorker(1 + (realm.castle.population.rebels - num4) / 4, send_state: false);
		}
		if (rebelion_risk != realm.castle.rebelion_risk)
		{
			realm.castle.NotifyListeners("rebelion_risk_changed");
		}
		realm.castle.SendState<Castle.PopulationState>();
	}

	private string ThinkRebeliousPopulation(bool allow_rebel_spawn)
	{
		int rebels = realm.castle.population.GetRebels();
		int workers = realm.castle.population.GetWorkers();
		int num = rebels + workers;
		if (num == 0)
		{
			return string.Empty;
		}
		AttemptConvert();
		if (rebels < 2 || rebels < workers)
		{
			return string.Empty;
		}
		if (realm.castle.rebelion_risk < 2)
		{
			return string.Empty;
		}
		float num2 = 50f * (float)rebels / (float)num;
		if (base.game.Random100CheatedUp(realm.GetKingdom().balance_factor_luck) >= num2)
		{
			return string.Empty;
		}
		float num3 = (allow_rebel_spawn ? def.chance_to_rebel : 0f);
		float chance_to_reinforce = def.chance_to_reinforce;
		if (base.game.Random(0f, num3 + chance_to_reinforce) <= chance_to_reinforce && TryReinforce())
		{
			realm.castle.population.rebelion_acc = 0.25f;
			return "rebel_reinforces";
		}
		if (TryRebel())
		{
			realm.castle.population.rebelion_acc = 0.25f;
			return "rebel_spawned";
		}
		return string.Empty;
	}

	private int MaxRebelsPerKingdom()
	{
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom?.stability == null)
		{
			return 0;
		}
		if (kingdom.is_player)
		{
			DevSettings.Def devSettingsDef = base.game.GetDevSettingsDef();
			return base.game.GetPerDifficultyInt(devSettingsDef.max_rebels_per_player_kingdom, null, kingdom.stability.def.max_rebels_per_kingdom);
		}
		return kingdom.stability.def.max_rebels_per_kingdom;
	}

	public bool CanRebel()
	{
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom?.stability == null)
		{
			return false;
		}
		if (kingdom.stability.def.max_rebels_per_game <= GetRebelCount(base.game) || MaxRebelsPerKingdom() <= kingdom.stability.GetRebelCountKingdom() || def.max_rebels_per_realm <= GetRebelCountRealm(realm) || def.rp_min_for_rebellion_spawn >= realm.castle.population.GetRebels())
		{
			return false;
		}
		if (!enabled)
		{
			return false;
		}
		return true;
	}

	public bool CanRebel(out Rebellion rebellion)
	{
		rebellion = null;
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom?.stability == null)
		{
			return false;
		}
		if (!kingdom.stability.CanHaveMoreRebellions())
		{
			for (int i = 0; i < realm.rebellions.Count; i++)
			{
				Rebellion rebellion2 = realm.rebellions[i];
				if (rebellion2.rebels.Count < rebellion2.def.max_rebels)
				{
					rebellion = rebellion2;
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public bool TryRebel()
	{
		if (!CanRebel())
		{
			return false;
		}
		if (!CanRebel(out var rebellion))
		{
			return false;
		}
		Rebel.Def result = null;
		RebelSpawnCondition.Def c_def = null;
		if (rebellion != null)
		{
			result = rebellion.rebel_def;
			c_def = rebellion.leader.condition_def;
			new Rebel(base.game, rebellion.kingdom_id, rebellion.loyal_to, realm, c_def, result, rebellion);
		}
		else
		{
			if (!CheckConditions(realm, conditions, out result, out c_def))
			{
				result = base.game.defs.Get<Rebel.Def>("Rebels");
			}
			Kingdom factionKingdom = FactionUtils.GetFactionKingdom(base.game, result.kingdom_key);
			int num = ((result.fraction_type == "LoyalistsFaction") ? realm.pop_majority.kingdom.id : factionKingdom.id);
			if (num == 0)
			{
				if (realm.init_kingdom_id != realm.kingdom_id && realm.init_kingdom_id > 0)
				{
					num = realm.init_kingdom_id;
				}
				else
				{
					result = base.game.defs.Get<Rebel.Def>("Rebels");
					factionKingdom = FactionUtils.GetFactionKingdom(base.game, result.kingdom_key);
					num = factionKingdom.id;
				}
			}
			new Rebel(base.game, factionKingdom.id, num, realm, c_def, result, rebellion);
		}
		int num2 = base.game.Random(def.min_rebelling, def.max_rebelling);
		int num3 = realm.castle.population.GetRebels() - 1;
		if (num2 > num3)
		{
			num2 = num3;
		}
		realm.castle.population.RemoveVillagers(num2, Population.Type.Rebel);
		return true;
	}

	public Rebel ForceRebel(Rebel.Def rDef = null, RebelSpawnCondition.Def cDef = null)
	{
		if (rDef == null && !CheckConditions(realm, conditions, out rDef, out cDef))
		{
			rDef = base.game.defs.Get<Rebel.Def>("Rebels");
		}
		Kingdom factionKingdom = FactionUtils.GetFactionKingdom(base.game, rDef.kingdom_key);
		int num = ((rDef.fraction_type == "LoyalistsFaction") ? realm.pop_majority.kingdom.id : factionKingdom.id);
		if (num == 0)
		{
			if (realm.init_kingdom_id != realm.kingdom_id && realm.init_kingdom_id > 0)
			{
				num = realm.init_kingdom_id;
			}
			else
			{
				rDef = base.game.defs.Get<Rebel.Def>("Rebels");
				factionKingdom = FactionUtils.GetFactionKingdom(base.game, rDef.kingdom_key);
				num = factionKingdom.id;
			}
		}
		return new Rebel(base.game, factionKingdom.id, num, realm, cDef, rDef);
	}

	public static int GetRebelCount(Game game)
	{
		rebel_kingdoms.Clear();
		int num = 0;
		List<Rebel.Def> defs = game.defs.GetDefs<Rebel.Def>();
		for (int i = 0; i < defs.Count; i++)
		{
			Kingdom factionKingdom = FactionUtils.GetFactionKingdom(game, defs[i].kingdom_key);
			if (factionKingdom != null && !rebel_kingdoms.Contains(factionKingdom))
			{
				rebel_kingdoms.Add(factionKingdom);
			}
		}
		for (int j = 0; j < rebel_kingdoms.Count; j++)
		{
			foreach (Army army in rebel_kingdoms[j].armies)
			{
				if (army.rebel != null)
				{
					num++;
				}
			}
		}
		return num;
	}

	public int GetRebelCountRealm(Realm r)
	{
		int num = 0;
		foreach (Army army in r.armies)
		{
			if (army.rebel != null)
			{
				num++;
			}
		}
		return num;
	}

	public int GetMigrantCount()
	{
		int num = 0;
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			num += GetMigrantCountKingdom(base.game.kingdoms[i]);
		}
		return num;
	}

	public int GetMigrantCountRealm(Realm r)
	{
		int num = 0;
		foreach (Migrant migrant in r.migrants)
		{
			if (migrant != null)
			{
				num++;
			}
		}
		return num;
	}

	public int GetMigrantCountKingdom(Kingdom k)
	{
		int num = 0;
		for (int i = 0; i < k.realms.Count; i++)
		{
			num += GetMigrantCountRealm(k.realms[i]);
		}
		return num;
	}

	public float GetRebellionRisk(int type)
	{
		if (rebellion_risk_per_category == null || rebellion_risk_per_category.Length == 0 || rebellion_risk_per_category.Length <= type)
		{
			return 0f;
		}
		return rebellion_risk_per_category[type].value;
	}

	public float GetRebellionRisk(string stat_type)
	{
		if (rebellion_risk_per_category == null || rebellion_risk_per_category.Length == 0)
		{
			return 0f;
		}
		for (int i = 0; i < rebellion_risk_per_category.Length; i++)
		{
			if (rebellion_risk_per_category[i].stat_type == stat_type)
			{
				return rebellion_risk_per_category[i].value;
			}
		}
		return 0f;
	}

	public float CalcRebellionRisk(int type)
	{
		List<RebellionRiskCategory.Def> categories = RebellionRiskCategory.GetCategories(base.game);
		if (categories == null)
		{
			return 0f;
		}
		if (type < 0 || type >= categories.Count)
		{
			return 0f;
		}
		string stat_name = categories[type].stat_name;
		if (local_stats?.def?.global_mods != null)
		{
			for (int i = 0; i < local_stats.def.global_mods.Count; i++)
			{
				Stat.GlobalModifier globalModifier = local_stats.def.global_mods[i];
				if (globalModifier.GetField().key == stat_name)
				{
					return globalModifier.value;
				}
			}
		}
		if (local_stats?.all_mods != null)
		{
			for (int j = 0; j < local_stats.all_mods.Count; j++)
			{
				Stat.Modifier modifier = local_stats.all_mods[j];
				if (modifier.GetField()?.key == stat_name)
				{
					return modifier.value;
				}
			}
		}
		return 0f;
	}

	public static float GetGlobalRebellionRisk(int type, Kingdom k, Stat global_stat)
	{
		if (k == null)
		{
			return 0f;
		}
		RebellionRiskCategory.Def def = RebellionRiskCategory.GetCategories(k.game)[type];
		if (global_stat?.def.global_mods != null)
		{
			for (int i = 0; i < global_stat.def.global_mods.Count; i++)
			{
				Stat.GlobalModifier globalModifier = global_stat.def.global_mods[i];
				if (globalModifier.GetField().key == def.stat_name)
				{
					return globalModifier.value;
				}
			}
		}
		if (global_stat?.all_mods != null)
		{
			for (int j = 0; j < global_stat.all_mods.Count; j++)
			{
				Stat.Modifier modifier = global_stat.all_mods[j];
				if (modifier.GetField().key == def.stat_name)
				{
					return modifier.value;
				}
			}
		}
		return 0f;
	}

	public float GetRebelionRisk_5()
	{
		return GetRebelionRisk_AtTime(5);
	}

	public float GetRebelionRisk_30()
	{
		return GetRebelionRisk_AtTime(30);
	}

	public float GetRebelionRisk_AtTime(int time)
	{
		if (realm.castle.rebelion_risk == 0)
		{
			return 0f;
		}
		float totalRebellionRisk = realm.GetTotalRebellionRisk();
		if (totalRebellionRisk >= 0f)
		{
			return 0f;
		}
		int rebels = realm.castle.population.GetRebels();
		int workers = realm.castle.population.GetWorkers();
		int num = rebels + workers;
		float num2 = Math.Max(realm.castle.game.GetMinRebelPopTime() - realm.castle.game.session_time.minutes, 0f);
		float num3 = 0.008f * (float)Math.Pow(Math.Abs(totalRebellionRisk) * def.create_rebel_pop_mod, 0.6000000238418579);
		if (realm.castle.rebelion_risk == 2)
		{
			float num4 = (float)Math.Pow(1f - 0.5f * (float)rebels / (float)num, time);
			return 1f - num4;
		}
		float num5 = float.MaxValue;
		if (num3 > 1E-06f)
		{
			num5 = (0.75f - realm.castle.population.rebelion_acc) / num3;
		}
		if (num5 - num2 > (float)time)
		{
			return 0f;
		}
		float num6 = (float)Math.Pow(1f - 0.5f * (float)rebels / (float)num, (int)Math.Floor(num5 - num2));
		return 1f - num6;
	}

	public static bool CheckConditions(Realm realm, RebelSpawnCondition.Def[] conditions, out Rebel.Def result, out RebelSpawnCondition.Def c_def)
	{
		result = null;
		c_def = null;
		if (realm == null)
		{
			return false;
		}
		if (conditions == null || conditions.Length == 0)
		{
			return false;
		}
		bool flag = !realm.pop_majority.kingdom.IsAllyOrOwn(realm.controller);
		float num = 0f;
		int num2 = -1;
		for (int i = 0; i < conditions.Length; i++)
		{
			RebelSpawnCondition.Def def = conditions[i];
			if (def.IsBase() || !def.periodic || def.spawn_chance == null || def.rebel_types == null || def.rebel_types.Count == 0)
			{
				continue;
			}
			if (!flag)
			{
				bool flag2 = true;
				for (int j = 0; j < def.rebel_types.Count; j++)
				{
					if (!def.rebel_types[j].name.Contains("Loyalist"))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					continue;
				}
			}
			float num3 = def.spawn_chance.Value(realm);
			if (num3 > num)
			{
				num = num3;
				num2 = i;
			}
		}
		if (num2 == -1)
		{
			return false;
		}
		c_def = conditions[num2];
		if (flag && c_def.name != "LoyalistsSpawnCondition" && realm.pop_majority.kingdom != realm.controller.GetKingdom() && realm.pop_majority.strength * realm.rebellionRisk.def.loyalist_spawn_chance_mod < (float)realm.game.Random(0, 100))
		{
			for (int k = 0; k < conditions.Length; k++)
			{
				if (conditions[k].name == "LoyalistsSpawnCondition")
				{
					c_def = conditions[k];
					break;
				}
			}
			result = c_def.rebel_types[0];
			return true;
		}
		int num4 = realm.game.Random(0, c_def.rebel_types.Count);
		for (int l = 0; l < c_def.rebel_types.Count; l++)
		{
			Rebel.Def def2 = c_def.rebel_types[(l + num4) % c_def.rebel_types.Count];
			if (flag || !def2.name.Contains("Loyalist"))
			{
				result = def2;
				return true;
			}
		}
		result = c_def.rebel_types[realm.game.Random(0, c_def.rebel_types.Count)];
		return true;
	}

	private bool TryReinforce()
	{
		Rebel rebel = null;
		for (int i = 0; i < this.realm.armies.Count; i++)
		{
			if (this.realm.armies[i].rebel?.rebellion != null && this.realm.armies[i].battle == null)
			{
				rebel = this.realm.armies[i].rebel;
				break;
			}
		}
		for (int j = 0; j < this.realm.logicNeighborsRestricted.Count; j++)
		{
			Realm realm = this.realm.logicNeighborsRestricted[j];
			for (int k = 0; k < realm.armies.Count; k++)
			{
				if (realm.armies[k].rebel?.rebellion != null && realm.armies[k].battle == null)
				{
					rebel = realm.armies[k].rebel;
					break;
				}
			}
		}
		if (rebel == null)
		{
			return false;
		}
		Army army = rebel.army;
		int num = base.game.Random(def.min_reinforce, def.max_reinforce);
		bool flag = false;
		for (int l = 0; l < num; l++)
		{
			if (base.game.Random(0, 100) < 50 && UpgradeUnits(army))
			{
				flag = true;
				break;
			}
			if (AddMilitia(army))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			rebel.rebellion?.AddWealth(def.reinforce_gold);
			this.realm.castle.population.RemoveVillagers(num, Population.Type.Rebel);
			rebel.NotifyListeners("reinforced");
		}
		return flag;
	}

	public bool UpgradeUnits(Army army)
	{
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit.damage > 0.5f)
			{
				unit.damage = 0f;
				return true;
			}
			if (unit.def.upgrade_to.Count == 0 && unit.damage > 0f)
			{
				unit.damage = 0f;
				return true;
			}
			Unit.Def def = null;
			int num = base.game.Random(0, unit.def.upgrade_to.Count);
			for (int j = 0; j < unit.def.upgrade_to.Count; j++)
			{
				Unit.Def def2 = base.game.defs.Get<Unit.Def>(unit.def.upgrade_to[(j + num) % unit.def.upgrade_to.Count].key);
				if (def2 != null && def2 != unit.def && realm.castle.available_units.CanBuildUnit(def2, unit.def.upgrades_to_available_units))
				{
					def = def2;
					break;
				}
			}
			if (def != null)
			{
				army.DelUnit(unit);
				army.AddUnit(def, i);
				return true;
			}
		}
		return false;
	}

	private bool AddMilitia(Army army)
	{
		int num = army.MaxUnits();
		if (army.units.Count < num)
		{
			Unit.Def unit = base.game.defs.Get<Unit.Def>("Militia_CE");
			if (!realm.castle.available_units.CanBuildUnit(unit))
			{
				unit = base.game.defs.Get<Unit.Def>("Militia_Arab");
			}
			army.AddUnit(unit);
			return true;
		}
		return false;
	}

	public float GetRebelliosWeight()
	{
		return def.rebellious_weight_base + def.rebellios_weight_per_rebel_population * (float)realm.castle.population.GetRebels() + ((realm.castle.governor != null) ? def.rebellious_weght_governed : 0f);
	}
}
