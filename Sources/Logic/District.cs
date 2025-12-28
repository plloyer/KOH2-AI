using System.Collections.Generic;

namespace Logic;

public class District : BaseObject
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

			public string id => def?.id ?? field?.key ?? "<unknown>";

			public override string ToString()
			{
				return $"{id} at ({pos.x},{pos.y})";
			}
		}

		public struct ArrowInfo
		{
			public DT.Field field;

			public DT.Field prefab_field;

			public DT.Field condition_field;

			public Point pos;

			public float width;

			public Point anchors_min;

			public Point anchors_max;
		}

		public bool buildable = true;

		public List<Building.Def.RequirementInfo> requires;

		public List<Building.Def.RequirementInfo> requires_or;

		public Point icon_size;

		public Point icon_spacing;

		public float panel_min_width;

		public float panel_padding;

		public float panel_background;

		public bool panel_background_colorize;

		public DT.Field panel_border;

		public DT.Field panel_shadow;

		public List<BuildingInfo> buildings = new List<BuildingInfo>();

		public List<ArrowInfo> arrows;

		public Point panel_size;

		public Point grid_cell_size;

		public Point grid_ofs;

		public List<Building.Def> GetBuildingDefs()
		{
			if (buildings == null)
			{
				return null;
			}
			List<Building.Def> list = new List<Building.Def>();
			for (int i = 0; i < buildings.Count; i++)
			{
				Building.Def def = buildings[i].def;
				list.Add(def);
			}
			return list;
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"parent" => GetParent(), 
				"buildings" => new Value(buildings), 
				"building_defs" => new Value(GetBuildingDefs()), 
				"arrows" => new Value(arrows), 
				"requires" => new Value(requires), 
				"requires_or" => new Value(requires_or), 
				"buildable" => buildable, 
				"always_available" => IsAlwaysAvailable(), 
				"is_common" => IsCommon(), 
				"is_pf" => IsPF(), 
				"is_common_or_pf" => IsCommon() || IsPF(), 
				_ => base.GetVar(key, vars, as_value), 
			};
		}

		public BuildingInfo FindBuilding(string bdef_id)
		{
			if (buildings == null)
			{
				return null;
			}
			for (int i = 0; i < buildings.Count; i++)
			{
				BuildingInfo buildingInfo = buildings[i];
				if (buildingInfo.id == bdef_id)
				{
					return buildingInfo;
				}
			}
			return null;
		}

		public BuildingInfo FindBuilding(Building.Def bdef)
		{
			if (buildings == null)
			{
				return null;
			}
			for (int i = 0; i < buildings.Count; i++)
			{
				BuildingInfo buildingInfo = buildings[i];
				if (buildingInfo.def == bdef || buildingInfo.def?.variant_of == bdef)
				{
					return buildingInfo;
				}
			}
			return null;
		}

		public Building.Def GetParent()
		{
			return Logic.Def.Get<Building.Def>(base.field?.parent);
		}

		public bool IsUpgrades()
		{
			return GetParent() != null;
		}

		public bool IsCommon()
		{
			return base.id == "CommonDistrict";
		}

		public static Def GetCommon(Game game)
		{
			return game?.defs?.Get<Def>("CommonDistrict");
		}

		public bool IsPF()
		{
			return base.id == "PFDistrict";
		}

		public bool IsOptional()
		{
			if (IsUpgrades())
			{
				return true;
			}
			if (requires == null && requires_or == null)
			{
				return false;
			}
			return true;
		}

		public bool IsAlwaysAvailable()
		{
			if (requires != null || requires_or != null)
			{
				return false;
			}
			Building.Def parent = GetParent();
			if (parent != null && !parent.IsAlwaysAvailable())
			{
				return false;
			}
			return true;
		}

		public static Def GetPF(Game game)
		{
			return game?.defs?.Get<Def>("PFDistrict");
		}

		public bool IsBuildable(Castle c)
		{
			if (!buildable)
			{
				return false;
			}
			if (requires != null && requires.Count > 0)
			{
				for (int i = 0; i < requires.Count; i++)
				{
					Building.Def.RequirementInfo req = requires[i];
					if (!c.MayMeetRequirement(req))
					{
						return false;
					}
				}
			}
			if (requires_or != null && requires_or.Count > 0)
			{
				bool flag = false;
				for (int j = 0; j < requires_or.Count; j++)
				{
					Building.Def.RequirementInfo req2 = requires_or[j];
					if (c.MayMeetRequirement(req2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		public override bool Load(Game game)
		{
			LoadCommonParams(game);
			if (game != null)
			{
				LoadRequirements(game);
			}
			LoadBuildings(game);
			LoadArrows(game);
			return true;
		}

		public override bool Validate(Game game)
		{
			Building.Def.ValidateRequirements(game, requires, base.id);
			Building.Def.ValidateRequirements(game, requires_or, base.id);
			if (IsBase())
			{
				LoadPFDistrict(game);
			}
			return true;
		}

		private void LoadCommonParams(Game game)
		{
			DT.Field field = base.field;
			buildable = field.GetBool("buildable", null, buildable);
			icon_size = field.GetPoint("icon_size");
			icon_spacing = field.GetPoint("icon_spacing");
			panel_min_width = field.GetFloat("panel_min_width");
			panel_padding = field.GetFloat("panel_padding", null, icon_spacing.x);
			panel_background = field.GetFloat("panel_background");
			panel_background_colorize = field.GetBool("panel_background_colorize");
			panel_border = field.FindChild("panel_border");
			panel_shadow = field.FindChild("panel_shadow");
			grid_cell_size = icon_size + icon_spacing;
			grid_ofs = grid_cell_size / 2f;
		}

		private void LoadRequirements(Game game)
		{
			DT.Field field = base.field;
			requires = Building.Def.LoadRequirements(game, field.FindChild("requires", null, allow_base: false));
			requires_or = Building.Def.LoadRequirements(game, field.FindChild("requires_or", null, allow_base: false));
		}

		public static List<Building.Def> LoadPrerequisites(Game game, DT.Field f)
		{
			if (game == null || f == null)
			{
				return null;
			}
			List<DT.Field> list = f.Children();
			if (list == null || list.Count == 0)
			{
				return null;
			}
			List<Building.Def> list2 = new List<Building.Def>();
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field = list[i];
				if (field != null)
				{
					Building.Def def = game.defs.Get<Building.Def>(field.key);
					if (def != null)
					{
						list2.Add(def);
					}
				}
			}
			return list2;
		}

		private Point Grid2Pix(Point pt)
		{
			pt.x *= grid_cell_size.x;
			pt.y *= grid_cell_size.y;
			pt -= grid_ofs;
			return pt;
		}

		private Point Pix2UV(Point pt)
		{
			pt.x /= panel_size.x;
			pt.y /= panel_size.y;
			return pt;
		}

		private Point Grid2UV(Point pt)
		{
			pt = Grid2Pix(pt);
			pt = Pix2UV(pt);
			return pt;
		}

		private void LoadBuildings(Game game)
		{
			buildings.Clear();
			List<DT.Field> list = base.field.FindChild("buildings")?.Children();
			if (list == null || list.Count == 0)
			{
				if (game != null && !IsBase() && !IsPF() && base.field?.parent?.Type() == "file")
				{
					Game.Log(base.field.Path(include_file: true) + ": District '" + base.id + "' has no buildings", Game.LogType.Error);
				}
				return;
			}
			Point pos = new Point(0f, 1f);
			Point point = new Point(1f, 1f);
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field = list[i];
				if (string.IsNullOrEmpty(field.key))
				{
					continue;
				}
				Value value = field.Value();
				if (value.is_number)
				{
					pos.x = value.Float();
				}
				else if (value.obj_val is Point point2)
				{
					pos = point2;
				}
				else
				{
					if (value.is_valid)
					{
						Game.Log(field.Path(include_file: true) + ": Invalid value, must be point, number or empty", Game.LogType.Error);
					}
					pos.x += 1f;
				}
				if (pos.x > point.x)
				{
					point.x = pos.x;
				}
				if (pos.y > point.y)
				{
					point.y = pos.y;
				}
				BuildingInfo buildingInfo = new BuildingInfo();
				buildingInfo.field = field;
				buildingInfo.def = game?.defs.Get<Building.Def>(field.key);
				if (buildingInfo.def != null)
				{
					if (buildingInfo.def.districts == null)
					{
						buildingInfo.def.districts = new List<Def>();
					}
					buildingInfo.def.districts.Add(this);
				}
				buildingInfo.pos = pos;
				buildingInfo.prerequisites = LoadPrerequisites(game, field.FindChild("prerequisites", null, allow_base: false));
				buildingInfo.prerequisites_or = LoadPrerequisites(game, field.FindChild("prerequisites_or", null, allow_base: false));
				buildings.Add(buildingInfo);
			}
			panel_size = new Point((point.x + 0.5f) * grid_cell_size.x, (point.y + 0.5f) * grid_cell_size.y) - grid_ofs;
			Point point3 = icon_size / 2f;
			for (int j = 0; j < buildings.Count; j++)
			{
				BuildingInfo buildingInfo2 = buildings[j];
				Point point4 = Grid2Pix(buildingInfo2.pos);
				buildingInfo2.anchors_min = Pix2UV(point4 - point3);
				buildingInfo2.anchors_max = Pix2UV(point4 + point3);
			}
		}

		private void LoadArrows(Game game)
		{
			DT.Field field = base.field.FindChild("arrows");
			arrows = new List<ArrowInfo>();
			if (field == null || field.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!string.IsNullOrEmpty(field2.key))
				{
					ArrowInfo item = new ArrowInfo
					{
						field = field2,
						prefab_field = (field2.FindChild("prefab") ?? base.field.FindChild("arrow_prefabs.arrow")),
						condition_field = field2.FindChild("condition"),
						pos = field2.Point()
					};
					BuildingInfo buildingInfo = FindBuilding(field2.key);
					if (buildingInfo != null)
					{
						item.pos += buildingInfo.pos;
					}
					item.width = field2.GetFloat("width", null, 1f);
					Point pt = Grid2Pix(new Point(item.pos.x - item.width / 2f, item.pos.y));
					pt.y += icon_size.y / 2f;
					Point pt2 = Grid2Pix(new Point(item.pos.x + item.width / 2f, item.pos.y));
					pt2.y += icon_size.y / 2f + icon_spacing.y;
					item.anchors_min = Pix2UV(pt);
					item.anchors_max = Pix2UV(pt2);
					arrows.Add(item);
				}
			}
		}

		private void LoadPFDistrict(Game game)
		{
			Def pF = GetPF(game);
			if (pF.buildings.Count > 0)
			{
				return;
			}
			List<Building.Def> defs = game.defs.GetDefs<Building.Def>();
			for (int i = 0; i < defs.Count; i++)
			{
				Building.Def def = defs[i];
				if (def.districts == null && def.HasPFRequirement(count_settlements: false))
				{
					BuildingInfo buildingInfo = new BuildingInfo();
					buildingInfo.def = def;
					pF.buildings.Add(buildingInfo);
				}
			}
			Game.Log($"PF buildings: {pF.buildings.Count}", Game.LogType.Message);
		}
	}
}
