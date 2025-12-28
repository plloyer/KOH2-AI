using System.Collections.Generic;

namespace Logic;

public class LearnNewSkillAction : Action
{
	public LearnNewSkillAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new LearnNewSkillAction(owner as Character, def);
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
		if (!character.CanLearnNewSkills())
		{
			return "cannot_learn";
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
		List<Skill.Def> list = base.own_character?.GetNewSkillOptions(null);
		if (list == null)
		{
			return null;
		}
		List<Value> list2 = new List<Value>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Skill.Def def = list[i];
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
			Skill.Def def = base.game.defs.Get<Skill.Def>(possibleTarget);
			if (def != null)
			{
				Vars vars = new Vars(def);
				vars.Set("owner", base.own_character);
				vars.Set("name", def.id + ".name");
				vars.Set("learn_cost", def.GetLearnCost(base.own_character));
				vars.Set("localization_key", "LearnNewSkillAction.picker_text");
				vars.Set("rightTextKey", "LearnNewSkillAction.learn_cost_text");
				list.Add(vars);
			}
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

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "learn_cost")
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
			return def.GetLearnCost(base.own_character);
		}
		return base.GetVar(key, vars, as_value);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null && args != null && args.Count >= 1)
		{
			string id = args[0].String();
			Skill.Def def = base.game.defs.Get<Skill.Def>(id);
			if (def != null)
			{
				character.AddSkill(def);
			}
		}
	}
}
