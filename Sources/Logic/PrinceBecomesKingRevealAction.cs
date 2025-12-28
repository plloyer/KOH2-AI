namespace Logic;

public class PrinceBecomesKingRevealAction : ResistRevealAction
{
	public PrinceBecomesKingRevealAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PrinceBecomesKingRevealAction(owner as Character, def);
	}
}
