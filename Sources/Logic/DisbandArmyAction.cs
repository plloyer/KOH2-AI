namespace Logic;

public class DisbandArmyAction : RecallAction
{
	public DisbandArmyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DisbandArmyAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.cur_action is RecallAction)
		{
			return null;
		}
		Army army = character.GetArmy();
		if (army == null)
		{
			return "no_army";
		}
		if (character.IsMarshal())
		{
			return "marshal";
		}
		if (army.battle != null)
		{
			return "in_battle";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		if (character.IsRebel())
		{
			return "rebel";
		}
		return "ok";
	}

	public override void Prepare()
	{
		base.own_character.GetArmy()?.Stop();
		base.Prepare();
	}
}
