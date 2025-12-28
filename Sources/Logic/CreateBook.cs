using System.Collections.Generic;

namespace Logic;

public class Book : Object
{
	public enum Type
	{
		Skill,
		Idea,
		Unique
	}

	public class Def : Logic.Def
	{
		public string name;

		public string title;

		public Type type = Type.Unique;

		public Skill.Def skill;

		public int price;

		public int effects_count;

		public bool CanApplySkill(Character c, Skill replace_skill = null)
		{
			if (skill == null)
			{
				return false;
			}
			if (replace_skill != null)
			{
				if (replace_skill.def == skill)
				{
					return false;
				}
				return true;
			}
			if (c.GetSkill(skill.name) != null)
			{
				return false;
			}
			if (!c.CanLearnNewSkills())
			{
				return false;
			}
			return true;
		}

		public int CountApplicableEffects(Character c)
		{
			int num = 0;
			if (CanApplySkill(c))
			{
				num++;
			}
			return num;
		}

		public void ApplyTo(Character c, Skill replace_skill = null)
		{
			if (CanApplySkill(c, replace_skill))
			{
				if (replace_skill != null)
				{
					c.ReplaceSkill(replace_skill, skill);
				}
				else
				{
					c.AddSkill(skill);
				}
			}
		}

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			name = field.Path();
			title = field.Path() + ".title";
			price = field.GetInt("price");
			return true;
		}

		public override bool Validate(Game game)
		{
			if (IsBase())
			{
				GenerateSkillDefs(game);
				GenerateIdeaDefs(game);
				return true;
			}
			if (base.field == null)
			{
				return true;
			}
			DT.Field field = base.field.GetRef("skill");
			if (field != null)
			{
				skill = field.def?.def as Skill.Def;
				if (skill != null)
				{
					AddEffect(Type.Unique);
				}
				else
				{
					Game.Log(base.field.Path(include_file: true) + ": invalid skill def", Game.LogType.Error);
				}
			}
			return true;
		}

		private bool Find(Skill.Def skill_def, Defs.Registry reg)
		{
			if (reg == null)
			{
				return false;
			}
			foreach (KeyValuePair<string, Logic.Def> def2 in reg.defs)
			{
				if (def2.Value is Def { type: Type.Skill } def && def.skill == skill_def)
				{
					return true;
				}
			}
			return false;
		}

		private void GenerateSkillDefs(Game game)
		{
			Defs.Registry registry = game.defs.Get(typeof(Def));
			if (registry == null)
			{
				return;
			}
			Defs.Registry registry2 = game.defs.Get(typeof(Skill.Def));
			if (registry2 == null)
			{
				return;
			}
			DT.Field field = base.field.FindChild("skill_titles");
			if (field == null || field.children == null)
			{
				return;
			}
			List<string> list = new List<string>();
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!(field2.key == ""))
				{
					list.Add(field2.Path());
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<string, Logic.Def> def in registry2.defs)
			{
				if (def.Value is Skill.Def skill_def && !Find(skill_def, registry))
				{
					GenerateSkillDef(game, skill_def, list[game.Random(0, list.Count)], registry);
				}
			}
		}

		private void GenerateSkillDef(Game game, Skill.Def skill_def, string title, Defs.Registry books)
		{
			string key = skill_def.name + "Book";
			if (!books.defs.ContainsKey(key))
			{
				Def def = new Def();
				def.name = key;
				def.title = title;
				def.type = Type.Skill;
				def.skill = skill_def;
				def.effects_count = 1;
				def.price = game.defs.Find<Def>("Book").price;
				books.defs.Add(def.name, def);
			}
		}

		private void GenerateIdeaDefs(Game game)
		{
			if (game.defs.Get(typeof(Def)) == null)
			{
				return;
			}
			DT.Field field = base.field.FindChild("idea_titles");
			if (field == null || field.children == null)
			{
				return;
			}
			List<string> list = new List<string>();
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!(field2.key == ""))
				{
					list.Add(field2.Path());
				}
			}
			_ = list.Count;
		}

		public override string ToString()
		{
			return type.ToString() + " " + name;
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return title;
		}

		private void AddEffect(Type type)
		{
			if (effects_count == 0)
			{
				this.type = type;
			}
			else
			{
				this.type = Type.Unique;
			}
			effects_count++;
		}
	}

	public delegate Book CreateBook(Def def, Kingdom owner, int copies = 1);

	public Def def;

	public Kingdom kingdom;

	public int copies = 1;

	public Book(Def def, Kingdom owner, int copies = 1)
		: base(owner.game)
	{
		this.def = def;
		kingdom = owner;
		this.copies = copies;
	}

	public static Book Create(Def def, Kingdom owner, int copies = 1)
	{
		return new Book(def, owner, copies);
	}

	public override string ToString()
	{
		return copies + "x " + def.ToString();
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return "Book.title_copies";
	}

	public int GetPrice()
	{
		return def.price;
	}
}
