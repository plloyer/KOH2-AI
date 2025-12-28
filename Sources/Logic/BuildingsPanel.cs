using System.Collections.Generic;

namespace Logic;

public class BuildingsPanel
{
	public class Def : Logic.Def
	{
		public class BuildingInfo
		{
			public DT.Field field;

			public Building.Def def;

			public Point pos;

			public Point anchors_min;

			public Point anchors_max;

			public List<Building.Def> prerequisites;

			public List<Building.Def> prerequisites_or;
		}

		public struct ArrowInfo
		{
			public DT.Field field;

			public DT.Field prefab_field;

			public float xmin;

			public float xmax;

			public float y;

			public Point anchors_min;

			public Point anchors_max;
		}

		public int cols;

		public int rows;

		public float[] icon_margins;

		public Point ofs;

		public List<BuildingInfo> buildings;

		public List<ArrowInfo> arrows;

		public BuildingInfo FindPart(Building.Def def)
		{
			if (def == null)
			{
				return null;
			}
			if (buildings == null)
			{
				return null;
			}
			for (int i = 0; i < buildings.Count; i++)
			{
				BuildingInfo buildingInfo = buildings[i];
				if (buildingInfo.def == def)
				{
					return buildingInfo;
				}
			}
			return null;
		}

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			cols = field.GetInt("cols");
			rows = field.GetInt("rows");
			LoadMargins(field.FindChild("icon_margins"));
			LoadBuildings(game, field.FindChild("buildings"));
			LoadArrows(field.FindChild("arrows"));
			return true;
		}

		private void LoadMargins(DT.Field f)
		{
			icon_margins = new float[4];
			if (f != null)
			{
				icon_margins[0] = f.GetFloat("left");
				icon_margins[1] = f.GetFloat("bottom");
				icon_margins[2] = f.GetFloat("right");
				icon_margins[3] = f.GetFloat("top");
			}
		}

		private void LoadBuildings(Game game, DT.Field f)
		{
			buildings = new List<BuildingInfo>();
			if (f == null || f.children == null)
			{
				return;
			}
			float num = 1f / (float)cols;
			float num2 = 1f / (float)rows;
			Point point = new Point(0f, 1f);
			float num3 = 1f;
			float num4 = 0f;
			for (int i = 0; i < f.children.Count; i++)
			{
				DT.Field field = f.children[i];
				if (string.IsNullOrEmpty(field.key))
				{
					continue;
				}
				BuildingInfo buildingInfo = new BuildingInfo();
				buildingInfo.field = field;
				buildingInfo.def = game?.defs.Get<Building.Def>(field.key);
				buildingInfo.pos = field.Point();
				if (buildingInfo.pos == Point.Invalid)
				{
					buildingInfo.pos.x = point.x + 1f;
					if (buildingInfo.pos.x > (float)cols)
					{
						buildingInfo.pos.x = 1f;
						buildingInfo.pos.y = point.y + 1f;
					}
					else
					{
						buildingInfo.pos.y = point.y;
					}
				}
				buildingInfo.anchors_min.x = (buildingInfo.pos.x - 1f + icon_margins[0]) * num;
				buildingInfo.anchors_min.y = (buildingInfo.pos.y - 1f + icon_margins[1]) * num2;
				buildingInfo.anchors_max.x = (buildingInfo.pos.x - icon_margins[2]) * num;
				buildingInfo.anchors_max.y = (buildingInfo.pos.y - icon_margins[3]) * num2;
				if (buildingInfo.anchors_min.x < num3)
				{
					num3 = buildingInfo.anchors_min.x;
				}
				if (buildingInfo.anchors_max.x > num4)
				{
					num4 = buildingInfo.anchors_max.x;
				}
				buildingInfo.prerequisites = LoadPrerequisites(game, field.FindChild("prerequisites", null, allow_base: false));
				buildingInfo.prerequisites_or = LoadPrerequisites(game, field.FindChild("prerequisites_or", null, allow_base: false));
				buildings.Add(buildingInfo);
				point = buildingInfo.pos;
			}
			ofs.x = 0.5f - (num3 + num4) * 0.5f;
		}

