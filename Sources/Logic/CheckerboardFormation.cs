namespace Logic;

public class CheckerboardFormation : RectFormation
{
	public CheckerboardFormation()
	{
	}

	public CheckerboardFormation(Def def, Squad owner)
		: base(def, owner)
	{
	}

	public new static Formation Create(Def def, Squad owner)
	{
		return FormationPool.GetCheckerboard(def, owner);
	}

	protected override Line CalcLine(int idx, int max_count, PPos pt, Point dir, float spacing, float dis = 0f, bool no_limit = false)
	{
		float num = 0.5f;
		if (idx % 2 == 1)
		{
			num = -0.5f;
		}
		PathData.DataPointers pointers = owner.game.path_finding.data.pointers;
		if (pointers.TraceDir(pt, dir.Right() * num, 1f, out var _) && pointers.IsPassable(pt + dir.Right() * num))
		{
			pt += dir.Right() * num;
		}
		return base.CalcLine(idx, max_count, pt, dir, spacing, dis, no_limit);
	}
}
