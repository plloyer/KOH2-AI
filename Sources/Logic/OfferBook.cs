using System.Collections.Generic;

namespace Logic;

public class OfferBook : Offer
{
	public OfferBook(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferBook(Kingdom from, Kingdom to, Book book)
		: base(from, to, book)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferBook(def, from, to);
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
		if ((GetSourceObj() as Kingdom).books.Count - CountSimilarOffersInParent(sameType: false, sameSourceTarget: true) < 1)
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
			return "not_in_war?!?";
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
			return "not_in_war?!?";
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Book arg = GetArg<Book>(0);
		if (arg == null)
		{
			return "book_missing";
		}
		if (arg.GetKingdom().id != kingdom.id)
		{
			return "book_not_from_giving_kingdom";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		if (!(GetSourceObj() is Kingdom kingdom))
		{
			return false;
		}
		if (kingdom.books.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < kingdom.books.Count; i++)
		{
			lst.Add(kingdom.books[i]);
		}
		ClearDuplicatesWithParent(idx, lst);
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom obj2 = GetTargetObj() as Kingdom;
		Book arg = GetArg<Book>(0);
		obj2.AddBook(arg.def);
		obj.DelBook(arg.def);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferBook)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandBook)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}

	public override float Eval(string threshold_name, bool reverse_kingdoms = false)
	{
		ProsAndCons prosAndCons = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);
		return prosAndCons?.def.cost.Float(prosAndCons, 100000f) ?? 0f;
	}
}
