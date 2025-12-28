using System;
using System.Collections.Generic;
using System.Threading;

namespace Logic;

public class PathFinding : Object
{
	[Serializable]
	public class Settings
	{
		public float tile_size = 1f;

		public float max_radius;

		public byte max_slope = 30;

		public float slope_avoidance = 10f;

		public float slope_power = 2f;

		public float river_avoidance = 5f;

		public float coast_avoidance = 5f;

		public float occupied_ladder_mod = 3f;

		public float occupied_by_me_ladder_mod = 0.5f;

		public int enter_water_weight_mod = 255;

		public float road_stickiness = 1.25f;

		public float ocean_high_pf_weight_mod = 1.5f;

		public bool towns_passable = true;

		public byte base_weight = 2;

		public byte estimate_weight = 2;

		public byte costly_estimate_weight = 5;

		public int max_steps = 100000;

		public bool multithreaded = true;

		public int grid_range = 20;

		public bool expand_towns;

		public bool is_runtime;

		public float grid_tile_size = 16f;

		public float reserve_grid_size = -1f;

		public float reserve_grid_avoid_tile_dist_mod = 3f;

		public float reserve_grid_avoidance_weight;

		public float ai_threat_avoidance_mod = 2f;

		public bool use_lsa_low_level = true;

		public int map_bounds_width;

		public bool limit_low_steps_by_high_grid_size;

		public float[] weight_per_area_type;

		public static readonly PathData.PassableArea.Type[] area_types = new PathData.PassableArea.Type[9]
		{
			PathData.PassableArea.Type.Generic,
			PathData.PassableArea.Type.ForcedGround,
			PathData.PassableArea.Type.Gate,
			PathData.PassableArea.Type.Ladder,
			PathData.PassableArea.Type.LadderExit,
			PathData.PassableArea.Type.Stairs,
			PathData.PassableArea.Type.Teleport,
			PathData.PassableArea.Type.Tower,
			PathData.PassableArea.Type.Wall
		};

		public static int PATypeID(PathData.PassableArea.Type type)
		{
			for (int i = 0; i < area_types.Length; i++)
			{
				if (area_types[i] == type)
				{
					return i;
				}
			}
			return -1;
		}

		public float AreaTypeWeight(PathData.PassableArea.Type type)
		{
			if (weight_per_area_type == null)
			{
				return 1f;
			}
			int num = PATypeID(type);
			if (num == -1)
			{
				return 1f;
			}
			if (weight_per_area_type.Length <= num)
			{
				return 1f;
			}
			return weight_per_area_type[num];
		}

		public void Save(DT.Field field)
		{
			field.SetValue("tile_size", DT.FloatToStr(tile_size));
			field.SetValue("max_radius", DT.FloatToStr(max_radius));
			field.SetValue("max_slope", max_slope.ToString());
			field.SetValue("slope_avoidance", DT.FloatToStr(slope_avoidance));
			field.SetValue("slope_power", DT.FloatToStr(slope_power));
			field.SetValue("river_avoidance", DT.FloatToStr(river_avoidance));
			field.SetValue("coast_avoidance", DT.FloatToStr(coast_avoidance));
			field.SetValue("road_stickiness", DT.FloatToStr(road_stickiness));
			field.SetValue("ocean_high_pf_weight_mod", DT.FloatToStr(ocean_high_pf_weight_mod));
			field.SetValue("towns_passable", towns_passable ? "true" : "false");
			field.SetValue("base_weight", base_weight.ToString());
			field.SetValue("estimate_weight", estimate_weight.ToString());
			field.SetValue("max_steps", max_steps.ToString());
			field.SetValue("multithreaded", multithreaded ? "true" : "false");
			field.SetValue("grid_range", DT.FloatToStr(grid_range));
			field.SetValue("expand_towns", expand_towns ? "true" : "false");
			field.SetValue("is_runtime", is_runtime ? "true" : "false");
			field.SetValue("grid_tile_size", grid_tile_size);
			field.SetValue("enter_water_weight_mod", enter_water_weight_mod);
			field.SetValue("reserve_grid_size", null, reserve_grid_size);
			field.SetValue("reserve_grid_avoid_tile_dist_mod", null, reserve_grid_avoid_tile_dist_mod);
			field.SetValue("reserve_grid_avoidance_weight", null, reserve_grid_avoidance_weight);
			field.SetValue("ai_threat_avoidance_mod", null, ai_threat_avoidance_mod);
			field.SetValue("use_lsa_low_level", null, use_lsa_low_level);
			field.SetValue("occupied_ladder_mod", null, occupied_ladder_mod);
			field.SetValue("occupied_by_me_ladder_mod", null, occupied_by_me_ladder_mod);
			field.SetValue("map_bounds_width", null, map_bounds_width);
			field.SetValue("limit_low_steps_by_high_grid_size", null, limit_low_steps_by_high_grid_size);
			if (weight_per_area_type != null)
			{
				DT.Field field2 = field.AddChild("area_weights");
				for (int i = 0; i < area_types.Length; i++)
				{
					PathData.PassableArea.Type type = area_types[i];
					string key = type.ToString();
					field2.SetValue(key, AreaTypeWeight(type));
				}
			}
		}

		public void Load(DT.Field field)
		{
			if (field == null)
			{
				return;
			}
			tile_size = field.GetFloat("tile_size", null, tile_size);
			max_radius = field.GetFloat("max_radius", null, max_radius);
			max_slope = (byte)field.GetInt("max_slope", null, max_slope);
			slope_avoidance = field.GetFloat("slope_avoidance", null, slope_avoidance);
			slope_power = field.GetFloat("slope_power", null, slope_power);
			river_avoidance = field.GetFloat("river_avoidance", null, river_avoidance);
			coast_avoidance = field.GetFloat("coast_avoidance", null, coast_avoidance);
			road_stickiness = field.GetFloat("road_stickiness", null, road_stickiness);
			ocean_high_pf_weight_mod = field.GetFloat("ocean_high_pf_weight_mod", null, ocean_high_pf_weight_mod);
			towns_passable = field.GetBool("towns_passable", null, towns_passable);
			base_weight = (byte)field.GetInt("base_weight", null, base_weight);
			estimate_weight = (byte)field.GetInt("estimate_weight", null, estimate_weight);
			max_steps = field.GetInt("max_steps", null, max_steps);
			multithreaded = field.GetBool("multithreaded", null, multithreaded);
			grid_range = field.GetInt("grid_range", null, grid_range);
			expand_towns = field.GetBool("expand_towns", null, expand_towns);
			is_runtime = field.GetBool("is_runtime", null, is_runtime);
			grid_tile_size = field.GetFloat("grid_tile_size", null, grid_tile_size);
			enter_water_weight_mod = field.GetInt("enter_water_weight_mod", null, enter_water_weight_mod);
			reserve_grid_size = field.GetFloat("reserve_grid_size", null, reserve_grid_size);
			reserve_grid_avoid_tile_dist_mod = field.GetFloat("reserve_grid_avoid_tile_dist_mod", null, reserve_grid_avoid_tile_dist_mod);
			reserve_grid_avoidance_weight = field.GetFloat("reserve_grid_avoidance_weight", null, reserve_grid_avoidance_weight);
			ai_threat_avoidance_mod = field.GetFloat("ai_threat_avoidance_mod", null, ai_threat_avoidance_mod);
			use_lsa_low_level = field.GetBool("use_lsa_low_level", null, use_lsa_low_level);
			occupied_ladder_mod = field.GetFloat("occupied_ladder_mod", null, occupied_ladder_mod);
			occupied_by_me_ladder_mod = field.GetFloat("occupied_by_me_ladder_mod", null, occupied_by_me_ladder_mod);
			map_bounds_width = field.GetInt("map_bounds_width", null, map_bounds_width);
			limit_low_steps_by_high_grid_size = field.GetBool("limit_low_steps_by_high_grid_size", null, limit_low_steps_by_high_grid_size);
			DT.Field field2 = field.FindChild("area_weights");
			if (field2 != null)
			{
				weight_per_area_type = new float[area_types.Length];
				for (int i = 0; i < area_types.Length; i++)
				{
					PathData.PassableArea.Type type = area_types[i];
					string key = type.ToString();
					weight_per_area_type[i] = field2.GetFloat(key, null, 1f);
				}
			}
			else
			{
				weight_per_area_type = new float[area_types.Length];
				for (int j = 0; j < area_types.Length; j++)
				{
					weight_per_area_type[j] = 1f;
				}
			}
		}
	}

	public enum State
	{
		Idle,
		NewPath,
		FindUnreservedPoint,
		SetupHighCridPath,
		ModifyHighGridStart,
		ModifyHighGridDest,
		CalcHighGridPath,
		HighStep,
		LowSteps,
		Finished
	}

	private enum FindUnreservedStage
	{
		HasntStarted = 0,
		OngoingCantReserveInTown = 1,
		OngoingCantReserveInTownHasHitTown = 2,
		OngoingCanReserveInTown = 4,
		FinishedSuccessfuly = 8,
		FinishedUnsuccesfuly = 0x10
	}

	public struct OpenHighNode
	{
		public PPos pos;

		public uint eval;

		public OpenHighNode(PPos pos, uint eval)
		{
			this.pos = pos;
			this.eval = eval;
		}

		public override string ToString()
		{
			return pos.ToString() + " " + eval;
		}
	}

	public struct OpenNode
	{
		public int x;

		public int y;

		public uint eval;

		public OpenNode(int x, int y, uint eval)
		{
			this.x = x;
			this.y = y;
			this.eval = eval;
		}
	}

	private struct PointInfo
	{
		public PPos right;

		public float dl;

		public float dr;

		public float ofs;
	}

	public Settings settings;

	public string map_name;

	public PathData data;

	public State state;

	public Path cur_path;

	private SquadPowerGrid[] powerGrids;

	private bool success;

	public List<Path> pending = new List<Path>();

	public List<Path> completed = new List<Path>();

	private FindUnreservedStage find_unreserved_stage;

	private int modify_high_grid_dir = -1;

	private Coord modify_high_grid_start_coords;

	private Point modify_high_grid_pos;

	private float modify_high_grid_max_tile_size_mod;

	private int modify_high_grid_original_start_x;

	private int modify_high_grid_original_start_y;

	private int modify_high_grid_original_goal_x;

	private int modify_high_grid_original_goal_y;

	private bool modify_high_grid_original_start_ocean;

	private bool modify_high_grid_original_goal_ocean;

	private bool finished_modify_high_grid_dir_step;

	private Thread thread;

	public object Lock = new object();

	private AutoResetEvent resume = new AutoResetEvent(initialState: false);

	public int start_x;

	public int start_y;

	public int goal_x;

	public int goal_y;

	private int est_range;

	private float r2;

	private bool flee;

	private PPos ptStart;

	private PPos ptGoal;

	private bool start_ocean;

	private bool goal_ocean;

	private int start_lsa;

	private int goal_lsa;

	private int steps_ahead;

	private const int max_steps_ahead = 1;

	private bool tried_ahead;

	public List<OpenNode> open = new List<OpenNode>(8192);

	public List<OpenHighNode> highGridOpen = new List<OpenHighNode>(8192);

	private List<PPos> highGridPath = new List<PPos>();

	public static List<PPos> highPath_dbg = new List<PPos>(128);

	public static Path path_dbg = null;

	private List<List<PPos>> finalPathPoints = new List<List<PPos>>();

	public static MapObject last_src_obj;

	public static PPos last_src_pt;

	public static PPos last_dst_pt;

	public static bool last_low_level_only;

	public static float last_range;

	public static bool last_flee;

	private OpenNode best_node = new OpenNode(-1, -1, uint.MaxValue);

	private OpenHighNode best_high_node = new OpenHighNode(new PPos(-1f, -1f), uint.MaxValue);

	public static bool use_heap = true;

	public int total_steps;

	public int max_open;

	public int fixed_max_steps = -1;

	public static bool debugging = false;

	public static int total_high_steps = 0;

	public static int total_low_steps = 0;

	public static Game.ProfileScope.Stats pfs_Process = new Game.ProfileScope.Stats();

	public static Game.ProfileScope.Stats pfs_HighLevelPath = new Game.ProfileScope.Stats();

	public static Game.ProfileScope.Stats pfs_ModifyHighGrid = new Game.ProfileScope.Stats();

	public static Game.ProfileScope.Stats pfs_LowLevelSteps = new Game.ProfileScope.Stats();

	public static Game.ProfileScope.Stats pfs_ClearClosed = new Game.ProfileScope.Stats();

	public const int river_offset_multiplier = 3;

	private ushort modified_node_count;

	private ushort[] modified_node_weights = new ushort[16];

	private Coord[] modified_nodes_coord = new Coord[2];

	public Point unreserved_point { get; private set; }

	public static void ClearProfileStats()
	{
		total_high_steps = 0;
		total_low_steps = 0;
		pfs_Process.Clear();
		pfs_HighLevelPath.Clear();
		pfs_ModifyHighGrid.Clear();
		pfs_LowLevelSteps.Clear();
		pfs_ClearClosed.Clear();
	}

	public static string StatsText(string new_line = "\n", string ident = "  ")
	{
		string text = $"High Steps: {total_high_steps}, Low Steps: {total_low_steps}";
		text = text + new_line + pad("Process:") + pfs_Process;
		text = text + new_line + pad(ident + "High Level Path:") + pfs_HighLevelPath;
		text = text + new_line + pad(ident + ident + "Modify High Grid:") + pfs_ModifyHighGrid;
		text = text + new_line + pad(ident + "Low Level Steps:") + pfs_LowLevelSteps;
		return text + new_line + pad(ident + "Clear Closed:") + pfs_ClearClosed;
		static string pad(string x)
		{
			return x.PadRight(24, ' ');
		}
	}

	private void BeginFindUnreserved(Path path, MapObject map_obj, PPos pos)
	{
		PathData.Node node = data.GetNode(pos);
		unreserved_point = pos;
		find_unreserved_stage = FindUnreservedStage.OngoingCantReserveInTown;
		if (path.ignore_reserve || !path.can_reserve || !path.check_reserve || path.src_obj == null || (node.weight <= 0 && !node.ocean) || map_obj == null || pos.paID != 0)
		{
			EndFindUnreserved(path, success: false);
			return;
		}
		data.IncVersion();
		Cleanup();
		AddWave(this, pos, 0u, 0);
	}

