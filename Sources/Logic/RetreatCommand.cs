using System;
using UnityEngine;

namespace Logic;

public class RetreatCommand : SquadCommand
{
	private PPos retreat_pos;

	private float retreat_timer;

	private bool ignore_retreat;

	public RetreatCommand(BattleAI ai, Squad target)
		: base(ai, target)
	{
		ignore_combat_when_changing_decision = true;
	}

	public override float Priority()
	{
		Squad squad = base.target as Squad;
		float threat = ai.GetThreat(squad);
		float num = float.MaxValue;
		float num2 = threat;
		float max_retreat_dist = ai.def.max_retreat_dist;
		float num3 = 1f;
		if (squad.def.type == Unit.Type.Noble)
		{
			num3 = Game.map_clamp(squad.simulation.damage / squad.simulation.max_damage, ai.def.retreat_command_threat_assessment_noble_health_map_min, ai.def.retreat_command_threat_assessment_noble_health_map_max, 1f, ai.def.retreat_command_threat_assessment_noble_mod);
		}
		Point point = Point.Invalid;
		float num4 = float.MaxValue;
		for (int i = 0; i < ai.enemy_groups.Count; i++)
		{
			PPos averagePosition = ai.enemy_groups[i].GetAveragePosition();
			float num5 = averagePosition.SqrDist(squad.position);
			if (num5 < num4)
			{
				num4 = num5;
				point = averagePosition;
			}
		}
		num4 = float.MinValue;
		for (float num6 = 0f - max_retreat_dist; num6 <= max_retreat_dist; num6 += 10f)
		{
			for (float num7 = 0f - max_retreat_dist; num7 <= max_retreat_dist; num7 += 10f)
			{
				float num8 = squad.position.x + num6;
				float num9 = squad.position.y + num7;
				Point point2 = new Point(num8, num9);
				if (num8 <= 0f || num9 <= 0f || num8 >= (float)squad.battle.batte_view_game.path_finding.data.width || num9 >= (float)squad.battle.batte_view_game.path_finding.data.height)
				{
					continue;
				}
				float num10 = Math.Max(0f, 1f - point2.Dist(squad.position) / max_retreat_dist);
				if (num10 <= 0f)
				{
					continue;
				}
				SquadPowerGrid.Threat threat2 = num10 * ai.battle.power_grids[1 - squad.battle_side].GetInterpolatedCell(num8, num9) * num3;
				float num11 = threat2.base_threat + threat2.salvos_about_to_land_threat;
				if (squad.def.is_cavalry)
				{
					num11 += threat2.anti_cavalry_threat;
				}
				else if (squad.def.is_ranged)
				{
					num11 += threat2.infantry_threat;
				}
				if (num11 > num2)
				{
					num2 = num11;
				}
				bool flag = false;
				if (num11 == num && point != Point.Invalid)
				{
					float num12 = point2.SqrDist(point);
					flag = num12 > num4;
					if (flag)
					{
						num4 = num12;
					}
				}
				if (num11 < num || flag)
				{
					num = num11;
					retreat_pos = new PPos(num8, num9);
				}
			}
		}
		if (num == float.MaxValue)
		{
			retreat_pos = base.target.position;
		}
		if (ai.my_squads.Count <= 1 || num2 == 0f)
		{
			return -1f;
		}
		float num13 = 1f - threat / num2;
		if (num13 <= 0f)
		{
			return -1f;
		}
		float num14 = RetreatModifier(squad);
		return Mathf.Max(0f, num14 * num13);
	}

	public static float RetreatModifier(Squad squad)
	{
		BattleAI battleAI = squad.battle.ai[squad.battle_side];
		float num = battleAI.def.retreat_command_mod * battleAI.def.retreat_command_threat_mod;
		if (squad.def.type == Unit.Type.Noble)
		{
			num *= battleAI.def.retreat_command_mod_noble;
			num = ((battleAI.battle_side != 0) ? (num * (2f * (1f - battleAI.battle.simulation.estimation))) : (num * (2f * battleAI.battle.simulation.estimation)));
			float num2 = squad.simulation.damage / squad.simulation.max_damage;
			num *= 1f + num2 * battleAI.def.retreat_command_mod_noble_health_modifier;
		}
		if (squad.def.is_siege_eq)
		{
			num *= 0.5f;
		}
		return num;
	}

	public override bool SingleSquad()
	{
		return true;
	}

	public override bool Validate()
	{
		if (!(base.target is Squad squad) || !squad.IsValid() || squad.IsDefeated() || squad.def.is_siege_eq)
		{
			return false;
		}
		return base.Validate();
	}

	public override bool Validate(Squad squad)
	{
		if (ignore_retreat || ai.estimation > ai.def.est_engage_perc_max || ai.estimation < ai.def.est_engage_perc_min)
		{
			return false;
		}
		if (squad.position.paID > 0 || squad.climbing)
		{
			return false;
		}
		if (squad.position.Dist(retreat_pos) <= 2f)
		{
			return false;
		}
		return base.Validate(squad);
	}

	public override void Execute()
	{
		base.Execute();
		if (ignore_retreat)
		{
			if (retreat_timer < 0f)
			{
				ignore_retreat = false;
				retreat_timer = ai.def.retreat_command_max_time;
			}
			retreat_timer -= ai.def.update_interval;
		}
		if (squads.Count == 0)
		{
			if (!ignore_retreat)
			{
				retreat_timer += ai.def.update_interval;
				if (retreat_timer > ai.def.retreat_command_max_time)
				{
					retreat_timer = ai.def.retreat_command_max_time;
				}
			}
		}
		else if (!(retreat_pos.x <= 0f) && !(retreat_pos.y <= 0f) && !(retreat_pos.x >= (float)ai.game.path_finding.data.width) && !(retreat_pos.y >= (float)ai.game.path_finding.data.height))
		{
			if (retreat_timer < 0f)
			{
				ignore_retreat = true;
				retreat_timer = ai.def.retreat_command_cd_time;
			}
			retreat_timer -= ai.def.update_interval;
			Squad squad = squads[0];
			if (squad.movement.path == null || (squad.movement.path != null && squad.movement.path.dst_pt.Dist(retreat_pos) > 0.1f))
			{
				squad.MoveTo(retreat_pos, 1f, double_time: true, (retreat_pos - squad.position).GetNormalized(), disengage: true);
			}
		}
	}
}
