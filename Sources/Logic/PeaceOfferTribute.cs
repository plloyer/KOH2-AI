using System.Collections.Generic;

namespace Logic;

public class PeaceOfferTribute : Offer
{
	public RelationUtils.Stance stance = RelationUtils.Stance.Peace;

	public PeaceOfferTribute(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public PeaceOfferTribute(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new PeaceOfferTribute(def, from, to);
	}

	public override void SetArgs(params Value[] args)
	{
		base.SetArgs(args);
		if (base.args == null)
		{
			base.args = new List<Value>();
		}
		for (int i = 0; i < def.max_args; i++)
		{
			Offer offer = Offer.Create("EmptyOffer", from, to, this);
			if (i <= base.args.Count)
			{
				base.args.Add(new Value(offer));
			}
		}
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (CountSimilarOffersInParent() > 0)
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
		if (!War.CanStop(from as Kingdom, to as Kingdom))
		{
			return "_cant_stop_war";
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
		if (!War.CanStop(from as Kingdom, to as Kingdom))
		{
			return "cant_stop_war";
		}
		int num = 0;
		for (int i = 0; i < args.Count; i++)
		{
			if (args[i].obj_val is Offer offer)
			{
				if (offer is EmptyOffer)
				{
					num++;
				}
				string text2 = offer.Validate();
				if (ShouldReturn(text2))
				{
					return text2;
				}
				if (text != "ok")
				{
					text = offer.def.field.GetString("diplomacy_event_id") + "." + text2;
					break;
				}
			}
		}
		if (num == args.Count)
		{
			return "only_empty_offers";
		}
		return text;
	}

	protected virtual string GetPeaceReason()
	{
		return "offer_tribute";
	}

	public override void OnAccept()
	{
		base.OnAccept();
		for (int i = 0; i < args.Count; i++)
		{
			(args[i].obj_val as Offer).OnAccept();
		}
		Kingdom kingdom = from as Kingdom;
		Kingdom k = to as Kingdom;
		Kingdom victor = GetTargetObj() as Kingdom;
		if (kingdom.FindWarWith(k) != null)
		{
			kingdom.EndWarWith(k, victor, GetPeaceReason());
		}
		kingdom.SetStance(k, stance);
	}

	public virtual float EvalSuboffers(string treshold_name, bool reverse_kingdoms = false)
	{
		float num = 0f;
		for (int i = 0; i < args.Count; i++)
		{
			float num2 = (args[i].obj_val as Offer).Eval(treshold_name, reverse_kingdoms);
			if (num2 > 0f)
			{
				num += num2;
			}
		}
		return num;
	}

	public override float Eval(string treshold_name, bool reverse_kingdoms = false)
	{
		return base.Eval(treshold_name, reverse_kingdoms) + EvalSuboffers(treshold_name, reverse_kingdoms) * def.field.GetFloat("sub_offers_weight_mod", null, 1f);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(PeaceOfferTribute)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(WhitePeaceOffer)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(PeaceDemandTribute)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
