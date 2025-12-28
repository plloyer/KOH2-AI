namespace Logic;

public class SweetenOffer : Offer
{
	public SweetenOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SweetenOffer(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SweetenOffer(def, from, to);
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		for (int i = 0; i < args.Count; i++)
		{
			if (args[i].obj_val is Offer offer)
			{
				string text2 = offer.Validate();
				if (ShouldReturn(text2))
				{
					return text2;
				}
				if (text2 != "ok" && text == "ok")
				{
					text = text2;
				}
			}
		}
		return text;
	}

	public override void OnAccept()
	{
		for (int i = 0; i < args.Count; i++)
		{
			(args[i].obj_val as Offer).OnAccept();
		}
	}

	public override void OnDecline()
	{
		(args[0].obj_val as Offer).OnDecline();
	}

	public override float Eval(string treshold_name, bool reverse_kingdoms = false)
	{
		float result = 0f;
		for (int i = 0; i < args.Count; i++)
		{
			result = (args[i].obj_val as Offer).Eval(treshold_name, reverse_kingdoms);
		}
		return result;
	}

	public override bool IsValidForAI()
	{
		return true;
	}
}
