using System;
using System.Collections.Generic;

namespace Logic;

public class BattleSimulation : Component
{
	public class Def : Logic.Def
	{
		public class MoraleFactor
		{
			public DT.Field field;

			public bool can_be_initial;
		}

		public struct StateInfo
		{
			public struct Chance
			{
				public int level;

				public float chance;
			}

			public float duration;

			public float duration_battleview;

			public Chance[] chance;

			public int id;

			public float morale_on_finish;

			public float morale_buff_nearby_enemies;

			public float morale_buff_nearby_enemies_range;

			public int apply_on_enemies_min;

			public int apply_on_enemies_max;

			public float morale_buff_nearby_friends;

			public float morale_buff_nearby_friends_range;

			public int apply_on_friends_min;

			public int apply_on_friends_max;

			public float initiative_my_side;

			public float initiative_enemy_side;

			public float GetChance(float level, Squad squad = null)
			{
				float num = GetChance(chance, level);
				if (squad?.def?.state_mods != null)
				{
					num *= squad.def.state_mods[id];
				}
				return num;
			}

			public static float GetChance(Chance[] chance, float level)
			{
				if (chance.Length == 0)
				{
					return 0f;
				}
				if (chance.Length == 1)
				{
					return chance[0].chance;
				}
				int num = chance.Length - 1;
				int num2 = chance.Length - 1;
				while (num2 >= 0 && !((float)chance[num2].level < level))
				{
					num = num2;
					num2--;
				}
				return chance[num].chance;
			}
		}

		public float min_distance_between_camps = 300f;

		public float max_distance_between_camps = 400f;

		public float army_offset_from_camp = 30f;

		public float break_siege_distance_to_camps = 300f;

		public float global_move_speed_mod = 1f;

		public float global_cth_mod = 0.01f;

		public float global_chance_to_shock_mod = 1f;

		public float defense_against_trample_mod = 2f;

		public float avoid_trample_tight_formation_mod = 2f;

		public float CTH_against_cav_charge_mod = 2f;

		public float castle_defenders_mod = 1f;

		public float min_castle_defender_bonus = 0.1f;

		public float castle_defenders_bonus_ranged = 0.5f;

		public float gate_assaulted_defense_mod = 0.5f;

		public float initiative_cooldown = 15f;

		public float initiative_countdown = 15f;

		public float morale_drop_from_initiative = -3f;

		public float aggressive_base_flank_angle;

		public float aggressive_additional_flank_angle_cavalry;

		public float aggressive_additional_flank_angle_infantry;

		public float defensive_base_flank_angle;

		public float defensive_additional_flank_angle_cavalry;

		public float defensive_additional_flank_angle_infantry;

		public float threat_additional_side_dist = 40f;

		public float morale_flank_map_min = -100f;

		public float morale_flank_map_max = 100f;

		public float morale_flank_min = 3f;

		public float morale_flank_max = -3f;

		public float threat_additional_rear_dist = 40f;

		public float morale_flank_rear_map_min = -100f;

		public float morale_flank_rear_map_max = 100f;

		public float morale_flank_rear_min = 3f;

		public float morale_flank_rear_max = -3f;

		public float morale_from_casualties = -15f;

		public float max_tick_time = 3f;

		public float max_tick_time_battleview = 1f;

		public float range_loss_per_round = 20f;

		public float range_loss_per_round_cavalry = 30f;

		public float salvo_per_shot;

		public int max_archer_rounds = 1;

		public float balance_constant_melee = 1f;

		public float balance_constant_ranged = 1f;

		public float balance_constant_trample = 1f;

		public float balance_constant_attrition = 1f;

		public float noble_defense_per_unit_in_army = 100f;

		public float[] morale_outnumbered;

		public float min_outnumber = 0.5f;

		public float max_outnumber = 2f;

		public float bonus_defense_per_morale = 100f;

		public float defense_per_morale_offset;

		public float bonus_flat_defense_per_morale;

		public float morale_on_reinforce_friend = 0.0001f;

		public float morale_on_reinforce_enemy = 0.0001f;

		public float min_morale_on_start = 3f;

		public float bonus_temp_morale_on_start = 3f;

		public float morale_drop_from_damage = -5f;

		public float defenders_resilience_bonus_mod = 0.5f;

		public float morale_effects_range_battleview_mod = 3f;

		public float morale_threshold_divider = -1f;

		public float bonus_cth_per_max_initiative = 10f;

		public float bonus_resilience_per_max_initiative = 10f;

		public float penalty_cth_no_initiative = -10f;

		public float penalty_resilience_no_initiative = -10f;

		public float max_dist_from_battle = 30f;

		public float unit_max_morale = 30f;

		public Squad.State[] state_chances_check_order;

		public float dist_between_units_x = 2f;

		public float dist_between_units_y = 2f;

		public float retreat_dist = 5f;

		public float disengage_dist = 5f;

		public StateInfo.Chance[] surrender_overall_morale_chance;

		public float duration_mod = 1f;

		public List<MoraleFactor> morale_factors;

		public float morale_recovery_mod = 1f;

		public float morale_decay_mod = 1f;

		public StateInfo[] state_infos;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			min_distance_between_camps = field.GetFloat("min_distance_between_camps", null, min_distance_between_camps);
			max_distance_between_camps = field.GetFloat("max_distance_between_camps", null, max_distance_between_camps);
			army_offset_from_camp = field.GetFloat("army_offset_from_camp", null, army_offset_from_camp);
			break_siege_distance_to_camps = field.GetFloat("break_siege_distance_to_camps", null, break_siege_distance_to_camps);
			global_move_speed_mod = field.GetFloat("global_move_speed_mod", null, global_move_speed_mod);
			global_cth_mod = field.GetFloat("global_cth_mod", null, global_cth_mod);
			global_chance_to_shock_mod = field.GetFloat("global_chance_to_shock_mod", null, global_chance_to_shock_mod);
			defense_against_trample_mod = field.GetFloat("defense_against_trample_mod", null, defense_against_trample_mod);
			avoid_trample_tight_formation_mod = field.GetFloat("avoid_trample_tight_formation_mod", null, avoid_trample_tight_formation_mod);
			CTH_against_cav_charge_mod = field.GetFloat("CTH_against_cav_charge_mod", null, CTH_against_cav_charge_mod);
			castle_defenders_mod = field.GetFloat("castle_defenders_mod", null, castle_defenders_mod);
			min_castle_defender_bonus = field.GetFloat("min_castle_defender_bonus", null, min_castle_defender_bonus);
			castle_defenders_bonus_ranged = field.GetFloat("castle_defenders_bonus_ranged", null, castle_defenders_bonus_ranged);
			gate_assaulted_defense_mod = field.GetFloat("gate_assaulted_defense_mod", null, gate_assaulted_defense_mod);
			initiative_cooldown = field.GetFloat("initiative_cooldown", null, initiative_cooldown);
			initiative_countdown = field.GetFloat("initiative_countdown", null, initiative_countdown);
			morale_drop_from_initiative = field.GetFloat("morale_drop_from_initiative", null, morale_drop_from_initiative);
			aggressive_base_flank_angle = field.GetFloat("aggressive_base_flank_angle", null, aggressive_base_flank_angle);
			aggressive_additional_flank_angle_cavalry = field.GetFloat("aggressive_additional_flank_angle_cavalry", null, aggressive_additional_flank_angle_cavalry);
			aggressive_additional_flank_angle_infantry = field.GetFloat("aggressive_additional_flank_angle_infantry", null, aggressive_additional_flank_angle_infantry);
			defensive_base_flank_angle = field.GetFloat("defensive_base_flank_angle", null, defensive_base_flank_angle);
			defensive_additional_flank_angle_cavalry = field.GetFloat("defensive_additional_flank_angle_cavalry", null, defensive_additional_flank_angle_cavalry);
			defensive_additional_flank_angle_infantry = field.GetFloat("defensive_additional_flank_angle_infantry", null, defensive_additional_flank_angle_infantry);
			threat_additional_side_dist = field.GetFloat("threat_additional_side_dist", null, threat_additional_rear_dist);
			morale_flank_map_min = field.GetFloat("morale_flank_map_min", null, morale_flank_map_min);
			morale_flank_map_max = field.GetFloat("morale_flank_map_max", null, morale_flank_map_max);
			morale_flank_min = field.GetFloat("morale_flank_min", null, morale_flank_min);
			morale_flank_max = field.GetFloat("morale_flank_max", null, morale_flank_max);
			threat_additional_rear_dist = field.GetFloat("threat_additional_rear_dist", null, threat_additional_rear_dist);
			morale_flank_rear_map_min = field.GetFloat("morale_flank_rear_map_min", null, morale_flank_rear_map_min);
			morale_flank_rear_map_max = field.GetFloat("morale_flank_rear_map_max", null, morale_flank_rear_map_max);
			morale_flank_rear_min = field.GetFloat("morale_flank_rear_min", null, morale_flank_rear_min);
			morale_flank_rear_max = field.GetFloat("morale_flank_rear_max", null, morale_flank_rear_max);
			morale_from_casualties = field.GetFloat("morale_from_casualties", null, morale_from_casualties);
			max_tick_time = field.GetFloat("max_tick_time", null, max_tick_time);
			max_tick_time_battleview = field.GetFloat("max_tick_time_battleview", null, max_tick_time_battleview);
			range_loss_per_round = field.GetFloat("range_loss_per_round", null, range_loss_per_round);
			range_loss_per_round_cavalry = field.GetFloat("range_loss_per_round_cavalry", null, range_loss_per_round_cavalry);
			salvo_per_shot = field.GetFloat("salvo_per_shot", null, salvo_per_shot);
			balance_constant_melee = field.GetFloat("balance_constant_melee", null, balance_constant_melee);
			balance_constant_ranged = field.GetFloat("balance_constant_ranged", null, balance_constant_ranged);
			balance_constant_trample = field.GetFloat("balance_constant_trample", null, balance_constant_trample);
			balance_constant_attrition = field.GetFloat("balance_constant_attrition", null, balance_constant_attrition);
			morale_on_reinforce_friend = field.GetFloat("morale_on_reinforce_friend", null, morale_on_reinforce_friend);
			morale_on_reinforce_enemy = field.GetFloat("morale_on_reinforce_enemy", null, morale_on_reinforce_enemy);
			max_dist_from_battle = field.GetFloat("max_dist_from_battle", null, max_dist_from_battle);
			unit_max_morale = field.GetFloat("unit_max_morale", null, unit_max_morale);
			bonus_defense_per_morale = field.GetFloat("bonus_defense_per_morale", null, bonus_defense_per_morale);
			defense_per_morale_offset = field.GetFloat("defense_per_morale_offset", null, defense_per_morale_offset);
			bonus_flat_defense_per_morale = field.GetFloat("bonus_flat_defense_per_morale", null, bonus_flat_defense_per_morale);
			noble_defense_per_unit_in_army = field.GetFloat("noble_defense_per_unit_in_army", null, noble_defense_per_unit_in_army);
			morale_drop_from_damage = field.GetFloat("morale_drop_from_damage", null, morale_drop_from_damage);
			max_archer_rounds = field.GetInt("max_archer_rounds", null, max_archer_rounds);
			dist_between_units_x = field.GetFloat("dist_between_units_x", null, dist_between_units_x);
			dist_between_units_y = field.GetFloat("dist_between_units_y", null, dist_between_units_y);
			retreat_dist = field.GetFloat("retreat_dist", null, retreat_dist);
			duration_mod = field.GetFloat("duration_mod", null, duration_mod);
			morale_effects_range_battleview_mod = field.GetFloat("morale_effects_range_battleview_mod", null, morale_effects_range_battleview_mod);
			defenders_resilience_bonus_mod = field.GetFloat("defenders_resilience_bonus_mod", null, defenders_resilience_bonus_mod);
			bonus_temp_morale_on_start = field.GetFloat("bonus_temp_morale_on_start", null, bonus_temp_morale_on_start);
			DT.Field field2 = field.FindChild("surrender_overall_morale_chance");
			surrender_overall_morale_chance = null;
			if (field2?.children != null)
			{
				List<StateInfo.Chance> list = new List<StateInfo.Chance>();
				for (int i = 0; i < field2.children.Count; i++)
				{
					DT.Field field3 = field2.children[i];
					StateInfo.Chance item = default(StateInfo.Chance);
					if (int.TryParse(field3.key, out item.level))
					{
						item.chance = field3.Float();
						list.Add(item);
					}
				}
				surrender_overall_morale_chance = list.ToArray();
			}
			morale_threshold_divider = field.GetFloat("morale_threshold_divider", null, morale_threshold_divider);
			bonus_cth_per_max_initiative = field.GetFloat("bonus_cth_per_max_initiative", null, bonus_cth_per_max_initiative);
			bonus_resilience_per_max_initiative = field.GetFloat("bonus_resilience_per_max_initiative", null, bonus_resilience_per_max_initiative);
			penalty_cth_no_initiative = field.GetFloat("penalty_cth_no_initiative", null, penalty_cth_no_initiative);
			penalty_resilience_no_initiative = field.GetFloat("penalty_resilience_no_initiative", null, penalty_resilience_no_initiative);
			morale_recovery_mod = field.GetFloat("morale_recovery_mod", null, morale_recovery_mod);
			morale_decay_mod = field.GetFloat("morale_decay_mod", null, morale_decay_mod);
			morale_factors = new List<MoraleFactor>();
			DT.Field field4 = field.FindChild("morale_factors");
			if (field4 != null)
			{
				for (int j = 0; j < field4.children.Count; j++)
				{
					DT.Field field5 = field4.children[j];
					if (!(field5.type != "float"))
					{
						bool can_be_initial = field5.GetBool("initial");
						morale_factors.Add(new MoraleFactor
						{
							field = field5,
							can_be_initial = can_be_initial
						});
					}
				}
			}
			DT.Field field6 = field.FindChild("morale_outnumbered");
			if (field6 != null)
			{
				int num = field6.NumValues();
				morale_outnumbered = new float[num];
				for (int k = 0; k < num; k++)
				{
					morale_outnumbered[k] = field6.Float(k);
				}
				min_outnumber = field6.GetFloat("min_outnumber", null, min_outnumber);
				max_outnumber = field6.GetFloat("max_outnumber", null, max_outnumber);
			}
			PerLevelValues perLevelValues = PerLevelValues.Parse<string>(field.FindChild("state_chances_check_order"));
			if (perLevelValues != null)
			{
				state_chances_check_order = new Squad.State[perLevelValues.items.Count];
				for (int l = 0; l < perLevelValues.items.Count; l++)
				{
					if (Enum.TryParse<Squad.State>(perLevelValues.items[l].value, out var result))
					{
						state_chances_check_order[l] = result;
					}
				}
			}
			min_morale_on_start = field.GetFloat("min_morale_on_start", null, min_morale_on_start);
			DT.Field field7 = field.FindChild("States");
			state_infos = new StateInfo[11];
			if (field7 == null)
			{
				return false;
			}
			for (Squad.State state = Squad.State.Idle; state < Squad.State.COUNT; state++)
			{
				DT.Field field8 = field7.FindChild(state.ToString());
				if (field8 == null)
				{
					continue;
				}
				StateInfo stateInfo = default(StateInfo);
				stateInfo.duration = field8.GetFloat("duration");
				stateInfo.duration_battleview = field8.GetFloat("duration_battleview", null, stateInfo.duration);
				stateInfo.id = (int)state;
				DT.Field field9 = field8.FindChild("chance");
				if (field9 != null)
				{
					if (field9.children == null || field9.children.Count == 0)
					{
						stateInfo.chance = new StateInfo.Chance[1];
						StateInfo.Chance chance = new StateInfo.Chance
						{
							level = 0,
							chance = field9.Float()
						};
						stateInfo.chance[0] = chance;
					}
					else
					{
						List<StateInfo.Chance> list2 = new List<StateInfo.Chance>();
						for (int m = 0; m < field9.children.Count; m++)
						{
							DT.Field field10 = field9.children[m];
							StateInfo.Chance item2 = default(StateInfo.Chance);
							if (int.TryParse(field10.key, out item2.level))
							{
								item2.chance = field10.Float();
								list2.Add(item2);
							}
						}
						stateInfo.chance = list2.ToArray();
					}
				}
				stateInfo.morale_buff_nearby_friends = field8.GetFloat("morale_buff_nearby_friends");
				stateInfo.morale_buff_nearby_friends_range = field8.GetFloat("morale_buff_nearby_friends_range");
				DT.Field field11 = field8.FindChild("apply_on_friends");
				if (field11 != null)
				{
					stateInfo.apply_on_friends_min = field11.Value(0);
					stateInfo.apply_on_friends_max = field11.Value(1);
				}
				DT.Field field12 = field8.FindChild("apply_on_enemies");
				if (field12 != null)
				{
					stateInfo.apply_on_enemies_min = field12.Value(0);
					stateInfo.apply_on_enemies_max = field12.Value(1);
				}
				stateInfo.morale_buff_nearby_enemies = field8.GetFloat("morale_buff_nearby_enemies");
				stateInfo.morale_buff_nearby_enemies_range = field8.GetFloat("morale_buff_nearby_enemies_range");
				stateInfo.morale_on_finish = field8.GetFloat("morale_on_finish");
				stateInfo.initiative_my_side = field8.GetFloat("initiative_my_side", null, stateInfo.initiative_my_side);
				stateInfo.initiative_enemy_side = field8.GetFloat("initiative_enemy_side", null, stateInfo.initiative_enemy_side);
				state_infos[(int)state] = stateInfo;
			}
			return true;
		}

