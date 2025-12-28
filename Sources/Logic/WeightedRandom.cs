using System;
using System.Collections.Generic;

namespace Logic;

public class WeightedRandom<T>
{
	public struct Option
	{
		public T val;

		public float weight;

		public override string ToString()
		{
			return $"[{weight}] {val}";
		}
	}

	public List<Option> options;

	public float weights_sum;

	public static Random rng = new Random();

	private static WeightedRandom<T> temp_instance = null;

	public WeightedRandom(int prealloc, int seed = -1)
	{
		options = new List<Option>(prealloc);
		if (seed != -1)
		{
			rng = new Random(seed);
		}
	}

	public override string ToString()
	{
		return $"[{weights_sum}] Options: {options.Count}";
	}

	public string Dump()
	{
		string text = ToString();
		for (int i = 0; i < options.Count; i++)
		{
			Option option = options[i];
			text += $"\n    {option}";
		}
		return text;
	}

	public void Clear()
	{
		options.Clear();
		weights_sum = 0f;
	}

	public bool AddOption(T val, float weight)
	{
		if (weight <= 0f)
		{
			return false;
		}
		options.Add(new Option
		{
			val = val,
			weight = weight
		});
		weights_sum += weight;
		return true;
	}

	public void DelOptionAtIndex(int idx)
	{
		if (idx >= 0 && idx < options.Count)
		{
			weights_sum -= options[idx].weight;
			options.RemoveAt(idx);
		}
	}

	public int ChooseIndex()
	{
		if (options.Count == 0)
		{
			return -1;
		}
		if (options.Count == 1)
		{
			return 0;
		}
		float num = (float)rng.NextDouble() * weights_sum;
		for (int i = 0; i < options.Count; i++)
		{
			Option option = options[i];
			if (num <= option.weight)
			{
				return i;
			}
			num -= option.weight;
		}
		return options.Count - 1;
	}

	public T Choose(T def_val = default(T), bool del_option = false)
	{
		int num = ChooseIndex();
		if (num < 0)
		{
			return def_val;
		}
		Option option = options[num];
		if (del_option)
		{
			options.RemoveAt(num);
			weights_sum -= option.weight;
		}
		return option.val;
	}

	public static WeightedRandom<T> GetTemp(int prealloc = 32)
	{
		if (temp_instance == null)
		{
			temp_instance = new WeightedRandom<T>(prealloc);
		}
		else
		{
			temp_instance.Clear();
		}
		return temp_instance;
	}

	public static T Choose(List<T> values, Func<T, float> calc_weight, T def_val = default(T))
	{
		if (values == null || values.Count == 0)
		{
			return def_val;
		}
		if (values.Count == 1)
		{
			return values[0];
		}
		if (calc_weight == null)
		{
			int index = rng.Next(values.Count);
			return values[index];
		}
		WeightedRandom<T> temp = GetTemp(values.Count);
		for (int i = 0; i < values.Count; i++)
		{
			T val = values[i];
			float weight = calc_weight(val);
			temp.AddOption(val, weight);
		}
		T result = temp.Choose();
		temp.Clear();
		return result;
	}
}
