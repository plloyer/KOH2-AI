namespace Logic;

public class PeaceDemandTribute : PeaceOfferTribute
{
	public PeaceDemandTribute(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public PeaceDemandTribute(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to, args)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new PeaceDemandTribute(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override float EvalSuboffers(string treshold_name, bool reverse_kingdoms = false)
	{
		float num = 0f;
		for (int i = 0; i < args.Count; i++)
		{
			float num2 = (args[i].obj_val as Offer).Eval(treshold_name, reverse_kingdoms);
			if (num2 < 0f)
			{
				num += num2;
			}
		}
		return num;
	}

	protected override string GetPeaceReason()
	{
		return "demand_tribute";
	}
}
