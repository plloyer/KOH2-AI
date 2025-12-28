using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Logic;

public class DefendCommand : SquadCommand
{
	public abstract class Subcommand
	{
		public DefendCommand defend_command;

		public List<Squad> squads = new List<Squad>();

		public float cached_priority;

		public bool adding_squad_lowers_priority;

		public float GetCount => (squads != null) ? squads.Count : 0;

		protected BattleAI ai => defend_command?.ai;

		protected CapturePoint cp => defend_command?.cp;

		public Subcommand(DefendCommand defend_command)
		{
			this.defend_command = defend_command;
		}

		public abstract void Execute();

		public abstract float Priority();

		public abstract float Priority(Squad squad);

		public virtual bool Validate()
		{
			return true;
		}

		public virtual bool Validate(Squad squad)
		{
			if (squad == null || !squad.IsValid() || squad.IsDefeated())
			{
				return false;
			}
			return true;
		}

		public virtual void Reset()
		{
			for (int num = squads.Count - 1; num >= 0; num--)
			{
				Squad squad = squads[num];
				defend_command.RemoveSquadAssigment(squad);
			}
			squads.Clear();
		}

		public virtual bool AddSquad(Squad squad)
		{
			if (!Validate(squad))
			{
				RemoveSquad(squad);
				return false;
			}
			Subcommand squadAssigment = defend_command.GetSquadAssigment(squad);
			if (squadAssigment == this)
			{
				return true;
			}
			if (squadAssigment != null && MaintainDecision(squad))
			{
				return false;
			}
			if (squads.Contains(squad))
			{
				squad.game.Error("Adding duplicate squad in sub_command");
			}
			else
			{
				squads.Add(squad);
			}
			defend_command.ChangeSquadAssigment(squad, this);
			return true;
		}

		protected virtual bool MaintainDecision(Squad squad)
		{
			Subcommand squadAssigment = defend_command.GetSquadAssigment(squad);
			if (!squadAssigment.Validate(squad))
			{
				return false;
			}
			float change_decision_ratio = ai.def.change_decision_ratio;
			if (cached_priority / squadAssigment.cached_priority >= change_decision_ratio)
			{
				return false;
			}
			return true;
		}

		public virtual void RemoveSquad(Squad squad, bool remove_from_assigment_dict = true)
		{
			if (remove_from_assigment_dict)
			{
				defend_command.RemoveSquadAssigment(squad);
			}
			squads.Remove(squad);
		}
	}

	private class AttackSubcommand : Subcommand
	{
		public Squad target;

