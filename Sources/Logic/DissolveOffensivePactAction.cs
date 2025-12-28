namespace Logic;

public class DissolveOffensivePactAction : DissolvePactAction
{
	public DissolveOffensivePactAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DissolveOffensivePactAction(owner as Character, def);
	}
}
