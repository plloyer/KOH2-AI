namespace Logic;

public class GrantIndependence : Offer
{
	public GrantIndependence(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public GrantIndependence(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new GrantIndependence(def, from, to);
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom.IsEnemy(kingdom2) || kingdom.sovereignState == null || kingdom.sovereignState != kingdom2)
		{
			return "not_a_vassal";
		}
		return text;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom.IsEnemy(kingdom2) || kingdom.sovereignState == null || kingdom.sovereignState != kingdom2)
		{
			return "not_a_vassal";
		}
		return text;
	}

	public override void OnAccept()
	{
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom2.DelVassalState(kingdom);
		kingdom2.NotifyListeners("grant_independence_to", kingdom);
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2);
		kingdomAndKingdomRelation.peace_time = base.game.time;
		kingdomAndKingdomRelation.OnChanged(kingdom, kingdom2);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override bool IsValidForAI()
	{
		return !AI;
	}
}
