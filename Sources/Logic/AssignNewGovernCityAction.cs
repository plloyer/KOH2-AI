namespace Logic;

internal class AssignNewGovernCityAction : ReassignGovernCityAction
{
	public AssignNewGovernCityAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AssignNewGovernCityAction(owner as Character, def);
	}

	protected override bool CompetingActionRunning()
	{
		if (base.own_character?.actions.Find("GovernCityAction")?.is_active ?? false)
		{
			return true;
		}
		if (base.own_character?.actions.Find("ReassignGovernCityAction")?.is_active ?? false)
		{
			return true;
		}
		return false;
	}
}
