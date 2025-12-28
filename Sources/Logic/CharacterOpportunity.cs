namespace Logic;

public abstract class CharacterOpportunity : Action
{
	public CharacterOpportunity(Character owner, Def def)
		: base(owner, def)
	{
	}
}
