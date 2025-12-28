using System.Collections.Generic;

namespace Logic;

public class ImportGoodAction : Action
{
	public ImportGoodAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ImportGoodAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_a_mission";
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

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		List<Vars> list = new List<Vars>(possibleTargets.Count);
		foreach (Value possibleTarget in possibleTargets)
		{
			Vars vars = new Vars(possibleTarget);
			vars.Set("argument_type", "Goods");
			vars.Set("owner", base.own_character);
			vars.Set("import_cost", GetImportCost(possibleTarget));
			vars.Set("kingdom", kingdom);
			vars.Set("rightTextKey", "ImportGoodAction.upkeep_text");
			list.Add(vars);
		}
		return list;
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

	private Resource GetImportCost(string good_name)
	{
		return base.own_character.CalcImportGoodUpkeep(new Character.ImportedGood
		{
			name = good_name,
			discount = 0f
		});
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "import_cost"))
		{
			if (key == "validate_prompt_text" && (args == null || (args.Count > 0 && args[0] == Value.Unknown)))
			{
				DT.Field field = def.field?.FindChild("validate_prompts");
				if (field != null)
				{
					return new Value(field.FindChild("ok"));
				}
			}
			return base.GetVar(key, vars, as_value);
		}
		if (args == null || args.Count < 1 || args[0] == Value.Unknown)
		{
			return Value.Unknown;
		}
		string good_name = args[0].String();
		return GetImportCost(good_name);
	}

	public override void Run()
	{
		Character character = base.own_character;
		Kingdom kingdom = own_kingdom;
		if (character == null || kingdom == null || args == null || args.Count < 1)
		{
			return;
		}
		string text = args[0].String();
		if (character.ImportGood(text, args[1]) && kingdom.is_player && (kingdom.player_goods_imported_before == null || !kingdom.player_goods_imported_before.Contains(text)))
		{
			if (kingdom.player_goods_imported_before == null)
			{
				kingdom.player_goods_imported_before = new HashSet<string>();
			}
			kingdom.player_goods_imported_before.Add(text);
			character.NotifyListeners("player_importing_good_first_time", text);
		}
	}
}
