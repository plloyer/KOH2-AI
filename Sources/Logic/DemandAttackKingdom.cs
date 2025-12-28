using System.Collections.Generic;

namespace Logic;

public class DemandAttackKingdom : Offer
{
	public DemandAttackKingdom(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandAttackKingdom(Kingdom from, Kingdom to, Kingdom kingdom)
		: base(from, to, kingdom)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandAttackKingdom(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override bool DoesOfferHasSameArgs(Offer offer)
	{
		if (args == null || args.Count == 0 || offer.args == null || args.Count != offer.args.Count)
		{
			return false;
		}
		if (offer is DemandAttackKingdom)
		{
			if (offer.GetSourceObj() != GetSourceObj())
			{
				return false;
			}
			if (args[0].Match(offer.args[0]))
			{
				return true;
			}
			return false;
		}
		if (offer is OfferSupportInWar)
		{
			return offer.DoesOfferHasSameArgs(this);
		}
		return false;
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (parent == null && (from as Kingdom).IsEnemy(to as Kingdom))
		{
			return false;
		}
		if (CountSimilarOffersInParent(sameType: false, sameSourceTarget: false, sameArgs: true) > 0)
		{
			return false;
		}
		return true;
	}

	public override bool IsWar(bool sender)
	{
		return !sender;
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
		Kingdom arg = GetArg<Kingdom>(0);
		if (arg == null || arg.IsDefeated())
		{
			return "_invalid_args";
		}
		if (!War.CanStart(arg, kingdom))
		{
			return "_invalid_args";
		}
		if (!kingdom.neighbors.Contains(arg))
		{
			return "_invalid_args";
		}
		if (!kingdom2.neighbors.Contains(arg) && !kingdom2.IsEnemy(arg))
		{
			return "_invalid_args";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		Kingdom kingdom = GetSourceObj() as Kingdom;
		if (!(GetTargetObj() is Kingdom kingdom2) || kingdom == null)
		{
			return false;
		}
		foreach (Kingdom kingdom3 in base.game.kingdoms)
		{
			if (kingdom3 != kingdom2 && kingdom3 != kingdom && !kingdom3.IsDefeated() && kingdom.neighbors.Contains(kingdom3) && !(kingdom2.GetRelationship(kingdom3) >= 400f) && War.CanStart(kingdom, kingdom3) && (kingdom2.neighbors.Contains(kingdom3) || kingdom2.IsEnemy(kingdom3)))
			{
				lst.Add(kingdom3);
			}
		}
		ClearDuplicatesWithParent(idx, lst);
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom provoker = GetTargetObj() as Kingdom;
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom arg = GetArg<Kingdom>(0);
		obj.StartWarWith(arg, War.InvolvementReason.DemandAttackKingdom, "DemandAttackKingdomMessage", provoker);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(DemandAttackKingdom)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(OfferAttackKingdom)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(SummonVassal)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(OfferSupportInWar)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandSupportInWar)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(AskForProtection)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}

	public override bool IsValidForAI()
	{
		if (!base.IsValidForAI())
		{
			return false;
		}
		if (AI)
		{
			Kingdom arg = GetArg<Kingdom>(0);
			if (arg == null)
			{
				return true;
			}
			if (arg.is_player)
			{
				return false;
			}
		}
		return true;
	}

	public override bool ValidateRelChange(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
	{
		if (answer == "accept" && from == kTgt && to == kSrc && perm + temp < 0f)
		{
			return false;
		}
		return true;
	}
}
