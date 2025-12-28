using System;

namespace Logic;

public class CaptureCommand : SquadCommand
{
	public CapturePoint target_capture_point;

	private PPos target_pos;

	public CaptureCommand(BattleAI ai, CapturePoint target, Squad squad)
		: base(ai, squad)
	{
		target_capture_point = target;
		CalculateBaseTargetPosition();
	}

	private void CalculateBaseTargetPosition()
	{
		target_pos = target_capture_point.position;
		if (target_capture_point.IsInsideWall && target_capture_point.fortifications.Count != 0 && !(ai.battle.citadel_position == PPos.Zero))
		{
			float min = target_capture_point.radius * 0.5f;
			float radius = target_capture_point.radius;
			PPos pPos = ai.battle.citadel_position - target_pos;
			float num = ai.game.Random(min, radius);
			PPos pPos2 = target_pos + pPos.GetNormalized() * num;
			target_pos = pPos2;
		}
	}

	public override float Priority()
	{
		if (ai.my_squads.Count <= 1)
		{
			return 0f;
		}
		CapturePoint capturePoint = target_capture_point;
		Squad squad = base.target as Squad;
		if (capturePoint == null || squad == null)
		{
			return 0f;
		}
		if (!IsSquadValidToCapture(squad, capturePoint.IsInsideWall))
		{
			return 0f;
		}
		Fortification fortification = capturePoint.fortification;
		if (fortification != null && fortification.IsDefeated())
		{
			return 0f;
		}
		if (squad.def.is_siege_eq && (fortification == null || (fortification != null && fortification.def.type != Fortification.Type.Gate) || ai.battle.type == Battle.Type.BreakSiege))
		{
			return 0f;
		}
		float num = capturePoint.position.Dist(squad.position) * ai.def.distance_mod;
		float num2 = ai.GetMaxThreat(capturePoint.position, capturePoint.radius, capturePoint.battle_side, interpolated: true);
		float importance = capturePoint.GetImportance();
		float num3 = 0f;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		if (num2 > 100f)
		{
			num2 = 100f;
		}
		num2 = ai.def.capture_threat_base - ai.def.capture_threat_mod * num2;
		num3 = num2;
		num3 += importance * ai.def.capture_importance_mod;
		num3 += num * ai.def.capture_distance_mod;
		if (capturePoint.has_enemy_troops && capturePoint.capturing_squads.Count != 0 && !capturePoint.capturing_squads.Contains(squad))
		{
			num3 *= 0.5f;
		}
		if (capturePoint.has_friendly_troops)
		{
			num3 *= 0.6f;
		}
		if (capturePoint.def.count_victory)
		{
			num3 += (float)(ai.important_enemy_capture_points - ai.important_my_capture_points) * ai.def.capture_per_cp_diff_bonus;
		}
		num3 = ((!capturePoint.def.count_victory) ? (num3 * ai.def.defend_command_mod) : (num3 * ai.def.defend_command_mod_victory));
		if (capturePoint.battle_side == 1 && fortification != null && fortification.def.type == Fortification.Type.Gate && !ai.battle.fortification_destroyed && ai.my_gates == 0)
		{
			num3 *= ai.def.capture_gate_mod;
		}
		if (fortification != null)
		{
			num3 *= ai.def.capture_fortification_mod;
		}
		if (squad.def.is_siege_eq)
		{
			num3 = ((!(num3 <= 0f) || fortification == null || fortification.def.type != Fortification.Type.Gate) ? (num3 * 2f) : (num3 + 80f));
		}
		if (squad.def.is_ranged && !squad.def.is_cavalry)
		{
			num3 *= 0.5f;
		}
		num3 = ((ai.battle_side != 1) ? (num3 * ai.def.capture_attacker_mod) : (num3 * ai.def.capture_defender_mod));
		int count = ai.battle.simulation.GetSquads(1 - ai.battle_side).Count;
		if (count > 0)
		{
			float num4 = count;
			for (int i = 0; i < ai.enemy_squads.Count; i++)
			{
				Squad squad2 = ai.enemy_squads[i];
				num4 -= 1f - squad2.simulation.damage;
			}
			num4 = Math.Max(0.1f, num4 / (float)count);
			num3 *= num4;
		}
		num3 *= ai.def.speed_mod * squad.def.move_speed;
		num3 *= ai.def.health_mod * (1f - squad.simulation.damage);
		num3 *= ai.capture_command_mod_mul;
		return Math.Max(0f, num3);
	}

