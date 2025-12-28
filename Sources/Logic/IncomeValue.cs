using System;

namespace Logic;

public struct IncomeValue
{
	public float base_value;

	public float flat_value;

	public float perc_value;

	public float non_perc_value;

	public float untaxed_value;

	public float taxed_value;

	public void Clear()
	{
		base_value = 0f;
		flat_value = 0f;
		perc_value = 0f;
		non_perc_value = 0f;
		untaxed_value = float.NaN;
		taxed_value = float.NaN;
	}

	public bool IsZero()
	{
		if (base_value == 0f && flat_value == 0f && perc_value == 0f)
		{
			return non_perc_value == 0f;
		}
		return false;
	}

	public void Add(IncomeValue val)
	{
		base_value += val.base_value;
		flat_value += val.flat_value;
		perc_value += val.perc_value;
		non_perc_value += val.non_perc_value;
	}

	public void Calc(float tax_mul, bool cap_to_0 = false, bool round_up = false)
	{
		untaxed_value = (base_value + flat_value) * (1f + perc_value * 0.01f) + non_perc_value;
		if (cap_to_0 && untaxed_value < 0f)
		{
			untaxed_value = 0f;
		}
		taxed_value = untaxed_value * tax_mul;
		if (round_up)
		{
			taxed_value = (float)Math.Ceiling(taxed_value);
		}
	}

	public override string ToString()
	{
		string text = DT.FloatToStr(base_value, 3);
		if (flat_value != 0f)
		{
			if (flat_value > 0f)
			{
				text += "+";
			}
			text += DT.FloatToStr(flat_value, 3);
		}
		if (perc_value != 0f)
		{
			if (perc_value > 0f)
			{
				text += "+";
			}
			text = text + DT.FloatToStr(perc_value, 3) + "%";
		}
		if (!float.IsNaN(untaxed_value) && untaxed_value != base_value)
		{
			if (text != "")
			{
				text += " = ";
			}
			text += DT.FloatToStr(untaxed_value, 3);
		}
		if (!float.IsNaN(taxed_value) && taxed_value != untaxed_value)
		{
			text = text + " (" + DT.FloatToStr(taxed_value, 3) + " after taxes)";
		}
		return text;
	}
}
