namespace Logic;

public class SearchForSpouseAction : Action
{
	public SearchForSpouseAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SearchForSpouseAction(owner as Character, def);
	}
}
