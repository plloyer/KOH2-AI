using System.Collections.Generic;

namespace Logic;

public class AskForLoan : Offer
{
	public AskForLoan(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public AskForLoan(Kingdom from, Kingdom to, int amount)
		: base(from, to, amount)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new AskForLoan(def, from, to);
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (!(from as Kingdom).GetRoyalMarriage(to as Kingdom))
		{
			return "not_married";
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
		if (!(from as Kingdom).GetRoyalMarriage(to as Kingdom))
		{
			return "not_married";
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		int num = GetArg(0);
		if (num <= 0)
		{
			return "negative_amount";
		}
		if (!ShouldSkip(text) && kingdom.resources[ResourceType.Gold] < (float)num)
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
		int num = (int)kingdom.resources[ResourceType.Gold];
		if (num < 10)
		{
			return false;
		}
		lst.Add(num / 10);
		lst.Add(num / 5);
		lst.Add(num / 4);
		lst.Add(num / 3);
		lst.Add(num / 2);
		lst.Add(num);
		return true;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom obj = GetTargetObj() as Kingdom;
		int num = GetArg(0);
		kingdom.SubResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, num);
		obj.AddResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, num);
		float value = 0f;
		obj.loans.TryGetValue(kingdom.id, out value);
		obj.loans[kingdom.id] = value + (float)num;
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override float Eval(string threshold_name, bool reverse_kingdoms = false)
	{
		ProsAndCons prosAndCons = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);
		if (prosAndCons == null)
		{
			return 0f;
		}
		return (float)GetArg(0) * prosAndCons.def.cost.Float(prosAndCons, 100000f);
	}
}
