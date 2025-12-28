using System.Collections.Generic;

namespace Logic;

public class IncreaseSkillRankAction : Action
{
	public IncreaseSkillRankAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new IncreaseSkillRankAction(owner as Character, def);
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
		if (character.prison_kingdom != null)
		{
			return "in_prison";
		}
		string text = ValidateInCourt();
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
		List<Skill> list = base.own_character?.skills;
		if (list == null)
		{
			return null;
		}
		List<Value> list2 = new List<Value>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Skill.Def def = list[i]?.def;
			if (def != null)
			{
				list2.Add(def.id);
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
			Skill.Def def = base.game.defs.Get<Skill.Def>(possibleTarget);
			if (def != null)
			{
				Vars vars = new Vars(def);
				vars.Set("owner", base.own_character);
				vars.Set("name", def.id + ".name");
				vars.Set("upgarde_cost", def.GetUpgardeCost(base.own_character));
				vars.Set("localization_key", "IncreaseSkillRankAction.picker_text");
				vars.Set("rightTextKey", "IncreaseSkillRankAction.upgarde_cost_text");
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

	public override bool ValidateArgs()
	{
		if (args == null)
		{
			return false;
		}
		if (args.Count < 1)
		{
			return false;
		}
		if (!args[0].is_string)
		{
			return false;
		}
		string id = args[0].String();
		Skill.Def def = base.game.defs.Find<Skill.Def>(id);
		if (def == null)
		{
			return false;
		}
		return base.own_character.CanAddSkillRank(def);
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
		if (key == "upgarde_cost")
		{
			if (args == null || args.Count < 1)
			{
				return Value.Unknown;
			}
			string id = args[0].String();
			Skill.Def def = base.game.defs.Get<Skill.Def>(id);
			if (def == null)
			{
				return Value.Unknown;
			}
			return def.GetUpgardeCost(base.own_character);
		}
		return base.GetVar(key, vars, as_value);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character == null || base.own_character.skills == null || base.own_character.skills.Count == 0 || args == null || args.Count < 1)
		{
			return;
		}
		string id = args[0].String();
		Skill.Def def = base.game.defs.Get<Skill.Def>(id);
		if (def == null)
		{
			return;
		}
		for (int i = 0; i < base.own_character.skills.Count; i++)
		{
			Skill skill = base.own_character.skills[i];
			if (skill != null && skill.def == def)
			{
				character.AddSkillRank(skill);
				break;
			}
		}
	}
}
