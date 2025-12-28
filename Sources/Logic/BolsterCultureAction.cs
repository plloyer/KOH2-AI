namespace Logic;

public class BolsterCultureAction : Action
{
	public BolsterCultureAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new BolsterCultureAction(owner as Character, def);
	}
}
