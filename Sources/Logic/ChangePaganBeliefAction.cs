namespace Logic;

public class ChangePaganBeliefAction : PromotePaganBeliefAction
{
	public ChangePaganBeliefAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ChangePaganBeliefAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.paganBelief == null)
		{
			return "no_belief";
		}
		return base.Validate(quick_out);
	}

	public override int NumPaganBeliefs()
	{
		return base.NumPaganBeliefs() - 1;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "belief")
		{
			return base.own_character.paganBelief.GetNameKey();
		}
		return base.GetVar(key, vars, as_value);
	}
}
