using System.Collections.Generic;

namespace Logic;

public class AbandonTraditionAction : Action
{
	public AbandonTraditionAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AbandonTraditionAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom == null)
		{
			return "not_a_kingdom";
		}
		string text = base.Validate(quick_out);
		if (text != "ok")
		{
			return text;
		}
		if (GetPossibleArgs() == null)
		{
			return "_no_targets";
		}
		return "ok";
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Tradition> list = own_kingdom?.traditions;
		if (list == null)
		{
			return null;
		}
		List<Value> list2 = new List<Value>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Tradition tradition = list[i];
			if (tradition != null)
			{
				list2.Add(tradition.def.id);
			}
		}
		return new List<Value>[1] { list2 };
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
			Tradition.Def def = base.game.defs.Get<Tradition.Def>(possibleTarget);
			if (def != null)
			{
				Vars vars = new Vars(def);
				vars.Set("owner", own_kingdom);
				vars.Set("name", def.id + ".name");
				vars.Set("localization_key", "IncreaseRankTraditionAction.picker_text");
				list.Add(vars);
			}
		}
		return list;
	}

	public override bool NeedsArgs()
	{
		if (args != null)
		{
			return args.Count == 0;
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "tradition"))
		{
			if (key == "icon")
			{
				if (args != null && args.Count > 0)
				{
					string id = args[0].String();
					Tradition.Def def = base.game.defs.Get<Tradition.Def>(id);
					if (def != null)
					{
						return new Value(def.field.FindChild("icon"));
					}
				}
				return new Value(base.def.field.FindChild("icon"));
			}
			return base.GetVar(key, vars, as_value);
		}
		if (args == null || args.Count < 1)
		{
			return Value.Unknown;
		}
		string id2 = args[0].String();
		Tradition.Def def2 = base.game.defs.Get<Tradition.Def>(id2);
		if (def2 != null)
		{
			return def2;
		}
		return Value.Null;
	}

	public override void Run()
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null || args == null || args.Count < 1 || args[0].obj_val == null || kingdom.traditions == null || kingdom.traditions.Count == 0)
		{
			return;
		}
		string text = args[0].String();
		for (int i = 0; i < kingdom.traditions.Count; i++)
		{
			Tradition tradition = kingdom.traditions[i];
			if (tradition != null && tradition.def.id == text)
			{
				kingdom.SetTradition(i, null, 0);
				break;
			}
		}
	}
}
