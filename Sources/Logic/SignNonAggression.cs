using Logic.ExtensionMethods;

namespace Logic;

public class SignNonAggression : Offer
{
	public SignNonAggression(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SignNonAggression(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SignNonAggression(def, from, to);
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
		if (kingdom.sovereignState == kingdom2)
		{
			return "is_vassal";
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2);
		if (kingdomAndKingdomRelation.stance.IsWar())
		{
			return "in_war";
		}
		if (kingdomAndKingdomRelation.stance.IsNonAgression())
		{
			return "in_non_agression";
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
		if (kingdom.sovereignState == kingdom2)
		{
			return "is_vassal";
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2);
		if (kingdomAndKingdomRelation.stance.IsWar())
		{
			return "in_war";
		}
		if (kingdomAndKingdomRelation.stance.IsNonAgression())
		{
			return "in_non_agression";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom.SetStance(kingdom2, RelationUtils.Stance.NonAggression);
		Vars vars = new Vars();
		vars.SetVar("kingdom_a", kingdom);
		vars.SetVar("kingdom_b", kingdom2);
		kingdom.game.BroadcastRadioEvent("NonAggressionSignedMessage", vars);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(SignNonAggression)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(SignNonAggressionRenewTheirKing)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(SignNonAggressionRenewOurKing)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
