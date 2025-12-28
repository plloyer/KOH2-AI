namespace Logic;

public class DiplomacySearchForHelpWithRebelsAction : Action
{
	public DiplomacySearchForHelpWithRebelsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacySearchForHelpWithRebelsAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		if (own_kingdom.rebellions.Count == 0)
		{
			return "no_rebellions";
		}
		return base.Validate(quick_out);
	}

	public override void Cancel(bool manual = false, bool notify = true)
	{
		base.own_character.DelStatus<SearchingForHelpWithRebelsStatus>();
		base.Cancel(manual, notify);
	}

	public override void Prepare()
	{
		SearchingForHelpWithRebelsStatus status = new SearchingForHelpWithRebelsStatus();
		base.own_character.AddStatus(status);
		base.Prepare();
	}
}
