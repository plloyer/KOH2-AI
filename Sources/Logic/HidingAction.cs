namespace Logic;

public class HidingAction : Action
{
	public HidingAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HidingAction(owner as Character, def);
	}

	public override void Run()
	{
		base.Run();
	}
}
