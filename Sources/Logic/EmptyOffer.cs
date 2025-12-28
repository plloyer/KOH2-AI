namespace Logic;

public class EmptyOffer : Offer
{
	public EmptyOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public EmptyOffer(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new EmptyOffer(def, from, to);
	}

	public override float Eval(string treshold_name, bool reverse_kingdoms = false)
	{
		return 0f;
	}
}
