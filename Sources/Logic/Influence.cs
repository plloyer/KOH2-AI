using System.Collections.Generic;

namespace Logic;

public class Influence
{
	public class Def : Logic.Def
	{
		public float princess_influence = 75f;

		public float queen_influence = 200f;

		public float vassal_multiplier = 1.5f;

		public float diplomat_addition = 0.15f;

		public List<float> distance_multipliers = new List<float>();

		public List<float> religion_multipliers = new List<float>();

		public float min;

		public float max;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			princess_influence = field.GetFloat("princess_influence");
			queen_influence = field.GetFloat("queen_influence");
			vassal_multiplier = field.GetFloat("vassal_multiplier");
			diplomat_addition = field.GetFloat("diplomat_addition");
			DT.Field field2 = field.FindChild("distance_multipliers");
			for (int i = 0; i < field2.NumValues(); i++)
			{
				distance_multipliers.Add(field2.Value(i));
			}
			DT.Field field3 = field.FindChild("religion_multipliers");
			for (int j = 0; j < field3.NumValues(); j++)
			{
				religion_multipliers.Add(field3.Value(j));
			}
			min = field.GetFloat("min");
			max = field.GetFloat("max");
			return true;
		}
	}
}
