namespace Logic;

public class DemandBook : OfferBook
{
	public DemandBook(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandBook(Kingdom from, Kingdom to, Book book)
		: base(from, to, book)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandBook(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}
}
