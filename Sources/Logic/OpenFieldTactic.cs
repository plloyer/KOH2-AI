using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Logic;

public class BattleAI : Object
{
	private enum State
	{
		Preparing,
		CalculatingPriorities,
		AssigningSquads,
		ExecutingCommands,
		Finished
	}

	[Flags]
	public enum EnableFlags
	{
		Disabled = 0,
		Pathfinding = 1,
		Commands = 2,
		All = 0x7FF
	}

	public enum OpenFieldTactic
	{
		None,
		Advantage,
		MoreUnits,
		LessUnitsAdvantage,
		LessUnitsDisadvantage
	}

	public class Def : Logic.Def
	{
		public float update_interval = 1f;

		public float shock_mod = 1f;

		public float noble_bonus = 10f;

		public float noble_attacked_bonus = 30f;

		public float archer_attacked_bonus = 5f;

		public float siege_eq_attacked_bonus = 5f;

		public float drop_per_squad = 10f;

		public float infantry_archer_attacker_bonus = 10f;

		public float anti_cavalry_bonus = -20f;

		public float cavalry_vs_ranged_bonus = 10f;

		public float cavalry_vs_lone_squad_mod = 5f;

		public float distance_mod = -0.5f;

		public float speed_mod = 10f;

		public float health_mod = 2f;

		public float threat_comparison_mod = 0.5f;

		public float optimal_threat_advantage = 5f;

		public float morale_mod = 0.1f;

		public float fighting_other_bonus = -20f;

		public float already_fighting_bonus = 20f;

		public float not_engaged_attack_dist_mod = 0.5f;

		public float threat_salvos_mod = 2f;

		public float max_retreat_dist = 25f;

		public float eng_engage_perc = 50f;

		public float est_engage_perc_max = 80f;

		public float est_engage_perc_min = 20f;

		public float max_attack_dist = 80f;

		public float max_attack_dist_cavalry_vs_lone_squad = 160f;

		public float max_optimal_position_search_dist = 10f;

		public int max_cavalry_per_squad = 2;

		public int max_infantry_per_squad = 3;

		public int max_ranged_per_squad_ranged = 3;

		public int max_ranged_per_squad_melee = 2;

		public float charge_dist = 30f;

		public float attack_command_mod = 1f;

		public float attack_command_safe_shooting_range_mod = 0.7f;

		public float retreat_command_mod = 1f;

		public float retreat_command_mod_noble = 2f;

		public float retreat_command_mod_noble_health_modifier = 2f;

		public float retreat_command_max_time = 10f;

		public float retreat_command_cd_time = 15f;

		public float retreat_command_threat_mod = 90f;

		public float retreat_command_threat_assessment_noble_mod = 2f;

		public float retreat_command_threat_assessment_noble_health_map_min;

		public float retreat_command_threat_assessment_noble_health_map_max = 0.5f;

		public float keep_formation_defender_mod = 1f;

		public float keep_formation_attacker_mod = 1f;

		public float keep_formation_range_main = 200f;

		public float keep_formation_range_sub = 150f;

		public bool keep_formation_defensive_mode;

		public float keep_formation_additional_safe_distance = 70f;

		public float follow_command_base = 1f;

		public float defend_max_dist = 300f;

		public float defend_command_mod = 1f;

		public float defend_command_mod_victory = 1.1f;

		public float defend_defender_mod = 1.1f;

		public float defend_attacker_mod = 0.8f;

		public float defend_additional_radius = 5f;

		public float defend_distance_value_base = 80f;

		public float defend_threat_mod = 20f;

		public float defend_importance_mod = 1f;

		public float defend_gate_mod = 1.3f;

		public float defend_fortification_mod = 0.9f;

		public float defend_targeting_point_mod = 5f;

		public int defend_base_squads_ammount = 3;

		public float defend_archer_bonus = -10f;

		public float defend_noble_bonus = -5f;

		public float defend_speed_mod = 2f;

		public float defend_per_cp_diff_bonus = 5f;

		public float defend_last_cp_mod = 3f;

		public float defend_per_targeting_enemy_amount_bonus = 0.5f;

		public float defend_per_enemy_in_range_amount_bonus = 0.5f;

		public float defend_scmd_attack_base_priority = 60f;

		public float defend_scmd_attack_capturing_bonus = 20f;

		public float defend_scmd_attack_distance_bonus = -0.05f;

		public float defend_scmd_attack_already_in_melee_bonus = -10f;

		public float defend_scmd_intercept_base_priority = 60f;

		public float defend_scmd_intercept_already_in_melee_bonus = -15f;

		public float defend_scmd_intercept_distance_bonus = -0.05f;

		public float defend_scmd_intercept_speed_mul = 2f;

		public float defend_scmd_avoid_base_priority = 39f;

		public float defend_scmd_avoid_avoid_range = 10f;

		public float defend_scmd_protect_point_base_priority = 20f;

		public float defend_scmd_protect_point_enemy_capturing_bonus = 20f;

		public float capture_defender_mod = 0.5f;

		public float capture_attacker_mod = 1f;

		public float capture_mod_range_min = 1f;

		public float capture_mod_range_max = 1f;

		public float capture_threat_base = 80f;

		public float capture_threat_mod = 10f;

		public float capture_importance_mod = 40f;

		public float capture_distance_mod = 1f;

		public float capture_gate_mod = 1.2f;

		public float capture_fortification_mod = 0.8f;

		public float capture_max_dist = 80f;

		public float capture_per_cp_diff_bonus = 5f;

		public float change_decision_ratio = 1.5f;

		public float change_decision_ratio_in_other_combat = 1.75f;

		public float change_decision_ratio_in_combat = 0.75f;

		public float destroy_wall_base = 140f;

		public float destroy_wall_destroyed_mod = 0.5f;

		public float climbing_squad_bonus = 10f;

		public float flanking_chance = 0.5f;

		public float treat_like_group_distance = 60f;

		public float low_tier_formation_change_chance = 0.05f;

		public float mid_tier_formation_change_chance = 0.1f;

		public float high_tier_formation_change_chance = 0.2f;

