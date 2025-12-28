using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public abstract class Formation : IDisposable
{
	public class Def : Logic.Def
	{
		public Type type;

		public CreateFormation create_func;

		public int cols = 12;

		public int rows = 4;

		public float pivot = 0.5f;

		public int min_cols = 1;

		public int min_rows = 1;

		public float min_rows_to_cols = 0.3f;

		public float col_spacing_mul;

		public float row_spacing_mul;

		public float deform_x = 0.25f;

		public float deform_y1 = 0.25f;

		public float deform_y2 = 1f;

		public float defensive_stance_control_zone = 8f;

		public float aggressive_mode_ground_control_zone = 30f;

		public float attacking_control_zone = 30f;

		public override bool Load(Game game)
		{
			DT.Field f = dt_def.field;
			Load(f);
			return true;
		}

		public void Load(DT.Field f)
		{
			LoadType(f);
			cols = f.GetInt("cols", null, cols);
			rows = f.GetInt("rows", null, rows);
			pivot = f.GetFloat("pivot", null, pivot);
			min_cols = f.GetInt("min_cols", null, min_cols);
			min_rows = f.GetInt("min_rows", null, min_rows);
			min_rows_to_cols = f.GetFloat("min_rows_to_cols", null, min_rows_to_cols);
			col_spacing_mul = f.GetFloat("col_spacing_mul", null, col_spacing_mul);
			row_spacing_mul = f.GetFloat("row_spacing_mul", null, row_spacing_mul);
			deform_x = f.GetFloat("deform_x", null, deform_x);
			deform_y1 = f.GetFloat("deform_y1", null, deform_y1);
			deform_y2 = f.GetFloat("deform_y2", null, deform_y2);
			defensive_stance_control_zone = f.GetFloat("defensive_stance_control_zone", null, defensive_stance_control_zone);
			aggressive_mode_ground_control_zone = f.GetFloat("aggressive_mode_ground_control_zone", null, aggressive_mode_ground_control_zone);
			attacking_control_zone = f.GetFloat("attacking_control_zone", null, attacking_control_zone);
		}

		private void LoadType(DT.Field f)
		{
			string key = f.key;
			create_func = null;
			if (cs_type == null)
			{
				Game.Log("Invalid formation type: " + key, Game.LogType.Error);
			}
			else if (f.based_on != null)
			{
				create_func = cs_type.FindCreateMethod(typeof(Formation), typeof(Def), typeof(Squad))?.func as CreateFormation;
				if (create_func == null)
				{
					Game.Log(key + " has no method Create", Game.LogType.Error);
				}
			}
		}
	}

	public struct Line
	{
		public PPos pt;

		public Point dir;

		public int count;

		public float left;

		public float right;

		public float dist;

		public static bool operator ==(Line line, Line other)
		{
			if (line.pt == other.pt && line.dir == other.dir && line.count == other.count && line.left == other.left && line.right == other.right)
			{
				return line.dist == other.dist;
			}
			return false;
		}

		public static bool operator !=(Line line, Line other)
		{
			if (!(line.pt != other.pt) && !(line.dir != other.dir) && line.count == other.count && line.left == other.left && line.right == other.right)
			{
				return line.dist != other.dist;
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return (((((638726775 * -1521134295 + pt.GetHashCode()) * -1521134295 + dir.GetHashCode()) * -1521134295 + count.GetHashCode()) * -1521134295 + left.GetHashCode()) * -1521134295 + right.GetHashCode()) * -1521134295 + dist.GetHashCode();
		}
	}

	public delegate Formation CreateFormation(Def def, Squad owner);

	public Def def;

	public Squad owner;

	public PPos pos;

	public Point dir;

	public int count;

	public int rows;

	public int cols;

	public float troop_radius;

	public Point spacing;

	public float spacing_mul = 1f;

	public float ratio;

	public PathData.PassableArea.Type allowed_areas = PathData.PassableArea.Type.All;

	public int battle_side = -1;

	public bool is_inside_wall;

	public const int MAX_LINES_COUNT = 100;

	public NativeArrayList<Line> lines;

	public NativeArrayList<Line> predicted_lines;

	public NativeArrayList<Line> undefined_lines;

	public float last_line_size;

	public NativeArrayList<PPos> positions;

	public bool dirty = true;

	private NativeArrayList<int> old_lines_troops_count;

	private const float front_spring_factor = 1.05f;

	private const float back_spring_factor = 1.4f;

	public float dense_width => (float)cols * troop_radius;

	public float dense_height => (float)rows * troop_radius;

	public float dense_radius
	{
		get
		{
			float num = dense_width;
			float num2 = dense_height;
			return ((num >= num2) ? num : num2) * 0.5f;
		}
	}

	public float cur_width => (float)cols * spacing.x;

	public float cur_height => (float)rows * spacing.y;

	public float cur_radius
	{
		get
		{
			float num = cur_width;
			float num2 = cur_height;
			return ((num >= num2) ? num : num2) * 0.5f;
		}
	}

	public float min_spacing => troop_radius * owner.def.min_troop_spacing;

	public float default_spacing => troop_radius * owner.def.default_troop_spacing;

	public float max_spacing => troop_radius * owner.def.max_troop_spacing;

	public Formation(Def def, Squad owner)
	{
		Setup(def, owner);
	}

	public Formation()
	{
		lines = new NativeArrayList<Line>(100);
		predicted_lines = new NativeArrayList<Line>(100);
		undefined_lines = NativeArrayList<Line>.CreateUndefined();
		positions = new NativeArrayList<PPos>(100);
		old_lines_troops_count = new NativeArrayList<int>(100);
		SetupDefaultValues();
	}

	public void Dispose()
	{
		lines.Dispose();
		predicted_lines.Dispose();
		undefined_lines.Dispose();
		positions.Dispose();
		old_lines_troops_count.Dispose();
	}

	private void SetupDefaultValues()
	{
		def = null;
		owner = null;
		pos = default(PPos);
		dir = default(Point);
		count = 0;
		rows = 0;
		cols = 0;
		troop_radius = 0f;
		spacing = default(Point);
		spacing_mul = 1f;
		ratio = 0f;
		allowed_areas = PathData.PassableArea.Type.All;
		battle_side = -1;
		is_inside_wall = false;
		lines.Clear();
		predicted_lines.Clear();
		last_line_size = 0f;
		positions.Clear();
		dirty = true;
	}

	public void Setup(Def def, Squad owner)
	{
		SetupDefaultValues();
		this.def = def;
		this.owner = owner;
		allowed_areas = owner.movement.allowed_non_ladder_areas;
		battle_side = owner.battle_side;
		is_inside_wall = owner.is_inside_walls_or_on_walls;
	}

	public static Formation Create(Def def, Squad owner)
	{
		if (def == null || def.create_func == null)
		{
			return null;
		}
		return def.create_func(def, owner);
	}

	public bool CopyParamsFrom(Formation f)
	{
		if (f == null)
		{
			return false;
		}
		bool result = false;
		if (troop_radius != f.troop_radius)
		{
			troop_radius = f.troop_radius;
			result = true;
		}
		if (allowed_areas != f.allowed_areas)
		{
			allowed_areas = f.allowed_areas;
			result = true;
		}
		if (battle_side != f.battle_side)
		{
			battle_side = f.battle_side;
			result = true;
		}
		if (is_inside_wall != f.is_inside_wall)
		{
			is_inside_wall = f.is_inside_wall;
			result = true;
		}
		if (spacing_mul != f.spacing_mul)
		{
			spacing_mul = f.spacing_mul;
			dirty = true;
			result = true;
		}
		if (f.count == count)
		{
			if (f.rows != rows || f.cols != cols)
			{
				dirty = true;
				result = true;
				cols = f.cols;
				rows = f.rows;
			}
			ratio = f.ratio;
			return result;
		}
		dirty = true;
		result = true;
		ratio = f.ratio;
		RecalcRatio();
		return result;
	}

	public void Copy(Formation f)
	{
		pos = f.pos;
		dir = f.dir;
		count = f.count;
		spacing = f.spacing;
		CopyParamsFrom(f);
		positions.SetCount(f.positions.Count);
		for (int i = 0; i < f.positions.Count; i++)
		{
			positions[i] = f.positions[i];
		}
	}

	public abstract int DefCount();

	public virtual void SetCount(int count)
	{
		if (this.count != count)
		{
			dirty = true;
			this.count = count;
		}
	}

	public virtual void RecalcRatio()
	{
	}

	public abstract int RowSize(int row);

	public virtual int MinCols()
	{
		return cols;
	}

	public virtual int MaxCols()
	{
		return cols;
	}

	public virtual void SetCols(int cols)
	{
	}

	public virtual void SetRows(int rows)
	{
	}

	public float MinWidth()
	{
		return (float)MinCols() * spacing.x;
	}

	public float MaxWidth()
	{
		return (float)MaxCols() * spacing.x;
	}

	public void SetWidth(float width)
	{
		int num = (int)(width / spacing.x);
		SetCols(num);
	}

	public void SetHeight(float height)
	{
		int num = (int)(height / spacing.y);
		SetRows(num);
	}

	public unsafe float CalcClearance(PPos pt, Point dir, float dist)
	{
		if (owner == null || owner.game == null || owner.game.passability == null)
		{
			return dist;
		}
		if (owner.game.path_finding.data == null)
		{
			return dist;
		}
		if (dist == 0f)
		{
			return dist;
		}
		bool num;
		PPos pt2 = default(PPos);
		fixed (PathData.DataPointers* pointers = &owner.game.path_finding.data.pointers)
		{
			num = FormationBurst.CalcClearance(pointers, &pt, &dir, dist, &pt2, allowed_areas, battle_side, troop_radius, is_inside_wall);
		}
		if (!num)
		{
			return pt.Dist(pt2);
		}
		return dist;
	}

	protected virtual Line CalcLine(int idx, int max_count, PPos pt, Point dir, float spacing, float dist = 0f, bool no_limit = false)
	{
		int num = ((!no_limit) ? 1 : 2);
		int num2 = RowSize(idx) * num;
		if (num2 > MaxCols() || (no_limit && pt.paID != 0))
		{
			num2 = ((!no_limit || num2 <= cols) ? MaxCols() : cols);
		}
		if (num2 > max_count)
		{
			num2 = max_count;
		}
		float num3 = (float)(num2 - 1) * spacing * 0.5f;
		if ((num3 < dist || dist == 0f) && (!no_limit || num3 == 0f))
		{
			dist = num3;
		}
		Point point = dir.Right();
		float num4 = CalcClearance(pt, -point, dist);
		float num5 = CalcClearance(pt, point, dist);
		if (num4 < dist || num5 < dist || Mathf.Abs(num3 - dist) > 0.001f)
		{
			num4 = Math.Max(0f, num4);
			num5 = Math.Max(0f, num5);
			bool flag = true;
			float num6 = spacing;
			int num7 = (int)Math.Floor((num4 + num5) / num6) + 1;
			if (num7 < 2)
			{
				num7 = (int)Math.Floor((num4 + num5) / min_spacing);
				num6 = min_spacing;
				if (num7 < 2)
				{
					num7 = 2;
					flag = false;
				}
			}
			if (num7 < num2 && num7 > 0)
			{
				num2 = num7;
			}
			if (flag)
			{
				float num8 = num4 + num5;
				float num9 = (float)(num2 - 1) * num6;
				if (num9 > 0f && num8 > 0f)
				{
					num4 *= num9 / num8;
					num5 *= num9 / num8;
				}
			}
		}
		return new Line
		{
			pt = pt,
			dir = dir,
			count = num2,
			left = num4,
			right = num5,
			dist = dist
		};
	}

	private unsafe void CalcLineTransform(float t, out PPos pt, out Point dir, float line_y, Point spacing, bool no_space = false, Line last_line = default(Line))
	{
		CalcLineTransformData calcLineTransformData = CalcLineTransformData.FromFormation(this);
		PPos pPos = default(PPos);
		Point point = default(Point);
		FormationBurst.CalcLineTransform(&calcLineTransformData, t, &pPos, &point, &spacing, no_space, &last_line);
		pt = pPos;
		dir = point;
	}

	public void CalcLines(bool no_vertical_space = false)
	{
		if (owner == null || owner.game == null || owner.game.path_finding.data == null)
		{
			return;
		}
		PathData.DataPointers pointers = owner.game.path_finding.data.pointers;
		int num = 10;
		int num2 = 6;
		int num3 = 4;
		float num4 = lines.Count;
		old_lines_troops_count.Clear();
		for (int i = 0; i < lines.Count; i++)
		{
			old_lines_troops_count.Add(lines[i].count);
		}
		lines.Clear();
		predicted_lines.Clear();
		Point point = spacing;
		float num5 = cur_height;
		bool flag = false;
		if (pos.paID > 0)
		{
			flag = !pointers.GetPA(pos.paID - 1).IsGround();
		}
		if (flag && spacing.x > min_spacing)
		{
			point.x = min_spacing;
			point.y = min_spacing;
			num4 = rows;
			num5 = (float)rows * point.y;
		}
		int num6 = count;
		float num7 = num4 * 0.5f * def.pivot * point.y;
		float num8 = CalcClearance(pos, dir, num7);
		float num9 = CalcClearance(pos, -dir, num5 * 2f);
		float num10 = num8 + num9;
		float base_dis = 0f;
		int num11 = cols;
		int num12 = rows;
		if (num8 < num7 || (pos.paID != 0 && num9 < num7) || flag)
		{
			num9 = (float)(int)Math.Floor(num9 / point.y) * 0.5f * def.pivot * point.y;
			int num13 = (int)Math.Ceiling(num10 / point.y);
			int val = (int)Math.Ceiling((float)num6 / (float)num13);
			val = Math.Max(MinCols(), val);
			if ((val * num13 < num6 || num13 < 2) && !flag && spacing.x > min_spacing)
			{
				point.x = min_spacing;
				point.y = min_spacing;
			}
			base_dis = (float)val * point.x * 0.5f;
			num7 = num8 - troop_radius * 0.5f;
			if (flag)
			{
				cols = val;
				rows = num13;
			}
		}
		CalculateLines(ref predicted_lines, num6, num7, point, ref undefined_lines, no_vertical_space, -1f, calculate_on_path: true, base_dis);
		if (predicted_lines.Count + num3 >= num)
		{
			num = predicted_lines.Count + num;
		}
		if (predicted_lines.Count > 0)
		{
			int idx = 0;
			float num14 = 0f;
			num14 = predicted_lines[0].left + predicted_lines[0].right;
			for (int j = 0; j < predicted_lines.Count; j++)
			{
				if (num14 < predicted_lines[j].left + predicted_lines[j].right)
				{
					idx = j;
					num14 = predicted_lines[j].left + predicted_lines[j].right;
				}
			}
			predicted_lines.Clear();
			float num15 = num14;
			float num16 = num7 + owner.movement.speed;
			PPos pt;
			PPos pPos;
			for (int num17 = num2; num17 > 0; num17--)
			{
				owner.CalcPos(out pt, out pPos, (float)(num17 + 1) * point.x + num16, owner.def.dir_look_ahead);
				Line element = CalcLine(idx, cols, pt, pPos, point.x);
				predicted_lines.Insert(num2 - num17, element);
				if (element.left + element.right < num15)
				{
					num15 = element.left + element.right;
				}
			}
			for (int k = 0; k < num; k++)
			{
				owner.CalcPos(out pt, out pPos, (float)(-k) * point.x + num16, owner.def.dir_look_ahead);
				Line item = CalcLine(idx, cols, pt, pPos, point.x);
				predicted_lines.Add(item);
				if (item.left + item.right < num15)
				{
					num15 = item.left + item.right;
				}
			}
			SpringLines(ref predicted_lines, num2, num15);
			float num18 = num14;
			num14 = predicted_lines[0].left + predicted_lines[0].right;
			float num19 = predicted_lines[0].left + predicted_lines[0].right;
			for (int l = 0; l < predicted_lines.Count - 1; l++)
			{
				if (num14 < predicted_lines[l].left + predicted_lines[l].right)
				{
					num14 = predicted_lines[l].left + predicted_lines[l].right;
				}
				if (num19 > predicted_lines[l].left + predicted_lines[l].right)
				{
					num19 = predicted_lines[l].left + predicted_lines[l].right;
				}
			}
			for (int m = 0; m < num2; m++)
			{
				predicted_lines.RemoveAt(0);
			}
			float num20 = (predicted_lines[0].left + predicted_lines[0].right) * 0.5f;
			num20 = (float)Math.Round(num20, 5);
			bool num21 = num19 <= point.x * (float)(def.min_cols - 1);
			if (last_line_size == 0f || num18 == num14)
			{
				last_line_size = num20;
			}
			if (Mathf.Abs(last_line_size - num20) > point.x * 0.25f)
			{
				num20 = Mathf.Lerp(last_line_size, num20, 0.1f);
				num20 = Mathf.Max(num20, point.x * 0.25f);
			}
			else
			{
				num20 = last_line_size;
			}
			lines.Clear();
			if (num21)
			{
				CalculateLines(ref lines, num6, num7, point, ref predicted_lines, no_vertical_space, num9, calculate_on_path: true, num20);
			}
			else
			{
				CalculateLines(ref lines, num6, num7, point, ref undefined_lines, no_vertical_space, num9, calculate_on_path: true, num20);
			}
			last_line_size = num20;
		}
		if (flag)
		{
			cols = num11;
			rows = num12;
		}
		if (lines.Count != old_lines_troops_count.Count)
		{
			dirty = true;
			return;
		}
		for (int n = 0; n < lines.Count; n++)
		{
			if (lines[n].count != old_lines_troops_count[n])
			{
				dirty = true;
				break;
			}
		}
	}

	private void SpringLines(ref NativeArrayList<Line> lines, int forward_line_prediction_distance, float smallest_base_predition_line_size)
	{
		bool flag = false;
		float num = 1.05f;
		do
		{
			flag = false;
			for (int i = 1; i < lines.Count - 1; i++)
			{
				Line line = lines[i];
				num = ((i < forward_line_prediction_distance || !(smallest_base_predition_line_size <= spacing.x * (float)(def.min_cols - 1))) ? 1.05f : 1.4f);
				if (line.dist <= 0f || line.count == 0)
				{
					continue;
				}
				Line value = lines[i - 1];
				Line value2 = lines[i + 1];
				float num2 = line.left * num / line.dist;
				float num3 = line.right * num / line.dist;
				if (line.left > 0f)
				{
					if (i - 1 >= 0 && value.left > 0f && value.left / value.dist > num2 + 0.0001f)
					{
						flag = true;
						value.left = value.dist * num2;
					}
					if (i + 1 < lines.Count && value2.left > 0f && value2.left / value2.dist > num2 + 0.0001f)
					{
						flag = true;
						value2.left = value2.dist * num2;
					}
				}
				if (line.right > 0f)
				{
					if (i - 1 >= 0 && value.right > 0f && value.right / value.dist > num3 + 0.0001f)
					{
						flag = true;
						value.right = value.dist * num3;
					}
					if (i + 1 < lines.Count && value2.right > 0f && value2.right / value2.dist > num3 + 0.0001f)
					{
						flag = true;
						value2.right = value2.dist * num3;
					}
				}
				lines[i - 1] = value;
				lines[i + 1] = value2;
			}
		}
		while (flag);
	}

	private void CalculateLines(ref NativeArrayList<Line> calcLines, int cnt, float t, Point spacing, ref NativeArrayList<Line> predicted_lines, bool no_vertical_space = false, float backwards_clearance = -1f, bool calculate_on_path = false, float base_dis = 0f)
	{
		int num = 0;
		PathData.DataPointers pointers = owner.game.path_finding.data.pointers;
		bool flag = !predicted_lines.IsNotDefined && predicted_lines.Count > 0;
		float dist = base_dis;
		bool flag2 = false;
		if (pos.paID > 0)
		{
			flag2 = !pointers.GetPA(pos.paID - 1).IsGround();
		}
		while (cnt > 0)
		{
			bool no_space = no_vertical_space && !calculate_on_path && backwards_clearance >= 0f && t < 0f - backwards_clearance && calcLines.Count > 0;
			CalcLineTransform(t, out var pt, out var point, 0f, spacing, no_space, (calcLines.Count > 0) ? calcLines[calcLines.Count - 1] : default(Line));
			if (pointers.BurstedTrace(pos, pt, out var result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, allowed_areas, battle_side, is_inside_wall))
			{
				pt.paID = result.paID;
			}
			PPos pPos = point;
			PPos pt2 = pt;
			if (calculate_on_path && owner.movement != null && owner.movement.path != null)
			{
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				if (num == 0)
				{
					flag5 = owner.movement.path.path_len < owner.def.dir_look_ahead * 2f + 2f * spacing.y;
					flag4 = owner.movement.path.t < owner.def.dir_look_ahead * 0.5f;
					calculate_on_path = !(flag3 || flag4 || flag5);
				}
				if ((num == 0 && owner.movement.path.t + t + owner.movement.speed + owner.def.dir_look_ahead > owner.movement.path.path_len) || owner.movement.path.t + t + owner.movement.speed < 0f)
				{
					calculate_on_path = false;
				}
				if (calculate_on_path)
				{
					owner.CalcPos(out pt2, out pPos, t + owner.movement.speed, owner.def.dir_look_ahead);
				}
			}
			if (flag && !predicted_lines.IsNotDefined && num < predicted_lines.Count)
			{
				float left = predicted_lines[num].left;
				float right = predicted_lines[num].right;
				dist = (left + right) * 0.5f;
			}
			if (!calculate_on_path && no_vertical_space)
			{
				dist = ((!flag2) ? (MaxWidth() * 0.5f) : ((float)cols * spacing.x * 0.5f));
			}
			Line item = CalcLine(num, cnt, pt2, pPos, spacing.x, dist, no_vertical_space);
			calcLines.Add(item);
			cnt -= item.count;
			t -= spacing.y;
			num++;
		}
	}

	public void CalcLinesOnlySides(bool no_vertical_space = false)
	{
		if (owner == null || owner.game == null || owner.game.path_finding.data == null)
		{
			return;
		}
		PathData.DataPointers pointers = owner.game.path_finding.data.pointers;
		List<int> list = new List<int>();
		for (int i = 0; i < lines.Count; i++)
		{
			list.Add(lines[i].count);
		}
		lines.Clear();
		int num = count;
		Point point = spacing;
		bool flag = false;
		if (pos.paID > 0)
		{
			flag = !pointers.GetPA(pos.paID - 1).IsGround();
		}
		if (flag && spacing.x > min_spacing)
		{
			point.x = min_spacing;
			point.y = min_spacing;
		}
		float num2 = (float)(rows - 1) * def.pivot * point.y;
		float num3 = CalcClearance(pos, dir, cur_height * 2f);
		float num4 = CalcClearance(pos, -dir, cur_height * 2f);
		float num5 = num3 + num4;
		float base_dis = 0f;
		int num6 = cols;
		int num7 = rows;
		if (num3 < num2 || (pos.paID != 0 && num4 < num2) || flag)
		{
			if (num3 > num2 && num5 >= cur_height)
			{
				num3 = num2;
			}
			num4 = (float)(int)Math.Floor(num4 / point.y) * 0.5f * def.pivot * point.y;
			int val = (int)Math.Ceiling(num5 / point.y);
			val = Math.Max(1, val);
			int val2 = (int)Math.Ceiling((float)num / (float)val);
			val2 = Math.Max(MinCols(), val2);
			if ((val2 * val < num || val < 2) && !flag && spacing.x > min_spacing)
			{
				point.x = min_spacing;
				point.y = min_spacing;
			}
			base_dis = (float)val2 * point.x * 0.5f;
			num2 = num3 - troop_radius * 0.5f;
			if (flag)
			{
				cols = val2;
				rows = val;
			}
		}
		CalculateLines(ref lines, num, num2, point, ref undefined_lines, no_vertical_space, num4, calculate_on_path: false, base_dis);
		if (flag)
		{
			cols = num6;
			rows = num7;
		}
		if (lines.Count != list.Count)
		{
			dirty = true;
			return;
		}
		for (int j = 0; j < lines.Count; j++)
		{
			if (lines[j].count != list[j])
			{
				dirty = true;
				break;
			}
		}
	}

	public void CalcPositions()
	{
		if (positions.Count != count)
		{
			positions.SetCount(count);
		}
		int num = 0;
		PathData.DataPointers pointers = owner.game.path_finding.data.pointers;
		for (int i = 0; i < lines.Count; i++)
		{
			Line line = lines[i];
			List<PPos> list = new List<PPos>();
			Point point = line.dir.Right();
			PPos to = line.pt - point * line.left;
			PPos to2 = line.pt + point * line.right;
			pointers.TraceManaged(line.pt, to, out var result, list, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, allowed_areas, battle_side, null, is_inside_wall);
			list.Reverse();
			to = result;
			list.Insert(0, to);
			pointers.TraceManaged(line.pt, to2, out result, list, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, allowed_areas, battle_side, null, is_inside_wall);
			to2 = result;
			PPos pPos = to;
			int j = 0;
			if (line.count > 1)
			{
				point *= (to2 - to).Length() / (float)(line.count - 1);
			}
			for (int k = 0; k < line.count; k++)
			{
				for (; j < list.Count - 2 && (pPos - to).SqrLength() > (list[j + 1] - to).SqrLength(); j++)
				{
				}
				pPos.paID = list[j].paID;
				positions[num++] = pPos;
				pPos += point;
			}
		}
	}

	public unsafe void Calc(PPos pos, Point dir, bool calc_positions = false, bool moving = false)
	{
		this.pos = pos;
		this.dir = dir;
		float num = troop_radius * 2f;
		if (count > 1)
		{
			spacing = new Point(num * (1f + def.col_spacing_mul * spacing_mul), num * (1f + def.row_spacing_mul * spacing_mul));
		}
		else
		{
			spacing = new Point(num * (0.5f + def.col_spacing_mul * spacing_mul), num * (0.5f + def.row_spacing_mul * spacing_mul));
		}
		if (owner?.game?.path_finding?.data == null || (owner.game.path_finding.data.pointers.Initted != null && !(*owner.game.path_finding.data.pointers.Initted)))
		{
			return;
		}
		bool no_vertical_space = false;
		float num2 = CalcClearance(pos, -dir, cur_height);
		float num3 = CalcClearance(pos, dir, cur_height);
		bool num4 = !moving;
		if (num2 < cur_height / 2f && num3 < cur_height / 2f)
		{
			this.pos += dir * (num3 - num2);
		}
		else if (pos.paID == 0)
		{
			if (num2 < cur_height / 2f)
			{
				this.pos += dir * (cur_height / 2f - (num2 - 0.1f));
			}
			if (num3 < cur_height / 2f)
			{
				this.pos -= dir * (cur_height / 2f - (num3 - 0.1f));
			}
		}
		num2 = CalcClearance(this.pos, -dir, cur_height);
		num3 = CalcClearance(this.pos, dir, cur_height);
		if (num3 < cur_height / 2f || num2 < cur_height / 2f)
		{
			no_vertical_space = true;
		}
		if (num4)
		{
			CalcLinesOnlySides(no_vertical_space);
		}
		else
		{
			CalcLines(no_vertical_space);
		}
		if (calc_positions)
		{
			CalcPositions();
		}
	}

	public void GetClearanceRange(PPos position, PPos dir, float dist, out float frontDistance, out float backDistance)
	{
		frontDistance = CalcClearance(position, dir, dist);
		backDistance = CalcClearance(position, -dir, dist);
	}
}
