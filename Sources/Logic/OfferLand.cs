namespace Logic;

public class OfferLand : DemandLand
{
	public OfferLand(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferLand(Kingdom from, Kingdom to, Realm realm)
		: base(from, to, realm)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferLand(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return from;
	}

	public override Object GetTargetObj()
	{
		return to;
	}

	public override float Eval(string threshold_name, bool reverse_kingdoms = false)
	{
		Realm arg = GetArg<Realm>(0);
		Kingdom forKingdom = GetSourceObj() as Kingdom;
		if (reverse_kingdoms)
		{
			if (threshold_name == "Propose")
			{
				forKingdom = GetTargetObj() as Kingdom;
			}
		}
		else if (threshold_name == "Accept")
		{
			forKingdom = GetTargetObj() as Kingdom;
		}
		ProsAndCons prosAndCons = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);
		if (prosAndCons == null)
		{
			return 0f;
		}
		return arg.CalcCost(forKingdom) * prosAndCons.def.cost.Float(prosAndCons, 100000f);
	}
}
