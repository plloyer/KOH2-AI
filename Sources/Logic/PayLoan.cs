using System.Collections.Generic;

namespace Logic;

public class PayLoan : Offer
{
	public PayLoan(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public PayLoan(Kingdom from, Kingdom to, int amount)
		: base(from, to, amount)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new PayLoan(def, from, to);
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
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom kingdom = GetTargetObj() as Kingdom;
		float value = 0f;
		if (!obj.loans.TryGetValue(kingdom.id, out value))
		{
			return "no_loan";
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
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		int num = GetArg(0);
		if (num <= 0)
		{
			return "negative_amount";
		}
		float value = 0f;
		if (!kingdom.loans.TryGetValue(kingdom2.id, out value))
		{
			return "no_loan";
		}
		if (value < (float)num)
		{
			return "too_much_gold";
		}
		if (!ShouldSkip(text) && kingdom.resources[ResourceType.Gold] < (float)num)
		{
			text = "not_enough_gold";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom == null)
		{
			return false;
		}
		float value = 0f;
		if (!kingdom.loans.TryGetValue(kingdom2.id, out value))
		{
			return false;
		}
		if (value < 10f)
		{
			return false;
		}
		lst.Add((int)(value / 10f));
		lst.Add((int)(value / 5f));
		lst.Add((int)(value / 4f));
		lst.Add((int)(value / 3f));
		lst.Add((int)(value / 2f));
		lst.Add((int)value);
		return true;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom kingdom = GetTargetObj() as Kingdom;
		int num = GetArg(0);
		obj.SubResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, num);
		kingdom.AddResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, num);
		float value = 0f;
		obj.loans.TryGetValue(kingdom.id, out value);
		obj.loans[kingdom.id] = value - (float)num;
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