		private List<Building.Def> LoadPrerequisites(Game game, DT.Field f)
		{
			if (game == null || f == null)
			{
				return null;
			}
			if (f.children == null)
			{
				return null;
			}
			List<Building.Def> list = new List<Building.Def>();
			for (int i = 0; i < f.children.Count; i++)
			{
				DT.Field field = f.children[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					Building.Def def = game.defs.Get<Building.Def>(field.key);
					if (def != null)
					{
						list.Add(def);
					}
				}
			}
			return list;
		}

		private void LoadArrows(DT.Field f)
		{
			arrows = new List<ArrowInfo>();
			if (f == null || f.children == null)
			{
				return;
			}
			float num = 1f / (float)cols;
			float num2 = 1f / (float)rows;
			for (int i = 0; i < f.children.Count; i++)
			{
				DT.Field field = f.children[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					ArrowInfo item = default(ArrowInfo);
					item.field = field;
					item.prefab_field = field.FindChild("prefab");
					item.y = field.GetFloat("row");
					item.xmin = field.GetFloat("left");
					item.xmax = field.GetFloat("right");
					item.anchors_min.x = (-0.5f + item.xmin) * num;
					item.anchors_min.y = (-0.5f + item.y - icon_margins[3]) * num2;
					item.anchors_max.x = (-0.5f + item.xmax) * num;
					item.anchors_max.y = (-0.5f + item.y + icon_margins[1]) * num2;
					arrows.Add(item);
				}
			}
		}

		public override bool Validate(Game game)
		{
			if (buildings == null)
			{
				return true;
			}
			for (int i = 0; i < buildings.Count; i++)
			{
				BuildingInfo bi = buildings[i];
				ValidatePrerequisites(bi);
			}
			return true;
		}

		private void ValidatePrerequisites(BuildingInfo bi)
		{
			ValidatePrerequisites(bi, bi.prerequisites);
			ValidatePrerequisites(bi, bi.prerequisites_or);
			if (bi.prerequisites == null && bi.prerequisites_or == null)
			{
				GeneratePrerequisites(bi);
			}
		}

		private void ValidatePrerequisites(BuildingInfo bi, List<Building.Def> preqs)
		{
			if (preqs != null)
			{
				for (int i = 0; i < preqs.Count; i++)
				{
					Building.Def preq = preqs[i];
					ValidatePrerequisite(bi, preq);
				}
			}
		}

		private void ValidatePrerequisite(BuildingInfo bi, Building.Def preq)
		{
			BuildingInfo buildingInfo = FindPart(preq);
			if (buildingInfo == null)
			{
				Game.Log(bi.field?.Path(include_file: true) + ": prerequisite '" + preq.id + "' not found in panel " + base.id, Game.LogType.Warning);
			}
			else if (bi.pos.y != buildingInfo.pos.y + 1f)
			{
				Game.Log($"{bi.field?.Path(include_file: true)}: prerequisite '{buildingInfo.field?.key}' at {buildingInfo.pos} is not on the row bellow {bi.field?.key} at {bi.pos}", Game.LogType.Warning);
			}
		}

		private void GeneratePrerequisites(BuildingInfo bi)
		{
			if (buildings == null || bi.pos.y <= 1f)
			{
				return;
			}
			DT.Field obj = bi.field.FindChild("prerequisites", null, allow_base: false);
			DT.Field field = bi.field.FindChild("prerequisites_or", null, allow_base: false);
			if (obj != null && field != null)
			{
				Game.Log(bi.field.Path(include_file: true) + ": both 'prerequisites' and 'prerequisites_or' are present for auto prerequisite generation", Game.LogType.Warning);
			}
			if (obj == null)
			{
				obj = field;
			}
			bool flag = obj?.String() == "all";
			List<Building.Def> list = null;
			for (int i = 0; i < buildings.Count; i++)
			{
				BuildingInfo buildingInfo = buildings[i];
				if (buildingInfo.def == null || buildingInfo.pos.y != bi.pos.y - 1f)
				{
					continue;
				}
				float num = bi.pos.x - buildingInfo.pos.x;
				if (flag || (!(num <= -1f) && !(num >= 1f)))
				{
					if (list == null)
					{
						list = new List<Building.Def>();
					}
					list.Add(buildingInfo.def);
				}
			}
			if (list != null)
			{
				if (field != null)
				{
					bi.prerequisites_or = list;
				}
				else
				{
					bi.prerequisites = list;
				}
			}
		}
	}
}
