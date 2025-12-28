namespace Logic;

public class SubordinateAction : Action
{
	public SubordinateAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SubordinateAction(owner as Character, def);
	}

	public override void Run()
	{
		Kingdom k = own_kingdom;
		base.game.religions.orthodox.SetSubordinated(k, subordinated: true);
		base.Run();
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "rel_change_with_orthodox_constantinople")
		{
			Value value = outcome.field.value;
			own_kingdom.AddRelationModifier(own_kingdom.game.religions.orthodox.head_kingdom, "rel_change_with_orthodox_constantinople", null, value);
			return true;
		}
		return base.ApplyOutcome(outcome);
	}
}
