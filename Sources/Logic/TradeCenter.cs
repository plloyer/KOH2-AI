using System.Collections.Generic;

namespace Logic;

public class TradeCenter : Component, IVars
{
	public class Def : Logic.Def
	{
		public int wave_strength;

		public int max_move_distance;

		public float income_per_province;

		public float income_per_good;

		public float income_commerse_percent;

		public int min;

		public int max;

		public int min_realm_distance_between;

		public float tc_spread_appeal_fade_perc = 90f;

		public float tc_spread_appeal_fade_const = 5f;

		public float tc_spread_appeal_fade_sea_const = 20f;

		public float tc_spread_relation_base = 1000f;

		public float tc_spread_relation_total = 2000f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			wave_strength = field.GetInt("wave_strength");
			max_move_distance = field.GetInt("max_move_distance");
			income_per_province = field.GetFloat("income_per_province");
			income_per_good = field.GetFloat("income_per_good");
			income_commerse_percent = field.GetFloat("income_commerse_percent");
			min = field.GetInt("min");
			max = field.GetInt("max");
			min_realm_distance_between = field.GetInt("min_realm_distance_between");
			tc_spread_appeal_fade_perc = field.GetFloat("tc_spread_appeal_fade_perc");
			tc_spread_appeal_fade_const = field.GetFloat("tc_spread_appeal_fade_const");
			tc_spread_appeal_fade_sea_const = field.GetFloat("tc_spread_appeal_fade_sea_const");
			tc_spread_relation_base = field.GetFloat("tc_spread_relation_base");
			tc_spread_relation_total = field.GetFloat("tc_spread_relation_total");
			return true;
		}
	}

	public Def def;

	public List<Realm> belongingRealms = new List<Realm>();

	public float incomeTaxedGoldFromOwnRealm;

	public float incomeUntaxedGold;

	public float incomeUntaxedGoldGoods;

	public float incomeUntaxedGoldCommerse;

	public bool income_valid;

	public Realm realm
	{
		get
		{
			return obj as Realm;
		}
		set
		{
			obj = value;
		}
	}

	public TradeCenter(Realm r)
		: base(r)
	{
		def = base.game.defs.Get<Def>("TradeCenter");
		r.NotifyListeners("became_trade_center");
	}

	public void DelBelongingRealm(Realm r)
	{
		belongingRealms.Remove(r);
		r.tradeCenter = null;
		r.tradeCenterDistance = -1;
		r.NotifyListeners("trade_center_changed");
	}

	public void AddBelongingRealm(Realm r, int distance)
	{
		belongingRealms.Add(r);
		r.tradeCenter = this;
		r.tradeCenterDistance = distance;
		r.NotifyListeners("trade_center_changed");
	}

	public void RecalcIncome(bool instant = false, bool forced = false)
	{
		if (!instant)
		{
			income_valid = false;
		}
		else if (!income_valid || forced)
		{
			income_valid = true;
			RecalcIncomeFromOwnRealm();
			RecalcGoldIncomeFromInfluencedRealms();
			income_valid = true;
		}
	}

	public float GetGoldIncome(bool recalcRealms)
	{
		return 0f + GetGoldIncomeFromOwnRealm(recalcRealms) + GetGoldIncomeFromInfluencedRealms(recalcRealms);
	}

	public float RecalcIncomeFromOwnRealm()
	{
		incomeTaxedGoldFromOwnRealm = 0f;
		realm.RecalcIncomes();
		foreach (Settlement settlement in realm.settlements)
		{
			incomeTaxedGoldFromOwnRealm += settlement.production_from_Trade_center[ResourceType.Gold];
		}
		return incomeTaxedGoldFromOwnRealm;
	}

	public float GetGoldIncomeFromOwnRealm(bool recalcRealm)
	{
		RecalcIncome(instant: true);
		return incomeTaxedGoldFromOwnRealm;
	}

	public float RecalcGoldIncomeFromInfluencedRealms()
	{
		incomeUntaxedGold = 0f;
		incomeUntaxedGoldCommerse = 0f;
		incomeUntaxedGoldGoods = 0f;
		for (int i = 0; i < belongingRealms.Count; i++)
		{
			Realm realm = belongingRealms[i];
			if (realm.IsInfluencedByTradeCenter() && !this.realm.IsEnemy(realm))
			{
				realm.RecalcIncomes();
				incomeUntaxedGold += def.income_per_province;
				incomeUntaxedGoldGoods += (float)realm.goods_produced.Count * def.income_per_good;
				incomeUntaxedGoldCommerse += realm.GetCommerce();
			}
		}
		float num = 1f + this.realm.GetKingdom().GetStat(Stats.ks_gold_from_own_TCs_perc) / 100f;
		incomeUntaxedGold *= num;
		incomeUntaxedGoldGoods *= num;
		incomeUntaxedGoldCommerse *= num;
		incomeUntaxedGoldCommerse *= def.income_commerse_percent / 100f;
		return incomeUntaxedGold + incomeUntaxedGoldGoods + incomeUntaxedGoldCommerse;
	}

	public float GetGoldIncomeFromInfluencedRealms(bool recalcRealms)
	{
		RecalcIncome(instant: true);
		return incomeUntaxedGold + incomeUntaxedGoldGoods + incomeUntaxedGoldCommerse;
	}

	public float GetInfluencedRealmsGoldIncome(bool recalcRealms)
	{
		float num = 0f;
		for (int i = 0; i < belongingRealms.Count; i++)
		{
			Realm realm = belongingRealms[i];
			if (realm.IsInfluencedByTradeCenter() && !this.realm.IsEnemy(realm))
			{
				if (recalcRealms)
				{
					realm.RecalcIncomes();
				}
				num += realm.incomeFromTradeCenterInfluence[ResourceType.Gold];
			}
		}
		return num;
	}

	public float GetTotalInfluence()
	{
		return GetGoldIncome(recalcRealms: true) + GetInfluencedRealmsGoldIncome(recalcRealms: false);
	}

	public void Clean()
	{
		realm.GetKingdom();
		for (int i = 0; i < realm.merchants.Count; i++)
		{
			Character character = realm.merchants[i];
			if (character != null && character.IsMerchant() && character.mission_realm == realm)
			{
				character.Recall();
			}
		}
		CleanRealms();
	}

	public void CleanRealms()
	{
		while (belongingRealms.Count > 0)
		{
			DelBelongingRealm(belongingRealms[0]);
		}
	}

	public void MoveTo(Realm newRealm)
	{
		Realm realm = this.realm;
		if (newRealm == null || !newRealm.IsValid() || newRealm.IsSeaRealm() || newRealm.castle == null)
		{
			Game.Log("Chaning the realm of a trade center failed - invalid realm (from " + this.realm.name + " to " + newRealm.name + ")", Game.LogType.Error);
			return;
		}
		newRealm.tradeCenter?.DelBelongingRealm(newRealm);
		int index = base.game.economy.tradeCenterRealms.IndexOf(this.realm);
		if (!base.game.economy.CanHaveTradeCenter(newRealm, this.realm))
		{
			Game.Log("Chaning the realm of a trade center failed - bad location (from " + this.realm.name + " to " + newRealm.name + ")", Game.LogType.Error);
		}
		else
		{
			this.realm.NotifyListeners("trade_center_moved", newRealm);
			Clean();
			realm.RemoveComponent(this);
			this.realm = newRealm;
			newRealm.AddComponent(this);
			base.game.economy.tradeCenterRealms[index] = newRealm;
			base.game.economy.RefreshTradeCenters();
			base.game.SendState<Game.TradeCentersState>();
			newRealm.NotifyListeners("became_trade_center");
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"IncomeUntaxedGoldTotal" => incomeUntaxedGold + incomeUntaxedGoldGoods + incomeUntaxedGoldCommerse, 
			"IncomeUntaxedGoldProvinces" => incomeUntaxedGold, 
			"IncomeUntaxedGoldGoods" => incomeUntaxedGoldGoods, 
			"IncomeUntaxedGoldCommerse" => incomeUntaxedGoldCommerse, 
			_ => Value.Unknown, 
		};
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("realm", realm);
		if (belongingRealms != null && belongingRealms.Count > 0)
		{
			dump.OpenSection("belonging realms");
			for (int i = 0; i < belongingRealms.Count; i++)
			{
				dump.Append(belongingRealms[i]?.name, belongingRealms[i]);
			}
			dump.CloseSection("belonging realms");
		}
		dump.Append("incomeUntaxedGold", incomeUntaxedGold.ToString());
		dump.Append("incomeUntaxedGoldGoods", incomeUntaxedGoldGoods.ToString());
		dump.Append("incomeUntaxedGoldCommerse", incomeUntaxedGoldCommerse.ToString());
		dump.Append("incomeTaxedGoldFromOwnRealm", incomeTaxedGoldFromOwnRealm.ToString());
		base.DumpInnerState(dump, verbosity);
	}
}
