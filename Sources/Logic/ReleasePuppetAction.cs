namespace Logic;

public class ReleasePuppetAction : Action
{
	public ReleasePuppetAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ReleasePuppetAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return false;
		}
		if (character.puppets == null)
		{
			return false;
		}
		if (!(target is Character item))
		{
			return false;
		}
		if (!character.puppets.Contains(item))
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		Character puppet = base.target as Character;
		base.own_character?.DelPuppet(puppet);
	}
}
