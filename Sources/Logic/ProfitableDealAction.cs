using System;
using System.Collections.Generic;

namespace Logic;

public class ProfitableDealAction : MerchantOpportunity
{
	public ProfitableDealAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ProfitableDealAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_mission_kingdom";
		}
		return base.Validate(quick_out);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		DT.Field field = def.field.FindChild("random_income_modifier");
		if (field == null)
		{
			return null;
		}
		float num = own_kingdom.income[ResourceType.Gold];
		Value value = field.Value(0);
		Value value2 = field.Value(1);
		int num2 = (((int)value2 > (int)value) ? 1 : (-1));
		int num3 = Math.Abs((int)value2 - (int)value);
		List<Value> list = new List<Value>(num3);
		for (int i = 0; i <= num3; i++)
		{
			list.Add(num * (float)((int)value + i * num2));
		}
		Value value3 = SuccessChanceValue(non_trivial_only: false);
		DT.Field field2 = def.field.FindChild("random_difficulty_modifier");
		if (field2 == null)
		{
			return null;
		}
		Value value4 = field2.Value(0);
		Value value5 = field2.Value(1);
		List<Value> list2 = new List<Value>(1);
		list2.Add(Map(value3, 0f, 100f, value4, value5));
		return new List<Value>[2] { list, list2 };
	}

	public float Clamp(float v, float min, float max)
	{
		if (v < min)
		{
			return min;
		}
		if (v > max)
		{
			return max;
		}
		return v;
	}

	public float Map(float v, float vmin, float vmax, float rmin, float rmax, bool clamp = false)
	{
		if (vmax == vmin)
		{
			return rmin;
		}
		if (clamp)
		{
			v = Clamp(v, vmin, vmax);
		}
		return rmin + (v - vmin) * (rmax - rmin) / (vmax - vmin);
	}

	public override void Run()
	{
		Value value = args[0];
		float amount = args[1].Float() * value.Float();
		own_kingdom.AddResources(KingdomAI.Expense.Category.Economy, ResourceType.Gold, amount);
		base.Run();
	}
}
