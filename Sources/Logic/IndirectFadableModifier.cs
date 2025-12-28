namespace Logic;

public class IndirectFadableModifier : Stat.Modifier, IVars
{
	public FadingModifier mod;

	public float tgt_value;

	public IndirectFadableModifier(FadingModifier mod, float tgt_value)
	{
		this.mod = mod;
		this.tgt_value = tgt_value;
	}

	public override DT.Field GetField()
	{
		return mod.def.field;
	}

	public override string ToString()
	{
		return "(indirect)" + mod.def.field.key + ": " + Stat.Val2Str(value);
	}

	public override float CalcValue(Stats stats, Stat stat)
	{
		float num = tgt_value;
		float num2 = mod.UpdateState();
		return mod.state switch
		{
			FadingModifier.State.FadeIn => num * num2, 
			FadingModifier.State.Active => num, 
			FadingModifier.State.FadeOut => num * (1f - num2), 
			_ => float.NaN, 
		};
	}

	public override void OnActivate(Stats stats, Stat stat, bool from_state = false)
	{
		value = CalcValue(stats, stat);
	}

	public override void OnDeactivate(Stats stats, Stat stat)
	{
	}

	public Value GetVar(string key, IVars vars, bool as_value)
	{
		return Vars.GetExact(mod, key, vars, as_value);
	}
}
