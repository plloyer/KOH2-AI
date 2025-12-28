using System.Collections.Generic;

namespace Logic;

public class LucrativeImportAction : MerchantOpportunity
{
	public LucrativeImportAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new LucrativeImportAction(owner as Character, def);
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
		if (!character.HasEmptyImportGoodSlot())
		{
			return "no_free_slot";
		}
		return base.Validate(quick_out);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<string> list = new List<string>();
		own_kingdom?.GetImportableGoods(base.own_character.mission_kingdom, list);
		if (list == null)
		{
			return null;
		}
		List<Value> list2 = new List<Value>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			list2.Add(text);
		}
		return new List<Value>[1] { list2 };
	}

	public override void Prepare()
	{
		base.Prepare();
		own_kingdom?.InvalidateIncomes();
	}

	public override void Run()
	{
		Value value = args[0];
		float discount = def.field.GetFloat("discount");
		base.own_character.ImportGood(value, -1, discount);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "goodDef"))
		{
			if (key == "cancelled_voice_line")
			{
				if (state < State.Running)
				{
					return "";
				}
				return base.GetVar(key, vars, as_value);
			}
			return base.GetVar(key, vars, as_value);
		}
		string good_name = GetVar("arg").String();
		return base.game.economy.GetGoodDef(good_name);
	}
}
