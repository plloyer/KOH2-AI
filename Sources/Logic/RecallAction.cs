namespace Logic;

public class RecallAction : Action
{
	public RecallAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new RecallAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "dead";
		}
		if (character.IsIdle() && character.mission_kingdom == null)
		{
			return "idle";
		}
		if (character.prison_kingdom != null)
		{
			return "in_prison";
		}
		if (character.cur_action != null)
		{
			return "in_action";
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

	public override void Prepare()
	{
		base.own_character.StopTrade();
		base.own_character.StopDiplomacy();
		base.Prepare();
	}

	public override void Run()
	{
		base.own_character.Recall();
		OnMissionChangedAnalytics();
		base.Run();
	}
}
