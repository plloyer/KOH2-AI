namespace Logic;

public class FollowCommand : SquadCommand
{
	private const float OFFSET_DIST_BONUS = 40f;

	private Squad follow_target;

	public FollowCommand(BattleAI ai, Squad target)
		: base(ai, target)
	{
		follow_target = FindNewFollowTarget();
	}

	public override bool SingleSquad()
	{
		return true;
	}

	public override float Priority()
	{
		return ai.def.follow_command_base;
	}

	public static float FollowOrder(Squad sq)
	{
		if (sq == null)
		{
			return 1f;
		}
		float num = (sq.def.is_cavalry ? 1f : (sq.def.is_ranged ? 1.5f : ((sq.def.type != Unit.Type.InventoryItem) ? 2f : 0f)));
		return num + 0.25f * ((6f - sq.def.move_speed) / 6f);
	}

	public override bool Validate()
	{
		if (!ValidateTarget(base.target as Squad))
		{
			return false;
		}
		return base.Validate();
	}

	public override bool Validate(Squad squad)
	{
		if (!base.Validate(squad))
		{
			return false;
		}
		return ValidateTarget(squad);
	}

	public static bool ValidateTarget(Squad target)
	{
		if (target == null || !target.IsValid() || target.IsDefeated() || target.simulation.state >= BattleSimulation.Squad.State.Fled || !target.def.is_siege_eq)
		{
			return false;
		}
		return true;
	}

	public override void Execute()
	{
		base.Execute();
		if (squads.Count != 0)
		{
			Squad squad = base.target as Squad;
			if (follow_target == null || follow_target.IsDefeated() || !follow_target.IsValid() || follow_target.simulation.state >= BattleSimulation.Squad.State.Fled || follow_target.position.paID > 0)
			{
				follow_target = FindNewFollowTarget();
			}
			if (follow_target != null && follow_target != squad && (squad.movement.path == null || (squad.movement.path != null && squad.target != follow_target && squad.position.Dist(follow_target.position) > squad.formation.cur_height + 40f + 10f)))
			{
				PPos position = follow_target.position;
				PPos normalized = (position - squad.position).GetNormalized();
				squad.MoveTo(position - normalized * 40f, squad.formation.cur_height, double_time: false, (follow_target.position - squad.position).GetNormalized(), disengage: false);
			}
		}
	}

	private Squad FindNewFollowTarget()
	{
		if (!(base.target is Squad squad))
		{
			return null;
		}
		Squad squad2 = null;
		if (squad.simulation.army != null)
		{
			squad2 = ai.GetCommander(ai.battle_side, squad.simulation.army);
		}
		else if (squad.simulation.garrison != null)
		{
			squad2 = ai.GetCommander(ai.battle_side, squad.simulation.garrison);
		}
		if (squad2 == null)
		{
			squad2 = ai.GetCommander(ai.battle_side);
		}
		return squad2;
	}
}
