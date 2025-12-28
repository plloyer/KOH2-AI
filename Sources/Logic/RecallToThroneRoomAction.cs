namespace Logic;

public class RecallToThroneRoomAction : RecallAction
{
	public RecallToThroneRoomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new RecallToThroneRoomAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.IsKing())
		{
			return "is_king";
		}
		if (!character.IsPrince())
		{
			return "not_royal";
		}
		if (!character.IsIdle())
		{
			return "not_idle";
		}
		if (character.prison_kingdom != null)
		{
			return "in_prison";
		}
		if (!character.IsInCourt())
		{
			return "not_in_court";
		}
		if (character.GetArmy() != null)
		{
			return "leading_army";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		if (character.cur_action is RecallAction)
		{
			return null;
		}
		if (character.cur_action is SpyPlot)
		{
			return "another_action_in_progress";
		}
		return "ok";
	}

	public override void Run()
	{
		base.own_character.ReturnToThroneRoom();
	}
}
