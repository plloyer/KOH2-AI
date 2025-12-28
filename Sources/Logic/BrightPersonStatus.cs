using System.Collections.Generic;

namespace Logic;

public class BrightPersonStatus : Status
{
	public new class FullData : Status.FullData
	{
		public List<string> skill_def_keys = new List<string>();

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is BrightPersonStatus brightPersonStatus))
			{
				return false;
			}
			if (skill_def_keys.Count > 0)
			{
				skill_def_keys.Clear();
			}
			foreach (string skill_def_key in brightPersonStatus.skill_def_keys)
			{
				skill_def_keys.Add(skill_def_key);
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			int count = skill_def_keys.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteStr(skill_def_keys[i], "skill_def_key_", i);
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			if (skill_def_keys.Count > 0)
			{
				skill_def_keys.Clear();
			}
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				skill_def_keys.Add(ser.ReadStr("skill_def_key_", i));
			}
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is BrightPersonStatus brightPersonStatus))
			{
				return false;
			}
			if (brightPersonStatus.skill_def_keys.Count > 0)
			{
				brightPersonStatus.skill_def_keys.Clear();
			}
			for (int i = 0; i < skill_def_keys.Count; i++)
			{
				brightPersonStatus.skill_def_keys.Add(skill_def_keys[i]);
			}
			return true;
		}
	}

	public HashSet<string> skill_def_keys = new HashSet<string>();

	public BrightPersonStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new BrightPersonStatus(def);
	}

	public void AddSkill(string skill)
	{
		skill_def_keys.Add(skill);
		owner.SendState<StatusesState>();
	}

	public void RemoveSkill(string skill)
	{
		skill_def_keys.Add(skill);
		owner.SendState<StatusesState>();
	}

	public bool CanTeachSkill(string skill_def_key)
	{
		if (skill_def_keys.Contains(skill_def_key))
		{
			return true;
		}
		if (owner is Character { skills: not null } character)
		{
			for (int i = 0; i < character.skills.Count; i++)
			{
				if (character.skills[i]?.def?.field?.key == skill_def_key)
				{
					return true;
				}
			}
		}
		return false;
	}

	public List<Skill.Def> GetSkills()
	{
		if (!(owner is Character character))
		{
			return null;
		}
		if (!((skill_def_keys != null && skill_def_keys.Count > 0) & (character.skills != null && character.skills.Count > 0)))
		{
			return null;
		}
		List<Skill.Def> list = new List<Skill.Def>();
		for (int i = 0; i < character.skills.Count; i++)
		{
			list.Add(character.skills[i].def);
		}
		foreach (string skill_def_key in skill_def_keys)
		{
			Skill.Def def = game.defs.Get<Skill.Def>(skill_def_key);
			if (def != null)
			{
				list.Add(def);
			}
		}
		return list;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "has_skills")
		{
			return skill_def_keys.Count > 0;
		}
		return base.GetVar(key, vars, as_value);
	}
}
