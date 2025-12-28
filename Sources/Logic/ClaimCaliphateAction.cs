using System;
using System.Collections.Generic;

namespace Logic;

public class ClaimCaliphateAction : Action
{
	public ClaimCaliphateAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ClaimCaliphateAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "not_a_kindom";
		}
		if (!kingdom.is_muslim)
		{
			return "not_muslim";
		}
		if (kingdom.IsCaliphate())
		{
			return "already_caliphate";
		}
		if (CheckReligionAction())
		{
			return "_religion_changing";
		}
		return base.Validate(quick_out);
	}

	private bool CheckReligionAction()
	{
		Action action = own_kingdom.actions.Find("BecomeCatholicAction");
		if (action != null && action.state == State.Preparing)
		{
			return true;
		}
		Action action2 = own_kingdom.actions.Find("BecomeOrthodoxAction");
		if (action2 != null && action2.state == State.Preparing)
		{
			return true;
		}
		Action action3 = own_kingdom.actions.Find("BecomeSunniAction");
		if (action3 != null && action3.state == State.Preparing)
		{
			return true;
		}
		Action action4 = own_kingdom.actions.Find("BecomeShiaAction");
		if (action4 != null && action4.state == State.Preparing)
		{
			return true;
		}
		return false;
	}

	private bool CheckRealms()
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		int count = kingdom.realms.Count;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (kingdom.realms[i].religion.def.muslim)
			{
				num++;
			}
		}
		if (num <= count / 2)
		{
			return false;
		}
		return true;
	}

	public override bool ValidateRequirement(DT.Field rf)
	{
		if (rf.key == "more_than_half_islamic_realms")
		{
			return CheckRealms();
		}
		return base.ValidateRequirement(rf);
	}

	public override void Run()
	{
		Kingdom kingdom = own_kingdom;
		kingdom.caliphate = true;
		Religion.RefreshModifiers(kingdom);
		kingdom.SendState<Kingdom.ReligionState>();
		kingdom.FireEvent("religion_changed", null);
		base.game.religions.FireEvent("caliphate_claimed", kingdom);
		own_kingdom.NotifyListeners("claimed_caliphate");
		base.Run();
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "rel_change_with_muslim")
		{
			own_kingdom.SubResources(KingdomAI.Expense.Category.Religion, GetCost());
			Value value = outcome.field.value;
			foreach (Kingdom kingdom in own_kingdom.game.kingdoms)
			{
				if (!kingdom.IsDefeated() && kingdom.is_muslim && kingdom != own_kingdom)
				{
					own_kingdom.AddRelationModifier(kingdom, "rel_change_with_muslim", null, value);
				}
			}
			return true;
		}
		return base.ApplyOutcome(outcome);
	}

	public static int sf_our_muslim_vassals(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		if (num == 0)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < kingdom.vassalStates.Count; i++)
		{
			if (kingdom.vassalStates[i].is_muslim)
			{
				num2++;
			}
		}
		return num2 * num;
	}

	public static int sf_have_theology_tradition(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Tradition.Def def = kingdom.game?.defs?.Get<Tradition.Def>("TheologyTradition");
		if (def == null)
		{
			return 0;
		}
		if (!kingdom.HasTradition(def))
		{
			return 0;
		}
		return factor.field.Int(sf.vars);
	}

	public static int sf_we_are_great_power(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		if (!kingdom.game.great_powers.TopKingdoms().Contains(kingdom))
		{
			return 0;
		}
		return factor.field.Int(sf.vars);
	}

	public static int sf_we_have_muslim_holy_lands(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = 0;
		List<Realm> list = kingdom.game?.religions?.sunni?.holy_lands_realms;
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].GetKingdom() == kingdom)
				{
					num++;
				}
			}
		}
		if ((kingdom.game?.religions?.shia?.holy_lands_realm)?.GetKingdom() == kingdom)
		{
			num++;
		}
		int num2 = factor.field.Int(sf.vars);
		return num * num2;
	}

	public static int sf_islamic_provinces(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		if (num == 0)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			if (kingdom.realms[i].religion.def.muslim)
			{
				num2++;
			}
		}
		return num2 * num;
	}

	public static int sf_no_caliphate_exists(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		for (int i = 0; i < sf.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = sf.game.kingdoms[i];
			if (kingdom != null && kingdom.IsCaliphate() && !kingdom.IsDefeated())
			{
				return 0;
			}
		}
		return factor.field.Int(sf.vars);
	}

	public static int sf_islamic_relations(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < sf.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = sf.game.kingdoms[i];
			if (kingdom2 == null || kingdom2 == kingdom || !kingdom2.is_muslim || kingdom2.IsDefeated() || kingdom2.IsCaliphate())
			{
				continue;
			}
			num2++;
			int count = kingdom2.realms.Count;
			int num3 = 0;
			for (int j = 0; j < kingdom2.realms.Count; j++)
			{
				if (kingdom2.realms[j].is_muslim)
				{
					num3++;
				}
			}
			float relationship = kingdom.GetRelationship(kingdom2);
			float influenceIn = kingdom.GetInfluenceIn(kingdom2);
			float num4 = 0f;
			if (kingdom2.sovereignState == kingdom)
			{
				float num5 = factor.field.GetInt("vassal_min");
				float num6 = (float)factor.field.GetInt("vassal_max") - num5;
				num4 = num5 + num6 * (float)num3 / (float)count;
			}
			float val = relationship + influenceIn + num4;
			val = Math.Min(val, factor.field.GetInt("kingdom_max"));
			val *= (float)(num3 / count);
			float val2 = factor.field.GetInt("min");
			float val3 = factor.field.GetInt("max");
			val = Math.Max(val, val2);
			val = Math.Min(val, val3);
			num += val;
		}
		num /= (float)num2;
		return (int)num;
	}

	public static int sf_existing_caliphates(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		int num = factor.field.Int(sf.vars);
		if (num == 0)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < sf.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = sf.game.kingdoms[i];
			if (kingdom != null && kingdom.IsCaliphate() && !kingdom.IsDefeated())
			{
				num2++;
			}
		}
		return num2 * num;
	}

	public static int sf_non_islamic_provinces(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int def_val = factor.field.Int(sf.vars);
		int num = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			if (!realm.religion.def.muslim)
			{
				string name = realm.religion.name;
				int num2 = factor.field.GetInt(name, sf.vars, def_val);
				num += num2;
			}
		}
		return num;
	}

	public static int sf_shia(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		if (!kingdom.is_shia)
		{
			return 0;
		}
		return factor.field.Int(sf.vars);
	}

	public static int sf_positive_relationship_with_muslim_kingdoms(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < kingdom.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = kingdom.game.kingdoms[i];
			if (!kingdom2.IsDefeated() && kingdom2.is_muslim && kingdom2 != kingdom)
			{
				float relationship = kingdom2.GetRelationship(kingdom);
				if (relationship > 0f)
				{
					num2 += relationship;
				}
				num += 1f;
			}
		}
		if (num == 0f)
		{
			return 0;
		}
		float value = num2 / num;
		int num3 = factor.field.Int(sf.vars);
		return (int)(Math.Abs(value) / Math.Abs(RelationUtils.Def.minRelationship) * (float)num3);
	}

	public static int sf_negative_relationship_with_muslim_kingdoms(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < kingdom.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = kingdom.game.kingdoms[i];
			if (!kingdom2.IsDefeated() && kingdom2.is_muslim && kingdom2 != kingdom)
			{
				float relationship = kingdom2.GetRelationship(kingdom);
				if (relationship < 0f)
				{
					num2 += relationship;
				}
				num += 1f;
			}
		}
		if (num == 0f)
		{
			return 0;
		}
		float value = num2 / num;
		int num3 = factor.field.Int(sf.vars);
		return (int)(Math.Abs(value) / Math.Abs(RelationUtils.Def.minRelationship) * (float)num3);
	}

	public static int sf_weak_scholars(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.IsCleric())
			{
				int classLevel = character.GetClassLevel();
				if (num < classLevel)
				{
					num = classLevel;
				}
			}
		}
		return factor.field.Int(sf.vars) + num;
	}

	public static int sf_we_are_too_small(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		int num2 = factor.field.GetInt("treshold", sf.vars);
		if (kingdom.realms.Count >= num2)
		{
			return 0;
		}
		return (num2 - kingdom.realms.Count) * num;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "fame")
		{
			return own_kingdom.GetStat(Stats.ks_fame_caliphate_bonus);
		}
		return base.GetVar(key, vars, as_value);
	}
}
