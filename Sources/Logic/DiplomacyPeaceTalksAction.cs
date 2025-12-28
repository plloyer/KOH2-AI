namespace Logic;

public class DiplomacyPeaceTalksAction : Action
{
	public DiplomacyPeaceTalksAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyPeaceTalksAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		return base.Validate(quick_out);
	}

	public override void Cancel(bool manual = false, bool notify = true)
	{
		base.own_character.DelStatus<PeaceTalksStatus>();
		base.Cancel(manual, notify);
	}

	public override void Prepare()
	{
		PeaceTalksStatus status = new PeaceTalksStatus(base.target as Kingdom);
		base.own_character.AddStatus(status);
		base.Prepare();
	}
}
