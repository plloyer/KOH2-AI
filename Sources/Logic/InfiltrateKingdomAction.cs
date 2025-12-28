namespace Logic;

public class InfiltrateKingdomAction : Action
{
	public InfiltrateKingdomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InfiltrateKingdomAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character?.GetArmy() != null)
		{
			return "leading_army";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!base.ValidateTarget(target))
		{
			return false;
		}
		if (!(target is Kingdom kingdom))
		{
			return false;
		}
		Kingdom kingdom2 = base.own_character.GetKingdom();
		if (kingdom.GetSpyFrom(kingdom2) != null)
		{
			return false;
		}
		if (kingdom2.GetInfiltratingSpy(kingdom) != null)
		{
			return false;
		}
		return true;
	}

	public override void Prepare()
	{
		base.Prepare();
		OnMissionChangedAnalytics();
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.SetMissionKingdom(base.target as Kingdom);
			character.SetDefaultStatus<InfiltratedKingdomStatus>();
			Timer.Start(character, "puppet_rebel_distance_check", def.field.GetFloat("puppet_rebel_distance_check_time", null, 120f));
			own_kingdom?.FireEvent("infiltrated", base.target);
			(base.target as Kingdom)?.FireEvent("infiltrated_by", character);
			base.Run();
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "distance"))
		{
			if (key == "spy_is_known")
			{
				return base.own_character.IsRevealedIn((base.target as Kingdom) ?? (vars?.GetVar("target").obj_val as Kingdom));
			}
			return base.GetVar(key, vars, as_value);
		}
		return ((float?)own_kingdom?.DistanceToKingdom(base.target as Kingdom)) ?? 0f;
	}
}
