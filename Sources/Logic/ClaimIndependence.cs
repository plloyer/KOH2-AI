namespace Logic;

public class ClaimIndependence : GrantIndependence
{
	public ClaimIndependence(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public ClaimIndependence(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new ClaimIndependence(def, from, to);
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (CountSimilarOffersInParent() > 0)
		{
			return false;
		}
		return true;
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
		if (kingdom.sovereignState == null || kingdom.sovereignState != kingdom2)
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
		if (kingdom.sovereignState == null || kingdom.sovereignState != kingdom2)
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
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2);
		kingdomAndKingdomRelation.peace_time = base.game.time;
		kingdomAndKingdomRelation.OnChanged(kingdom, kingdom2);
		kingdom.NotifyListeners("independence", kingdom2);
	}

	public override void OnDecline()
	{
		Cancel();
		new IndependenceWarOffer(to as Kingdom, from as Kingdom).Send();
		base.OnDecline();
	}

	public override void OnCustomCounterOfferAnswer()
	{
		if (parent == null)
		{
			return;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (War.CanStart(kingdom, kingdom2))
		{
			War war = kingdom.StartWarWith(kingdom2, War.InvolvementReason.VassalIndependenceClaim, "KingdomDeclaredIndependenceMessage");
			if (war != null)
			{
				war.SetType("IndependenceWar");
				kingdom.NotifyListeners("independence", kingdom2);
			}
		}
	}

	public override Object GetSourceObj()
	{
		return from;
	}

	public override Object GetTargetObj()
	{
		return to;
	}

	public override bool IsValidForAI()
	{
		return true;
	}
}
