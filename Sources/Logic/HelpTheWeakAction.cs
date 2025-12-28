namespace Logic;

public class HelpTheWeakAction : Action
{
	public HelpTheWeakAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HelpTheWeakAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!base.own_character.IsCleric())
		{
			return "not_cleric";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		base.owner.SetStatus<HelpTheWeakStatus>();
		base.Run();
	}
}
