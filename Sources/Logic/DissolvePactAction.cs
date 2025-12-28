namespace Logic;

public class DissolvePactAction : Action
{
	public DissolvePactAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DissolvePactAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.pact == null)
		{
			return "no_pact";
		}
		return "ok";
	}

	public override void Run()
	{
		if (base.own_character.pact != null)
		{
			base.own_character.pact.Dissolve();
		}
		base.Run();
	}
}
