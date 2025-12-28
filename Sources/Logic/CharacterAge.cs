using System;

namespace Logic;

public class CharacterAge
{
	public class Def : Logic.Def
	{
		public float age_multiplier = 1f;

		public float die_chance_perc = 5f;

		public float die_check_interval_min = 60f;

		public float die_check_interval_max = 90f;

		public int age_infant = 300;

		public int age_child = 600;

		public int age_juvenile = 600;

		public int age_young = 1200;

		public int age_adult = 3300;

		public int age_old = 1200;

		public int age_venerable_min = 300;

		public int age_venerable_max = 1200;

		public float initial_automarriage_cooldown_min = 1800f;

		public float initial_automarriage_cooldown_max = 1830f;

		public float marriage_check_interval = 120f;

		public float child_birth_check_rate_min = 80f;

		public float child_birth_check_rate_max = 100f;

		public float child_birth_min_time_after_marriage_or_birth = 600f;

		public float child_birth_max_time_after_marriage_or_birth = 650f;

		public float child_birth_chance_perc = 15f;

		public float[] marriage_chance_perc = new float[7] { 0f, 0f, 0f, 20f, 20f, 20f, 20f };

		public DT.Field die_chance_interval_field;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			age_multiplier = field.GetFloat("age_multiplier", null, age_multiplier);
			die_chance_perc = field.GetFloat("die_chance_perc", null, die_chance_perc);
			initial_automarriage_cooldown_min = field.GetFloat("initial_automarriage_cooldown_min", null, initial_automarriage_cooldown_min);
			initial_automarriage_cooldown_max = field.GetFloat("initial_automarriage_cooldown_max", null, initial_automarriage_cooldown_max);
			marriage_check_interval = field.GetFloat("marriage_check_interval", null, marriage_check_interval);
			child_birth_check_rate_min = field.GetFloat("child_birth_check_rate_min", null, child_birth_check_rate_min);
			child_birth_check_rate_max = field.GetFloat("child_birth_check_rate_max", null, child_birth_check_rate_max);
			child_birth_min_time_after_marriage_or_birth = field.GetFloat("child_birth_min_time_after_marriage_or_birth", null, child_birth_min_time_after_marriage_or_birth);
			child_birth_max_time_after_marriage_or_birth = field.GetFloat("child_birth_max_time_after_marriage_or_birth", null, child_birth_max_time_after_marriage_or_birth);
			child_birth_chance_perc = field.GetFloat("child_birth_chance_perc", null, child_birth_chance_perc);
			LoadMarriageChances(field.FindChild("marriage_chance_perc"), ref marriage_chance_perc);
			age_infant = field.GetInt("age_infant", null, age_infant);
			age_child = field.GetInt("age_child", null, age_child);
			age_juvenile = field.GetInt("age_juvenile", null, age_juvenile);
			age_young = field.GetInt("age_young", null, age_young);
			age_adult = field.GetInt("age_adult", null, age_adult);
			age_old = field.GetInt("age_old", null, age_old);
			DT.Field field2 = field.FindChild("age_venerable");
			if (field2 != null)
			{
				age_venerable_min = field2.Value(0).int_val;
				age_venerable_max = field2.Value(1).int_val;
			}
			DT.Field field3 = field.FindChild("die_check_interval");
			if (field3 != null)
			{
				die_check_interval_min = field3.Value(0).int_val;
				die_check_interval_max = field3.Value(1).int_val;
			}
			die_check_interval_max = Math.Max(die_check_interval_max, die_check_interval_min);
			return true;
		}

		private void LoadMarriageChances(DT.Field field, ref float[] chances)
		{
			int num = 7;
			if (chances == null)
			{
				chances = new float[num];
			}
			else if (chances.Length < num)
			{
				Array.Resize(ref chances, num);
			}
			for (int i = 0; i < num; i++)
			{
				Value value = field.Value(i);
				if (value.is_valid)
				{
					if (value.type == Value.Type.Int)
					{
						chances[i] = value.int_val;
					}
					if (value.type == Value.Type.Float)
					{
						chances[i] = value.float_val;
					}
				}
			}
		}
	}
}
