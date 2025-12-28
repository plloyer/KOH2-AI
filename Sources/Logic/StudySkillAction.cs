using System.Collections.Generic;

namespace Logic;

public class StudySkillAction : Action
{
	public StudySkillAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new StudySkillAction(owner as Character, def);
	}

	public override bool NeedsTarget()
	{
		return base.NeedsTarget();
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.prison_kingdom != null)
		{
			return "in_prison";
		}
		if (GetPossibleArgs() == null)
		{
			return "_no_targets";
		}
		if (character.statuses.Find<BrightPersonStatus>() == null)
		{
			return "not_a_bright_character";
		}
		if (IsOverLimit(character))
		{
			return "skill_limit";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		return true;
	}

	public override bool ValidateArgs()
	{
		return base.ValidateArgs();
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		List<Vars> list = new List<Vars>(possibleTargets.Count);
		foreach (Value possibleTarget in possibleTargets)
		{
			Vars vars = new Vars();
			vars.Set("icon_key", possibleTarget.String() + ".name");
			vars.Set("localization_key", possibleTarget.String() + ".name");
			list.Add(vars);
		}
		return list;
	}

	public override Value GetArg(int idx, IVars vars)
	{
		if (args == null)
		{
			return Value.Unknown;
		}
		if (idx < 0 || idx >= args.Count)
		{
			return Value.Unknown;
		}
		Value result = args[idx];
		if (result.is_string)
		{
			Skill.Def def = base.game.defs.Get<Skill.Def>(result.String());
			if (def != null)
			{
				return new Value(def);
			}
		}
		return result;
	}

	public override List<Value>[] GetPossibleArgs()
	{
		BrightPersonStatus status = base.own_character.statuses.Find<BrightPersonStatus>();
		List<Value>[] array = new List<Value>[def.arg_types.Count];
		int arg;
		for (arg = 0; arg < def.arg_types.Count; arg++)
		{
			List<Value> args = null;
			string text = def.arg_types[arg];
			if (text == "skill")
			{
				Kingdom kingdom = GetKingdom();
				if (kingdom == null)
				{
					return null;
				}
				if (kingdom.court != null)
				{
					for (int i = 0; i < kingdom.court.Count; i++)
					{
						Character character = kingdom.court[i];
						if (character != null)
						{
							EvaluateTeacher(character);
						}
					}
				}
				if (kingdom.royalFamily != null && kingdom.royalFamily.Children != null && kingdom.royalFamily.Children.Count > 0)
				{
					for (int j = 0; j < kingdom.royalFamily.Children.Count; j++)
					{
						EvaluateTeacher(kingdom.royalFamily.Children[j]);
					}
				}
			}
			array[arg] = args;
			void EvaluateTeacher(Character c)
			{
				if (c != null && c != base.own_character && c.skills != null && c.skills.Count != 0)
				{
					for (int k = 0; k < c.skills.Count; k++)
					{
						Skill skill = c.skills[k];
						if (skill != null && (status == null || !status.CanTeachSkill(skill.def.field.key)))
						{
							AddArg(ref args, skill.def.field.key, arg);
						}
					}
				}
			}
		}
		return array;
	}

	public override void Prepare()
	{
		base.Prepare();
	}

	public override void Run()
	{
		string text = args[0].String();
		if (base.game.defs.Get<Skill.Def>(text) != null)
		{
			base.own_character.statuses.Find<BrightPersonStatus>().AddSkill(text);
		}
		base.Run();
	}

	private bool IsOverLimit(Character character, BrightPersonStatus status = null)
	{
		if (character == null)
		{
			return false;
		}
		if (status == null)
		{
			status = base.own_character.statuses.Find<BrightPersonStatus>();
		}
		if (status == null)
		{
			return false;
		}
		int num = def.field.GetInt("max_skill_cnt");
		return status.skill_def_keys.Count >= num;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "has_literacy"))
		{
			if (key == "has_learning")
			{
				return HasSkill(base.own_character, "LearningSkill");
			}
			return base.GetVar(key, vars, as_value);
		}
		return HasSkill(base.own_character, "LiteracySkill");
	}

	private bool HasSkill(Character c, string skill_id)
	{
		if (c == null)
		{
			return false;
		}
		if (c.skills == null)
		{
			return false;
		}
		int i = 0;
		for (int count = c.skills.Count; i < count; i++)
		{
			if (c.skills[i] != null && c.skills[i].def.field.key == skill_id)
			{
				return true;
			}
		}
		return false;
	}
}
