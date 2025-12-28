using System.Collections.Generic;

namespace Logic;

public class DemandGold : Offer
{
	public DemandGold(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandGold(Kingdom from, Kingdom to, int s_level)
		: base(from, to, s_level)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandGold(def, from, to);
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
		if (parent is CounterOffer || parent is SweetenOffer)
		{
			Offer arg = parent.GetArg<Offer>(0);
			if (arg is PeaceDemandTribute || arg is PeaceOfferTribute)
			{
				if (CountSimilarOffersInParent(sameType: false, sameSourceTarget: true) > 1)
				{
					return false;
				}
			}
			else if (CountSimilarOffersInParent() > 0)
			{
				return false;
			}
		}
		else if (CountSimilarOffersInParent(sameType: false, sameSourceTarget: true) > 0)
		{
			return false;
		}
		return true;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		int num = GetGoldAmount();
		if (num <= 0)
		{
			return "negative_amount";
		}
		if (!ShouldSkip(text))
		{
			foreach (Offer item in GetSimilarOffersInParent(sameType: false, sameSourceTarget: true))
			{
				num += (item as DemandGold).GetGoldAmount();
			}
			if ((GetSourceObj() as Kingdom).resources[ResourceType.Gold] < (float)num)
			{
				text = "_not_enough_gold";
			}
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
		int num = kingdom.DiplomaticGoldLevels();
		float diplomaticGoldAmount = kingdom.GetDiplomaticGoldAmount(1);
		for (int i = 1; i <= num; i++)
		{
			lst.Add(i);
		}
		return kingdom.resources[ResourceType.Gold] >= diplomaticGoldAmount;
	}

	public int GetGoldAmount()
	{
		if (args == null || args.Count == 0)
		{
			return 0;
		}
		return (int)(GetSourceObj() as Kingdom).GetDiplomaticGoldAmount(GetArg(0));
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom kingdom = GetTargetObj() as Kingdom;
		int goldAmount = GetGoldAmount();
		obj.SubResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, goldAmount);
		kingdom.AddResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, goldAmount);
	}

	public override float Eval(string threshold_name, bool reverse_kingdoms = false)
	{
		ProsAndCons prosAndCons = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);
		if (prosAndCons == null)
		{
			return 0f;
		}
		return (float)GetGoldAmount() * prosAndCons.def.cost.Float(prosAndCons, 100000f);
	}

	public override bool IsValidForAI()
	{
		if (AI && parent == null)
		{
			return false;
		}
		return base.IsValidForAI();
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferGold)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandGold)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}

	public virtual float GetRelationsTreasury()
	{
		Kingdom kingdom = from as Kingdom;
		Kingdom obj = to as Kingdom;
		float maxTreasury = kingdom.GetMaxTreasury();
		float maxTreasury2 = obj.GetMaxTreasury();
		if (maxTreasury < maxTreasury2)
		{
			return maxTreasury;
		}
		return (maxTreasury + maxTreasury2) / 2f;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"relations_treasury_divider" => GetRelationsTreasury(), 
			"gold_amount" => GetGoldAmount(), 
			"donor" => GetSourceObj() as Kingdom, 
			"recipient" => GetTargetObj() as Kingdom, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}
}