		private static void LoadMinMaxField(DT.Field field, ref float val_min, ref float val_max, string key)
		{
			DT.Field field2 = field.FindChild(key);
			if (field2 != null)
			{
				if (field2.NumValues() == 2)
				{
					val_min = field2.Float(0, null, val_min);
					val_max = field2.Float(1, null, val_max);
				}
				else
				{
					val_min = (val_max = field2.Float(0, null, val_min));
				}
			}
			else
			{
				val_min = field.GetFloat(key + "_min", null, val_min);
				val_max = field.GetFloat(key + "_max", null, val_max);
			}
		}

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			update_interval = game.GetPerDifficultyFloat(field.FindChild("update_interval"), null, update_interval);
			shock_mod = game.GetPerDifficultyFloat(field.FindChild("shock_mod"), null, shock_mod);
			noble_bonus = game.GetPerDifficultyFloat(field.FindChild("noble_bonus"), null, noble_bonus);
			noble_attacked_bonus = game.GetPerDifficultyFloat(field.FindChild("noble_attacked_bonus"), null, noble_attacked_bonus);
			archer_attacked_bonus = game.GetPerDifficultyFloat(field.FindChild("archer_attacked_bonus"), null, archer_attacked_bonus);
			siege_eq_attacked_bonus = game.GetPerDifficultyFloat(field.FindChild("siege_eq_attacked_bonus"), null, siege_eq_attacked_bonus);
			drop_per_squad = game.GetPerDifficultyFloat(field.FindChild("drop_per_squad"), null, drop_per_squad);
			threat_salvos_mod = game.GetPerDifficultyFloat(field.FindChild("threat_salvos_mod"), null, threat_salvos_mod);
			infantry_archer_attacker_bonus = game.GetPerDifficultyFloat(field.FindChild("infantry_archer_attacker_bonus"), null, infantry_archer_attacker_bonus);
			anti_cavalry_bonus = game.GetPerDifficultyFloat(field.FindChild("anti_cavalry_bonus"), null, anti_cavalry_bonus);
			cavalry_vs_ranged_bonus = game.GetPerDifficultyFloat(field.FindChild("cavalry_vs_ranged_bonus"), null, cavalry_vs_ranged_bonus);
			cavalry_vs_lone_squad_mod = game.GetPerDifficultyFloat(field.FindChild("cavalry_vs_lone_squad_mod"), null, cavalry_vs_lone_squad_mod);
			distance_mod = game.GetPerDifficultyFloat(field.FindChild("distance_mod"), null, distance_mod);
			speed_mod = game.GetPerDifficultyFloat(field.FindChild("speed_mod"), null, speed_mod);
			health_mod = game.GetPerDifficultyFloat(field.FindChild("health_mod"), null, health_mod);
			threat_comparison_mod = game.GetPerDifficultyFloat(field.FindChild("threat_comparison_mod"), null, threat_comparison_mod);
			optimal_threat_advantage = game.GetPerDifficultyFloat(field.FindChild("optimal_threat_advantage"), null, optimal_threat_advantage);
			morale_mod = game.GetPerDifficultyFloat(field.FindChild("morale_mod"), null, morale_mod);
			fighting_other_bonus = game.GetPerDifficultyFloat(field.FindChild("fighting_other_bonus"), null, fighting_other_bonus);
			already_fighting_bonus = game.GetPerDifficultyFloat(field.FindChild("already_fighting_bonus"), null, already_fighting_bonus);
			not_engaged_attack_dist_mod = game.GetPerDifficultyFloat(field.FindChild("not_engaged_attack_dist_mod"), null, not_engaged_attack_dist_mod);
			max_retreat_dist = game.GetPerDifficultyFloat(field.FindChild("max_retreat_dist"), null, max_retreat_dist);
			max_cavalry_per_squad = game.GetPerDifficultyInt(field.FindChild("max_cavalry_per_squad"), null, max_cavalry_per_squad);
			max_infantry_per_squad = game.GetPerDifficultyInt(field.FindChild("max_infantry_per_squad"), null, max_infantry_per_squad);
			max_ranged_per_squad_ranged = game.GetPerDifficultyInt(field.FindChild("max_ranged_per_squad_ranged"), null, max_ranged_per_squad_ranged);
			max_ranged_per_squad_melee = game.GetPerDifficultyInt(field.FindChild("max_ranged_per_squad_melee"), null, max_ranged_per_squad_melee);
			charge_dist = game.GetPerDifficultyFloat(field.FindChild("charge_dist"), null, charge_dist);
			eng_engage_perc = game.GetPerDifficultyFloat(field.FindChild("eng_engage_perc"), null, eng_engage_perc);
			est_engage_perc_max = game.GetPerDifficultyFloat(field.FindChild("est_engage_perc_max"), null, est_engage_perc_max);
			est_engage_perc_min = game.GetPerDifficultyFloat(field.FindChild("est_engage_perc_min"), null, est_engage_perc_min);
			max_attack_dist = game.GetPerDifficultyFloat(field.FindChild("max_attack_dist"), null, max_attack_dist);
			max_attack_dist_cavalry_vs_lone_squad = game.GetPerDifficultyFloat(field.FindChild("max_attack_dist_cavalry_vs_lone_squad"), null, max_attack_dist_cavalry_vs_lone_squad);
			max_optimal_position_search_dist = game.GetPerDifficultyFloat(field.FindChild("max_optimal_position_search_dist"), null, max_optimal_position_search_dist);
			attack_command_mod = game.GetPerDifficultyFloat(field.FindChild("attack_command_mod"), null, attack_command_mod);
			attack_command_safe_shooting_range_mod = game.GetPerDifficultyFloat(field.FindChild("attack_command_safe_shooting_range_mod"), null, attack_command_safe_shooting_range_mod);
			retreat_command_mod = game.GetPerDifficultyFloat(field.FindChild("retreat_command_mod"), null, retreat_command_mod);
			retreat_command_mod_noble = game.GetPerDifficultyFloat(field.FindChild("retreat_command_mod_noble"), null, retreat_command_mod_noble);
			retreat_command_mod_noble_health_modifier = game.GetPerDifficultyFloat(field.FindChild("retreat_command_mod_noble_health_modifier"), null, retreat_command_mod_noble_health_modifier);
			retreat_command_max_time = game.GetPerDifficultyFloat(field.FindChild("retreat_command_max_time"), null, retreat_command_max_time);
			retreat_command_cd_time = game.GetPerDifficultyFloat(field.FindChild("retreat_command_cd_time"), null, retreat_command_cd_time);
			retreat_command_threat_mod = game.GetPerDifficultyFloat(field.FindChild("retreat_command_threat_mod"), null, retreat_command_threat_mod);
			retreat_command_threat_assessment_noble_mod = game.GetPerDifficultyFloat(field.FindChild("retreat_command_threat_assessment_noble_mod"), null, retreat_command_threat_assessment_noble_mod);
			LoadMinMaxField(field, ref retreat_command_threat_assessment_noble_health_map_min, ref retreat_command_threat_assessment_noble_health_map_max, "retreat_command_threat_assessment_noble_health_map");
			keep_formation_defender_mod = game.GetPerDifficultyFloat(field.FindChild("keep_formation_defender_mod"), null, keep_formation_defender_mod);
			keep_formation_attacker_mod = game.GetPerDifficultyFloat(field.FindChild("keep_formation_attacker_mod"), null, keep_formation_attacker_mod);
			keep_formation_defensive_mode = game.GetPerDifficultyBool(field.FindChild("keep_formation_defensive_mode"), null, keep_formation_defensive_mode);
			keep_formation_range_main = game.GetPerDifficultyFloat(field.FindChild("keep_formation_range_main"), null, keep_formation_range_main);
			keep_formation_range_sub = game.GetPerDifficultyFloat(field.FindChild("keep_formation_range_sub"), null, keep_formation_range_sub);
			keep_formation_additional_safe_distance = game.GetPerDifficultyFloat(field.FindChild("keep_formation_additional_safe_distance"), null, keep_formation_additional_safe_distance);
			follow_command_base = game.GetPerDifficultyFloat(field.FindChild("follow_command_base"), null, follow_command_base);
			defend_max_dist = game.GetPerDifficultyFloat(field.FindChild("defend_max_dist"), null, defend_max_dist);
			defend_command_mod = game.GetPerDifficultyFloat(field.FindChild("defend_command_mod"), null, defend_command_mod);
			defend_command_mod_victory = game.GetPerDifficultyFloat(field.FindChild("defend_command_mod_victory"), null, defend_command_mod_victory);
			defend_additional_radius = game.GetPerDifficultyFloat(field.FindChild("defend_additional_radius"), null, defend_additional_radius);
			defend_distance_value_base = game.GetPerDifficultyFloat(field.FindChild("defend_distance_value_base"), null, defend_distance_value_base);
			defend_threat_mod = game.GetPerDifficultyFloat(field.FindChild("defend_threat_mod"), null, defend_threat_mod);
			defend_importance_mod = game.GetPerDifficultyFloat(field.FindChild("defend_importance_mod"), null, defend_importance_mod);
			defend_defender_mod = game.GetPerDifficultyFloat(field.FindChild("defend_defender_mod"), null, defend_defender_mod);
			defend_attacker_mod = game.GetPerDifficultyFloat(field.FindChild("defend_attacker_mod"), null, defend_attacker_mod);
			defend_gate_mod = game.GetPerDifficultyFloat(field.FindChild("defend_gate_mod"), null, defend_gate_mod);
			defend_fortification_mod = game.GetPerDifficultyFloat(field.FindChild("defend_fortification_mod"), null, defend_fortification_mod);
			defend_targeting_point_mod = game.GetPerDifficultyFloat(field.FindChild("defend_targeting_point_mod"), null, defend_targeting_point_mod);
			defend_base_squads_ammount = game.GetPerDifficultyInt(field.FindChild("defend_base_squads_ammount"), null, defend_base_squads_ammount);
			defend_archer_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_archer_bonus"), null, defend_archer_bonus);
			defend_noble_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_noble_bonus"), null, defend_noble_bonus);
			defend_speed_mod = game.GetPerDifficultyFloat(field.FindChild("defend_speed_mod"), null, defend_speed_mod);
			defend_per_cp_diff_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_per_cp_diff_bonus"), null, defend_per_cp_diff_bonus);
			defend_last_cp_mod = game.GetPerDifficultyFloat(field.FindChild("defend_last_cp_mod"), null, defend_last_cp_mod);
			defend_per_targeting_enemy_amount_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_per_targeting_enemy_amount_bonus"), null, defend_per_targeting_enemy_amount_bonus);
			defend_per_enemy_in_range_amount_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_per_enemy_in_range_amount_bonus"), null, defend_per_enemy_in_range_amount_bonus);
			defend_scmd_attack_base_priority = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_attack_base_priority"), null, defend_scmd_attack_base_priority);
			defend_scmd_attack_capturing_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_attack_capturing_bonus"), null, defend_scmd_attack_capturing_bonus);
			defend_scmd_attack_distance_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_attack_distance_bonus"), null, defend_scmd_attack_distance_bonus);
			defend_scmd_attack_already_in_melee_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_attack_already_in_melee_bonus"), null, defend_scmd_attack_already_in_melee_bonus);
			defend_scmd_intercept_base_priority = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_intercept_base_priority"), null, defend_scmd_intercept_base_priority);
			defend_scmd_intercept_already_in_melee_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_intercept_already_in_melee_bonus"), null, defend_scmd_intercept_already_in_melee_bonus);
			defend_scmd_intercept_distance_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_intercept_distance_bonus"), null, defend_scmd_intercept_distance_bonus);
			defend_scmd_intercept_speed_mul = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_intercept_speed_mul"), null, defend_scmd_intercept_speed_mul);
			defend_scmd_avoid_base_priority = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_avoid_base_priority"), null, defend_scmd_avoid_base_priority);
			defend_scmd_avoid_avoid_range = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_avoid_avoid_range"), null, defend_scmd_avoid_avoid_range);
			defend_scmd_protect_point_base_priority = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_protect_point_base_priority"), null, defend_scmd_protect_point_base_priority);
			defend_scmd_protect_point_enemy_capturing_bonus = game.GetPerDifficultyFloat(field.FindChild("defend_scmd_protect_point_enemy_capturing_bonus"), null, defend_scmd_protect_point_enemy_capturing_bonus);
			capture_defender_mod = game.GetPerDifficultyFloat(field.FindChild("capture_defender_mod"), null, capture_defender_mod);
			capture_attacker_mod = game.GetPerDifficultyFloat(field.FindChild("capture_attacker_mod"), null, capture_attacker_mod);
			capture_mod_range_min = game.GetPerDifficultyFloat(field.FindChild("capture_mod_range_min"), null, capture_mod_range_min);
			capture_mod_range_max = game.GetPerDifficultyFloat(field.FindChild("capture_mod_range_max"), null, capture_mod_range_max);
			capture_threat_base = game.GetPerDifficultyFloat(field.FindChild("capture_threat_base"), null, capture_threat_base);
			capture_threat_mod = game.GetPerDifficultyFloat(field.FindChild("capture_threat_mod"), null, capture_threat_mod);
			capture_importance_mod = game.GetPerDifficultyFloat(field.FindChild("capture_importance_mod"), null, capture_importance_mod);
			capture_distance_mod = game.GetPerDifficultyFloat(field.FindChild("capture_distance_mod"), null, capture_distance_mod);
			capture_gate_mod = game.GetPerDifficultyFloat(field.FindChild("capture_gate_mod"), null, capture_gate_mod);
			capture_fortification_mod = game.GetPerDifficultyFloat(field.FindChild("capture_fortification_mod"), null, capture_fortification_mod);
			capture_max_dist = game.GetPerDifficultyFloat(field.FindChild("capture_max_dist"), null, capture_max_dist);
			capture_per_cp_diff_bonus = game.GetPerDifficultyFloat(field.FindChild("capture_per_cp_diff_bonus"), null, capture_per_cp_diff_bonus);
			change_decision_ratio = game.GetPerDifficultyFloat(field.FindChild("change_decision_ratio"), null, change_decision_ratio);
			change_decision_ratio_in_other_combat = game.GetPerDifficultyFloat(field.FindChild("change_decision_ratio_in_other_combat"), null, change_decision_ratio_in_other_combat);
			change_decision_ratio_in_combat = game.GetPerDifficultyFloat(field.FindChild("change_decision_ratio_in_combat"), null, change_decision_ratio_in_combat);
			destroy_wall_base = game.GetPerDifficultyFloat(field.FindChild("destroy_wall_base"), null, destroy_wall_base);
			destroy_wall_destroyed_mod = game.GetPerDifficultyFloat(field.FindChild("destroy_wall_destroyed_mod"), null, destroy_wall_destroyed_mod);
			climbing_squad_bonus = game.GetPerDifficultyFloat(field.FindChild("climbing_squad_bonus"), null, climbing_squad_bonus);
			flanking_chance = game.GetPerDifficultyFloat(field.FindChild("flanking_chance"), null, flanking_chance);
			treat_like_group_distance = game.GetPerDifficultyFloat(field.FindChild("treat_like_group_distance"), null, treat_like_group_distance);
			low_tier_formation_change_chance = game.GetPerDifficultyFloat(field.FindChild("low_tier_formation_change_chance"), null, low_tier_formation_change_chance);
			mid_tier_formation_change_chance = game.GetPerDifficultyFloat(field.FindChild("mid_tier_formation_change_chance"), null, mid_tier_formation_change_chance);
			high_tier_formation_change_chance = game.GetPerDifficultyFloat(field.FindChild("high_tier_formation_change_chance"), null, high_tier_formation_change_chance);
			return true;
		}
	}

	private State state;

	public Battle battle;

	public int battle_side;

	public List<Squad> enemy_squads = new List<Squad>();

	public List<Squad> my_squads = new List<Squad>();

	public List<Squad> my_squads_sorted = new List<Squad>();

	public List<Squad> cavalry = new List<Squad>();

	public List<Squad> infantry = new List<Squad>();

	public List<SquadGroup> enemy_groups;

	public List<SquadGroup> groups;

	public Squad[] last_followed = new Squad[3];

	public List<CapturePoint> enemy_capture_points = new List<CapturePoint>();

	public List<CapturePoint> my_capture_points = new List<CapturePoint>();

	public int important_enemy_capture_points;

	public int important_my_capture_points;

	public int my_gates;

	public int enemy_gates;

	public PPos ref_direction;

	public PPos my_camps_avg_pos;

	public PPos enemy_camps_avg_pos;

	public bool flanking;

	public bool second_flanking;

	public bool guard_left;

	public bool can_kill_our_marshals;

	private StandardArmyFormation selected_army_formation = new StandardArmyFormation();

	public static EnableFlags player_flags;

	public EnableFlags owner_enabled = EnableFlags.All;

	public EnableFlags supporter_enabled = EnableFlags.All;

	public EnableFlags second_supporter_enabled = EnableFlags.All;

	public EnableFlags mercenary_enabled = EnableFlags.All;

	public Def def;

	public Kingdom owner;

	public Kingdom supporter;

	public Kingdom second_supporter;

	public Kingdom garrison_kingdom;

	public Kingdom first_army_kingdom;

	public Kingdom second_army_kingdom;

	public Army army1;

	public Army army2;

	public Garrison garrison;

	public List<SquadCommand> squad_commands = new List<SquadCommand>();

	public float engaged_perc;

	public float enemy_engaged_perc;

	public float estimation;

	public float proportion = 1f;

	public bool tactic_choosen;

	public float last_estimation_check_value;

	public OpenFieldTactic actual_tactic;

	private bool offset_updates;

	public float capture_command_mod_mul = 1f;

	public static EnableFlags ParseAIFlags(EnableFlags enabled, string s)
	{
		EnableFlags enableFlags = EnableFlags.Disabled;
		int num = 0;
		for (int i = 0; i < s.Length; i++)
		{
			char c = char.ToLowerInvariant(s[i]);
			EnableFlags enableFlags2;
			if ((uint)c <= 49u)
			{
				switch (c)
				{
				case '+':
					if (num == 0 && enableFlags == EnableFlags.Disabled)
					{
						enableFlags = enabled;
					}
					num = 1;
					continue;
				case '-':
					if (num == 0 && enableFlags == EnableFlags.Disabled)
					{
						enableFlags = enabled;
					}
					num = 2;
					continue;
				case '1':
					break;
				default:
					continue;
				}
			}
			else if (c != '2')
			{
				if (c != 'c')
				{
					if (c != 'p')
					{
						continue;
					}
					enableFlags2 = EnableFlags.Pathfinding;
				}
				else
				{
					enableFlags2 = EnableFlags.Commands;
				}
				goto IL_0065;
			}
			enableFlags2 = EnableFlags.All;
			goto IL_0065;
			IL_0065:
			enableFlags = ((num != 2) ? (enableFlags | enableFlags2) : (enableFlags & ~enableFlags2));
		}
		return enableFlags;
	}

	public bool HasOwnerFlag(EnableFlags flags)
	{
		return (owner_enabled & flags) != 0;
	}

	public bool HasSupporterFlag(EnableFlags flags)
	{
		return (supporter_enabled & flags) != 0;
	}

	public bool HasSecondSupporterFlag(EnableFlags flags)
	{
		return (second_supporter_enabled & flags) != 0;
	}

	public bool HasMercenaryFlag(EnableFlags flags)
	{
		return (mercenary_enabled & flags) != 0;
	}

	public int GetFollowID(Squad src)
	{
		List<Army> armies = battle.GetArmies(battle_side);
		if (armies == null)
		{
			return 0;
		}
		if (armies.Count <= 1 || src?.simulation?.army == armies[0] || !src.IsValid())
		{
			return 0;
		}
		if (src.simulation.garrison != null)
		{
			return 2;
		}
		return 1;
	}

	public Squad GetFollowTarget(Squad src)
	{
		return last_followed[GetFollowID(src)];
	}

	public void SetFollowTarget(Squad src, Squad tgt)
	{
		last_followed[GetFollowID(src)] = tgt;
	}

	public void SetFollowTarget(int src, Squad tgt)
	{
		last_followed[src] = tgt;
	}

	public bool IsOwner(Kingdom kingdom)
	{
		if (garrison_kingdom != null)
		{
			return kingdom == garrison_kingdom;
		}
		return kingdom == first_army_kingdom;
	}

	public bool IsSupporter(Kingdom kingdom)
	{
		if (garrison_kingdom != null)
		{
			return kingdom == first_army_kingdom;
		}
		return kingdom == second_army_kingdom;
	}

	public bool IsSecondSupporter(Kingdom kingdom)
	{
		if (garrison_kingdom != null)
		{
			return kingdom == second_army_kingdom;
		}
		return false;
	}

	public void RefreshDef()
	{
		string text = battle.type.ToString();
		if (battle.type == Battle.Type.PlunderInterrupt)
		{
			text = Battle.Type.Plunder.ToString();
		}
		def = battle.game.defs.Find<Def>("BattleAI" + text);
		if (def == null)
		{
			def = battle.game.defs.GetBase<Def>();
		}
		def.Load(game.game);
	}

	public void LoadSpecificDef(string name)
	{
		string text = Battle.Type.OpenField.ToString();
		def = battle.game.defs.Find<Def>("BattleAI" + text + name);
		if (def == null)
		{
			def = battle.game.defs.GetBase<Def>();
		}
		def.Load(game.game);
	}

	public BattleAI(Battle battle, int battle_side, Kingdom garrison_kingdom, Kingdom first_army_kingdom, Kingdom second_army_kingdom, List<Army> armies, Garrison garrison = null)
		: base(battle?.batte_view_game)
	{
		this.battle = battle;
		this.battle_side = battle_side;
		RefreshDef();
		SetUpAI(garrison_kingdom, first_army_kingdom, second_army_kingdom, armies, garrison);
	}

	public void SetUpAI(Kingdom garrison_kingdom, Kingdom first_army_kingdom, Kingdom second_army_kingdom, List<Army> armies, Garrison garrison = null)
	{
		SetGarrisonKingdom(garrison_kingdom);
		SetFirstArmyKingdom(first_army_kingdom);
		SetSecondArmyKingdom(second_army_kingdom);
		this.garrison = garrison;
		if (garrison_kingdom != null)
		{
			SetOwner(garrison_kingdom);
			if (first_army_kingdom != garrison_kingdom && first_army_kingdom != null)
			{
				SetSupporter(first_army_kingdom);
				if (second_army_kingdom != null && second_army_kingdom != garrison_kingdom && second_army_kingdom != first_army_kingdom)
				{
					SetSecondSupporter(second_army_kingdom);
				}
				else
				{
					SetSecondSupporter(null);
				}
			}
			else if (second_army_kingdom != garrison_kingdom)
			{
				SetSupporter(second_army_kingdom);
				SetSecondSupporter(null);
			}
			else
			{
				SetSupporter(null);
				SetSecondSupporter(null);
			}
		}
		else
		{
			SetOwner(first_army_kingdom);
			if (second_army_kingdom != first_army_kingdom)
			{
				SetSupporter(second_army_kingdom);
			}
			else
			{
				SetSupporter(null);
			}
			SetSecondSupporter(null);
		}
		if (armies.Count > 0)
		{
			army1 = armies[0];
		}
		else
		{
			army1 = null;
		}
		if (armies.Count > 1)
		{
			army2 = armies[1];
		}
		else
		{
			army2 = null;
		}
	}

	public override Kingdom GetKingdom()
	{
		return owner?.GetKingdom();
	}

	public override IRelationCheck GetStanceObj()
	{
		return null;
	}

	protected override void OnStart()
	{
		base.OnStart();
		List<Squad> list = battle.squads.Get(battle_side);
		List<Squad> list2 = battle.squads.Get(1 - battle_side);
		for (int i = 0; i < list.Count; i++)
		{
			if (!my_squads.Contains(list[i]))
			{
				my_squads.Add(list[i]);
			}
		}
		for (int j = 0; j < list2.Count; j++)
		{
			if (!enemy_squads.Contains(list2[j]))
			{
				enemy_squads.Add(list2[j]);
			}
		}
		for (int k = 0; k < battle.capture_points.Count; k++)
		{
			AddCapturePoint(battle.capture_points[k]);
		}
		CalculateDirection();
		UpdateAfter(def.update_interval + def.update_interval * 0.5f * (float)battle_side);
		SelectFightingFormation();
		SelectCommandsMulValues();
		state = State.Preparing;
	}

	private void SelectCommandsMulValues()
	{
		capture_command_mod_mul = UnityEngine.Random.Range(def.capture_mod_range_min, def.capture_mod_range_max);
	}

	public void OnRestart()
	{
		flanking = false;
		second_flanking = false;
		guard_left = false;
		squad_commands = new List<SquadCommand>();
		List<Squad> collection = battle.squads.Get(battle_side);
		List<Squad> collection2 = battle.squads.Get(1 - battle_side);
		my_squads = new List<Squad>(collection);
		enemy_squads = new List<Squad>(collection2);
		state = State.Preparing;
		offset_updates = true;
	}

	private void SelectFightingFormation()
	{
		List<StandardArmyFormation> list = PrepareArmyFormations();
		int index = UnityEngine.Random.Range(0, list.Count);
		selected_army_formation = list[index];
	}

	private List<StandardArmyFormation> PrepareArmyFormations()
	{
		return new List<StandardArmyFormation>
		{
			new StandardArmyFormation()
		};
	}

	public override void OnUpdate()
	{
		if (battle.stage != Battle.Stage.Ongoing)
		{
			UpdateAfter(def.update_interval + (offset_updates ? (def.update_interval * 0.5f * (float)battle_side) : 0f));
			if (offset_updates)
			{
				offset_updates = false;
			}
		}
		else if (battle.power_grids == null)
		{
			UpdateAfter(def.update_interval);
		}
		else if (my_squads.Count == 0 || enemy_squads.Count == 0)
		{
			UpdateAfter(def.update_interval);
		}
		else if (state == State.Finished || state == State.Preparing)
		{
			state = State.Preparing;
			CountSquads();
			UpdateGroups();
			UpdateEnemyGroups();
			if (!IsSiege() && (!tactic_choosen || Mathf.Abs(last_estimation_check_value - estimation) > 15f))
			{
				ChooseOverallTactic();
			}
			CreateCommands();
			state = State.CalculatingPriorities;
			UpdateNextFrame();
		}
		else if (state == State.CalculatingPriorities)
		{
			CalcPriorities();
			SortCommands();
			state = State.AssigningSquads;
			UpdateNextFrame();
		}
		else if (state == State.AssigningSquads)
		{
			AssignSquads();
			state = State.ExecutingCommands;
			UpdateNextFrame();
		}
		else if (state == State.ExecutingCommands)
		{
			UpdateAfter(def.update_interval);
			ExecuteCommands();
			UpdateFormationSettings();
			state = State.Finished;
		}
	}

	public bool IsSiege()
	{
		if (battle.is_siege && battle.settlement != null)
		{
			return battle.settlement is Castle;
		}
		return false;
	}

	public void AddSquad(Squad squad)
	{
		if (squad.is_main_squad && !squad.IsDefeated())
		{
			if (squad.battle_side == battle_side)
			{
				my_squads.Add(squad);
			}
			else if (squad.battle_side == 1 - battle_side)
			{
				enemy_squads.Add(squad);
			}
		}
	}

	public void DelSquad(Squad squad)
	{
		if (squad.battle_side == battle_side)
		{
			my_squads.Remove(squad);
		}
		else if (squad.battle_side == 1 - battle_side)
		{
			enemy_squads.Remove(squad);
		}
	}

	public void AddCapturePoint(CapturePoint capture_point)
	{
		if (capture_point.battle_side == battle_side)
		{
			my_capture_points.Add(capture_point);
			if (capture_point.def.count_victory)
			{
				important_my_capture_points++;
			}
			Fortification fortification = capture_point.fortification;
			if (fortification != null && fortification.def.type == Fortification.Type.Gate)
			{
				my_gates++;
			}
		}
		else if (capture_point.battle_side == 1 - battle_side)
		{
			enemy_capture_points.Add(capture_point);
			if (capture_point.def.count_victory)
			{
				important_enemy_capture_points++;
			}
			Fortification fortification2 = capture_point.fortification;
			if (fortification2 != null && fortification2.def.type == Fortification.Type.Gate)
			{
				enemy_gates++;
			}
		}
	}

	public void DelCapturePoint(CapturePoint capture_point)
	{
		if (capture_point.battle_side == battle_side)
		{
			my_capture_points.Remove(capture_point);
			if (capture_point.def.count_victory)
			{
				important_my_capture_points--;
			}
			Fortification fortification = capture_point.fortification;
			if (fortification != null && fortification.def.type == Fortification.Type.Gate)
			{
				my_gates--;
				if (my_gates < 0)
				{
					my_gates = 0;
				}
			}
		}
		else
		{
			if (capture_point.battle_side != 1 - battle_side)
			{
				return;
			}
			enemy_capture_points.Remove(capture_point);
			if (capture_point.def.count_victory)
			{
				important_enemy_capture_points--;
			}
			Fortification fortification2 = capture_point.fortification;
			if (fortification2 != null && fortification2.def.type == Fortification.Type.Gate)
			{
				enemy_gates--;
				if (enemy_gates < 0)
				{
					enemy_gates = 0;
				}
			}
		}
	}

	private void CountSquads()
	{
		int num = 0;
		for (int i = 0; i < my_squads.Count; i++)
		{
			Squad squad = my_squads[i];
			if (squad != null && !squad.IsDefeated() && (squad.enemy_squad != null || squad.ranged_enemy != null))
			{
				num++;
			}
		}
		int num2 = 0;
		for (int j = 0; j < enemy_squads.Count; j++)
		{
			Squad squad2 = enemy_squads[j];
			if (squad2 != null && !squad2.IsDefeated() && (squad2.enemy_squad != null || squad2.ranged_enemy != null))
			{
				num2++;
			}
		}
		my_squads_sorted.Clear();
		my_squads_sorted.AddRange(my_squads);
		proportion = (float)my_squads.Count / (float)enemy_squads.Count;
		if (proportion < 1f)
		{
			proportion = 1f;
		}
		engaged_perc = (float)num / (float)my_squads.Count * 100f;
		enemy_engaged_perc = (float)num2 / (float)my_squads.Count * 100f;
		if (battle_side == 0)
		{
			estimation = (1f - battle.simulation.estimation) * 100f;
		}
		else
		{
			estimation = battle.simulation.estimation * 100f;
		}
	}

	private void ChooseOverallTactic()
	{
		bool flag = false;
		int count = enemy_squads.Count;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		foreach (Squad enemy_squad in enemy_squads)
		{
			if (enemy_squad.def.is_cavalry)
			{
				num4++;
			}
			else if (enemy_squad.def.is_ranged)
			{
				num3++;
			}
			else if (enemy_squad.def.is_defense)
			{
				num2++;
			}
			else if (enemy_squad.def.is_siege_eq)
			{
				num5++;
			}
			else
			{
				num++;
			}
		}
		int count2 = my_squads.Count;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		foreach (Squad my_squad in my_squads)
		{
			if (my_squad.def.is_cavalry)
			{
				num9++;
			}
			else if (my_squad.def.is_ranged)
			{
				num8++;
			}
			else if (my_squad.def.is_defense)
			{
				num7++;
			}
			else if (my_squad.def.is_siege_eq)
			{
				num10++;
			}
			else
			{
				num6++;
			}
		}
		if ((float)count2 >= 0.5f * (float)count)
		{
			if (estimation > 30f)
			{
				if (actual_tactic != OpenFieldTactic.Advantage)
				{
					LoadSpecificDef("Advantage");
					actual_tactic = OpenFieldTactic.Advantage;
					flag = true;
				}
			}
			else if (num9 > num4)
			{
				if (actual_tactic != OpenFieldTactic.MoreUnits)
				{
					LoadSpecificDef("MoreUnits");
					actual_tactic = OpenFieldTactic.MoreUnits;
					flag = true;
				}
			}
			else if (actual_tactic != OpenFieldTactic.MoreUnits)
			{
				LoadSpecificDef("MoreUnits");
				actual_tactic = OpenFieldTactic.MoreUnits;
				flag = true;
			}
		}
		else if (estimation > 30f)
		{
			if (num9 > num4)
			{
				if (actual_tactic != OpenFieldTactic.LessUnitsAdvantage)
				{
					LoadSpecificDef("LessUnitsAdvantage");
					actual_tactic = OpenFieldTactic.LessUnitsAdvantage;
					flag = true;
				}
			}
			else if (actual_tactic != OpenFieldTactic.LessUnitsAdvantage)
			{
				LoadSpecificDef("LessUnitsAdvantage");
				actual_tactic = OpenFieldTactic.LessUnitsAdvantage;
				flag = true;
			}
		}
		else if (actual_tactic != OpenFieldTactic.LessUnitsDisadvantage)
		{
			LoadSpecificDef("LessUnitsDisadvantage");
			actual_tactic = OpenFieldTactic.LessUnitsDisadvantage;
			flag = true;
		}
		tactic_choosen = true;
		last_estimation_check_value = estimation;
		if (!flag)
		{
			return;
		}
		foreach (KeepFormationCommand keepFormationCommand in GetKeepFormationCommands())
		{
			keepFormationCommand.Reset();
			squad_commands.Remove(keepFormationCommand);
		}
	}

	private void CreateCommands()
	{
		for (int num = squad_commands.Count - 1; num >= 0; num--)
		{
			SquadCommand squadCommand = squad_commands[num];
			if (!squadCommand.Validate())
			{
				squadCommand.Reset();
				squad_commands.RemoveAt(num);
			}
		}
		CreateKeepFormationCommands();
		for (int i = 0; i < my_squads.Count; i++)
		{
			Squad squad = my_squads[i];
			if (GetRetreatCommand(squad) == null)
			{
				RetreatCommand item = new RetreatCommand(this, squad);
				squad_commands.Add(item);
			}
			if (battle.fortifications != null && GetCommand<DestroyWallCommand>(squad) == null && DestroyWallCommand.ValidateTarget(squad))
			{
				DestroyWallCommand destroyWallCommand = new DestroyWallCommand(this, squad);
				if (destroyWallCommand.Validate())
				{
					squad_commands.Add(destroyWallCommand);
				}
			}
			if (squad.def.is_siege_eq && GetCommand<FollowCommand>(squad) == null && FollowCommand.ValidateTarget(squad))
			{
				FollowCommand followCommand = new FollowCommand(this, squad);
				if (followCommand.Validate())
				{
					squad_commands.Add(followCommand);
				}
			}
			for (int j = 0; j < enemy_capture_points.Count; j++)
			{
				if (CaptureCommand.ValidateTarget(enemy_capture_points[j], enemy_capture_points[j].battle_side) && GetCaptureCommand(enemy_capture_points[j], squad) == null)
				{
					CaptureCommand item2 = new CaptureCommand(this, enemy_capture_points[j], squad);
					squad_commands.Add(item2);
				}
			}
		}
		for (int k = 0; k < enemy_squads.Count; k++)
		{
			if (AttackCommand.ValidateTarget(enemy_squads[k]) && GetAttackCommand(enemy_squads[k]) == null)
			{
				AttackCommand item3 = new AttackCommand(this, enemy_squads[k]);
				squad_commands.Add(item3);
			}
		}
		for (int l = 0; l < my_capture_points.Count; l++)
		{
			if (DefendCommand.ValidateTarget(my_capture_points[l], battle_side) && GetDefendCommand(my_capture_points[l]) == null)
			{
				DefendCommand item4 = new DefendCommand(this, my_capture_points[l]);
				squad_commands.Add(item4);
			}
		}
	}

	private void CreateKeepFormationCommands()
	{
		List<KeepFormationCommand> keepFormationCommands = GetKeepFormationCommands();
		bool flag = garrison == null;
		bool flag2 = first_army_kingdom == null || (IsOwner(first_army_kingdom) && !army1.act_separately);
		bool flag3 = second_army_kingdom == null || (IsOwner(second_army_kingdom) && !army2.act_separately) || (second_army_kingdom == first_army_kingdom && !army2.act_separately);
		KeepFormationCommand keepFormationCommand = null;
		KeepFormationCommand keepFormationCommand2 = null;
		KeepFormationCommand keepFormationCommand3 = null;
		for (int i = 0; i < keepFormationCommands.Count; i++)
		{
			if (!flag && keepFormationCommands[i].IsMain() && keepFormationCommands[i].owner == garrison.settlement)
			{
				flag = true;
				keepFormationCommand = keepFormationCommands[i];
				if (flag2 && flag3)
				{
					break;
				}
			}
			else if (!flag2 && keepFormationCommands[i].IsMain() && keepFormationCommands[i].owner == army1)
			{
				flag2 = true;
				keepFormationCommand2 = keepFormationCommands[i];
				if (flag && flag3)
				{
					break;
				}
			}
			else if (!flag3 && keepFormationCommands[i].IsMain() && keepFormationCommands[i].owner == army2)
			{
				flag3 = true;
				keepFormationCommand3 = keepFormationCommands[i];
				if (flag && flag2)
				{
					break;
				}
			}
		}
		if (!flag)
		{
			Army army = ((army1 != null && !army1.act_separately) ? army1 : null);
			PrepareKeepFormation(out var commander, out var target, out var formation, supporter: false, null, army, garrison);
			if (commander != null)
			{
				KeepFormationCommand item = new KeepFormationCommand(this, garrison.settlement, target, commander, formation, commander.GetKingdom(), def.keep_formation_defensive_mode, def.keep_formation_range_main, main: true);
				squad_commands.Add(item);
				keepFormationCommands.Add(item);
			}
		}
		else if (keepFormationCommand != null)
		{
			ValidateFormationCommands(keepFormationCommands, keepFormationCommand);
		}
		if (!flag2)
		{
			PrepareKeepFormation(out var commander2, out var target2, out var formation2, supporter: true, first_army_kingdom, army1);
			if (commander2 != null)
			{
				KeepFormationCommand item2 = new KeepFormationCommand(this, army1, target2, commander2, formation2, commander2.GetKingdom(), def.keep_formation_defensive_mode, def.keep_formation_range_main, main: true);
				squad_commands.Add(item2);
				keepFormationCommands.Add(item2);
			}
		}
		else if (keepFormationCommand2 != null)
		{
			ValidateFormationCommands(keepFormationCommands, keepFormationCommand2);
		}
		if (!flag3)
		{
			PrepareKeepFormation(out var commander3, out var target3, out var formation3, supporter: true, second_army_kingdom, army2);
			if (commander3 != null)
			{
				KeepFormationCommand item3 = new KeepFormationCommand(this, army2, target3, commander3, formation3, commander3.GetKingdom(), def.keep_formation_defensive_mode, def.keep_formation_range_main, main: true);
				squad_commands.Add(item3);
				keepFormationCommands.Add(item3);
			}
		}
		else if (keepFormationCommand3 != null)
		{
			ValidateFormationCommands(keepFormationCommands, keepFormationCommand3);
		}
		CreateLackingKeepFormationCommands(keepFormationCommands);
	}

	private void ValidateFormationCommands(List<KeepFormationCommand> kf_commands, KeepFormationCommand main_kf_command)
	{
		if (main_kf_command == null)
		{
			return;
		}
		Squad commander = main_kf_command.GetCommander();
		for (int num = kf_commands.Count - 1; num >= 0; num--)
		{
			KeepFormationCommand keepFormationCommand = kf_commands[num];
			Army army = keepFormationCommand.owner as Army;
			if (!keepFormationCommand.IsMain() && keepFormationCommand.Merge_with_main && keepFormationCommand.GetKingdom() == main_kf_command.GetKingdom() && ((keepFormationCommand.owner == main_kf_command.owner && (army == null || (army != null && army.act_separately))) || (army != null && !army.act_separately)))
			{
				Squad commander2 = keepFormationCommand.GetCommander();
				if (commander.position.Dist(commander2.position) < main_kf_command.Range)
				{
					keepFormationCommand.Reset();
					squad_commands.Remove(keepFormationCommand);
					kf_commands.RemoveAt(num);
				}
			}
		}
	}

	public bool HasOwnerFlag(Squad squad, EnableFlags flags)
	{
		if (squad.IsMercenary())
		{
			return HasMercenaryFlag(flags);
		}
		if (IsOwner(squad.GetKingdom()))
		{
			return HasOwnerFlag(flags);
		}
		if (IsSupporter(squad.GetKingdom()))
		{
			return HasSupporterFlag(flags);
		}
		if (IsSecondSupporter(squad.GetKingdom()))
		{
			return HasSecondSupporterFlag(flags);
		}
		return false;
	}

	private void CreateLackingKeepFormationCommands(List<KeepFormationCommand> kf_commands)
	{
		new List<Squad>();
		my_squads_sorted.Sort((Squad x, Squad y) => x.def.CTH.CompareTo(y.def.CTH));
		for (int num = my_squads_sorted.Count - 1; num >= 0; num--)
		{
			Squad squad = my_squads_sorted[num];
			if (!squad.def.is_siege_eq && HasOwnerFlag(squad, EnableFlags.Commands))
			{
				bool flag = squad.ai_command != null;
				if (!flag)
				{
					foreach (KeepFormationCommand kf_command in kf_commands)
					{
						if (kf_command.Validate(squad))
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					Squad commander = squad;
					PrepareKeepFormationAdditional(kf_commands, squad.GetKingdom(), ref commander, out var target, out var formation);
					MapObject mapObject = commander.simulation.army;
					if (mapObject == null)
					{
						mapObject = garrison.settlement;
					}
					KeepFormationCommand item = new KeepFormationCommand(this, mapObject, target, commander, formation, squad.GetKingdom(), def.keep_formation_defensive_mode, def.keep_formation_range_sub, main: false, merge_with_main: true);
					squad_commands.Add(item);
					kf_commands.Add(item);
				}
			}
		}
	}

	private void PrepareKeepFormation(out Squad commander, out MapObject target, out StandardArmyFormation formation, bool supporter = false, Kingdom kingdom = null, Army army = null, Garrison garrison = null)
	{
		bool flag = false;
		if (!supporter)
		{
			if (army != null)
			{
				commander = GetCommander(battle_side, army);
			}
			else if (garrison != null)
			{
				commander = GetCommander(battle_side, garrison);
			}
			else
			{
				commander = GetCommander(battle_side);
			}
			if (commander != null)
			{
				target = GetNearestEnemyCapturePoint(commander.position);
			}
			else
			{
				target = enemy_capture_points[0];
			}
			if (commander != null && commander.simulation.army != null && commander.simulation.army.battleview_army_formation != null)
			{
				formation = commander.simulation.army.battleview_army_formation;
				flag = true;
			}
			else
			{
				formation = selected_army_formation;
			}
		}
		else
		{
			if (army != null)
			{
				commander = GetCommander(battle_side, army);
			}
			else if (garrison != null)
			{
				commander = GetCommander(battle_side, garrison);
			}
			else
			{
				commander = GetCommander(battle_side, kingdom);
			}
			if (commander != null)
			{
				target = GetNearestEnemyCapturePoint(commander.position);
			}
			else
			{
				target = enemy_capture_points[enemy_capture_points.Count - 1];
			}
			if (commander != null && commander.simulation.army != null && commander.simulation.army.battleview_army_formation != null)
			{
				formation = commander.simulation.army.battleview_army_formation;
				flag = true;
			}
			else
			{
				formation = selected_army_formation;
			}
		}
		if (commander == null || !IsSiege())
		{
			return;
		}
		Squad siegeEqSquad = GetSiegeEqSquad(battle_side);
		if (siegeEqSquad != null && battle.type == Battle.Type.Assault && my_gates == 0 && !battle.fortification_destroyed)
		{
			commander = siegeEqSquad;
			target = null;
			formation = new StandardArmyFormation(10f, 0f, 0f, 2f);
			formation.settings.reserve_line_offset = -20f;
		}
		else if (enemy_capture_points != null && enemy_capture_points.Count > 0 && my_gates == 0 && !battle.fortification_destroyed && battle.type == Battle.Type.Assault)
		{
			CapturePoint capturePoint = null;
			float num = float.MaxValue;
			for (int i = 0; i < enemy_capture_points.Count; i++)
			{
				CapturePoint capturePoint2 = enemy_capture_points[i];
				if (capturePoint2.fortification != null && capturePoint2.fortification.def.type == Fortification.Type.Gate)
				{
					float num2 = commander.position.Dist(capturePoint2.position);
					if (num2 < num)
					{
						capturePoint = capturePoint2;
						num = num2;
					}
				}
			}
			target = capturePoint;
		}
		else if (battle.type == Battle.Type.BreakSiege && battle_side == 0)
		{
			target = null;
			if (!flag)
			{
				formation = new StandardArmyFormation(StandardArmyFormation.ArmyFormationType.TwoLinesAndReserve);
			}
		}
		if (battle.type == Battle.Type.Assault && battle_side == 1 && (commander == null || commander.is_inside_walls_or_on_walls))
		{
			target = null;
			if (!flag)
			{
				formation = new StandardArmyFormation(StandardArmyFormation.ArmyFormationType.OneLine);
			}
		}
	}

	private CapturePoint GetNearestEnemyCapturePoint(Point position)
	{
		float num = float.MaxValue;
		CapturePoint result = null;
		foreach (CapturePoint enemy_capture_point in enemy_capture_points)
		{
			float num2 = enemy_capture_point.position.Dist(position);
			if (num2 < num)
			{
				num = num2;
				result = enemy_capture_point;
			}
		}
		return result;
	}

	private void PrepareKeepFormationAdditional(List<KeepFormationCommand> kf_commands, Kingdom kingdom, ref Squad commander, out MapObject target, out StandardArmyFormation formation)
	{
		if (commander != null)
		{
			target = GetNearestEnemySquad(commander.position);
		}
		else
		{
			target = GetCommander(1 - battle_side);
		}
		formation = new StandardArmyFormation(StandardArmyFormation.ArmyFormationType.TwoLines);
		if (kf_commands.Count > 0)
		{
			for (int i = 0; i < kf_commands.Count; i++)
			{
				if (kf_commands[i].IsMain() && kf_commands[i].GetKingdom() == kingdom)
				{
					Squad formationCommander = kf_commands[i].GetFormationCommander();
					if (formationCommander != null && !formationCommander.def.is_siege_eq)
					{
						target = formationCommander;
						return;
					}
				}
			}
		}
		Squad squad = null;
		for (int j = 0; j < my_squads.Count; j++)
		{
			Squad squad2 = my_squads[j];
			if (!squad2.IsDefeated() && squad2 != commander)
			{
				if (squad2.def.type == Unit.Type.Noble)
				{
					target = squad2;
					return;
				}
				if (squad == null || squad.def.CTH < squad2.def.CTH)
				{
					squad = squad2;
				}
			}
		}
		target = squad;
	}

	private void CalcPriorities()
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			float cached_priority = squad_commands[i].Priority();
			squad_commands[i].cached_priority = cached_priority;
		}
	}

	public List<KeepFormationCommand> GetKeepFormationCommands()
	{
		List<KeepFormationCommand> list = new List<KeepFormationCommand>();
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is KeepFormationCommand item)
			{
				list.Add(item);
			}
		}
		return list;
	}

	private KeepFormationCommand GetMainKeepFormationCommand(MapObject owner)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is KeepFormationCommand keepFormationCommand && keepFormationCommand.IsMain() && keepFormationCommand.GetCommander() != null && keepFormationCommand.owner == owner)
			{
				return keepFormationCommand;
			}
		}
		return null;
	}

	private KeepFormationCommand GetKeepFormationCommand(MapObject target)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is KeepFormationCommand keepFormationCommand && keepFormationCommand.target == target)
			{
				return keepFormationCommand;
			}
		}
		return null;
	}

	private AttackCommand GetAttackCommand(MapObject target)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is AttackCommand attackCommand && attackCommand.target == target)
			{
				return attackCommand;
			}
		}
		return null;
	}

	private RetreatCommand GetRetreatCommand(Squad target)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is RetreatCommand retreatCommand && retreatCommand.target == target)
			{
				return retreatCommand;
			}
		}
		return null;
	}

	private FollowCommand GetFollowCommand(Squad target)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is FollowCommand followCommand && followCommand.target == target)
			{
				return followCommand;
			}
		}
		return null;
	}

	private CaptureCommand GetCaptureCommand(CapturePoint capture_point, Squad target)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is CaptureCommand captureCommand && captureCommand.target_capture_point == capture_point && captureCommand.target == target)
			{
				return captureCommand;
			}
		}
		return null;
	}

	private DefendCommand GetDefendCommand(CapturePoint target)
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is DefendCommand defendCommand && defendCommand.target == target)
			{
				return defendCommand;
			}
		}
		return null;
	}

	private T GetCommand<T>(MapObject target) where T : SquadCommand
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			if (squad_commands[i] is T val && val.target == target)
			{
				return val;
			}
		}
		return null;
	}

	private void SortCommands()
	{
		squad_commands = squad_commands.OrderByDescending((SquadCommand x) => x.cached_priority).ToList();
	}

	private void CalcMarshalSafety()
	{
		can_kill_our_marshals = false;
		float num = 0f;
		float num2 = 0f;
		Squad squad = null;
		for (int i = 0; i < my_squads.Count; i++)
		{
			Squad squad2 = my_squads[i];
			if (squad2 == null || !squad2.is_main_squad || (squad2.def.type == Unit.Type.Noble && !squad2.IsDefeated()))
			{
				num += (float)squad2.NumTroops() * (1f + squad2.def.defense / 100f);
				squad = squad2;
			}
		}
		if (squad == null)
		{
			return;
		}
		for (int j = 0; j < enemy_squads.Count; j++)
		{
			Squad squad3 = enemy_squads[j];
			if (squad3 != null && squad3.is_main_squad && !squad3.IsDefeated())
			{
				num2 += (float)squad3.NumTroops() * squad3.simulation.unit.CTH_modified(squad.simulation) / 100f;
			}
		}
		float num3 = num2 / num;
		can_kill_our_marshals = num3 > 0.5f;
	}

	private void AssignSquads()
	{
		int num = 0;
		CalcMarshalSafety();
		int num2 = 0;
		while (num2 < squad_commands.Count && my_squads_sorted.Count > 0)
		{
			SquadCommand command = squad_commands[num2];
			if (command.SingleSquad())
			{
				Squad squad = command.target as Squad;
				command.AddSquad(squad);
			}
			else
			{
				try
				{
					my_squads_sorted.Sort((Squad x, Squad y) => command.Priority(x).CompareTo(command.Priority(y)));
				}
				catch
				{
				}
				bool flag = false;
				int num3 = num;
				for (int num4 = my_squads_sorted.Count - 1 - num3; num4 >= 0; num4--)
				{
					Squad squad2 = my_squads_sorted[num4];
					SquadCommand ai_command = squad2.ai_command;
					bool flag2 = ai_command == command;
					bool flag3 = ai_command?.adding_squad_lowers_priority ?? false;
					if (command.AddSquad(squad2))
					{
						if (!flag2 && flag3)
						{
							ai_command.cached_priority += def.drop_per_squad;
							flag = true;
						}
						if (!flag2 && command.adding_squad_lowers_priority)
						{
							command.cached_priority -= def.drop_per_squad;
							flag = true;
						}
					}
					else if (flag2 && flag3)
					{
						ai_command.cached_priority += def.drop_per_squad;
						flag = true;
					}
					if (flag)
					{
						SortCommands();
						break;
					}
				}
				if (flag)
				{
					num++;
					continue;
				}
			}
			num = 0;
			num2++;
		}
	}

	private void ExecuteCommands()
	{
		for (int i = 0; i < squad_commands.Count; i++)
		{
			squad_commands[i].Execute();
		}
	}

	private void UpdateFormationSettings()
	{
		foreach (Squad my_squad in my_squads)
		{
			if (my_squad == null || my_squad.IsDefeated() || my_squad.is_fleeing || !my_squad.is_main_squad || my_squad.GetKingdom().is_player)
			{
				continue;
			}
			float num = 0f;
			num = my_squad.def.tier switch
			{
				0 => def.low_tier_formation_change_chance, 
				1 => def.mid_tier_formation_change_chance, 
				_ => def.high_tier_formation_change_chance, 
			};
			if (UnityEngine.Random.value > num)
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			if (my_squad.enemy_shooting_squads.Count > 0)
			{
				flag = true;
			}
			else if (!my_squad.def.is_cavalry && my_squad.CavalryNearby())
			{
				flag3 = true;
			}
			else
			{
				flag2 = true;
			}
			if (my_squad.def.is_cavalry && (!my_squad.def.is_ranged || my_squad.salvos_left <= 0))
			{
				if (((my_squad.command == Squad.Command.Attack || my_squad.command == Squad.Command.Charge) && my_squad.target != null) || (my_squad.ai_command != null && my_squad.ai_command is AttackCommand))
				{
					flag4 = true;
				}
				else
				{
					flag5 = true;
				}
			}
			else
			{
				flag5 = true;
			}
			if (flag)
			{
				my_squad.SetSpacing(Squad.Spacing.Expanded);
			}
			else if (flag3)
			{
				my_squad.SetSpacing(Squad.Spacing.Shrinken);
			}
			else if (flag2)
			{
				my_squad.SetSpacing(Squad.Spacing.Default);
			}
			if (flag4)
			{
				my_squad.SetFormation("Triangle");
			}
			else if (flag5)
			{
				if (!my_squad.def.is_defense)
				{
					my_squad.SetFormation("Rect");
				}
				else
				{
					my_squad.SetFormation("Checkerboard");
				}
			}
		}
	}

	public void SetOwner(Kingdom owner)
	{
		this.owner = owner;
		if (owner != null)
		{
			owner_enabled = (owner.is_player ? player_flags : EnableFlags.All);
		}
	}

	public void SetSupporter(Kingdom supporter)
	{
		this.supporter = supporter;
		if (supporter != null)
		{
			supporter_enabled = (supporter.is_player ? player_flags : EnableFlags.All);
		}
	}

	public void SetSecondSupporter(Kingdom second_supporter)
	{
		this.second_supporter = second_supporter;
		if (second_supporter != null)
		{
			second_supporter_enabled = (second_supporter.is_player ? player_flags : EnableFlags.All);
		}
	}

	public void SetGarrisonKingdom(Kingdom kingdom)
	{
		garrison_kingdom = kingdom;
	}

	public void SetFirstArmyKingdom(Kingdom kingdom)
	{
		first_army_kingdom = kingdom;
	}

	public void SetSecondArmyKingdom(Kingdom kingdom)
	{
		second_army_kingdom = kingdom;
	}

	public void AddArmy(Army army)
	{
		if (army1 == null)
		{
			army1 = army;
			SetFirstArmyKingdom(army.GetKingdom());
		}
		else if (army2 == null)
		{
			army2 = army;
			SetSecondArmyKingdom(army.GetKingdom());
		}
	}

	public float GetThreat(Squad squad, bool interpolated = false)
	{
		if (battle.power_grids == null)
		{
			return 0f;
		}
		if (interpolated)
		{
			return battle.power_grids[squad.battle_side].GetBaseThreatInterpolated(squad.position.x, squad.position.y);
		}
		return battle.power_grids[squad.battle_side].GetBaseThreat(squad.position.x, squad.position.y);
	}

	public float GetThreat(PPos position, int battle_side, bool interpolated = false)
	{
		if (battle.power_grids == null)
		{
			return 0f;
		}
		if (interpolated)
		{
			return battle.power_grids[battle_side].GetBaseThreatInterpolated(position.x, position.y);
		}
		return battle.power_grids[battle_side].GetBaseThreat(position.x, position.y);
	}

	public float GetMaxThreat(PPos position, float radius, int battle_side, bool interpolated = false)
	{
		if (battle.power_grids == null)
		{
			return 0f;
		}
		float num = 0f;
		for (float num2 = position.x - radius; num2 <= position.x + radius; num2 += (float)battle.power_grids[battle_side].tile_size_x)
		{
			if (num2 < 0f)
			{
				continue;
			}
			for (float num3 = position.y - radius; num3 <= position.y + radius; num3 += (float)battle.power_grids[battle_side].tile_size_y)
			{
				if (!(num3 < 0f))
				{
					float num4 = 0f;
					num4 = ((!interpolated) ? battle.power_grids[battle_side].GetBaseThreat(num2, num3) : battle.power_grids[battle_side].GetBaseThreatInterpolated(num2, num3));
					if (num4 > num)
					{
						num = num4;
					}
				}
			}
		}
		return num;
	}

	public float GetEnemyThreat(Squad squad, bool check_anti_cavalry = false, bool check_infantry = false)
	{
		if (battle.power_grids == null)
		{
			return 0f;
		}
		float baseThreat = battle.power_grids[1 - squad.battle_side].GetBaseThreat(squad.position.x, squad.position.y);
		if (check_anti_cavalry)
		{
			return baseThreat + battle.power_grids[1 - squad.battle_side].GetAntiCavalryThreat(squad.position.x, squad.position.y);
		}
		if (check_infantry)
		{
			return baseThreat + battle.power_grids[1 - squad.battle_side].GetInfantryThreat(squad.position.x, squad.position.y);
		}
		return baseThreat;
	}

	public float GetAntiCavalryThreat(Squad squad)
	{
		if (battle.power_grids == null)
		{
			return 0f;
		}
		return battle.power_grids[squad.battle_side].GetAntiCavalryThreat(squad.position.x, squad.position.y);
	}

	public float GetInfantryThreat(Squad squad, float x = 0f, float y = 0f)
	{
		if (battle.power_grids == null)
		{
			return 0f;
		}
		return battle.power_grids[squad.battle_side].GetInfantryThreat(squad.position.x + x, squad.position.y + y);
	}

	public Squad GetCommander(int side)
	{
		List<Squad> obj = ((side == battle_side) ? my_squads : enemy_squads);
		Squad squad = null;
		foreach (Squad item in obj)
		{
			if (item != null && item.IsValid() && !item.IsDefeated() && (side != battle_side || IsOwner(item.GetKingdom())))
			{
				if (item.def.type == Unit.Type.Noble)
				{
					return item;
				}
				if (squad == null || squad.def.CTH < item.def.CTH)
				{
					squad = item;
				}
			}
		}
		return squad;
	}

	public Squad GetCommander(int side, Garrison garrison)
	{
		List<Squad> obj = ((side == battle_side) ? my_squads : enemy_squads);
		Squad squad = null;
		foreach (Squad item in obj)
		{
			if (item != null && item.IsValid() && !item.IsDefeated() && item.simulation.garrison == garrison)
			{
				if (item.def.type == Unit.Type.Noble)
				{
					return item;
				}
				if (squad == null || squad.def.CTH < item.def.CTH)
				{
					squad = item;
				}
			}
		}
		return squad;
	}

	public Squad GetCommander(int side, Army army)
	{
		List<Squad> obj = ((side == battle_side) ? my_squads : enemy_squads);
		Squad squad = null;
		foreach (Squad item in obj)
		{
			if (item != null && item.IsValid() && !item.IsDefeated() && item.simulation.army == army)
			{
				if (item.def.type == Unit.Type.Noble)
				{
					return item;
				}
				if (squad == null || squad.def.CTH < item.def.CTH)
				{
					squad = item;
				}
			}
		}
		return squad;
	}

	public Squad GetCommander(int side, Kingdom kingdom)
	{
		List<Squad> obj = ((side == battle_side) ? my_squads : enemy_squads);
		Squad squad = null;
		foreach (Squad item in obj)
		{
			if (item != null && item.IsValid() && !item.IsDefeated() && item.GetKingdom() == kingdom)
			{
				if (item.def.type == Unit.Type.Noble)
				{
					return item;
				}
				if (squad == null || squad.def.CTH < item.def.CTH)
				{
					squad = item;
				}
			}
		}
		return squad;
	}

	public Squad GetSiegeEqSquad(int battle_side)
	{
		foreach (Squad item in (battle_side == this.battle_side) ? my_squads : enemy_squads)
		{
			if (!item.IsDefeated() && item.def.is_siege_eq)
			{
				return item;
			}
		}
		return null;
	}

	public bool IsCastleOpenToEnter()
	{
		if (((battle_side == 0) ? my_gates : enemy_gates) <= 0)
		{
			return battle.fortification_destroyed;
		}
		return true;
	}

	public Squad GetNearestEnemySquad(PPos pos)
	{
		if (enemy_squads == null || enemy_squads.Count == 0)
		{
			return null;
		}
		Squad squad = enemy_squads[0];
		float num = pos.SqrDist(squad.position);
		for (int i = 1; i < enemy_squads.Count; i++)
		{
			Squad squad2 = enemy_squads[i];
			if (pos.SqrDist(squad2.position) < num)
			{
				squad = squad2;
				num = pos.SqrDist(squad.position);
			}
		}
		return squad;
	}

	public void CalculateDirection()
	{
		my_camps_avg_pos = default(PPos);
		for (int i = 0; i < my_capture_points.Count; i++)
		{
			my_camps_avg_pos += my_capture_points[i].position;
		}
		my_camps_avg_pos /= (float)my_capture_points.Count;
		enemy_camps_avg_pos = default(PPos);
		for (int j = 0; j < enemy_capture_points.Count; j++)
		{
			enemy_camps_avg_pos += enemy_capture_points[j].position;
		}
		enemy_camps_avg_pos /= (float)enemy_capture_points.Count;
		ref_direction = enemy_camps_avg_pos - my_camps_avg_pos;
		if (ref_direction.SqrLength() > 0f)
		{
			ref_direction.Normalize();
		}
	}

	public void UpdateGroups()
	{
		groups = new List<SquadGroup>();
		Squad commander = GetCommander(battle_side);
		if (commander == null)
		{
			return;
		}
		List<Squad> list = new List<Squad>(my_squads);
		int num = list.Count - 1;
		int num2 = list.Count - 1;
		int num3 = 0;
		while (list.Count > 0)
		{
			Squad squad = ((num2 == num) ? commander : list[list.Count - 1]);
			groups.Add(new SquadGroup(num3));
			List<Squad> list2 = new List<Squad> { squad };
			groups[num3].AddSquad(squad);
			list.Remove(squad);
			while (list2.Count > 0)
			{
				List<Squad> list3 = new List<Squad>(list2);
				list2.Clear();
				for (int num4 = list.Count - 1; num4 >= 0; num4--)
				{
					Squad squad2 = list[num4];
					foreach (Squad item in list3)
					{
						if (squad2.position.Dist(item.position) < def.treat_like_group_distance && squad2.kingdom_id == item.kingdom_id)
						{
							groups[num3].AddSquad(squad2);
							list.Remove(squad2);
							list2.Add(squad2);
							break;
						}
					}
				}
			}
			num2 = list.Count - 1;
			num3++;
		}
	}

	public void UpdateEnemyGroups()
	{
		if (battle == null || battle.ai.Length < 2)
		{
			return;
		}
		enemy_groups = battle.ai[1 - battle_side].groups;
		if (enemy_groups != null)
		{
			return;
		}
		enemy_groups = new List<SquadGroup>();
		Squad commander = GetCommander(1 - battle_side);
		List<Squad> list = new List<Squad>(enemy_squads);
		int num = list.Count - 1;
		int num2 = list.Count - 1;
		int num3 = 0;
		while (list.Count > 0)
		{
			Squad squad = ((num2 == num) ? commander : list[list.Count - 1]);
			enemy_groups.Add(new SquadGroup(num3));
			List<Squad> list2 = new List<Squad> { squad };
			enemy_groups[num3].AddSquad(squad);
			list.Remove(squad);
			while (list2.Count > 0)
			{
				List<Squad> list3 = new List<Squad>(list2);
				list2.Clear();
				for (int num4 = list.Count - 1; num4 >= 0; num4--)
				{
					Squad squad2 = list[num4];
					foreach (Squad item in list3)
					{
						if (squad2.position.Dist(item.position) < def.treat_like_group_distance)
						{
							enemy_groups[num3].AddSquad(squad2);
							list.Remove(squad2);
							list2.Add(squad2);
							break;
						}
					}
				}
			}
			num2 = list.Count - 1;
			num3++;
		}
	}

	public int GetSquadGroupCount(Squad squad)
	{
		if (squad.battle_side == battle_side)
		{
			foreach (SquadGroup group in groups)
			{
				if (group.ContainsSquad(squad))
				{
					return group.squads.Count();
				}
			}
		}
		else
		{
			foreach (SquadGroup enemy_group in enemy_groups)
			{
				if (enemy_group.ContainsSquad(squad))
				{
					return enemy_group.squads.Count();
				}
			}
		}
		return 0;
	}

	public SquadGroup GetFurthestEnemyGroup(PPos direction)
	{
		SquadGroup result = enemy_groups[0];
		PPos pPos = enemy_groups[0].MaxPoint(direction);
		for (int i = 1; i < enemy_groups.Count; i++)
		{
			PPos pPos2 = groups[i].MaxPoint(direction);
			if (direction.Dot(pPos2 - pPos) > 0f)
			{
				pPos = pPos2;
				result = enemy_groups[i];
			}
		}
		return result;
	}

	public Squad GetFurthestSquad(PPos direction)
	{
		return GetFurthestGroup(direction).GetFurthestSquad(direction);
	}

	public Squad GetFurthestEnemySquad(PPos direction)
	{
		return GetFurthestEnemyGroup(direction).GetFurthestSquad(direction);
	}

	public void FlankManeuver()
	{
		CreateFlankingFormation();
	}

	public bool HasFlankingSense()
	{
		if ((flanking && second_flanking) || (float)my_squads.Count < (float)enemy_squads.Count * (1f - def.flanking_chance))
		{
			return false;
		}
		return true;
	}

	public SquadGroup GetFurthestGroup(PPos direction)
	{
		SquadGroup result = groups[0];
		PPos pPos = groups[0].MaxPoint(direction);
		for (int i = 1; i < groups.Count; i++)
		{
			PPos pPos2 = groups[i].MaxPoint(direction);
			if (direction.Dot(pPos2 - pPos) > 0f)
			{
				pPos = pPos2;
				result = groups[i];
			}
		}
		return result;
	}

	public void OnSupportersJoinedMainArmy(bool ignore_right)
	{
		GetMainKeepFormationCommand(army1).OnSupportersJoinedDefensivePosition(ignore_right);
	}

	public void CreateFlankingFormation()
	{
		KeepFormationCommand mainKeepFormationCommand = GetMainKeepFormationCommand(army1);
		if (mainKeepFormationCommand == null)
		{
			return;
		}
		List<Squad> squads = new List<Squad>(mainKeepFormationCommand.squads);
		Squad commander = GetCommander(1 - battle_side);
		int num = (int)((1f - (float)GetSquadGroupCount(commander) / (float)enemy_squads.Count) * (float)squads.Count + (float)UnityEngine.Random.Range(0, squads.Count / 3));
		if (num >= my_squads.Count - 1)
		{
			num = my_squads.Count - 2;
		}
		if (num < 2 || mainKeepFormationCommand == null)
		{
			return;
		}
		for (int num2 = squads.Count - 1; num2 >= 0; num2--)
		{
			Squad squad = squads[num2];
			if (!squad.def.is_cavalry || squad.def.type == Unit.Type.Noble || squad.def.secondary_type == Unit.Type.Noble)
			{
				squads.Remove(squad);
			}
		}
		if (squads.Count <= 0)
		{
			return;
		}
		squads.OrderBy((Squad s) => s.position.Dist(squads[0].position));
		for (int num3 = squads.Count - 1; num3 >= 0; num3--)
		{
			Squad squad2 = squads[num3];
			if (squads.Count > num || squad2.position.Dist(squads[0].position) > def.treat_like_group_distance)
			{
				squads.Remove(squad2);
			}
		}
		num = squads.Count;
		if (squads.Count <= 0)
		{
			return;
		}
		Squad squad3 = squads[0];
		MapObject nearestEnemyCapturePoint = GetNearestEnemyCapturePoint(squad3.position);
		if (nearestEnemyCapturePoint == null)
		{
			return;
		}
		MapObject mapObject = squad3.simulation.army;
		if (mapObject == null)
		{
			mapObject = garrison.settlement;
		}
		KeepFormationCommand keepFormationCommand = new KeepFormationCommand(this, mapObject, nearestEnemyCapturePoint, squad3, new StandardArmyFormation(StandardArmyFormation.ArmyFormationType.OneLine), squad3.GetKingdom(), defensive_mode: false, def.keep_formation_range_sub, main: false, merge_with_main: false, num, avoid_enemies: true);
		foreach (Squad item in squads)
		{
			mainKeepFormationCommand.RemoveSquad(item);
			keepFormationCommand.AddSquad(item);
		}
		squad_commands.Add(keepFormationCommand);
		if (!flanking)
		{
			flanking = true;
		}
		else
		{
			second_flanking = true;
		}
	}

	public void OnKeepFormationCheckpoint()
	{
		if (IsSiege())
		{
			return;
		}
		Squad commander = GetCommander(1 - battle_side);
		KeepFormationCommand keepFormationCommand = GetKeepFormationCommand(commander);
		if (keepFormationCommand == null)
		{
			if (HasSplitingSense())
			{
				SplitForceToAttackNobleman();
			}
		}
		else
		{
			AdjustSplitForceSize(keepFormationCommand, commander);
		}
		if (HasFlankingSense() && UnityEngine.Random.Range(0f, 1f) < def.flanking_chance)
		{
			FlankManeuver();
		}
	}

	public void LeaveCampGuards()
	{
		if (guard_left || my_squads.Count - 1 <= enemy_squads.Count)
		{
			return;
		}
		List<Squad> squads = GetMainKeepFormationCommand(army1).squads;
		List<Squad> list = new List<Squad>();
		int num = my_squads.Count - enemy_squads.Count - 1;
		num = ((squads.Count < num) ? squads.Count : num);
		int num2 = UnityEngine.Random.Range(1, num);
		List<Squad> list2 = new List<Squad>();
		foreach (Squad item in squads)
		{
			if (item.def.is_cavalry)
			{
				continue;
			}
			if (item.def.is_defense || item.def.is_ranged)
			{
				list.Add(item);
				if (list.Count == num2)
				{
					break;
				}
			}
			else
			{
				list2.Add(item);
			}
		}
		if (list.Count < num2)
		{
			foreach (Squad item2 in list2)
			{
				list.Add(item2);
				if (list.Count == num2)
				{
					break;
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		MapObject mapObject = list[0].simulation.army;
		if (mapObject == null)
		{
			mapObject = garrison.settlement;
		}
		KeepFormationCommand keepFormationCommand = new KeepFormationCommand(this, mapObject, GetCommander(1 - battle_side), list[0], new StandardArmyFormation(StandardArmyFormation.ArmyFormationType.TwoLinesRangedAtFront), list[0].GetKingdom(), defensive_mode: true, def.keep_formation_range_sub);
		foreach (Squad item3 in list)
		{
			item3.ai_command.RemoveSquad(item3);
			keepFormationCommand.AddSquad(item3);
		}
		squad_commands.Add(keepFormationCommand);
		guard_left = true;
	}

	public bool HasSplitingSense()
	{
		KeepFormationCommand mainKeepFormationCommand = GetMainKeepFormationCommand(army1);
		if (mainKeepFormationCommand?.target == null)
		{
			return false;
		}
		if (GetCommander(battle_side) != mainKeepFormationCommand.GetCommander())
		{
			return false;
		}
		Squad commander = GetCommander(1 - battle_side);
		if (commander == null)
		{
			return false;
		}
		PPos pt = commander.position - my_camps_avg_pos;
		if (pt.SqrLength() > 0f)
		{
			pt.Normalize();
			PPos pt2 = commander.position - enemy_camps_avg_pos;
			if (pt2.SqrLength() > 0f)
			{
				pt2.Normalize();
				if (ref_direction.Dot(pt) < 0f && ref_direction.Dot(pt2) > 0f)
				{
					return false;
				}
				if ((float)GetSquadGroupCount(commander) > (float)enemy_squads.Count * 0.3f)
				{
					if (mainKeepFormationCommand == null)
					{
						return false;
					}
					Squad commander2 = mainKeepFormationCommand.GetCommander();
					if (commander2 == null)
					{
						return false;
					}
					PPos position = commander2.position;
					PPos normalized = (mainKeepFormationCommand.target.position - position).GetNormalized();
					PPos normalized2 = (commander.position - position).GetNormalized();
					if (normalized.Dot(normalized2) < 0.6f)
					{
						return true;
					}
				}
				return false;
			}
			return false;
		}
		return false;
	}

	public void SplitForceToAttackNobleman()
	{
		Squad enemy_commander = GetCommander(1 - battle_side);
		int num = (int)((float)GetSquadGroupCount(enemy_commander) / (float)enemy_squads.Count * (float)my_squads.Count + (float)UnityEngine.Random.Range(0, my_squads.Count / 10));
		if (num >= my_squads.Count - 1)
		{
			num = my_squads.Count - 2;
		}
		if (num < 2)
		{
			return;
		}
		KeepFormationCommand mainKeepFormationCommand = GetMainKeepFormationCommand(army1);
		if (mainKeepFormationCommand != null)
		{
			Squad commander = GetCommander(battle_side);
			mainKeepFormationCommand.RemoveSquad(commander);
			MapObject mapObject = commander.simulation.army;
			if (mapObject == null)
			{
				mapObject = garrison.settlement;
			}
			KeepFormationCommand keepFormationCommand = new KeepFormationCommand(this, mapObject, enemy_commander, commander, new StandardArmyFormation(StandardArmyFormation.ArmyFormationType.TwoLinesRangedAtFront), commander.GetKingdom(), defensive_mode: false, def.keep_formation_range_sub, main: false, merge_with_main: false, num);
			keepFormationCommand.AddSquad(commander);
			squad_commands.Add(keepFormationCommand);
			List<Squad> list = new List<Squad>(mainKeepFormationCommand.squads);
			list.OrderBy((Squad s) => s.position.Dist(enemy_commander.position));
			for (int num2 = 0; num2 < num - 1 && num2 < list.Count; num2++)
			{
				Squad squad = list[num2];
				mainKeepFormationCommand.RemoveSquad(squad);
				keepFormationCommand.AddSquad(squad);
			}
		}
	}

	public void AdjustSplitForceSize(KeepFormationCommand command, Squad enemy_commander)
	{
		PPos pt = enemy_commander.position - my_camps_avg_pos;
		if (!(pt.SqrLength() > 0f))
		{
			return;
		}
		pt.Normalize();
		PPos pt2 = enemy_commander.position - enemy_camps_avg_pos;
		if (pt2.SqrLength() > 0f)
		{
			pt2.Normalize();
			if (command.GetCommander().position.Dist(enemy_commander.position) > 2f * def.max_attack_dist && ref_direction.Dot(pt) < 0f && ref_direction.Dot(pt2) > 0f)
			{
				command.SetSquadsLimit(0);
			}
			else if (command.GetSquadsLimit() > GetSquadGroupCount(enemy_commander) / enemy_squads.Count * my_squads.Count + my_squads.Count / 10)
			{
				command.SetSquadsLimit(GetSquadGroupCount(enemy_commander) / enemy_squads.Count * my_squads.Count + my_squads.Count / 10);
			}
		}
	}

	public bool IsInTowersRange(PPos pos, float additional_dist = 0f)
	{
		if (battle.fortifications != null)
		{
			for (int i = 0; i < battle.fortifications.Count; i++)
			{
				Fortification fortification = battle.fortifications[i];
				if (!fortification.IsDefeated() && fortification.battle_side != battle_side && fortification.shoot_comp != null && pos.Dist(fortification.position) < fortification.shoot_comp.salvo_def.max_shoot_range + additional_dist)
				{
					return true;
				}
			}
		}
		return false;
	}
}