	private void EndFindUnreserved(Path path, bool success)
	{
		if (find_unreserved_stage == FindUnreservedStage.OngoingCantReserveInTown)
		{
			find_unreserved_stage = (success ? FindUnreservedStage.FinishedSuccessfuly : FindUnreservedStage.FinishedUnsuccesfuly);
		}
		else if (find_unreserved_stage == FindUnreservedStage.OngoingCantReserveInTownHasHitTown)
		{
			find_unreserved_stage = (success ? FindUnreservedStage.FinishedSuccessfuly : FindUnreservedStage.OngoingCanReserveInTown);
			if (!success)
			{
				PPos dst_pt = path.dst_pt;
				data.IncVersion();
				Cleanup();
				AddWave(this, dst_pt, 0u, 0);
			}
		}
		else
		{
			find_unreserved_stage = (success ? FindUnreservedStage.FinishedSuccessfuly : FindUnreservedStage.FinishedUnsuccesfuly);
		}
	}

	private void ApplyFindUnreserved(Path path)
	{
		if (find_unreserved_stage == FindUnreservedStage.FinishedSuccessfuly)
		{
			float num = unreserved_point.Dist(path.dst_pt);
			path.range = Math.Max(0f, path.range - num);
			path.dst_pt = ClampPositionToWorldBounds(unreserved_point);
			Cleanup();
		}
		find_unreserved_stage = FindUnreservedStage.HasntStarted;
		state = State.SetupHighCridPath;
	}

	private void FindUnreservedSteps(Path path, MapObject map_obj, PPos pos, int steps)
	{
		int highPFGrid_width = data.highPFGrid_width;
		int highPFGrid_height = data.highPFGrid_height;
		float reserve_grid_size = settings.reserve_grid_size;
		float reserve_grid_avoid_tile_dist_mod = settings.reserve_grid_avoid_tile_dist_mod;
		int num = 0;
		if (open.Count == 0)
		{
			EndFindUnreserved(path, success: false);
			return;
		}
		while (open.Count > 0)
		{
			PathData.Node res;
			int x;
			int y;
			PPos pPos = PopWave(this, out res, out x, out y);
			res.open = false;
			data.ModifyNode(x, y, res);
			float x2 = pPos.x;
			float y2 = pPos.y;
			if (Math.Abs(x2 - pos.x) > reserve_grid_size * reserve_grid_avoid_tile_dist_mod || Math.Abs(y2 - pos.y) > reserve_grid_size * reserve_grid_avoid_tile_dist_mod)
			{
				continue;
			}
			int num2 = -1;
			for (int i = -1; i <= 1; i++)
			{
				float num3 = y2 * settings.tile_size + (float)i;
				for (int j = -1; j <= 1; j++)
				{
					if (i == 0 && j == 0)
					{
						continue;
					}
					num++;
					if (num > steps)
					{
						return;
					}
					num2++;
					float num4 = x2 * settings.tile_size + (float)j;
					if (!(num4 >= 0f) || !(num4 < (float)highPFGrid_width * reserve_grid_size) || !(num3 >= 0f) || !(num3 < (float)highPFGrid_height * reserve_grid_size))
					{
						continue;
					}
					PPos pPos2 = new PPos(num4, num3);
					PathData.Node node = data.GetNode(pPos2);
					if (CalcWeight(res, node, pPos2, this) == 0)
					{
						continue;
					}
					if (find_unreserved_stage != FindUnreservedStage.OngoingCanReserveInTown && node.town)
					{
						find_unreserved_stage = FindUnreservedStage.OngoingCantReserveInTownHasHitTown;
						continue;
					}
					uint num5 = res.path_eval + (uint)pPos2.SqrDist(pos);
					if (node.closed == data.version)
					{
						if (num5 < node.path_eval)
						{
							ModifyEval(pPos2, num5, (byte)num2, this);
						}
						continue;
					}
					AddWave(this, pPos2, num5, (byte)num2);
					if (find_unreserved_stage == FindUnreservedStage.OngoingCanReserveInTown || CanReserveCell(map_obj, pPos2))
					{
						unreserved_point = pPos2;
						EndFindUnreserved(path, success: true);
						return;
					}
				}
			}
		}
	}

	public PPos FindUnreserved(MapObject map_obj, PPos pos)
	{
		if (map_obj == null || pos.paID != 0)
		{
			return pos;
		}
		using (Game.Profile("PathFinding.FindUnreservedCell", log: false, settings.multithreaded ? (-1) : 0, pfs_ModifyHighGrid))
		{
			data.IncVersion();
			int highPFGrid_width = data.highPFGrid_width;
			int highPFGrid_height = data.highPFGrid_height;
			float reserve_grid_size = settings.reserve_grid_size;
			PathData.Node node = data.GetNode(pos);
			if (node.weight <= 0 && !node.ocean)
			{
				return pos;
			}
			float reserve_grid_avoid_tile_dist_mod = settings.reserve_grid_avoid_tile_dist_mod;
			open.Clear();
			AddWave(this, pos, 0u, 0);
			while (open.Count > 0)
			{
				PathData.Node res;
				int x;
				int y;
				PPos pPos = PopWave(this, out res, out x, out y);
				res.open = false;
				data.ModifyNode(x, y, res);
				float x2 = pPos.x;
				float y2 = pPos.y;
				if (Math.Abs(x2 - pos.x) > reserve_grid_size * reserve_grid_avoid_tile_dist_mod || Math.Abs(y2 - pos.y) > reserve_grid_size * reserve_grid_avoid_tile_dist_mod)
				{
					continue;
				}
				int num = -1;
				for (int i = -1; i <= 1; i++)
				{
					float num2 = y2 * settings.tile_size + (float)i;
					for (int j = -1; j <= 1; j++)
					{
						if (i == 0 && j == 0)
						{
							continue;
						}
						num++;
						float num3 = x2 * settings.tile_size + (float)j;
						if (!(num3 >= 0f) || !(num3 < (float)highPFGrid_width * reserve_grid_size) || !(num2 >= 0f) || !(num2 < (float)highPFGrid_height * reserve_grid_size))
						{
							continue;
						}
						PPos pPos2 = new PPos(num3, num2);
						PathData.Node node2 = data.GetNode(pPos2);
						if (CalcWeight(res, node2, pPos2, this) == 0)
						{
							continue;
						}
						uint num4 = res.path_eval + (uint)pPos2.SqrDist(pos);
						if (node2.closed == data.version)
						{
							if (num4 < node2.path_eval)
							{
								ModifyEval(pPos2, num4, (byte)num, this);
							}
							continue;
						}
						AddWave(this, pPos2, num4, (byte)num);
						if (CanReserveCell(map_obj, pPos2))
						{
							return pPos2;
						}
					}
				}
			}
			return pos;
		}
	}

	private void HeapPush(int x, int y, uint eval)
	{
		if (open.Count >= max_open)
		{
			max_open = open.Count + 1;
		}
		OpenNode openNode = new OpenNode
		{
			x = x,
			y = y,
			eval = eval
		};
		if (!use_heap)
		{
			for (int i = 0; i < open.Count; i++)
			{
				if (eval <= open[i].eval)
				{
					open.Insert(i, openNode);
					return;
				}
			}
			open.Add(openNode);
			return;
		}
		int num = open.Count;
		open.Add(openNode);
		while (num > 0)
		{
			int num2 = HeapParent(num);
			if (eval > open[num2].eval)
			{
				break;
			}
			open[num] = open[num2];
			num = num2;
		}
		open[num] = openNode;
	}

	private void HeapPushHighNode(PPos pos, uint eval)
	{
		if (highGridOpen.Count >= max_open)
		{
			max_open = highGridOpen.Count + 1;
		}
		OpenHighNode openHighNode = new OpenHighNode(pos, eval);
		if (!use_heap)
		{
			for (int i = 0; i < highGridOpen.Count; i++)
			{
				if (eval <= highGridOpen[i].eval)
				{
					highGridOpen.Insert(i, openHighNode);
					return;
				}
			}
			highGridOpen.Add(openHighNode);
			return;
		}
		int num = highGridOpen.Count;
		highGridOpen.Add(openHighNode);
		while (num > 0)
		{
			int num2 = HeapParent(num);
			if (eval > highGridOpen[num2].eval)
			{
				break;
			}
			highGridOpen[num] = highGridOpen[num2];
			num = num2;
		}
		highGridOpen[num] = openHighNode;
	}

	private OpenNode HeapPop()
	{
		int count = open.Count;
		if (count <= 0)
		{
			return new OpenNode(-1, -1, 0u);
		}
		OpenNode result = open[0];
		if (!use_heap)
		{
			open.RemoveAt(0);
			return result;
		}
		OpenNode value = open[count - 1];
		int num = 0;
		for (int num2 = HeapLeftChild(num); num2 < count; num2 = HeapLeftChild(num))
		{
			int num3 = HeapRightFromLeft(num2);
			int num4 = ((num3 < count && open[num3].eval < open[num2].eval) ? num3 : num2);
			if (open[num4].eval >= value.eval)
			{
				break;
			}
			open[num] = open[num4];
			num = num4;
		}
		open[num] = value;
		open.RemoveAt(count - 1);
		return result;
	}

	private OpenHighNode HeapPopHighNode()
	{
		int count = highGridOpen.Count;
		if (count <= 0)
		{
			return new OpenHighNode(PPos.Zero, 0u);
		}
		OpenHighNode result = highGridOpen[0];
		if (!use_heap)
		{
			highGridOpen.RemoveAt(0);
			return result;
		}
		OpenHighNode value = highGridOpen[count - 1];
		int num = 0;
		for (int num2 = HeapLeftChild(num); num2 < count; num2 = HeapLeftChild(num))
		{
			int num3 = HeapRightFromLeft(num2);
			int num4 = ((num3 < count && highGridOpen[num3].eval < highGridOpen[num2].eval) ? num3 : num2);
			if (highGridOpen[num4].eval >= value.eval)
			{
				break;
			}
			highGridOpen[num] = highGridOpen[num4];
			num = num4;
		}
		highGridOpen[num] = value;
		highGridOpen.RemoveAt(count - 1);
		return result;
	}

	private static int HeapParent(int i)
	{
		return (i - 1) / 2;
	}

	private static int HeapLeftChild(int i)
	{
		return i * 2 + 1;
	}

	private static int HeapRightFromLeft(int i)
	{
		return i + 1;
	}

	private ushort Estimate(int x, int y)
	{
		data.WorldToGrid(data.GridToWorld(x, y).pos, out var x2, out var y2);
		data.WorldToGrid(data.GridToWorld(goal_x, goal_y).pos, out var x3, out var y3);
		int val = Math.Abs(x2 - x3);
		int val2 = Math.Abs(y2 - y3);
		int num = Math.Max(val, val2);
		int num2 = Math.Min(val, val2);
		int num3 = num2;
		int num4 = num - num2;
		int num5 = settings.estimate_weight * (num4 * 5 + num3 * 7);
		if (flee && (cur_path.low_level_only || SkipHighPF(cur_path, low_level_only: false)))
		{
			num5 = est_range - num5;
			if (num5 < 0)
			{
				num5 = 0;
			}
		}
		return (ushort)num5;
	}

	public static bool CheckReachable(PathData data, Point a, Point b)
	{
		PathData.Node node = data.GetNode(a);
		if (data.GetNode(b).ocean != node.ocean)
		{
			return false;
		}
		if (data.pointers.BurstedTrace(a, b, out var result) || data.pointers.BurstedTrace(b, a, out var result2))
		{
			return true;
		}
		return !data.GetNode((result + result2) / 2f).river;
	}

	private bool IsGoal(int x, int y, int estimate)
	{
		if (flee)
		{
			return estimate == 0;
		}
		if (!ValidToEndOn(x, y))
		{
			return false;
		}
		PPos pPos = data.GridToWorld(x, y);
		if (pPos.paID != ptGoal.paID)
		{
			return false;
		}
		if (pPos.paID > 0 && pPos.paID == ptGoal.paID)
		{
			return true;
		}
		if (estimate > est_range)
		{
			return false;
		}
		float num = pPos.SqrDist(ptGoal);
		if (est_range == 0)
		{
			return true;
		}
		if (num > r2)
		{
			return false;
		}
		if (float.IsInfinity(pPos.x) || float.IsInfinity(pPos.y) || float.IsInfinity(ptGoal.x) || float.IsInfinity(ptGoal.y) || float.IsNaN(pPos.x) || float.IsNaN(pPos.y) || float.IsNaN(ptGoal.x) || float.IsNaN(ptGoal.y))
		{
			Error($"invalid IsGoal points pt: {pPos}, ptGoal: {ptGoal}");
			return false;
		}
		if (!CheckReachable(data, pPos, ptGoal))
		{
			return false;
		}
		return true;
	}

