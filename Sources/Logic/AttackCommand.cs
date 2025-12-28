using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public class AttackCommand : SquadCommand
{
	public int attacker_cavalry;

	public int attacker_archers_melee;

	public int attacker_archers_ranged;

	public int attacker_infantry;

	public AttackCommand(BattleAI ai, MapObject target = null)
		: base(ai, target)
	{
		adding_squad_lowers_priority = true;
	}

	public override float Priority()
	{
		Squad squad = base.target as Squad;
		float threat = ai.GetThreat(squad, interpolated: true);
		Mathf.Clamp(threat, 0f, 100f);
		threat = 100f - threat;
		threat -= squad.simulation.GetMorale() * ai.def.shock_mod;
		if (squad.def.type == Unit.Type.Noble)
		{
			threat += ai.def.noble_bonus;
		}
		if (squad?.enemy_squad?.def != null && squad.enemy_squad.def.type == Unit.Type.Noble)
		{
			threat += ai.def.noble_attacked_bonus;
		}
		if (squad.climbing)
		{
			threat += ai.def.climbing_squad_bonus;
		}
		if (!squad.def.is_ranged)
		{
			if (squad?.enemy_squad?.def != null && squad.enemy_squad.def.is_ranged && !squad.enemy_squad.def.is_cavalry)
			{
				threat += ai.def.archer_attacked_bonus;
			}
			else if (squad?.target is Squad squad2 && squad2?.def != null && squad2.def.is_ranged && !squad2.def.is_cavalry)
			{
				threat += ai.def.archer_attacked_bonus;
			}
		}
		if (squad?.enemy_squad?.def != null && squad.enemy_squad.def.is_siege_eq)
		{
			threat += ai.def.siege_eq_attacked_bonus;
		}
		threat -= ai.def.drop_per_squad * (float)squads.Count;
		threat = Mathf.Clamp(threat, 1f, threat);
		return Math.Max(1f, ai.def.attack_command_mod * threat / 1.21f);
	}

	public override float Priority(Squad attacker)
	{
		Squad squad = base.target as Squad;
		float num = 0f;
		if (attacker.is_fighting)
		{
			num = ((!attacker.melee_squads.Contains(squad)) ? (num + ai.def.fighting_other_bonus) : (num + ai.def.already_fighting_bonus));
		}
		num += squad.position.Dist(attacker.position) * ai.def.distance_mod;
		if (attacker.def.CTH_cavalry_mod > 1f && squad.def.is_cavalry)
		{
			num -= ai.def.anti_cavalry_bonus;
		}
		if (attacker.def.is_cavalry)
		{
			num += PriorityAsCavalry(attacker);
		}
		else if (!attacker.def.is_ranged && squad?.enemy_squad?.def != null && squad.enemy_squad.def.is_ranged)
		{
			num += ai.def.infantry_archer_attacker_bonus;
		}
		num += ai.def.speed_mod * attacker.def.move_speed / squad.def.move_speed;
		num += ai.def.health_mod * (1f - attacker.simulation.damage) / (1f - squad.simulation.damage);
		float num2 = attacker.Threat() * 10f;
		float num3 = squad.Threat() * 10f;
		num = ((!(num2 >= num3 + ai.def.optimal_threat_advantage)) ? (num - (num3 + ai.def.optimal_threat_advantage - num2) * ai.def.threat_comparison_mod) : (num - (num2 - num3 - ai.def.optimal_threat_advantage) * ai.def.threat_comparison_mod));
		if (attacker.simulation != null && squad.simulation != null)
		{
			float morale = attacker.simulation.GetMorale();
			float morale2 = squad.simulation.GetMorale();
			num += (morale - morale2) * ai.def.morale_mod;
		}
		return num;
	}

	private float PriorityAsCavalry(Squad attacker)
	{
		Squad squad = base.target as Squad;
		float num = 0f;
		num = ((!squad.def.is_ranged) ? (num + ai.GetThreat(squad) * ai.def.cavalry_vs_lone_squad_mod) : (num + ai.def.cavalry_vs_ranged_bonus));
		if (!attacker.CanShoot(squad))
		{
			num += ai.GetAntiCavalryThreat(squad) * ai.def.anti_cavalry_bonus;
		}
		return num;
	}

	public override bool AddSquad(Squad squad)
	{
		if (!ValidateCount(squad))
		{
			return false;
		}
		if (base.AddSquad(squad))
		{
			RecalcCounts();
			return true;
		}
		return false;
	}

	public override bool Validate()
	{
		if (!ValidateTarget(base.target as Squad))
		{
			return false;
		}
		if (!base.Validate())
		{
			return false;
		}
		return true;
	}

	public static bool ValidateTarget(Squad target)
	{
		if (target == null || !target.IsValid() || target.IsDefeated() || target.simulation.state >= BattleSimulation.Squad.State.Fled)
		{
			return false;
		}
		return true;
	}

	public override bool Validate(Squad squad)
	{
		if (!base.Validate(squad))
		{
			return false;
		}
		if (ai.my_squads.Count == 1)
		{
			return true;
		}
		Squad squad2 = base.target as Squad;
		bool flag = squad.def.is_cavalry && squad.def.type != Unit.Type.Noble && ai.GetSquadGroupCount(squad2) <= 2;
		float num = squad.position.Dist(base.target.position);
		float num2 = ((!flag) ? ai.def.max_attack_dist : ai.def.max_attack_dist_cavalry_vs_lone_squad);
		if (squad.def.is_siege_eq && squad.salvos_left > 0 && squad.def.is_ranged)
		{
			num2 = Mathf.Max(squad.Max_Shoot_Dist, num2);
		}
		if (squad2.def.is_siege_eq && squad2.salvo_def != null && squad.is_inside_walls == squad2.is_inside_walls && (!squad2.CanBePacked() || (squad2.CanBePacked() && (!squad2.is_packed || (squad2.is_packed && squad2.IsPacking())))))
		{
			num2 = Mathf.Max(squad2.salvo_def.max_shoot_range * 1.1f, num2);
		}
		if (num <= num2 && IsTargetRunningAway(squad) && !squad.def.is_siege_eq)
		{
			float num3 = ((squad.GetStamina() >= 0.3f * squad.MaxStamina()) ? (squad.def.move_speed * squad.def.run_speed_mul) : squad.def.move_speed);
			float num4 = ((squad2.double_time && squad2.GetStamina() >= 0.2f * squad2.MaxStamina()) ? (squad2.def.move_speed * squad2.def.run_speed_mul) : squad2.def.move_speed);
			num2 *= Mathf.Clamp01(Mathf.Pow(num3 / num4, 2f));
		}
		if (num > num2 && ai.estimation > ai.def.est_engage_perc_min && (ai.can_kill_our_marshals || (ai.estimation < ai.def.est_engage_perc_max && ai.engaged_perc < ai.def.eng_engage_perc)))
		{
			return false;
		}
		if (squad.def.type == Unit.Type.Noble && ai.estimation > ai.def.est_engage_perc_min)
		{
			PPos pPos = squad2.VisualPosition();
			float num5 = squad.EnemyThreatAtPosition(pPos);
			float num6 = Math.Max(squad.ThreatAtPosition(pPos), squad.ThreatAtPosition());
			if (num5 > num6)
			{
				return false;
			}
		}
		if (ai.enemy_engaged_perc == 0f && num > num2 * ai.def.not_engaged_attack_dist_mod && ai.my_squads.Count > 1 && !flag && !squad.def.is_siege_eq && ai.estimation > ai.def.est_engage_perc_min && ai.estimation < ai.def.est_engage_perc_max && !squad2.def.is_siege_eq)
		{
			return false;
		}
		if (ai.IsSiege() && squad2.is_inside_walls_or_on_walls != squad.is_inside_walls_or_on_walls && !ai.IsCastleOpenToEnter() && !squad.def.is_siege_eq)
		{
			return false;
		}
		if (squad.def.is_siege_eq && squad.salvo_def != null && num < squad.salvo_def.min_shoot_range)
		{
			return false;
		}
		if (!ValidateSquad(squad, squad2))
		{
			return false;
		}
		if (!squad.def.can_attack_melee && squad.melee_squads.Count > 0)
		{
			return false;
		}
		return true;
	}

	public bool IsTargetRunningAway(Squad squad)
	{
		Squad obj = base.target as Squad;
		PPos pPos = base.target.position - squad.position;
		float num = obj.direction.Dot(pPos.GetNormalized());
		if (obj.movement.IsMoving() && num > -0.1f)
		{
			return true;
		}
		return false;
	}

	private bool ValidateSquad(Squad squad, Squad target_squad)
	{
		if (!ai.battle.fortification_destroyed)
		{
			if (squad.battle_side == 1 && !target_squad.is_inside_walls_or_on_walls && squad.is_inside_walls_or_on_walls && ai.enemy_gates == 0 && ai.battle.type != Battle.Type.BreakSiege)
			{
				return false;
			}
			if ((!squad.def.is_ranged || squad.salvos_left == 0) && squad.battle_side == 0 && target_squad.is_inside_walls_or_on_walls && !squad.is_inside_walls_or_on_walls && ai.my_gates == 0)
			{
				return false;
			}
		}
		if (target_squad.position.paID > 0 && squad.def.is_cavalry && !squad.game.path_finding.data.pointers.GetPA(target_squad.position.paID - 1).IsGround())
		{
			return false;
		}
		return true;
	}

	private bool ValidateCount(Squad squad)
	{
		if (squad == null)
		{
			return false;
		}
		int num = 0;
		if ((base.target as Squad).def.is_siege_eq)
		{
			num = 1;
		}
		if (squad.def.is_ranged)
		{
			bool flag = attacker_archers_ranged >= ai.def.max_ranged_per_squad_ranged + num;
			if (attacker_archers_melee >= ai.def.max_ranged_per_squad_melee + num && flag)
			{
				return false;
			}
		}
		else
		{
			if (squad.def.is_cavalry && attacker_cavalry >= ai.def.max_cavalry_per_squad + num)
			{
				return false;
			}
			if (attacker_infantry >= ai.def.max_infantry_per_squad + num)
			{
				return false;
			}
		}
		return true;
	}

	public override void RemoveSquad(int i)
	{
		base.RemoveSquad(i);
		RecalcCounts();
	}

	public override void RemoveSquad(Squad squad)
	{
		base.RemoveSquad(squad);
		RecalcCounts();
	}

	private void RecalcCounts()
	{
		attacker_infantry = (attacker_cavalry = (attacker_archers_melee = (attacker_archers_ranged = 0)));
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad = squads[i];
			if (squad == null || !squad.IsValid())
			{
				continue;
			}
			if (squad.def.is_ranged)
			{
				if (squad.CanShoot(base.target) && (float)attacker_archers_ranged < ai.proportion * (float)ai.def.max_ranged_per_squad_ranged)
				{
					attacker_archers_ranged++;
				}
				else
				{
					attacker_archers_melee++;
				}
			}
			else if (squad.def.is_cavalry)
			{
				attacker_cavalry++;
			}
			else
			{
				attacker_infantry++;
			}
		}
	}

	public override void Reset()
	{
		base.Reset();
		attacker_infantry = (attacker_cavalry = (attacker_archers_melee = 0));
	}

	public override void Execute()
	{
		base.Execute();
		Squad squad = base.target as Squad;
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad2 = squads[i];
			float num = squad2.position.Dist(base.target.position);
			bool flag = num < ai.def.charge_dist || squad.def.is_siege_eq || (squad.def.is_ranged && num < squad.Max_Shoot_Dist);
			if (squad2.def.is_ranged && !squad2.def.is_siege_eq && squad2.salvos_left > 0 && squad2.position.paID == 0 && ExecuteArchersAttackCommand(squad, squad2))
			{
				continue;
			}
			if (squad.climbing && squad2.def.is_infantry)
			{
				if (squad2.movement.path == null || squad2.movement.path.dst_pt.Dist(squad.climbing_pos) > 0.1f)
				{
					squad2.MoveTo(squad.climbing_pos, 0f, flag, squad2.direction, disengage: false);
				}
			}
			else if (squad2.target != base.target || (!squad2.double_time && flag))
			{
				squad2.SetStance(Squad.Stance.Aggressive);
				squad2.Attack(base.target, flag);
			}
		}
	}

	private bool ExecuteArchersAttackCommand(Squad target_sq, Squad squad)
	{
		if (ai.GetThreat(target_sq) < 0.5f || squad.climbing)
		{
			return false;
		}
		float num = squad.salvo_def.max_shoot_range * ai.def.attack_command_safe_shooting_range_mod;
		target_sq.CalcPos(out var pos, out var _, ai.def.update_interval);
		float num2 = pos.Dist(squad.position);
		float num3 = target_sq.position.Dist(squad.position);
		if (num3 < num)
		{
			PPos normalized = (squad.position - target_sq.position).GetNormalized();
			PPos pt = squad.position + normalized * (squad.salvo_def.max_shoot_range - num3);
			squad.MoveTo(pt, 0f, squad.GetStamina() / squad.def.stamina_max > 0.5f, normalized, squad.is_fighting || squad.is_fighting_target);
			return true;
		}
		if (num2 < num)
		{
			PPos normalized = (squad.position - pos).GetNormalized();
			PPos pt = squad.position + normalized * (squad.salvo_def.max_shoot_range - num2);
			squad.MoveTo(pt, 0f, squad.GetStamina() / squad.def.stamina_max > 0.5f, normalized, squad.is_fighting || squad.is_fighting_target);
			return true;
		}
		if (squad.def.is_cavalry && num2 < squad.salvo_def.max_shoot_range && num2 > num)
		{
			PPos normalized = (squad.position - pos).GetNormalized();
			PPos pt = squad.position;
			float num4 = squad.salvo_def.max_shoot_range * 0.85f;
			float num5 = 25f;
			int num6 = 5;
			for (int i = 0; i < num6; i++)
			{
				float num7 = UnityEngine.Random.Range((0f - num5) * 2f, num5 * 2f);
				if (num7 > 0f && num7 < num5)
				{
					num7 += num5;
				}
				if (num7 < 0f && num7 > 0f - num5)
				{
					num7 -= num5;
				}
				PPos normalized2 = normalized.GetRotated(num7).GetNormalized();
				pt = pos + normalized2 * num4;
				PPos pPos = squad.position - pt;
				if (squad.direction.Dot(pPos) < 0f)
				{
					pt = pos + normalized2 * squad.salvo_def.max_shoot_range;
				}
				if (ai.GetThreat(pt, target_sq.battle_side, interpolated: true) == 0f)
				{
					break;
				}
			}
			squad.MoveTo(pt, 0f, squad.GetStamina() / squad.def.stamina_max > 0.5f, normalized, squad.is_fighting || squad.is_fighting_target);
			return true;
		}
		if (!squad.def.is_cavalry && squad.position.paID == 0)
		{
			float max_optimal_position_search_dist = ai.def.max_optimal_position_search_dist;
			int iterations = 2;
			PPos normalized = (squad.position - target_sq.position).GetNormalized();
			List<PPos> betterPositionsToShoot = GetBetterPositionsToShoot(target_sq, squad, num, max_optimal_position_search_dist, iterations);
			if (betterPositionsToShoot != null && betterPositionsToShoot.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, betterPositionsToShoot.Count);
				PPos pt = betterPositionsToShoot[index];
				squad.MoveTo(pt, 0f, squad.GetStamina() / squad.def.stamina_max > 0.5f, normalized, squad.is_fighting || squad.is_fighting_target);
				return true;
			}
		}
		return false;
	}

	private unsafe List<PPos> GetBetterPositionsToShoot(Squad target_sq, Squad squad, float safe_distance, float range, int iterations)
	{
		bool enabled = squad.trees_buff.enabled;
		bool flag = !squad.high_ground_buff.enabled || (squad.high_ground_buff.enabled && squad.actual_position_height - target_sq.actual_position_height <= 0f);
		if (!enabled && !flag)
		{
			return null;
		}
		List<PPos> list = new List<PPos>();
		float threat = ai.GetThreat(squad.position, target_sq.battle_side, interpolated: true);
		for (int i = -iterations; i <= iterations; i++)
		{
			for (int j = -iterations; j <= iterations; j++)
			{
				float num = squad.position.x + (float)i * range;
				float num2 = squad.position.y + (float)j * range;
				PPos pPos = new PPos(num, num2);
				if (num <= 0f || num2 <= 0f || num >= (float)squad.battle.batte_view_game.path_finding.data.width || num2 >= (float)squad.battle.batte_view_game.path_finding.data.height || squad.battle.GetTreeCount(pPos) >= squad.trees_buff.tree_buff_def.min_trees_count)
				{
					continue;
				}
				bool flag2 = false;
				if (enabled)
				{
					flag2 = true;
				}
				else
				{
					float height = ai.game.heights.data->GetHeight((int)pPos.x, (int)pPos.y);
					if (height > squad.actual_position_height)
					{
						float num3 = Math.Abs(target_sq.actual_position_height - squad.actual_position_height);
						squad.high_ground_buff.CalcMod(height, target_sq.actual_position_height, out var res, out var height_difference);
						float num4 = Math.Abs(height_difference);
						if (num4 > squad.high_ground_buff.high_ground_def.min_height_diff && num4 - num3 >= 1f && num4 - num3 < 2f && res > 0f)
						{
							flag2 = true;
						}
					}
				}
				if (!flag2)
				{
					continue;
				}
				float threat2 = ai.GetThreat(pPos, target_sq.battle_side, interpolated: true);
				if (threat - threat2 >= -0.1f)
				{
					float num5 = pPos.Dist(target_sq.position);
					if (num5 >= safe_distance && num5 <= squad.salvo_def.max_shoot_range)
					{
						list.Add(pPos);
					}
				}
			}
		}
		return list;
	}
}
