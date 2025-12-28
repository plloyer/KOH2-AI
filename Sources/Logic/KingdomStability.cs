using System;

namespace Logic;

public class KingdomStability : Component
{
	public class Def : Logic.Def
	{
		public float stability_increase_from_CA = 20f;

		public float stability_decrease_from_CA = 50f;

		public float stability_differences_added_provinces = 7f;

		public float stability_differences_min_perc = 30f;

		public float stability_differences_religion_max = 50f;

		public float stability_differences_culture_max = 25f;

		public float stability_pagan_tolerance_modifier = 0.5f;

		public int max_rebels_per_kingdom = 15;

		public int max_rebels_per_game = 300;

		public int max_rebellions_per_game = 20;

		public int min_provinces_per_rebellion = 3;

		public int max_rebellions_per_AI_kingdom = 3;

		public DT.Field max_rebellions_for_AI_kingdom_formula;

		public DT.Field max_rebellions_for_Player_kingdom_formula;

		public float stability_tooltip_threshold = -10f;

		public float rebel_risk_base_period = 120f;

		public float rebel_risk_variable_period = 60f;

		public float max_risk_hunger = -10f;

		public float risk_global_leader_level = -1f;

		public float risk_global_rebel = -1f;

		public override bool Load(Game game)
		{
			stability_increase_from_CA = base.field.GetFloat("stability_increase_from_CA", null, stability_increase_from_CA);
			stability_decrease_from_CA = base.field.GetFloat("stability_decrease_from_CA", null, stability_decrease_from_CA);
			stability_differences_added_provinces = base.field.GetFloat("stability_differences_added_provinces", null, stability_differences_added_provinces);
			stability_differences_min_perc = base.field.GetFloat("stability_differences_min_perc", null, stability_differences_min_perc);
			stability_differences_religion_max = base.field.GetFloat("stability_differences_religion_max", null, stability_differences_religion_max);
			stability_differences_culture_max = base.field.GetFloat("stability_differences_culture_max", null, stability_differences_culture_max);
			stability_pagan_tolerance_modifier = base.field.GetFloat("stability_pagan_tolerance_modifier", null, stability_pagan_tolerance_modifier);
			max_rebels_per_kingdom = base.field.GetInt("max_rebels_per_kingdom", null, max_rebels_per_kingdom);
			max_rebels_per_game = base.field.GetInt("max_rebels_per_game", null, max_rebels_per_game);
			max_rebellions_per_game = base.field.GetInt("max_rebellions_per_game", null, max_rebellions_per_game);
			min_provinces_per_rebellion = base.field.GetInt("min_provinces_per_rebellion", null, min_provinces_per_rebellion);
			max_rebellions_per_AI_kingdom = base.field.GetInt("max_rebellions_per_AI_kingdom", null, max_rebellions_per_AI_kingdom);
			max_rebellions_for_AI_kingdom_formula = base.field.FindChild("max_rebellions_for_AI_kingdom_formula");
			max_rebellions_for_Player_kingdom_formula = base.field.FindChild("max_rebellions_for_Player_kingdom_formula");
			stability_tooltip_threshold = base.field.GetFloat("stability_tooltip_threshold", null, stability_tooltip_threshold);
			rebel_risk_base_period = base.field.GetFloat("rebel_risk_base_period", null, rebel_risk_base_period);
			rebel_risk_variable_period = base.field.GetFloat("rebel_risk_variable_period", null, rebel_risk_variable_period);
			max_risk_hunger = base.field.GetFloat("max_risk_hunger ", null, max_risk_hunger);
			risk_global_leader_level = base.field.GetFloat("risk_global_leader_level", null, risk_global_leader_level);
			risk_global_rebel = base.field.GetFloat("risk_global_rebel", null, risk_global_rebel);
			return base.Load(game);
		}
	}

	public Def def;

	public Stat kingdom_stat;

	private Kingdom kingdom;

	public float value;

	private float CA_mod = 1f;

	private int CA_category = -1;

	private RebellionRisk.CategoryInfo[] rebellion_risk_per_category;

	private ValueCache has_factors_cache;

	public KingdomStability(Kingdom kingdom)
		: base(kingdom)
	{
		def = base.game.defs.Get<Def>("KingdomStability");
		this.kingdom = kingdom;
		if (kingdom.stats.stats.Count != 0)
		{
			kingdom_stat = kingdom.stats.Find(Stats.ks_stability);
		}
	}

	public int MaxRebellionsPerKingdom()
	{
		if (kingdom.is_player)
		{
			return def.max_rebellions_for_Player_kingdom_formula.Int(kingdom);
		}
		return def.max_rebellions_for_AI_kingdom_formula.Int(kingdom);
	}

