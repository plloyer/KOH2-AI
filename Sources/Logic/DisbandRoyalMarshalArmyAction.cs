namespace Logic;

public class DisbandRoyalMarshalArmyAction : DisbandArmyAction
{
	public DisbandRoyalMarshalArmyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DisbandRoyalMarshalArmyAction(owner as Character, def);
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
		if (!character.IsMarshal())
		{
			return "not_marshal";
		}
		if (!character.IsKingOrPrince())
		{
			return "not_royal";
		}
		if (army.battle != null)
		{
			return "in_battle";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		return "ok";
	}

	public override void Run()
	{
		base.own_character.Recall(disband_army: true, from_prison: false, spawn_marshall: false);
	}
}
