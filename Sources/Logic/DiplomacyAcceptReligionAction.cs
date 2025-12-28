namespace Logic;

public class DiplomacyAcceptReligionAction : DiplomatAction
{
	public DiplomacyAcceptReligionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyAcceptReligionAction(owner as Character, def);
	}

	public override void Run()
	{
		if (base.own_character != null)
		{
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
		if (character.GetSkill("Piety") == null)
		{
			return "no_finances_skill";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		return base.Validate(quick_out);
	}
}
