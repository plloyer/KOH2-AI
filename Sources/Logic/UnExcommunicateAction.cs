namespace Logic;

public class UnExcommunicateAction : Action
{
	public UnExcommunicateAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new UnExcommunicateAction(owner as Kingdom, def);
	}

	public override void Run()
	{
		base.game?.religions?.catholic?.UnExcommunicate(own_kingdom);
		base.Run();
	}
}
