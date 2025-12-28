namespace Logic;

public static class RealmCost
{
	public class Def : Logic.Def
	{
		public float base_cost = 1000f;

		public float per_settlement = 1000f;

		public float per_province_feature = 2000f;

		public float per_province_feature_we_have_elsewhere = 1000f;

		public float trade_center = 5000f;

		public float per_trade_center_zone_realm_cost = 100f;

		public float religious_center_from_our_religion = 20000f;

		public float religious_center_from_other_religion = 5000f;

		public float per_siege_defense = 10f;

		public float population_is_loyal_to_us = 3000f;

		public float population_has_our_religion = 2000f;

		public float building_cost_mult = 0.25f;

		public float kingdom_to_size_value = 20000f;

		public float multiplier = 1f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			base_cost = field.GetFloat("base", null, base_cost);
			per_settlement = field.GetFloat("per_settlement", null, per_settlement);
			per_province_feature = field.GetFloat("per_province_feature", null, per_province_feature);
			per_province_feature_we_have_elsewhere = field.GetFloat("per_province_feature_we_have_elsewhere", null, per_province_feature_we_have_elsewhere);
			trade_center = field.GetFloat("trade_center", null, trade_center);
			per_trade_center_zone_realm_cost = field.GetFloat("per_trade_center_zone_realm", null, per_trade_center_zone_realm_cost);
			religious_center_from_our_religion = field.GetFloat("religious_center_from_our_religion", null, religious_center_from_our_religion);
			religious_center_from_other_religion = field.GetFloat("religious_center_from_other_religion", null, religious_center_from_other_religion);
			per_siege_defense = field.GetFloat("per_siege_defense", null, per_siege_defense);
			population_is_loyal_to_us = field.GetFloat("population_is_loyal_to_us", null, population_is_loyal_to_us);
			population_has_our_religion = field.GetFloat("population_has_our_religion", null, population_has_our_religion);
			building_cost_mult = field.GetFloat("building_cost_mult", null, building_cost_mult);
			kingdom_to_size_value = field.GetFloat("kingdom_to_size_value", null, kingdom_to_size_value);
			multiplier = field.GetFloat("multiplier", null, multiplier);
			return base.Load(game);
		}
	}

	public static Def def;

	public static float CalcSettlementsCost(Realm r, Kingdom forKingdom)
	{
		float num = 0f;
		for (int i = 0; i < r.settlements.Count; i++)
		{
			Settlement settlement = r.settlements[i];
			if (settlement.IsActiveSettlement() && !(settlement is Castle))
			{
				num += def.per_settlement;
			}
		}
		return num;
	}

	public static float CalcProvinceFeaturesCost(Realm r, Kingdom forKingdom)
	{
		float num = 0f;
		for (int i = 0; i < r.features.Count; i++)
		{
			string item = r.features[i];
			bool flag = false;
			for (int j = 0; j < forKingdom.realms.Count; j++)
			{
				if (forKingdom.realms[j].features.Contains(item))
				{
					flag = true;
					break;
				}
			}
			num = ((!flag) ? (num + def.per_province_feature) : (num + def.per_province_feature_we_have_elsewhere));
		}
		return num;
	}

	public static float CalcTradeCenterCost(Realm r, Kingdom forKingdom)
	{
		if (!r.IsTradeCenter())
		{
			return 0f;
		}
		float trade_center = def.trade_center;
		float num = (float)(r.tradeCenter.belongingRealms.Count - 1) * def.per_trade_center_zone_realm_cost;
		return trade_center + num;
	}

	public static float CalcReligiousCenterCost(Realm r, Kingdom forKingdom)
	{
		float result = 0f;
		Religions religions = r.game.religions;
		if (religions.catholic.hq_realm == r || religions.catholic.holy_lands_realm == r)
		{
			result = ((!forKingdom.is_catholic) ? def.religious_center_from_other_religion : def.religious_center_from_our_religion);
		}
		else if (religions.orthodox.hq_realm == r)
		{
			result = ((!forKingdom.is_orthodox) ? def.religious_center_from_other_religion : def.religious_center_from_our_religion);
		}
		else if (religions.sunni.hq_realm == r || religions.sunni.holy_lands_realms.Contains(r))
		{
			result = ((!forKingdom.is_sunni) ? def.religious_center_from_other_religion : def.religious_center_from_our_religion);
		}
		else if (religions.shia.hq_realm == r || religions.shia.holy_lands_realm == r)
		{
			result = ((!forKingdom.is_shia) ? def.religious_center_from_other_religion : def.religious_center_from_our_religion);
		}
		else if (religions.pagan.hq_realm == r)
		{
			result = ((!forKingdom.is_pagan) ? def.religious_center_from_other_religion : def.religious_center_from_our_religion);
		}
		return result;
	}

	public static float CalcSiegeDefense(Realm r, Kingdom forKingdom)
	{
		return r.castle.GetCurrentSiegeDefence() * def.per_siege_defense;
	}

	public static float CalcPopulationCost(Realm r, Kingdom forKingdom)
	{
		float result = 0f;
		if (r.pop_majority.kingdom == forKingdom)
		{
			result = def.population_is_loyal_to_us;
		}
		else if (r.pop_majority.kingdom.religion == forKingdom.religion)
		{
			result = def.population_has_our_religion;
		}
		return result;
	}

	public static float CalcBuildingsCost(Realm r, Kingdom forKingdom)
	{
		if (r.castle.buildings == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < r.castle.buildings.Count; i++)
		{
			Building building = r.castle.buildings[i];
			if (building != null && building.IsBuilt())
			{
				num += building.def.GetCost(r, forKingdom).Get(ResourceType.Gold);
			}
		}
		return num * def.building_cost_mult;
	}

	public static float CalcKingdomFactor(Realm r, Kingdom forKingdom)
	{
		return def.kingdom_to_size_value / (float)forKingdom.realms.Count;
	}

	public static float CalcRealmCost(Realm r, Kingdom forKingdom = null)
	{
		if (r?.castle == null)
		{
			return 0f;
		}
		if (forKingdom == null)
		{
			forKingdom = r.GetKingdom();
			if (forKingdom == null)
			{
				return 0f;
			}
		}
		if (def == null)
		{
			def = r.game.defs.GetBase<Def>();
		}
		float num = CalcSettlementsCost(r, forKingdom);
		float num2 = CalcProvinceFeaturesCost(r, forKingdom);
		float num3 = CalcTradeCenterCost(r, forKingdom);
		float num4 = CalcReligiousCenterCost(r, forKingdom);
		float num5 = CalcSiegeDefense(r, forKingdom);
		float num6 = CalcPopulationCost(r, forKingdom);
		float num7 = CalcBuildingsCost(r, forKingdom);
		float num8 = CalcKingdomFactor(r, forKingdom);
		return (def.base_cost + num + num2 + num3 + num4 + num5 + num6 + num7 + num8) * def.multiplier;
	}

	public static string Dump(Realm r, Kingdom forKingdom)
	{
		if (r?.castle == null)
		{
			return "";
		}
		if (forKingdom == null)
		{
			forKingdom = r.GetKingdom();
			if (forKingdom == null)
			{
				return "";
			}
		}
		if (def == null)
		{
			def = r.game.defs.GetBase<Def>();
		}
		float num = CalcSettlementsCost(r, forKingdom);
		float num2 = CalcProvinceFeaturesCost(r, forKingdom);
		float num3 = CalcTradeCenterCost(r, forKingdom);
		float num4 = CalcReligiousCenterCost(r, forKingdom);
		float num5 = CalcSiegeDefense(r, forKingdom);
		float num6 = CalcPopulationCost(r, forKingdom);
		float num7 = CalcBuildingsCost(r, forKingdom);
		float num8 = CalcKingdomFactor(r, forKingdom);
		float num9 = def.base_cost + num + num2 + num3 + num4 + num5 + num6 + num7 + num8;
		num9 *= def.multiplier;
		string text = "";
		text = text + "Cost of " + r.name + "(" + r.castle.name + "):";
		text = text + "\nBase - " + def.base_cost;
		text = text + "\nSettlements - " + num;
		text = text + "\nFeatures - " + num2;
		text = text + "\nTradeCenter - " + num3;
		text = text + "\nReligiousCenters - " + num4;
		text = text + "\nSiegeDefense - " + num5;
		text = text + "\nPopulation - " + num6;
		text = text + "\nBuildings - " + num7;
		text = text + "\nKingdomFactor - " + num8;
		text = text + "\nMultiplier - x" + def.multiplier;
		return text + "\nTotal - " + num9;
	}
}
