using System.Collections.Generic;

namespace Logic;

public class DemandFundCrusade : Offer
{
	public DemandFundCrusade(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandFundCrusade(Kingdom from, Kingdom to, int s_level)
		: base(from, to, s_level)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandFundCrusade(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom2 != base.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		if (!kingdom.is_catholic)
		{
			return "not_catholic";
		}
		if (kingdom.excommunicated)
		{
			return "excommunicated";
		}
		if (kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
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
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom2 != base.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		if (kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		if (!kingdom.is_catholic)
		{
			return "not_catholic";
		}
		if (kingdom.excommunicated)
		{
			return "excommunicated";
		}
		int goldAmount = GetGoldAmount();
		if (goldAmount <= 0)
		{
			return "negative_amount";
		}
		if (!ShouldSkip(text) && (GetSourceObj() as Kingdom).resources[ResourceType.Gold] < (float)goldAmount)
		{
			text = "_not_enough_gold";
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
		GetTargetObj();
		int goldAmount = GetGoldAmount();
		obj.SubResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, goldAmount);
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