	private bool IsSquadValidToCapture(Squad squad, bool isInsideWall)
	{
		if (squad.def.is_cavalry && ai.my_gates == 0 && !ai.battle.fortification_destroyed && isInsideWall)
		{
			Fortification fortification = target_capture_point.fortification;
			if (ai.GetSiegeEqSquad(squad.battle_side) == null && fortification != null && fortification.def.type == Fortification.Type.Gate)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	public override bool SingleSquad()
	{
		return true;
	}

	public override bool Validate()
	{
		if (target_capture_point == null || target_capture_point.battle_side == ai.battle_side)
		{
			return false;
		}
		if (!(base.target is Squad squad) || !squad.IsValid() || squad.IsDefeated())
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
		float num = squad.position.Dist(target_capture_point.position);
		if (squad.def.is_siege_eq && squad.CanAttack(target_capture_point.fortification))
		{
			return true;
		}
		if (squad.def.is_cavalry && target_capture_point.has_friendly_troops)
		{
			for (int i = 0; i < target_capture_point.ally_squads.Count; i++)
			{
				if (target_capture_point.ally_squads[i].is_on_walls)
				{
					return false;
				}
			}
		}
		float num2 = ((squad.ai_command == this) ? 1.2f : 1f);
		if (num > ai.def.capture_max_dist * num2 || ((ai.estimation > ai.def.est_engage_perc_max || ai.estimation < ai.def.est_engage_perc_min) && (ai.my_gates > 0 || ai.battle.fortification_destroyed)))
		{
			return false;
		}
		return true;
	}

	public static bool ValidateTarget(CapturePoint target, int battle_side)
	{
		if (target == null || !target.IsValid() || target.battle_side != battle_side)
		{
			return false;
		}
		return true;
	}

	public override void Execute()
	{
		base.Execute();
		if (squads.Count == 0 || target_pos.x <= 0f || target_pos.y <= 0f || target_pos.x >= (float)ai.game.path_finding.data.width || target_pos.y >= (float)ai.game.path_finding.data.height)
		{
			return;
		}
		Squad squad = squads[0];
		Fortification fortification = target_capture_point.fortification;
		if (fortification != null && fortification.def.type == Fortification.Type.Gate && squad.can_attack_gates)
		{
			int num = 0;
			for (int i = 0; i < fortification.cur_attackers.Count; i++)
			{
				Squad squad2 = fortification.cur_attackers[i];
				if (squad2 != squad && squad2.is_fighting && squad2.enemy_melee_fortification == fortification)
				{
					num++;
				}
			}
			if (squad.def.is_siege_eq || ((num < 2 || squad.enemy_melee_fortification == fortification) && squad.def.is_cavalry && ai.my_gates == 0 && !ai.battle.fortification_destroyed && ai.GetSiegeEqSquad(squad.battle_side) == null))
			{
				if (squad.enemy_melee_fortification != fortification || (!squad.is_fighting && squad.movement.path == null))
				{
					squad.Attack(fortification, double_time: false);
				}
				return;
			}
		}
		if (squad.movement.path == null || !(squad.movement.path.dst_pt == target_pos))
		{
			squad.MoveTo(target_pos, 1f, double_time: false, (target_pos - squad.position).GetNormalized(), disengage: false, defeated_move: false, avoiding_move: false, returning_move: false, ignore_reserve: false, -1f, important: false, add_to_command_queue: false, from_command_queue: false, force_no_threat: true);
		}
	}
}
