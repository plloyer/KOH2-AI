namespace Logic;

public class DiplomacyArrangeAnnexationAction : DiplomatAction
{
	public DiplomacyArrangeAnnexationAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyArrangeAnnexationAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character == null)
		{
			return;
		}
		Kingdom mission_kingdom = character.mission_kingdom;
		if (mission_kingdom == null || mission_kingdom.IsDefeated())
		{
			return;
		}
		using (new Kingdom.CacheRBS("DiplomacyArrangeAnnexationAction"))
		{
			for (int num = mission_kingdom.realms.Count - 1; num >= 0; num--)
			{
				mission_kingdom.realms[num].SetKingdom(own_kingdom.id, ignore_victory: false, check_cancel_battle: true, via_diplomacy: true);
			}
		}
		base.Run();
	}

	public override void Finish()
	{
		_ = base.own_character;
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
		if (character.mission_kingdom.sovereignState != own_kingdom)
		{
			return "not_a_vassal";
		}
		return base.Validate(quick_out);
	}
}
