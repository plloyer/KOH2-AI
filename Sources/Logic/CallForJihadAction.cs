namespace Logic;

public class CallForJihadAction : Action
{
	public CallForJihadAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CallForJihadAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "not_a_kindom";
		}
		if (!kingdom.is_muslim || !kingdom.IsCaliphate())
		{
			return "not_caliphate";
		}
		return "ok";
	}

	public override bool ValidateRequirement(DT.Field rf)
	{
		if (rf.key == "adult_or_younger_king")
		{
			return own_kingdom.GetKing().age <= Character.Age.Adult;
		}
		if (rf.key == "king_not_venerable")
		{
			return own_kingdom.GetKing().age != Character.Age.Venerable;
		}
		if (rf.key == "no_other_jihad")
		{
			return own_kingdom.game.religions.jihad_kingdoms.Count == 0;
		}
		return base.ValidateRequirement(rf);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom kingdom) || kingdom.IsDefeated())
		{
			return false;
		}
		if (kingdom.is_muslim)
		{
			return false;
		}
		if (!own_kingdom.IsEnemy(kingdom))
		{
			return false;
		}
		War war = own_kingdom.FindWarWith(kingdom);
		if (war == null || !war.IsLeader(own_kingdom) || !war.IsLeader(kingdom))
		{
			return false;
		}
		if (kingdom.jihad_attacker != null)
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		if (own_kingdom.jihad_target == null)
		{
			War.StartJihad(own_kingdom, base.target as Kingdom);
			base.Run();
		}
	}
}
