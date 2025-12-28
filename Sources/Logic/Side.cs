using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public class KeepFormationCommand : SquadCommand
{
	private struct Threats
	{
		public enum Side
		{
			None,
			Front,
			Back,
			Right,
			Left
		}

		public readonly float front;

		public readonly float back;

		public readonly float right;

		public readonly float left;

		public Threats(float front, float back, float right, float left)
		{
			this.front = front;
			this.back = back;
			this.right = right;
			this.left = left;
		}

		public Side GetMostNumerousSide(Side ignored_side = Side.None)
		{
			if (ignored_side != Side.Front && (front >= back || ignored_side == Side.Back) && (front >= right || ignored_side == Side.Right) && (front >= left || ignored_side == Side.Left))
			{
				return Side.Front;
			}
			if (ignored_side != Side.Right && (right >= back || ignored_side == Side.Back) && (right >= front || ignored_side == Side.Front) && (right >= left || ignored_side == Side.Left))
			{
				return Side.Right;
			}
			if (ignored_side != Side.Left && (left >= back || ignored_side == Side.Back) && (left >= front || ignored_side == Side.Front) && (left >= right || ignored_side == Side.Right))
			{
				return Side.Left;
			}
			if (ignored_side != Side.Back && (back >= left || ignored_side == Side.Left) && (back >= front || ignored_side == Side.Front) && (back >= right || ignored_side == Side.Right))
			{
				return Side.Back;
			}
			return Side.Front;
		}
	}

	private const float MAX_THREAT_DIST = 450f;

	private const float LOW_THREAT_DIST = 300f;

	private const float HIGH_TREAT_DIST = 150f;

	private const float LOW_THREAT_MUL = 0.5f;

	private const float HIGH_TREAT_MUL = 2f;

	private const float FORMATION_MOVE_DELTA = 25f;

	public Dictionary<Squad, PPos> offsets;

	public Dictionary<Squad, float> paths_completeness = new Dictionary<Squad, float>();

	private Squad squad_to_follow;

	private Kingdom kingdom;

	private bool done;

	public PPos target_pos;

	private bool is_outside_wall;

	private float heading;

	private StandardArmyFormation formation;

	private bool formation_valid;

	private bool first_execute;

	private bool defensive_mode;

	public float medium_completeness;

	public float min_speed;

	private Squad slowest_squad;

	private bool main;

	private float range;

	private int squads_limit;

	private bool can_split;

	private bool merge_with_main;

	private float next_checkpoint_time;

	private bool calc_next_checkpoint_time;

	private bool first_checkpoint;

	private bool avoid_enemies;

	private bool is_on_defensive_position;

	private bool check_distance_to_towers;

	public MapObject owner { get; private set; }

	public float Range => range;

	public bool Merge_with_main => merge_with_main;

	private float GetDistanceToFormation(Squad squad)
	{
		if (squad_to_follow != null)
		{
			return squad.position.Dist(squad_to_follow.position);
		}
		return squad.position.Dist(target_pos);
	}

	public Squad GetCommander()
	{
		return squad_to_follow;
	}

	public Squad GetFormationCommander()
	{
		return ArmyFormation.FindCommander(squads);
	}

	public bool IsMain()
	{
		return main;
	}

	public Kingdom GetKingdom()
	{
		return kingdom;
	}

	public KeepFormationCommand(BattleAI ai, MapObject owner, MapObject target, Squad squad_to_follow, StandardArmyFormation army_formation, Kingdom kingdom, bool defensive_mode = false, float range = 150f, bool main = false, bool merge_with_main = false, int squads_limit = -1, bool avoid_enemies = false)
		: base(ai, target)
	{
		this.owner = owner;
		this.squad_to_follow = squad_to_follow;
		formation = army_formation;
		this.defensive_mode = defensive_mode;
		this.kingdom = kingdom;
		this.main = main;
		this.range = range;
		this.merge_with_main = merge_with_main;
		this.squads_limit = squads_limit;
		this.avoid_enemies = avoid_enemies;
		ChooseFirstTargetPos();
	}

	public override float Priority()
	{
		if (ai.battle_side == 0)
		{
			return ai.def.keep_formation_attacker_mod;
		}
		return ai.def.keep_formation_defender_mod;
	}

	private bool IsFighting()
	{
		if (ai?.groups == null || ai.groups.Count == 0 || squad_to_follow == null)
		{
			return false;
		}
		if (is_on_defensive_position)
		{
			return false;
		}
		SquadGroup squadGroup = null;
		for (int i = 0; i < ai.groups.Count; i++)
		{
			SquadGroup squadGroup2 = ai.groups[i];
			if (squadGroup2.ContainsSquad(squad_to_follow))
			{
				squadGroup = squadGroup2;
				break;
			}
		}
		if (squadGroup != null)
		{
			for (int j = 0; j < squadGroup.squads.Count; j++)
			{
				if (squadGroup.squads[j].is_fighting)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool Validate()
	{
		if (squad_to_follow == null || !squad_to_follow.IsValid() || squad_to_follow.IsDefeated() || squad_to_follow.simulation.state >= BattleSimulation.Squad.State.Retreating)
		{
			return false;
		}
		if (base.target != null && !base.target.IsValid())
		{
			return false;
		}
		if (squad_to_follow.def.is_siege_eq && ai.battle.fortification_destroyed)
		{
			return false;
		}
		return base.Validate();
	}

	public override bool Validate(Squad squad)
	{
		if (IsFighting())
		{
			return false;
		}
		bool flag = kingdom == squad.GetKingdom();
		if (squad?.simulation?.garrison?.settlement != null)
		{
			flag &= owner == squad.simulation.garrison.settlement;
		}
		else if (squad?.simulation?.army != null && squad.simulation.army.act_separately)
		{
			flag &= owner == squad.simulation.army;
		}
		if (offsets == null || !offsets.ContainsKey(squad) || offsets[squad].Length() < range)
		{
			flag = ((squad_to_follow == null) ? (flag & (squad.position.Dist(target_pos) < range)) : (flag & (squad.position.Dist(squad_to_follow.position) < range)));
		}
		flag &= !squad.def.is_siege_eq;
		return base.Validate(squad) && flag;
	}

	public bool ValidateCount()
	{
		if (squads_limit < 0)
		{
			return true;
		}
		if (squads.Count < squads_limit)
		{
			return true;
		}
		return false;
	}

	public override bool AddSquad(Squad squad)
	{
		if (!ValidateCount())
		{
			return false;
		}
		return base.AddSquad(squad);
	}

	protected override bool MaintainDecision(Squad squad)
	{
		if (squad.ai_command is KeepFormationCommand)
		{
			KeepFormationCommand keepFormationCommand = (KeepFormationCommand)squad.ai_command;
			if (!keepFormationCommand.can_split)
			{
				return true;
			}
			return keepFormationCommand.GetDistanceToFormation(squad) < GetDistanceToFormation(squad);
		}
		return base.MaintainDecision(squad);
	}

	public override void Execute()
	{
		base.Execute();
		if (squads.Count != 0)
		{
			CheckFormation();
			if (calc_next_checkpoint_time && formation_valid && !first_execute)
			{
				CalcCheckpointTime();
			}
			if (!formation_valid)
			{
				CreateFormation();
			}
			CalcMediumPathCompleteness();
			CheckCompletion();
			if (done)
			{
				ChooseNewTargetPos();
			}
			OrderSquads();
			if (first_execute)
			{
				first_execute = false;
			}
		}
	}

	private void CheckCompletion()
	{
		bool flag = false;
		if (done)
		{
			done = false;
			flag = true;
		}
		else
		{
			done = true;
		}
		if (!first_execute && base.target == null && !squads.Contains(squad_to_follow) && squad_to_follow.direction.Heading() != heading && !done)
		{
			float num = Mathf.Abs(Angle.Diff(squad_to_follow.direction.Heading(), heading));
			done = num > 30f;
		}
		if (!first_execute && !done && !flag && next_checkpoint_time <= ai.game.time_f)
		{
			done = true;
		}
		int num2 = (is_outside_wall ? Mathf.Min(1, squads.Count - 1) : Mathf.Min(2, squads.Count - 1));
		int num3 = 0;
		float num4 = (is_outside_wall ? 0.7f : 0.5f);
		float num5 = (is_outside_wall ? 0.8f : 0.7f);
		float num6 = (is_outside_wall ? 10f : 15f);
		foreach (Squad squad in squads)
		{
			if (first_execute || squad.movement.pf_path != null || (squad.movement.path != null && !(squad.movement.path.path_len - squad.movement.path.t < num6) && (!paths_completeness.ContainsKey(squad) || !(paths_completeness[squad] > num5))))
			{
				if (num3 >= num2 || !paths_completeness.ContainsKey(squad) || !(paths_completeness[squad] > num4))
				{
					done = false;
					break;
				}
				num3++;
			}
		}
	}

	private void OrderSquads()
	{
		foreach (Squad squad in squads)
		{
			float num = CalcSpeed(squad);
			bool flag = num > squad.normal_move_speed;
			if (offsets.ContainsKey(squad))
			{
				PPos pPos = offsets[squad];
				PPos pPos2 = target_pos + pPos.GetRotated(0f - heading);
				if (ai.battle.IsOutsideWall(pPos2) != is_outside_wall)
				{
					pPos2 = target_pos;
				}
				squad.can_avoid = false;
				if (squad.position.Dist(pPos2) > 2f && (squad.movement.path == null || !(squad.movement.path.dst_pt.Dist(pPos2) < 0.1f)))
				{
					squad.MoveTo(pPos2, 0f, flag, PPos.UnitRight.GetRotated(0f - heading), disengage: false, defeated_move: false, avoiding_move: false, returning_move: false, ignore_reserve: false, num);
				}
				else if (num != squad.movement.speed)
				{
					squad.SetMoveSpeed(num);
					if (flag)
					{
						squad.SetDoubleTime(value: true);
					}
					else
					{
						squad.SetDoubleTime(value: false);
					}
				}
			}
			else
			{
				Debug.LogError("Offsets count = " + offsets.Count + ", squads count = " + squads.Count + ", squad = " + squad);
			}
		}
	}

	private void ChooseFirstTargetPos()
	{
		first_execute = true;
		calc_next_checkpoint_time = true;
		first_checkpoint = true;
		if (ai.IsSiege() && IsMain())
		{
			if (ai.battle_side == 1 && base.target != null && squad_to_follow.is_inside_walls)
			{
				target_pos = FindPositionInFrontOfGate(ref heading, base.target.position);
				is_outside_wall = ai.battle.IsOutsideWall(target_pos);
				return;
			}
			if (base.target == null && ai.battle_side == 0 && ai.battle.type == Battle.Type.BreakSiege)
			{
				InitTargetPosIfNoTarget();
				PPos position = FindBreakSiegeDefendingPosition(ref heading);
				target_pos = AdjustTargetPosition(position, ref heading);
				is_outside_wall = ai.battle.IsOutsideWall(target_pos);
				if (owner is Army army && !ai.battle.IsReinforcement(army))
				{
					is_on_defensive_position = true;
				}
				return;
			}
		}
		if (base.target == null)
		{
			InitTargetPosIfNoTarget();
			return;
		}
		if (!defensive_mode)
		{
			PPos pPos = default(PPos);
			if (formation.formation_squads.commander != null && formation.offsets.ContainsKey(formation.formation_squads.commander))
			{
				pPos = formation.offsets[formation.formation_squads.commander];
			}
			PPos pPos2 = base.target.position - squad_to_follow.position;
			heading = pPos2.Heading();
			target_pos = squad_to_follow.position + pPos2.GetNormalized() * 20f - pPos.GetRotated(0f - heading);
		}
		else
		{
			PPos pPos3 = default(PPos);
			for (int i = 0; i < ai.my_capture_points.Count; i++)
			{
				CapturePoint capturePoint = ai.my_capture_points[i];
				pPos3 += capturePoint.position;
			}
			pPos3 /= (float)ai.my_capture_points.Count;
			PPos pPos4 = base.target.position - squad_to_follow.position;
			heading = pPos4.Heading();
			target_pos = pPos3 + pPos4.GetNormalized() * 40f;
		}
		is_outside_wall = ai.battle.IsOutsideWall(target_pos);
	}

	private void InitTargetPosIfNoTarget()
	{
		if (squad_to_follow.movement.path != null && squad_to_follow.movement.path.dst_pt != squad_to_follow.position)
		{
			heading = (squad_to_follow.movement.path.dst_pt - squad_to_follow.position).Heading();
		}
		else
		{
			heading = squad_to_follow.direction.Heading();
		}
		target_pos = squad_to_follow.position;
		is_outside_wall = ai.battle.IsOutsideWall(target_pos);
	}

	private void ChooseNewTargetPos()
	{
		calc_next_checkpoint_time = true;
		first_checkpoint = false;
		formation_valid = false;
		ai.OnKeepFormationCheckpoint();
		PPos pPos = default(PPos);
		if (base.target == null)
		{
			if (IsMain() && ai.IsSiege() && ai.battle_side == 0 && ai.battle.type == Battle.Type.BreakSiege)
			{
				float num = heading;
				float num2 = heading;
				PPos pPos2 = FindBreakSiegeDefendingPosition(ref num2);
				Threats threats = AnalyzeThreats(pPos2, num2);
				bool flag = false;
				bool flag2 = false;
				if (formation.settings.one_sided)
				{
					flag = !formation.settings.ignore_right && threats.front > 0.5f * threats.right && threats.left > 0.5f * threats.right;
					flag2 = formation.settings.ignore_right && threats.front > 0.5f * threats.left && threats.right > 0.5f * threats.left;
				}
				Threats.Side mostNumerousSide = threats.GetMostNumerousSide(flag ? Threats.Side.Right : (flag2 ? Threats.Side.Left : Threats.Side.None));
				if (mostNumerousSide == Threats.Side.Right)
				{
					PPos rotated = PPos.UnitRight.GetRotated(0f - num2);
					num2 -= 30f;
					PPos rotated2 = PPos.UnitRight.GetRotated(0f - num2);
					pPos2 = GetAverageCampsPositions();
					pPos2 += rotated2 * 120f - rotated * 80f;
				}
				if (mostNumerousSide == Threats.Side.Left)
				{
					PPos rotated3 = PPos.UnitRight.GetRotated(0f - num2);
					num2 += 30f;
					PPos rotated4 = PPos.UnitRight.GetRotated(0f - num2);
					pPos2 = GetAverageCampsPositions();
					pPos2 += rotated4 * 120f - rotated3 * 80f;
				}
				PPos pPos3 = pPos2;
				if (!is_on_defensive_position)
				{
					pPos3 = AdjustTargetPosition(pPos3, ref num);
				}
				if (pPos3.Dist(pPos2) > 30f)
				{
					pPos3 = MovePositionAwayFromTowers(pPos3, ref num2, 0f, ai.def.keep_formation_additional_safe_distance);
					heading = num;
				}
				else
				{
					heading = num2;
					if (!is_on_defensive_position)
					{
						PPos unitRight = PPos.UnitRight;
						unitRight.GetRotated(heading);
						PPos pt = new PPos(0f - unitRight.y, unitRight.x);
						if (target_pos != pPos2)
						{
							if ((target_pos - pPos2).Dot(pt) > 0f)
							{
								formation.settings.ignore_right = true;
							}
							else
							{
								formation.settings.ignore_right = false;
							}
						}
						is_on_defensive_position = true;
						formation.settings.one_sided = true;
						ai.OnSupportersJoinedMainArmy(!formation.settings.ignore_right);
					}
				}
				target_pos = pPos3;
				is_outside_wall = ai.battle.IsOutsideWall(target_pos);
				if (!IsInSafePosition(out var _, out var min_safe_dist))
				{
					PPos pPos4 = PPos.UnitRight.GetRotated(0f - heading) * Mathf.Max(min_safe_dist, 3f);
					target_pos -= pPos4;
					is_outside_wall = ai.battle.IsOutsideWall(target_pos);
				}
			}
			else
			{
				if (squad_to_follow.movement.path != null && squad_to_follow.movement.path.dst_pt != squad_to_follow.position)
				{
					pPos = squad_to_follow.movement.path.dst_pt - squad_to_follow.position;
					heading = pPos.Heading();
				}
				else
				{
					heading = squad_to_follow.direction.Heading();
				}
				float num3 = pPos.Length();
				float value = 50f;
				if (num3 > 50f)
				{
					value = Mathf.Clamp(value, 40f, 60f);
					target_pos = squad_to_follow.position + pPos.GetNormalized() * value;
					is_outside_wall = ai.battle.IsOutsideWall(target_pos);
				}
			}
		}
		else
		{
			if (defensive_mode)
			{
				return;
			}
			_ = is_outside_wall;
			PPos pPos5 = target_pos;
			if (base.target != null && base.target is CapturePoint && (base.target as CapturePoint).battle_side == ai.battle_side && ai.enemy_capture_points.Count > 0)
			{
				base.target = ai.enemy_capture_points[0];
			}
			PPos pPos6 = base.target.position;
			Squad squad = base.target as Squad;
			bool flip_direction = false;
			if (ai.IsSiege() && ai.battle_side == 1 && base.target != null)
			{
				if (is_outside_wall && WillGoThroughCity())
				{
					PPos pPos7 = FindPositionInFrontOfGate(ref heading, squad_to_follow.position, look_towards_citadel: true, 30f);
					if (pPos5.Dist(pPos7) > 60f)
					{
						target_pos = AdjustTargetPosition(pPos7, ref heading, squad, flip_direction);
						is_outside_wall = ai.battle.IsOutsideWall(target_pos);
						return;
					}
				}
				else if (!is_outside_wall)
				{
					PPos position = FindPositionInFrontOfGate(ref heading, base.target.position, look_towards_citadel: true);
					target_pos = AdjustTargetPosition(position, ref heading, squad, flip_direction);
					is_outside_wall = ai.battle.IsOutsideWall(target_pos);
					return;
				}
			}
			if (squad != null && squad.battle_side == squad_to_follow.battle_side)
			{
				float num4 = 40f;
				PPos pPos8 = pPos6 - squad.direction.GetNormalized() * num4;
				Squad commander = ai.GetCommander(1 - squad_to_follow.battle_side);
				if (commander != null)
				{
					float num5 = commander.position.Dist(pPos8);
					float num6 = commander.position.Dist(base.target.position);
					if (num5 < num6)
					{
						pPos8 = pPos6 + squad.direction.GetNormalized() * num4;
						flip_direction = true;
					}
				}
				pPos6 = pPos8;
			}
			pPos6 = AdjustTargetPosition(pPos6, ref heading, squad, flip_direction);
			if (avoid_enemies)
			{
				pPos6 = MovePositionAwayFromEnemies(pPos6, ref heading);
			}
			if (ai.IsSiege() && ai.battle_side == 0 && ai.battle.type == Battle.Type.BreakSiege)
			{
				float num7 = heading;
				pPos6 = MovePositionAwayFromTowers(pPos6, ref num7, 0f, ai.def.keep_formation_additional_safe_distance);
			}
			target_pos = pPos6;
			is_outside_wall = ai.battle.IsOutsideWall(target_pos);
		}
	}

	private bool WillGoThroughCity()
	{
		if (base.target != null && ai.battle != null)
		{
			if (!ai.battle.IsOutsideWall(target_pos) || !ai.battle.IsOutsideWall(base.target.position))
			{
				return true;
			}
			PPos pPos = base.target.position - target_pos;
			PPos pPos2 = pPos.GetNormalized() * 25f;
			for (int num = (int)(pPos.Length() / 25f); num > 0; num--)
			{
				PPos pPos3 = target_pos + pPos2 * num;
				if (!ai.battle.IsOutsideWall(pPos3))
				{
					return true;
				}
			}
		}
		return false;
	}

	private PPos FindBreakSiegeDefendingPosition(ref float heading)
	{
		PPos averageCampsPositions = GetAverageCampsPositions();
		return MovePositionAwayFromTowers(averageCampsPositions, ref heading, 50f);
	}

	private PPos FindPositionInFrontOfGate(ref float heading, PPos ref_pos, bool look_towards_citadel = false, float offset = 50f)
	{
		PPos result = base.target.position;
		if (ai.my_gates > 0)
		{
			CapturePoint capturePoint = null;
			float num = float.MaxValue;
			for (int i = 0; i < ai.my_capture_points.Count; i++)
			{
				CapturePoint capturePoint2 = ai.my_capture_points[i];
				if (capturePoint2.fortification != null && capturePoint2.fortification.def.type == Fortification.Type.Gate)
				{
					float num2 = ref_pos.Dist(capturePoint2.position);
					if (num2 < num)
					{
						capturePoint = capturePoint2;
						num = num2;
					}
				}
			}
			if (capturePoint != null)
			{
				PPos pPos = capturePoint.position - squad_to_follow.position;
				if (ai.battle.citadel_position != PPos.Zero)
				{
					pPos = capturePoint.position - ai.battle.citadel_position;
				}
				heading = (look_towards_citadel ? (0f - pPos.Heading()) : pPos.Heading());
				result = capturePoint.position + pPos.GetNormalized() * offset;
			}
		}
		return result;
	}

	private PPos GetAverageCampsPositions()
	{
		PPos result = default(PPos);
		if (ai.my_capture_points.Count == 0)
		{
			result = squad_to_follow.position;
		}
		for (int i = 0; i < ai.my_capture_points.Count; i++)
		{
			CapturePoint capturePoint = ai.my_capture_points[i];
			result += capturePoint.position;
		}
		if (ai.my_capture_points.Count > 0)
		{
			result /= (float)ai.my_capture_points.Count;
		}
		return result;
	}

	private Threats AnalyzeThreats(PPos reference_point, float reference_heading)
	{
		PPos rotated = PPos.UnitRight.GetRotated(0f - reference_heading);
		PPos pPos = new PPos(rotated.y, 0f - rotated.x);
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		if (ai != null && ai.enemy_groups != null)
		{
			foreach (SquadGroup enemy_group in ai.enemy_groups)
			{
				PPos pPos2 = enemy_group.GetAveragePosition() - reference_point;
				if (pPos2.SqrLength() < 202500f)
				{
					float num5 = 1f;
					num5 = ((pPos2.SqrLength() < 22500f) ? 2f : num5);
					num5 = ((pPos2.SqrLength() > 90000f) ? 0.5f : num5);
					pPos2 = pPos2.GetNormalized();
					if ((double)pPos2.Dot(rotated) >= 0.866)
					{
						num += (float)enemy_group.squads.Count * num5;
					}
					else if ((double)pPos2.Dot(pPos) > 0.5)
					{
						num3 += (float)enemy_group.squads.Count * num5;
					}
					else if ((double)pPos2.Dot(-pPos) > 0.5)
					{
						num4 += (float)enemy_group.squads.Count;
					}
					else
					{
						num2 += (float)enemy_group.squads.Count;
					}
				}
			}
		}
		return new Threats(num, num2, num3, num4);
	}

	private PPos MovePositionAwayFromTowers(PPos pos, ref float heading, float offset_mul = 0f, float additional_offset = 0f)
	{
		PPos pPos = ai.battle.citadel_position - pos;
		heading = pPos.Heading();
		PPos pPos2 = pos + pPos.GetNormalized() * offset_mul;
		PPos pPos3 = pPos2 + pPos.GetNormalized() * 50f;
		if (ai.GetThreat(pPos3, 1 - ai.battle_side, interpolated: true) > 0f && ai.battle.fortifications != null)
		{
			float num = 0f;
			PPos pPos4 = pPos3 + pPos.Right().GetNormalized() * 50f;
			PPos pPos5 = pPos3 - pPos.Right().GetNormalized() * 50f;
			for (int i = 0; i < ai.battle.fortifications.Count; i++)
			{
				Fortification fortification = ai.battle.fortifications[i];
				if (fortification.IsDefeated() || fortification.battle_side == ai.battle_side || fortification.shoot_comp == null)
				{
					continue;
				}
				float num2 = pPos3.Dist(fortification.position);
				float num3 = pPos4.Dist(fortification.position);
				float num4 = pPos5.Dist(fortification.position);
				float num5 = Mathf.Min(num2, num3, num4);
				if (num5 < fortification.shoot_comp.salvo_def.max_shoot_range + additional_offset)
				{
					float num6 = fortification.shoot_comp.salvo_def.max_shoot_range - num5 + additional_offset;
					if (num6 > num)
					{
						num = num6;
					}
				}
			}
			if (num > 0f)
			{
				pPos2 -= pPos.GetNormalized() * (num + 1f);
			}
		}
		return pPos2;
	}

	private PPos MovePositionAwayFromEnemies(PPos pos, ref float heading)
	{
		if (ai != null && ai.enemy_groups != null && ai.enemy_groups.Count > 0 && squad_to_follow != null)
		{
			SquadGroup squadGroup = ai.enemy_groups[0];
			PPos pPos = pos - ai.enemy_groups[0].GetAveragePosition();
			float num = 3f * ai.def.max_attack_dist;
			for (int i = 1; i < ai.enemy_groups.Count; i++)
			{
				SquadGroup squadGroup2 = ai.enemy_groups[i];
				PPos pPos2 = pos - squadGroup2.GetAveragePosition();
				if (pPos2.SqrLength() < pPos.SqrLength())
				{
					squadGroup = squadGroup2;
					pPos = pPos2;
				}
			}
			if (squadGroup.GetAveragePosition().Dist(pos) > num)
			{
				return pos;
			}
			PPos pPos3 = target_pos - pos;
			PPos normalized = new PPos(0f - pPos3.y, pPos3.x).GetNormalized();
			PPos pPos4 = 0.75f * ((num - pPos.Length()) * pPos.GetNormalized()).Project(normalized);
			if (pPos4.Length() > 50f)
			{
				pPos4 = pPos4.GetNormalized() * 50f;
			}
			PPos pPos5 = pos + pPos4;
			if (ai.game.path_finding.data.IsPassable(pPos5))
			{
				heading = (target_pos - pPos5).Heading();
				return pPos5;
			}
			return pos;
		}
		return pos;
	}

	private PPos AdjustTargetPosition(PPos position, ref float heading, Squad sq_tg = null, bool flip_direction = false)
	{
		PPos pPos = position - target_pos;
		float num = pPos.Length();
		float max = 75f;
		float min = 50f;
		float num2 = ((num < 75f) ? num : UnityEngine.Random.Range(min, max));
		if (num > 30f)
		{
			heading = pPos.Heading();
			PPos result = target_pos + pPos.GetNormalized() * num2;
			if (sq_tg != null && sq_tg.battle_side == squad_to_follow.battle_side && num < 100f)
			{
				if (!flip_direction)
				{
					heading = sq_tg.direction.Heading();
				}
				else
				{
					heading = (-sq_tg.direction).Heading();
				}
			}
			return result;
		}
		return position;
	}

	private bool IsInSafePosition(out List<Squad> enemies_in_range, out float min_safe_dist)
	{
		enemies_in_range = new List<Squad>();
		min_safe_dist = 0f;
		for (int i = 0; i < ai.enemy_squads.Count; i++)
		{
			Squad squad = ai.enemy_squads[i];
			if (!squad.def.is_ranged || !squad.is_inside_walls_or_on_walls || (squad.def.is_siege_eq && squad.CanBePacked() && (squad.is_packed || squad.IsPacking())))
			{
				continue;
			}
			for (int j = 0; j < squads.Count; j++)
			{
				Squad squad2 = squads[j];
				if ((squad2.movement != null && squad2.movement.path != null) || squad2.movement.pf_path != null)
				{
					continue;
				}
				PPos pt = squad2.position;
				if (offsets.ContainsKey(squad2))
				{
					PPos pPos = offsets[squad2];
					pt = target_pos + pPos.GetRotated(0f - heading);
				}
				float num = squad.position.Dist(pt);
				float num2 = squad.Max_Shoot_Dist + ai.def.keep_formation_additional_safe_distance;
				if (num < squad.Max_Shoot_Dist + ai.def.keep_formation_additional_safe_distance)
				{
					if (!enemies_in_range.Contains(squad))
					{
						enemies_in_range.Add(squad);
					}
					if (min_safe_dist < num2 - num)
					{
						min_safe_dist = num2 - num;
					}
				}
			}
		}
		return min_safe_dist == 0f;
	}

	private void CheckFormation()
	{
		if (offsets == null)
		{
			formation_valid = false;
			return;
		}
		foreach (Squad squad in squads)
		{
			if (!offsets.ContainsKey(squad))
			{
				formation_valid = false;
				break;
			}
		}
		List<Squad> list = new List<Squad>(offsets.Keys);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (!squads.Contains(list[num]))
			{
				formation_valid = false;
				offsets.Remove(list[num]);
			}
		}
	}

	private void CreateFormation()
	{
		if (ai.game.path_finding?.data == null)
		{
			return;
		}
		if (!main)
		{
			if (squads.Count > 4)
			{
				formation.settings.formation_type = StandardArmyFormation.ArmyFormationType.RangedAtFront;
			}
			else
			{
				formation.settings.formation_type = StandardArmyFormation.ArmyFormationType.OneLine;
			}
		}
		slowest_squad = ArmyFormation.FindSlowestSquad(squads, out min_speed);
		if (is_outside_wall)
		{
			formation_valid = formation.CreateFormation(target_pos, heading, squads, out offsets, GetIsCorrectPosFunc(), use_hungarian_algorithm: true);
		}
		else
		{
			formation_valid = formation.CreateInsideWallsFormation(target_pos, heading, squads, out offsets, GetIsCorrectPosFunc(), use_hungarian_algorithm: true);
		}
		calc_next_checkpoint_time = true;
	}

	private Func<PPos, float, bool> GetIsCorrectPosFunc()
	{
		if (check_distance_to_towers)
		{
			return IsCorrectBreakSiegeDefensePos;
		}
		return ai.game.path_finding.data.IsPassable;
	}

	private bool IsCorrectBreakSiegeDefensePos(PPos pt, float radius = 0f)
	{
		bool flag = ai.game.path_finding.data.IsPassable(pt, radius);
		if (flag)
		{
			flag = !ai.IsInTowersRange(pt, ai.def.keep_formation_additional_safe_distance);
		}
		return flag;
	}

	private void CalcCheckpointTime()
	{
		float num = ai.game.time_f;
		foreach (Squad squad in squads)
		{
			if (squad != null && squad.movement.path != null && squad.normal_move_speed != 0f)
			{
				float num2 = ai.game.time_f + squad.movement.path.path_len / squad.normal_move_speed;
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		next_checkpoint_time = num;
		calc_next_checkpoint_time = false;
	}

	private void CalcMediumPathCompleteness()
	{
		paths_completeness.Clear();
		if (squads.Count <= 0)
		{
			medium_completeness = 0f;
			return;
		}
		float num = 0f;
		foreach (Squad squad in squads)
		{
			if (squad.movement.path == null || squad.movement.path.dst_pt == squad.movement.path.src_pt)
			{
				num += 1f;
				paths_completeness.Add(squad, 1f);
				continue;
			}
			PPos v = squad.movement.path.dst_pt - squad.movement.path.src_pt;
			float num2 = (squad.position - squad.movement.path.src_pt).ProjLen(v) / v.Length();
			num += num2;
			paths_completeness.Add(squad, num2);
		}
		medium_completeness = num / (float)squads.Count;
	}

	private float CalcSpeed(Squad sq)
	{
		float normal_move_speed = sq.normal_move_speed;
		float num = sq.def.run_speed_mul * normal_move_speed;
		float num2 = (first_checkpoint ? normal_move_speed : min_speed);
		float num4;
		if (base.target != null)
		{
			float num3 = ((paths_completeness != null && paths_completeness.ContainsKey(sq)) ? paths_completeness[sq] : 1f);
			if (num3 > 1f)
			{
				return normal_move_speed;
			}
			num4 = Mathf.Clamp(num2 * (1f + (medium_completeness - num3) * 10f), 0f, num);
			float num5 = (sq.double_time ? 0.444f : 0.666f);
			num4 = ((num4 > normal_move_speed + 0.25f * normal_move_speed && sq.GetStamina() / sq.MaxStamina() > num5) ? num : ((!(num4 < num2 * 0.75f)) ? normal_move_speed : 0f));
		}
		else
		{
			PPos pPos = offsets[sq];
			PPos pPos2 = target_pos + pPos.GetRotated(0f - heading);
			float num6 = (pPos2 - sq.position).Length();
			if (num6 < num2 * ai.def.update_interval * 4f && num6 > 0f)
			{
				num4 = num2;
			}
			else if (num6 < 0f)
			{
				num4 = 0f;
			}
			else
			{
				num4 = Mathf.Clamp((pPos2 - sq.position).Length() * 0.1f, 0f, num);
				if (num4 > 0f && num4 < 1f)
				{
					num4 = ((num2 < 1f) ? num2 : 1f);
				}
			}
		}
		return num4;
	}

	public override void RemoveSquad(Squad squad)
	{
		bool flag = false;
		if (squad.ai_command == this && squad == squad_to_follow)
		{
			flag = true;
		}
		base.RemoveSquad(squad);
		squad.can_avoid = true;
		if (flag)
		{
			RefreshSquadToFollow();
		}
	}

	public void SetDefensiveMode(bool value)
	{
		if (value != defensive_mode)
		{
			defensive_mode = value;
			ChooseFirstTargetPos();
		}
	}

	public void SetSquadsLimit(int limit)
	{
		if (limit < 0)
		{
			squads_limit = -1;
		}
		else
		{
			squads_limit = limit;
		}
	}

	public int GetSquadsLimit()
	{
		return squads_limit;
	}

	public void RefreshSquadToFollow()
	{
		if (squads.Count >= 1)
		{
			List<Squad> list = new List<Squad>(squads);
			list.Sort((Squad x, Squad y) => formation.Order(y).CompareTo(formation.Order(x)));
			list.Reverse();
			squad_to_follow = list[0];
		}
	}

	public void OnSupportersJoinedDefensivePosition(bool ignore_right)
	{
		formation.settings.one_sided = true;
		formation.settings.ignore_right = ignore_right;
		is_on_defensive_position = true;
		formation_valid = false;
	}
}
