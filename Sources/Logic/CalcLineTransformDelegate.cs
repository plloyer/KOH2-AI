using AOT;
using Unity.Burst;

namespace Logic;

[BurstCompile]
public class FormationBurst
{
	public unsafe delegate void CalcLineTransformDelegate(CalcLineTransformData* _calcData, float t, PPos* _pt, Point* _dir, Point* _spacing, bool no_space, Formation.Line* _last_line);

	public unsafe delegate bool CalcClearanceDelegate(PathData.DataPointers* _data, PPos* _pt, Point* _dir, float _dist, PPos* _pp, PathData.PassableArea.Type _allowed_areas, int _battle_side, float _troop_radius, bool was_inside_wall);

	private static bool is_compiled = false;

	public unsafe static CalcLineTransformDelegate CalcLineTransform = CalcLineTransformUnoptimized;

	public unsafe static CalcClearanceDelegate CalcClearance = CalcClearanceUnoptimized_ForBurst;

	public unsafe static void Compile()
	{
		if (!is_compiled)
		{
			CalcClearance = BurstCompiler.CompileFunctionPointer<CalcClearanceDelegate>(CalcClearanceUnoptimized_ForBurst).Invoke;
			CalcLineTransform = BurstCompiler.CompileFunctionPointer<CalcLineTransformDelegate>(CalcLineTransformUnoptimized).Invoke;
			is_compiled = true;
		}
	}

	public unsafe static void Decompile()
	{
		if (is_compiled)
		{
			CalcClearance = CalcClearanceUnoptimized_ForBurst;
			CalcLineTransform = CalcLineTransformUnoptimized;
			is_compiled = false;
		}
	}

	[BurstCompile]
	[MonoPInvokeCallback(typeof(CalcLineTransformDelegate))]
	public unsafe static void CalcLineTransformUnoptimized(CalcLineTransformData* _calcData, float t, PPos* _pt, Point* _dir, Point* _spacing, bool no_space, Formation.Line* _last_line)
	{
		CalcLineTransformData calcData = *_calcData;
		PPos pPos = *_pt;
		Point point = *_dir;
		Point spacing = *_spacing;
		Formation.Line line = *_last_line;
		PathData.DataPointers data = calcData.data;
		point = calcData.dir;
		if (line == default(Formation.Line))
		{
			pPos = calcData.pos + t * point;
		}
		else
		{
			pPos = line.pt + spacing.y * -line.dir;
			point = line.dir;
		}
		if (!no_space && (!data.Trace(calcData.pos, pPos, out var _, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, calcData.allowed_areas, calcData.battle_side, calcData.is_inside_wall) || !data.IsPassable(pPos)) && line != default(Formation.Line))
		{
			no_space = true;
		}
		if (no_space)
		{
			Point vr = line.dir.Right();
			float num = 0.2f;
			if (line.count > 1)
			{
				num = (line.left + line.right) / (float)(line.count - 1);
			}
			float right = line.right;
			if (!FindNextGoodPointForLineFromTroops(ref calcData, ref pPos, line.dir, line.pt, vr, spacing, num * 0.5f))
			{
				right = line.left;
				vr *= -1f;
				if (!FindNextGoodPointForLineFromTroops(ref calcData, ref pPos, line.dir, line.pt, vr, spacing, num * 0.5f))
				{
					point = line.dir.Right();
					if (line.right > line.left)
					{
						point = -line.dir.Right();
						right = line.right;
					}
					pPos = line.pt + -point * right + -point * 2f * spacing.x;
				}
				else
				{
					point = line.dir;
				}
			}
			else
			{
				point = line.dir;
			}
		}
		*_pt = pPos;
		*_dir = point;
	}

	public static bool FindNextGoodPointForLineFromTroops(ref CalcLineTransformData calcData, ref PPos pt, Point dir, PPos line_pt, Point vr, Point spacing, float line_spacing)
	{
		if (line_spacing <= 0.0001f)
		{
			return false;
		}
		PathData.DataPointers data = calcData.data;
		float num = 0f;
		int num2 = 0;
		PPos pPos = line_pt;
		float num3 = 0f;
		float num4 = CalcClearanceUnoptimized(calcData, pPos, vr, calcData.MaxWidth);
		PPos result = pPos;
		do
		{
			if (CalcClearanceUnoptimized(calcData, pPos, -dir, spacing.y) >= spacing.y - 0.02f)
			{
				PPos pPos2 = pPos + -dir * (spacing.y - 0.1f);
				if (data.IsPassable(pPos2))
				{
					pt = pPos2;
					return true;
				}
			}
			PPos pPos3 = pPos;
			pPos = line_pt + num2 * vr * line_spacing + vr * (num3 - 0.02f);
			pPos.paID = result.paID;
			PPos pPos4 = pPos3;
			pPos4.paID = result.paID;
			if (!data.Trace(pPos4, pPos, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, calcData.allowed_areas, calcData.battle_side, calcData.is_inside_wall))
			{
				pPos.paID = result.paID;
			}
			num = (float)num2 * line_spacing + num3;
			float num5 = num4 - (float)num2 * line_spacing;
			if (num5 > 0f && num5 < line_spacing && num3 == 0f && num4 > 0f)
			{
				num3 = num5;
			}
			else
			{
				num2++;
			}
		}
		while (num <= num4);
		return false;
	}

	[BurstCompile]
	[MonoPInvokeCallback(typeof(CalcClearanceDelegate))]
	private unsafe static bool CalcClearanceUnoptimized_ForBurst(PathData.DataPointers* _data, PPos* _pt, Point* _dir, float _dist, PPos* _pp, PathData.PassableArea.Type _allowed_areas, int _battle_side, float _troop_radius, bool was_inside_wall)
	{
		for (int i = 0; i < 3; i++)
		{
			if (!_data->TraceDir(*_pt, _dir->GetNormalized(), _dist, out *_pp, _allowed_areas, _battle_side, was_inside_wall))
			{
				break;
			}
			if (_data->IsPassable(*_pp))
			{
				return true;
			}
			_dist -= _troop_radius;
		}
		return false;
	}

	private unsafe static bool CalcClearanceUnoptimized(PathData.DataPointers* _data, PPos* _pt, Point* _dir, float _dist, PPos* _pp, PathData.PassableArea.Type _allowed_areas, int _battle_side, float _troop_radius)
	{
		for (int i = 0; i < 3; i++)
		{
			if (!_data->TraceDir(*_pt, _dir->GetNormalized(), _dist, out *_pp, _allowed_areas, _battle_side))
			{
				break;
			}
			if (_data->IsPassable(*_pp))
			{
				return true;
			}
			_dist -= _troop_radius;
		}
		return false;
	}

	public unsafe static float CalcClearanceUnoptimized(CalcLineTransformData calcData, PPos pt, Point dir, float dist)
	{
		if (!calcData.owner_game_passability_exists || dist == 0f)
		{
			return dist;
		}
		PPos pt2 = default(PPos);
		if (!CalcClearanceUnoptimized(&calcData.data, &pt, &dir, dist, &pt2, calcData.allowed_areas, calcData.battle_side, calcData.troop_radius))
		{
			return pt.Dist(pt2);
		}
		return dist;
	}
}
