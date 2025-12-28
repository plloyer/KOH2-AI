using System;

namespace Logic;

public class FadingRebellionRiskModifier : FadingModifier
{
	public new class Def : FadingModifier.Def
	{
		public float stability_unlinearity_mod = 3f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			stability_unlinearity_mod = field.GetFloat("stability_unlinearity_mod", null, stability_unlinearity_mod);
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
			return new FadingRebellionRiskModifier(game, null);
		}

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is FadingRebellionRiskModifier fadingRebellionRiskModifier))
			{
				return false;
			}
			result = fadingRebellionRiskModifier.result;
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
			(mod as FadingRebellionRiskModifier).result = result;
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

	public FadingRebellionRiskModifier(Game game, Def def, Object source = null, Object target = null)
		: base(game, def, source, target)
	{
		state = State.Active;
		result = 0f;
	}

	public override float CalcValue(Stats stats, Stat stat)
	{
		if (!(base.def is Def def))
		{
			return 0f;
		}
		float num = tgt_value;
		float num2 = UpdateState();
		return state switch
		{
			State.FadeIn => num * num2, 
			State.Active => num, 
			State.FadeOut => num * (1f - (float)Math.Pow(num2, def.stability_unlinearity_mod)), 
			_ => result, 
		};
	}

	public void Stop(Stats stats)
	{
		result = CalcValue(stats, stat);
		state_time = game.time;
	}

	public static FadingRebellionRiskModifier Add(Object obj, Def def, IVars vars = null)
	{
		if (def == null)
		{
			return null;
		}
		Stats stats = obj?.GetStats();
		if (stats == null)
		{
			return null;
		}
		Stat stat = stats.Find(def.stat_name);
		if (stat == null)
		{
			return null;
		}
		FadingRebellionRiskModifier mod = new FadingRebellionRiskModifier(obj.game, def);
		stat.AddModifier(mod);
		return mod;
	}
}
