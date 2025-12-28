using AOT;
using Unity.Burst;

namespace Logic;

[BurstCompile]
public static class PathFindingBurst
{
	public unsafe delegate bool TraceDelegate(PathData.DataPointers* data, PPos* _from, PPos* _to, PPos* _result, bool ignore_impassable_terrain, bool check_in_area, bool water_is_passable, PathData.PassableArea.Type allowed_types, int battle_side, bool was_inside_wall);

	public static bool is_compiled = false;

	public unsafe static TraceDelegate Trace = TraceUnoptimized;

	public unsafe static void Compile()
	{
		if (!is_compiled)
		{
			Trace = BurstCompiler.CompileFunctionPointer<TraceDelegate>(TraceUnoptimized).Invoke;
			is_compiled = true;
		}
	}

	public unsafe static void Decompile()
	{
		if (is_compiled)
		{
			Trace = TraceUnoptimized;
			is_compiled = false;
		}
	}

	[BurstCompile]
	[MonoPInvokeCallback(typeof(TraceDelegate))]
	public unsafe static bool TraceUnoptimized(PathData.DataPointers* data, PPos* _from, PPos* _to, PPos* _result, bool ignore_impassable_terrain, bool check_in_area, bool water_is_passable, PathData.PassableArea.Type allowed_types, int battle_side, bool was_inside_wall)
	{
		PPos pPos = *_from;
		PPos to = *_to;
		PPos result = *_result;
		int idx_hit;
		bool result2 = data->Trace(pPos, to, out result, out idx_hit, ignore_impassable_terrain, check_in_area, water_is_passable, allowed_types, battle_side, was_inside_wall);
		*_result = result;
		return result2;
	}
}
