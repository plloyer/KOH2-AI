using System;

namespace Logic;

public class DemandSupportInWar : OfferSupportInWar
{
	public DemandSupportInWar(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandSupportInWar(Kingdom from, Kingdom to, War war)
		: base(from, to, war)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandSupportInWar(def, from, to);
	}

	public override bool IsWar(bool sender)
	{
		return !sender;
	}

	public override bool ValidateVassalage(Kingdom helper, Kingdom helped)
	{
		if (helped.sovereignState == helper)
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
		if (text.StartsWith("_", StringComparison.Ordinal))
		{
			return text.Remove(0, 1);
		}
		return text;
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