		private float base_priority
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_attack_base_priority;
				}
				return 60f;
			}
		}

		private float capturing_bonus
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_attack_capturing_bonus;
				}
				return 20f;
			}
		}

		private float distance_bonus
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_attack_distance_bonus;
				}
				return -0.05f;
			}
		}

		private float already_in_melee_bonus
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_attack_already_in_melee_bonus;
				}
				return -10f;
			}
		}

		public AttackSubcommand(DefendCommand defend_command, Squad target)
			: base(defend_command)
		{
			this.target = target;
			adding_squad_lowers_priority = true;
		}

		public override void Execute()
		{
			for (int num = squads.Count - 1; num >= 0; num--)
			{
				Squad squad = squads[num];
				if (target.climbing && squad.def.is_infantry)
				{
					squad.MoveTo(target.climbing_pos, 0f, squad.position.Dist(target.position) < base.ai.def.charge_dist, squad.direction, disengage: false);
				}
				else if (squad.target != target)
				{
					bool force_melee = base.cp.has_enemy_troops && !base.cp.has_friendly_troops;
					squad.SetStance(Squad.Stance.Defensive);
					squad.Attack(target, squad.position.Dist(target.position) < base.ai.def.charge_dist, command: false, force_melee);
				}
			}
		}

		public override float Priority()
		{
			float num = base_priority;
			if (base.cp.capturing_squads.Contains(target))
			{
				num += capturing_bonus;
			}
			num += target.position.Dist(base.cp.position) * distance_bonus;
			if (target.melee_squads != null)
			{
				foreach (Squad melee_squad in target.melee_squads)
				{
					if (!defend_command.squads.Contains(melee_squad))
					{
						num += already_in_melee_bonus;
					}
				}
			}
			return num;
		}

		public override float Priority(Squad squad)
		{
			float num = 0f;
			if (squad.is_fighting)
			{
				num = ((!squad.melee_squads.Contains(target)) ? (num + base.ai.def.fighting_other_bonus) : (num + base.ai.def.already_fighting_bonus));
			}
			if (squad.def.CTH_cavalry_mod > 1f && target.def.is_cavalry)
			{
				num -= base.ai.def.anti_cavalry_bonus;
			}
			if (squad.def.is_cavalry)
			{
				num = ((!target.def.is_ranged) ? (num + base.ai.GetThreat(target) * base.ai.def.cavalry_vs_lone_squad_mod) : (num + base.ai.def.cavalry_vs_ranged_bonus));
				if (!squad.CanShoot(target))
				{
					num += base.ai.GetAntiCavalryThreat(target) * base.ai.def.anti_cavalry_bonus;
				}
			}
			else if (!squad.def.is_ranged && target?.enemy_squad?.def != null && target.enemy_squad.def.is_ranged)
			{
				num += base.ai.def.infantry_archer_attacker_bonus;
			}
			if (squad.simulation != null && squad.simulation != null)
			{
				float morale = squad.simulation.GetMorale();
				float morale2 = squad.simulation.GetMorale();
				num += (morale - morale2) * base.ai.def.morale_mod;
			}
			return num;
		}

		public override bool Validate()
		{
			if (defend_command.enemies_in_range.Contains(target))
			{
				return true;
			}
			return false;
		}

		public override bool Validate(Squad squad)
		{
			if (!base.Validate(squad))
			{
				return false;
			}
			if (target.position.Dist(base.cp.position) < base.cp.radius + base.ai.def.defend_additional_radius * 2f && (base.ai.IsCastleOpenToEnter() || target.is_inside_walls_or_on_walls == squad.is_inside_walls_or_on_walls || (target.climbing && squad.def.is_infantry)))
			{
				PathData.PassableArea passableArea = default(PathData.PassableArea);
				if (target.position.paID > 0)
				{
					passableArea = target.game.path_finding.data.pointers.GetPA(target.position.paID - 1);
				}
				if (target.climbing && squad.def.is_infantry)
				{
					return true;
				}
				if (!squad.def.is_cavalry || target.position.paID <= 0 || passableArea.IsGround())
				{
					return true;
				}
			}
			return false;
		}
	}

	private class InterceptSubcommand : Subcommand
	{
		public Squad target;

		private float base_priority
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_intercept_base_priority;
				}
				return 60f;
			}
		}

		private float already_in_melee_bonus
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_intercept_already_in_melee_bonus;
				}
				return -15f;
			}
		}

		private float speed_mul
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_intercept_speed_mul;
				}
				return 2f;
			}
		}

		private float distance_bonus
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_intercept_distance_bonus;
				}
				return -0.05f;
			}
		}

		public InterceptSubcommand(DefendCommand defend_command, Squad target)
			: base(defend_command)
		{
			this.target = target;
			adding_squad_lowers_priority = true;
		}

		public override void Execute()
		{
			for (int num = squads.Count - 1; num >= 0; num--)
			{
				Squad squad = squads[num];
				if (target.climbing && squad.def.is_infantry)
				{
					squad.MoveTo(target.climbing_pos, 0f, squad.position.Dist(target.position) < base.ai.def.charge_dist, squad.direction, disengage: false);
				}
				else if (squad.target != target)
				{
					squad.SetStance(Squad.Stance.Defensive);
					squad.Attack(target, squad.position.Dist(target.position) < base.ai.def.charge_dist);
				}
			}
		}

		public override float Priority()
		{
			float num = base_priority;
			num += target.position.Dist(base.cp.position) * distance_bonus;
			if (target.melee_squads != null)
			{
				foreach (Squad melee_squad in target.melee_squads)
				{
					if (!defend_command.squads.Contains(melee_squad))
					{
						num += already_in_melee_bonus;
					}
				}
			}
			return num;
		}

		public override float Priority(Squad squad)
		{
			float num = 0f;
			if (squad.def.CTH_cavalry_mod > 1f && target.def.is_cavalry)
			{
				num -= base.ai.def.anti_cavalry_bonus;
			}
			if (squad.def.is_cavalry)
			{
				num = ((!target.def.is_ranged) ? (num + base.ai.GetThreat(target) * base.ai.def.cavalry_vs_lone_squad_mod) : (num + base.ai.def.cavalry_vs_ranged_bonus));
				if (!squad.CanShoot(target))
				{
					num += base.ai.GetAntiCavalryThreat(target) * base.ai.def.anti_cavalry_bonus;
				}
			}
			else if (!squad.def.is_ranged && target?.enemy_squad?.def != null && target.enemy_squad.def.is_ranged)
			{
				num += base.ai.def.infantry_archer_attacker_bonus;
			}
			num += base.ai.def.speed_mod * squad.def.move_speed / target.def.move_speed * speed_mul;
			num += base.ai.def.health_mod * (1f - squad.simulation.damage) / (1f - target.simulation.damage);
			return num + target.position.Dist(squad.position) * base.ai.def.distance_mod;
		}

		public override bool Validate()
		{
			if (defend_command.enemies_targeting_point.Contains(target))
			{
				return true;
			}
			return false;
		}

		public override bool Validate(Squad squad)
		{
			if (!base.Validate(squad))
			{
				return false;
			}
			if (target.position.SqrDist(squad.position) < target.position.SqrDist(base.cp.position) && (base.ai.IsCastleOpenToEnter() || target.is_inside_walls_or_on_walls == squad.is_inside_walls_or_on_walls || (target.climbing && squad.def.is_infantry)))
			{
				PathData.PassableArea passableArea = default(PathData.PassableArea);
				if (target.position.paID > 0)
				{
					passableArea = target.game.path_finding.data.pointers.GetPA(target.position.paID - 1);
				}
				if (target.climbing && squad.def.is_infantry)
				{
					return true;
				}
				if (!squad.def.is_cavalry || target.position.paID <= 0 || passableArea.IsGround())
				{
					return true;
				}
			}
			return false;
		}
	}

	private class AvoidSubcommand : Subcommand
	{
		public Squad target;

		private float base_priority
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_avoid_base_priority;
				}
				return 39f;
			}
		}

		private float avoid_range
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_avoid_avoid_range;
				}
				return 10f;
			}
		}

		public AvoidSubcommand(DefendCommand defend_command, Squad target)
			: base(defend_command)
		{
			this.target = target;
		}

		public override void Execute()
		{
			PathData.PassableArea passableArea = default(PathData.PassableArea);
			if (target.position.paID > 0)
			{
				passableArea = target.game.path_finding.data.pointers.GetPA(target.position.paID - 1);
			}
			for (int num = squads.Count - 1; num >= 0; num--)
			{
				Squad squad = squads[num];
				bool flag = (!squad.def.is_cavalry || target.position.paID <= 0 || passableArea.IsGround()) && !target.climbing && (base.ai.IsCastleOpenToEnter() || target.is_inside_walls_or_on_walls == squad.is_inside_walls_or_on_walls);
				if (squad.def.is_cavalry && squad.def.type != Unit.Type.Noble && !target.def.is_cavalry && flag)
				{
					squad.SetStance(Squad.Stance.Aggressive);
					squad.Attack(target, squad.position.Dist(target.position) < base.ai.def.charge_dist);
				}
				else if (base.cp.fortification == null)
				{
					PPos pPos = default(PPos);
					int num2 = 0;
					foreach (Squad enemy_shooting_squad in defend_command.enemy_shooting_squads)
					{
						float num3 = enemy_shooting_squad.salvo_def.max_shoot_range + avoid_range - squad.position.Dist(enemy_shooting_squad.position);
						if (num3 > 0f)
						{
							pPos += (squad.position - enemy_shooting_squad.position).GetNormalized() * num3;
							num2++;
						}
					}
					pPos /= (float)num2;
					PPos pt = squad.position + pPos;
					if (base.cp.position.Dist(pt) < base.cp.radius + base.ai.def.defend_additional_radius)
					{
						squad.MoveTo(pt, 0f, double_time: true, -pPos, disengage: false);
					}
					else if (flag)
					{
						squad.SetStance(Squad.Stance.Aggressive);
						squad.Attack(target, squad.position.Dist(target.position) < base.ai.def.charge_dist);
					}
				}
				else if (flag && target.def.is_siege_eq)
				{
					squad.SetStance(Squad.Stance.Aggressive);
					squad.Attack(target, squad.position.Dist(target.position) < base.ai.def.charge_dist);
				}
			}
		}

		public override float Priority()
		{
			return base_priority;
		}

		public override float Priority(Squad squad)
		{
			return 0f;
		}

		public override bool Validate(Squad squad)
		{
			if (!base.Validate(squad))
			{
				return false;
			}
			return true;
		}

		public override bool Validate()
		{
			if (target.salvos_left > 0 && target.position.Dist(base.cp.position) < base.cp.radius + base.ai.def.defend_additional_radius)
			{
				return true;
			}
			return false;
		}
	}

	private class ProtectPointSubcommand : Subcommand
	{
		private float base_priority
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_protect_point_base_priority;
				}
				return 20f;
			}
		}

		private float enemy_capturing_bonus
		{
			get
			{
				if (base.ai != null)
				{
					return base.ai.def.defend_scmd_protect_point_enemy_capturing_bonus;
				}
				return 20f;
			}
		}

		public ProtectPointSubcommand(DefendCommand defend_command)
			: base(defend_command)
		{
		}

		public override void Execute()
		{
			List<PPos> used_positions = new List<PPos>();
			for (int num = squads.Count - 1; num >= 0; num--)
			{
				Squad squad = squads[num];
				PPos pPos = defend_command.target_pos;
				if (base.cp.is_capturing)
				{
					pPos = base.cp.position;
				}
				if (defend_command.force_movement)
				{
					pPos = defend_command.CalculateTargetPosition();
				}
				bool flag = false;
				bool flag2 = false;
				float num2 = 10f;
				for (int i = 0; i < base.ai.enemy_squads.Count; i++)
				{
					Squad squad2 = base.ai.enemy_squads[i];
					if (squad2 == null || !squad2.IsValid() || squad2.IsDefeated() || !squad2.def.is_siege_eq || !squad2.def.is_ranged)
					{
						continue;
					}
					float num3 = squad2.position.Dist(squad.position);
					if (num3 > squad2.salvo_def.min_shoot_range && num3 < squad2.salvo_def.max_shoot_range)
					{
						bool flag3 = defend_command.IsTargetPositionInFortificationRange(squad, num2);
						flag2 = flag3;
						if (!flag3 || (flag3 && squad.position.paID > 0))
						{
							pPos = defend_command.FindPositionCloseToFortification(used_positions, num2);
							flag = true;
							break;
						}
					}
				}
				if (!flag && !squad.def.is_cavalry && squad.def.is_ranged)
				{
					pPos = ((!defend_command.IsTargetPositionInFortificationRange(squad)) ? defend_command.FindDefencePosition() : ((squad.movement.path == null) ? squad.position : squad.movement.path.dst_pt));
				}
				bool flag4 = false;
				if (!flag && base.ai.battle.towers != null)
				{
					for (int j = 0; j < base.ai.battle.towers.Count; j++)
					{
						Fortification fortification = base.ai.battle.towers[j];
						if (fortification.battle_side == base.ai.battle_side || fortification.IsDefeated())
						{
							continue;
						}
						float num4 = fortification.position.Dist(pPos);
						if (num4 < fortification.shoot_comp.salvo_def.max_shoot_range)
						{
							pPos += (pPos - fortification.position).GetNormalized() * (fortification.shoot_comp.salvo_def.max_shoot_range - num4 + 20f);
							if (fortification.position.Dist(squad.position) < fortification.shoot_comp.salvo_def.max_shoot_range)
							{
								flag4 = true;
							}
						}
					}
				}
				bool flag5 = squad.position.Dist(pPos) > base.cp.radius * 4f && (squad.movement.path == null || squad.movement.path.dst_pt.Dist(pPos) > 0.2f);
				if (((defend_command.force_movement || flag5) && !flag2) || flag || flag4)
				{
					squad.SetStance(Squad.Stance.Defensive);
					squad.MoveTo(pPos, 1f, double_time: true, (pPos - squad.position).GetNormalized(), disengage: false);
				}
			}
		}

		public override float Priority()
		{
			float num = base_priority;
			if (defend_command.cp.has_enemy_troops && !defend_command.cp.has_friendly_troops)
			{
				return num += enemy_capturing_bonus;
			}
			return num;
		}

		public override float Priority(Squad squad)
		{
			return 0f;
		}
	}

	private PPos target_pos;

	private List<Squad> enemies_in_range = new List<Squad>();

	private List<Fortification> fortifications_in_range = new List<Fortification>();

	private List<Squad> enemies_targeting_point = new List<Squad>();

	private List<Squad> enemy_shooting_squads = new List<Squad>();

	private float closest_enemy_dist = float.MaxValue;

	private CapturePoint cp;

	private float reposition_timer = 30f;

	private float reposition_cd;

	private bool force_movement;

	private List<Subcommand> subcommands = new List<Subcommand>();

	private Dictionary<Squad, Subcommand> squads_assigments = new Dictionary<Squad, Subcommand>();

	public DefendCommand(BattleAI ai, CapturePoint target)
		: base(ai, target)
	{
		cp = target;
		fortifications_in_range = GetAllWallsAroundPoint(target.position);
		target_pos = CalculateTargetPosition();
		reposition_cd = ai.game.Random(0f, reposition_timer);
		adding_squad_lowers_priority = true;
	}

	private PPos CalculateTargetPosition()
	{
		PPos position = cp.position;
		if (!cp.IsInsideWall || cp.fortifications.Count == 0 || ai.battle.citadel_position == PPos.Zero)
		{
			return position;
		}
		float num = cp.radius;
		PPos normalized = (ai.battle.citadel_position - position).GetNormalized();
		if (enemies_in_range.Count > 0 || closest_enemy_dist < num * 2f + ai.def.defend_additional_radius)
		{
			return position + normalized * (cp.radius * 1.5f);
		}
		int num2 = 15;
		if (num < (float)num2)
		{
			num = num2;
		}
		float min = num * 2f;
		float max = num * 4f;
		float num3 = ai.game.Random(min, max);
		PPos pPos = UnityEngine.Random.insideUnitCircle * cp.radius * 2f;
		return position + normalized * num3 + pPos;
	}

	private List<Fortification> GetAllWallsAroundPoint(PPos position)
	{
		CapturePoint capturePoint = base.target as CapturePoint;
		List<Fortification> list = new List<Fortification>();
		if (ai.battle.fortifications != null)
		{
			for (int i = 0; i < ai.battle.fortifications.Count; i++)
			{
				Fortification fortification = ai.battle.fortifications[i];
				if (fortification.def.type == Fortification.Type.Wall && !fortification.IsDefeated() && capturePoint.position.Dist(fortification.position) < capturePoint.radius + ai.def.defend_additional_radius * 2f)
				{
					list.Add(fortification);
				}
			}
		}
		return list;
	}

	public override float Priority()
	{
		enemies_in_range.Clear();
		enemies_targeting_point.Clear();
		CapturePoint capturePoint = base.target as CapturePoint;
		float importance = capturePoint.GetImportance();
		float num = ai.GetMaxThreat(capturePoint.position, capturePoint.radius + ai.def.defend_additional_radius, 1 - ai.battle_side, interpolated: true);
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 100f)
		{
			num = 100f;
		}
		closest_enemy_dist = float.MaxValue;
		float num2 = 0f;
		float num3 = 1.5f;
		for (int i = 0; i < ai.enemy_squads.Count; i++)
		{
			Squad squad = ai.enemy_squads[i];
			float num4 = squad.Threat();
			float num5 = capturePoint.position.Dist(squad.position);
			if (num4 > num2)
			{
				num2 = num4;
			}
			if (num5 < ai.def.max_attack_dist && (squad.movement.path == null || squad.command == Squad.Command.Fight))
			{
				num5 *= num3;
			}
			if (num5 < closest_enemy_dist)
			{
				closest_enemy_dist = num5;
			}
			if (num5 < capturePoint.radius + ai.def.defend_additional_radius)
			{
				enemies_in_range.Add(squad);
			}
			else
			{
				if (squad.movement.path == null)
				{
					continue;
				}
				bool flag = false;
				for (int j = 0; j < squad.command_queue.Count; j++)
				{
					Squad.MoveCommand moveCommand = squad.command_queue[j];
					float num6 = ((j != squad.command_queue.Count - 1) ? capturePoint.radius : (capturePoint.radius + ai.def.defend_additional_radius));
					if (capturePoint.position.Dist(moveCommand.target_pos) < num6)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					enemies_targeting_point.Add(squad);
				}
			}
		}
		closest_enemy_dist *= ai.def.distance_mod;
		float num7 = ai.def.defend_threat_mod * num + (ai.def.defend_distance_value_base + closest_enemy_dist) + (float)enemies_targeting_point.Count() * ai.def.defend_targeting_point_mod;
		num7 *= importance * ai.def.defend_importance_mod;
		num7 = ((ai.battle_side != 1) ? (num7 * ai.def.defend_attacker_mod) : (num7 * ai.def.defend_defender_mod));
		Fortification fortification = capturePoint.fortification;
		if (fortification != null)
		{
			Fortification.Type type = fortification.def.type;
			if (type == Fortification.Type.Gate)
			{
				num7 = ((ai.enemy_gates != 0 || ai.battle.fortification_destroyed) ? (num7 * ai.def.defend_fortification_mod) : (num7 * ai.def.defend_gate_mod));
			}
			else if (ai.enemy_gates > 0 || ai.battle.fortification_destroyed)
			{
				num7 *= ai.def.defend_fortification_mod;
			}
		}
		else if (ai.battle_side == 1 && ai.enemy_gates == 0 && !ai.battle.fortification_destroyed && capturePoint.def.count_victory)
		{
			num7 *= ai.def.defend_fortification_mod;
		}
		if (capturePoint.def.count_victory)
		{
			num7 += (float)(ai.important_enemy_capture_points - ai.important_my_capture_points) * ai.def.defend_per_cp_diff_bonus;
		}
		if (ai.important_my_capture_points == 1 && capturePoint.def.count_victory)
		{
			num7 *= ai.def.defend_last_cp_mod;
		}
		num7 = ((!capturePoint.def.count_victory) ? (num7 * ai.def.defend_command_mod) : (num7 * ai.def.defend_command_mod_victory));
		num7 -= ai.def.drop_per_squad * (float)squads.Count;
		return Math.Max(0f, num7);
	}

	public override float Priority(Squad defender)
	{
		CapturePoint capturePoint = base.target as CapturePoint;
		float num = 0f;
		num += capturePoint.position.Dist(defender.position) * ai.def.distance_mod;
		num += ai.def.defend_speed_mod * defender.def.run_anim_speed;
		num += ai.def.health_mod * (1f - defender.simulation.damage);
		if (defender.def.type == Unit.Type.Noble)
		{
			num += ai.def.defend_noble_bonus;
		}
		if (defender.def.is_ranged)
		{
			num += ai.def.defend_archer_bonus;
		}
		return num;
	}

	public override bool Validate()
	{
		if (!(base.target is CapturePoint capturePoint) || !capturePoint.IsValid() || capturePoint.battle_side != ai.battle_side || (capturePoint.fortification != null && capturePoint.fortification.IsDefeated()))
		{
			return false;
		}
		return base.Validate();
	}

	public override bool Validate(Squad squad)
	{
		if (!IsSquadValidToDefend(squad))
		{
			return false;
		}
		float num = squad.position.Dist(base.target.position);
		float defend_max_dist = ai.def.defend_max_dist;
		if (defend_max_dist > 0f && squads.Count > 0 && num > defend_max_dist && ai.important_my_capture_points > 1)
		{
			return false;
		}
		return base.Validate(squad);
	}

	private bool IsSquadValidToDefend(Squad squad)
	{
		if (squad.def.is_cavalry && ai.my_gates == 0 && !ai.battle.fortification_destroyed)
		{
			CapturePoint capturePoint = base.target as CapturePoint;
			if (!squad.is_inside_walls_or_on_walls && capturePoint.IsInsideWall)
			{
				return false;
			}
		}
		if (squad.def.is_siege_eq)
		{
			return false;
		}
		return true;
	}

	public static bool ValidateTarget(CapturePoint target, int battle_side)
	{
		if (target == null || !target.IsValid() || target.battle_side != battle_side || (target.fortification != null && target.fortification.IsDefeated()))
		{
			return false;
		}
		return true;
	}

	public override bool AddSquad(Squad squad)
	{
		if ((float)squads.Count >= (float)ai.def.defend_base_squads_ammount + (float)enemies_in_range.Count * ai.def.defend_per_enemy_in_range_amount_bonus + (float)enemies_targeting_point.Count() * ai.def.defend_per_targeting_enemy_amount_bonus)
		{
			return false;
		}
		if (base.AddSquad(squad))
		{
			return true;
		}
		return false;
	}

	public override void RemoveSquad(Squad squad)
	{
		base.RemoveSquad(squad);
		GetSquadAssigment(squad)?.RemoveSquad(squad);
	}

	public override void RemoveSquad(int i)
	{
		Squad squad = squads[i];
		GetSquadAssigment(squad)?.RemoveSquad(squad);
		base.RemoveSquad(i);
	}

	public override void Execute()
	{
		base.Execute();
		if (squads.Count != 0 && !(target_pos.x <= 0f) && !(target_pos.y <= 0f) && !(target_pos.x >= (float)ai.game.path_finding.data.width) && !(target_pos.y >= (float)ai.game.path_finding.data.height))
		{
			if (reposition_cd < reposition_timer)
			{
				reposition_cd += ai.def.update_interval;
			}
			else
			{
				target_pos = CalculateTargetPosition();
				reposition_cd = 0f;
				force_movement = true;
			}
			CreateSubcommands();
			CalculateSubcommandsPiorities();
			SortSubcommands();
			AssignSquadsToSubcommands();
			ExecuteSubcommands();
		}
	}

	private void CreateSubcommands()
	{
		for (int num = subcommands.Count - 1; num >= 0; num--)
		{
			Subcommand subcommand = subcommands[num];
			if (!subcommand.Validate())
			{
				subcommand.Reset();
				subcommands.RemoveAt(num);
			}
		}
		foreach (Squad item5 in enemies_in_range)
		{
			if (GetAttackSubcommand(item5) == null)
			{
				AttackSubcommand item = new AttackSubcommand(this, item5);
				subcommands.Add(item);
			}
		}
		foreach (Squad item6 in enemies_targeting_point)
		{
			if (GetInterceptSubcommand(item6) == null)
			{
				InterceptSubcommand item2 = new InterceptSubcommand(this, item6);
				subcommands.Add(item2);
			}
		}
		enemy_shooting_squads = new List<Squad>();
		foreach (Squad squad in squads)
		{
			foreach (Squad enemy_shooting_squad in squad.enemy_shooting_squads)
			{
				if (!enemy_shooting_squads.Contains(enemy_shooting_squad))
				{
					enemy_shooting_squads.Add(enemy_shooting_squad);
				}
			}
		}
		foreach (Squad enemy_shooting_squad2 in enemy_shooting_squads)
		{
			if (GetAvoidSubcommand(enemy_shooting_squad2) == null)
			{
				AvoidSubcommand item3 = new AvoidSubcommand(this, enemy_shooting_squad2);
				subcommands.Add(item3);
			}
		}
		if (GetProtectPointSubcommand() == null)
		{
			ProtectPointSubcommand item4 = new ProtectPointSubcommand(this);
			subcommands.Add(item4);
		}
	}

	private AttackSubcommand GetAttackSubcommand(Squad target)
	{
		foreach (Subcommand subcommand in subcommands)
		{
			if (subcommand is AttackSubcommand attackSubcommand && attackSubcommand.target == target)
			{
				return attackSubcommand;
			}
		}
		return null;
	}

	private InterceptSubcommand GetInterceptSubcommand(Squad target)
	{
		foreach (Subcommand subcommand in subcommands)
		{
			if (subcommand is InterceptSubcommand interceptSubcommand && interceptSubcommand.target == target)
			{
				return interceptSubcommand;
			}
		}
		return null;
	}

	private AvoidSubcommand GetAvoidSubcommand(Squad target)
	{
		foreach (Subcommand subcommand in subcommands)
		{
			if (subcommand is AvoidSubcommand avoidSubcommand && avoidSubcommand.target == target)
			{
				return avoidSubcommand;
			}
		}
		return null;
	}

	private ProtectPointSubcommand GetProtectPointSubcommand()
	{
		foreach (Subcommand subcommand in subcommands)
		{
			if (subcommand is ProtectPointSubcommand result)
			{
				return result;
			}
		}
		return null;
	}

	private void CalculateSubcommandsPiorities()
	{
		foreach (Subcommand subcommand in subcommands)
		{
			float num = subcommand.Priority();
			subcommand.cached_priority = num;
		}
	}

	private void AssignSquadsToSubcommands()
	{
		List<Squad> list = new List<Squad>(squads);
		int num = 0;
		int num2 = 0;
		while (num2 < subcommands.Count && list.Count > 0)
		{
			Subcommand scmd = subcommands[num2];
			for (int num3 = scmd.squads.Count - 1; num3 >= 0; num3--)
			{
				Squad squad = scmd.squads[num3];
				if (squad == null || !squad.IsValid() || squad.IsDefeated())
				{
					scmd.RemoveSquad(squad);
				}
			}
			list.Sort((Squad x, Squad y) => scmd.Priority(x).CompareTo(scmd.Priority(y)));
			bool flag = false;
			int num4 = num;
			for (int num5 = list.Count - 1 - num4; num5 >= 0; num5--)
			{
				Squad squad2 = list[num5];
				Subcommand squadAssigment = GetSquadAssigment(squad2);
				bool flag2 = squadAssigment == scmd;
				bool flag3 = squadAssigment?.adding_squad_lowers_priority ?? false;
				if (scmd.AddSquad(squad2))
				{
					if (!flag2 && flag3)
					{
						squadAssigment.cached_priority += ai.def.drop_per_squad;
						flag = true;
					}
					if (!flag2 && scmd.adding_squad_lowers_priority)
					{
						scmd.cached_priority -= ai.def.drop_per_squad;
						flag = true;
					}
				}
				else if (flag2 && flag3)
				{
					squadAssigment.cached_priority += ai.def.drop_per_squad;
					flag = true;
				}
				if (flag)
				{
					SortSubcommands();
					break;
				}
			}
			if (flag)
			{
				num++;
				continue;
			}
			num = 0;
			num2++;
		}
	}

	private void ExecuteSubcommands()
	{
		foreach (Subcommand subcommand in subcommands)
		{
			subcommand.Execute();
		}
	}

	private void SortSubcommands()
	{
		subcommands = subcommands.OrderByDescending((Subcommand x) => x.cached_priority).ToList();
	}

	private void RemoveSquadAssigment(Squad squad)
	{
		if (squads_assigments.ContainsKey(squad))
		{
			squads_assigments[squad].RemoveSquad(squad, remove_from_assigment_dict: false);
			squads_assigments.Remove(squad);
		}
	}

	private void ChangeSquadAssigment(Squad squad, Subcommand subcommand)
	{
		if (subcommand == null)
		{
			RemoveSquadAssigment(squad);
		}
		if (squads_assigments.ContainsKey(squad))
		{
			squads_assigments[squad].RemoveSquad(squad, remove_from_assigment_dict: false);
			squads_assigments[squad] = subcommand;
		}
		else
		{
			squads_assigments.Add(squad, subcommand);
		}
	}

	private Subcommand GetSquadAssigment(Squad squad)
	{
		if (squads_assigments.ContainsKey(squad))
		{
			return squads_assigments[squad];
		}
		return null;
	}

	private PPos FindPositionCloseToFortification(List<PPos> used_positions, float offset_from_fortification)
	{
		int num = 10;
		int num2 = 0;
		PPos pPos;
		do
		{
			pPos = FindDefencePosition();
			PPos normalized = (ai.battle.citadel_position - target_pos).GetNormalized();
			pPos += normalized * offset_from_fortification;
			pPos.paID = 0;
			num2++;
		}
		while (used_positions.Contains(pPos) && num2 < num);
		if (!used_positions.Contains(pPos))
		{
			used_positions.Add(pPos);
		}
		return pPos;
	}

	private bool IsTargetPositionInFortificationRange(Squad squad, float additional_radius = 0f)
	{
		if (squad?.formation == null)
		{
			return false;
		}
		if (fortifications_in_range == null)
		{
			return false;
		}
		PPos pPos = squad.position;
		if (squad?.movement?.path != null)
		{
			pPos = squad.movement.path.dst_pt;
		}
		for (int i = 0; i < fortifications_in_range.Count; i++)
		{
			Fortification fortification = fortifications_in_range[i];
			if (fortification != null && pPos.Dist(fortification.position) < squad.formation.cur_radius + additional_radius)
			{
				return true;
			}
		}
		return false;
	}

	private PPos FindDefencePosition()
	{
		PPos position = (base.target as CapturePoint).position;
		if (fortifications_in_range != null && fortifications_in_range.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, fortifications_in_range.Count);
			Fortification fortification = fortifications_in_range[index];
			int num = 0;
			int num2 = 10;
			while (fortification.IsDefeated() && num < num2)
			{
				index = UnityEngine.Random.Range(0, fortifications_in_range.Count);
				fortification = fortifications_in_range[index];
				num++;
			}
			for (int i = 0; i < fortification.paids.Count; i++)
			{
				if (fortification.game == null)
				{
					break;
				}
				if (fortification.game.path_finding == null)
				{
					break;
				}
				PathData.PassableArea passableArea = default(PathData.PassableArea);
				if (fortification.paids[i] > 0)
				{
					passableArea = fortification.game.path_finding.data.pointers.GetPA(fortification.paids[i] - 1);
				}
				if (passableArea.enabled)
				{
					position.x = fortification.position.x;
					position.y = fortification.position.y;
					position.paID = fortification.paids[i];
					break;
				}
			}
		}
		return position;
	}
}
