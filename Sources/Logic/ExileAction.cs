namespace Logic;

public class ExileAction : Action
{
	public ExileAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ExileAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "dead";
		}
		if (character.IsKingOrPrince())
		{
			return "family_member";
		}
		Kingdom kingdom = character.GetKingdom();
		if (kingdom == null)
		{
			return "no_kingdom";
		}
		if (character.IsRebel())
		{
			return "rebel";
		}
		if (!own_kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		if (character.IsPope())
		{
			return "pope";
		}
		if (character.IsPatriarch())
		{
			return "patriarch";
		}
		if (character.prison_kingdom != null && character.prison_kingdom != kingdom)
		{
			return "imprisoned";
		}
		return "ok";
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "wages_decrease"))
		{
			if (key == "new_wages")
			{
				return GetNewWages();
			}
			return base.GetVar(key, vars, as_value);
		}
		return GetWagesDecrease();
	}

	private float GetWagesDecrease()
	{
		Kingdom kingdom = base.owner.GetKingdom();
		if (kingdom == null)
		{
			return 0f;
		}
		if (base.own_character == null)
		{
			return 0f;
		}
		float knightsWage = kingdom.GetKnightsWage(base.own_character.class_name);
		int knightsOnWageCount = kingdom.GetKnightsOnWageCount(base.own_character.class_name);
		if (knightsOnWageCount <= 0)
		{
			return 0f;
		}
		float wageGoldAtCount = kingdom.GetWageGoldAtCount(knightsOnWageCount - 1);
		return knightsWage - wageGoldAtCount;
	}

	private float GetNewWages()
	{
		Kingdom kingdom = base.owner.GetKingdom();
		if (kingdom == null)
		{
			return 0f;
		}
		if (base.own_character == null)
		{
			return 0f;
		}
		int knightsOnWageCount = kingdom.GetKnightsOnWageCount(base.own_character.class_name);
		if (knightsOnWageCount <= 0)
		{
			return 0f;
		}
		return kingdom.GetWageGoldAtCount(knightsOnWageCount - 1);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.NotifyListeners("exiled");
			Kingdom kingdom = base.owner.GetKingdom();
			int clericsCount = kingdom.GetClericsCount();
			Kingdom hq_kingdom = base.game.religions.catholic.hq_kingdom;
			bool flag = hq_kingdom != null && !hq_kingdom.IsDefeated() && (hq_kingdom.sovereignState == null || hq_kingdom.sovereignState != kingdom);
			bool flag2 = base.game.religions.catholic.head != null && base.game.religions.catholic.head.GetOriginalKingdom() != kingdom;
			if (character.IsCleric() && kingdom.is_catholic && clericsCount == 1 && flag && flag2)
			{
				kingdom.AddRelationModifier(hq_kingdom, "rel_catholic_last_cleric_exiled", this);
			}
			if (character.IsInCourt())
			{
				character.GetKingdom()?.DelCourtMember(character);
			}
			else
			{
				character.Die(new DeadStatus("exile", character));
			}
		}
	}
}
