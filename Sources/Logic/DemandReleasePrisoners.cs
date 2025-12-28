using System.Collections.Generic;

namespace Logic;

public class DemandReleasePrisoners : Offer
{
	public DemandReleasePrisoners(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandReleasePrisoners(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandReleasePrisoners(def, from, to);
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
		if (CountSimilarOffersInParent(sameType: false, sameSourceTarget: true) > 0)
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
		if (!ShouldSkip(text) && !GetPossibleArgValues(0, Offer.tmp_values))
		{
			text = "_no_possible_args";
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
		if (!ShouldSkip(text) && !GetPossibleArgValues(0, Offer.tmp_values))
		{
			text = "_no_possible_args";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kTarget = GetTargetObj() as Kingdom;
		if (kingdom == null || kTarget == null)
		{
			return false;
		}
		if (kingdom.prisoners == null || kingdom.prisoners.Count == 0)
		{
			return false;
		}
		List<Character> list = kingdom.prisoners.FindAll((Character p) => p.kingdom_id == kTarget.id && !p.IsMercenary());
		if (list.Count == 0)
		{
			return false;
		}
		foreach (Character item in list)
		{
			lst.Add(item);
		}
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		GetPossibleArgValues(0, Offer.tmp_values);
		foreach (Value tmp_value in Offer.tmp_values)
		{
			Character obj = tmp_value.obj_val as Character;
			obj.Imprison(null, recall: true, send_state: true, "diplomacy");
			obj.NotifyListeners("prisoner_released");
		}
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferReleasePrisoners)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandReleasePrisoners)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
