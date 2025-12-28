using System.Collections.Generic;

namespace Logic;

public class ProvinceFeature
{
	public class Def : Logic.Def
	{
		public Point area_min_norm;

		public Point area_max_norm;

		public int max_PF_per_province = 8;

		public int min_distance_between = -1;

		public bool show_in_political_view = true;

		public bool show_in_castle = true;

		public float spawn_chance_base = 30f;

		public List<(string, float)> spawn_chances_per_feature;

		public bool requre_costal;

		public bool requre_distant_port;

		public int max_total_count = 100;

		public int sort_id;

		public bool special;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			max_PF_per_province = field.GetInt("max_PF_per_province", null, max_PF_per_province);
			min_distance_between = field.GetInt("min_distance_between");
			DT.Field field2 = field.FindChild("area");
			if (field2 != null)
			{
				field2.NumValues();
				area_min_norm = new Point(field2.Float(0), field2.Float(1));
				area_max_norm = new Point(field2.Float(2), field2.Float(3));
			}
			show_in_political_view = field.GetBool("show_in_political_view", null, show_in_political_view);
			show_in_castle = field.GetBool("show_in_castle", null, show_in_castle);
			LoadSpawnChances(field.FindChild("spawn_chance"));
			requre_costal = field.GetBool("requre_costal", null, requre_costal);
			max_total_count = field.GetInt("max_total_count", null, max_total_count);
			sort_id = field.GetInt("sort_id", null, sort_id);
			special = field.GetBool("special", null, special);
			requre_distant_port = field.GetBool("requre_distant_port", null, requre_distant_port);
			return true;
		}

		private void LoadSpawnChances(DT.Field f)
		{
			spawn_chances_per_feature?.Clear();
			if (f == null)
			{
				return;
			}
			spawn_chance_base = f.Float(null, spawn_chance_base);
			List<DT.Field> list = f.Children();
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field = list[i];
				if (string.IsNullOrEmpty(field.key))
				{
					continue;
				}
				float num = field.Float();
				if (num != 0f)
				{
					if (spawn_chances_per_feature == null)
					{
						spawn_chances_per_feature = new List<(string, float)>();
					}
					spawn_chances_per_feature.Add((field.key, num));
				}
			}
		}

		public float GetSpawnChance(Realm realm)
		{
			float num = spawn_chance_base;
			if (spawn_chances_per_feature != null && spawn_chances_per_feature.Count > 0)
			{
				for (int i = 0; i < spawn_chances_per_feature.Count; i++)
				{
					string item = spawn_chances_per_feature[i].Item1;
					if (realm.HasTag(item))
					{
						num += spawn_chances_per_feature[i].Item2;
					}
					if (item == "Coastal" && realm.HasCostalCastle())
					{
						num += spawn_chances_per_feature[i].Item2;
					}
					if (item == "Mountains" && realm.def.GetBool("has_mountains"))
					{
						num += spawn_chances_per_feature[i].Item2;
					}
				}
			}
			return num;
		}

		public bool ValidateArena(Point world_point, Point world_size)
		{
			if (area_min_norm.SqrLength() != 0f)
			{
				if (world_point.x / world_size.x < area_min_norm.x)
				{
					return false;
				}
				if (world_point.y / world_size.y < area_min_norm.y)
				{
					return false;
				}
			}
			if (area_max_norm.SqrLength() != 0f)
			{
				if (world_point.x / world_size.x > area_max_norm.x)
				{
					return false;
				}
				if (world_point.y / world_size.y > area_max_norm.y)
				{
					return false;
				}
			}
			return true;
		}
	}
}
