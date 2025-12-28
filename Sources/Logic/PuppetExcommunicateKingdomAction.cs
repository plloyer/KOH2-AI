namespace Logic;

public class PuppetExcommunicateKingdomAction : PuppetPlot
{
	public PuppetExcommunicateKingdomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetExcommunicateKingdomAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "no_mission_kingdom";
		}
		if (!base.own_character.mission_kingdom.is_catholic)
		{
			return "mission_kingdom_not_catholic";
		}
		if (base.own_character.mission_kingdom.excommunicated)
		{
			return "already_excommunicated";
		}
		if (base.own_character.mission_kingdom == base.game?.religions?.catholic?.hq_kingdom)
		{
			return "mission_kingdom_is_papacy";
		}
		return base.Validate(quick_out);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (!puppet.IsPope())
		{
			return "not_pope";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		Character character = base.target as Character;
		base.game.religions.catholic.Excommunicate(base.own_character.mission_kingdom);
		base.own_character.mission_kingdom.DelSpecialCourtMember(base.game.religions.catholic.head);
		base.own_character.DelPuppet(character);
		character.ClearMasters("Betray");
		base.Run();
	}
}
