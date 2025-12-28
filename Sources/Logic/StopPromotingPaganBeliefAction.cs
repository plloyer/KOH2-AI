namespace Logic;

public class StopPromotingPaganBeliefAction : Action, IListener
{
	public StopPromotingPaganBeliefAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new StopPromotingPaganBeliefAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		if (!own_kingdom.is_pagan)
		{
			return "not_pagan";
		}
		if (base.own_character.paganBelief == null)
		{
			return "no_belief";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		base.own_character.StopPromotingPaganBelief(apply_penalties: false, notify: false);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "belief")
		{
			return base.own_character?.paganBelief?.GetNameKey();
		}
		return base.GetVar(key, vars, as_value);
	}
}
