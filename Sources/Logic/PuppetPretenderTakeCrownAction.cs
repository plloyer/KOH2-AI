namespace Logic;

public class PuppetPretenderTakeCrownAction : Action
{
	public RoyalFamily.Pretender pretender;

	public PuppetPretenderTakeCrownAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetPretenderTakeCrownAction(owner as Character, def);
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		if (outcome.key == "become_vassal")
		{
			pretender.loyal_to.AddVassalState(own_kingdom);
			return true;
		}
		return base.ApplyOutcome(outcome);
	}

	public override string Validate(bool quick_out = false)
	{
		return "ok";
	}

	public override void Run()
	{
		pretender = null;
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "was_puppet"))
		{
			if (key == "loyal_to_kingdom")
			{
				return pretender.loyal_to;
			}
			return base.GetVar(key, vars, as_value);
		}
		return pretender.was_puppet;
	}
}
