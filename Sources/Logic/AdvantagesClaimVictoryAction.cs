namespace Logic;

public class AdvantagesClaimVictoryAction : Action
{
	public AdvantagesClaimVictoryAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AdvantagesClaimVictoryAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom == null)
		{
			return "no_kingdom";
		}
		return own_kingdom.GetAdvantages().ValidateClaimVictory();
	}

	public override void Run()
	{
		own_kingdom.GetAdvantages().ClaimVictory();
	}
}
