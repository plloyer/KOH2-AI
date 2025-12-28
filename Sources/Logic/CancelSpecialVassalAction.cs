using System.Collections.Generic;

namespace Logic;

public class CancelSpecialVassalAction : Action
{
	public CancelSpecialVassalAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CancelSpecialVassalAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "dead";
		}
		if (own_kingdom == null)
		{
			return "no_kingodm";
		}
		if (character.sex != Character.Sex.Male)
		{
			return "not_a_male";
		}
		if (!own_kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		if (!character.IsKing())
		{
			return "not_king";
		}
		if (character.IsPope())
		{
			return "is_pope";
		}
		if (character?.GetArmy() != null)
		{
			return "leading_army";
		}
		return "ok";
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value> list = new List<Value>(own_kingdom.vassalStates.Count);
		if (args == null)
		{
			if (own_kingdom.vassalStates.Count <= 0)
			{
				return null;
			}
			for (int i = 0; i < own_kingdom.vassalStates.Count; i++)
			{
				Kingdom kingdom = own_kingdom.vassalStates[i];
				if (kingdom.IsVassal() && kingdom.vassalage != null && (kingdom.vassalage.def.type == Vassalage.Type.Scuttage || kingdom.vassalage.def.type == Vassalage.Type.March || kingdom.vassalage.def.type == Vassalage.Type.SacredLand))
				{
					list.Add(kingdom);
				}
			}
		}
		if (list.Count > 0)
		{
			return new List<Value>[1] { list };
		}
		return null;
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		List<Vars> list = new List<Vars>(possibleTargets.Count);
		if (arg_type == 0)
		{
			foreach (Value possibleTarget in possibleTargets)
			{
				Vars vars = new Vars(possibleTarget);
				vars.Set("argument_type", "Kingdom");
				vars.Set("owner", base.own_character);
				vars.Set("Kingdom", possibleTarget.Get<Kingdom>().Name);
				list.Add(vars);
			}
		}
		return list;
	}

	public override bool ValidateArg(Value value, int def_type)
	{
		if (!NeedsArgs())
		{
			return true;
		}
		if (value == Value.Unknown || value == Value.Null)
		{
			return false;
		}
		if (def_type >= def.arg_types.Count)
		{
			return false;
		}
		Object obj = value.Get<Object>();
		string text = def.arg_types[def_type];
		if (text == "Kingdom")
		{
			if (!(obj is Kingdom))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public override string CheckEnabled()
	{
		if (base.own_character.prison_kingdom != null)
		{
			return "imprisoned";
		}
		return "ok";
	}

	public override void Run()
	{
		if (args != null && args.Count >= 1)
		{
			args[0].Get<Kingdom>().ChangeVassalageType(Vassalage.Type.Vassal);
		}
	}
}
