using System;

namespace Logic;

public class TriangleFormation : Formation
{
	public TriangleFormation(Def def, Squad owner)
		: base(def, owner)
	{
	}

	public TriangleFormation()
	{
	}

	public new static Formation Create(Def def, Squad owner)
	{
		return FormationPool.GetTriangle(def, owner);
	}

	public override int DefCount()
	{
		return def.rows * (def.rows + 1) / 2;
	}

	public override void SetCount(int count)
	{
		if (count != base.count)
		{
			base.SetCount(count);
			rows = (int)Math.Ceiling((Math.Sqrt(1 + 8 * count) - 1.0) * 0.5);
			cols = rows;
		}
	}

	public override int RowSize(int row)
	{
		return row + 1;
	}
}
