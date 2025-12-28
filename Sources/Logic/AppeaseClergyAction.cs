namespace Logic;

public class AppeaseClergyAction : CharacterOpportunity
{
	private int increase;

	public AppeaseClergyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AppeaseClergyAction(owner as Character, def);
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			DT.Field field = def.field.FindChild("increase");
			increase = base.game.Random(field.Value(0), (int)field.Value(1) + 1);
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.SetVar("rand_increase", increase);
	}
}
