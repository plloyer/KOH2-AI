using System.Collections.Generic;

namespace Logic;

public class DiplomacySendGiftAction : DiplomatAction
{
	public DiplomacySendGiftAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacySendGiftAction(owner as Character, def);
	}

	public override void Run()
	{
		base.Run();
	}

	public override bool ApplyCost(bool check_first = true)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (args == null || args.Count < 1)
		{
			return false;
		}
		Value value = args[0];
		if (check_first && kingdom.resources.Get(ResourceType.Gold) < (float)value)
		{
			return false;
		}
		kingdom.SubResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, value);
		return true;
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
			return "not_in_a_kingdom";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateArg(Value value, int def_type)
	{
		if (def_type != 0)
		{
			return base.ValidateArg(value, def_type);
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		float num = value.Float();
		if (kingdom.resources.Get(ResourceType.Gold) < num)
		{
			return false;
		}
		return true;
	}

	public override List<Value>[] GetPossibleArgs()
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		kingdom.resources.Get(ResourceType.Gold);
		List<Value> list = new List<Value>(5);
		for (int i = 1; i <= 5; i++)
		{
			int num = (int)kingdom.GetDiplomaticGoldAmount(i);
			list.Add(num);
		}
		return new List<Value>[1] { list };
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		if (own_kingdom == null)
		{
			return null;
		}
		List<Vars> list = new List<Vars>(possibleTargets.Count);
		foreach (Value possibleTarget in possibleTargets)
		{
			Vars vars = new Vars(possibleTarget);
			vars.Set("kingdom", own_kingdom);
			vars.Set("localization_key", "DiplomacySendGiftAction.picker_text");
			Resource resource = new Resource();
			resource.Add(ResourceType.Gold, possibleTarget.Float());
			vars.Set("amount", resource);
			list.Add(vars);
		}
		return list;
	}
}
