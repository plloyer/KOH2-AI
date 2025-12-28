using System;

namespace Logic;

public class WarExhaustionModifier : FadingModifier
{
	public new class Def : FadingModifier.Def
	{
		public float min;

		public float max;

		public float time_period_increase;

		public float increase_base;

		public float increase_per_war;

		public float time_period_decrease;

		public float decrease_base;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			min = field.GetFloat("min", null, min);
			max = field.GetFloat("max", null, max);
			time_period_increase = field.GetFloat("time_period_increase", null, time_period_increase);
			increase_base = field.GetFloat("increase_base", null, increase_base);
			increase_per_war = field.GetFloat("increase_per_war", null, increase_per_war);
			time_period_decrease = field.GetFloat("time_period_decrease", null, increase_per_war);
			decrease_base = field.GetFloat("decrease_base", null, decrease_base);
			return base.Load(game);
		}

		public new static Def NewDef()
		{
			return new Def();
		}
	}

	private new class FullData : FadingModifier.FullData
	{
		public float result;

		public override object GetObject(Game game)
		{
			return new WarExhaustionModifier(game, null);
		}

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is WarExhaustionModifier warExhaustionModifier))
			{
				return false;
			}
			result = warExhaustionModifier.result;
			return base.InitFrom(obj);
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteFloat(result, "last_value");
			base.Save(ser);
		}

		public override void Load(Serialization.IReader ser)
		{
			result = ser.ReadFloat("last_value");
			base.Load(ser);
		}

		public override FadingModifier.Def GetDef(Game game, string defName)
		{
			return game.defs.Get<Def>(defName);
		}

		public override bool ApplyMod(FadingModifier mod, Stats stats, bool is_existing)
		{
			(mod as WarExhaustionModifier).result = result;
			return base.ApplyMod(mod, stats, is_existing);
		}
	}

	public float result;

	public override Data GetFullData()
	{
		FullData fullData = FullData.Create();
		if (fullData.InitFrom(this))
		{
			return fullData;
		}
		return null;
	}

	public WarExhaustionModifier(Game game, Def def, Object source = null, Object target = null)
		: base(game, def, source, target)
	{
		state = State.Active;
		if (def == null)
		{
			result = 0f;
		}
		else
		{
			result = def.min;
		}
	}

	public override float CalcValue(Stats stats, Stat stat)
	{
		if (!(base.def is Def { time_period_decrease: not 0f, time_period_increase: not 0f } def))
		{
			return 0f;
		}
		if (Game.isLoadingSaveGame)
		{
			if (result > def.max)
			{
				result = def.max;
			}
			if (result < def.min)
			{
				result = def.min;
			}
			return result;
		}
		float num = game.time - state_time;
		Kingdom kingdom = source as Kingdom;
		if (kingdom.wars.Count > 0)
		{
			float num2 = (float)Math.Floor(num / def.time_period_increase);
			if ((double)num2 <= 1E-05)
			{
				return result;
			}
			result += num2 * (def.increase_base + def.increase_per_war * (float)(kingdom.wars.Count - 1));
			state_time = game.time;
			if (result > def.max)
			{
				result = def.max;
			}
		}
		else
		{
			float num3 = (float)Math.Floor(num / def.time_period_decrease);
			if ((double)num3 <= 1E-05)
			{
				return result;
			}
			result -= num3 * def.decrease_base;
			state_time = game.time;
			if (result < def.min)
			{
				return float.NaN;
			}
		}
		return result;
	}

	public void Stop(Stats stats)
	{
		result = CalcValue(stats, stat);
		state_time = game.time;
	}
}
