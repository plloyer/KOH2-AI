namespace Logic;

public class DiplomacyProvokeWarWithAction : DiplomatAction
{
	public DiplomacyProvokeWarWithAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyProvokeWarWithAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.mission_kingdom.StartWarWith(base.target as Kingdom, War.InvolvementReason.DiplomatProvocation, "WarProvokedMessage", own_kingdom);
			base.Run();
		}
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		Kingdom k = target as Kingdom;
		if (!War.CanStart(base.own_character.mission_kingdom, k))
		{
			return false;
		}
		return base.ValidateTarget(target);
	}
}