	public bool CanReserveCell(MapObject map_obj, PPos pt, bool check_prio = true)
	{
		if (map_obj == null)
		{
			return true;
		}
		if (data.reservation_grid == null)
		{
			return true;
		}
		if (pt.paID != 0)
		{
			return true;
		}
		Coord coord = WorldToReservationGrid(pt);
		bool result = true;
		float reservationRadius = map_obj.GetReservationRadius();
		float num = CalcReservePrio(map_obj, pt);
		float num2 = settings.reserve_grid_size * settings.reserve_grid_avoid_tile_dist_mod;
		float num3 = num2 * num2;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Coord coord2 = new Coord(coord.x + i, coord.y + j);
				if (!InReserveBounds(coord2))
				{
					continue;
				}
				lock (Lock)
				{
					List<MapObject> reserved = GetReserved(coord2);
					for (int k = 0; k < reserved.Count; k++)
					{
						MapObject mapObject = reserved[k];
						if (mapObject == null || mapObject == map_obj || map_obj.IgnoreCollision(mapObject) || mapObject.position.SqrDist(mapObject.reserve_pt) > num3)
						{
							continue;
						}
						float num4 = pt.SqrDist(mapObject.reserve_pt);
						float reservationRadius2 = mapObject.GetReservationRadius();
						float num5 = reservationRadius + reservationRadius2;
						float num6 = num5 * num5;
						if (!(num4 < num6))
						{
							continue;
						}
						if (check_prio)
						{
							float num7 = CalcReservePrio(mapObject, mapObject.reserve_pt);
							if ((!map_obj.ForceReservePush(mapObject) && num > num7) || map_obj.ForceReserveCheck(mapObject))
							{
								result = false;
							}
						}
						else
						{
							result = false;
						}
					}
				}
			}
		}
		return result;
	}

	private void Open(int x, int y, uint prev_eval, int dir, ref PathData.Node prev, bool expect_ground, bool cheap_water_enter = true, bool force_no_pas = false)
	{
		if ((data.AreGroundNodeCords(x, y) || expect_ground) && (x < 0 || x >= data.width || y < 0 || y >= data.height))
		{
			return;
		}
		PPos pos = data.GridToWorld(x, y);
		PathData.Node node = data.GetNode(pos);
		if (dir == 12 && node.river_offset == 0)
		{
			return;
		}
		if (pos.paID != 0)
		{
			if (force_no_pas)
			{
				return;
			}
			PathData.PassableArea passableArea = data.pas[pos.paID - 1];
			if ((cur_path != null && !cur_path.low_level_only && pos.paID != ptGoal.paID && pos.paID != ptStart.paID) || !passableArea.CanEnter(cur_path, this, pos.paID, prev.pa_id))
			{
				return;
			}
		}
		if (node.water != prev.water && cur_path != null && !cur_path.can_enter_water)
		{
			return;
		}
		int num;
		if (node.ocean)
		{
			num = settings.base_weight;
			if (!prev.ocean)
			{
				if ((cur_path != null && cur_path.flee) || !settings.towns_passable)
				{
					return;
				}
				num = ((!(start_ocean != goal_ocean && cheap_water_enter)) ? (num * settings.enter_water_weight_mod) : (num * 3));
			}
		}
		else
		{
			if (prev.ocean && cur_path != null && cur_path.flee)
			{
				return;
			}
			num = data.GetWeight(node, (cur_path != null) ? cur_path.min_radius : 0f, (cur_path != null) ? cur_path.max_radius : 0f);
			if (num == 0)
			{
				if (prev.ocean || prev.weight > 0)
				{
					return;
				}
				num = 10;
			}
			else if (prev.ocean)
			{
				num = settings.base_weight;
				num = ((!((start_ocean != goal_ocean || start_lsa != goal_lsa) && cheap_water_enter)) ? (num * settings.enter_water_weight_mod) : (num * 3));
			}
		}
		int num2 = ((dir == 0 || dir == 2 || dir == 5 || dir == 7) ? 7 : 5);
		if (dir >= 8 && dir <= 11)
		{
			num2 = 0;
		}
		if (dir == 12)
		{
			num = (int)settings.river_avoidance;
		}
		uint num3 = prev_eval + (uint)(num2 * num);
		if (node.closed == data.version)
		{
			if (num3 >= node.path_eval)
			{
				return;
			}
		}
		else
		{
			node.closed = data.version;
			node.estimate = Estimate(x, y);
		}
		node.dir = (byte)dir;
		node.path_eval = num3;
		node.open = true;
		data.ModifyNode(x, y, node);
		HeapPush(x, y, node.path_eval + node.estimate);
	}

	private void Cleanup()
	{
		using (Game.Profile("PathFinding.Cleanup", log: false, settings.multithreaded ? (-1) : 0))
		{
			for (int i = 0; i < open.Count; i++)
			{
				data.nodes[i].open = false;
			}
			open.Clear();
		}
	}

	public static Coord GetRiverOffset(byte river_offset)
	{
		int num = river_offset >> 7;
		int num2 = (river_offset >> 4) & 7;
		int num3 = (river_offset >> 3) & 1;
		int num4 = river_offset & 7;
		return new Coord(((num == 1) ? 1 : (-1)) * num2 * 3, ((num3 == 1) ? 1 : (-1)) * num4 * 3);
	}

	private bool ValidToEndOn(int x, int y)
	{
		if (data.AreGroundNodeCords(x, y))
		{
			return true;
		}
		if (y <= 0)
		{
			return true;
		}
		if (data.pointers.GetPA(y - 1).type == PathData.PassableArea.Type.Teleport)
		{
			if (y == ptGoal.paID && state == State.LowSteps && (highGridPath == null || highGridPath[highGridPath.Count - 1].paID != y))
			{
				return true;
			}
			return false;
		}
		return true;
	}

	private bool Step(float bounds_center_x = -1f, float bounds_center_y = -1f, float max_tile_size = -1f, bool cheap_water_enter = true, bool force_no_pas = false)
	{
		using (Game.Profile("PathFinding.Step", log: false, settings.multithreaded ? (-1) : 0))
		{
			total_steps++;
			total_low_steps++;
			if (settings.max_steps > 0 && total_steps >= settings.max_steps)
			{
				success = false;
				return false;
			}
			if (cur_path != null && !cur_path.use_max_steps_possible && settings.limit_low_steps_by_high_grid_size && fixed_max_steps > 0 && total_steps >= fixed_max_steps)
			{
				success = false;
				return false;
			}
			OpenNode openNode = HeapPop();
			if (openNode.x < 0 && openNode.y < 0)
			{
				success = false;
				return false;
			}
			PathData.Node prev = data.GetNode(openNode.x, openNode.y);
			if (!prev.open)
			{
				return true;
			}
			PathData.Node newVal = prev;
			newVal.open = false;
			data.ModifyNode(openNode.x, openNode.y, newVal);
			if ((best_node.x < 0 || prev.estimate < best_node.eval) && (ValidToEndOn(openNode.x, openNode.y) || best_node.x < 0))
			{
				best_node = new OpenNode(openNode.x, openNode.y, prev.estimate);
			}
			if (IsGoal(openNode.x, openNode.y, prev.estimate))
			{
				best_node = new OpenNode(openNode.x, openNode.y, prev.estimate);
				success = true;
				return false;
			}
			PPos pPos = data.GridToWorld(openNode.x, openNode.y);
			if (pPos.paID == 0 && bounds_center_x >= 0f && (Math.Abs(pPos.x - bounds_center_x) > max_tile_size || Math.Abs(pPos.y - bounds_center_y) > max_tile_size))
			{
				return true;
			}
			int num = 0;
			bool flag = data.AreGroundNodeCords(openNode.x, openNode.y);
			if (flag)
			{
				num = -1;
				if (prev.river_offset != 0 && prev.dir != 12)
				{
					Coord riverOffset = GetRiverOffset(prev.river_offset);
					Open(openNode.x + riverOffset.x, openNode.y + riverOffset.y, prev.path_eval, 12, ref prev, expect_ground: true, cheap_water_enter, force_no_pas);
				}
				for (int i = -1; i <= 1; i++)
				{
					for (int j = -1; j <= 1; j++)
					{
						if (i != 0 || j != 0)
						{
							num++;
							if (data.IsInBounds(openNode.x + j, openNode.y + i))
							{
								Open(openNode.x + j, openNode.y + i, prev.path_eval, num, ref prev, expect_ground: true, cheap_water_enter, force_no_pas);
							}
						}
					}
				}
			}
			else
			{
				for (int k = 0; k < PathData.PassableArea.numNodes; k++)
				{
					int num2 = (openNode.y - 1) * PathData.PassableArea.numNodes + k;
					if (data.GetPANodeIndex(openNode.x, openNode.y) != num2)
					{
						data.GetPaNodeCords(num2, out var x, out var y);
						Open(x, y, prev.path_eval, data.GetPANodeAreaIndex(openNode.x, openNode.y), ref prev, expect_ground: false, cheap_water_enter, force_no_pas);
					}
				}
			}
			if (!PathData.IsGroundPAid(prev.pa_id))
			{
				int pa_id = prev.pa_id;
				for (int l = 0; l < PathData.PassableArea.numNodes; l++)
				{
					PathData.PassableAreaNode paNode = data.GetPaNode((pa_id - 1) * PathData.PassableArea.numNodes + l);
					int x2 = -1;
					int y2 = -1;
					if (flag && paNode.type == PathData.PassableAreaNode.Type.Ground)
					{
						int num3 = (int)(paNode.pos.x / settings.tile_size);
						int num4 = (int)(paNode.pos.y / settings.tile_size);
						if (openNode.x != num3 || openNode.y != num4)
						{
							continue;
						}
						int idx = (pa_id - 1) * PathData.PassableArea.numNodes + l;
						data.GetPaNodeCords(idx, out x2, out y2);
					}
					else if (!flag)
					{
						int pANodeIndex = data.GetPANodeIndex(openNode.x, openNode.y);
						PathData.PassableAreaNode paNode2 = data.GetPaNode(pANodeIndex);
						if (paNode.link != 0 && pANodeIndex != 0 && paNode2.type == PathData.PassableAreaNode.Type.Normal && paNode.link == data.GetPANodeIndex(openNode.x, openNode.y))
						{
							PathData.PassableAreaNode pANode = data.pointers.GetPANode(paNode.link);
							if (!data.pointers.GetPA(pANode.pos.paID - 1).enabled)
							{
								continue;
							}
							int idx2 = (pa_id - 1) * PathData.PassableArea.numNodes + l;
							data.GetPaNodeCords(idx2, out x2, out y2);
						}
					}
					if (x2 != -1 || y2 != -1)
					{
						Open(x2, y2, prev.path_eval, 8 + l, ref prev, expect_ground: false, cheap_water_enter, force_no_pas);
						break;
					}
				}
			}
			else
			{
				PathData.PassableAreaNode paNode3 = data.GetPaNode(openNode.x, openNode.y);
				if (paNode3.type != PathData.PassableAreaNode.Type.Ground)
				{
					return true;
				}
				int pANodeAreaIndex = data.GetPANodeAreaIndex(openNode.x, openNode.y);
				int x3 = (int)(paNode3.pos.x / data.settings.tile_size);
				int y3 = (int)(paNode3.pos.y / data.settings.tile_size);
				if (data.GetNode(x3, y3).pa_id == openNode.y)
				{
					Open(x3, y3, prev.path_eval, 8 + pANodeAreaIndex, ref prev, expect_ground: true, cheap_water_enter, force_no_pas);
				}
			}
			return true;
		}
	}

	private bool SteepAngleHighSegment(List<PPos> current)
	{
		if (finalPathPoints.Count == 0)
		{
			return false;
		}
		List<PPos> list = finalPathPoints[finalPathPoints.Count - 1];
		if (list == null || list.Count <= 1 || current.Count <= 1)
		{
			return false;
		}
		PPos pPos = list[list.Count - 1];
		PPos pPos2 = list[list.Count - 2];
		return Math.Abs(Point.AngleBetween(c: current[1], a: pPos2, b: pPos)) < 90f;
	}

	public static byte GetDir(int cx, int cy)
	{
		if (cx == 0 && cy == 0)
		{
			return 8;
		}
		byte b = byte.MaxValue;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j != 0 || i != 0)
				{
					b++;
					if (j == cx && i == cy)
					{
						return b;
					}
				}
			}
		}
		return b;
	}

	public Coord GetReverseDir(int dir)
	{
		return GetDir(data.GridReverseDir[dir]);
	}

	public Coord GetDir(int i)
	{
		if (i < 0 || i >= data.GridNeighborOffset.Length)
		{
			return Coord.Zero;
		}
		return data.GridNeighborOffset[i];
	}

	private bool HighGridStep(Path path)
	{
		using (Game.Profile("PathFinding.HighGridStep", log: false, settings.multithreaded ? (-1) : 0))
		{
			float weight;
			List<PPos> list = ConstructGridPath(path.max_radius, out weight);
			if (list == null)
			{
				return false;
			}
			if (highGridPath.Count <= 2)
			{
				path.path_eval += weight;
				finalPathPoints.Add(list);
				return false;
			}
			total_steps = 0;
			best_node = new OpenNode(-1, -1, uint.MaxValue);
			Cleanup();
			data.IncVersion();
			float num = 0f;
			bool flag = false;
			if (steps_ahead > 1)
			{
				for (int i = 0; i < steps_ahead; i++)
				{
					PPos pPos = highGridPath[i];
					Coord coord = WorldToGridCoord(pPos);
					PPos pPos2 = highGridPath[i + 1];
					Coord coord2 = WorldToGridCoord(pPos2);
					num += (float)(int)data.GetHighNodeWeight(coord.x, coord.y, GetDir(coord2.x - coord.x, coord2.y - coord.y));
				}
				if (num < weight * 0.9f)
				{
					steps_ahead--;
					if (steps_ahead >= 1)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				tried_ahead = false;
				for (int j = 0; j < steps_ahead; j++)
				{
					highGridPath.RemoveAt(0);
				}
				bool flag2 = true;
				for (int k = 0; k < list.Count; k++)
				{
					if (list[k].paID != 0)
					{
						flag2 = false;
						break;
					}
				}
				if (flag2 && highGridPath.Count > 1)
				{
					int num2 = list.Count / 2 + 1;
					if (start_ocean != goal_ocean)
					{
						for (int l = 0; l < list.Count; l++)
						{
							if (data.GetNode(list[l]).ocean == goal_ocean)
							{
								num2 = Math.Max(num2 + 1, l);
								break;
							}
						}
					}
					int count = list.Count - num2;
					list.RemoveRange(num2, count);
				}
				finalPathPoints.Add(list);
				SetPTStart(list[list.Count - 1]);
				path.path_eval += weight;
			}
			ResetPTGoal();
			return true;
		}
	}

	private void SetPTStart(PPos pt)
	{
		ptStart = pt;
		data.WorldToGrid(ptStart, out start_x, out start_y);
		PathData.Node node = data.GetNode(start_x, start_y);
		start_ocean = node.ocean;
		start_lsa = node.lsa;
	}

	private void SetPTGoal(PPos pt)
	{
		ptGoal = pt;
		data.WorldToGrid(ptGoal, out goal_x, out goal_y);
		PathData.Node node = data.GetNode(goal_x, goal_y);
		goal_ocean = node.ocean;
		goal_lsa = node.lsa;
	}

	private List<PPos> ConstructFinalPath()
	{
		if (finalPathPoints == null || finalPathPoints.Count == 0)
		{
			return new List<PPos>();
		}
		if (finalPathPoints.Count == 1)
		{
			return finalPathPoints[0];
		}
		List<PPos> list = finalPathPoints[0];
		if (list == null)
		{
			list = new List<PPos>();
		}
		for (int i = 1; i < finalPathPoints.Count; i++)
		{
			if (finalPathPoints[i] != null)
			{
				list.AddRange(finalPathPoints[i]);
			}
		}
		Path path = new Path(game, list[0]);
		path.SetPath(list);
		int num = 0;
		List<PPos> list2 = new List<PPos>();
		float num2 = 0f;
		while (true)
		{
			int num3 = path.FindSegment(num2);
			for (int j = num; j < num3; j++)
			{
				Path.Segment segment = path.segments[j];
				if (segment.pt.paID != 0 && (list2.Count == 0 || segment.pt != list2[list2.Count - 1]))
				{
					list2.Add(segment.pt);
				}
			}
			if ((path.GetPathPoint(num2, out var pt, out var _) || num2 == path.path_len) && pt.paID == 0 && (list2.Count == 0 || pt != list2[list2.Count - 1]))
			{
				list2.Add(pt);
			}
			num = num3;
			if (num2 > path.path_len)
			{
				num2 = path.path_len;
				continue;
			}
			if (num2 == path.path_len)
			{
				break;
			}
			num2 += 2f;
		}
		return list2;
	}

	private void ResetPTGoal()
	{
		if (highGridPath.Count <= 1)
		{
			Finish();
			return;
		}
		if (!tried_ahead)
		{
			tried_ahead = true;
			steps_ahead = Math.Min(highGridPath.Count - 1, 2);
			for (int i = 0; i <= steps_ahead; i++)
			{
				if (highGridPath[i].paID != 0)
				{
					steps_ahead = 1;
					break;
				}
			}
		}
		SetPTGoal(highGridPath[steps_ahead]);
		if (cur_path != null)
		{
			if (ptGoal == highGridPath[highGridPath.Count - 1])
			{
				est_range = (int)(cur_path.range * (float)(int)settings.estimate_weight * 5f * 1.1f / settings.tile_size);
			}
			else
			{
				est_range = 0;
			}
		}
		fixed_max_steps = (int)(settings.grid_tile_size * settings.grid_tile_size * 10f * (float)steps_ahead);
		PathData.Node prev = data.GetNode(ptStart);
		Open(start_x, start_y, 0u, prev.dir, ref prev, expect_ground: false);
	}

	public float ThreatMod(Point pos)
	{
		float num = 0f;
		if (cur_path.battle_side < 0 || !cur_path.check_threat || powerGrids == null)
		{
			return num;
		}
		SquadPowerGrid.Threat interpolatedCell = powerGrids[1 - cur_path.battle_side].GetInterpolatedCell(pos.x, pos.y);
		num += interpolatedCell.base_threat;
		if (cur_path.check_anti_cavalry_threat)
		{
			num += interpolatedCell.anti_cavalry_threat;
		}
		if (cur_path.check_anti_ranged_threat)
		{
			num += interpolatedCell.infantry_threat;
		}
		return num * settings.ai_threat_avoidance_mod;
	}

	public float ReserveMod(Point pos)
	{
		MapObject mapObject = cur_path?.src_obj;
		if (settings.reserve_grid_avoidance_weight <= 0f || mapObject == null || data?.reservation_grid == null)
		{
			return 1f;
		}
		float reservationRadius = mapObject.GetReservationRadius();
		Coord coord = WorldToReservationGrid(pos);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Coord coord2 = new Coord(coord.x + i, coord.y + j);
				if (!InReserveBounds(coord2))
				{
					continue;
				}
				lock (Lock)
				{
					List<MapObject> reserved = GetReserved(coord2);
					for (int k = 0; k < reserved.Count; k++)
					{
						MapObject mapObject2 = reserved[k];
						if (mapObject.AvoidAlreadyReservedPosition(mapObject2))
						{
							float num = mapObject2.VisualPosition().SqrDist(pos);
							float num2 = mapObject2.GetReservationRadius() + reservationRadius;
							if (num < num2 * num2)
							{
								return settings.reserve_grid_avoidance_weight;
							}
						}
					}
				}
			}
		}
		return 1f;
	}

	public void ResetModifyHighGrid()
	{
		modify_high_grid_dir = -1;
	}

	public bool StartModifyHighGridDir()
	{
		using (Game.Profile("PathFinding.StartNewModifyHighGridDir"))
		{
			if (modify_high_grid_dir >= 7)
			{
				return false;
			}
			modify_high_grid_dir++;
			finished_modify_high_grid_dir_step = false;
			Coord dir = GetDir(modify_high_grid_dir);
			int x = dir.x;
			int y = dir.y;
			Coord pos = new Coord(modify_high_grid_start_coords.x + x, modify_high_grid_start_coords.y + y);
			Point point = GridCoordToWorld(pos);
			PathData.Node node = data.GetNode(point);
			success = false;
			if (node.weight == 0 && !node.water)
			{
				return false;
			}
			SetPTGoal(point);
			Cleanup();
			data.IncVersion();
			total_steps = 0;
			best_node = new OpenNode(-1, -1, uint.MaxValue);
			PathData.Node prev = data.GetNode(modify_high_grid_pos);
			Open(start_x, start_y, 0u, prev.dir, ref prev, expect_ground: false, cheap_water_enter: false);
			return true;
		}
	}

	private bool HasValidModifyDir()
	{
		if (modify_high_grid_dir > -1)
		{
			return modify_high_grid_dir <= 7;
		}
		return false;
	}

	public void ModifyHighGridSteps(bool cheap_enter_water = true)
	{
		if (finished_modify_high_grid_dir_step && modify_high_grid_dir >= 7)
		{
			if (state == State.ModifyHighGridStart)
			{
				FinishModifyHighGridStart();
			}
			else
			{
				FinishModifyHighGridDest();
			}
			return;
		}
		if (finished_modify_high_grid_dir_step || modify_high_grid_dir == -1)
		{
			bool flag = false;
			for (int i = modify_high_grid_dir; i <= 7; i++)
			{
				if (StartModifyHighGridDir())
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				finished_modify_high_grid_dir_step = true;
			}
		}
		if (HasValidModifyDir() && PerformSteps(1000, start_x, start_y, modify_high_grid_max_tile_size_mod * settings.grid_tile_size, cheap_enter_water, force_no_pas: true) && open.Count == 0)
		{
			finished_modify_high_grid_dir_step = true;
		}
		if (success)
		{
			using (Game.Profile("PathFinding.ModifyHighGridWeight"))
			{
				PPos pos = data.GridToWorld(best_node.x, best_node.y);
				PathData.Node node = data.GetNode(pos);
				data.SetHighNodeWeight(modify_high_grid_start_coords.x, modify_high_grid_start_coords.y, modify_high_grid_dir, (ushort)node.path_eval);
				finished_modify_high_grid_dir_step = true;
			}
		}
	}

	public void ModifyHighGridSteps(Path path)
	{
		using (Game.Profile("PathFinding.Steps", log: false, settings.multithreaded ? (-1) : 0, pfs_LowLevelSteps))
		{
			bool flag = true;
			if (modify_high_grid_dir == -1)
			{
				flag = ((state != State.ModifyHighGridStart) ? ModifyHighGridDest(path) : ModifyHighGridStart(path));
			}
			if (flag)
			{
				ModifyHighGridSteps(cheap_enter_water: false);
			}
		}
	}

	public void ModifyHighGridSteps(Point pos)
	{
		using (Game.Profile("PathFinding.Steps", log: false, settings.multithreaded ? (-1) : 0, pfs_LowLevelSteps))
		{
			if (modify_high_grid_dir == -1)
			{
				ModifyHighGridStart(pos);
			}
			ModifyHighGridSteps(cheap_enter_water: false);
		}
	}

	private bool ModifyHighGridStart(Path path)
	{
		bool flag = true;
		if (path.src_pt.paID == 0 && !path.low_level_only && BeginModifyHighGrid(path.src_pt, is_regular_pf: true, stop_impassable: false, 1.5f, modified_node_weights, modified_nodes_coord))
		{
			flag = false;
		}
		if (flag)
		{
			FinishModifyHighGridStart();
		}
		return !flag;
	}

	private bool ModifyHighGridDest(Path path)
	{
		bool flag = true;
		if (path.dst_pt.paID == 0 && !path.low_level_only && BeginModifyHighGrid(path.dst_pt, is_regular_pf: true, stop_impassable: true, 1.5f, modified_node_weights, modified_nodes_coord))
		{
			flag = false;
		}
		if (flag)
		{
			FinishModifyHighGridDest();
		}
		return !flag;
	}

	private void ModifyHighGridStart(Point pos)
	{
		if (!BeginModifyHighGrid(pos, is_regular_pf: false, stop_impassable: false))
		{
			FinishModifyHighGridStart();
		}
	}

	private void FinishModifyHighGrid()
	{
		if (modify_high_grid_dir != -1)
		{
			total_steps = 0;
			Cleanup();
			start_x = modify_high_grid_original_start_x;
			start_y = modify_high_grid_original_start_y;
			goal_x = modify_high_grid_original_goal_x;
			goal_y = modify_high_grid_original_goal_y;
			start_ocean = modify_high_grid_original_start_ocean;
			goal_ocean = modify_high_grid_original_goal_ocean;
			ResetModifyHighGrid();
		}
	}

	private void FinishModifyHighGridStart()
	{
		FinishModifyHighGrid();
		state = State.ModifyHighGridDest;
	}

	private void FinishModifyHighGridDest()
	{
		state = State.CalcHighGridPath;
		FinishModifyHighGrid();
	}

	private void BeginGrid(Path path, bool low_level_only = false)
	{
		using (Game.Profile("PathFinding.BeginGrid", log: false, settings.multithreaded ? (-1) : 0, pfs_HighLevelPath))
		{
			finalPathPoints.Clear();
			Cleanup();
			find_unreserved_stage = FindUnreservedStage.HasntStarted;
			ResetModifyHighGrid();
			ResetModifiedHighGridNodes(this);
			state = State.FindUnreservedPoint;
			path.src_pt = ClampPositionToWorldBounds(path.src_pt);
			path.dst_pt = ClampPositionToWorldBounds(path.dst_pt);
		}
	}

	public PPos ClampPositionToWorldBounds(PPos pos)
	{
		if (game == null)
		{
			return pos;
		}
		if (pos.paID != 0)
		{
			return pos;
		}
		Point point = new Point(game.world_size.x - settings.grid_tile_size, game.world_size.y - settings.grid_tile_size);
		PPos result = pos;
		if (result.x < 0f)
		{
			result.x = 0f;
		}
		if (result.x > point.x)
		{
			result.x = point.x;
		}
		if (result.y < 0f)
		{
			result.y = 0f;
		}
		if (result.y > point.y)
		{
			result.y = point.y;
		}
		return result;
	}

	public void Add(Path path)
	{
		UpdateNextFrame();
		lock (Lock)
		{
			path.state = Path.State.Pending;
			Kingdom kingdom = path.src_obj?.GetKingdom();
			if ((kingdom != null && !kingdom.is_player) || game.type == "battle_view")
			{
				pending.Add(path);
			}
			else
			{
				pending.Insert(0, path);
			}
			if (pending.Count == 1 && settings.multithreaded)
			{
				resume.Set();
			}
		}
	}

	public void Del(Path path)
	{
		lock (Lock)
		{
			if (path.state == Path.State.Pending)
			{
				pending.Remove(path);
			}
			path.state = Path.State.Stopped;
			path.src_obj = null;
			path.dst_obj = null;
			path.segments = null;
			path.path_len = 0f;
			path.t = 0f;
			path.segment_idx = 0;
		}
	}

	private void NotifyCompleted(Path path)
	{
		if (path.state == Path.State.Succeeded || path.state == Path.State.Failed)
		{
			Movement movement = path?.src_obj?.movement;
			if (movement?.obj != null && movement.obj.IsValid())
			{
				movement.OnPathFindingComplete(path);
			}
			path?.onPathFindingComplete?.Invoke();
		}
	}

	private List<PointInfo> CalcClearance(List<PPos> points, float radius)
	{
		List<PointInfo> list = new List<PointInfo>(points.Count);
		float num = 2f * radius;
		float num2 = 0f;
		list.Add(default(PointInfo));
		for (int i = 1; i < points.Count - 1; i++)
		{
			PPos pPos = points[i];
			PPos pPos2 = points[i + 1] - pPos;
			PPos pPos3 = pPos - points[i - 1];
			PointInfo item = default(PointInfo);
			item.right = (pPos2 + pPos3).Right(1f);
			item.dr = data.TraceDir(pPos, item.right, num) - num2;
			item.dl = data.TraceDir(pPos, item.right * -1f, num) + num2;
			if (item.dr < radius || item.dl < radius)
			{
				float num3 = item.dr + item.dl;
				if (num3 > num)
				{
					num3 = num;
				}
				num2 += item.dr - num3 / 2f;
			}
			item.ofs = num2;
			list.Add(item);
		}
		list.Add(default(PointInfo));
		return list;
	}

	private void BlurOffsets(List<PointInfo> infos)
	{
		float[] array = new float[infos.Count];
		for (int i = 1; i < infos.Count - 1; i++)
		{
			array[i] = (infos[i - 1].ofs + infos[i].ofs + infos[i + 1].ofs) / 3f;
		}
		for (int j = 1; j < infos.Count - 1; j++)
		{
			PointInfo value = infos[j];
			value.ofs = array[j];
			infos[j] = value;
		}
	}

	private void ApplyOffsets(List<PPos> points, List<PointInfo> infos)
	{
		for (int i = 1; i < points.Count - 1; i++)
		{
			points[i] += infos[i].right * infos[i].ofs;
		}
	}

	private void ApplyRadius(List<PPos> points, float radius)
	{
		if (!(radius <= 0f))
		{
			List<PointInfo> infos = CalcClearance(points, radius);
			for (int i = 0; i < 4; i++)
			{
				BlurOffsets(infos);
			}
			ApplyOffsets(points, infos);
		}
	}

	private void Optimize(List<PPos> points, float radius)
	{
		for (int i = 0; i < points.Count - 2; i++)
		{
			PPos pPos = points[i];
			for (int num = points.Count - 1; num >= i + 2; num--)
			{
				PPos to = points[num];
				if (pPos.paID == 0 && to.paID == 0 && data.Trace(pPos, to, radius))
				{
					points.RemoveRange(i + 1, num - i - 1);
					break;
				}
			}
		}
	}

	private void Smooth(List<PPos> points)
	{
		PPos pPos = points[0];
		for (int i = 1; i < points.Count - 1; i++)
		{
			PPos pPos2 = points[i];
			if (pPos.paID == 0 && pPos2.paID == 0)
			{
				points[i] = (pPos + pPos2) * 0.5f;
			}
			pPos = pPos2;
		}
	}

	private List<PPos> ConstructGridPath(float radius, out float weight)
	{
		weight = 0f;
		if (data == null || (best_node.x < 0 && best_node.y < 0))
		{
			return null;
		}
		List<PPos> list = new List<PPos>();
		OpenNode openNode = best_node;
		if (ptGoal.paID > 0 || (highGridPath.Count > 2 && (r2 == 0f || ptGoal != cur_path.dst_pt)))
		{
			list.Add(ptGoal);
		}
		bool flag = false;
		while (true)
		{
			bool flag2 = data.AreGroundNodeCords(openNode.x, openNode.y);
			if (list.Count > 100000)
			{
				break;
			}
			if ((openNode.x == start_x && openNode.y == start_y) || (!flag2 && openNode.y == start_y))
			{
				list.Add(ptStart);
				break;
			}
			if (openNode.x == goal_x && openNode.y == goal_y)
			{
				list.Add(ptGoal);
			}
			else if (!flag)
			{
				PPos pPos = data.GridToWorld(openNode.x, openNode.y);
				list.Add(pPos);
				PathData.Node node = data.GetNode(pPos);
				if ((float)node.path_eval > weight)
				{
					weight = node.path_eval;
				}
			}
			flag = false;
			PathData.Node node2 = data.GetNode(openNode.x, openNode.y);
			if (node2.dir == 12)
			{
				Coord riverOffset = GetRiverOffset(node2.river_offset);
				data.GetNodeCoords(data.GetNodeIdx(openNode.x + riverOffset.x, openNode.y + riverOffset.y), out openNode.x, out openNode.y);
				list.Add(new PPos(openNode.x, openNode.y, -1));
				flag = true;
			}
			else if (node2.dir >= 8 && node2.dir <= 11)
			{
				if (!PathData.IsGroundPAid(node2.pa_id))
				{
					int idx = (node2.pa_id - 1) * PathData.PassableArea.numNodes + (node2.dir - 8);
					if (flag2)
					{
						data.GetPaNodeCords(idx, out openNode.x, out openNode.y);
					}
					else
					{
						data.GetPaNodeCords(idx, out openNode.x, out openNode.y);
					}
				}
				else if (!flag2)
				{
					PathData.PassableAreaNode paNode = data.GetPaNode(openNode.x, openNode.y);
					openNode.x = (int)(paNode.pos.x / data.settings.tile_size);
					openNode.y = (int)(paNode.pos.y / data.settings.tile_size);
				}
				else
				{
					Game.Log("Ecnountered a path node with portal direction and no portal: (" + openNode.x + "," + openNode.y + ")", Game.LogType.Error);
				}
			}
			else if (flag2)
			{
				data.GetNodeCoords(data.GetNodeIdx(openNode.x, openNode.y) + data.NeighborOffset[7 - node2.dir], out openNode.x, out openNode.y);
			}
			else
			{
				data.GetPaNodeCords((openNode.y - 1) * PathData.PassableArea.numNodes + node2.dir, out openNode.x, out openNode.y);
			}
		}
		if (list.Count == 1)
		{
			list.Insert(0, data.GridToWorld(start_x, start_y));
		}
		Smooth(list);
		if (radius > 0f)
		{
			Optimize(list, radius);
		}
		list.Reverse();
		return list;
	}

	public PathFinding(Game game, string map_name)
		: base(game)
	{
		PathFindingBurst.Compile();
		this.map_name = map_name;
		LoadSettings();
		if (game != null && game.type != "battle_view")
		{
			LoadPFData();
		}
	}

	public PathData.HighAdditionalNode GetAdditionalNode(int paid)
	{
		return data.highPFAdditionalNodes[Math.Abs(paid) - 1];
	}

	private List<PPos> ReconstructHighGridPath(Path path, PPos current, bool success = true)
	{
		using (Game.Profile("PathFinding.ReconstructHighGridPath", log: false, settings.multithreaded ? (-1) : 0))
		{
			ResetModifiedHighGridNodes(this);
			if (current == PPos.Zero)
			{
				return new List<PPos> { path.src_pt, path.dst_pt };
			}
			List<PPos> list = new List<PPos>();
			PPos pPos = current;
			if (success && !flee)
			{
				pPos = path.dst_pt;
			}
			else if (current.paID == 0)
			{
				pPos = GridCoordToWorld(WorldToGridCoord(current));
			}
			if (current.paID < 0)
			{
				pPos.paID = 0;
			}
			while (current != PPos.Zero && !(pPos == PPos.Zero))
			{
				if (pPos.paID == 0)
				{
					list.Add(ClampPositionToWorldBounds(pPos));
				}
				else
				{
					list.Add(pPos);
				}
				if (current.paID < 0)
				{
					PathData.HighAdditionalNode additionalNode = GetAdditionalNode(current.paID);
					if (additionalNode.bestComeFrom != null)
					{
						current = GridCoordToWorld(additionalNode.bestComeFrom.coord, offset: false);
						pPos = current;
						current.paID = -additionalNode.bestComeFrom.id;
					}
					else if (additionalNode.bestComeFromTerrain != byte.MaxValue)
					{
						Coord closestReachableTerrain = additionalNode.GetClosestReachableTerrain(this, additionalNode.bestComeFromTerrain);
						current = GridCoordToWorld(closestReachableTerrain, offset: false);
						pPos = GridCoordToWorld(closestReachableTerrain);
					}
					else
					{
						current = PPos.Zero;
					}
				}
				else if (current.paID == 0)
				{
					Coord coord = WorldToGridCoord(current);
					PathData.HighGridNode highGridNode = data.GetHighGridNode(coord.x, coord.y);
					int dirCoords = highGridNode.GetDirCoords();
					if (dirCoords == -1)
					{
						break;
					}
					if (highGridNode.CameFromAdditionalNode)
					{
						int item = 0;
						if (!data.highPFAdditionalGrid.TryGetFirstValue(coord, out item, out var it))
						{
							break;
						}
						int num = dirCoords;
						if (num > 0)
						{
							for (int i = 0; i != num; i++)
							{
								if (!data.highPFAdditionalGrid.TryGetNextValue(out item, ref it))
								{
									break;
								}
							}
						}
						PathData.HighAdditionalNode additionalNode2 = GetAdditionalNode(item);
						current = new PPos(GridCoordToWorld(additionalNode2.coord, offset: false), -item);
						pPos = current;
						pPos.paID = 0;
					}
					else if (dirCoords >= 8)
					{
						if (!data.paIDGrid.TryGetFirstValue(coord, out var item2, out var it2))
						{
							break;
						}
						int num2 = dirCoords - 8;
						if (num2 > 0)
						{
							for (int j = 0; j != num2; j++)
							{
								if (!data.paIDGrid.TryGetNextValue(out item2, ref it2))
								{
									break;
								}
							}
						}
						int x = item2.x;
						current = data.highPassableAreaNodes[x - 1].pos;
						pPos = current;
					}
					else
					{
						Coord dir = GetDir(dirCoords);
						coord -= dir;
						current = GridCoordToWorld(coord, offset: false);
						pPos = GridCoordToWorld(coord);
					}
				}
				else
				{
					PathData.HighPassableAreaNode highPassableAreaNode = data.highPassableAreaNodes[current.paID - 1];
					if (highPassableAreaNode.bestComeFrom != null)
					{
						current = highPassableAreaNode.bestComeFrom.pos;
						pPos = current;
					}
					else if (highPassableAreaNode.bestComeFromTerrain != byte.MaxValue)
					{
						Coord pos = highPassableAreaNode.closest_reachable_terrain[highPassableAreaNode.bestComeFromTerrain];
						current = GridCoordToWorld(pos, offset: false);
						pPos = GridCoordToWorld(pos);
					}
					else
					{
						current = PPos.Zero;
					}
				}
			}
			if (!success && list.Count <= 1)
			{
				list.Add(path.src_pt);
				if (data.pointers.BurstedTrace(path.src_pt, path.dst_pt, out var _, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, cur_path.allowed_area_types, cur_path.battle_side, cur_path.was_inside_wall))
				{
					list.Add(path.dst_pt);
				}
				else
				{
					list.Add(path.src_pt);
				}
			}
			else if (list.Count == 1)
			{
				list.Insert(0, path.src_pt);
			}
			else
			{
				list.Reverse();
				list[0] = path.src_pt;
			}
			highPath_dbg.Clear();
			foreach (PPos item3 in list)
			{
				highPath_dbg.Add(item3);
			}
			return list;
		}
	}

	public bool InReserveBounds(Point pos)
	{
		return InReserveBounds(WorldToReservationGrid(pos));
	}

	public bool InReserveBounds(Coord coord)
	{
		if (coord.x >= 0 && coord.y >= 0 && coord.x < data.reservation_width)
		{
			return coord.y < data.reservation_height;
		}
		return false;
	}

	public Coord WorldToReservationGrid(Point pos)
	{
		if (settings.reserve_grid_size == -1f)
		{
			return Coord.Zero;
		}
		return new Coord((int)(pos.x / settings.reserve_grid_size), (int)(pos.y / settings.reserve_grid_size));
	}

	public Point ReservationGridToWorld(Coord coord)
	{
		if (settings.reserve_grid_size == -1f)
		{
			return Point.Zero;
		}
		return new Point((float)coord.x * settings.reserve_grid_size, (float)coord.y * settings.reserve_grid_size);
	}

	public void ReserveCell(MapObject obj, Point pos)
	{
		Coord coord = WorldToReservationGrid(pos);
		ReserveCell(obj, coord, pos);
	}

	public void ReserveCell(MapObject obj, Coord coord, Point pt)
	{
		if ((cur_path != null && !cur_path.can_reserve) || data?.reservation_grid == null || !InReserveBounds(coord) || obj == null || !obj.IsValid())
		{
			return;
		}
		obj.ClearReserved();
		lock (Lock)
		{
			List<MapObject> list = data.reservation_grid[coord.x, coord.y];
			obj.reserve_pt = pt;
			list.Add(obj);
		}
	}

	public void UnreserveCell(MapObject obj, Coord coord)
	{
		lock (Lock)
		{
			Path path = cur_path;
			if ((path == null || path.can_reserve || path.src_obj != obj) && data?.reservation_grid != null && InReserveBounds(coord))
			{
				data.reservation_grid[coord.x, coord.y]?.Remove(obj);
			}
		}
	}

	public List<MapObject> GetReserved(Point pos)
	{
		Coord coord = WorldToReservationGrid(pos);
		return GetReserved(coord);
	}

	public List<MapObject> GetReserved(Coord coord)
	{
		using (Game.Profile("GetReserved"))
		{
			if (data?.reservation_grid == null)
			{
				return null;
			}
			return data.reservation_grid[coord.x, coord.y];
		}
	}

	public float CalcReservePrio(MapObject obj, PPos pos)
	{
		return obj.position.SqrDist(pos);
	}

	public Coord WorldToGridCoord(Point pos)
	{
		return new Coord((int)(pos.x / settings.grid_tile_size), (int)(pos.y / settings.grid_tile_size));
	}

	public Point GridCoordToWorld(Coord pos, bool offset = true)
	{
		Point point = new Point((float)pos.x * settings.grid_tile_size, (float)pos.y * settings.grid_tile_size);
		if (offset && data.HighCoordsInBounds(pos.x, pos.y))
		{
			return point + data.GetHighGridNode(pos.x, pos.y).GetCellOffset();
		}
		return point + new Point(settings.grid_tile_size / 2f, settings.grid_tile_size / 2f);
	}

	private static int CalcWeight(PathData.Node prev, PathData.Node node, Point pos, PathFinding lpf)
	{
		int num;
		if (node.ocean)
		{
			num = lpf.settings.base_weight;
			if (!prev.ocean)
			{
				if (!lpf.settings.towns_passable)
				{
					return 0;
				}
				if (lpf.state == State.FindUnreservedPoint)
				{
					return 0;
				}
				num = lpf.settings.enter_water_weight_mod;
			}
		}
		else
		{
			num = lpf.data.GetWeight(node, 0f, 0f);
			if (num == 0)
			{
				if (prev.ocean || prev.weight > 0)
				{
					return 0;
				}
				num = 10;
			}
			else if (prev.ocean)
			{
				if (lpf.state == State.FindUnreservedPoint)
				{
					return 0;
				}
				num = lpf.settings.base_weight;
				num *= lpf.settings.enter_water_weight_mod;
			}
		}
		return num;
	}

	public static void AddWave(PathFinding lpf, Point coord, uint new_eval, byte new_dir)
	{
		lpf.data.WorldToGrid(coord, out var x, out var y);
		PathData.Node node = lpf.data.GetNode(x, y);
		node.path_eval = new_eval;
		node.dir = new_dir;
		node.closed = lpf.data.version;
		node.open = true;
		lpf.data.ModifyNode(x, y, node);
		lpf.HeapPush(x, y, node.path_eval);
	}

	public static void ModifyEval(Point coord, uint new_eval, byte new_dir, PathFinding lpf)
	{
		lpf.data.WorldToGrid(coord, out var x, out var y);
		PathData.Node node = lpf.data.GetNode(x, y);
		node.path_eval = new_eval;
		node.dir = new_dir;
		node.closed = lpf.data.version;
	}

	public static PPos PopWave(PathFinding lpf, out PathData.Node res, out int x, out int y)
	{
		OpenNode openNode = lpf.HeapPop();
		x = openNode.x;
		y = openNode.y;
		res = lpf.data.GetNode(openNode.x, openNode.y);
		return lpf.data.GridToWorld(openNode.x, openNode.y);
	}

	private static void ReconstructDebugWave(PathFinding lpf, Coord start_coord, PPos start_pos, int node_order)
	{
		if (!debugging)
		{
			return;
		}
		start_pos = new PPos((int)start_pos.x, (int)start_pos.y);
		PathData.Node node = lpf.data.GetNode(start_pos);
		if (node.closed == 0 || (node.weight == 0 && !node.ocean))
		{
			return;
		}
		int num = -1;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j == 0 && i == 0)
				{
					continue;
				}
				num++;
				Coord pos = start_coord + new Coord(j, i);
				Point point = lpf.GridCoordToWorld(pos);
				if (lpf.data.GetNode(point).closed != node.closed)
				{
					continue;
				}
				List<PPos> list = new List<PPos>();
				Point point2 = point;
				while (true)
				{
					list.Add(point2);
					PathData.Node node2 = lpf.data.GetNode(point2);
					byte dir = node2.dir;
					if (dir == byte.MaxValue || node2.closed != node.closed || point2 == start_pos || node2.closed == 0 || (node2.weight == 0 && !node2.ocean))
					{
						break;
					}
					if (dir == 12)
					{
						Coord riverOffset = GetRiverOffset(node2.river_offset);
						point2 += new Point(riverOffset.x, riverOffset.y);
					}
					else
					{
						Coord dir2 = lpf.GetDir(dir);
						point2 -= new Point(dir2.x, dir2.y);
					}
				}
			}
		}
	}

	public bool BeginModifyHighGrid(PPos pos, bool is_regular_pf, bool stop_impassable, float max_length_mod = 1.5f, ushort[] modified_node_weights = null, Coord[] modified_nodes_coord = null)
	{
		Coord coord = (modify_high_grid_start_coords = WorldToGridCoord(pos));
		modify_high_grid_pos = pos;
		modify_high_grid_original_start_x = start_x;
		modify_high_grid_original_start_y = start_y;
		modify_high_grid_original_goal_x = goal_x;
		modify_high_grid_original_goal_y = goal_y;
		modify_high_grid_original_start_ocean = start_ocean;
		modify_high_grid_original_goal_ocean = goal_ocean;
		if (!is_regular_pf)
		{
			pos = GridCoordToWorld(coord);
		}
		PathData.Node node = data.GetNode(pos);
		modify_high_grid_max_tile_size_mod = max_length_mod;
		data.WorldToGrid(pos, out start_x, out start_y);
		start_ocean = data.GetNode(start_x, start_y).ocean;
		if (modified_node_weights != null && is_regular_pf)
		{
			modified_nodes_coord[modified_node_count] = coord;
			for (int i = 0; i < 8; i++)
			{
				modified_node_weights[8 * modified_node_count + i] = data.GetHighNodeWeight(coord.x, coord.y, i);
			}
			modified_node_count++;
		}
		for (int j = 0; j < 8; j++)
		{
			data.SetHighNodeWeight(coord.x, coord.y, j, ushort.MaxValue);
		}
		if (node.weight <= 0 && !node.ocean && stop_impassable)
		{
			return false;
		}
		return true;
	}

	public static PathData.HighGridNode ModifyHighAdditional(PPos pos, PathFinding lpf, bool stop_impassable = true, float max_length_mod = 1.5f)
	{
		using (Game.Profile("PathFinding.ModifyHighGrid", log: false, lpf.settings.multithreaded ? (-1) : 0, pfs_ModifyHighGrid))
		{
			lpf.data.IncVersion();
			int highPFGrid_width = lpf.data.highPFGrid_width;
			int highPFGrid_height = lpf.data.highPFGrid_height;
			float grid_tile_size = lpf.settings.grid_tile_size;
			Coord start = lpf.WorldToGridCoord(pos);
			PathData.Node node = lpf.data.GetNode(pos);
			PathData.HighGridNode highGridNode = lpf.data.GetHighGridNode(start.x, start.y);
			if (node.weight <= 0 && !node.ocean && stop_impassable)
			{
				return highGridNode;
			}
			lpf.open.Clear();
			AddWave(lpf, pos, 0u, 0);
			int found = 0;
			while (lpf.open.Count > 0)
			{
				PathData.Node res;
				int x;
				int y;
				PPos pPos = PopWave(lpf, out res, out x, out y);
				res.open = false;
				lpf.data.ModifyNode(x, y, res);
				float x2 = pPos.x;
				float y2 = pPos.y;
				if (Math.Abs(x2 - pos.x) > grid_tile_size * max_length_mod || Math.Abs(y2 - pos.y) > grid_tile_size * max_length_mod)
				{
					continue;
				}
				int num = -1;
				for (int i = -1; i <= 1; i++)
				{
					float num2 = y2 * lpf.settings.tile_size + (float)i;
					for (int j = -1; j <= 1; j++)
					{
						if (i == 0 && j == 0)
						{
							continue;
						}
						num++;
						float num3 = x2 * lpf.settings.tile_size + (float)j;
						if (!(num3 >= 0f) || !(num3 < (float)highPFGrid_width * grid_tile_size) || !(num2 >= 0f) || !(num2 < (float)highPFGrid_height * grid_tile_size))
						{
							continue;
						}
						PPos pPos2 = new PPos(num3, num2);
						PathData.Node node2 = lpf.data.GetNode(pPos2);
						int num4 = CalcWeight(res, node2, pPos2, lpf);
						if (num4 == 0)
						{
							continue;
						}
						float num5 = 5f;
						if (j != 0 && i != 0)
						{
							num5 = 7f;
						}
						uint num6 = (uint)((float)res.path_eval + (float)num4 * num5);
						bool flag = node2.closed == lpf.data.version;
						if (flag)
						{
							if (num6 < node2.path_eval)
							{
								ModifyEval(pPos2, num6, (byte)num, lpf);
							}
							continue;
						}
						AddWave(lpf, pPos2, num6, (byte)num);
						if (CheckIsGoalAdditional(pPos2, start, grid_tile_size, highPFGrid_width, highPFGrid_height, lpf, ref found, flag) && found == 9)
						{
							return highGridNode;
						}
					}
				}
			}
			return highGridNode;
		}
	}

	private static bool CheckIsGoalAdditional(Point pos, Coord start, float tile_size, int width, int height, PathFinding lpf, ref int found, bool was_closed = false)
	{
		int num = -1;
		pos = new Point((int)Math.Floor(pos.x), (int)Math.Floor(pos.y));
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				int num2 = start.x + j;
				int num3 = start.y + i;
				Point point = lpf.GridCoordToWorld(new Coord(num2, num3));
				if (i == 0 && j == 0)
				{
					if (!(Math.Abs(pos.x - point.x) < lpf.settings.tile_size) || !(Math.Abs(pos.y - point.y) < lpf.settings.tile_size) || num2 < 0 || num2 >= width || num3 < 0 || num3 >= height)
					{
						continue;
					}
					PathData.Node node = lpf.data.GetNode(pos);
					if (node.path_eval >= 65535)
					{
						node.path_eval = 65535u;
					}
					if (lpf.data.highPFAdditionalGrid.TryGetFirstValue(start, out var item, out var it))
					{
						lpf.GetAdditionalNode(item).terrain_weights[8] = (ushort)node.path_eval;
						while (lpf.data.highPFAdditionalGrid.TryGetNextValue(out item, ref it))
						{
							lpf.GetAdditionalNode(item).terrain_weights[8] = (ushort)node.path_eval;
						}
						if (!was_closed)
						{
							found++;
						}
					}
					continue;
				}
				num++;
				if (!(Math.Abs(pos.x - point.x) < lpf.settings.tile_size) || !(Math.Abs(pos.y - point.y) < lpf.settings.tile_size))
				{
					continue;
				}
				if (num2 >= 0 && num2 < width && num3 >= 0 && num3 < height)
				{
					PathData.Node node2 = lpf.data.GetNode(pos);
					if (node2.path_eval >= 65535)
					{
						node2.path_eval = 65535u;
					}
					if (lpf.data.highPFAdditionalGrid.TryGetFirstValue(start, out var item2, out var it2))
					{
						lpf.GetAdditionalNode(item2).terrain_weights[num] = (ushort)node2.path_eval;
						while (lpf.data.highPFAdditionalGrid.TryGetNextValue(out item2, ref it2))
						{
							lpf.GetAdditionalNode(item2).terrain_weights[num] = (ushort)node2.path_eval;
						}
						if (!was_closed)
						{
							found++;
						}
					}
				}
				return true;
			}
		}
		return false;
	}

	private static bool IsGoalHighPF(PathFinding lpf, PPos current, PPos dest)
	{
		if (lpf.flee)
		{
			if (current.paID == 0)
			{
				Coord coord = lpf.WorldToGridCoord(current);
				return lpf.data.GetHighGridNode(coord.x, coord.y).estimate == 0;
			}
			if (current.paID > 0)
			{
				return lpf.data.highPassableAreaNodes[current.paID - 1].estimate == 0;
			}
			return false;
		}
		return current.paID == dest.paID;
	}

	private static bool IsGoalHighPF(PathFinding lpf, Coord current_coord, Coord dest_coord, PPos current, PPos dest, float r2)
	{
		if (lpf.flee)
		{
			return IsGoalHighPF(lpf, current, dest);
		}
		if ((!(current_coord == dest_coord) || dest.paID != 0) && (!(current.SqrDist(dest) <= r2) || dest.paID != 0))
		{
			return false;
		}
		return CheckReachable(lpf.data, current, dest);
	}

	private static void ResetModifiedHighGridNodes(PathFinding lpf)
	{
		if (lpf.modified_node_weights == null || lpf.modified_node_count == 0)
		{
			return;
		}
		for (int num = lpf.modified_node_count - 1; num >= 0; num--)
		{
			Coord coord = lpf.modified_nodes_coord[num];
			for (int i = 0; i < 8; i++)
			{
				lpf.data.SetHighNodeWeight(coord.x, coord.y, i, lpf.modified_node_weights[num * 8 + i]);
			}
		}
		lpf.modified_node_count = 0;
	}

	private bool SkipHighPF(Path path, bool low_level_only)
	{
		if (data?.highPFGrid == null || data.highPFGrid.Length == 0 || path.low_level_only || low_level_only || (path.flee && !path.modified_flee))
		{
			return true;
		}
		return false;
	}

	private PathData.HighAdditionalNode GetAdditionalNode(PPos pt)
	{
		if (pt.paID > 0)
		{
			return null;
		}
		PathData pathData = data;
		if (pathData != null)
		{
			_ = pathData.highPFAdditionalGrid;
			if (0 == 0)
			{
				if (!data.GetNode(pt).ocean)
				{
					return null;
				}
				Coord coord = WorldToGridCoord(pt);
				if (data.highPFAdditionalGrid.TryGetFirstValue(coord, out var item, out var it))
				{
					PathData.HighAdditionalNode additionalNode = GetAdditionalNode(item);
					if (additionalNode.coord == coord)
					{
						return additionalNode;
					}
					while (data.highPFAdditionalGrid.TryGetNextValue(out item, ref it))
					{
						additionalNode = GetAdditionalNode(item);
						if (additionalNode.coord == coord)
						{
							return additionalNode;
						}
					}
				}
				return null;
			}
		}
		return null;
	}

	private List<PPos> GetHighGridPath(Path path, bool low_level_only)
	{
		using (Game.Profile("PathFinding.GetHighGridPath", log: false, settings.multithreaded ? (-1) : 0))
		{
			if (SkipHighPF(path, low_level_only))
			{
				return new List<PPos> { path.src_pt, path.dst_pt };
			}
			Coord dest_coord = WorldToGridCoord(path.dst_pt);
			highGridOpen.Clear();
			best_high_node = new OpenHighNode(new PPos(-1f, -1f), uint.MaxValue);
			data.IncHighPFVersion();
			PathData.HighAdditionalNode additionalNode = GetAdditionalNode(path.src_pt);
			if (additionalNode != null)
			{
				PPos pPos = GridCoordToWorld(additionalNode.coord, offset: false);
				pPos.paID = -additionalNode.id;
				additionalNode.bestComeFrom = null;
				additionalNode.path_eval = 0u;
				additionalNode.estimate = EstimateHighGrid(pPos, path.dst_pt);
				additionalNode.closed = data.high_pf_version;
				additionalNode.bestComeFromTerrain = byte.MaxValue;
				HeapPushHighNode(pPos, 0u);
			}
			else
			{
				if (path.src_pt.paID == 0)
				{
					Coord coord = WorldToGridCoord(path.src_pt);
					PathData.HighGridNode highGridNode = data.GetHighGridNode(coord.x, coord.y);
					highGridNode.path_eval = 0u;
					highGridNode.estimate = EstimateHighGrid(path.src_pt, path.dst_pt);
					highGridNode.Dir = 15;
					highGridNode.Open = true;
					data.SetHighNodeClosed(coord.x, coord.y, data.high_pf_version);
					data.SetHighNode(coord.x, coord.y, highGridNode);
				}
				else
				{
					PathData.HighPassableAreaNode highPassableAreaNode = data.highPassableAreaNodes[path.src_pt.paID - 1];
					highPassableAreaNode.closed = data.high_pf_version;
					highPassableAreaNode.bestComeFrom = null;
					highPassableAreaNode.bestComeFromTerrain = byte.MaxValue;
					highPassableAreaNode.estimate = EstimateHighGrid(path.src_pt, path.dst_pt);
					highPassableAreaNode.path_eval = 0u;
				}
				HeapPushHighNode(path.src_pt, 0u);
			}
			int highPFGrid_width = data.highPFGrid_width;
			int highPFGrid_height = data.highPFGrid_height;
			while (highGridOpen.Count != 0)
			{
				total_high_steps++;
				PPos pos = HeapPopHighNode().pos;
				if (pos.paID < 0)
				{
					Coord current_coord = WorldToGridCoord(pos);
					if (IsGoalHighPF(this, current_coord, dest_coord, pos, path.dst_pt, r2))
					{
						return ReconstructHighGridPath(path, pos);
					}
					PathData.HighAdditionalNode additionalNode2 = GetAdditionalNode(pos.paID);
					if (best_high_node.pos.x < 0f || additionalNode2.estimate < best_high_node.eval)
					{
						data.WorldToGrid(pos, out var _, out var _);
						best_high_node = new OpenHighNode(pos, additionalNode2.estimate);
					}
					ExitHighAdditionalNode(additionalNode2, path.dst_pt);
					for (int i = 0; i < additionalNode2.ribs.Count; i++)
					{
						PathData.HighAdditionalRib highAdditionalRib = additionalNode2.ribs[i];
						PathData.HighAdditionalNode other = highAdditionalRib.GetOther(additionalNode2);
						EvalHighGridNeighbor(additionalNode2, other, highAdditionalRib.weight, path.dst_pt);
					}
					continue;
				}
				if (pos.paID > 0)
				{
					if (IsGoalHighPF(this, pos, path.dst_pt))
					{
						return ReconstructHighGridPath(path, pos);
					}
					PathData.HighPassableAreaNode highPassableAreaNode2 = data.highPassableAreaNodes[pos.paID - 1];
					if (best_high_node.pos.x < 0f || highPassableAreaNode2.estimate < best_high_node.eval)
					{
						data.WorldToGrid(pos, out var x2, out var y2);
						if (ValidToEndOn(x2, y2) || best_high_node.pos.x < 0f)
						{
							best_high_node = new OpenHighNode(pos, highPassableAreaNode2.estimate);
						}
					}
					ExitHighPassableArea(highPassableAreaNode2, path.dst_pt);
					for (int j = 0; j < highPassableAreaNode2.ribs.Count; j++)
					{
						PathData.HighPassableAreaRib highPassableAreaRib = highPassableAreaNode2.ribs[j];
						if (highPassableAreaRib.weight != 0)
						{
							PathData.HighPassableAreaNode other2 = highPassableAreaRib.GetOther(highPassableAreaNode2);
							EvalHighGridNeighbor(highPassableAreaNode2, other2, highPassableAreaRib.weight, path.dst_pt);
						}
					}
					continue;
				}
				Coord coord2 = WorldToGridCoord(pos);
				if (IsGoalHighPF(this, coord2, dest_coord, GridCoordToWorld(coord2), path.dst_pt, r2))
				{
					return ReconstructHighGridPath(path, pos);
				}
				PathData.HighGridNode highGridNode2 = data.GetHighGridNode(coord2.x, coord2.y);
				if (!highGridNode2.Open)
				{
					continue;
				}
				if (best_high_node.pos.x < 0f || highGridNode2.estimate < best_high_node.eval)
				{
					best_high_node = new OpenHighNode(pos, highGridNode2.estimate);
				}
				highGridNode2.Open = false;
				EnterHighPassableArea(coord2, path.dst_pt);
				EnterHighAdditionalNode(coord2, path.dst_pt);
				int num = -1;
				for (int k = -1; k <= 1; k++)
				{
					int num2 = coord2.y + k;
					for (int l = -1; l <= 1; l++)
					{
						if (l != 0 || k != 0)
						{
							num++;
							int num3 = coord2.x + l;
							if (num3 >= 0 && num3 < highPFGrid_width && num2 >= 0 && num2 < highPFGrid_height)
							{
								EvalHighGridNeighbor(neighbor: new Coord(num3, num2), current: coord2, dir: (byte)num, dst_pt: path.dst_pt);
							}
						}
					}
				}
			}
			return ReconstructHighGridPath(path, best_high_node.pos, success: false);
		}
	}

	private void EnterHighPassableArea(Coord coord, PPos dst_pt)
	{
		if (data.GetHighGridNode(coord.x, coord.y).HasPassableArea && data.paIDGrid.TryGetFirstValue(coord, out var item, out var it))
		{
			EnterHighPassableArea(coord, dst_pt, item.x, item.y);
			while (data.paIDGrid.TryGetNextValue(out item, ref it))
			{
				EnterHighPassableArea(coord, dst_pt, item.x, item.y);
			}
		}
	}

	private void EnterHighAdditionalNode(Coord coord, PPos dst_pt)
	{
		if (data.GetHighGridNode(coord.x, coord.y).HasAdditionalNodes && data.highPFAdditionalGrid.TryGetFirstValue(coord, out var item, out var it))
		{
			EnterHighAdditionalNode(coord, dst_pt, item);
			while (data.highPFAdditionalGrid.TryGetNextValue(out item, ref it))
			{
				EnterHighAdditionalNode(coord, dst_pt, item);
			}
		}
	}

	private void EnterHighAdditionalNode(Coord coord, PPos dst_pt, int paid)
	{
		PathData.HighGridNode highGridNode = data.GetHighGridNode(coord.x, coord.y);
		PathData.HighAdditionalNode additionalNode = GetAdditionalNode(paid);
		Coord coord2 = coord - additionalNode.coord;
		byte dir = GetDir(coord2.x, coord2.y);
		ushort num = additionalNode.terrain_weights[dir];
		if (num < ushort.MaxValue)
		{
			PPos pPos = GridCoordToWorld(additionalNode.coord, offset: false);
			pPos.paID = -paid;
			uint num2 = highGridNode.path_eval + num;
			if (additionalNode.closed != data.high_pf_version || num2 < additionalNode.path_eval)
			{
				additionalNode.bestComeFrom = null;
				additionalNode.path_eval = num2;
				additionalNode.estimate = EstimateHighGrid(pPos, dst_pt);
				additionalNode.closed = data.high_pf_version;
				additionalNode.bestComeFromTerrain = dir;
				HeapPushHighNode(pPos, additionalNode.path_eval + additionalNode.estimate);
			}
		}
	}

	private void ExitHighAdditionalNode(PathData.HighAdditionalNode node, PPos dst_pt)
	{
		if (node.terrain_weights == null)
		{
			return;
		}
		for (int i = 0; i < node.terrain_weights.Length; i++)
		{
			Coord closestReachableTerrain = node.GetClosestReachableTerrain(this, (byte)i);
			if (closestReachableTerrain == Coord.Zero || !data.HighCoordsInBounds(closestReachableTerrain.x, closestReachableTerrain.y))
			{
				continue;
			}
			Point point = GridCoordToWorld(closestReachableTerrain);
			Point point2 = GridCoordToWorld(closestReachableTerrain, offset: false);
			PathData.HighGridNode highGridNode = data.GetHighGridNode(closestReachableTerrain.x, closestReachableTerrain.y);
			ushort highNodeClosed = data.GetHighNodeClosed(closestReachableTerrain.x, closestReachableTerrain.y);
			byte b = 0;
			if (!data.highPFAdditionalGrid.TryGetFirstValue(closestReachableTerrain, out var item, out var it))
			{
				continue;
			}
			if (item != node.id)
			{
				while (data.highPFAdditionalGrid.TryGetNextValue(out item, ref it))
				{
					b++;
					if (item == node.id)
					{
						break;
					}
				}
			}
			ushort num = node.terrain_weights[i];
			if (num != 0 && num < ushort.MaxValue)
			{
				uint num2 = node.path_eval + num;
				if (highNodeClosed != data.high_pf_version || num2 < highGridNode.path_eval)
				{
					highGridNode.Dir = b;
					highGridNode.CameFromAdditionalNode = true;
					highGridNode.Open = true;
					highGridNode.path_eval = num2;
					highGridNode.estimate = EstimateHighGrid(point, dst_pt);
					HeapPushHighNode(point2, highGridNode.estimate + highGridNode.path_eval);
					data.SetHighNodeClosed(closestReachableTerrain.x, closestReachableTerrain.y, data.high_pf_version);
					data.SetHighNode(closestReachableTerrain.x, closestReachableTerrain.y, highGridNode);
				}
			}
		}
	}

	private float LadderModWeight(PathData.PassableArea area, int paid)
	{
		float result = 1f;
		if (cur_path.src_obj != null && area.type == PathData.PassableArea.Type.Ladder && cur_path.src_obj is Squad squad && squad.battle.ladders != null && squad.battle.ladders.TryGetValue(paid, out var value))
		{
			for (int i = 0; i < value.squads.Count; i++)
			{
				Squad squad2 = value.squads[i];
				if (squad2 != squad)
				{
					result = settings.occupied_ladder_mod;
					break;
				}
				if (squad2.climbing)
				{
					result = settings.occupied_by_me_ladder_mod;
					break;
				}
			}
		}
		return result;
	}

	public static uint CalcGroundWeight(PathFinding lpf, Point currentPos, Point dst_pt, float min_radius = 0f, float max_radius = 0f)
	{
		uint num = 0u;
		Coord tile = Coord.WorldToGrid(currentPos, 1f);
		Point ptLocal = Coord.WorldToLocal(tile, currentPos, 1f);
		Point destLocal = Coord.WorldToLocal(tile, dst_pt, 1f);
		Coord t = Coord.Invalid;
		Coord t2 = Coord.Invalid;
		PathData.Node node = lpf.data.GetNode(currentPos);
		do
		{
			PathData.Node node2 = lpf.data.GetNode(currentPos);
			int num2;
			if (node2.ocean)
			{
				num2 = lpf.settings.base_weight;
			}
			else
			{
				num2 = lpf.data.GetWeight(node2, min_radius, max_radius);
				if (num2 == 0)
				{
					num2 = 10;
				}
				else if (node.ocean)
				{
					num2 *= 3;
				}
			}
			int num3 = 7;
			num += (uint)(num3 * num2);
			currentPos = Coord.GridToWorld(tile, 1f);
		}
		while (Coord.RayStep(ref tile, ref ptLocal, ref destLocal, 0f, out t, out t2));
		return num;
	}

	public static uint CalcPassableAreaWeight(int base_weight, Point currentPos, Point dst_pt)
	{
		uint num = 0u;
		Coord tile = Coord.WorldToGrid(currentPos, 1f);
		Point ptLocal = Coord.WorldToLocal(tile, currentPos, 1f);
		Point destLocal = Coord.WorldToLocal(tile, dst_pt, 1f);
		Coord t = Coord.Invalid;
		Coord t2 = Coord.Invalid;
		do
		{
			int num2 = 7;
			num += (uint)(num2 * base_weight);
			currentPos = Coord.GridToWorld(tile, 1f);
		}
		while (Coord.RayStep(ref tile, ref ptLocal, ref destLocal, 0f, out t, out t2));
		return num;
	}

	private void EnterHighPassableArea(Coord coord, PPos dst_pt, int paid, int weight)
	{
		using (Game.Profile("PathFinding.EnterHighPassableArea", log: false, settings.multithreaded ? (-1) : 0))
		{
			if (paid == 0)
			{
				return;
			}
			PathData.PassableArea pA = data.pointers.GetPA(paid - 1);
			if (!pA.CanEnter(cur_path, this, paid))
			{
				return;
			}
			PathData.HighGridNode highGridNode = data.GetHighGridNode(coord.x, coord.y);
			PathData.HighPassableAreaNode highPassableAreaNode = data.highPassableAreaNodes[paid - 1];
			uint num = (uint)((float)(uint)weight * ReserveMod(highPassableAreaNode.pos) * (1f + ThreatMod(highPassableAreaNode.pos)) * settings.AreaTypeWeight(pA.type));
			if (num >= 65535)
			{
				return;
			}
			uint num2 = highGridNode.path_eval + num;
			if (highPassableAreaNode.closed != data.high_pf_version || num2 < highPassableAreaNode.path_eval)
			{
				int num3 = highPassableAreaNode.closest_reachable_terrain.IndexOf(coord);
				if (num3 != -1)
				{
					highPassableAreaNode.bestComeFrom = null;
					highPassableAreaNode.path_eval = num2;
					highPassableAreaNode.estimate = EstimateHighGrid(highPassableAreaNode.pos, dst_pt);
					highPassableAreaNode.closed = data.high_pf_version;
					highPassableAreaNode.bestComeFromTerrain = (byte)num3;
					HeapPushHighNode(highPassableAreaNode.pos, highPassableAreaNode.path_eval + highPassableAreaNode.estimate);
				}
			}
		}
	}

	private void ExitHighPassableArea(PathData.HighPassableAreaNode node, PPos dst_pt)
	{
		using (Game.Profile("PathFinding.ExitHighPassableArea", log: false, settings.multithreaded ? (-1) : 0))
		{
			if (node.closest_reachable_terrain == null)
			{
				return;
			}
			for (int i = 0; i < node.closest_reachable_terrain.Count; i++)
			{
				Coord coord = node.closest_reachable_terrain[i];
				Point point = GridCoordToWorld(coord);
				Point point2 = GridCoordToWorld(coord, offset: false);
				PathData.HighGridNode highGridNode = data.GetHighGridNode(coord.x, coord.y);
				byte b = 8;
				if (!data.paIDGrid.TryGetFirstValue(coord, out var item, out var it))
				{
					continue;
				}
				int x = item.x;
				int y = item.y;
				if (x != node.pos.paID)
				{
					while (data.paIDGrid.TryGetNextValue(out item, ref it))
					{
						b++;
						int x2 = item.x;
						y = item.y;
						if (x2 == node.pos.paID)
						{
							break;
						}
					}
				}
				uint num = (uint)((float)y * (1f + ThreatMod(point2)) * ReserveMod(point2));
				uint num2 = node.path_eval + num;
				if (data.GetHighNodeClosed(coord.x, coord.y) != data.high_pf_version || num2 < highGridNode.path_eval)
				{
					highGridNode.Dir = b;
					highGridNode.Open = true;
					highGridNode.path_eval = num2;
					highGridNode.estimate = EstimateHighGrid(point, dst_pt);
					HeapPushHighNode(point2, highGridNode.estimate + highGridNode.path_eval);
					data.SetHighNodeClosed(coord.x, coord.y, data.high_pf_version);
					data.SetHighNode(coord.x, coord.y, highGridNode);
				}
			}
		}
	}

	private ushort EstimateHighGrid(PPos a, PPos b)
	{
		int num = (int)a.x;
		int num2 = (int)a.y;
		int num3 = (int)b.x;
		int num4 = (int)b.y;
		int val = Math.Abs(num - num3);
		int val2 = Math.Abs(num2 - num4);
		int num5 = Math.Max(val, val2);
		int num6 = Math.Min(val, val2);
		int num7 = num6;
		int num8 = num5 - num6;
		ushort num9 = (ushort)(settings.estimate_weight * (num8 * 5 + num7 * 7));
		if (flee)
		{
			num9 = (ushort)Math.Max(0, est_range - num9);
			if (num9 < 0)
			{
				num9 = 0;
			}
		}
		return num9;
	}

	private void EvalHighGridNeighbor(Coord current, Coord neighbor, byte dir, PPos dst_pt)
	{
		using (Game.Profile("PathFinding.EvalHighGridNeighborTerrain", log: false, settings.multithreaded ? (-1) : 0))
		{
			PathData.HighGridNode highGridNode = data.GetHighGridNode(current.x, current.y);
			PathData.HighGridNode highGridNode2 = data.GetHighGridNode(neighbor.x, neighbor.y);
			ushort highNodeWeight = data.GetHighNodeWeight(current.x, current.y, dir);
			Point point = GridCoordToWorld(neighbor);
			if (highNodeWeight >= ushort.MaxValue)
			{
				return;
			}
			float num = ThreatMod(point);
			highNodeWeight = (ushort)((float)(int)highNodeWeight * ReserveMod(point) * (1f + num));
			uint path_eval = highGridNode.path_eval;
			if (cur_path != null && cur_path.flee)
			{
				Point point2 = GridCoordToWorld(current);
				PathData.Node node = data.GetNode(point2);
				if (data.GetNode(point).water != node.water)
				{
					return;
				}
			}
			uint num2 = path_eval + highNodeWeight;
			bool flag = data.GetHighNodeClosed(neighbor.x, neighbor.y) == data.high_pf_version;
			if (!flag || num2 < highGridNode2.path_eval)
			{
				Point point3 = GridCoordToWorld(neighbor, offset: false);
				if (highGridNode.Dir == 7 - dir && flag && !highGridNode.CameFromAdditionalNode)
				{
					Game.Log("High level pathfinding is attempting to go backwards, consult the council of pathfinding elders", Game.LogType.Error);
					return;
				}
				highGridNode2.Dir = dir;
				highGridNode2.Open = true;
				highGridNode2.path_eval = num2;
				highGridNode2.estimate = EstimateHighGrid(point3, dst_pt);
				HeapPushHighNode(point3, highGridNode2.estimate + highGridNode2.path_eval);
				data.SetHighNodeClosed(neighbor.x, neighbor.y, data.high_pf_version);
				data.SetHighNode(neighbor.x, neighbor.y, highGridNode2);
			}
		}
	}

	private void EvalHighGridNeighbor(PathData.HighPassableAreaNode current, PathData.HighPassableAreaNode neighbor, ushort wt, PPos dst_pt)
	{
		using (Game.Profile("PathFinding.EvalHighGridNeighborPassableAreas", log: false, settings.multithreaded ? (-1) : 0))
		{
			PathData.PassableArea pA = data.pointers.GetPA(neighbor.pos.paID - 1);
			if (!pA.CanEnter(cur_path, this, neighbor.pos.paID, current.pos.paID))
			{
				return;
			}
			uint num = (uint)((float)(int)wt * ReserveMod(neighbor.pos) * (1f + ThreatMod(neighbor.pos)) * settings.AreaTypeWeight(pA.type));
			if (num < 65535)
			{
				uint num2 = current.path_eval + num;
				num2 = (uint)((float)num2 * LadderModWeight(pA, neighbor.pos.paID));
				if ((neighbor.closed != data.high_pf_version || num2 < neighbor.path_eval) && (current.bestComeFrom == null || current.bestComeFrom != neighbor))
				{
					neighbor.bestComeFrom = current;
					neighbor.path_eval = num2;
					neighbor.estimate = EstimateHighGrid(neighbor.pos, dst_pt);
					HeapPushHighNode(neighbor.pos, neighbor.path_eval + neighbor.estimate);
					neighbor.closed = data.high_pf_version;
				}
			}
		}
	}

	private void EvalHighGridNeighbor(PathData.HighAdditionalNode current, PathData.HighAdditionalNode neighbor, ushort wt, PPos dst_pt)
	{
		if (wt < ushort.MaxValue)
		{
			uint num = current.path_eval + wt;
			if (neighbor.closed != data.high_pf_version || num < neighbor.path_eval)
			{
				neighbor.bestComeFrom = current;
				neighbor.path_eval = num;
				PPos pPos = GridCoordToWorld(neighbor.coord, offset: false);
				neighbor.estimate = EstimateHighGrid(pPos, dst_pt);
				pPos.paID = -neighbor.id;
				HeapPushHighNode(pPos, neighbor.path_eval + neighbor.estimate);
				neighbor.closed = data.high_pf_version;
			}
		}
	}

	public unsafe bool CheckValid()
	{
		if (data?.nodes != null)
		{
			PathData pathData = data;
			if (pathData != null)
			{
				_ = pathData.pointers;
				if (true && data.pointers.Initted != null && *data.pointers.Initted && data.highPFGrid != null)
				{
					return data.highPassableAreaNodes != null;
				}
			}
		}
		return false;
	}

	private void Finish(bool force_fail = false)
	{
		state = State.Finished;
		Path path = cur_path;
		List<PPos> path2 = null;
		if (!force_fail)
		{
			using (Game.Profile("PathFinding.ConstructFinalPath", log: false, settings.multithreaded ? (-1) : 0))
			{
				path2 = ConstructFinalPath();
			}
		}
		using (Game.Profile("PathFinding.NotifyCompleted", log: false, settings.multithreaded ? (-1) : 0))
		{
			lock (Lock)
			{
				if (path.state == Path.State.Pending)
				{
					path.state = ((success && !force_fail) ? Path.State.Succeeded : Path.State.Failed);
					pending.Remove(path);
					path.SetPath(path2);
					if (path.modified_flee)
					{
						path.flee = true;
					}
					if (settings.multithreaded)
					{
						completed.Add(path);
					}
					else
					{
						NotifyCompleted(path);
					}
				}
			}
		}
		cur_path = null;
	}

	private bool PerformSteps(int max_count, float bounds_center_x = -1f, float bounds_center_y = -1f, float max_tile_size = -1f, bool cheap_water_enter = true, bool force_no_pas = false)
	{
		using (Game.Profile("PathFinding.Steps", log: false, settings.multithreaded ? (-1) : 0, pfs_LowLevelSteps))
		{
			for (int i = 0; i < 1000; i++)
			{
				if (!Step(bounds_center_x, bounds_center_y, max_tile_size, cheap_water_enter, force_no_pas))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void SetupHighGridPath(Path path, bool low_level_only)
	{
		total_steps = 0;
		max_open = 0;
		data.IncVersion();
		highPath_dbg.Clear();
		if (path.src_pt.paID == -1)
		{
			path.src_pt.paID = 0;
		}
		Squad squad = path.src_obj as Squad;
		powerGrids = null;
		if (squad?.def != null && squad.battle.power_grids != null && squad.battle.power_grids[1 - squad.battle_side] != null)
		{
			BattleAI battleAI = squad.battle.ai[squad.battle_side];
			if (squad.battle?.ai != null && !path.force_no_threat && ((squad.simulation != null && squad.simulation.state == BattleSimulation.Squad.State.Retreating) || battleAI.HasOwnerFlag(squad, BattleAI.EnableFlags.Pathfinding)))
			{
				path.check_threat = true;
				if (squad.def.is_cavalry)
				{
					path.check_anti_cavalry_threat = true;
				}
				else if (squad.def.is_ranged)
				{
					path.check_anti_ranged_threat = true;
				}
				powerGrids = squad.battle.power_grids;
			}
		}
		r2 = path.range * path.range;
		flee = path.flee;
		est_range = 0;
		fixed_max_steps = -1;
		if (SkipHighPF(path, low_level_only))
		{
			state = State.CalcHighGridPath;
		}
		else
		{
			state = State.ModifyHighGridStart;
		}
	}

	private void CalcHighGridPath(Path path, bool low_level_only)
	{
		highGridPath = GetHighGridPath(path, low_level_only);
		data.IncVersion();
		tried_ahead = false;
		best_node = new OpenNode(-1, -1, uint.MaxValue);
		SetPTStart(path.src_pt);
		ResetPTGoal();
	}

	public bool Process(bool force_valid = false, bool low_level_only = false)
	{
		if (!CheckValid() && !force_valid)
		{
			return true;
		}
		using (Game.Profile("PathFinding.Process", log: false, settings.multithreaded ? (-1) : 0, pfs_Process))
		{
			try
			{
				Path path;
				lock (Lock)
				{
					if (pending.Count <= 0)
					{
						state = State.Idle;
						return false;
					}
					path = pending[0];
					if (path != cur_path)
					{
						if (cur_path == null)
						{
							state = State.NewPath;
							cur_path = path;
							if (path.dst_obj != null && path.look_ahead)
							{
								Movement movement = path.src_obj?.movement;
								Movement movement2 = path.dst_obj?.movement;
								if (movement2 != null && movement != null && movement2.path != null)
								{
									movement2.path.GetPathPoint(movement2.path.t + movement.speed / 2f, out var _, out path.dst_pt);
								}
							}
						}
						else
						{
							path = cur_path;
						}
					}
					if (path.state == Path.State.Stopped || (path.src_obj != null && !path.src_obj.IsValid()))
					{
						pending.Remove(path);
						state = State.Finished;
						cur_path = null;
						return true;
					}
				}
				if (data == null)
				{
					success = false;
					Finish();
					return true;
				}
				if (state == State.NewPath)
				{
					BeginGrid(path, low_level_only);
					return true;
				}
				if (state == State.FindUnreservedPoint)
				{
					if ((find_unreserved_stage & (FindUnreservedStage)24) != FindUnreservedStage.HasntStarted)
					{
						ApplyFindUnreserved(path);
						return true;
					}
					if (find_unreserved_stage == FindUnreservedStage.HasntStarted)
					{
						BeginFindUnreserved(path, path.src_obj, path.dst_pt);
					}
					if ((find_unreserved_stage & (FindUnreservedStage)24) != FindUnreservedStage.HasntStarted)
					{
						return true;
					}
					if ((find_unreserved_stage & (FindUnreservedStage)7) != FindUnreservedStage.HasntStarted)
					{
						FindUnreservedSteps(path, path.src_obj, path.dst_pt, 1000);
					}
					return true;
				}
				if (state == State.SetupHighCridPath)
				{
					SetupHighGridPath(path, low_level_only);
					return true;
				}
				if (state == State.ModifyHighGridStart || state == State.ModifyHighGridDest)
				{
					ModifyHighGridSteps(path);
					return true;
				}
				if (state == State.CalcHighGridPath)
				{
					CalcHighGridPath(path, low_level_only);
					if (state == State.Finished)
					{
						return true;
					}
					state = State.LowSteps;
					return true;
				}
				if (state == State.HighStep)
				{
					if (!HighGridStep(path))
					{
						Finish();
						return true;
					}
					state = State.LowSteps;
					if (PerformSteps(1000))
					{
						state = State.HighStep;
					}
					return true;
				}
				if (PerformSteps(1000))
				{
					state = State.HighStep;
				}
				return true;
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex2)
			{
				if (cur_path != null)
				{
					game.Error("Pathfinding error: " + ex2?.ToString() + " \n" + $"path: src_pt {cur_path.src_pt}, dst_pt {cur_path.dst_pt}, src_obj {cur_path.src_obj}, dst_obj {cur_path.dst_obj}");
					Finish(force_fail: true);
					return true;
				}
				game.Error("Pathfinding error: " + ex2.ToString());
			}
			return true;
		}
	}

	private void ThreadFunc()
	{
		Thread.CurrentThread.Name = "Path Finding";
		while (true)
		{
			if (debugging)
			{
				Thread.Yield();
			}
			else if (!Process())
			{
				resume.WaitOne();
			}
		}
	}

	private void LoadSettings()
	{
		settings = new Settings();
		DT.Field field = game?.dt.Find("PathFindings." + map_name);
		if (field != null)
		{
			settings.Load(field);
		}
	}

	public void LoadPFData(bool for_generation = false)
	{
		if (map_name == null)
		{
			Error("No map loaded");
		}
		else
		{
			if (settings.is_runtime || data != null)
			{
				return;
			}
			PathData pathData = data;
			if (pathData != null)
			{
				_ = pathData.pointers;
				if (true)
				{
					data.pointers.Dispose();
				}
			}
			data = new PathData();
			data.settings = settings;
			data.Load(map_name, for_generation);
			data.pointers = new PathData.DataPointers(ref data);
			if (settings.max_radius > 0f)
			{
				data.BuildClearance();
			}
			data.initted = true;
		}
	}

	public void LoadPFData(int width, int height, PathData.Cell[] grid)
	{
		if (map_name == null)
		{
			Error("No map loaded");
			return;
		}
		PathData pathData = data;
		if (pathData != null)
		{
			_ = pathData.pointers;
			if (true)
			{
				data.pointers.Dispose();
			}
		}
		data = new PathData();
		data.settings = settings;
		data.Load(width, height, grid);
		data.pointers = new PathData.DataPointers(ref data);
		if (settings.max_radius > 0f)
		{
			data.BuildClearance();
		}
		data.initted = true;
	}

	public void UpdateEmptySettlementsPathData(Settlement s, int range = 20)
	{
		data.WorldToGrid(s.position, out var x, out var y);
		int num = Math.Max(x - range / 2, 0);
		int num2 = Math.Min(x + range / 2, data.width);
		int num3 = Math.Max(y - range / 2, 0);
		int num4 = Math.Min(y + range / 2, data.height);
		for (int i = num; i < num2; i++)
		{
			for (int j = num3; j < num4; j++)
			{
				PathData.Node node = data.GetNode(i, j);
				if (node.town)
				{
					node.town = false;
					data.CalcWeight(ref node);
					data.ModifyNode(i, j, node);
				}
			}
		}
	}

	public void UpdateEmptySettlementsPathData()
	{
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			for (int j = 0; j < realm.settlements.Count; j++)
			{
				Settlement settlement = realm.settlements[j];
				if (!settlement.IsActiveSettlement())
				{
					UpdateEmptySettlementsPathData(settlement);
				}
			}
		}
	}

	public void FlushPending(bool mt_only = false)
	{
		if (!settings.multithreaded)
		{
			if (!mt_only)
			{
				while (Process())
				{
				}
			}
			return;
		}
		int num = 0;
		while (pending.Count != 0 || cur_path != null)
		{
			if (++num > 10000)
			{
				Game.Log("Infinite loop in PathFinding.FlushPending!", Game.LogType.Error);
				lock (Lock)
				{
					pending.Clear();
					break;
				}
			}
			Thread.Yield();
		}
	}

	protected override void OnStart()
	{
		if (settings.multithreaded)
		{
			thread = new Thread(ThreadFunc);
			thread.Start();
		}
		base.OnStart();
	}

	public override void OnUpdate()
	{
		if (settings.multithreaded)
		{
			lock (Lock)
			{
				for (int i = 0; i < completed.Count; i++)
				{
					Path path = completed[i];
					NotifyCompleted(path);
				}
				completed.Clear();
			}
		}
		if (pending.Count == 0)
		{
			StopUpdating();
			return;
		}
		UpdateNextFrame();
		if (!settings.multithreaded && !debugging)
		{
			Process();
		}
	}

	protected override void OnDestroy()
	{
		if (thread != null)
		{
			thread.Abort();
			thread = null;
		}
		PathData pathData = data;
		if (pathData != null)
		{
			_ = pathData.pointers;
			if (true)
			{
				data?.pointers.Dispose();
			}
		}
		PathData pathData2 = data;
		if (pathData2 != null)
		{
			_ = pathData2.paIDGrid;
			if (true && data.paIDGrid.IsCreated)
			{
				data.paIDGrid.Dispose();
			}
		}
		PathData pathData3 = data;
		if (pathData3 != null)
		{
			_ = pathData3.highPFAdditionalGrid;
			if (true && data.highPFAdditionalGrid.IsCreated)
			{
				data.highPFAdditionalGrid.Dispose();
			}
		}
		base.OnDestroy();
	}
}
