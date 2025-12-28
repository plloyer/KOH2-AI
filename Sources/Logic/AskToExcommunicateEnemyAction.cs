namespace Logic;

public class AskToExcommunicateEnemyAction : ExcommunicateAction
{
	public AskToExcommunicateEnemyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AskToExcommunicateEnemyAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		string text = base.Validate(quick_out: false);
		if (text != "ok")
		{
			return text;
		}
		if (base.own_character.GetSpecialCourtKingdom() == base.game.religions.catholic.hq_kingdom)
		{
			return "not_a_foreign_pope";
		}
		if (base.game.session_time < base.game.religions.catholic.next_enemy_excommunicate)
		{
			return "_cooldown";
		}
		return text;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!base.ValidateTarget(target))
		{
			return false;
		}
		if (!(target as Kingdom).IsEnemy(base.game.religions.catholic.head_kingdom))
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		base.Run();
		base.game.religions.catholic.CalcNextEnemyExcommunicateTime();
	}
}
