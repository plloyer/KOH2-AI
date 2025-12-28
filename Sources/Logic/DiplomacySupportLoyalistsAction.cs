namespace Logic;

public class DiplomacySupportLoyalistsAction : DiplomatAction
{
	public DiplomacySupportLoyalistsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacySupportLoyalistsAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character == null)
		{
			return;
		}
		for (int i = 0; i < character.mission_kingdom.rebellions.Count; i++)
		{
			Rebellion rebellion = character.mission_kingdom.rebellions[i];
			if (rebellion != null && rebellion.IsLoyalist())
			{
				rebellion.Support(character.mission_kingdom, 100);
			}
		}
		base.Run();
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
		if (character.GetSkill("Conspiracy") == null)
		{
			return "no_conspiracy_skill";
		}
		for (int i = 0; i < character.mission_kingdom.armies_in.Count; i++)
		{
			Rebel rebel = character.mission_kingdom.armies_in[i].rebel;
			if (rebel != null && rebel.IsLoyalist())
			{
				return base.Validate(quick_out);
			}
		}
		return "no_loyalists_in_mission_kingdom";
	}
}
