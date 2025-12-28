namespace Logic;

public class BolsterInfluenceAction : Action
{
	public BolsterInfluenceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new BolsterInfluenceAction(owner as Character, def);
	}
}