		public float GetMoraleOutnumbered(float ratio)
		{
			float num = min_outnumber;
			float num2 = max_outnumber;
			float num3 = (num + num2) / 2f;
			float num4 = morale_outnumbered[0];
			float num5 = morale_outnumbered[1];
			float num6 = morale_outnumbered[2];
			if (ratio == num3)
			{
				return num5;
			}
			if (ratio < num3)
			{
				num2 = num3;
				num6 = num5;
			}
			else
			{
				num = num3;
				num4 = num5;
			}
			return num4 + (ratio - num) / (num2 - num) * (num6 - num4);
		}

		public float GetMoraleFlanked(float threat)
		{
			float num = threat;
			float num2 = morale_flank_map_min;
			float num3 = morale_flank_map_max;
			float num4 = (num2 + num3) / 2f;
			float num5 = morale_flank_min;
			float num6 = 0f;
			float num7 = morale_flank_max;
			if (num == num4)
			{
				return num6;
			}
			if (num < num2)
			{
				num = num2;
			}
			if (num > num3)
			{
				num = num3;
			}
			if (num < num4)
			{
				num3 = num4;
				num7 = num6;
			}
			else
			{
				num2 = num4;
				num5 = num6;
			}
			return num5 + (num - num2) / (num3 - num2) * (num7 - num5);
		}

		public float GetMoraleFlankedRear(float threat)
		{
			float num = threat;
			float num2 = morale_flank_rear_map_min;
			float num3 = morale_flank_rear_map_max;
			float num4 = (num2 + num3) / 2f;
			float num5 = morale_flank_rear_min;
			float num6 = 0f;
			float num7 = morale_flank_rear_max;
			if (num == num4)
			{
				return num6;
			}
			if (num < num2)
			{
				num = num2;
			}
			if (num > num3)
			{
				num = num3;
			}
			if (num < num4)
			{
				num3 = num4;
				num7 = num6;
			}
			else
			{
				num2 = num4;
				num5 = num6;
			}
			return num5 + (num - num2) / (num3 - num2) * (num7 - num5);
		}

