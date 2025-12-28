namespace Logic;

public class ForceRevealAction : ResistRevealAction
{
	public ForceRevealAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ForceRevealAction(owner as Character, def);
	}
}
