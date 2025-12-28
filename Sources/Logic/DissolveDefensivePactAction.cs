namespace Logic;

public class DissolveDefensivePactAction : DissolvePactAction
{
	public DissolveDefensivePactAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DissolveDefensivePactAction(owner as Character, def);
	}
}