	public bool CanHaveMoreRebellions()
	{
		int num = MaxRebellionsPerKingdom();
		int count = kingdom.rebellions.Count;
		if (num - count <= 0)
		{
			return false;
		}
		int num2 = 0;
		base.game.num_objects_by_type.TryGetValue(typeof(Rebellion), out num2);
		return def.max_rebellions_per_game - num2 > 0;
	}

	public void Build()
	{
		if (kingdom_stat == null || rebellion_risk_per_category != null)
		{
			return;
		}
		rebellion_risk_per_category = RebellionRisk.BuildInfo(kingdom_stat, base.game, global: true);
		for (int i = 0; i < rebellion_risk_per_category.Length; i++)
		{
			if (rebellion_risk_per_category[i].stat_type == "crown_authority")
			{
				CA_category = i;
				break;
			}
		}
		SpecialEvent(think_rebel: false);
		UpdateAfter(def.rebel_risk_base_period + base.game.Random(0f, def.rebel_risk_variable_period));
	}

	public void Recalc()
	{
		if (kingdom_stat == null || rebellion_risk_per_category == null || !kingdom.IsAuthority())
		{
			return;
		}
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		if (crownAuthority == null)
		{
			return;
		}
		value = kingdom_stat.CalcValue();
		for (int i = 0; i < rebellion_risk_per_category.Length; i++)
		{
			RebellionRisk.CategoryInfo categoryInfo = rebellion_risk_per_category[i];
			if (categoryInfo.stat_mod != null)
			{
				categoryInfo.value = categoryInfo.stat_mod.value;
			}
		}
		if (CA_category != -1)
		{
			CA_mod = 1f;
			int num = crownAuthority.GetValue();
			if (num > 0)
			{
				if (value < 0f)
				{
					CA_mod = 1f - Game.map_clamp(num, 0f, 5f, 0f, def.stability_decrease_from_CA) / 100f;
				}
				else
				{
					CA_mod = 1f + Game.map_clamp(num, 0f, 5f, 0f, def.stability_increase_from_CA) / 100f;
				}
			}
			else if (value < 0f)
			{
				CA_mod = 1f + Game.map_clamp(-num, 0f, 5f, 0f, def.stability_increase_from_CA) / 100f;
			}
			else
			{
				CA_mod = 1f - Game.map_clamp(-num, 0f, 5f, 0f, def.stability_decrease_from_CA) / 100f;
			}
			float num2 = (float)Math.Round(value * CA_mod);
			float num3 = num2 - value;
			if (Math.Abs(num3) >= 1f)
			{
				value = num2;
				SetCategory(GetStability(CA_category) + num3, CA_category);
			}
		}
		kingdom.SendState<Kingdom.StabilityState>();
	}

	public float GetStability()
	{
		return value;
	}

	public float GetCAMod()
	{
		if (CA_category != -1)
		{
			return GetStability(CA_category);
		}
		return 1f;
	}

	public float GetStability(int category)
	{
		return (float)Math.Round(rebellion_risk_per_category[category].value);
	}

	public float GetStability(string category)
	{
		for (int i = 0; i < rebellion_risk_per_category.Length; i++)
		{
			if (rebellion_risk_per_category[i].stat_type == category)
			{
				return GetStability(i);
			}
		}
		return 0f;
	}

	public bool HasFactors()
	{
		if (base.game == null)
		{
			return false;
		}
		if (has_factors_cache == null)
		{
			has_factors_cache = new ValueCache(delegate
			{
				for (int i = 0; i < rebellion_risk_per_category.Length; i++)
				{
					if ((float)Math.Round(rebellion_risk_per_category[i].value) != 0f)
					{
						return true;
					}
				}
				return false;
			}, base.game);
		}
		return has_factors_cache.GetValue();
	}

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

	public void SpecialEvent(bool think_rebel)
	{
		if (!kingdom.IsAuthority())
		{
			return;
		}
		Recalc();
		int num = 0;
		int maxRebelsToSpawn = kingdom.GetMaxRebelsToSpawn();
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			if (kingdom.realms[i].rebellionRisk?.Recalc(think_rebel, num < maxRebelsToSpawn) == "rebel_spawned")
			{
				num++;
			}
		}
	}

	public override void OnUpdate()
	{
		if (kingdom == null || !kingdom.IsValid())
		{
			Destroy();
			return;
		}
		UpdateAfter(def.rebel_risk_base_period + base.game.Random(0f, def.rebel_risk_variable_period));
		if (!kingdom.IsDefeated())
		{
			SpecialEvent(think_rebel: false);
		}
	}

	public int GetRebelCountKingdom()
	{
		int num = 0;
		foreach (Army item in kingdom.armies_in)
		{
			if (item.rebel != null)
			{
				num++;
			}
		}
		return num;
	}
}