		public float BonusDefenseFromMorale(float morale)
		{
			return bonus_defense_per_morale * (morale - defense_per_morale_offset) / Math.Max(0f, unit_max_morale - defense_per_morale_offset);
		}
	}

	public class Squad : IVars
	{
		public enum State
		{
			Idle,
			Moving,
			Charging,
			Attacking,
			Shooting,
			Disengaging,
			Retreating,
			Fled,
			Dead,
			Left,
			Stuck,
			COUNT
		}

		public BattleSimulation simulation;

		public Army army;

		public Garrison garrison;

		private Unit _unit;

		private InventoryItem _equipment;

		public Squad main_squad;

		public List<Squad> sub_squads;

		public float disorganize;

		public int unit_idx = -1;

		public int equipment_idx = -1;

		public Logic.Squad squad;

		public Unit.Def def;

		public SalvoData.Def salvo_def;

		public int battle_side = -1;

		public int battle_row;

		public int battle_col;

		public Point initial_position = Point.Zero;

		public Point position = Point.Zero;

		public Point tgt_position = Point.Zero;

		public float mod_move_speed = 1f;

		public int number_attacks;

		public int number_attacked;

		public Point army_center = Point.Zero;

		public float heading;

		public bool temporary;

		public bool spawned_in_bv;

		public State state;

		public Time state_end_time = Time.Zero;

		public float max_damage = 1f;

		public float damage;

		public float damage_acc;

		public float initial_damage;

		public float remaining_salvos;

		public float initial_morale;

		public float temporary_morale_acc;

		public float permanent_morale;

		public float temporary_morale;

		public float[] permanent_morale_factors;

		public Squad target;

		public List<Squad> engaged_squads = new List<Squad>();

		public int max_troops = -1;

		public bool is_dirty;

		public bool killed_in_bv;

		private static List<Squad> friends_in_range = new List<Squad>();

		private static List<Squad> enemies_in_range = new List<Squad>();

		public Unit unit
		{
			get
			{
				if (_unit == null)
				{
					_unit = ((garrison != null) ? garrison.GetUnit(unit_idx) : ((army != null) ? army.GetUnit(unit_idx) : null));
					if (_unit != null && simulation?.battle != null && !simulation.battle.IsFinishing())
					{
						_unit.simulation = this;
					}
				}
				return _unit;
			}
			set
			{
				_unit = value;
				if (_unit != null)
				{
					unit_idx = ((garrison != null) ? garrison.units.IndexOf(_unit) : ((army != null) ? army.units.IndexOf(_unit) : (-1)));
				}
				else
				{
					unit_idx = -1;
				}
			}
		}

		public InventoryItem equipment
		{
			get
			{
				if (_equipment == null)
				{
					_equipment = ((army != null) ? army.GetEquipment(equipment_idx) : null);
					if (_equipment != null && simulation?.battle != null && !simulation.battle.IsFinishing())
					{
						_equipment.simulation = this;
					}
				}
				return _equipment;
			}
			set
			{
				_equipment = value;
				if (_equipment != null)
				{
					equipment_idx = ((army != null) ? army.siege_equipment.IndexOf(_equipment) : (-1));
				}
				else
				{
					equipment_idx = -1;
				}
			}
		}

		public Point world_pos
		{
			get
			{
				if (simulation == null)
				{
					return position;
				}
				return simulation.battle.position + position;
			}
		}

		public Point world_tgt_pos
		{
			get
			{
				if (simulation == null)
				{
					return position;
				}
				return simulation.battle.position + tgt_position;
			}
		}

		public int max_troops_limited => (int)((float)max_troops * max_damage);

		public float total_manpower()
		{
			float num = 0f;
			if (sub_squads != null)
			{
				for (int i = 0; i < sub_squads.Count; i++)
				{
					Squad squad = sub_squads[i];
					num += squad.total_manpower();
				}
			}
			else
			{
				if (IsDefeated())
				{
					return 0f;
				}
				num += (float)unit.max_manpower_modified_locked_in_battle();
			}
			return num;
		}

		public float manpower()
		{
			float num = 0f;
			if (sub_squads != null)
			{
				for (int i = 0; i < sub_squads.Count; i++)
				{
					Squad squad = sub_squads[i];
					num += squad.manpower();
				}
			}
			else
			{
				if (IsDefeated())
				{
					return 0f;
				}
				Unit.ManpowerBreakdownByHealth(unit.max_manpower_modified_locked_in_battle(), damage, out var healthy, out var _);
				num += (float)healthy;
			}
			return num;
		}

		public Logic.Squad FindSquad()
		{
			if (sub_squads != null)
			{
				for (int i = 0; i < sub_squads.Count; i++)
				{
					Squad squad = sub_squads[i];
					if (squad != null && !squad.IsDefeated())
					{
						return squad.squad;
					}
				}
			}
			return this.squad;
		}

		public override string ToString()
		{
			return "{[" + ((battle_side == 0) ? "A" : "D") + ":" + simulation.GetSquadIndex(this) + "]" + def.id + ", D:" + (int)(damage * 100f) + "%, M:" + GetMorale() + ", " + state.ToString() + "}";
		}

		public Squad(Multiplayer multiplayer, BattleSimulation simulation)
		{
			this.simulation = simulation;
		}

		public Squad(Unit u, Garrison garrison, BattleSimulation simulation, int side = 1, float morale = 0f)
		{
			this.simulation = simulation;
			this.garrison = garrison;
			army = u.army;
			unit = u;
			def = u.def;
			salvo_def = u.salvo_def;
			battle_side = side;
			battle_row = u.battle_row;
			battle_col = u.battle_col;
			damage = u.damage;
			initial_damage = damage;
			initial_morale = morale;
			u.simulation = this;
			RecalcPermanentMorale();
			AddMorale(simulation.def.bonus_temp_morale_on_start, acc: false);
			if (u.def.is_ranged)
			{
				remaining_salvos = u.SalvoCapacityModified();
			}
		}

		public Squad(Unit u, BattleSimulation simulation, float morale = 0f)
		{
			this.simulation = simulation;
			army = u.army;
			unit = u;
			def = u.def;
			salvo_def = u.salvo_def;
			garrison = u.garrison;
			if (army != null)
			{
				battle_side = army.battle_side;
			}
			else if (garrison != null)
			{
				battle_side = 1;
			}
			else
			{
				simulation.game.Warning($"Trying to add {u} with no battle side");
			}
			battle_row = u.battle_row;
			battle_col = u.battle_col;
			damage = u.damage;
			initial_damage = damage;
			initial_morale = morale;
			u.simulation = this;
			RecalcPermanentMorale();
			AddMorale(simulation.def.bonus_temp_morale_on_start, acc: false);
			if (u.def.is_ranged)
			{
				remaining_salvos = u.SalvoCapacityModified();
			}
		}

		public Squad(Squad original, float damage, float max_damage)
		{
			simulation = original.simulation;
			army = original.army;
			garrison = original.garrison;
			unit = original.unit;
			def = original.def;
			salvo_def = original.salvo_def;
			battle_side = original.battle_side;
			battle_row = original.battle_row;
			battle_col = original.battle_col;
			this.damage = damage;
			initial_damage = damage;
			this.max_damage = max_damage;
			initial_morale = original.initial_morale;
			permanent_morale = original.permanent_morale;
			temporary_morale = original.temporary_morale;
			remaining_salvos = original.remaining_salvos;
			main_squad = original;
			equipment = original.equipment;
			spawned_in_bv = original.spawned_in_bv;
			temporary = original.temporary;
			ValidateMaxTroops();
			original.sub_squads.Add(this);
		}

		public Squad(InventoryItem u, BattleSimulation simulation)
		{
			this.simulation = simulation;
			army = u.army;
			equipment = u;
			def = u.def;
			salvo_def = simulation.game.defs.Find<SalvoData.Def>(u.def.salvo_def);
			battle_side = army.battle_side;
			battle_row = u.battle_row;
			battle_col = u.battle_col;
			damage = u.damage;
			initial_damage = damage;
			initial_morale = 0f;
			u.simulation = this;
			RecalcPermanentMorale();
			AddMorale(simulation.def.bonus_temp_morale_on_start, acc: false);
			ValidateMaxTroops();
			if (u.def.is_ranged)
			{
				remaining_salvos = u.def.salvo_capacity;
			}
		}

		public void TryShock(Squad e, bool is_trample = false)
		{
			float num = unit.chance_to_shock_Modified(is_trample) * simulation.def.global_chance_to_shock_mod;
			float num2 = e.resilience_total();
			if (num2 < 0f)
			{
				num -= num2;
			}
			if ((float)simulation.game.Random(0, 100) < num)
			{
				float num3 = unit.shock_damage_base();
				if (is_trample)
				{
					num3 += unit.shock_damage_bonus_trample();
				}
				e.AddMorale(0f - num3);
			}
		}

		private bool SetTargetPosition(Point pt, float offset, float max_duration, bool validate = false, bool slow_down = false)
		{
			Point point = pt - position;
			float num = point.Normalize() - offset * simulation.wv_scale_def;
			mod_move_speed = 1f;
			if (slow_down)
			{
				mod_move_speed = num / def.move_speed / max_duration;
			}
			else if (num / def.move_speed > max_duration)
			{
				num = def.move_speed;
			}
			if (num < offset)
			{
				num = offset;
			}
			Point point2 = position + point * num;
			if (validate && point2.Length() > simulation.def.max_dist_from_battle)
			{
				return false;
			}
			tgt_position = point2;
			is_dirty = true;
			return true;
		}

		public void AddMorale(State state)
		{
			Def.StateInfo stateInfo = simulation.def.state_infos[(int)state];
			AddMorale(stateInfo.morale_on_finish);
		}

		public void AddMorale(float val, bool acc = true, bool ignore_resilience = false)
		{
			if (def.type == Unit.Type.InventoryItem)
			{
				return;
			}
			if (val < 0f && !ignore_resilience)
			{
				float num = resilience_total();
				if (num > 0f && (float)simulation.game.Random(0, 100) < num)
				{
					return;
				}
			}
			float num2 = GetMorale() + val;
			if (val > 0f && num2 > simulation.def.unit_max_morale)
			{
				val -= num2 - simulation.def.unit_max_morale;
			}
			else if (val < 0f && num2 < 0f)
			{
				val -= num2;
			}
			if (acc)
			{
				temporary_morale_acc += val;
			}
			else
			{
				temporary_morale += val;
			}
		}

		public void AddMoraleAOE(State state)
		{
			if (squad == null || squad.is_main_squad)
			{
				Def.StateInfo info = simulation.def.state_infos[(int)state];
				if (info.morale_buff_nearby_enemies != 0f || info.morale_buff_nearby_friends != 0f)
				{
					AddMoraleAOE(battle_side, info, friends_in_range);
					AddMoraleAOE(1 - battle_side, info, enemies_in_range);
				}
			}
		}

		public void AddInitiative(State state)
		{
			if (squad != null && !squad.is_main_squad)
			{
				return;
			}
			Battle battle = simulation.battle;
			if (battle.initiative != null && squad != null && squad.is_main_squad)
			{
				Def.StateInfo stateInfo = simulation.def.state_infos[(int)state];
				if (battle.initiative_side == battle_side)
				{
					battle.initiative.Add(stateInfo.initiative_my_side);
				}
				else
				{
					battle.initiative.Add(stateInfo.initiative_enemy_side);
				}
			}
		}

		public void AddMoraleAOE(int side, Def.StateInfo info, List<Squad> in_range)
		{
			in_range.Clear();
			int num = -1;
			Point pt = position;
			if (this.squad != null)
			{
				pt = this.squad.VisualPosition();
			}
			float num3;
			float num2;
			if (side == battle_side)
			{
				num2 = info.morale_buff_nearby_friends_range;
				num3 = info.morale_buff_nearby_friends;
				if (info.apply_on_friends_min >= 0 && info.apply_on_friends_max > 0)
				{
					num = simulation.game.Random(info.apply_on_friends_min, info.apply_on_friends_max);
				}
			}
			else
			{
				num2 = info.morale_buff_nearby_enemies_range;
				num3 = info.morale_buff_nearby_enemies;
				if (info.apply_on_enemies_min >= 0 && info.apply_on_enemies_max > 0)
				{
					num = simulation.game.Random(info.apply_on_enemies_min, info.apply_on_enemies_max);
				}
			}
			num2 *= simulation.def.morale_effects_range_battleview_mod;
			if (num3 == 0f)
			{
				return;
			}
			List<Squad> squads = simulation.GetSquads(side);
			for (int i = 0; i < squads.Count; i++)
			{
				Squad squad = squads[i];
				if (squad == null || squad.IsDefeated())
				{
					continue;
				}
				if (squad.sub_squads != null)
				{
					for (int j = 0; j < squad.sub_squads.Count; j++)
					{
						Squad squad2 = squad.sub_squads[j];
						if (num2 > 0f)
						{
							Point point = squad2.position;
							if (squad2.squad != null)
							{
								point = squad2.squad.VisualPosition();
							}
							if (point.Dist(pt) > num2)
							{
								continue;
							}
						}
						in_range.Add(squad2);
					}
					continue;
				}
				if (num2 > 0f)
				{
					Point point2 = squad.position;
					if (squad.squad != null)
					{
						point2 = squad.squad.VisualPosition();
					}
					if (point2.Dist(pt) > num2)
					{
						continue;
					}
				}
				in_range.Add(squad);
			}
			if (num < 0)
			{
				num = in_range.Count;
			}
			for (int k = 0; k < num; k++)
			{
				if (in_range.Count == 0)
				{
					break;
				}
				int index = simulation.game.Random(0, in_range.Count);
				in_range[index].AddMorale(num3);
				in_range.RemoveAt(index);
			}
		}

		public float GetMorale()
		{
			return Game.clamp(Game.clamp(permanent_morale, 0f, simulation.def.unit_max_morale) + temporary_morale, 0f, simulation.def.unit_max_morale);
		}

		public float MoraleNormalized(float total_manpower)
		{
			float num = 0f;
			if (sub_squads != null)
			{
				for (int i = 0; i < sub_squads.Count; i++)
				{
					Squad squad = sub_squads[i];
					num += squad.MoraleNormalized(total_manpower);
				}
			}
			else
			{
				num = GetMorale() * this.total_manpower() / total_manpower;
			}
			return num;
		}

		public void RecalcPermanentMorale()
		{
			if (unit == null)
			{
				return;
			}
			permanent_morale = 0f;
			int count = simulation.def.morale_factors.Count;
			if (permanent_morale_factors == null || permanent_morale_factors.Length != count)
			{
				permanent_morale_factors = new float[count];
			}
			for (int i = 0; i < simulation.def.morale_factors.Count; i++)
			{
				Def.MoraleFactor moraleFactor = simulation.def.morale_factors[i];
				if (moraleFactor.can_be_initial)
				{
					float num = moraleFactor.field.Value(this);
					permanent_morale_factors[i] = num;
					permanent_morale += num;
				}
			}
			for (int j = 0; j < simulation.def.morale_factors.Count; j++)
			{
				Def.MoraleFactor moraleFactor2 = simulation.def.morale_factors[j];
				if (!moraleFactor2.can_be_initial)
				{
					float num2 = moraleFactor2.field.Value(this);
					permanent_morale_factors[j] = num2;
					permanent_morale += num2;
				}
			}
			permanent_morale = Game.clamp(permanent_morale, 0f - initial_morale, simulation.def.unit_max_morale);
		}

		public void CheckThresholds()
		{
			if (squad != null && squad.main_squad != null)
			{
				squad.main_squad.simulation.CheckThresholds();
			}
			else
			{
				if (!MoraleEffectsActive || this.state >= State.Fled || simulation.def.state_chances_check_order == null || def.type == Unit.Type.InventoryItem)
				{
					return;
				}
				int num = (int)Math.Ceiling(GetMorale());
				for (int i = 0; i < simulation.def.state_chances_check_order.Length; i++)
				{
					State state = simulation.def.state_chances_check_order[i];
					if (this.state < state && (def.type != Unit.Type.Noble || (state != State.Fled && simulation.battle.batte_view_game != null)))
					{
						Def.StateInfo stateInfo = simulation.def.state_infos[(int)state];
						float chance = stateInfo.GetChance(num, this);
						if (chance > 0f && simulation.game.Random(0f, 100f) < chance)
						{
							SetState(state);
							simulation.ThinkSurrender(battle_side);
							break;
						}
					}
				}
			}
		}

		public void SetState(State state, Point target_pos, float offset = 0f, float duration = 0f)
		{
			bool flag = false;
			if (duration == 0f)
			{
				duration = simulation.def.state_infos[(int)state].duration;
				flag = true;
			}
			if (state == State.Moving || (uint)(state - 5) <= 1u)
			{
				SetTargetPosition(target_pos, offset, duration, validate: false, !flag);
				if (flag)
				{
					duration = tgt_position.Dist(position) / def.move_speed;
				}
				SetState(state, duration, target);
			}
		}

		public void SetState(State state, Squad target = null, float fixed_duration = -1f)
		{
			float num = simulation.def.state_infos[(int)state].duration;
			if (simulation.battle.batte_view_game != null)
			{
				num = simulation.def.state_infos[(int)state].duration_battleview;
			}
			if (fixed_duration != -1f)
			{
				num = fixed_duration;
			}
			bool flag = fixed_duration != -1f;
			switch (state)
			{
			case State.Attacking:
				num = def.attack_interval;
				break;
			case State.Shooting:
				num = def.shoot_interval;
				break;
			case State.Moving:
				if (target == null)
				{
					break;
				}
				if (target.state == State.Moving || target.state == State.Retreating || target.state == State.Disengaging)
				{
					if (target.target == this)
					{
						if (flag)
						{
							SetTargetPosition((target.position + position) / 2f, def.radius + target.def.radius, num, validate: false, slow_down: true);
							target.SetTargetPosition(tgt_position, def.radius + target.def.radius, num, validate: false, slow_down: true);
						}
						else
						{
							SetTargetPosition((target.position + position) / 2f, def.radius + target.def.radius, num);
							target.SetTargetPosition(tgt_position, def.radius + target.def.radius, num);
							target.state_end_time = simulation.time + target.tgt_position.Dist(target.position) / target.def.move_speed;
						}
					}
					else
					{
						SetTargetPosition(target.tgt_position, def.radius + target.def.radius, num, validate: false, flag);
					}
				}
				else
				{
					SetTargetPosition(target.position, def.radius + target.def.radius, num, validate: false, flag);
				}
				if (state == State.Moving)
				{
					num = Math.Max(num, tgt_position.Dist(position) / def.move_speed);
				}
				break;
			case State.Disengaging:
			case State.Retreating:
			{
				List<Squad> squads = simulation.GetSquads(battle_side);
				if (simulation.battle.batte_view_game != null)
				{
					break;
				}
				Squad squad = null;
				for (int i = 0; i < squads.Count; i++)
				{
					Squad squad2 = squads[i];
					if (squad2 != this && squad2.def.type == Unit.Type.Noble)
					{
						squad = squad2;
						break;
					}
				}
				if (squad == null)
				{
					squads = simulation.GetSquads(1 - battle_side);
					Point zero = Point.Zero;
					int num2 = 0;
					for (int j = 0; j < squads.Count; j++)
					{
						Squad squad3 = squads[j];
						if (squad3 != this)
						{
							zero += squad3.position;
							num2++;
						}
					}
					if (num2 > 0)
					{
						zero /= (float)num2;
					}
					if (!SetTargetPosition(position - (zero - position).GetNormalized() * def.move_speed * num, 0f, num, validate: true))
					{
						return;
					}
				}
				else
				{
					Point point = squad.initial_position;
					Point point2 = position - point;
					float num3 = 0f;
					switch (state)
					{
					case State.Retreating:
						num3 = simulation.def.retreat_dist;
						break;
					case State.Disengaging:
						num3 = simulation.def.disengage_dist;
						break;
					}
					Point pt = point + point2.Right(1f) * (0f - num3);
					Point pt2 = point + point2.Right(1f) * num3;
					if (pt.Dist(position) < pt2.Dist(position))
					{
						if (!SetTargetPosition(pt, 0f, num, validate: true))
						{
							return;
						}
					}
					else if (!SetTargetPosition(pt2, 0f, num, validate: true))
					{
						return;
					}
				}
				num = Math.Max(num, tgt_position.Dist(position) / def.move_speed);
				break;
			}
			default:
				position = tgt_position;
				break;
			}
			SetState(state, num, target);
		}

		public void SetState(State state, float duration, Squad target = null)
		{
			is_dirty = true;
			this.target = target;
			bool flag = this.state != state && (this.state == State.Stuck || this.state == State.Fled || this.state == State.Dead);
			this.state = state;
			duration *= simulation.def.duration_mod;
			AddMoraleAOE(state);
			AddInitiative(state);
			if (duration > 0f)
			{
				state_end_time = simulation.time + duration;
			}
			else
			{
				state_end_time = Time.Zero;
			}
			switch (state)
			{
			case State.Disengaging:
				Disengage();
				return;
			case State.Fled:
				Disengage();
				if (squad != null)
				{
					squad.OnDefeat(recalc: false);
				}
				break;
			case State.Dead:
			case State.Stuck:
				Disengage();
				if (squad != null)
				{
					squad.OnDefeat(recalc: false, swap_main_squad: true);
				}
				break;
			case State.Retreating:
				Disengage();
				if (squad != null && simulation.battle.batte_view_game != null)
				{
					squad.Flee(duration);
				}
				break;
			}
			if (flag && squad != null)
			{
				squad.NotifyListeners("refresh_drawers");
			}
		}

		private void Disengage()
		{
			if (engaged_squads != null)
			{
				for (int num = engaged_squads.Count - 1; num >= 0; num--)
				{
					simulation.Disengage(this, engaged_squads[num]);
				}
			}
		}

		public bool IsDefeated()
		{
			if (sub_squads != null)
			{
				bool result = true;
				for (int i = 0; i < sub_squads.Count; i++)
				{
					if (!sub_squads[i].IsDefeated())
					{
						result = false;
						break;
					}
				}
				return result;
			}
			return state >= State.Fled;
		}

		public void ValidateMaxTroops()
		{
			if (!Game.isLoadingSaveGame && max_troops < 0)
			{
				if (unit == null)
				{
					max_troops = 1;
				}
				else
				{
					max_troops = unit.max_size_modified();
				}
			}
		}

		public int NumTroops()
		{
			if (max_troops < 0)
			{
				ValidateMaxTroops();
			}
			return max_troops - (int)((float)max_troops * damage);
		}

		public float FriendlyFireReduction()
		{
			return def.bonus_friendly_fire_reduction_perc.GetValue(unit, include_battle_bonuses: true);
		}

		public float CTH_Modified(Squad enemy = null)
		{
			int rid = ((army != null) ? ((army.realm_in != null) ? army.realm_in.id : 0) : 0);
			return def.CTH_modified(simulation.battle, use_battle_bonuses: true, battle_side, unit.level, enemy, rid, army?.leader, garrison, this);
		}

		public float chance_to_shock_Modified()
		{
			int rid = ((army != null) ? ((army.realm_in != null) ? army.realm_in.id : 0) : 0);
			return def.shock_chance_modified(simulation.battle, use_battle_bonuses: true, battle_side, unit.level, null, rid, army?.leader, garrison, this);
		}

		public float chance_to_shock_base()
		{
			return def.chance_to_shock;
		}

		public float CTH_shoot_mod_modified()
		{
			return def.CTH_shoot_mod_modified(this);
		}

		public float CTH_Ranged_Modified(Squad enemy = null)
		{
			return CTH_Modified(enemy) * CTH_shoot_mod_modified();
		}

		public float defense_Modified(Squad enemy = null)
		{
			return def.defense_modified(simulation.battle, use_battle_bonuses: true, battle_side, unit.level, this, army?.leader, garrison, enemy, null, unit.OutOfSupplies());
		}

		public float defense_Modified_siege(InventoryItem enemy = null)
		{
			return def.defense_modified(simulation.battle, use_battle_bonuses: true, battle_side, unit.level, this, army?.leader, garrison, null, enemy.def, unit.OutOfSupplies());
		}

		public float defense_against_ranged_Modified()
		{
			return def.defense_against_ranged_modified(simulation.battle, use_battle_bonuses: true, battle_side, unit.level, this, army?.leader, garrison, null, unit.OutOfSupplies());
		}

		public float resilience_base()
		{
			return def.resilience;
		}

		public float CTH_against_me_Modified()
		{
			if (squad == null)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < squad.buffs.Count; i++)
			{
				SquadBuff squadBuff = squad.buffs[i];
				num += squadBuff.getCTHAgainstMe();
			}
			return num;
		}

		public float resilience_bonus()
		{
			return unit.resilience_bonus(this);
		}

		public float resilience_total()
		{
			return resilience_base() + resilience_bonus();
		}

		public float move_speed_bonus()
		{
			if (squad == null)
			{
				return 0f;
			}
			return (float)Math.Round(squad.normal_move_speed - def.move_speed, 3);
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "unit":
				return new Value(unit);
			case "unit_def":
				return def;
			case "army":
				return army;
			case "garrison":
				return new Value(garrison);
			case "level":
				if (unit == null)
				{
					return 0;
				}
				return unit.level;
			case "resilience_base":
				return resilience_base();
			case "resilience_bonus":
				return resilience_bonus();
			case "resilience_total":
				return resilience_total();
			case "CTH":
				return def.CTH;
			case "CTH_bonus":
				return CTH_Modified() - def.CTH;
			case "CTH_ranged":
				return def.CTH_ranged_base();
			case "CTH_ranged_bonus":
				return CTH_Ranged_Modified() - def.CTH_ranged_base();
			case "defense":
				return def.defense;
			case "defense_modified":
				return defense_Modified();
			case "defense_bonus":
				return defense_Modified() - def.defense;
			case "defense_against_ranged":
				return def.defense_against_ranged_base();
			case "defense_against_ranged_bonus":
				return defense_against_ranged_Modified() - def.defense;
			case "defense_against_ranged_modified":
				return defense_against_ranged_Modified();
			case "defense_against_ranged_mod":
				return def.defense_against_ranged_mod_modified(this);
			case "defense_bonus_visual":
			{
				float num4 = (float)Math.Round(100f - 10000f / (100f + defense_Modified()) - (float)def.GetVar("defense_visual", vars));
				if (num4 == 0f)
				{
					return Value.Unknown;
				}
				return num4;
			}
			case "move_speed_bonus":
				return move_speed_bonus();
			case "move_speed":
				if (this.squad == null)
				{
					return 0;
				}
				return def.move_speed;
			case "salvos":
				if (!def.is_ranged)
				{
					return Value.Null;
				}
				if (this.squad == null)
				{
					return remaining_salvos;
				}
				return this.squad.salvos_left;
			case "manpower":
				return def.manpower_mul * (float)NumTroops();
			case "num_troops":
				return NumTroops();
			case "size":
				return (int)Math.Ceiling((float)def.size * max_damage);
			case "size_bonus":
			{
				int num2 = max_troops_limited - (int)Math.Ceiling((float)def.size * max_damage);
				if (num2 == 0)
				{
					return Value.Unknown;
				}
				return num2;
			}
			case "chance_to_shock_base":
				return chance_to_shock_base();
			case "chance_to_shock_bonus":
			{
				float num = chance_to_shock_Modified() - chance_to_shock_base();
				if (num == 0f)
				{
					return Value.Unknown;
				}
				return num;
			}
			case "command":
				if (this.squad == null)
				{
					return Value.Unknown;
				}
				return this.squad.command.ToString();
			case "siege_defense":
				return simulation.battle.siege_defense;
			case "max_siege_defense":
				if (simulation.battle?.settlement?.GetRealm() == null)
				{
					return 0;
				}
				return simulation.battle.settlement.GetRealm().GetStat(Stats.rs_siege_defense);
			case "is_starving":
				if (battle_side == 1 && simulation?.battle?.settlement_food_copy != null)
				{
					return simulation.battle.settlement_food_copy.Get() <= 0f;
				}
				return false;
			case "state":
				return state.ToString();
			case "temporary_morale":
				return temporary_morale;
			case "morale":
				return GetMorale();
			case "stamina":
				if (this.squad == null || def.is_siege_eq)
				{
					return Value.Unknown;
				}
				return this.squad.GetStamina();
			case "disorganize":
				return disorganize;
			case "health":
				if (this.squad == null)
				{
					return -1;
				}
				if (def.is_siege_eq)
				{
					return this.squad.siege_health * this.squad.def.manpower_mul;
				}
				return this.squad.NumTroops();
			case "max_health":
				if (this.squad == null)
				{
					return -1;
				}
				if (def.is_siege_eq)
				{
					return unit.max_health_modified() * this.squad.def.manpower_mul;
				}
				return this.squad.simulation.max_troops;
			case "has_shoot_advantage":
			{
				if (this.squad == null)
				{
					return false;
				}
				Logic.Squad squad2 = this.squad.ranged_enemy as Logic.Squad;
				bool flag4 = squad2 != null;
				bool flag5 = flag4 && this.squad.CanShoot(squad2, max_dist: true);
				float num3 = ((this.squad.high_ground_buff == null) ? 0f : this.squad.high_ground_buff.GetCTHShootMod());
				bool flag6 = num3 < 0f || (this.squad.trees_buff != null && this.squad.trees_buff.enabled) || (flag4 && squad2.trees_buff != null && squad2.trees_buff.enabled);
				return flag4 && flag5 && !flag6 && num3 > 0f;
			}
			case "has_shoot_disadvantage":
			{
				if (this.squad == null)
				{
					return false;
				}
				Logic.Squad squad = this.squad.ranged_enemy as Logic.Squad;
				bool flag = squad != null;
				bool flag2 = flag && this.squad.CanShoot(squad, max_dist: true);
				bool flag3 = ((this.squad.high_ground_buff == null) ? 0f : this.squad.high_ground_buff.GetCTHShootMod()) < 0f || (this.squad.trees_buff != null && this.squad.trees_buff.enabled) || (flag && squad.trees_buff != null && squad.trees_buff.enabled);
				return flag && flag2 && flag3;
			}
			case "cth_from_buffs":
				return def.CTH_from_buffs(this);
			case "defense_from_level":
				return def.defense_from_level(unit.battle, use_battle_bonuses: true, battle_side, unit.level, this, army?.leader, garrison, null, null, unit.OutOfSupplies());
			case "defense_from_morale_flat":
				return def.defense_from_morale_flat(this);
			case "defense_from_morale_perc":
				return def.defense_from_morale_perc(this);
			case "defense_from_buffs":
				return def.defense_from_buffs(this);
			case "defense_for_noble":
				return def.defense_for_noble(this);
			case "shock_chance_from_charging":
				return def.shock_chance_from_charging(this);
			case "resilience_from_buffs":
				return unit.resilience_from_buffs(this);
			default:
				if (this.squad?.buffs != null)
				{
					for (int i = 0; i < this.squad.buffs.Count; i++)
					{
						SquadBuff squadBuff = this.squad.buffs[i];
						if (squadBuff.field.key == key)
						{
							return squadBuff;
						}
					}
				}
				if (unit != null)
				{
					return unit.GetVar(key, vars, as_value);
				}
				return Value.Unknown;
			}
		}
	}

	public class Totals
	{
		public float size;

		public float initial_count;

		public float last_count;

		public float count;

		public float attack_power = 1f;

		public float defense_power = 1f;

		public float highest_max_shoot_range;

		public float highest_max_shoot_range_attack_duration;

		public int number_of_ranged_attacks;

		public bool all_cav = true;

		public int starting_capture_point_count;

		public int squad_count;

		public BattleSimulation simulation;

		public void Reset(bool reset_all_cav = false)
		{
			size = 0f;
			count = 0f;
			attack_power = 1f;
			defense_power = 1f;
			squad_count = 0;
			if (reset_all_cav)
			{
				all_cav = true;
			}
		}

		public void Add(Squad sq, bool reset_all_cav = false)
		{
			if (sq == null)
			{
				return;
			}
			if (sq.sub_squads != null)
			{
				for (int i = 0; i < sq.sub_squads.Count; i++)
				{
					Squad sq2 = sq.sub_squads[i];
					Add(sq2, reset_all_cav);
				}
			}
			else if (!sq.IsDefeated())
			{
				Unit.Def def = sq.def;
				sq.ValidateMaxTroops();
				size += sq.max_troops_limited;
				float num = sq.NumTroops();
				count += num;
				if (!sq.IsDefeated())
				{
					attack_power += num * CTH_Estimate(def, sq.salvo_def, (int)sq.remaining_salvos) / 100f;
					defense_power += num * (1f + def.defense / 100f);
				}
				if (sq.def.is_ranged && sq.salvo_def.max_shoot_range > highest_max_shoot_range)
				{
					highest_max_shoot_range = sq.salvo_def.max_shoot_range;
					highest_max_shoot_range_attack_duration = sq.def.shoot_interval;
				}
				if (!sq.def.is_cavalry && reset_all_cav)
				{
					all_cav = false;
				}
				squad_count++;
			}
		}

		private float CTH_Estimate(Unit.Def def, SalvoData.Def salvo_def, int salvos_left)
		{
			if (salvo_def == null || !def.is_ranged || salvos_left <= 0)
			{
				return def.CTH;
			}
			float num = def.CTH_ranged_base() * (100f + simulation.battle.def.range_estimation_mod * salvo_def.max_shoot_range) / 100f;
			if (def.is_siege_eq)
			{
				num *= (float)salvo_def.arrows_per_troop * def.siege_damage;
			}
			return num;
		}

		public void Add(List<Squad> squads, bool reset_all_cav = false)
		{
			for (int i = 0; i < squads.Count; i++)
			{
				Add(squads[i], reset_all_cav);
			}
		}

		public void Add(Unit unit, bool reset_all_cav = false)
		{
			if (!unit.IsDefeated())
			{
				Unit.Def def = unit.def;
				int num = unit.max_size_modified();
				size += num;
				float num2 = (float)num * (1f - unit.damage);
				count += num2;
				attack_power += num2 * CTH_Estimate(unit.def, unit.salvo_def, (unit.simulation != null) ? ((int)unit.simulation.remaining_salvos) : 0) / 100f;
				defense_power += num2 * (1f + def.defense / 100f);
				if (!def.is_cavalry && reset_all_cav)
				{
					all_cav = false;
				}
				squad_count++;
			}
		}

		public void Add(List<Unit> units, bool reset_all_cav = false)
		{
			for (int i = 0; i < units.Count; i++)
			{
				Add(units[i], reset_all_cav);
			}
		}

		public void Calc(List<Squad> squads, List<InventoryItem> items, bool reset_all_cav = false)
		{
			Reset(reset_all_cav);
			Add(squads, reset_all_cav);
			if (items == null)
			{
				return;
			}
			for (int i = 0; i < items.Count; i++)
			{
				InventoryItem inventoryItem = items[i];
				if (inventoryItem?.simulation != null && inventoryItem?.simulation?.sub_squads == null)
				{
					Add(inventoryItem.simulation, reset_all_cav);
				}
			}
		}

		public void Calc(Totals other)
		{
			size = other.size;
			initial_count = other.initial_count;
			last_count = other.last_count;
			count = other.count;
			attack_power = other.attack_power;
			defense_power = other.defense_power;
			highest_max_shoot_range = other.highest_max_shoot_range;
			highest_max_shoot_range_attack_duration = other.highest_max_shoot_range_attack_duration;
			number_of_ranged_attacks = other.number_of_ranged_attacks;
			all_cav = other.all_cav;
			starting_capture_point_count = other.starting_capture_point_count;
			squad_count = other.squad_count;
		}
	}

	private const float average_shoot_interval = 3f;

	public string attacker_tactics;

	public string defender_tactics;

	public List<Squad> attacker_squads = new List<Squad>();

	public List<Squad> defender_squads = new List<Squad>();

	public List<InventoryItem> attacker_equipment = new List<InventoryItem>();

	public List<InventoryItem> defender_equipment = new List<InventoryItem>();

	public Totals attacker_totals = new Totals();

	public Totals defender_totals = new Totals();

	public Totals attacker_totals_potential = new Totals();

	public Totals defender_totals_potential = new Totals();

	public float estimation = 0.5f;

	public float estimation_potential = 0.5f;

	public float initial_estimation = 0.5f;

	public float est_ratio = 1f;

	public float est_ratio_potential = 1f;

	public bool totals_dirty;

	public Def def;

	private Fortification.Def ranged_fortification_def;

	private int fortification_num_arrows = -1;

	private float fortification_cth = -1f;

	public Battle battle;

	public Time last_update;

	public static bool MoraleEffectsActive = true;

	public bool thought_archer_rounds_last_tick;

	private float wv_scale_def = 1.4f;

	private bool added_initial_squads;

	public bool loaded_from_save;

	public static bool log_attacks = false;

	public static bool save_log = false;

	private Time next_siege_tick;

	private Time next_attrition_tick;

	public Time time
	{
		get
		{
			if (battle?.batte_view_game == null)
			{
				return base.game.time;
			}
			return battle.batte_view_game.time;
		}
	}

	public void AddSiegeSquad(InventoryItem u)
	{
		Squad simulation = u.simulation;
		Unit unit = (simulation.unit = u.army.AddUnit(u.def));
		unit.simulation = simulation;
		AddSquad(simulation);
	}

	public void DelSiegeSquad(InventoryItem u)
	{
		DelSquad(u.simulation);
		u.simulation.unit = null;
	}

	public float OverallMorale(int battle_side)
	{
		float num = 0f;
		float num2 = TotalManpower(battle_side);
		if (num2 == 0f)
		{
			return 0f;
		}
		return num + OverallMorale(battle_side, num2);
	}

	public Totals GetTotals(int battle_side)
	{
		if (battle_side != 0)
		{
			return defender_totals;
		}
		return attacker_totals;
	}

	public Totals GetPotentialTotals(int battle_side)
	{
		if (battle_side != 0)
		{
			return defender_totals_potential;
		}
		return attacker_totals_potential;
	}

	public float TotalManpower(int battle_side)
	{
		float num = 0f;
		List<Squad> squads = GetSquads(battle_side);
		for (int i = 0; i < squads.Count; i++)
		{
			num += squads[i].total_manpower();
		}
		return num;
	}

	private float OverallMorale(int battle_side, float total_manpower)
	{
		List<Squad> squads = GetSquads(battle_side);
		float num = 0f;
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad = squads[i];
			if (!squad.IsDefeated())
			{
				num += squad.MoraleNormalized(total_manpower);
			}
		}
		return num;
	}

	public float GetEstimation()
	{
		return estimation;
	}

	public float GetEstimation(int battle_side)
	{
		float num = GetEstimation();
		if (battle_side != 0)
		{
			return num;
		}
		return 1f - num;
	}

	public float GetEstimationPotential()
	{
		return estimation_potential;
	}

	public float GetEstimationPotential(int battle_side)
	{
		if (battle_side != 0)
		{
			return estimation_potential;
		}
		return 1f - estimation_potential;
	}

	public List<Squad> GetSquads(int side)
	{
		return side switch
		{
			0 => attacker_squads, 
			1 => defender_squads, 
			_ => null, 
		};
	}

	public List<InventoryItem> GetEquipment(int side)
	{
		return side switch
		{
			0 => attacker_equipment, 
			1 => defender_equipment, 
			_ => null, 
		};
	}

	public BattleSimulation(Battle battle)
		: base(battle)
	{
		DT.Field field = base.game.dt.Find("wv_scale");
		if (field != null)
		{
			wv_scale_def = field.GetFloat("unit_scale", null, 1.4f);
		}
		next_siege_tick = base.game.time + battle.def.attrition_tick;
		this.battle = battle;
		attacker_totals.simulation = this;
		defender_totals.simulation = this;
		attacker_totals_potential.simulation = this;
		defender_totals_potential.simulation = this;
		RefreshDef();
	}

	public void RefreshDef()
	{
		if (battle?.batte_view_game != null)
		{
			def = base.game.defs.Find<Def>("BattleViewSimulation");
		}
		else
		{
			def = base.game.defs.GetBase<Def>();
		}
		last_update = base.game.time;
	}

	public void ForceCalcTotals()
	{
		totals_dirty = true;
		CalcTotals();
	}

	public void CalcTotals(bool update_last = false, bool reset_ranged_rounds = false)
	{
		if (totals_dirty)
		{
			Battle battle = obj as Battle;
			totals_dirty = false;
			if (update_last)
			{
				attacker_totals.last_count = attacker_totals.count;
				defender_totals.last_count = defender_totals.count;
			}
			attacker_totals.Calc(attacker_squads, attacker_equipment, reset_ranged_rounds);
			defender_totals.Calc(defender_squads, defender_equipment, reset_ranged_rounds);
			if (battle.type == Battle.Type.Siege || battle.type == Battle.Type.Assault)
			{
				defender_totals.defense_power += battle.castle_defender_bonus;
			}
			attacker_totals_potential.Calc(attacker_totals);
			defender_totals_potential.Calc(defender_totals);
			if (battle.reinforcements[0].army != null && battle.GetSupporter(0) == null)
			{
				attacker_totals_potential.Add(battle.reinforcements[0].army.units, reset_ranged_rounds);
			}
			if (battle.reinforcements[1].army != null && battle.GetSupporter(1) == null)
			{
				defender_totals_potential.Add(battle.reinforcements[1].army.units, reset_ranged_rounds);
			}
			if (battle.reinforcements[2].army != null && battle.GetSupporter(0) == null)
			{
				attacker_totals_potential.Add(battle.reinforcements[2].army.units, reset_ranged_rounds);
			}
			if (battle.reinforcements[3].army != null && battle.GetSupporter(1) == null)
			{
				defender_totals_potential.Add(battle.reinforcements[3].army.units, reset_ranged_rounds);
			}
			CalcEstimation(attacker_totals, defender_totals, reset_ranged_rounds, out estimation, out est_ratio);
			CalcEstimation(attacker_totals_potential, defender_totals_potential, reset_ranged_rounds, out estimation_potential, out est_ratio_potential);
		}
	}

	private void CalcEstimation(Totals attacker_totals, Totals defender_totals, bool reset_ranged_rounds, out float estimation, out float est_ratio)
	{
		estimation = 0.5f;
		est_ratio = 1f;
		if (attacker_totals.defense_power <= 0f && defender_totals.defense_power <= 0f)
		{
			estimation = 0.5f;
			return;
		}
		if (defender_totals.defense_power <= 0f)
		{
			estimation = 0f;
			return;
		}
		if (attacker_totals.defense_power <= 0f)
		{
			estimation = 1f;
			return;
		}
		float num = attacker_totals.attack_power / defender_totals.defense_power;
		float num2 = defender_totals.attack_power / attacker_totals.defense_power;
		est_ratio = num / num2;
		estimation = num2 / (num2 + num);
		if (reset_ranged_rounds)
		{
			if (attacker_totals.all_cav)
			{
				defender_totals.number_of_ranged_attacks = (int)Math.Min(def.max_archer_rounds, Math.Ceiling(defender_totals.highest_max_shoot_range / def.range_loss_per_round_cavalry));
			}
			else
			{
				defender_totals.number_of_ranged_attacks = (int)Math.Min(def.max_archer_rounds, Math.Ceiling(defender_totals.highest_max_shoot_range / def.range_loss_per_round));
			}
			if (defender_totals.all_cav)
			{
				attacker_totals.number_of_ranged_attacks = (int)Math.Min(def.max_archer_rounds, Math.Ceiling(attacker_totals.highest_max_shoot_range / def.range_loss_per_round_cavalry));
			}
			else
			{
				attacker_totals.number_of_ranged_attacks = (int)Math.Min(def.max_archer_rounds, Math.Ceiling(attacker_totals.highest_max_shoot_range / def.range_loss_per_round));
			}
		}
	}

	public List<string> ListTactics(int side)
	{
		return (obj as Battle).def.field?.FindChild("tactics")?.FindChild((side == 0) ? "attacker" : "defender")?.Keys();
	}

	public void DecideTactics()
	{
		List<string> list = ListTactics(0);
		if (list != null && list.Count > 0)
		{
			attacker_tactics = list[0];
		}
		List<string> list2 = ListTactics(1);
		if (list2 != null && list2.Count > 0)
		{
			defender_tactics = list2[0];
		}
	}

	public void AddInitialSquads()
	{
		if (added_initial_squads)
		{
			return;
		}
		added_initial_squads = true;
		if (obj.IsAuthority())
		{
			Battle battle = obj as Battle;
			for (int i = 0; i <= 1; i++)
			{
				List<Army> armies = battle.GetArmies(i);
				if (armies == null)
				{
					continue;
				}
				for (int j = 0; j < armies.Count; j++)
				{
					Army army = armies[j];
					if (!loaded_from_save)
					{
						AddSquads(army);
					}
					AddEquipment(army);
				}
			}
			DecideTactics();
			battle.SendState<Battle.SimSquadsState>();
		}
		totals_dirty = true;
		CalcTotals(update_last: false, reset_ranged_rounds: true);
		initial_estimation = estimation;
	}

	public override void OnStart()
	{
		AddInitialSquads();
		UpdateNextFrame();
		ranged_fortification_def = base.game.defs.Get<Fortification.Def>("RangedFortification");
		base.OnStart();
	}

	public override void OnUpdate()
	{
		using (Game.Profile("BattleSimulation.OnUpdate"))
		{
			base.OnUpdate();
			UpdateMorale();
			if (obj.IsAuthority())
			{
				Think();
				if (battle.batte_view_game == null)
				{
					Time state_end_time = base.game.time;
					if (battle.batte_view_game == null)
					{
						state_end_time += def.max_tick_time;
					}
					else
					{
						state_end_time += def.max_tick_time_battleview;
					}
					for (int i = 0; i < 2; i++)
					{
						List<Squad> squads = GetSquads(i);
						for (int j = 0; j < squads.Count; j++)
						{
							Squad squad = squads[j];
							if (squad.state > Squad.State.Idle && squad.state < Squad.State.Fled && squad.state_end_time != Time.Zero && squad.state_end_time < state_end_time && squad.state_end_time > base.game.time)
							{
								state_end_time = squad.state_end_time;
							}
						}
					}
					UpdateAfter(state_end_time - base.game.time);
				}
			}
			else
			{
				UpdateAfter(def.max_tick_time);
			}
			last_update = time;
		}
	}

	public void LogTotal(Squad sq, string msg)
	{
		LogTotal(sq.ToString() + ": " + msg);
	}

	public void LogTotal(string msg)
	{
		if (log_attacks)
		{
			Log(msg);
		}
	}

	public void OnArmyJoined(Army army)
	{
		AddSquads(army);
		AddEquipment(army);
		CalcTotals();
	}

	public void OnArmyLeft(Army army)
	{
		DelSquads(army);
		DelEquipment(army);
	}

	public Point CalcPosition(Point pos, Point dir, Squad sq, int row, int col)
	{
		pos += dir * def.dist_between_units_y * row;
		pos += dir.Right() * col * def.dist_between_units_x;
		return pos;
	}

	private float CalcHeading(Point pos, float army_heading)
	{
		float to = (-pos).Heading();
		return Angle.Lerp360(army_heading, to, 0.5f);
	}

	public int GetSquadIndex(Squad squad)
	{
		if (squad == null)
		{
			return -1;
		}
		if (squad.battle_side == 0)
		{
			return attacker_squads.IndexOf(squad);
		}
		if (squad.battle_side == 1)
		{
			int num = defender_squads.IndexOf(squad);
			if (num < 0)
			{
				return -1;
			}
			return attacker_squads.Count + num;
		}
		return -1;
	}

	public Squad GetSquadByIndex(int idx)
	{
		if (idx < 0)
		{
			return null;
		}
		if (idx < attacker_squads.Count)
		{
			return attacker_squads[idx];
		}
		idx -= attacker_squads.Count;
		if (idx < defender_squads.Count)
		{
			return defender_squads[idx];
		}
		return null;
	}

	public void AddSquad(Squad squad)
	{
		List<Squad> squads = GetSquads(squad.battle_side);
		if (squads != null)
		{
			squad.ValidateMaxTroops();
			int num = squad.def.TroopsToManpower(squad.NumTroops());
			if (squad.battle_side == 0)
			{
				attacker_totals.initial_count += num;
			}
			else
			{
				defender_totals.initial_count += num;
			}
			squads.Add(squad);
			totals_dirty = true;
		}
	}

	public void DelSquad(Squad sq)
	{
		if (sq != null)
		{
			Battle battle = obj as Battle;
			GetSquads(sq.battle_side).Remove(sq);
			if (battle.winner < 0 && !sq.IsDefeated() && sq.equipment == null)
			{
				sq.SetState(Squad.State.Left);
				battle.SendSubstate<Battle.SimSquadsState.SquadDamageState>(GetSquadIndex(sq));
			}
		}
	}

	private void PositionSquads(List<Squad> squads, Army army)
	{
		if (squads == null || squads.Count == 0)
		{
			return;
		}
		Point point;
		if (army == null)
		{
			point = battle.CalcArmyOffset(null, 1, supporter: false, battle.def.fight_range);
			Army.CalcBattleFormation(battle.settlement?.garrison?.units, 0, null);
		}
		else
		{
			point = battle.CalcArmyOffset(army, army.battle_side, army.is_supporter, battle.def.fight_range);
			army.CalcBattleFormation();
		}
		Point dir = -point.GetNormalized();
		float army_heading = dir.Heading();
		for (int i = 0; i < squads.Count; i++)
		{
			Squad squad = squads[i];
			if (squad.army == army && !(squad.initial_position != Point.Zero))
			{
				int col = squad.unit.battle_col - Army.battle_cols / 2;
				int row = -squad.unit.battle_row;
				squad.position = CalcPosition(point, dir, squad, row, col);
				squad.initial_position = squad.position;
				squad.tgt_position = squad.position;
				squad.heading = CalcHeading(squad.position, army_heading);
				squad.army_center = point;
			}
		}
	}

	private void PositionEquipment(List<InventoryItem> squads, Army army)
	{
		if (squads == null || squads.Count == 0)
		{
			return;
		}
		Point point = ((army != null) ? battle.CalcArmyOffset(army, army.battle_side, army.is_supporter, battle.def.fight_range) : battle.CalcArmyOffset(null, 1, supporter: false, battle.def.fight_range));
		for (int i = 0; i < squads.Count; i++)
		{
			InventoryItem inventoryItem = squads[i];
			if (inventoryItem.army == army)
			{
				Squad simulation = inventoryItem.simulation;
				if (!(simulation.initial_position != Point.Zero))
				{
					simulation.initial_position = point;
					simulation.position = point;
				}
			}
		}
	}

	public void PositionSettlementDefenders()
	{
		List<Squad> squads = GetSquads(1);
		PositionSquads(squads, null);
	}

	private void AddSquads(Army army)
	{
		List<Squad> squads = GetSquads(army.battle_side);
		if (squads == null)
		{
			return;
		}
		for (int i = 0; i < squads.Count; i++)
		{
			if (squads[i].unit == null)
			{
				return;
			}
		}
		Totals totals = null;
		totals = ((army.battle_side != 0) ? defender_totals : attacker_totals);
		if (squads.Count > 0)
		{
			float num = 0f;
			float num2 = 0f;
			for (int j = 0; j < army.units.Count; j++)
			{
				Unit unit = army.units[j];
				float num3 = (float)unit.max_size_modified() * unit.health;
				num += num3 * def.morale_on_reinforce_friend;
				num2 += num3 * def.morale_on_reinforce_enemy;
			}
			for (int k = 0; k < squads.Count; k++)
			{
				squads[k].AddMorale(num, acc: true, ignore_resilience: true);
			}
			List<Squad> squads2 = GetSquads(1 - army.battle_side);
			for (int l = 0; l < squads2.Count; l++)
			{
				squads2[l].AddMorale(num2, acc: true, ignore_resilience: true);
			}
			ApplyDamage();
		}
		float morale = Math.Max(army.GetMorale(), def.min_morale_on_start);
		for (int m = 0; m < army.units.Count; m++)
		{
			Unit unit2 = army.units[m];
			if (unit2.simulation == null)
			{
				Squad squad = new Squad(unit2, this, morale);
				squad.ValidateMaxTroops();
				totals.initial_count += squad.def.TroopsToManpower(squad.NumTroops());
				squads.Add(squad);
			}
		}
		PositionSquads(squads, army);
		totals_dirty = true;
	}

	public void AddEquipment(Army army)
	{
		List<InventoryItem> equipment = GetEquipment(army.battle_side);
		if (army.siege_equipment == null)
		{
			return;
		}
		for (int i = 0; i < army.siege_equipment.Count; i++)
		{
			if (army.siege_equipment[i].simulation == null)
			{
				new Squad(army.siege_equipment[i], this);
				equipment.Add(army.siege_equipment[i]);
			}
		}
		PositionEquipment(equipment, army);
	}

	private void DelSquads(Army army)
	{
		Battle battle = obj as Battle;
		List<Squad> squads = GetSquads(army.battle_side);
		if (squads == null)
		{
			return;
		}
		if (squads.Count > 0)
		{
			float num = 0f;
			float num2 = 0f;
			for (int i = 0; i < army.units.Count; i++)
			{
				Unit unit = army.units[i];
				float num3 = (float)unit.max_size_modified() * unit.health;
				num -= num3 * def.morale_on_reinforce_friend;
				num2 -= num3 * def.morale_on_reinforce_enemy;
			}
			for (int j = 0; j < squads.Count; j++)
			{
				squads[j].AddMorale(num, acc: true, ignore_resilience: true);
			}
			List<Squad> squads2 = GetSquads(1 - army.battle_side);
			for (int k = 0; k < squads2.Count; k++)
			{
				squads2[k].AddMorale(num2, acc: true, ignore_resilience: true);
			}
			ApplyDamage();
		}
		for (int num4 = squads.Count - 1; num4 >= 0; num4--)
		{
			Squad squad = squads[num4];
			if (squad.garrison == null && squad.army == army)
			{
				if (squad.unit != null)
				{
					squad.unit.simulation = null;
				}
				if (!battle.IsFinishing() || battle.batte_view_game != null)
				{
					squads.RemoveAt(num4);
				}
			}
		}
		battle.SendState<Battle.SimSquadsState>();
		if (battle.IsFinishing())
		{
			army.SendState<Army.UnitsState>();
		}
		totals_dirty = true;
	}

	private void DelEquipment(Army army)
	{
		Battle battle = obj as Battle;
		List<InventoryItem> equipment = GetEquipment(army.battle_side);
		if (equipment == null)
		{
			return;
		}
		for (int num = equipment.Count - 1; num >= 0; num--)
		{
			InventoryItem inventoryItem = equipment[num];
			if (inventoryItem.army == army)
			{
				inventoryItem.simulation = null;
				if (!battle.IsFinishing() || battle.batte_view_game != null)
				{
					equipment.RemoveAt(num);
				}
			}
		}
		totals_dirty = true;
	}

	private void ThinkAttrition()
	{
		if ((battle.type != Battle.Type.Assault && battle.type != Battle.Type.Siege) || base.game.time < next_attrition_tick)
		{
			return;
		}
		next_attrition_tick = base.game.time + battle.def.attrition_tick;
		for (int i = 0; i <= 1; i++)
		{
			GetSquads(i);
			List<InventoryItem> equipment = GetEquipment(i);
			List<Squad> squads = GetSquads(1 - i);
			List<Squad> list = new List<Squad>();
			for (int j = 0; j < squads.Count; j++)
			{
				Squad squad = squads[j];
				if (!squad.IsDefeated())
				{
					list.Add(squad);
				}
			}
			if (list != null && list.Count != 0)
			{
				if (i == 1)
				{
					ThinkAttritionTowers(i, list);
				}
				for (int k = 0; k < equipment.Count; k++)
				{
					InventoryItem item = equipment[k];
					ThinKAttritionSiegeEquipment(item, i, list);
				}
			}
		}
	}

	private void Think()
	{
		Battle battle = obj as Battle;
		if (totals_dirty)
		{
			CalcTotals(update_last: true);
			battle.NotifyListeners("changed");
		}
		if (battle.stage != Battle.Stage.Ongoing)
		{
			return;
		}
		if (battle.batte_view_game == null)
		{
			if (battle.type == Battle.Type.Siege)
			{
				ThinkSiege();
			}
			else
			{
				ThinkNormal();
			}
			ThinkAttrition();
		}
		else
		{
			for (int i = 0; i <= 1; i++)
			{
				List<Squad> squads = GetSquads(i);
				if (squads != null)
				{
					for (int j = 0; j < squads.Count; j++)
					{
						Squad sq = squads[j];
						UpdateState(sq);
					}
				}
			}
		}
		ApplyDamage();
		if (totals_dirty)
		{
			CalcTotals(update_last: true);
			battle.NotifyListeners("changed");
		}
	}

	public bool ThinkSurrender(int side)
	{
		if (!MoraleEffectsActive)
		{
			return false;
		}
		if (def.surrender_overall_morale_chance == null)
		{
			return false;
		}
		float level = OverallMorale(side);
		float chance = Def.StateInfo.GetChance(def.surrender_overall_morale_chance, level);
		if ((float)base.game.Random(0, 100) < chance)
		{
			if (battle.batte_view_game != null)
			{
				Kingdom sideKingdom = battle.GetSideKingdom(side);
				if (sideKingdom == null || sideKingdom.is_player)
				{
					return false;
				}
				battle.SetBattleViewVictoryReason(Battle.VictoryReason.Surrender, 1 - side);
			}
			else
			{
				battle.SetVictoryReason(Battle.VictoryReason.Surrender);
			}
			return true;
		}
		return false;
	}

	private void UpdateMorale()
	{
		float num = time - last_update;
		if (num <= 0f)
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			List<Squad> squads = GetSquads(i);
			for (int j = 0; j < squads.Count; j++)
			{
				Squad squad = squads[j];
				UpdateMorale(squad, num);
			}
		}
	}

	private void Surrender(Squad squad)
	{
		if (squad.sub_squads != null)
		{
			for (int i = 0; i < squad.sub_squads.Count; i++)
			{
				Squad squad2 = squad.sub_squads[i];
				Surrender(squad2);
			}
		}
		else if (!squad.IsDefeated())
		{
			squad.SetState(Squad.State.Fled);
		}
	}

	private void UpdateMorale(Squad squad, float elapsed)
	{
		if (squad?.unit?.battle == null)
		{
			return;
		}
		if (squad.sub_squads != null)
		{
			for (int i = 0; i < squad.sub_squads.Count; i++)
			{
				Squad squad2 = squad.sub_squads[i];
				UpdateMorale(squad2, elapsed);
			}
		}
		else
		{
			if (squad.IsDefeated())
			{
				return;
			}
			squad.RecalcPermanentMorale();
			if (battle.stage == Battle.Stage.Ongoing)
			{
				if (squad.temporary_morale > 0f)
				{
					squad.temporary_morale += elapsed * squad.def.morale_decay_modified(squad.unit);
					squad.temporary_morale = Math.Max(0f, squad.temporary_morale);
				}
				else if (squad.temporary_morale < 0f)
				{
					squad.temporary_morale += elapsed * squad.def.morale_recovery_modified(squad.unit);
					squad.temporary_morale = Math.Min(0f, squad.temporary_morale);
				}
			}
		}
	}

	private bool ThinkArcherRounds()
	{
		_ = obj;
		bool flag = false;
		int num = 0;
		float num2 = 0f;
		for (int i = 0; i <= 1; i++)
		{
			float num3 = 0f;
			if (i == 0)
			{
				if (attacker_totals.number_of_ranged_attacks == 0)
				{
					continue;
				}
				num3 = ((!defender_totals.all_cav) ? ((float)attacker_totals.number_of_ranged_attacks * def.range_loss_per_round) : ((float)attacker_totals.number_of_ranged_attacks * def.range_loss_per_round_cavalry));
				if (attacker_totals.number_of_ranged_attacks > num)
				{
					num = attacker_totals.number_of_ranged_attacks;
					num2 = attacker_totals.highest_max_shoot_range_attack_duration;
				}
				attacker_totals.number_of_ranged_attacks--;
			}
			else
			{
				if (defender_totals.number_of_ranged_attacks == 0)
				{
					continue;
				}
				num3 = ((!attacker_totals.all_cav) ? ((float)defender_totals.number_of_ranged_attacks * def.range_loss_per_round) : ((float)defender_totals.number_of_ranged_attacks * def.range_loss_per_round_cavalry));
				if (defender_totals.number_of_ranged_attacks > num)
				{
					num = defender_totals.number_of_ranged_attacks;
					num2 = defender_totals.highest_max_shoot_range_attack_duration;
				}
				defender_totals.number_of_ranged_attacks--;
			}
			List<Squad> squads = GetSquads(i);
			for (int j = 0; j < squads.Count; j++)
			{
				Squad squad = squads[j];
				if (squad.IsDefeated() || !squad.def.is_ranged)
				{
					continue;
				}
				if (squad.salvo_def.max_shoot_range < num3)
				{
					if (squad.state != Squad.State.Moving)
					{
						float num4 = 1f - squad.salvo_def.max_shoot_range / num3;
						Squad enemy = GetEnemy(squad, ranged: true);
						if (enemy != null)
						{
							Point point = enemy.position - squad.position;
							squad.SetState(Squad.State.Moving, squad.position + point * num4);
						}
					}
				}
				else
				{
					Squad enemy2 = GetEnemy(squad, ranged: true);
					if (enemy2 != null)
					{
						flag = true;
						Shoot(squad, enemy2);
					}
				}
			}
		}
		if (!flag)
		{
			if (thought_archer_rounds_last_tick)
			{
				for (int k = 0; k < 2; k++)
				{
					List<Squad> squads2 = GetSquads(k);
					for (int l = 0; l < squads2.Count; l++)
					{
						Squad squad2 = squads2[l];
						if (!squad2.IsDefeated())
						{
							squad2.SetState(Squad.State.Idle);
						}
					}
				}
			}
		}
		else if (flag)
		{
			float fixed_duration = (float)num * num2;
			for (int m = 0; m < 2; m++)
			{
				List<Squad> squads3 = GetSquads(m);
				for (int n = 0; n < squads3.Count; n++)
				{
					Squad squad3 = squads3[n];
					if (!squad3.IsDefeated() && !squad3.def.is_ranged && squad3.def.type != Unit.Type.Noble && squad3.state != Squad.State.Moving)
					{
						Squad enemy3 = GetEnemy(squad3, ranged: false);
						if (enemy3 != null)
						{
							squad3.SetState(Squad.State.Moving, enemy3, fixed_duration);
						}
					}
				}
			}
		}
		thought_archer_rounds_last_tick = flag;
		return flag;
	}

	private void ThinkNormal()
	{
		if ((obj as Battle).batte_view_game != null || ThinkArcherRounds())
		{
			return;
		}
		for (int i = 0; i <= 1; i++)
		{
			List<Squad> squads = GetSquads(i);
			if (squads != null)
			{
				for (int j = 0; j < squads.Count; j++)
				{
					Squad sq = squads[j];
					TryDisengage(sq);
				}
			}
		}
		for (int k = 0; k <= 1; k++)
		{
			List<Squad> squads2 = GetSquads(k);
			if (squads2 != null)
			{
				for (int l = 0; l < squads2.Count; l++)
				{
					Squad sq2 = squads2[l];
					Think(sq2);
				}
			}
		}
	}

	private void ThinkSiege()
	{
		Battle battle = obj as Battle;
		if (!(base.game.time >= next_siege_tick))
		{
			return;
		}
		next_siege_tick = base.game.time + battle.def.attrition_tick;
		for (int i = 0; i <= 1; i++)
		{
			List<Squad> squads = GetSquads(i);
			GetEquipment(i);
			List<Squad> squads2 = GetSquads(1 - i);
			List<Squad> list = new List<Squad>();
			for (int j = 0; j < squads2.Count; j++)
			{
				Squad squad = squads2[j];
				if (!squad.IsDefeated())
				{
					list.Add(squad);
				}
			}
			if (list == null || list.Count == 0)
			{
				continue;
			}
			if (i == 1)
			{
				Realm realm = battle.settlement.GetRealm();
				int num = 0;
				for (int k = 0; k < realm.settlements.Count; k++)
				{
					Settlement settlement = realm.settlements[k];
					if (settlement.level != 0 && settlement.IsActiveSettlement() && !settlement.IsOccupied())
					{
						num += (int)settlement.def.attrition.GetFlat(settlement.level);
					}
				}
				if (num > 0)
				{
					List<Unit.Def> availableUnitTypes = realm.castle.GetAvailableUnitTypes();
					float num2 = float.MinValue;
					Unit.Def def = null;
					for (int l = 0; l < availableUnitTypes.Count; l++)
					{
						float num3 = availableUnitTypes[l].siege_strength_modified(battle, use_battle_bonuses: true, i, 0, null, battle.settlement?.garrison);
						if (num3 > num2)
						{
							num2 = num3;
							def = availableUnitTypes[l];
						}
					}
					if (def != null)
					{
						for (int m = 0; m < num; m++)
						{
							Squad enemy = list[base.game.Random(0, list.Count)];
							KeepsAttack(def, enemy);
						}
					}
				}
			}
			if (squads == null || squads.Count == 0)
			{
				continue;
			}
			int req_melee_attrition_troops = battle.def.req_melee_attrition_troops;
			int req_ranged_attrition_troops = battle.def.req_ranged_attrition_troops;
			for (int n = 0; n < squads.Count; n++)
			{
				Squad squad2 = squads[n];
				if (squad2.IsDefeated())
				{
					continue;
				}
				float num4 = squad2.NumTroops();
				bool flag = squad2.def.is_ranged && squad2.remaining_salvos > 0f;
				int num5;
				if (flag)
				{
					num5 = (int)Math.Floor(num4 / (float)req_ranged_attrition_troops);
					if ((int)num4 % req_ranged_attrition_troops > base.game.Random(0, req_ranged_attrition_troops))
					{
						num5++;
					}
				}
				else
				{
					num5 = (int)Math.Floor(num4 / (float)req_melee_attrition_troops);
					if ((int)num4 % req_melee_attrition_troops > base.game.Random(0, req_melee_attrition_troops))
					{
						num5++;
					}
				}
				Character character = (battle.settlement as Castle)?.governor;
				float num6 = 1f;
				if (i == 1 && character != null)
				{
					num6 *= 1f + battle.settlement.GetRealm().GetStat(Stats.rs_governor_enemy_attrition) / 100f;
				}
				for (int num7 = 0; num7 < num5; num7++)
				{
					Squad enemy2 = list[base.game.Random(0, list.Count)];
					if (flag)
					{
						Shoot(squad2, enemy2, num6 * 3f / squad2.def.shoot_interval);
					}
					else
					{
						Attack(squad2, enemy2, -1, num6 * 3f / squad2.def.attack_interval);
					}
				}
			}
		}
	}

	private void ThinkAttritionTowers(int battle_side, List<Squad> enemies)
	{
		if (ranged_fortification_def != null)
		{
			if (fortification_num_arrows == -1)
			{
				fortification_num_arrows = ranged_fortification_def.num_arrows.Int(battle);
				fortification_cth = ranged_fortification_def.CTH.Float(battle);
			}
			int num = (int)((float)fortification_num_arrows * battle.siege_defense / battle.initial_siege_defense_pre_condition);
			float cth = fortification_cth;
			float shoot_interval = ranged_fortification_def.shoot_interval;
			Squad enemy = GetEnemy(ranged_fortification_def, battle_side, ranged: true);
			int req_equipment_attrition_troops = battle.def.req_equipment_attrition_troops;
			int num2 = (int)Math.Floor((float)num / (float)req_equipment_attrition_troops);
			if (num % req_equipment_attrition_troops > base.game.Random(0, req_equipment_attrition_troops))
			{
				num2++;
			}
			AttackAttrition(num, cth, enemy, (float)num2 * 3f / shoot_interval);
		}
	}

	private void ThinKAttritionSiegeEquipment(InventoryItem item, int battle_side, List<Squad> enemies)
	{
		if (item.def.item_type == Unit.ItemType.SiegeEquipment)
		{
			int arrows_per_troop = item.simulation.salvo_def.arrows_per_troop;
			float cth = item.def.CTH_modified(battle, use_battle_bonuses: true, item.simulation.battle_side, 0, null, battle.realm_id, item.army?.leader, null, null) * item.def.CTH_shoot_mod_modified();
			float shoot_interval = item.def.shoot_interval;
			Squad enemy = GetEnemy(item.def, battle_side, ranged: true);
			int req_equipment_attrition_troops = battle.def.req_equipment_attrition_troops;
			int num = (int)Math.Floor((float)arrows_per_troop / (float)req_equipment_attrition_troops);
			if (arrows_per_troop % req_equipment_attrition_troops > base.game.Random(0, req_equipment_attrition_troops))
			{
				num++;
			}
			AttackAttrition(arrows_per_troop, cth, enemy, (float)num * item.def.attrition_mod * 3f / shoot_interval);
		}
	}

	private bool TryDisengage(Squad sq)
	{
		if (sq.state != Squad.State.Attacking || sq.state_end_time > time)
		{
			return false;
		}
		if (sq.engaged_squads.Count == 0)
		{
			return false;
		}
		Def.StateInfo stateInfo = def.state_infos[5];
		float chance = stateInfo.GetChance(0f, sq);
		if (base.game.Random(0f, 100f) < chance)
		{
			Disengage(sq);
			return true;
		}
		return false;
	}

	private void Think(Squad sq)
	{
		UpdateState(sq);
		if (sq.state != Squad.State.Idle || sq.state_end_time != Time.Zero)
		{
			return;
		}
		Squad squad = null;
		if (sq.remaining_salvos > 0f && sq.engaged_squads.Count == 0)
		{
			squad = GetEnemy(sq, ranged: true);
			if (squad != null)
			{
				Shoot(sq, squad);
			}
			return;
		}
		squad = GetEnemy(sq, ranged: false);
		if (squad == null)
		{
			return;
		}
		if (sq.engaged_squads.Count == 0 && sq.def.type == Unit.Type.Noble)
		{
			List<Squad> squads = GetSquads(sq.battle_side);
			bool flag = true;
			for (int i = 0; i < squads.Count; i++)
			{
				Squad squad2 = squads[i];
				if (squad2 != null && !squad2.IsDefeated() && squad2.def.type != Unit.Type.Noble)
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				List<Squad> squads2 = GetSquads(1 - sq.battle_side);
				int num = 0;
				for (int j = 0; j < squads2.Count; j++)
				{
					Squad squad3 = squads2[j];
					if (squad3 != null && !squad3.IsDefeated() && squad3.def.type != Unit.Type.Noble)
					{
						num++;
					}
				}
				if (num > 1)
				{
					return;
				}
			}
		}
		if (sq.target == null || sq.target != squad || sq.position.Dist(squad.position) > sq.def.move_speed)
		{
			sq.SetState(Squad.State.Moving, squad);
			return;
		}
		Engage(sq, squad);
		Engage(squad, sq);
		Attack(sq, squad);
	}

	private void UpdateState(Squad sq)
	{
		if (sq.sub_squads != null)
		{
			for (int i = 0; i < sq.sub_squads.Count; i++)
			{
				Squad sq2 = sq.sub_squads[i];
				UpdateState(sq2);
			}
		}
		else if (sq.state != Squad.State.Idle && sq.target != null && sq.target.IsDefeated())
		{
			sq.SetState(Squad.State.Idle, sq.target);
			Disengage(sq, sq.target);
			totals_dirty = true;
		}
		else if (!(sq.state_end_time == Time.Zero) && !(sq.state_end_time > time))
		{
			sq.AddMorale(sq.state);
			sq.SetState(Squad.State.Idle, sq.target);
			totals_dirty = true;
		}
	}

	private bool IsValidEnemy(Squad sq, Squad enemy)
	{
		if (enemy == null)
		{
			return false;
		}
		if (enemy.battle_side != 1 - sq.battle_side)
		{
			return false;
		}
		if (enemy.IsDefeated())
		{
			return false;
		}
		return true;
	}

	private void FindClosestTargets(Squad squad, List<Squad> enemies, List<Squad> my_squads, bool ignore_cavalry, bool ignore_disengaging, out Squad closest_archer, out Squad closest_cavalry, out Squad closest_non_marshal, out Squad closest_squad, out Squad closest_unengaged_archer, out Squad closest_unengaged_cavalry, out Squad closest_unengaged_non_marshal, out Squad closest_unengaged_squad)
	{
		closest_archer = null;
		closest_cavalry = null;
		closest_squad = null;
		closest_non_marshal = null;
		closest_unengaged_archer = null;
		closest_unengaged_cavalry = null;
		closest_unengaged_non_marshal = null;
		closest_unengaged_squad = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MaxValue;
		float num4 = float.MaxValue;
		float num5 = float.MaxValue;
		float num6 = float.MaxValue;
		float num7 = float.MaxValue;
		float num8 = float.MaxValue;
		Point pt = Point.Zero;
		if (squad != null)
		{
			pt = squad.position;
		}
		for (int i = 0; i < enemies.Count; i++)
		{
			Squad squad2 = enemies[i];
			if (squad2 == null || squad2.IsDefeated() || (ignore_cavalry && squad2.def.is_cavalry) || (ignore_disengaging && (squad2.state == Squad.State.Disengaging || squad2.state == Squad.State.Retreating)))
			{
				continue;
			}
			float num9 = squad2.position.SqrDist(pt);
			int cached = -1;
			if (squad2.def.type != Unit.Type.Noble)
			{
				if (num9 < num4)
				{
					num4 = num9;
					closest_non_marshal = squad2;
				}
				if (num9 < num8 && !AlreadyTargetted(squad, squad2, my_squads, ref cached))
				{
					num8 = num9;
					closest_unengaged_non_marshal = squad2;
				}
				if (squad2.def.is_ranged)
				{
					if (num9 < num)
					{
						num = num9;
						closest_archer = squad2;
					}
					if (num9 < num5 && !AlreadyTargetted(squad, squad2, my_squads, ref cached))
					{
						num5 = num9;
						closest_unengaged_archer = squad2;
					}
				}
				if (squad2.def.is_cavalry)
				{
					if (num9 < num2)
					{
						num2 = num9;
						closest_cavalry = squad2;
					}
					if (num9 < num6 && !AlreadyTargetted(squad, squad2, my_squads, ref cached))
					{
						num6 = num9;
						closest_unengaged_cavalry = squad2;
					}
				}
			}
			if (num9 < num3)
			{
				num3 = num9;
				closest_squad = squad2;
			}
			if (num9 < num7 && !AlreadyTargetted(squad, squad2, my_squads, ref cached))
			{
				num7 = num9;
				closest_unengaged_squad = squad2;
			}
		}
	}

	private bool AlreadyTargetted(Squad sq, Squad enemy, List<Squad> my_squads, ref int cached)
	{
		if (cached != -1)
		{
			return cached == 1;
		}
		bool flag = false;
		if (sq != null)
		{
			for (int i = 0; i < my_squads.Count; i++)
			{
				Squad squad = my_squads[i];
				if (squad != sq && squad.target == enemy && squad.state != Squad.State.Shooting)
				{
					flag = true;
					break;
				}
			}
		}
		cached = (flag ? 1 : 0);
		return flag;
	}

	private Squad PickRandomClosestEnemy(Squad sq, List<Squad> enemies, bool ranged, bool repick_if_no_marshal, bool ignore_cavalry, bool ignore_disengaging)
	{
		List<Squad> squads = GetSquads(sq.battle_side);
		FindClosestTargets(sq, enemies, squads, ignore_cavalry, ignore_disengaging, out var closest_archer, out var closest_cavalry, out var closest_non_marshal, out var closest_squad, out var closest_unengaged_archer, out var closest_unengaged_cavalry, out var closest_unengaged_non_marshal, out var closest_unengaged_squad);
		if (closest_archer != null && (float)base.game.Random(0, 100) < sq.def.chance_target_closest_archer)
		{
			if (closest_unengaged_archer != null && (float)base.game.Random(0, 100) < sq.def.chance_target_already_targetted_squad)
			{
				return closest_unengaged_archer;
			}
			return closest_archer;
		}
		if (closest_cavalry != null && (float)base.game.Random(0, 100) < sq.def.chance_target_closest_cavalry)
		{
			if (closest_unengaged_cavalry != null && (float)base.game.Random(0, 100) < sq.def.chance_target_already_targetted_squad)
			{
				return closest_unengaged_cavalry;
			}
			return closest_cavalry;
		}
		if (closest_non_marshal != null && (float)base.game.Random(0, 100) < sq.def.chance_ignore_marshal)
		{
			if (closest_unengaged_non_marshal != null && (float)base.game.Random(0, 100) < sq.def.chance_target_already_targetted_squad)
			{
				return closest_unengaged_non_marshal;
			}
			return closest_non_marshal;
		}
		if (closest_unengaged_squad != null && (float)base.game.Random(0, 100) < sq.def.chance_target_already_targetted_squad)
		{
			return closest_unengaged_squad;
		}
		return closest_squad;
	}

	private Squad PickRandomEnemy(float chance_target_already_targetted_squad, float chance_target_closest_archer, float chance_target_closest_cavalry, float chance_ignore_marshal, int battle_side, List<Squad> enemies, bool ranged, bool repick_if_no_marshal, bool ignore_cavalry, bool ignore_disengaging)
	{
		List<Squad> squads = GetSquads(battle_side);
		FindClosestTargets(null, enemies, squads, ignore_cavalry, ignore_disengaging, out var closest_archer, out var closest_cavalry, out var closest_non_marshal, out var closest_squad, out var closest_unengaged_archer, out var closest_unengaged_cavalry, out var closest_unengaged_non_marshal, out var closest_unengaged_squad);
		if (closest_archer != null && (float)base.game.Random(0, 100) < chance_target_closest_archer)
		{
			if (closest_unengaged_archer != null && (float)base.game.Random(0, 100) < chance_target_already_targetted_squad)
			{
				return closest_unengaged_archer;
			}
			return closest_archer;
		}
		if (closest_cavalry != null && (float)base.game.Random(0, 100) < chance_target_closest_cavalry)
		{
			if (closest_unengaged_cavalry != null && (float)base.game.Random(0, 100) < chance_target_already_targetted_squad)
			{
				return closest_unengaged_cavalry;
			}
			return closest_cavalry;
		}
		if (closest_non_marshal != null && (float)base.game.Random(0, 100) < chance_ignore_marshal)
		{
			if (closest_unengaged_non_marshal != null && (float)base.game.Random(0, 100) < chance_target_already_targetted_squad)
			{
				return closest_unengaged_non_marshal;
			}
			return closest_non_marshal;
		}
		if (closest_unengaged_squad != null && (float)base.game.Random(0, 100) < chance_target_already_targetted_squad)
		{
			return closest_unengaged_squad;
		}
		return closest_squad;
	}

	private Squad GetEnemy(Squad sq, bool ranged)
	{
		Squad squad = null;
		List<Squad> list = ((sq.engaged_squads.Count <= 0) ? GetSquads(1 - sq.battle_side) : sq.engaged_squads);
		if (list == null)
		{
			return null;
		}
		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				squad = PickRandomClosestEnemy(sq, list, ranged, repick_if_no_marshal: false, j == 0 && (float)base.game.Random(0, 100) < sq.def.chance_ignore_cavalry, i == 0);
				if (squad != null)
				{
					break;
				}
			}
			if (squad != null)
			{
				break;
			}
		}
		return squad;
	}

	private Squad GetEnemy(Unit.Def def, int battle_side, bool ranged)
	{
		Squad squad = null;
		List<Squad> squads = GetSquads(1 - battle_side);
		if (squads == null)
		{
			return null;
		}
		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				squad = PickRandomEnemy(def.chance_target_already_targetted_squad, def.chance_target_closest_archer, def.chance_target_closest_cavalry, def.chance_ignore_marshal, battle_side, squads, ranged, repick_if_no_marshal: false, j == 0 && (float)base.game.Random(0, 100) < def.chance_ignore_cavalry, i == 0);
				if (squad != null)
				{
					break;
				}
			}
			if (squad != null)
			{
				break;
			}
		}
		return squad;
	}

	private Squad GetEnemy(Fortification.Def def, int battle_side, bool ranged)
	{
		Squad squad = null;
		List<Squad> squads = GetSquads(1 - battle_side);
		if (squads == null)
		{
			return null;
		}
		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				squad = PickRandomEnemy(def.chance_target_already_targetted_squad, def.chance_target_closest_archer, def.chance_target_closest_cavalry, def.chance_ignore_marshal, battle_side, squads, ranged, repick_if_no_marshal: false, j == 0 && (float)base.game.Random(0, 100) < def.chance_ignore_cavalry, i == 0);
				if (squad != null)
				{
					break;
				}
			}
			if (squad != null)
			{
				break;
			}
		}
		return squad;
	}

	public void Attack(Squad sq, Squad enemy, int battle_side = -1, float mult = 1f, bool try_shock = true)
	{
		if (enemy != null)
		{
			if (battle_side < 0)
			{
				battle_side = sq.battle_side;
			}
			_ = obj;
			sq.SetState(Squad.State.Attacking, enemy);
			float num = sq.NumTroops();
			float num2 = sq.CTH_Modified(enemy);
			float num3 = 100f + enemy.defense_Modified(sq);
			float num4 = num * mult * num2 / num3;
			float num5 = def.global_cth_mod * def.balance_constant_melee * num4 / (float)enemy.max_troops;
			enemy.damage_acc += num5;
			if (try_shock)
			{
				sq.TryShock(enemy);
			}
			sq.number_attacks++;
			enemy.number_attacked++;
		}
	}

	public void KeepsAttack(Unit.Def def, Squad enemy, float size = 1f)
	{
		Battle battle = obj as Battle;
		float num = def.CTH_modified(battle, use_battle_bonuses: true, 1, 1, enemy, 0, null, battle.settlement?.garrison, null);
		float num2 = 100f + enemy.defense_Modified();
		float num3 = size * num / num2;
		LogTotal($"{def.field.key} attacked {enemy} from keeps and killed {num3} troops");
		enemy.damage_acc += this.def.global_cth_mod * this.def.balance_constant_melee * num3 / (float)enemy.max_troops;
		enemy.number_attacked++;
	}

	private void Attack(InventoryItem equipment, Squad enemy, int battle_side)
	{
		float num = equipment.def.CTH_ranged_base();
		if (equipment.simulation.salvo_def != null)
		{
			num *= (float)equipment.simulation.salvo_def.arrows_per_troop;
		}
		float num2 = 100f + enemy.defense_Modified_siege(equipment);
		float num3 = (float)equipment.def.size * num / num2;
		LogTotal($"{equipment.def.field.key} attacked {enemy} and killed {num3} troops");
		enemy.damage_acc += def.global_cth_mod * def.balance_constant_melee * num3;
		enemy.number_attacked++;
	}

	public float NobleDefense(Squad noble)
	{
		float num = 0f;
		if (noble.def.type != Unit.Type.Noble || noble.army == null)
		{
			return num;
		}
		for (int i = 0; i < noble.army.units.Count; i++)
		{
			Unit unit = noble.army.units[i];
			if (unit?.simulation != null && unit.simulation != noble && unit.def.type != Unit.Type.Noble && !unit.IsDefeated())
			{
				num += def.noble_defense_per_unit_in_army * (1f - unit.simulation.damage) / unit.simulation.max_damage;
			}
		}
		return num;
	}

	private void AttackAttrition(float cnt, float cth, Squad enemy, float mul = 1f)
	{
		if (enemy?.unit != null)
		{
			float num = 100f + enemy.defense_against_ranged_Modified();
			float num2 = mul * cnt * cth / num;
			enemy.damage_acc += def.global_cth_mod * def.balance_constant_attrition * num2 / (float)enemy.max_troops;
		}
	}

	private void Shoot(Squad sq, Squad enemy, float mul = 1f, bool splash = true)
	{
		if (sq.unit == null || enemy?.unit == null || sq.state >= Squad.State.Retreating)
		{
			return;
		}
		if (splash)
		{
			sq.SetState(Squad.State.Shooting, enemy);
		}
		float num = sq.NumTroops();
		float num2 = sq.CTH_Ranged_Modified(enemy);
		float num3 = 100f + enemy.defense_against_ranged_Modified();
		float num4 = mul * num * num2 / num3;
		enemy.damage_acc += def.global_cth_mod * def.balance_constant_ranged * num4 / (float)enemy.max_troops;
		sq.TryShock(enemy);
		sq.remaining_salvos -= def.salvo_per_shot;
		if (sq.remaining_salvos < 0f)
		{
			sq.remaining_salvos = 0f;
		}
		if (splash)
		{
			if (enemy.engaged_squads.Count > 0)
			{
				Squad enemy2 = GetEnemy(enemy, ranged: true);
				Shoot(sq, enemy2, Math.Max(0f, Math.Min(1f, sq.salvo_def.friendly_fire_mod * (1f - enemy2.FriendlyFireReduction() / 100f))), splash: false);
			}
			sq.number_attacks++;
			enemy.number_attacked++;
		}
	}

	private void Engage(Squad sq, Squad enemy)
	{
		if (enemy == null || sq == null)
		{
			return;
		}
		if (enemy.state >= Squad.State.Retreating || sq.state >= Squad.State.Retreating)
		{
			Disengage(sq, enemy);
		}
		else
		{
			if (sq.engaged_squads.Contains(enemy))
			{
				return;
			}
			bool num = sq.engaged_squads.Count == 0;
			sq.engaged_squads.Add(enemy);
			if (num)
			{
				if (enemy.def.type == Unit.Type.Defense && enemy.engaged_squads.Count == 0)
				{
					Attack(enemy, sq);
				}
				if (sq.def.is_cavalry && !enemy.def.is_cavalry)
				{
					Trample(sq, enemy);
				}
			}
		}
	}

	private void Disengage(Squad sq)
	{
		sq.SetState(Squad.State.Disengaging);
	}

	private void Disengage(Squad sq, Squad enemy)
	{
		sq.engaged_squads.Remove(enemy);
		enemy.engaged_squads.Remove(sq);
	}

	private void Trample(Squad sq, Squad enemy)
	{
		Battle battle = obj as Battle;
		if (!battle.is_siege && battle.type != Battle.Type.Naval)
		{
			Attack(sq, enemy, -1, def.balance_constant_trample * sq.unit.trample_chance_modified(), try_shock: false);
			sq.TryShock(enemy, is_trample: true);
		}
	}

	private void ApplyDamage()
	{
		int num = 0;
		for (int i = 0; i <= 1; i++)
		{
			List<Squad> squads = GetSquads(i);
			if (squads != null)
			{
				bool has_bad_morale = false;
				for (int num2 = squads.Count - 1; num2 >= 0; num2--)
				{
					Squad sq = squads[num2];
					ApplyDamageMorale(sq, num, ref has_bad_morale);
					num++;
				}
			}
		}
	}

	private void ApplyDamageMorale(Squad sq, int idx, ref bool has_bad_morale)
	{
		if (sq.sub_squads != null)
		{
			for (int num = sq.sub_squads.Count - 1; num >= 0; num--)
			{
				Squad sq2 = sq.sub_squads[num];
				ApplyDamageMorale(sq2, idx, ref has_bad_morale);
			}
			return;
		}
		if (ApplyDamage(sq))
		{
			obj.SendSubstate<Battle.SimSquadsState.SquadDamageState>(idx);
		}
		if (sq.temporary_morale_acc < 0f)
		{
			has_bad_morale = true;
		}
		if (ApplyMorale(sq))
		{
			obj.SendSubstate<Battle.SimSquadsState.SquadMoraleState>(idx);
		}
		if (sq.is_dirty)
		{
			sq.is_dirty = false;
			obj.SendSubstate<Battle.SimSquadsState.SquadStateInfoState>(idx);
		}
	}

	public void CheckSurrender(Squad sq)
	{
		if (battle.batte_view_game != null)
		{
			return;
		}
		Army army = sq.army;
		if (army == null)
		{
			return;
		}
		float army_surrender_marshal_death = army.def.army_surrender_marshal_death;
		if (!(army_surrender_marshal_death > 0f))
		{
			return;
		}
		for (int i = 0; i < army.units.Count; i++)
		{
			Squad simulation = army.units[i].simulation;
			if (simulation != sq && !simulation.IsDefeated() && simulation.def.type != Unit.Type.Noble && simulation.def.type != Unit.Type.InventoryItem && base.game.Random(0f, 100f) < army_surrender_marshal_death)
			{
				simulation.SetState(Squad.State.Fled);
			}
		}
	}

	public bool ApplyDamage(Squad sq, bool force = false)
	{
		if (sq == null)
		{
			return false;
		}
		if (!force && (sq.damage_acc == 0f || sq.state == Squad.State.Dead || sq.state == Squad.State.Left))
		{
			return false;
		}
		totals_dirty = true;
		if (sq.def.type == Unit.Type.Noble && sq.damage + sq.damage_acc >= 1f)
		{
			CheckSurrender(sq);
		}
		if (sq.squad != null)
		{
			sq.squad.OnTrigger("took_damage");
		}
		sq.CheckThresholds();
		float num = sq.damage_acc / (1f - sq.damage);
		sq.AddMorale(num * def.morale_drop_from_damage);
		if (battle.batte_view_game == null || battle.IsFinishing() || battle.battle_map_finished)
		{
			sq.damage += sq.damage_acc;
		}
		if (sq.unit != null && sq.main_squad == null)
		{
			sq.unit.SetDamage(sq.damage);
		}
		sq.damage_acc = 0f;
		int num2 = sq.NumTroops();
		int num3 = sq.def.min_troops_in_battle();
		if (sq.damage >= 1f)
		{
			sq.SetState(Squad.State.Dead);
			return true;
		}
		if (num2 <= num3 && (sq.squad == null || sq.squad.is_main_squad))
		{
			sq.SetState((battle.batte_view_game == null) ? Squad.State.Dead : Squad.State.Fled);
			if (battle.batte_view_game == null || battle.IsFinishing() || battle.battle_map_finished)
			{
				sq.damage = 1f;
				if (sq.unit != null && sq.main_squad == null)
				{
					sq.unit.SetDamage(sq.damage);
				}
			}
			return true;
		}
		return true;
	}

	public bool ApplyMorale(Squad sq)
	{
		if (sq.temporary_morale_acc == 0f || sq.state == Squad.State.Dead || sq.state == Squad.State.Left)
		{
			return false;
		}
		totals_dirty = true;
		float temporary_morale_acc = sq.temporary_morale_acc;
		if (def.morale_threshold_divider > 0f)
		{
			int num = (int)Math.Ceiling(sq.GetMorale() / def.morale_threshold_divider);
			sq.temporary_morale += temporary_morale_acc;
			sq.temporary_morale_acc = 0f;
			if ((int)Math.Floor(sq.GetMorale() / def.morale_threshold_divider) - num < -1)
			{
				sq.CheckThresholds();
			}
		}
		else
		{
			sq.temporary_morale += temporary_morale_acc;
			sq.temporary_morale_acc = 0f;
			if (temporary_morale_acc < 0f)
			{
				sq.CheckThresholds();
			}
		}
		return true;
	}

	public void OnRestart()
	{
		last_update = time;
	}

	public void RestartTotals()
	{
		attacker_totals.starting_capture_point_count = 0;
		defender_totals.starting_capture_point_count = 0;
	}
}
