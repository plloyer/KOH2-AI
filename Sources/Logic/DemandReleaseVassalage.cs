using System.Collections.Generic;

namespace Logic;

public class DemandReleaseVassalage : Offer
{
	public DemandReleaseVassalage(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandReleaseVassalage(Kingdom from, Kingdom to, Kingdom vassal)
		: base(from, to, vassal)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandReleaseVassalage(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
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
		return true;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
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
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
		}
		if (GetArg<Kingdom>(0).sovereignState != GetSourceObj() as Kingdom)
		{
			return "_invalid_args";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		foreach (Kingdom vassalState in (GetSourceObj() as Kingdom).vassalStates)
		{
			lst.Add(vassalState);
		}
		ClearDuplicatesWithParent(idx, lst);
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom arg = GetArg<Kingdom>(0);
		obj.DelVassalState(arg);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferReleaseVassalage)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandReleaseVassalage)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
