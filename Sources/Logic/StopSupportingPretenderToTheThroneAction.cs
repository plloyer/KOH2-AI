namespace Logic;

public class StopSupportingPretenderToTheThroneAction : Action
{
	public StopSupportingPretenderToTheThroneAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new StopSupportingPretenderToTheThroneAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!ValidateTarget(base.target))
		{
			return "no_valid_target";
		}
		return base.Validate(quick_out);
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
		if (!(target is Character character2))
		{
			return false;
		}
		if (!character.puppets.Contains(character2))
		{
			return false;
		}
		if (character2 != character2.GetKingdom()?.royalFamily?.crownPretender?.pretender)
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		(base.target as Character).GetKingdom().royalFamily.SetPretender(null, null, was_puppet: false);
	}
}
