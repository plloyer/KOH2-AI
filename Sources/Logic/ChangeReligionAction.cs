using System.Collections.Generic;

namespace Logic;

public class ChangeReligionAction : Action
{
	private Religion old_religion;

	public ChangeReligionAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ChangeReligionAction(owner as Kingdom, def);
	}

	public Religion GetTartgetReligion()
	{
		string name = def.field.GetString("target_religion");
		return base.game.religions.Get(name);
	}

	public override string Validate(bool quick_out = false)
	{
		Religion religion = own_kingdom.religion;
		Religion tartgetReligion = GetTartgetReligion();
		if (tartgetReligion == null || tartgetReligion == religion)
		{
			return "already_of_that_religion";
		}
		if (own_kingdom.caliphate)
		{
			return "_is_caliphate";
		}
		if (own_kingdom.actions.Find("ClaimCaliphateAction").state == State.Preparing)
		{
			return "_claming_caliphate";
		}
		return base.Validate(quick_out);
	}

	private void ApplyOutcomeChangeRelWithTargetReligion(OutcomeDef outcome)
	{
		DT.Field field = outcome.field;
		string modifierName = field.GetString("liege");
		Value val = field.Value();
		Religion tartgetReligion = GetTartgetReligion();
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = base.game.kingdoms[i];
			if (kingdom.religion == tartgetReligion && !kingdom.IsDefeated())
			{
				own_kingdom.AddRelationModifier(kingdom, val, null);
				if (kingdom == own_kingdom.sovereignState)
				{
					own_kingdom.AddRelationModifier(kingdom, modifierName, null);
				}
			}
		}
	}

	private void ApplyOutcomeChangeRelWithPreviousReligion(OutcomeDef outcome)
	{
		DT.Field field = outcome.field;
		Value val = field.Value();
		string modifierName = field.GetString("papacy");
		string modifierName2 = field.GetString("constantinople");
		Religion religion = old_religion;
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = base.game.kingdoms[i];
			if (kingdom.religion == religion && !kingdom.IsDefeated())
			{
				own_kingdom.AddRelationModifier(kingdom, val, null);
				if (base.game.religions.catholic.hq_kingdom == kingdom)
				{
					own_kingdom.AddRelationModifier(kingdom, modifierName, null);
				}
				if (base.game.religions.orthodox.hq_kingdom == kingdom)
				{
					own_kingdom.AddRelationModifier(kingdom, modifierName2, null);
				}
			}
		}
	}

	private void KillKingByNobility()
	{
		own_kingdom.GetKing().Die(new DeadStatus("killed_by_nobility", own_kingdom.GetKing()));
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "change_rel_with_target_religion":
			ApplyOutcomeChangeRelWithTargetReligion(outcome);
			return true;
		case "change_rel_with_previous_religion":
			ApplyOutcomeChangeRelWithPreviousReligion(outcome);
			return true;
		case "kill_king_by_nobility":
			KillKingByNobility();
			return true;
		default:
			return base.ApplyOutcome(outcome);
		}
	}

	public override void Run()
	{
		old_religion = own_kingdom.religion;
		Religion tartgetReligion = GetTartgetReligion();
		own_kingdom.ChangeReligion(tartgetReligion);
		base.Run();
	}

	private float GetSFRelationBonus()
	{
		float num = 0f;
		Religion tartgetReligion = GetTartgetReligion();
		int num2 = 0;
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = base.game.kingdoms[i];
			if (kingdom.religion == tartgetReligion && !kingdom.IsDefeated())
			{
				num2++;
				num += base.game.Map(kingdom.GetRelationship(own_kingdom), RelationUtils.Def.minRelationship, RelationUtils.Def.maxRelationship, -20f, 20f);
			}
		}
		if (num2 == 0)
		{
			return 0f;
		}
		return num / (float)num2;
	}

	private float GetSFLiegeBonus()
	{
		Religion tartgetReligion = GetTartgetReligion();
		Kingdom sovereignState = own_kingdom.sovereignState;
		if (sovereignState == null)
		{
			return 0f;
		}
		if (sovereignState.religion == own_kingdom.religion)
		{
			return def.field.GetFloat("sf_liege_same_religion");
		}
		if (sovereignState.religion != own_kingdom.religion)
		{
			if (sovereignState.religion == tartgetReligion)
			{
				return def.field.GetFloat("sf_liege_target_religion");
			}
			return def.field.GetFloat("sf_liege_diff_religion");
		}
		return 0f;
	}

	private bool IsLastFromReligion()
	{
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = base.game.kingdoms[i];
			if (!kingdom.IsDefeated() && kingdom.religion == own_kingdom.religion && kingdom != own_kingdom)
			{
				return false;
			}
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"relations_sf_chance" => GetSFRelationBonus(), 
			"liege_sf_chance" => GetSFLiegeBonus(), 
			"is_last_from_religon" => IsLastFromReligion(), 
			"target_religion" => GetTartgetReligion(), 
			_ => base.GetVar(key, vars, as_value), 
		};
	}

	public static int sf_we_have_sunni_holy_lands(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
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
		int num2 = factor.field.Int(sf.vars);
		return num * num2;
	}

	public static int sf_we_hold_Baghdad(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		if ((kingdom.game?.religions?.shia?.holy_lands_realm)?.GetKingdom() == kingdom)
		{
			return factor.field.Int(sf.vars);
		}
		return 0;
	}

	public static int sf_num_disloyal_realms(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
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
		Religion religion = (sf.vars as ChangeReligionAction)?.GetTartgetReligion();
		if (religion == null)
		{
			religion = ((sf.vars as Vars)?.obj.obj_val as ChangeReligionAction)?.GetTartgetReligion();
		}
		if (religion == null)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			if (realm.pop_majority.kingdom != kingdom && realm.religion != religion)
			{
				num2++;
			}
		}
		return num2 * num;
	}

	public static int sf_leading_crusade(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Character character = kingdom.game.religions.catholic.crusade?.leader;
		if (character != null && character.kingdom_id == kingdom.id)
		{
			return factor.field.Int(sf.vars);
		}
		return 0;
	}
}
