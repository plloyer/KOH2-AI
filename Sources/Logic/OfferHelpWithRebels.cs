using System;

namespace Logic;

public class OfferHelpWithRebels : Offer
{
	public OfferHelpWithRebels(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferHelpWithRebels(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferHelpWithRebels(def, from, to);
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (CountSimilarOffersInParent(sameType: false, sameSourceTarget: true) > 0)
		{
			return false;
		}
		return true;
	}

	public bool HasEnemyRebels(Kingdom helped, Kingdom helper)
	{
		for (int i = 0; i < helped.rebellions.Count; i++)
		{
			Rebellion rebellion = helped.rebellions[i];
			for (int j = 0; j < rebellion.rebels.Count; j++)
			{
				if (helper.IsEnemy(rebellion.rebels[j]?.army))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom helped = GetTargetObj() as Kingdom;
		if (kingdom.is_player)
		{
			return "helper_is_player";
		}
		if (kingdom.wars.Count != 0)
		{
			return "_helper_has_wars";
		}
		if (!kingdom.neighbors.Contains(helped))
		{
			return "_not_neighbor";
		}
		if (helped.rebellions.Count == 0)
		{
			return "_helped_has_no_rebellions";
		}
		if (!HasEnemyRebels(helped, kingdom))
		{
			return "_helped_has_no_enemy_rebellions";
		}
		Tuple<Kingdom, Time> tuple = kingdom.ai.helpWithRebels.Find((Tuple<Kingdom, Time> t) => t.Item1 == helped);
		if (tuple != null && tuple.Item2 > base.game.time)
		{
			return "_already_helping_another";
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
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom helped = GetTargetObj() as Kingdom;
		if (kingdom.is_player)
		{
			return "helper_is_player";
		}
		if (kingdom.wars.Count != 0)
		{
			return "_helper_has_wars";
		}
		if (!kingdom.neighbors.Contains(helped))
		{
			return "_not_neighbor";
		}
		if (helped.rebellions.Count == 0)
		{
			return "_helped_has_no_rebellions";
		}
		if (!HasEnemyRebels(helped, kingdom))
		{
			return "_helped_has_no_enemy_rebellions";
		}
		Tuple<Kingdom, Time> tuple = kingdom.ai.helpWithRebels.Find((Tuple<Kingdom, Time> t) => t.Item1 == helped);
		if (tuple != null && tuple.Item2 > base.game.time)
		{
			return "_already_helping_another";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom k = GetTargetObj() as Kingdom;
		obj.AddHelpWithRebelsOf(k);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferHelpWithRebels)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandHelpWithRebels)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
