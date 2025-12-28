namespace Logic;

public class JoinVassalOffer : Offer
{
	public JoinVassalOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public JoinVassalOffer(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new JoinVassalOffer(def, from, to);
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
		KingdomAndKingdomRelation.Get(kingdom, kingdom2);
		if (kingdom2.sovereignState != kingdom)
		{
			return "not_a_vassal";
		}
		if (kingdom.GetKing() == null || !kingdom.GetKing().IsDiplomat())
		{
			return "not_a_king";
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
		KingdomAndKingdomRelation.Get(kingdom, kingdom2);
		if (kingdom2.sovereignState != kingdom)
		{
			return "not_a_vassal";
		}
		if (kingdom.GetKing() == null || !kingdom.GetKing().IsDiplomat())
		{
			return "not_a_king";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom.DelVassalState(kingdom2);
		while (kingdom2.realms.Count > 0)
		{
			kingdom2.realms[0].SetKingdom(kingdom.id, ignore_victory: false, check_cancel_battle: true, via_diplomacy: true);
		}
	}
}
