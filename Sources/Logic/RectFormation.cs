using System;

namespace Logic;

public class RectFormation : Formation
{
	public RectFormation(Def def, Squad owner)
		: base(def, owner)
	{
	}

	public RectFormation()
	{
	}

	public new static Formation Create(Def def, Squad owner)
	{
		return FormationPool.GetRect(def, owner);
	}

	private void CalcColsRows(int count, float ratio, out int cols, out int rows)
	{
		double a = Math.Sqrt((float)count * ratio);
		cols = (int)Math.Ceiling(a);
		int num = MinCols();
		if (cols < num)
		{
			cols = num;
		}
		rows = (int)Math.Ceiling((float)cols / ratio);
		if ((float)rows / (float)cols < def.min_rows_to_cols)
		{
			cols = (int)Math.Ceiling((float)count * def.min_rows_to_cols);
			rows = (int)Math.Ceiling((float)count / (float)cols);
		}
	}

	public override int DefCount()
	{
		return def.rows * def.cols;
	}

	public override void SetCount(int count)
	{
		if (count != base.count)
		{
			base.SetCount(count);
			RecalcRatio();
		}
	}

	public override void RecalcRatio()
	{
		float num = ((ratio != 0f) ? ratio : ((float)def.cols / (float)def.rows));
		CalcColsRows(count, num, out cols, out rows);
	}

	public override int RowSize(int row)
	{
		return cols;
	}

	public override int MinCols()
	{
		return Math.Min(count, def.min_cols);
	}

	public override int MaxCols()
	{
		float num = (float)(int)Math.Ceiling((float)DefCount() / (float)def.min_rows) / (float)def.min_rows;
		CalcColsRows(count, num, out var val, out var _);
		return Math.Min(count, val);
	}

	public override void SetCols(int cols)
	{
		int num = MinCols();
		int num2 = MaxCols();
		if (cols < num)
		{
			cols = num;
		}
		else if (cols > num2)
		{
			cols = num2;
		}
		if (base.cols != cols)
		{
			dirty = true;
			base.cols = cols;
			rows = (int)Math.Ceiling((float)count / (float)cols);
			ratio = (float)cols / (float)rows;
		}
	}

	public override void SetRows(int rows)
	{
		base.rows = Math.Max(1, rows);
		cols = (int)Math.Ceiling((float)count / (float)rows);
		dirty = true;
		ratio = (float)cols / (float)rows;
	}
}
