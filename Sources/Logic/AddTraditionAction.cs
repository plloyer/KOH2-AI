using System;
using System.Collections.Generic;

namespace Logic;

public class AddTraditionAction : Action
{
	private Tradition.Type tradition_type = Tradition.Type.All;

	public AddTraditionAction(Kingdom owner, Def def)
		: base(owner, def)
	{
		ReadTraditionType();
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AddTraditionAction(owner as Kingdom, def);
	}

	private void ReadTraditionType()
	{
		string text = def?.field?.GetString("tradition_type");
		if (!string.IsNullOrEmpty(text))
		{
			if (Enum.TryParse<Tradition.Type>(text, out var result))
			{
				tradition_type = result;
			}
			else
			{
				Game.Log(def.field.Path(include_file: true) + ": Invalid tradition type '" + text + "'", Game.LogType.Warning);
			}
		}
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "not_a_kingdom";
		}
		string text = base.Validate(quick_out);
		if (text != "ok")
		{
			return text;
		}
		if (!kingdom.CanAddTradition(tradition_type))
		{
			return "_maxed_out";
		}
		if (GetPossibleArgs() == null)
		{
			return "_no_targets";
		}
		if (!kingdom.HasFreeTraditionSlot())
		{
			return "no_free_slots";
		}
		return "ok";
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Tradition.Def> list = own_kingdom?.GetNewTraditionOptions(tradition_type);
		if (list == null)
		{
			return null;
		}
		List<Value> list2 = new List<Value>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Tradition.Def def = list[i];
			list2.Add(def.id);
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
				vars.Set("cost", def.GetAdoptCost(own_kingdom, (args != null && args.Count >= 2) ? args[1].Int() : (-1)));
				vars.Set("kingdom", own_kingdom);
				vars.Set("localization_key", "AddTraditionAction.picker_text");
				list.Add(vars);
			}
		}
		return list;
	}

	public override bool ValidateArgs()
	{
		if (args != null && args.Count >= 1)
		{
			string id = args[0];
			Tradition.Def def = base.game.defs.Get<Tradition.Def>(id);
			if (def == null)
			{
				return false;
			}
			if (!own_kingdom.CanAddTradition(def))
			{
				return false;
			}
		}
		return base.ValidateArgs();
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (def.cost == null)
		{
			return null;
		}
		if (vars == null)
		{
			if (target == null)
			{
				vars = this;
			}
			else
			{
				Vars vars2 = new Vars(this);
				vars2.Set("target", target);
				vars = vars2;
			}
		}
		return Resource.Parse(def.cost, vars);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "tradition":
		{
			if (args == null || args.Count < 1)
			{
				return Value.Unknown;
			}
			string id2 = args[0].String();
			Tradition.Def def3 = base.game.defs.Get<Tradition.Def>(id2);
			if (def3 != null)
			{
				return def3;
			}
			return Value.Null;
		}
		case "adopt_cost":
		{
			if (args == null || args.Count < 1)
			{
				return Value.Unknown;
			}
			string id3 = args[0].String();
			Tradition.Def def4 = base.game.defs.Get<Tradition.Def>(id3);
			if (def4 == null)
			{
				return Value.Unknown;
			}
			return def4.GetAdoptCost(own_kingdom, (args.Count >= 2) ? args[1].Int() : (-1));
		}
		case "cur_skill_traditions":
		{
			List<Tradition> list = own_kingdom?.traditions;
			if (list == null || list.Count == 0)
			{
				return Value.Null;
			}
			string text = "";
			for (int i = 0; i < list.Count; i++)
			{
				Tradition.Def def2 = list[i]?.def;
				if (def2 != null && def2.MatchTrditionType(tradition_type))
				{
					if (text != "")
					{
						text += "{p}";
					}
					text += $"    {'{'}{def2.id}.name{'}'}";
				}
			}
			if (text == "")
			{
				return Value.Null;
			}
			return "@" + text;
		}
		case "icon":
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
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override void Run()
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom != null && args != null && args.Count >= 1)
		{
			string id = args[0].String();
			Tradition.Def def = base.game.defs.Get<Tradition.Def>(id);
			if (def != null)
			{
				int slot_idx = ((args.Count >= 2) ? args[1].Int() : (-1));
				Resource cost = GetCost();
				kingdom.AddTradition(def, 1, slot_idx);
				kingdom.OnAddTraditionAnalytics(def, cost);
			}
		}
	}
}
