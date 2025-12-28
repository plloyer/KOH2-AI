using System;
using System.Collections.Generic;

namespace Logic;

public class FamousPerson
{
	public class Def : Logic.Def
	{
		public string name;

		public Character.Sex sex;

		public List<DT.Field> skills = new List<DT.Field>();

		public BuildPrerqusite require;

		public int gp_min_skills = 1;

		public int gp_max_skills = 3;

		public float migrate_time_min = 60f;

		public float migrate_time_max = 120f;

		public int year_born;

		public float chance_die_pillage = 15f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			name = field.GetString("name");
			PerLevelValues perLevelValues = PerLevelValues.Parse<string>(field.FindChild("skills"));
			for (int i = 0; i < perLevelValues.items.Count; i++)
			{
				skills.Add(game.dt.Find(string.Concat(perLevelValues.items[i].value, "Skill")));
			}
			require = BuildPrerqusite.BuildUnitPrerqusite(this, game);
			try
			{
				sex = (Character.Sex)Enum.Parse(typeof(Character.Sex), field.GetString("sex"));
			}
			catch
			{
				game.Log("Creating character with invalid sex - " + base.field.key);
			}
			gp_min_skills = field.GetInt("gp_min_skills", null, gp_min_skills);
			gp_max_skills = field.GetInt("gp_max_skills", null, gp_max_skills);
			migrate_time_min = field.GetFloat("migrate_time_min", null, migrate_time_min);
			migrate_time_max = field.GetFloat("migrate_time_max", null, migrate_time_max);
			year_born = field.GetInt("year_born", null, year_born);
			chance_die_pillage = field.GetFloat("chance_die_pillage", null, chance_die_pillage);
			return true;
		}
	}
}
