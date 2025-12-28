using System;

namespace Logic;

public class Fame : Component
{
	public class Def : Logic.Def
	{
		public string name;

		public float max = 10000f;

		public float fame_vassal_base = 10f;

		public float fame_rel_vassal_mod = 0.005f;

		public float fame_marriage_base = 0.1f;

		public float fame_marriage_queen_mod = 1.5f;

		public float fame_marriage_princess_mod = 1f;

		public float fame_rel_marriage_mod = 0.005f;

		public float TC_base = 10f;

		public float TC_mod = 2f;

		public float min_fame_victory_perc = 50f;

		public float fame_per_good_produced = 100f;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			max = field.GetFloat("max", null, max);
			fame_vassal_base = field.GetFloat("fame_vassal_base", null, fame_vassal_base);
			fame_rel_vassal_mod = field.GetFloat("fame_rel_vassal_mod", null, fame_rel_vassal_mod);
			fame_marriage_base = field.GetFloat("fame_marriage_base", null, fame_marriage_base);
			fame_marriage_queen_mod = field.GetFloat("fame_marriage_queen_mod", null, fame_marriage_queen_mod);
			fame_marriage_princess_mod = field.GetFloat("fame_marriage_princess_mod", null, fame_marriage_princess_mod);
			fame_rel_marriage_mod = field.GetFloat("fame_rel_marriage_mod", null, fame_rel_marriage_mod);
			TC_base = field.GetFloat("TC_base", null, TC_base);
			TC_mod = field.GetFloat("TC_mod", null, TC_mod);
			min_fame_victory_perc = field.GetFloat("min_fame_victory_perc", null, min_fame_victory_perc);
			fame_per_good_produced = field.GetFloat("fame_per_good_produced", null, fame_per_good_produced);
			return true;
		}
	}

	public Def def;

	private Kingdom kingdom;

	public float fame_bonus;

	public float base_fame;

	public float last_base_fame_frame_recalc = -1f;

	public float fame;

	public float realms_fame;

	public float building_fame;

	public float rankings_fame;

	public float trade_centers_fame;

	public float marriages_fame;

	public float vassals_fame;

	public float produced_goods_fame;

	public float ecumenical_patriarch_fame
	{
		get
		{
			if (kingdom == null)
			{
				return 0f;
			}
			return kingdom.GetStat(Stats.ks_fame_ecumenical_patriarch_bonus);
		}
	}

	public float caliphate_fame
	{
		get
		{
			if (kingdom == null)
			{
				return 0f;
			}
			return kingdom.GetStat(Stats.ks_fame_caliphate_bonus);
		}
	}

	public float autocephaly_fame
	{
		get
		{
			if (kingdom == null)
			{
				return 0f;
			}
			return kingdom.GetStat(Stats.ks_fame_autocephaly_bonus);
		}
	}

	public float non_orthodox_fame
	{
		get
		{
			if (kingdom == null)
			{
				return 0f;
			}
			return kingdom.GetStat(Stats.ks_fame_non_orthodox_bonus);
		}
	}

	public float traditions_fame
	{
		get
		{
			if (kingdom == null)
			{
				return 0f;
			}
			return kingdom.GetStat(Stats.ks_fame_traditions);
		}
	}

	public Fame(Kingdom kingdom)
		: base(kingdom)
	{
		this.kingdom = kingdom;
		def = base.game.defs.GetBase<Def>();
	}

	public override void OnStart()
	{
		base.OnStart();
	}

	public void StartUpdating()
	{
		UpdateInBatch(base.game.update_5sec);
	}

	public override void OnUpdate()
	{
		CalcFame();
	}

	public float GetMaxFame()
	{
		return def.max;
	}

	public float GetMinVicotryFame()
	{
		return def.min_fame_victory_perc / 100f * def.max;
	}

	public float GetBaseFame()
	{
		return base_fame;
	}

	public float GetFame()
	{
		return fame;
	}

	private float CalcBaseFame()
	{
		if (last_base_fame_frame_recalc == (float)base.game.frame)
		{
			return base_fame;
		}
		kingdom.UpdateRealmTags();
		building_fame = 0f;
		realms_fame = 0f;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			realms_fame += realm.fame;
			if (realm?.castle?.governor != null)
			{
				realms_fame += realm.GetStat(Stats.rs_fame_town_bonus);
				building_fame += realm.GetStat(Stats.rs_fame_buildings_bonus);
			}
		}
		rankings_fame = kingdom.CalcRankingCategoriesScore();
		produced_goods_fame = (float)kingdom.goods_produced.Count * def.fame_per_good_produced;
		trade_centers_fame = 0f;
		for (int j = 0; j < base.game.economy.tradeCenterRealms.Count; j++)
		{
			Realm realm2 = base.game.economy.tradeCenterRealms[j];
			if (realm2.kingdom_id == kingdom.id)
			{
				trade_centers_fame += def.TC_base + realm2.GetGoldFromTradeCenter(recalcRealms: true) * def.TC_mod;
			}
		}
		trade_centers_fame = (float)Math.Ceiling(trade_centers_fame / 5f) * 5f;
		base_fame = fame_bonus;
		base_fame += kingdom.GetStat(Stats.ks_fame_bonus);
		base_fame += building_fame;
		base_fame += realms_fame;
		base_fame += rankings_fame;
		base_fame += produced_goods_fame;
		base_fame += trade_centers_fame;
		last_base_fame_frame_recalc = base.game.frame;
		return base_fame;
	}

	public float CalcFame()
	{
		vassals_fame = 0f;
		for (int i = 0; i < this.kingdom.vassalStates.Count; i++)
		{
			Kingdom kingdom = this.kingdom.vassalStates[i];
			kingdom.fameObj.CalcBaseFame();
			vassals_fame += kingdom.base_fame * (def.fame_vassal_base + (this.kingdom.GetRelationship(kingdom) + RelationUtils.Def.maxRelationship) * def.fame_rel_vassal_mod);
		}
		marriages_fame = 0f;
		for (int j = 0; j < this.kingdom.marriages.Count; j++)
		{
			Marriage marriage = this.kingdom.marriages[j];
			if (marriage == null)
			{
				Warning("Null marriage " + j + " for kingdom " + this.kingdom);
				continue;
			}
			Kingdom otherKingdom = marriage.GetOtherKingdom(this.kingdom);
			if (otherKingdom == null || !this.kingdom.GetRoyalMarriage(otherKingdom) || marriage.kingdom_wife != this.kingdom || marriage.kingdom_husband == marriage.kingdom_wife)
			{
				continue;
			}
			bool flag = false;
			for (int k = 0; k < this.kingdom.marriages.Count; k++)
			{
				if (k == j)
				{
					continue;
				}
				Marriage marriage2 = this.kingdom.marriages[k];
				if (marriage2 == null)
				{
					Warning("Null marriage " + k + " for kingdom " + this.kingdom);
				}
				else if (marriage2.kingdom_wife == this.kingdom && marriage2.kingdom_husband == otherKingdom)
				{
					if (marriage2.wife.IsQueen() && !marriage.wife.IsQueen())
					{
						flag = true;
						break;
					}
					if (j < k && marriage2.wife.title == marriage.wife.title)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag && otherKingdom != null)
			{
				otherKingdom.fameObj.CalcBaseFame();
				marriages_fame += otherKingdom.base_fame * def.fame_marriage_base * (marriage.wife.IsQueen() ? def.fame_marriage_queen_mod : def.fame_marriage_princess_mod);
			}
		}
		vassals_fame = (float)Math.Ceiling(vassals_fame / 5f) * 5f;
		marriages_fame = (float)Math.Ceiling(marriages_fame / 5f) * 5f;
		fame = CalcBaseFame();
		fame += vassals_fame;
		fame += marriages_fame;
		return fame;
	}

	public float GetModifierValue(string mod_name, Vars vars = null)
	{
		return def.field.GetFloat(mod_name, vars);
	}

	public bool AddFameModifier(string mod_name, Vars vars = null, float valueMultiplier = 1f, bool send_state = true)
	{
		if (!kingdom.IsAuthority())
		{
			return false;
		}
		float num = GetModifierValue(mod_name, vars) * valueMultiplier;
		if (num > 0f)
		{
			AddFameValue(num);
		}
		else
		{
			SubFameValue(0f - num);
		}
		return true;
	}

	public bool AddFame(float val, bool send_state = true)
	{
		if (val < 0f)
		{
			return SubFameValue(0f - val);
		}
		return AddFameValue(val);
	}

	private bool AddFameValue(float val, bool send_state = true)
	{
		if (!kingdom.IsAuthority())
		{
			return false;
		}
		fame_bonus += val;
		if (send_state)
		{
			kingdom.SendState<Kingdom.FameState>();
		}
		return true;
	}

	private bool SubFameValue(float val, bool send_state = true)
	{
		if (!kingdom.IsAuthority())
		{
			return false;
		}
		fame_bonus -= val;
		if (send_state)
		{
			kingdom.SendState<Kingdom.FameState>();
		}
		return true;
	}
}
