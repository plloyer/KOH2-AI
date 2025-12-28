using System;
using System.Collections.Generic;

namespace Logic;

public class Unit : IVars
{
	public enum Type
	{
		Militia = 0,
		Infantry = 1,
		Defense = 2,
		Cavalry = 3,
		Ranged = 4,
		Guard = 5,
		InventoryItem = 6,
		Noble = 7,
		COUNT = 8,
		Invalid = 8
	}

	public enum ItemType
	{
		SiegeEquipment = 0,
		Food = 1,
		COUNT = 2,
		Invalid = 2
	}

	public class BonusDef
	{
		public enum StackMode
		{
			Own,
			Max,
			Add
		}

		public string name;

		public DT.Field field;

		public Def unit_def;

		public string battle_mod;

		public List<string> character_stats;

		public List<string> realm_stats;

		public List<string> kingdom_stats;

		public StackMode stack_mode;

		public DT.Field validate_field;

		public int battle_side = -1;

		public BonusDef(Def unit_def, DT.Field parent_field, string name)
		{
			this.name = name;
			this.unit_def = unit_def;
			battle_mod = name;
			unit_def.bonus_defs.Add(this);
			DT.Field field = parent_field?.FindChild(name);
			if (field == null)
			{
				return;
			}
			this.field = field;
			DT.Field based_on = unit_def.field;
			DT.Field field2 = field;
			bool flag = false;
			while (true)
			{
				if (field2 != null)
				{
					if (validate_field == null)
					{
						validate_field = field2.FindChild("validate");
					}
					if (battle_side == -1)
					{
						battle_side = field2.GetInt("battle_side", null, -1);
					}
					int num = field2.NumValues();
					for (int i = 0; i < num; i++)
					{
						string text = field2.String(i);
						if (!string.IsNullOrEmpty(text))
						{
							if (!text.StartsWith("cs_", StringComparison.Ordinal))
							{
								Game.Log(based_on.Path(include_file: true) + ": Invalid unit stat: '" + text + " - must start with 'cs_'", Game.LogType.Error);
								continue;
							}
							string stat_name = text;
							text = text.Substring(3);
							string stat_name2 = "rs_" + text;
							string stat_name3 = "ks_" + text;
							AddStat(ref character_stats, stat_name);
							AddStat(ref realm_stats, stat_name2);
							AddStat(ref kingdom_stats, stat_name3);
						}
					}
					if (!flag)
					{
						DT.Field field3 = field2.FindChild("stack");
						string text2 = field3?.String();
						if (!string.IsNullOrEmpty(text2))
						{
							switch (text2)
							{
							case "max":
								stack_mode = StackMode.Max;
								break;
							case "add":
								stack_mode = StackMode.Add;
								break;
							case "own":
								stack_mode = StackMode.Own;
								break;
							default:
								Game.Log(field3.Path(include_file: true) + ": Invalid stack mode '" + text2 + "', must be 'max', 'add' or 'own'", Game.LogType.Error);
								break;
							}
							flag = true;
						}
					}
				}
				based_on = based_on.based_on;
				if (based_on == null)
				{
					break;
				}
				field2 = based_on.FindChild("bonuses", null, allow_base: false)?.FindChild(name);
			}
			ValidateBattleMod();
			ValidateStats(ref character_stats, "CharacterStats", must_exist: true);
			ValidateStats(ref realm_stats, "RealmStats", must_exist: false);
			ValidateStats(ref kingdom_stats, "KingdomStats", must_exist: false);
		}

		public bool Validate(Battle battle, int battle_side = -1)
		{
			if (this.battle_side != -1 && battle_side != -1 && this.battle_side != battle_side)
			{
				return false;
			}
			if (validate_field == null)
			{
				return true;
			}
			return validate_field.Bool(battle);
		}

		private void AddStat(ref List<string> stats, string stat_name)
		{
			if (stats == null)
			{
				stats = new List<string>();
			}
			if (!stats.Contains(stat_name))
			{
				stats.Add(stat_name);
			}
		}

		private void ValidateBattleMod()
		{
			switch (battle_mod)
			{
			case "CTH_perc":
				return;
			case "defense":
				return;
			case "defense_flat":
				return;
			case "retreat_damage_mod":
				return;
			}
			battle_mod = null;
		}

		private void ValidateStats(ref List<string> stats, string stats_def_id, bool must_exist)
		{
			if (stats == null || this.field == null)
			{
				return;
			}
			DT.Def def = this.field.dt.FindDef(stats_def_id);
			if (def?.field == null && must_exist)
			{
				Game.Log(this.field.Path(include_file: true) + ": " + stats_def_id + " not found", Game.LogType.Error);
				return;
			}
			int num;
			for (num = 0; num < stats.Count; num++)
			{
				string text = stats[num];
				DT.Field field = def?.field?.FindChild(text);
				if (field != null && field.Type() == "stat")
				{
					return;
				}
				if (must_exist)
				{
					Game.Log(this.field.Path(include_file: true) + ": Invalid stat '" + text, Game.LogType.Error);
				}
				stats.RemoveAt(num);
				num--;
			}
			if (stats.Count == 0)
			{
				stats = null;
			}
		}

		public List<string> GetStatNames(Stats stats)
		{
			return stats.def.id switch
			{
				"CharcterStats" => character_stats, 
				"RealmStats" => realm_stats, 
				"KingdomStats" => kingdom_stats, 
				_ => null, 
			};
		}

		public float GetValue(Stats stats)
		{
			List<string> statNames = GetStatNames(stats);
			return GetValue(stats, statNames);
		}

		public float GetValue(Stats stats, List<string> stat_names)
		{
			if (stats == null || stat_names == null)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < stat_names.Count; i++)
			{
				string text = stat_names[i];
				float num2 = stats.Get(text);
				num += num2;
			}
			return num;
		}

		public float GetValue(Character c)
		{
			return GetValue(c?.stats, character_stats);
		}

		public float GetValue(Realm r)
		{
			if (r == null)
			{
				return 0f;
			}
			if (realm_stats != null)
			{
				return GetValue(r.stats, realm_stats);
			}
			if (kingdom_stats == null)
			{
				return 0f;
			}
			return GetValue(r.GetKingdom()?.stats, kingdom_stats);
		}

		public float GetValue(Kingdom k)
		{
			return GetValue(k?.stats, kingdom_stats);
		}

		public float GetValue(Battle battle, int side, Character leader, Garrison garrison, bool use_battle_bonuses)
		{
			float res = 0f;
			if (!Validate(battle, side))
			{
				return res;
			}
			if (battle == null || side < 0 || stack_mode == StackMode.Own)
			{
				res = ((leader != null) ? GetValue(leader) : ((battle == null || side != 1) ? GetValue(garrison?.settlement?.GetRealm()) : GetValue(battle.settlement?.GetRealm())));
				ApplyWarBonuses(ref res);
				ApplyBattleBonuses(ref res);
				return res;
			}
			if (side == 1)
			{
				AddValue(ref res, battle.settlement?.GetRealm()?.stats, realm_stats);
			}
			List<Army> armies = battle.GetArmies(side);
			if (armies != null)
			{
				for (int i = 0; i < armies.Count; i++)
				{
					Army army = armies[i];
					AddValue(ref res, army.leader?.stats, character_stats);
				}
			}
			ApplyWarBonuses(ref res);
			ApplyBattleBonuses(ref res);
			return res;
			void ApplyBattleBonuses(ref float v)
			{
				if (use_battle_bonuses && battle != null && side >= 0 && battle_mod != null)
				{
					battle.GetUnitMods(side, unit_def, battle_mod, out var _, out var perc, out var _);
					if (perc != 0f)
					{
						float value = unit_def.terrain_bonuses_perc.GetValue(battle, side, leader, garrison, use_battle_bonuses: false);
						perc = ((!(perc > 0f)) ? (perc * (1f - value * 0.01f)) : (perc * (1f + value * 0.01f)));
						v += perc;
					}
				}
			}
			void ApplyWarBonuses(ref float v)
			{
				Kingdom kingdom = null;
				if (leader != null)
				{
					if (leader.GetArmy() != null)
					{
						kingdom = leader.GetKingdom();
					}
				}
				else if (garrison != null)
				{
					kingdom = garrison.settlement?.GetKingdom();
				}
				if (kingdom?.wars != null)
				{
					for (int j = 0; j < kingdom.wars.Count; j++)
					{
						Kingdom enemyLeader = kingdom.wars[j].GetEnemyLeader(kingdom);
						if (enemyLeader == null)
						{
							break;
						}
						float bonus = War.GetBonus(kingdom, enemyLeader, name);
						v += bonus;
					}
				}
			}
		}

		public float GetValue(Unit unit, bool include_battle_bonuses)
		{
			if (unit == null)
			{
				return 0f;
			}
			Battle battle = unit.battle;
			if (battle != null && stack_mode != StackMode.Own)
			{
				int side = unit.battle_side;
				return GetValue(battle, side, unit.army?.leader, unit.garrison, include_battle_bonuses);
			}
			if (unit.army?.leader != null)
			{
				return GetValue(unit.army.leader.stats, character_stats);
			}
			Realm r = unit.garrison?.settlement?.GetRealm();
			return GetValue(r);
		}

		private void AddValue(ref float res, Stats stats, List<string> stat_names)
		{
			float value = GetValue(stats, stat_names);
			if (value != 0f)
			{
				if (stack_mode == StackMode.Add)
				{
					res += value;
				}
				else if (stack_mode != StackMode.Max)
				{
					Game.Log($"{this}: Unsupported stack mode: '{stack_mode}'", Game.LogType.Error);
				}
				else if (!(Math.Abs(value) <= Math.Abs(res)))
				{
					res = value;
				}
			}
		}

		public override string ToString()
		{
			return $"{stack_mode} {field?.Path() ?? name} = {field?.ValueStr()}";
		}
	}

	public class Def : Logic.Def
	{
		public PerLevelValues experience_to_next;

		public ItemType item_type;

		public Type type = Type.COUNT;

		public Type secondary_type = Type.COUNT;

		public bool is_heavy;

		public string name;

		public bool available;

		public bool hide_if_unavailable;

		public bool transfer_to_crusader = true;

		public List<string> counter;

		public List<string> countered;

		public int troops_def_idx = -1;

		public float rotation_speed = 90f;

		public float move_speed = 3f;

		public float run_speed_mul = 1.5f;

		public float charge_speed_mul = 1.5f;

		public float max_speed_mul = 1.5f;

		public float min_speed = 2f;

		public float max_acceleration = 3f;

		public float max_deceleration = 5f;

		public float walk_anim_speed = 2.3f;

		public float trot_anim_speed = -1f;

		public float run_anim_speed = 3.45f;

		public float sprint_anim_speed = -1f;

		public float charge_anim_speed = 3.45f;

		public Point[] terrain_normal_points;

		public float max_rotation_x = 360f;

		public float max_rotation_z = 360f;

		public float walk_to_trot_ratio = 0.5f;

		public float trot_to_run_ratio = 0.75f;

		public float run_to_sprint_ratio = 0.5f;

		public float turn_speed = 180f;

		public float max_health = -1f;

		public float default_troop_spacing = 3f;

		public float min_troop_spacing = 2f;

		public float max_troop_spacing = 4f;

		public List<BonusDef> bonus_defs = new List<BonusDef>();

		public BonusDef bonus_squad_size_perc;

		public BonusDef terrain_bonuses_perc;

		public BonusDef bonus_CTH_perc;

		public BonusDef bonus_CTH_flat;

		public BonusDef bonus_defense_perc;

		public BonusDef bonus_defense_flat;

		public BonusDef bonus_defense_during_siege_perc;

		public BonusDef bonus_sieging_troops_defense_perc;

		public BonusDef bonus_retreat_damage_mod;

		public BonusDef bonus_max_shoot_range_perc;

		public BonusDef bonus_salvo_capacity_perc;

		public BonusDef bonus_friendly_fire_reduction_perc;

		public BonusDef bonus_stamina_perc;

		public BonusDef bonus_morale_recovery_perc;

		public BonusDef bonus_morale_decay_perc;

		public BonusDef bonus_naval_CTH_perc;

		public BonusDef bonus_enemy_rebels_morale;

		public BonusDef bonus_troops_resilience;

		public BonusDef bonus_trample_shock_damage_perc;

		public BonusDef bonus_siege_strength_perc;

		public BonusDef bonus_shock_chance_flat;

		public BonusDef bonus_shock_chance_perc;

		public BonusDef bonus_discount_perc;

		public BonusDef bonus_health_perc;

		public BonusDef bonus_trample_chance_perc;

		public float base_morale_recovery;

		public float base_morale_decay;

		public float resilience;

		public int health_segments = 5;

		public float attack_interval = 3f;

		public float CTH = 10f;

		public float CTH_per_level = 20f;

		public float CTH_cavalry_mod = 1f;

		public float CTH_shoot_mod = 1f;

		public float CTH_siege_vs_siege;

		public float naval_CTH_perc;

		public float defeat_under_num_units_mul = 0.25f;

		public float[] state_mods = new float[11];

		public float defense = 30f;

		public float defense_per_level = 20f;

		public float defense_against_ranged_mod = 1f;

		public float defense_starvation_penalty = -50f;

		public float retreat_damage_mod;

		public float chance_to_shock = 1f;

		public float shock_damage = 1f;

		public int size = 50;

		public float bv_squad_size_multiplier = 1f;

		public int max_size = 300;

		public float levy_troop_bonus = 1f;

		public float manpower_mul = 10f;

		public bool can_attack_melee = true;

		public float attrition_mod;

		public bool ai_emergency_only;

		public string surrender_def_key = "surrender_unit";

		public string packed_def_key;

		public float stamina_max = 100f;

		public float stamina_per_level = 10f;

		public float stamina_req_run = 25f;

		public float stamina_req_charge = 25f;

		public float stamina_rate_idle = 4f;

		public float stamina_rate_move;

		public float stamina_rate_fight = 5f;

		public float stamina_rate_run = -10f;

		public float stamina_rate_charge = -10f;

		public float battle_row;

		public float battle_col;

		public float world_speed_mod = 1f;

		public float battle_scale = 1.5f;

		public float radius = 0.5f;

		public float selection_radius = 0.5f;

		public float attack_range = 3f;

		public float shoot_interval = 1f;

		public int salvo_capacity = 10;

		public float base_morale;

		public float trample_chance = 2f;

		public string salvo_def;

		public float climb_cooldown = 1.5f;

		public float death_decal_min_scale = 0.5f;

		public float death_decal_max_scale = 1f;

		public float death_decal_min_alpha = 0.5f;

		public float death_decal_max_alpha = 1f;

		public float death_decal_appear_time = 10f;

		public float ragdoll_sink_time = 15f;

		public float sink_depth = 1f;

		public float max_ragdoll_velocity = 4f;

		public float unpack_time = -1f;

		public float charge_time = 4f;

		public float charge_duration = 5f;

		public float charge_chance_to_shock_perc = 100f;

		public float max_random_shoot_offset;

		public float check_under_fire_cooldown = 60f;

		public float siege_strength = 1f;

		public float siege_damage;

		public float siege_strength_per_level = 1f;

		public float resilience_per_level = 1f;

		public float chance_target_closest_archer;

		public float chance_target_closest_cavalry;

		public float chance_ignore_marshal;

		public float chance_target_already_targetted_squad = 50f;

		public float chance_ignore_cavalry;

		public string formation = "Rect";

		public Squad.Stance default_stance;

		public Resource cost;

		public Resource upkeep;

		public Resource cost_merc;

		public Resource upkeep_merc;

		public PerLevelValues heal_gold_cost_per_level;

		public List<Resource> progressive_cost;

		public List<Resource> progressive_upkeep;

		public float food_upkeep_per_unit;

		public float add_supplies;

		public float add_squad_size_perc;

		public List<DT.Field> upgrade_to;

		public bool upgrades_to_available_units;

		public BuildPrerqusite buildPrerqusite;

		public int tier;

		public bool special;

		public Religion.Def[] kingdom_religion;

		public Religion.Def[] realm_religion;

		public string IdleSoundLoop;

		public string CheeringSoundLoop;

		public string MarchingSoundLoop;

		public string RunningSoundLoop;

		public string SprintingSoundLoop;

		public string ChargingSoundLoop;

		public string BattleSoundVoiceLoop;

		public string BattleSoundWeaponsLoop;

		public string BattleSoundHorsesLoop;

		public string VoiceSoundEffectPath;

		public string DyingSoundLoop;

		public string DyingSoundHorsesLoop;

		public string HitWoodGateSound;

		public string HitMetalGateSound;

		public string PackingSoundLoop;

		public string SelectSound;

		public string select_voice;

		public string melee_attack_range_voice_line;

		public string charge_voice_line;

		public string hold_fire_voice_line;

		public string allow_fire_voice_line;

		public string stand_ground_voice_line;

		public string at_ease_voice_line;

		public string face_voice_line;

		public string shoot_voice_line;

		public string line_voice_line;

		public string melee_attack_voice_line;

		public string under_fire_voice_line;

		public string losing_voice_line;

		public string winning_voice_line;

		public string refuse_order_voice_line;

		public string place_voice_line;

		public string reload_voice_line;

		public string retreat_voice_line;

		public string run_voice_line;

		public string square_voice_line;

		public string stop_voice_line;

		public string shrink_voice_line;

		public string triangle_voice_line;

		public string walk_voice_line;

		public string widen_voice_line;

		public string ram_battering_voice_line;

		public string ladders_voice_line;

		public string take_capture_point_voice_line;

		public string defend_capture_point_voice_line;

		public string units_died_voice_line;

		public string marked_as_target_voice_line;

		public string unmarked_as_target_voice_line;

		public string enemy_cavalry_attacks_us_voice_line;

		public string enemy_flees_voice_line;

		public string flanked_voice_line;

		public string gates_attacked_voice_line;

		private Game game;

		public bool is_cavalry
		{
			get
			{
				if (type != Type.Cavalry)
				{
					return secondary_type == Type.Cavalry;
				}
				return true;
			}
		}

		public bool is_ranged
		{
			get
			{
				if (type != Type.Ranged)
				{
					return secondary_type == Type.Ranged;
				}
				return true;
			}
		}

		public bool is_defense
		{
			get
			{
				if (type != Type.Defense)
				{
					return secondary_type == Type.Defense;
				}
				return true;
			}
		}

		public bool is_guard
		{
			get
			{
				if (type != Type.Guard)
				{
					return secondary_type == Type.Guard;
				}
				return true;
			}
		}

		public bool is_infantry
		{
			get
			{
				if (!is_cavalry)
				{
					return !is_ranged;
				}
				return false;
			}
		}

		public bool is_siege_eq
		{
			get
			{
				if (type != Type.InventoryItem)
				{
					return secondary_type == Type.InventoryItem;
				}
				return true;
			}
		}

		public float strength_eval => siege_strength + cost.Get(ResourceType.Gold) / 100f;

		public float scaled_radius => radius * battle_scale;

		public float dir_look_ahead => move_speed * 2f;

		public float morale_recovery_modified(Unit unit)
		{
			float num = base_morale_recovery;
			float value = bonus_morale_recovery_perc.GetValue(unit, include_battle_bonuses: false);
			float num2 = num * (1f + value / 100f);
			if (unit.battle != null)
			{
				num2 *= unit.battle.simulation.def.morale_recovery_mod;
			}
			return num2;
		}

		public float morale_decay_modified(Unit unit)
		{
			float num = base_morale_decay;
			float value = bonus_morale_decay_perc.GetValue(unit, include_battle_bonuses: false);
			float num2 = num * (1f + value / 100f);
			if (unit.battle != null)
			{
				num2 *= unit.battle.simulation.def.morale_decay_mod;
			}
			return num2;
		}

		public float max_health_modified(Battle battle, int rid, int battle_side, Garrison garrison, bool use_battle_bonuses, Character leader)
		{
			float num = max_health;
			float value = bonus_health_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			return num * (1f + value / 100f);
		}

		public float out_of_supplies_cavalry(bool out_of_supplies)
		{
			if (is_cavalry && out_of_supplies)
			{
				return -50f;
			}
			return 0f;
		}

		public float out_of_supplies_ranged(bool out_of_supplies)
		{
			if (is_ranged && out_of_supplies)
			{
				return -75f;
			}
			return 0f;
		}

		public float CTH_from_level(int level)
		{
			return (float)level * CTH_per_level;
		}

		public float CTH_from_naval_perc(Battle battle, int rid, int battle_side, Garrison garrison, bool use_battle_bonuses, Character leader)
		{
			return naval_CTH_perc + CTH_from_naval_bonus_perc(battle, rid, battle_side, garrison, use_battle_bonuses, leader);
		}

		public float CTH_from_naval_bonus_perc(Battle battle, int rid, int battle_side, Garrison garrison, bool use_battle_bonuses, Character leader)
		{
			return 0f + bonus_naval_CTH_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
		}

		public float CTH_from_initiative(Battle battle, int battle_side)
		{
			float num = 0f;
			if (battle?.initiative != null && battle.initiative_side == battle_side)
			{
				float num2 = battle.initiative.Get();
				if (num2 > 0f)
				{
					float max = battle.initiative.GetMax();
					float num3 = num2 / max;
					num += battle.simulation.def.bonus_cth_per_max_initiative * num3;
				}
				else
				{
					num += battle.simulation.def.penalty_cth_no_initiative;
				}
			}
			return num;
		}

		public float CTH_from_buffs(BattleSimulation.Squad squad)
		{
			float num = 0f;
			if (squad?.squad != null)
			{
				for (int i = 0; i < squad.squad.buffs.Count; i++)
				{
					SquadBuff squadBuff = squad.squad.buffs[i];
					num += squadBuff.GetCTH();
					if (is_cavalry)
					{
						num += squadBuff.getCTHCavalry();
					}
				}
			}
			return num;
		}

		public float CTH_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, BattleSimulation.Squad enemy, int rid, Character leader, Garrison garrison, BattleSimulation.Squad squad)
		{
			float num = CTH + bonus_CTH_flat.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			float value = bonus_CTH_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			value += CTH_from_level(level);
			if ((battle == null && rid < 0) || (battle != null && battle.type == Battle.Type.Naval))
			{
				value += CTH_from_naval_perc(battle, rid, battle_side, garrison, use_battle_bonuses, leader);
			}
			value += CTH_from_initiative(battle, battle_side);
			value += CTH_from_buffs(squad);
			float num2 = num * (1f + value * 0.01f);
			if (enemy != null && enemy.def.is_cavalry && (battle == null || battle.type != Battle.Type.Naval))
			{
				num2 *= CTH_cavalry_mod;
			}
			return num2;
		}

		public float CTH_shoot_mod_modified(BattleSimulation.Squad squad = null)
		{
			float cTH_shoot_mod = CTH_shoot_mod;
			float num = 0f;
			if (squad?.squad != null)
			{
				for (int i = 0; i < squad.squad.buffs.Count; i++)
				{
					SquadBuff squadBuff = squad.squad.buffs[i];
					num += squadBuff.GetCTHShootMod();
				}
			}
			return cTH_shoot_mod * (1f + num / 100f);
		}

		public float CTH_ranged_base()
		{
			return CTH * CTH_shoot_mod;
		}

		public float CTH_ranged_modified(Army army, Garrison garrison)
		{
			float num = CTH_modified(null, use_battle_bonuses: false, -1, 0, null, 0, army?.leader, garrison, null);
			float num2 = CTH_shoot_mod_modified();
			return num * num2;
		}

		public float defense_from_level(Battle battle, bool use_battle_bonuses, int battle_side, int level, BattleSimulation.Squad squad, Character leader, Garrison garrison, BattleSimulation.Squad enemy, Def enemy_def = null, bool out_of_supplies = false)
		{
			return (float)level * defense_per_level;
		}

		public float defense_from_castle_defender_bonus(Battle battle, int battle_side, BattleSimulation.Squad enemy = null, Def enemy_def = null)
		{
			float num = 0f;
			if (battle?.batte_view_game != null)
			{
				return num;
			}
			if (battle != null && battle.is_siege && battle.type != Battle.Type.BreakSiege && battle_side == 1)
			{
				float num2 = battle.castle_defender_bonus;
				if ((enemy != null && enemy.def.is_ranged) || (enemy_def != null && enemy_def.is_ranged))
				{
					num2 *= battle.simulation.def.castle_defenders_bonus_ranged;
				}
				num += num2;
			}
			return num;
		}

		public float defense_from_siege_resilience(Battle battle, int battle_side)
		{
			float num = 0f;
			if (battle != null && battle.is_siege && battle.type == Battle.Type.Siege && battle_side == 1)
			{
				num += battle.resilience * battle.def.resilience_defense_flat_mod;
			}
			return num;
		}

		public float defense_from_morale_flat(BattleSimulation.Squad squad)
		{
			float num = 0f;
			if (squad != null)
			{
				if (squad.def.is_siege_eq)
				{
					return num;
				}
				float morale = squad.GetMorale();
				if (BattleSimulation.MoraleEffectsActive)
				{
					num += squad.simulation.def.bonus_flat_defense_per_morale * morale;
				}
			}
			return num;
		}

		public float defense_from_morale_perc(BattleSimulation.Squad squad)
		{
			float num = 0f;
			if (squad != null)
			{
				if (squad.def.is_siege_eq)
				{
					return num;
				}
				float morale = squad.GetMorale();
				if (squad.squad != null)
				{
					for (int i = 0; i < squad.squad.buffs.Count; i++)
					{
						SquadBuff squadBuff = squad.squad.buffs[i];
						num += squadBuff.GetDefense();
					}
				}
				if (BattleSimulation.MoraleEffectsActive)
				{
					num += squad.simulation.def.BonusDefenseFromMorale(morale);
				}
			}
			return num;
		}

		public float defense_from_buffs(BattleSimulation.Squad squad)
		{
			float num = 0f;
			if (squad?.squad != null)
			{
				for (int i = 0; i < squad.squad.buffs.Count; i++)
				{
					SquadBuff squadBuff = squad.squad.buffs[i];
					num += squadBuff.GetDefense();
				}
			}
			return num;
		}

		public float defense_for_noble(BattleSimulation.Squad squad)
		{
			float num = 0f;
			if (squad != null)
			{
				num += squad.simulation.NobleDefense(squad);
			}
			return num;
		}

		public float defense_from_starvation(Battle battle, bool out_of_supplies, int battle_side)
		{
			float num = 0f;
			if (out_of_supplies && battle != null && battle_side == 0 && battle.type == Battle.Type.Siege)
			{
				num += defense_starvation_penalty;
			}
			return num;
		}

		public float defense_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, BattleSimulation.Squad squad, Character leader, Garrison garrison, BattleSimulation.Squad enemy, Def enemy_def = null, bool out_of_supplies = false)
		{
			float num = defense;
			float value = bonus_defense_flat.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			float num2 = 0f;
			float value2 = bonus_defense_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			value2 += defense_from_level(battle, use_battle_bonuses, battle_side, level, squad, leader, garrison, enemy, enemy_def, out_of_supplies);
			value2 += bonus_defense_during_siege_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			value2 += defense_from_castle_defender_bonus(battle, battle_side, enemy, enemy_def);
			value2 += bonus_sieging_troops_defense_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			num2 += defense_from_siege_resilience(battle, battle_side);
			value += defense_from_morale_flat(squad);
			value2 += defense_from_morale_perc(squad);
			value2 += defense_from_buffs(squad);
			value2 += defense_for_noble(squad);
			value2 += defense_from_starvation(battle, out_of_supplies, battle_side);
			return (num + value) * (1f + value2 * 0.01f) + num2;
		}

		public float defense_against_ranged_mod_modified(BattleSimulation.Squad squad)
		{
			float num = defense_against_ranged_mod;
			float num2 = 0f;
			if (squad?.squad != null)
			{
				for (int i = 0; i < squad.squad.buffs.Count; i++)
				{
					SquadBuff squadBuff = squad.squad.buffs[i];
					num2 += squadBuff.GetDefenseAgainstRanged();
				}
			}
			return num * (1f + num2 / 100f);
		}

		public float defense_against_ranged_base()
		{
			return defense * defense_against_ranged_mod;
		}

		public float defense_against_ranged_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, BattleSimulation.Squad squad, Character leader, Garrison garrison, BattleSimulation.Squad enemy, bool out_of_supplies)
		{
			return defense_modified(battle, use_battle_bonuses, battle_side, level, squad, leader, garrison, enemy, null, out_of_supplies) * defense_against_ranged_mod_modified(squad);
		}

		public float retreat_damage_mod_modified(Unit attack_target, Battle battle, bool use_battle_bonuses, int battle_side, int level, Character leader, Garrison garrison)
		{
			float num = retreat_damage_mod * move_speed / attack_target.def.move_speed;
			float value = bonus_retreat_damage_mod.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			Kingdom kingdom = ((attack_target.army != null) ? attack_target.army.GetKingdom() : ((attack_target.garrison != null) ? attack_target.garrison.settlement.GetKingdom() : null));
			num *= 1f + value * 0.01f;
			if (kingdom != null && kingdom.is_player)
			{
				DevSettings.Def devSettingsDef = kingdom.game.GetDevSettingsDef();
				num *= kingdom.game.GetPerDifficultyFloat(devSettingsDef.army_player_retreat_penalty, null);
			}
			return num;
		}

		public float shock_chance_from_charging(BattleSimulation.Squad squad, bool is_charge = false)
		{
			float num = 0f;
			if (is_charge || (squad?.squad != null && squad.squad.HasCharge()))
			{
				num += charge_chance_to_shock_perc;
			}
			return num;
		}

		public float shock_chance_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, BattleSimulation.Squad enemy, int rid, Character leader, Garrison garrison, BattleSimulation.Squad squad, bool is_charge = false)
		{
			float num = chance_to_shock + bonus_shock_chance_flat.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			float value = bonus_shock_chance_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			value += shock_chance_from_charging(squad, is_charge);
			return num * (1f + value * 0.01f);
		}

		public int min_troops_in_battle()
		{
			return (int)((float)size * defeat_under_num_units_mul);
		}

		public int GetMaxLevyTroops(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetMaxLevyTroops"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				Army army2 = unit?.army;
				if (army2 != null)
				{
					for_hire_army = true;
				}
				if (army2 == null)
				{
					army2 = army;
				}
				if (army2 == null || !for_hire_army)
				{
					return 0;
				}
				return (int)(float)army2.GetLevyBonus();
			}
		}

		public int GetRealmLevyGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetRealmLevyGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null)
				{
					num += (float)g.settlement.levy_manpower;
				}
				return (int)num;
			}
		}

		public int GetExcessLevyGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetExcessLevyGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null)
				{
					num += (float)g.settlement.excess_levy_manpower;
				}
				return (int)num;
			}
		}

		public int GetExcessTownGuardGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetExcessTownGuardGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null)
				{
					num += (float)g.settlement.excess_town_guard_manpower;
				}
				return (int)num;
			}
		}

		public int GetExcessWorkerGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetExcessWorkerGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null)
				{
					num += (float)g.settlement.excess_worker_manpower;
				}
				return (int)num;
			}
		}

		public int GetSiegeDefenseGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_garrison = false, bool for_hire_army = false)
		{
			using (Game.Profile("GetSiegeDefenseGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null && (for_hire_garrison || (unit != null && (unit?.simulation == null || !unit.simulation.temporary))))
				{
					num += (float)g.settlement.siege_defense_garrison_manpower;
				}
				return (int)num;
			}
		}

		public int GetRSGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_garrison = false, bool for_hire_army = false)
		{
			using (Game.Profile("GetSiegeDefenseGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null && (for_hire_garrison || (unit != null && (unit?.simulation == null || !unit.simulation.temporary))))
				{
					num += g.settlement.GetRealm().GetStat(Stats.rs_garrison_manpower_bonus);
				}
				return (int)num;
			}
		}

		public int GetSiegeDefenseTempGarrisonBonus(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetSiegeDefenseTempGarrisonBonus"))
			{
				if (is_siege_eq)
				{
					return 0;
				}
				if (army != null && for_hire_army)
				{
					return 0;
				}
				if (g == null)
				{
					g = unit?.garrison;
				}
				float num = 0f;
				if (g != null && (unit == null || (unit.simulation != null && unit.simulation.temporary)))
				{
					num += (float)g.settlement.siege_defense_temp_defender_manpower;
				}
				return (int)num;
			}
		}

		public float GetMarchBonusPerc(Unit unit)
		{
			Army army = unit?.army;
			if (army != null)
			{
				Kingdom kingdom = army.GetKingdom();
				if (kingdom != null)
				{
					float stat = kingdom.GetStat(Stats.ks_squad_size_per_keep_perc);
					if (stat > 0f)
					{
						return stat * (float)Math.Min(kingdom.GetNumberOfKeeps(), 10);
					}
				}
			}
			return 0f;
		}

		public float GetAdditionalTroopsPerc(Unit unit, Garrison g = null, Army army = null, bool for_hire_army = false)
		{
			using (Game.Profile("GetAdditionalTroopsPerc"))
			{
				if (is_siege_eq)
				{
					return 0f;
				}
				Army army2 = unit?.army;
				if (army2 != null)
				{
					for_hire_army = true;
				}
				if (army2 == null)
				{
					army2 = army;
				}
				if (army2 == null || !for_hire_army)
				{
					return 0f;
				}
				float num = 0f;
				for (int i = 0; i < army2.siege_equipment.Count; i++)
				{
					InventoryItem inventoryItem = army2.siege_equipment[i];
					if (inventoryItem?.def != null)
					{
						num += inventoryItem.def.add_squad_size_perc;
					}
				}
				return num;
			}
		}

		public int GetMaxTroopsFlat(Unit unit, bool add_size = true, bool add_levies = true, Army army = null)
		{
			int num = 0;
			if (add_size)
			{
				num += size;
			}
			return num;
		}

		public float GetMaxTroopsPercBonus(Unit unit, bool add_stats, bool add_additional, bool add_levies, bool add_levy_realm_defender, bool add_levy_excess_defender, bool add_town_guard_excess_defender, bool add_worker_excess_defender, bool add_siege_defense_defender, bool add_siege_defense_temporary_defender, bool add_rs_garrison_bonus, Garrison garrison = null, Army army = null, bool for_hire_garrison = false, bool for_hire_army = false)
		{
			float num = 0f;
			if (is_siege_eq)
			{
				return num;
			}
			unit?.InitCache();
			if (add_stats)
			{
				using (Game.Profile("GetBonusSquadSizePerc"))
				{
					num = ((unit?.bonus_squad_size_perc_cache == null) ? (num + bonus_squad_size_perc.GetValue(null, -1, army?.leader, garrison, use_battle_bonuses: false)) : (num + unit.bonus_squad_size_perc_cache.GetValue().Float()));
				}
			}
			if (add_additional)
			{
				num += GetAdditionalTroopsPerc(unit, garrison, army, for_hire_army);
			}
			if (add_levies)
			{
				num += (float)GetMaxLevyTroops(unit, garrison, army, for_hire_army);
			}
			if (add_levy_realm_defender)
			{
				num += (float)GetRealmLevyGarrisonBonus(unit, garrison, army, for_hire_army);
			}
			if (add_levy_excess_defender)
			{
				num += (float)GetExcessLevyGarrisonBonus(unit, garrison, army, for_hire_army);
			}
			if (add_town_guard_excess_defender)
			{
				num += (float)GetExcessTownGuardGarrisonBonus(unit, garrison, army, for_hire_army);
			}
			if (add_worker_excess_defender)
			{
				num += (float)GetExcessWorkerGarrisonBonus(unit, garrison, army, for_hire_army);
			}
			if (add_siege_defense_defender)
			{
				num += (float)GetSiegeDefenseGarrisonBonus(unit, garrison, army, for_hire_garrison, for_hire_army);
			}
			if (add_siege_defense_temporary_defender)
			{
				num += (float)GetSiegeDefenseTempGarrisonBonus(unit, garrison, army, for_hire_army);
			}
			if (add_rs_garrison_bonus)
			{
				num += (float)GetRSGarrisonBonus(unit, garrison, army, for_hire_garrison, for_hire_army);
			}
			return num;
		}

		public int GetMaxTroops(Unit unit, bool add_size = true, bool add_levies = true, bool add_stats = true, bool add_additional = true, bool add_levy_realm_defender = true, bool add_levy_excess_defender = true, bool add_town_guard_excess_defender = true, bool add_worker_excess_defender = true, bool add_siege_defense_defender = true, bool add_siege_defense_temporary_defender = true, bool add_rs_garrison_bonus = true, Garrison garrison = null, Army army = null, bool for_hire_garrison = false, bool for_hire_army = false)
		{
			int maxTroopsFlat = GetMaxTroopsFlat(unit, add_size, add_levies, army);
			float maxTroopsPercBonus = GetMaxTroopsPercBonus(unit, add_stats, add_additional, add_levies, add_levy_realm_defender, add_levy_excess_defender, add_town_guard_excess_defender, add_worker_excess_defender, add_siege_defense_defender, add_siege_defense_temporary_defender, add_rs_garrison_bonus, garrison, army, for_hire_garrison, for_hire_army);
			return (int)Math.Min(100.0, Math.Ceiling((float)maxTroopsFlat * (1f + maxTroopsPercBonus * 0.01f)));
		}

		public int GetMaxTroops(Realm r)
		{
			int num = size;
			float value = bonus_squad_size_perc.GetValue(r);
			if (value != 0f)
			{
				num = (int)((float)num * (1f + value * 0.01f));
			}
			if (num > max_size)
			{
				num = max_size;
			}
			return num;
		}

		public int GetMaxManPower(Unit unit, bool add_size = true, bool add_levies = true, bool add_stats = true, bool add_additional = true, bool add_levy_realm_defender = true, bool add_levy_excess_defender = true, bool add_town_guard_excess_defender = true, bool add_worker_excess_defender = true, bool add_siege_defense_defender = true, bool add_siege_defense_temporary_defender = true, bool add_rs_garrison_bonus = true, Garrison garrison = null, Army army = null, bool for_hire_garrison = false, bool for_hire_army = false)
		{
			return TroopsToManpower(GetMaxTroops(unit, add_size, add_levies, add_stats, add_additional, add_levy_realm_defender, add_levy_excess_defender, add_town_guard_excess_defender, add_worker_excess_defender, add_siege_defense_defender, add_siege_defense_temporary_defender, add_rs_garrison_bonus, garrison, army, for_hire_garrison, for_hire_army));
		}

		public int TroopsToManpower(int troops)
		{
			return (int)Math.Round(manpower_mul * (float)troops);
		}

		public int GetMaxManPower(Realm r)
		{
			int maxTroops = GetMaxTroops(r);
			return TroopsToManpower(maxTroops);
		}

		public int manpower_base_size(Unit unit = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false);
		}

		public int manpower_base_levies(Unit unit = null)
		{
			return GetMaxManPower(unit, add_size: false, add_levies: true, add_stats: false, add_additional: false);
		}

		public int manpower_perc_levies(Unit unit = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: true, add_stats: false, add_additional: false) - manpower_base_size();
		}

		public int manpower_bonus(Unit unit = null)
		{
			return GetMaxManPower(unit) - manpower_base_size();
		}

		public int manpower_realm_levies_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: true, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public int manpower_excess_levies_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: true, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public int manpower_excess_town_guard_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: false, add_town_guard_excess_defender: true, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public int manpower_excess_worker_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: true, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public int manpower_siege_defense_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: true, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public int manpower_siege_defense_temp_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: true, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public int manpower_rs_garrison_bonus(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: false, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: true, garrison) - manpower_base_size();
		}

		public int manpower_province_levies_for_garrison(Unit unit = null, Garrison garrison = null)
		{
			return GetMaxManPower(unit, add_size: true, add_levies: false, add_stats: false, add_additional: false, add_levy_realm_defender: true, add_levy_excess_defender: false, add_town_guard_excess_defender: false, add_worker_excess_defender: false, add_siege_defense_defender: false, add_siege_defense_temporary_defender: false, add_rs_garrison_bonus: false, garrison) - manpower_base_size();
		}

		public float stamina_from_level(Battle battle, bool use_battle_bonuses, int battle_side, int level, Character leader, Garrison garrison)
		{
			return (float)level * stamina_per_level;
		}

		public float stamina_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, Character leader, Garrison garrison)
		{
			float num = stamina_max;
			float value = bonus_stamina_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			value += stamina_from_level(battle, use_battle_bonuses, battle_side, level, leader, garrison);
			return num * (1f + value * 0.01f);
		}

		public float siege_strength_from_level(int level)
		{
			return (float)level * siege_strength_per_level;
		}

		public float siege_strength_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, Character leader, Garrison garrison)
		{
			float num = siege_strength + siege_strength_from_level(level);
			float value = bonus_siege_strength_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			return num * (1f + value * 0.01f);
		}

		public float trample_chance_modified(Battle battle, bool use_battle_bonuses, int battle_side, int level, Character leader, Garrison garrison)
		{
			float num = trample_chance;
			float value = bonus_trample_chance_perc.GetValue(battle, battle_side, leader, garrison, use_battle_bonuses);
			return num * (1f + value / 100f);
		}

		public float GetMaxCost(ResourceType type)
		{
			return Math.Max(cost?.Get(type) ?? 0f, cost_merc?.Get(type) ?? 0f);
		}

		public Resource CalcUpkeep(Army army, Garrison garrison, int idx, Game game = null)
		{
			Resource resource = upkeep;
			if (type == Type.InventoryItem && progressive_upkeep != null)
			{
				int num = army?.CountInventory(this, idx) ?? 0;
				if (num >= progressive_upkeep.Count)
				{
					num = progressive_upkeep.Count - 1;
				}
				resource = progressive_upkeep[num];
			}
			if (food_upkeep_per_unit != 0f && army != null)
			{
				int num2 = army.CountUnits();
				if (num2 > 0)
				{
					resource = new Resource(resource);
					resource.Add(ResourceType.Food, food_upkeep_per_unit * (float)num2);
				}
			}
			if (type != Type.InventoryItem && garrison != null && garrison?.settlement?.GetRealm()?.castle.governor == null)
			{
				resource = new Resource(resource);
				resource.Mul(0.1f);
			}
			if (game == null)
			{
				game = army?.game;
			}
			if (game == null)
			{
				game = garrison?.settlement?.game;
			}
			DevSettings.Def def = game?.GetDevSettingsDef();
			if (def == null)
			{
				return resource;
			}
			if (def.unit_food_upkeep_mod != 1f)
			{
				resource = new Resource(resource);
				float num3 = resource.Get(ResourceType.Food);
				num3 *= def.unit_food_upkeep_mod;
				resource.Set(ResourceType.Food, num3);
			}
			return resource;
		}

		public override bool Load(Game game)
		{
			this.game = game;
			name = dt_def.path;
			DT.Field field = dt_def.field;
			DT.Field field2 = field;
			DT.Field based_on = field.based_on;
			is_heavy = false;
			type = Type.COUNT;
			while (true)
			{
				if (field2 != null && field2.key.StartsWith("Heavy", StringComparison.Ordinal))
				{
					is_heavy = true;
				}
				if (field2.key == "Noble")
				{
					type = Type.Noble;
					break;
				}
				if (based_on == null)
				{
					type = Type.COUNT;
					break;
				}
				if (Enum.TryParse<Type>(field2.key, out type))
				{
					break;
				}
				field2 = based_on;
				based_on = field2.based_on;
			}
			if (!Enum.TryParse<Type>(field.GetString("secondary_type"), ignoreCase: true, out secondary_type))
			{
				secondary_type = Type.COUNT;
			}
			if (type == Type.InventoryItem)
			{
				field2 = field;
				based_on = field.based_on;
				string value;
				while (true)
				{
					if (based_on == null)
					{
						value = "Invalid";
						break;
					}
					if (based_on.key == "InventoryItem")
					{
						value = field2.key;
						break;
					}
					field2 = based_on;
					based_on = field2.based_on;
				}
				if (!Enum.TryParse<ItemType>(value, out item_type))
				{
					item_type = ItemType.COUNT;
				}
			}
			else
			{
				item_type = ItemType.COUNT;
			}
			if (type == Type.COUNT && name != "Unit" && item_type != ItemType.COUNT)
			{
				game.Error("Unknown unit type: " + name);
			}
			available = field.GetBool("available", null, available);
			hide_if_unavailable = field.GetBool("hide_if_unavailable", null, hide_if_unavailable);
			transfer_to_crusader = field.GetBool("transfer_to_crusader", null, transfer_to_crusader);
			DT.Field field3 = field.FindChild("counter");
			counter = new List<string>();
			for (int i = 0; i < field3.NumValues(); i++)
			{
				if (field3.Value(i, null, calc_expression: false).obj_val is string item)
				{
					counter.Add(item);
				}
			}
			DT.Field field4 = field.FindChild("countered");
			countered = new List<string>();
			for (int j = 0; j < field4.NumValues(); j++)
			{
				if (field4.Value(j, null, calc_expression: false).obj_val is string item2)
				{
					countered.Add(item2);
				}
			}
			bv_squad_size_multiplier = field.GetFloat("bv_squad_size_multiplier", null, bv_squad_size_multiplier);
			experience_to_next = PerLevelValues.Parse<float>(base.field.FindChild("experience_to_next"));
			rotation_speed = field.GetFloat("rotation_speed", null, rotation_speed);
			float num = field.GetFloat("speed_mul", null, 1f);
			move_speed = field.GetFloat("move_speed", null, move_speed) * num;
			run_speed_mul = field.GetFloat("run_speed_mul", null, run_speed_mul);
			charge_speed_mul = field.GetFloat("charge_speed_mul", null, charge_speed_mul);
			min_speed = field.GetFloat("min_speed", null, min_speed);
			max_speed_mul = field.GetFloat("max_speed_mul", null, max_speed_mul);
			max_acceleration = field.GetFloat("max_acceleration", null, max_acceleration);
			max_deceleration = field.GetFloat("max_deceleration", null, max_deceleration);
			walk_anim_speed = field.GetFloat("walk_anim_speed", null, walk_anim_speed);
			run_anim_speed = field.GetFloat("run_anim_speed", null, run_anim_speed);
			trot_anim_speed = field.GetFloat("trot_anim_speed", null, trot_anim_speed);
			sprint_anim_speed = field.GetFloat("sprint_anim_speed", null, sprint_anim_speed);
			charge_anim_speed = field.GetFloat("charge_anim_speed", null, charge_anim_speed);
			walk_to_trot_ratio = field.GetFloat("walk_to_trot_ratio", null, walk_to_trot_ratio);
			trot_to_run_ratio = field.GetFloat("trot_to_run_ratio", null, trot_to_run_ratio);
			run_to_sprint_ratio = field.GetFloat("run_to_sprint_ratio", null, run_to_sprint_ratio);
			max_random_shoot_offset = field.GetFloat("max_random_shoot_offset", null, max_random_shoot_offset);
			turn_speed = field.GetFloat("turn_speed", null, turn_speed);
			health_segments = field.GetInt("health_segments", null, health_segments);
			base_morale_recovery = field.GetFloat("base_morale_recovery", null, base_morale_recovery);
			base_morale_decay = field.GetFloat("base_morale_decay", null, base_morale_decay);
			climb_cooldown = field.GetFloat("climb_cooldown", null, climb_cooldown);
			death_decal_max_scale = field.GetFloat("death_decal_max_scale", null, death_decal_max_scale);
			death_decal_min_scale = field.GetFloat("death_decal_min_scale", null, death_decal_min_scale);
			death_decal_max_alpha = field.GetFloat("death_decal_max_alpha", null, death_decal_max_alpha);
			death_decal_min_alpha = field.GetFloat("death_decal_min_alpha", null, death_decal_min_alpha);
			death_decal_appear_time = field.GetFloat("death_decal_appear_time", null, death_decal_appear_time);
			ragdoll_sink_time = field.GetFloat("ragdoll_sink_time", null, ragdoll_sink_time);
			sink_depth = field.GetFloat("sink_depth", null, sink_depth);
			max_ragdoll_velocity = field.GetFloat("max_ragdoll_velocity", null, max_ragdoll_velocity);
			unpack_time = field.GetFloat("unpack_time", null, unpack_time);
			charge_time = field.GetFloat("charge_time", null, charge_time);
			charge_duration = field.GetFloat("charge_duration", null, charge_duration);
			charge_chance_to_shock_perc = field.GetFloat("charge_chance_to_shock_perc", null, charge_chance_to_shock_perc);
			default_troop_spacing = field.GetFloat("default_troop_spacing", null, default_troop_spacing);
			min_troop_spacing = field.GetFloat("min_troop_spacing", null, min_troop_spacing);
			max_troop_spacing = field.GetFloat("max_troop_spacing", null, max_troop_spacing);
			check_under_fire_cooldown = field.GetFloat("check_under_fire_cooldown", null, check_under_fire_cooldown);
			DT.Field field5 = field.FindChild("terrain_normal_points");
			if (field5 != null)
			{
				List<Point> list = new List<Point>();
				for (int k = 0; k < field5.NumValues(); k += 2)
				{
					float x = field5.Float(k);
					float y = field5.Float(k + 1);
					list.Add(new Point(x, y));
				}
				terrain_normal_points = list.ToArray();
			}
			max_rotation_x = field.GetFloat("max_rotation_x", null, max_rotation_x);
			max_rotation_z = field.GetFloat("max_rotation_z", null, max_rotation_z);
			chance_target_closest_archer = field.GetFloat("chance_target_closest_archer", null, chance_target_closest_archer);
			chance_target_closest_cavalry = field.GetFloat("chance_target_closest_cavalry", null, chance_target_closest_cavalry);
			chance_ignore_marshal = field.GetFloat("chance_ignore_marshal", null, chance_ignore_marshal);
			chance_target_already_targetted_squad = field.GetFloat("chance_target_already_targetted_squad", null, chance_target_already_targetted_squad);
			chance_ignore_cavalry = field.GetFloat("chance_ignore_cavalry", null, chance_ignore_cavalry);
			attack_interval = field.GetFloat("attack_interval", null, attack_interval);
			CTH = field.GetFloat("CTH", null, CTH);
			CTH_per_level = field.GetFloat("CTH_per_level", null, CTH_per_level);
			CTH_cavalry_mod = field.GetFloat("CTH_cavalry_mod", null, CTH_cavalry_mod);
			CTH_shoot_mod = field.GetFloat("CTH_shoot_mod", null, CTH_shoot_mod);
			CTH_siege_vs_siege = field.GetFloat("CTH_siege_vs_siege", null, CTH_siege_vs_siege);
			max_health = field.GetFloat("max_health", null, max_health);
			naval_CTH_perc = field.GetFloat("naval_CTH_perc", null, naval_CTH_perc);
			retreat_damage_mod = field.GetFloat("retreat_damage_mod");
			defense = field.GetFloat("defense", null, defense);
			defense_per_level = field.GetFloat("defense_per_level", null, defense_per_level);
			defense_against_ranged_mod = field.GetFloat("defense_against_ranged_mod", null, defense_against_ranged_mod);
			defense_starvation_penalty = field.GetFloat("defense_starvation_penalty", null, defense_starvation_penalty);
			chance_to_shock = field.GetFloat("chance_to_shock", null, chance_to_shock);
			shock_damage = field.GetFloat("shock_damage", null, shock_damage);
			attrition_mod = field.GetFloat("attrition_mod", null, attrition_mod);
			can_attack_melee = field.GetBool("can_attack_melee", null, can_attack_melee);
			defeat_under_num_units_mul = field.GetFloat("defeat_under_num_units_mul", null, defeat_under_num_units_mul);
			DT.Field field6 = field.FindChild("States");
			for (int l = 0; l < state_mods.Length; l++)
			{
				state_mods[l] = 1f;
				BattleSimulation.Squad.State state = (BattleSimulation.Squad.State)l;
				DT.Field field7 = field6?.FindChild(state.ToString());
				if (field7 != null)
				{
					state_mods[l] = field7.Float();
				}
			}
			stamina_max = field.GetFloat("stamina_max", null, stamina_max);
			stamina_per_level = field.GetFloat("stamina_per_level", null, stamina_per_level);
			stamina_req_run = field.GetFloat("stamina_req_run", null, stamina_req_run);
			stamina_req_charge = field.GetFloat("stamina_req_charge", null, stamina_req_charge);
			stamina_rate_idle = field.GetFloat("stamina_rate_idle", null, stamina_rate_idle);
			stamina_rate_move = field.GetFloat("stamina_rate_move", null, stamina_rate_move);
			stamina_rate_fight = field.GetFloat("stamina_rate_fight", null, stamina_rate_fight);
			stamina_rate_run = field.GetFloat("stamina_rate_run", null, stamina_rate_run);
			stamina_rate_charge = field.GetFloat("stamina_rate_charge", null, stamina_rate_charge);
			size = (int)Math.Round((float)field.GetInt("size", null, size) * field.GetFloat("size_mul", null, 1f));
			max_size = field.GetInt("max_size", null, max_size);
			manpower_mul = field.GetFloat("manpower_mul", null, manpower_mul);
			levy_troop_bonus = field.GetFloat("levy_troop_bonus", null, levy_troop_bonus);
			battle_row = field.GetFloat("battle_row", null, battle_row);
			battle_col = field.GetFloat("battle_col", null, battle_col);
			world_speed_mod = field.GetFloat("world_speed_mod", null, world_speed_mod);
			ai_emergency_only = field.GetBool("ai_emergency_only");
			DT.Field parent_field = field.FindChild("bonuses");
			bonus_defs.Clear();
			bonus_squad_size_perc = new BonusDef(this, parent_field, "squad_size_perc");
			terrain_bonuses_perc = new BonusDef(this, parent_field, "terrain_bonuses_perc");
			bonus_CTH_perc = new BonusDef(this, parent_field, "CTH_perc");
			bonus_CTH_flat = new BonusDef(this, parent_field, "CTH_flat");
			bonus_defense_perc = new BonusDef(this, parent_field, "defense");
			bonus_defense_flat = new BonusDef(this, parent_field, "defense_flat");
			bonus_defense_during_siege_perc = new BonusDef(this, parent_field, "defense_during_siege_perc");
			bonus_sieging_troops_defense_perc = new BonusDef(this, parent_field, "sieging_troops_defense_perc");
			bonus_retreat_damage_mod = new BonusDef(this, parent_field, "retreat_damage_mod");
			bonus_max_shoot_range_perc = new BonusDef(this, parent_field, "max_shoot_range");
			bonus_salvo_capacity_perc = new BonusDef(this, parent_field, "salvo_capacity");
			bonus_friendly_fire_reduction_perc = new BonusDef(this, parent_field, "friendly_fire_reduction");
			bonus_stamina_perc = new BonusDef(this, parent_field, "stamina_perc");
			bonus_morale_recovery_perc = new BonusDef(this, parent_field, "morale_recovery_perc");
			bonus_morale_decay_perc = new BonusDef(this, parent_field, "morale_decay_perc");
			bonus_naval_CTH_perc = new BonusDef(this, parent_field, "naval_CTH_perc");
			bonus_enemy_rebels_morale = new BonusDef(this, parent_field, "enemy_rebels_morale");
			bonus_troops_resilience = new BonusDef(this, parent_field, "troops_resilience");
			bonus_trample_shock_damage_perc = new BonusDef(this, parent_field, "trample_shock_damage_perc");
			bonus_siege_strength_perc = new BonusDef(this, parent_field, "siege_strength_perc");
			bonus_shock_chance_flat = new BonusDef(this, parent_field, "shock_chance_flat");
			bonus_shock_chance_perc = new BonusDef(this, parent_field, "shock_chance_perc");
			bonus_discount_perc = new BonusDef(this, parent_field, "discount_perc");
			bonus_health_perc = new BonusDef(this, parent_field, "health_perc");
			bonus_trample_chance_perc = new BonusDef(this, parent_field, "trample_chance_perc");
			IdleSoundLoop = field.GetString("idle_sound");
			CheeringSoundLoop = field.GetString("cheering_sound");
			MarchingSoundLoop = field.GetString("marching_sound");
			RunningSoundLoop = field.GetString("running_sound");
			SprintingSoundLoop = field.GetString("sprinting_sound");
			ChargingSoundLoop = field.GetString("charging_sound");
			BattleSoundVoiceLoop = field.GetString("battle_sound_voice");
			BattleSoundWeaponsLoop = field.GetString("battle_sound_weapons");
			BattleSoundHorsesLoop = field.GetString("battle_sound_horses");
			VoiceSoundEffectPath = field.GetString("voice_sound_effect");
			DyingSoundLoop = field.GetString("dying_sound", null, null);
			DyingSoundHorsesLoop = field.GetString("dying_sound_horses", null, null);
			HitWoodGateSound = field.GetString("hit_wood_gate_sound");
			HitMetalGateSound = field.GetString("hit_metal_gate_sound");
			PackingSoundLoop = field.GetString("packing_sound");
			SelectSound = field.GetString("select_sound");
			select_voice = field.GetString("select_voice_line", null, null);
			melee_attack_range_voice_line = field.GetString("melee_attack_range_voice_line", null, null);
			charge_voice_line = field.GetString("charge_voice_line", null, null);
			hold_fire_voice_line = field.GetString("hold_fire_voice_line", null, null);
			allow_fire_voice_line = field.GetString("allow_fire_voice_line", null, null);
			stand_ground_voice_line = field.GetString("stand_ground_voice_line", null, null);
			at_ease_voice_line = field.GetString("at_ease_voice_line", null, null);
			face_voice_line = field.GetString("face_voice_line", null, null);
			shoot_voice_line = field.GetString("shoot_voice_line", null, null);
			line_voice_line = field.GetString("line_voice_line", null, null);
			melee_attack_voice_line = field.GetString("melee_attack_voice_line", null, null);
			under_fire_voice_line = field.GetString("under_fire_voice_line", null, null);
			losing_voice_line = field.GetString("losing_voice_line", null, null);
			winning_voice_line = field.GetString("winning_voice_line", null, null);
			refuse_order_voice_line = field.GetString("refuse_order_voice_line", null, null);
			place_voice_line = field.GetString("place_voice_line", null, null);
			reload_voice_line = field.GetString("reload_voice_line", null, null);
			retreat_voice_line = field.GetString("retreat_voice_line", null, null);
			run_voice_line = field.GetString("run_voice_line", null, null);
			square_voice_line = field.GetString("square_voice_line", null, null);
			stop_voice_line = field.GetString("stop_voice_line", null, null);
			shrink_voice_line = field.GetString("shrink_voice_line", null, null);
			triangle_voice_line = field.GetString("triangle_voice_line", null, null);
			walk_voice_line = field.GetString("walk_voice_line", null, null);
			widen_voice_line = field.GetString("widen_voice_line", null, null);
			ram_battering_voice_line = field.GetString("ram_battering_voice_line", null, null);
			ladders_voice_line = field.GetString("ladders_voice_line", null, null);
			take_capture_point_voice_line = field.GetString("take_capture_point_voice_line", null, null);
			defend_capture_point_voice_line = field.GetString("defend_capture_point_voice_line", null, null);
			units_died_voice_line = field.GetString("units_died_voice_line", null, null);
			marked_as_target_voice_line = field.GetString("marked_as_target_voice_line", null, null);
			unmarked_as_target_voice_line = field.GetString("unmarked_as_target_voice_line", null, null);
			enemy_cavalry_attacks_us_voice_line = field.GetString("enemy_cavalry_attacks_us_voice_line", null, null);
			enemy_flees_voice_line = field.GetString("enemy_flees_voice_line", null, null);
			flanked_voice_line = field.GetString("flanked_voice_line", null, null);
			gates_attacked_voice_line = field.GetString("gates_attacked_voice_line", null, null);
			battle_scale = field.GetFloat("battle_scale", null, battle_scale);
			radius = field.GetFloat("radius", null, radius) * field.GetFloat("radius_mul", null, 1f);
			selection_radius = field.GetFloat("selection_radius", null, selection_radius);
			attack_range = field.GetFloat("attack_range", null, attack_range);
			formation = field.GetString("formation", null, formation);
			if (Enum.TryParse<Squad.Stance>(field.GetString("default_stance", null, "Defensive"), out var result))
			{
				default_stance = result;
			}
			shoot_interval = field.GetFloat("shoot_interval", null, shoot_interval);
			salvo_capacity = field.GetInt("salvo_capacity", null, salvo_capacity);
			salvo_def = field.GetString("salvo_def", null, salvo_def);
			base_morale = field.GetFloat("base_morale", null, base_morale);
			trample_chance = field.GetFloat("trample_chance", null, trample_chance);
			siege_strength = field.GetFloat("siege_strength", null, siege_strength);
			siege_damage = field.GetFloat("siege_damage", null, siege_damage);
			siege_strength_per_level = field.GetFloat("siege_strength_per_level", null, siege_strength_per_level);
			resilience_per_level = field.GetFloat("resilience_per_level", null, resilience_per_level);
			cost = LoadSplitResources("cost");
			upkeep = LoadSplitResources("upkeep");
			cost_merc = LoadSplitResources("cost_merc");
			upkeep_merc = LoadSplitResources("upkeep_merc");
			progressive_cost = LoadProgressiveCost(field.FindChild("progressive_cost"));
			progressive_upkeep = LoadProgressiveCost(field.FindChild("progressive_upkeep"));
			heal_gold_cost_per_level = PerLevelValues.Parse<float>(base.field.FindChild("heal_gold_cost_per_level"));
			food_upkeep_per_unit = field.GetFloat("food_upkeep_per_unit", null, food_upkeep_per_unit);
			add_supplies = field.GetFloat("add_supplies", null, add_supplies);
			add_squad_size_perc = field.GetFloat("add_squad_size_perc", null, add_squad_size_perc);
			buildPrerqusite = BuildPrerqusite.BuildUnitPrerqusite(this, game);
			resilience = field.GetFloat("resilience", null, resilience);
			tier = field.GetInt("tier", null, tier);
			special = field.GetBool("special", null, special);
			DT.Field field8 = field.FindChild("upgrade_to");
			upgrade_to = new List<DT.Field>();
			for (int m = 0; m < field8.NumValues(); m++)
			{
				if (field8.Value(m, null, calc_expression: false).obj_val is DT.Field item3)
				{
					upgrade_to.Add(item3);
				}
			}
			upgrades_to_available_units = field.GetBool("upgrades_to_available_units", null, upgrades_to_available_units);
			kingdom_religion = LoadReligionFilters(field.FindChild("kingdom_religion"), game);
			realm_religion = LoadReligionFilters(field.FindChild("realm_religion"), game);
			surrender_def_key = field.GetString("surrender_unit", null, surrender_def_key);
			packed_def_key = field.GetString("packed_def_key", null, packed_def_key);
			return true;
		}

		private Resource LoadSplitResources(string key)
		{
			bool flag = true;
			Resource resource = new Resource();
			for (int i = 0; i < 13; i++)
			{
				ResourceType resourceType = (ResourceType)i;
				DT.Field field = base.field.FindChild(key + "_" + resourceType);
				if (field != null)
				{
					resource.Add(Resource.Parse(field), 1f);
					flag = false;
				}
			}
			if (flag)
			{
				DT.Field field2 = base.field.FindChild(key);
				if (field2 != null)
				{
					return Resource.Parse(field2);
				}
			}
			return resource;
		}

		private List<Resource> LoadProgressiveCost(DT.Field f)
		{
			if (f == null)
			{
				return null;
			}
			List<DT.Field> list = f.Children();
			if (list == null)
			{
				return null;
			}
			List<Resource> list2 = null;
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field = list[i];
				if (string.IsNullOrEmpty(field.key))
				{
					continue;
				}
				Resource resource = Resource.Parse(field);
				if (!(resource == null))
				{
					if (list2 == null)
					{
						list2 = new List<Resource>();
					}
					list2.Add(resource);
				}
			}
			return list2;
		}

		private Religion.Def[] LoadReligionFilters(DT.Field f, Game game)
		{
			if (f == null)
			{
				return null;
			}
			int num = f.NumValues();
			if (num == 0)
			{
				return null;
			}
			Religion.Def[] array = new Religion.Def[num];
			if (num == 1)
			{
				DT.Field field = f.value.obj_val as DT.Field;
				array[0] = game.defs.Find<Religion.Def>(field?.key);
			}
			else
			{
				for (int i = 0; i < num; i++)
				{
					DT.Field field2 = f.Value(i, null, calc_expression: false).obj_val as DT.Field;
					array[i] = game.defs.Find<Religion.Def>(field2?.key);
				}
			}
			return array;
		}

		public bool ReligionEligable(Realm realm)
		{
			bool flag = true;
			bool flag2 = true;
			Religion.Def def = realm?.religion?.def;
			if (def != null && realm_religion != null && realm_religion.Length != 0)
			{
				flag = false;
				for (int i = 0; i < realm_religion.Length; i++)
				{
					flag |= realm_religion[i] == def;
				}
			}
			Religion.Def def2 = realm?.GetKingdom()?.religion?.def;
			if (def2 != null && kingdom_religion != null && kingdom_religion.Length != 0)
			{
				flag2 = false;
				for (int j = 0; j < kingdom_religion.Length; j++)
				{
					flag2 |= kingdom_religion[j] == def2;
				}
			}
			return flag && flag2;
		}

		public float base_manpower()
		{
			return (float)size * manpower_mul;
		}

		public BonusDef FindBonusDef(string name)
		{
			for (int i = 0; i < bonus_defs.Count; i++)
			{
				BonusDef bonusDef = bonus_defs[i];
				if (bonusDef.name == name)
				{
					return bonusDef;
				}
			}
			return null;
		}

		private float defense_modified(IVars vars)
		{
			Army army = Vars.Get<Army>(vars, "army");
			Garrison garrison = Vars.Get<Castle>(vars, "castle")?.garrison;
			return defense_modified(army, garrison);
		}

		private float defense_modified(Army army, Garrison garrison)
		{
			return defense_modified(null, use_battle_bonuses: false, -1, 0, null, army?.leader, garrison, null);
		}

		private float defense_against_ranged_modified(IVars vars)
		{
			Army army = Vars.Get<Army>(vars, "army");
			Garrison garrison = Vars.Get<Castle>(vars, "castle")?.garrison;
			return defense_against_ranged_modified(army, garrison);
		}

		private float defense_against_ranged_modified(Army army, Garrison garrison)
		{
			return defense_against_ranged_modified(null, use_battle_bonuses: false, -1, 0, null, army?.leader, garrison, null, out_of_supplies: false);
		}

		private float resilience_modified(IVars vars)
		{
			Army army = Vars.Get<Army>(vars, "army");
			Garrison garrison = Vars.Get<Castle>(vars, "castle")?.garrison;
			return resilience_modified(army, garrison);
		}

		private float resilience_modified(Army army, Garrison garrison)
		{
			float value = bonus_troops_resilience.GetValue(null, -1, army?.leader, garrison, use_battle_bonuses: false);
			return resilience + value;
		}

		private float max_manpower_modified(IVars vars)
		{
			Army army = Vars.Get<Army>(vars, "army");
			Garrison garrison = Vars.Get<Castle>(vars, "castle")?.garrison;
			bool flag = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
			bool for_hire_army = army != null && !flag;
			return max_manpower_modified(army, garrison, flag, for_hire_army);
		}

		private float max_manpower_modified(Army army, Garrison garrison, bool for_hire_garrison = false, bool for_hire_army = false)
		{
			return GetMaxManPower(null, add_size: true, add_levies: true, add_stats: true, add_additional: true, add_levy_realm_defender: true, add_levy_excess_defender: true, add_town_guard_excess_defender: true, add_worker_excess_defender: true, add_siege_defense_defender: true, add_siege_defense_temporary_defender: true, add_rs_garrison_bonus: true, garrison, army, for_hire_garrison, for_hire_army);
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "unit":
			case "unit_def":
				return this;
			case "name":
				return GetNameKey(vars);
			case "cost":
			{
				Castle castle = Vars.Get<Castle>(vars, "castle");
				if (castle != null)
				{
					Army army15 = Vars.Get<Army>(vars, "army");
					return castle.GetUnitCost(this, army15);
				}
				Mercenary mercenary = Vars.Get<Mercenary>(vars, "merc");
				Army army16 = Vars.Get<Army>(vars, "buyer");
				Unit unit4 = Vars.Get<Unit>(vars, "unit");
				if (mercenary != null && unit4 != null)
				{
					return mercenary.GetUnitCost(unit4, army16, Vars.Get<Kingdom>(vars, "hire_kingdom"), vars.GetVar("ignore_former_kingdom_discount").Bool());
				}
				return cost;
			}
			case "upkeep":
			{
				Army army3 = null;
				Garrison garrison2 = null;
				if (vars != null)
				{
					if (!(vars is Unit unit))
					{
						if (vars is Vars vars2)
						{
							Vars vars3 = vars2;
							if (vars3.obj.obj_val is Army army4)
							{
								army3 = army4;
							}
							else if (vars3.obj.obj_val is Garrison garrison3)
							{
								garrison2 = garrison3;
							}
							else if (vars3.obj.obj_val is Unit unit2)
							{
								army3 = unit2.army;
								garrison2 = unit2.garrison;
							}
						}
					}
					else
					{
						army3 = unit.army;
						garrison2 = unit.garrison;
					}
				}
				if (army3 == null && vars != null)
				{
					army3 = Vars.Get<Army>(vars, "army");
					if (army3 == null)
					{
						Unit unit3 = Vars.Get<Unit>(vars, "unit");
						if (unit3 != null)
						{
							army3 = unit3.army;
						}
					}
				}
				if (army3 != null && army3.IsHiredMercenary())
				{
					Resource upkeepMerc = army3.GetUpkeepMerc();
					if (upkeepMerc == null || upkeepMerc.IsZero())
					{
						return Value.Null;
					}
					return upkeepMerc;
				}
				Game game = army3?.game;
				if (game == null)
				{
					game = Vars.Get<Castle>(vars, "castle")?.game;
				}
				Resource resource = CalcUpkeep(army3, garrison2, -1, game);
				if (resource == null || resource.IsZero())
				{
					return Value.Null;
				}
				return resource;
			}
			case "available":
			{
				Castle castle3 = Vars.Get<Castle>(vars, "castle");
				if (castle3 == null)
				{
					return Value.Null;
				}
				return castle3.GetHireResources();
			}
			case "requirements":
			{
				if (buildPrerqusite == null)
				{
					return Value.Null;
				}
				Castle castle2 = Vars.Get<Castle>(vars, "castle");
				Army army21 = Vars.Get<Army>(vars, "army");
				return buildPrerqusite.GetLoclaized(this.game, castle2, army21?.leader);
			}
			case "max_shoot_range":
			{
				SalvoData.Def def2 = this.game?.defs?.Find<SalvoData.Def>(salvo_def);
				if (def2 == null)
				{
					return Value.Null;
				}
				return def2.max_shoot_range;
			}
			case "salvo_capacity":
				if (!is_ranged)
				{
					return Value.Null;
				}
				break;
			case "defense_visual":
				return (float)Math.Round(100f - 10000f / (100f + defense));
			case "is_ranged":
				return is_ranged;
			case "CTH_ranged":
				return CTH * CTH_shoot_mod;
			case "manpower":
				return base_manpower();
			case "is_siege":
				return is_siege_eq;
			case "is_noble":
				return type == Type.Noble || secondary_type == Type.Noble;
			case "defense_modified":
				return defense_modified(vars);
			case "defense_bonus":
				return defense_modified(vars) - defense;
			case "defense_against_ranged":
				return defense_against_ranged_base();
			case "defense_against_ranged_bonus":
			{
				Army army19 = Vars.Get<Army>(vars, "army");
				Garrison garrison10 = Vars.Get<Castle>(vars, "castle")?.garrison;
				return defense_against_ranged_modified(army19, garrison10) - defense;
			}
			case "defense_against_ranged_modified":
				return defense_against_ranged_modified(vars);
			case "naval_strength_perc":
				return CTH_from_naval_perc(null, 0, -1, null, use_battle_bonuses: true, null);
			case "naval_strength_bonus_perc":
				return CTH_from_naval_bonus_perc(null, 0, -1, null, use_battle_bonuses: true, null);
			case "resilience_bonus":
				return resilience_modified(vars) - resilience;
			case "max_manpower_bonus":
				return (float)Math.Round(max_manpower_modified(vars) - base_manpower());
			case "manpower_from_additional_troops":
			{
				Army army10 = Vars.Get<Army>(vars, "army");
				Garrison g4 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag12 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army4 = army10 != null && !flag12;
				return GetAdditionalTroopsPerc(null, g4, army10, for_hire_army4);
			}
			case "manpower_from_governor_levy_troops":
			{
				Army army17 = Vars.Get<Army>(vars, "army");
				Garrison g6 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag20 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army6 = army17 != null && !flag20;
				return GetMaxLevyTroops(null, g6, army17, for_hire_army6);
			}
			case "manpower_from_garrison_levy_bonus":
			{
				Army army9 = Vars.Get<Army>(vars, "army");
				Garrison g3 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag11 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army3 = army9 != null && !flag11;
				return GetRealmLevyGarrisonBonus(null, g3, army9, for_hire_army3);
			}
			case "manpower_from_garrison_excess_levy_bonus":
			{
				Army army6 = Vars.Get<Army>(vars, "army");
				Garrison g2 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag6 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army2 = army6 != null && !flag6;
				return GetExcessLevyGarrisonBonus(null, g2, army6, for_hire_army2);
			}
			case "manpower_from_garrison_excess_town_guard_bonus":
			{
				Army army18 = Vars.Get<Army>(vars, "army");
				Garrison g7 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag21 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army7 = army18 != null && !flag21;
				return GetExcessTownGuardGarrisonBonus(null, g7, army18, for_hire_army7);
			}
			case "manpower_from_garrison_excess_worker_bonus":
			{
				Army army14 = Vars.Get<Army>(vars, "army");
				Garrison g5 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag19 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army5 = army14 != null && !flag19;
				return GetExcessWorkerGarrisonBonus(null, g5, army14, for_hire_army5);
			}
			case "manpower_from_siege_defense_garrison":
			{
				Army army = Vars.Get<Army>(vars, "army");
				Garrison g = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army = army != null && !flag;
				return GetSiegeDefenseGarrisonBonus(null, g, army, flag, for_hire_army) + GetSiegeDefenseTempGarrisonBonus(null, g, army, for_hire_army);
			}
			case "manpower_from_garrison_realm":
			{
				Army army20 = Vars.Get<Army>(vars, "army");
				Garrison g8 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag22 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool for_hire_army8 = army20 != null && !flag22;
				return GetRSGarrisonBonus(null, g8, army20, flag22, for_hire_army8);
			}
			case "stamina_bonus":
			{
				Army army8 = Vars.Get<Army>(vars, "army");
				Garrison garrison6 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag9 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag10 = army8 != null && !flag9;
				return stamina_modified(null, use_battle_bonuses: false, -1, 0, (!flag10) ? null : army8?.leader, flag9 ? garrison6 : null) - stamina_max;
			}
			case "CTH_bonus":
			{
				Army army13 = Vars.Get<Army>(vars, "army");
				Garrison garrison9 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag17 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag18 = army13 != null && !flag17;
				return CTH_modified(null, use_battle_bonuses: false, -1, 0, null, 0, (!flag18) ? null : army13?.leader, flag17 ? garrison9 : null, null) - CTH;
			}
			case "CTH_ranged_bonus":
			{
				Army army7 = Vars.Get<Army>(vars, "army");
				Garrison garrison5 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag7 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag8 = army7 != null && !flag7;
				return CTH_ranged_modified(flag8 ? army7 : null, flag7 ? garrison5 : null) - CTH_ranged_base();
			}
			case "max_shoot_range_bonus":
			{
				SalvoData.Def def = this.game?.defs?.Find<SalvoData.Def>(salvo_def);
				if (def == null)
				{
					return 0;
				}
				Army army11 = Vars.Get<Army>(vars, "army");
				Garrison garrison7 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag13 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag14 = army11 != null && !flag13;
				return (float)Math.Round(CalcModifiedValue(def.max_shoot_range, bonus_max_shoot_range_perc, use_battle_bonuses: false, flag14 ? army11 : null, flag13 ? garrison7 : null) - def.max_shoot_range, 3);
			}
			case "salvo_capacity_bonus":
			{
				Army army2 = Vars.Get<Army>(vars, "army");
				Garrison garrison = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag2 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag3 = army2 != null && !flag2;
				return (float)Math.Round((float)(int)CalcModifiedValue(salvo_capacity, bonus_salvo_capacity_perc, use_battle_bonuses: false, flag3 ? army2 : null, flag2 ? garrison : null) - (float)salvo_capacity, 3);
			}
			case "chance_to_shock_bonus":
			{
				Army army12 = Vars.Get<Army>(vars, "army");
				Garrison garrison8 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag15 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag16 = army12 != null && !flag15;
				return shock_chance_modified(null, use_battle_bonuses: false, -1, 0, null, 0, (!flag16) ? null : army12?.leader, flag15 ? garrison8 : null, null) - chance_to_shock;
			}
			case "siege_strength_bonus":
			{
				Army army5 = Vars.Get<Army>(vars, "army");
				Garrison garrison4 = Vars.Get<Castle>(vars, "castle")?.garrison;
				bool flag4 = Vars.Get(vars, "for_hire_garrison").Bool() && !Vars.Get(vars, "is_bayer_a_army").Bool();
				bool flag5 = army5 != null && !flag4;
				return siege_strength_modified(null, use_battle_bonuses: false, -1, 0, (!flag5) ? null : army5?.leader, flag4 ? garrison4 : null) - siege_strength;
			}
			case "realm_religion_requirement":
			{
				List<Religion.Def> list2 = ReligionRequirement(realm_religion);
				if (list2 == null)
				{
					return Value.Unknown;
				}
				return new Value(list2);
			}
			case "kingdom_religion_requirement":
			{
				List<Religion.Def> list = ReligionRequirement(kingdom_religion);
				if (list == null)
				{
					return Value.Unknown;
				}
				return new Value(list);
			}
			}
			if (key.StartsWith("bonus_", StringComparison.Ordinal))
			{
				string text = key.Substring(6);
				BonusDef bonusDef = FindBonusDef(text);
				if (bonusDef != null)
				{
					return new Value(bonusDef);
				}
			}
			return base.GetVar(key, vars, as_value);
		}

		private List<Religion.Def> ReligionRequirement(Religion.Def[] religion)
		{
			if (religion == null || religion.Length == 0)
			{
				return null;
			}
			List<Religion.Def> list = new List<Religion.Def>();
			for (int i = 0; i < religion.Length; i++)
			{
				list.Add(religion[i]);
			}
			return list;
		}

		public float CalcModifiedValue(float base_value, BonusDef bonus_def, bool use_battle_bonuses = true, Army army = null, Garrison garrison = null)
		{
			if (bonus_def == null)
			{
				return base_value;
			}
			float value = bonus_def.GetValue(null, -1, army?.leader, garrison, use_battle_bonuses);
			return base_value * (1f + value * 0.01f);
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return base.id + ".name";
		}

		public void OnHiredAnalytics(Army army, Garrison garrison, Resource cost, string location)
		{
			Kingdom kingdom = null;
			Realm realm = null;
			int num = -1;
			if (army != null)
			{
				kingdom = army.GetKingdom();
				realm = army.realm_in;
				num = army.GetNid();
			}
			else if (garrison != null)
			{
				kingdom = garrison.settlement.GetKingdom();
				realm = garrison.settlement.GetRealm();
				num = garrison.settlement.GetNid();
			}
			if (kingdom != null && kingdom.is_player && realm != null && num != -1 && type != Type.COUNT)
			{
				Vars vars = new Vars();
				vars.Set("armyID", num.ToString());
				vars.Set("unitID", ToString());
				if (army != null && army.leader != null)
				{
					vars.Set("characterName", army.leader.Name);
				}
				vars.Set("unitName", name);
				vars.Set("unitType", type.ToString());
				vars.Set("unitTier", tier);
				vars.Set("province", realm.name);
				vars.Set("unitLocation", location);
				vars.Set("unitQuantity", size);
				vars.Set("unitPower", TroopsToManpower(size));
				if (cost != null)
				{
					vars.Set("goldCost", (int)cost[ResourceType.Gold]);
					vars.Set("foodCost", (int)cost[ResourceType.Food]);
					vars.Set("leviesCost", (int)cost[ResourceType.Levy]);
					vars.Set("populationCost", (int)cost[ResourceType.Workers]);
				}
				else
				{
					vars.Set("goldCost", 0);
				}
				kingdom.FireEvent("analytics_unit_hired", vars, kingdom.id);
			}
		}

		public static void RoundCost(Resource cost)
		{
			cost.Round();
			float num = cost[ResourceType.Gold];
			num = 5 * (int)Math.Round((double)num / 5.0);
			cost[ResourceType.Gold] = num;
		}
	}

	public Def def;

	public SalvoData.Def salvo_def;

	public Army army;

	public Garrison garrison;

	private ValueCache manpower_cache;

	private ValueCache bonus_squad_size_perc_cache;

	public float damage;

	public BattleSimulation.Squad simulation;

	public float speed_mod = 1f;

	public float experience;

	public int level;

	public const int base_level = 0;

	public bool mercenary;

	public int VO_actor_id = -100;

	public int battle_row = -1;

	public int battle_col = -1;

	public Game game
	{
		get
		{
			Game game = army?.game;
			if (game == null)
			{
				game = garrison?.settlement?.game;
			}
			return game;
		}
	}

	public Battle battle
	{
		get
		{
			object obj = army?.battle;
			if (obj == null)
			{
				Garrison obj2 = garrison;
				if (obj2 == null)
				{
					return null;
				}
				Settlement settlement = obj2.settlement;
				if (settlement == null)
				{
					return null;
				}
				obj = settlement.battle;
			}
			return (Battle)obj;
		}
	}

	public float health => 1f - damage;

	public int battle_side
	{
		get
		{
			if (army != null)
			{
				return army.battle_side;
			}
			if (garrison?.settlement.battle != null)
			{
				return 1;
			}
			return -1;
		}
	}

	public void InitCache()
	{
		Game game = this.game;
		if (game == null)
		{
			return;
		}
		if (manpower_cache == null)
		{
			manpower_cache = new ValueCache(() => def.GetMaxManPower(this), game);
		}
		if (bonus_squad_size_perc_cache == null)
		{
			bonus_squad_size_perc_cache = new ValueCache(() => def.bonus_squad_size_perc.GetValue(this, include_battle_bonuses: false) + def.GetMarchBonusPerc(this), game);
		}
	}

	public void SetArmy(Army army)
	{
		this.army = army;
	}

	public void SetGarrison(Garrison garrison)
	{
		this.garrison = garrison;
	}

	public override string ToString()
	{
		return string.Format("{0} {1} at {2},{3}, level: {4}", (def == null) ? "unknown" : def.name, mercenary ? "mercenary" : "", battle_row, battle_col, level);
	}

	public string GetNameKey(IVars vars = null, string form = "")
	{
		return def.id + ".name";
	}

	public void AddExperience(float val, bool allow_peasants = false)
	{
		if (level == def.experience_to_next.items.Count)
		{
			return;
		}
		experience = (float)Math.Round(experience + val, 2);
		float num = experience * (army?.leader?.GetStat(Stats.cs_troop_experience_gain_rate_perc) ?? 0f) / 100f;
		experience += num;
		level = 0;
		for (int i = 0; i < def.experience_to_next.items.Count; i++)
		{
			float num2 = def.experience_to_next.items[i].value;
			if (experience >= num2)
			{
				level++;
				OnLevelUpAnalytics(army, garrison);
				continue;
			}
			break;
		}
	}

	public int Index()
	{
		if (army != null && army.units != null)
		{
			return army.units.IndexOf(this);
		}
		if (garrison != null && garrison.units != null)
		{
			return garrison.units.IndexOf(this);
		}
		return -1;
	}

	public bool IsDefeated()
	{
		if (simulation != null && simulation.IsDefeated())
		{
			return true;
		}
		if (damage >= 1f)
		{
			return true;
		}
		return false;
	}

	public bool BelowMinTroops()
	{
		int num = NumTroops();
		int num2 = def.min_troops_in_battle();
		return num <= num2;
	}

	public int NumTroops()
	{
		int num = max_size_modified();
		return num - (int)((float)num * damage);
	}

	public void SetDamage(float damage, bool send_state = true)
	{
		this.damage = damage;
		if (this.damage > 1f)
		{
			this.damage = 1f;
		}
		if (this.damage < 0f)
		{
			this.damage = 0f;
		}
		if (!send_state)
		{
			return;
		}
		int num = Index();
		if (num < 0)
		{
			return;
		}
		if (army != null)
		{
			army.SendSubstate<Army.UnitsState.UnitState>(num);
		}
		if (simulation != null && simulation.garrison != null && simulation.garrison.settlement != null)
		{
			if (simulation.battle_side == 1)
			{
				simulation.garrison.settlement.SendSubstate<Settlement.GarrisonUnitsState.UnitState>(num);
			}
			else if (simulation.battle_side == 0)
			{
				simulation.garrison.settlement.SendSubstate<Settlement.AttackerGarrisonUnitsState.UnitState>(num);
			}
		}
	}

	public bool OutOfSupplies()
	{
		if (garrison != null && garrison.settlement is Castle castle)
		{
			return castle.GetFoodStorage() <= 0f;
		}
		if (army != null)
		{
			if (army.castle != null)
			{
				return army.castle.GetFoodStorage() <= 0f;
			}
			return army.IsStarving();
		}
		return false;
	}

	public float trample_chance_modified()
	{
		return def.trample_chance_modified(battle, use_battle_bonuses: true, battle_side, level, army?.leader, garrison);
	}

	public float CTH_modified(BattleSimulation.Squad enemy = null, bool use_battle_bonuses = true)
	{
		Battle battle = this.battle;
		int rid = ((army != null) ? ((army.realm_in != null) ? army.realm_in.id : 0) : 0);
		int num = ((simulation == null) ? (-1) : simulation.battle_side);
		return def.CTH_modified(battle, use_battle_bonuses, num, level, enemy, rid, army?.leader, garrison, simulation);
	}

	public float CTH_ranged_modified(BattleSimulation.Squad enemy = null, bool use_battle_bonuses = true)
	{
		return CTH_modified(enemy, use_battle_bonuses) * def.CTH_shoot_mod_modified();
	}

	public float defense_modified(bool use_battle_bonuses = true, BattleSimulation.Squad enemy = null)
	{
		return def.defense_modified(battle, use_battle_bonuses, battle_side, level, simulation, army?.leader, garrison, enemy, null, OutOfSupplies());
	}

	public float defense_against_ranged_modified(bool use_battle_bonuses = true, BattleSimulation.Squad enemy = null)
	{
		return def.defense_against_ranged_modified(battle, use_battle_bonuses, battle_side, level, simulation, army?.leader, garrison, enemy, OutOfSupplies());
	}

	public float chance_to_shock_Modified(bool is_charge = false)
	{
		int rid = ((army != null) ? ((army.realm_in != null) ? army.realm_in.id : 0) : 0);
		return def.shock_chance_modified(simulation?.simulation?.battle, use_battle_bonuses: true, battle_side, level, null, rid, army?.leader, garrison, simulation, is_charge);
	}

	public float chance_to_shock_base()
	{
		return def.chance_to_shock;
	}

	public int max_size_modified()
	{
		return def.GetMaxTroops(this);
	}

	public int max_size_modified_locked_in_battle()
	{
		if (simulation != null)
		{
			return simulation.max_troops_limited;
		}
		return def.GetMaxTroops(this);
	}

	public int max_manpower_modified()
	{
		InitCache();
		if (manpower_cache == null)
		{
			return 0;
		}
		return manpower_cache.GetValue();
	}

	public int max_manpower_modified_locked_in_battle()
	{
		if (simulation != null)
		{
			return (int)((float)simulation.max_troops_limited * def.manpower_mul);
		}
		return max_manpower_modified();
	}

	public int manpower_alive_modified()
	{
		return num_alive();
	}

	public int manpower_base_size()
	{
		return def.manpower_base_size(this);
	}

	public int manpower_base_levies()
	{
		return def.manpower_base_levies(this);
	}

	public int manpower_perc_levies()
	{
		return def.manpower_perc_levies(this);
	}

	public int manpower_bonus()
	{
		return def.manpower_bonus(this);
	}

	public int manpower_realm_levies_bonus()
	{
		return def.manpower_realm_levies_bonus(this);
	}

	public int manpower_excess_levies_bonus()
	{
		return def.manpower_excess_levies_bonus(this);
	}

	public int manpower_excess_town_guard_bonus()
	{
		return def.manpower_excess_town_guard_bonus(this);
	}

	public int manpower_excess_worker_bonus()
	{
		return def.manpower_excess_worker_bonus(this);
	}

	public int manpower_siege_defense_bonus()
	{
		return def.manpower_siege_defense_bonus(this);
	}

	public int manpower_rs_garrison_bonus()
	{
		return def.manpower_rs_garrison_bonus(this);
	}

	public int manpower_province_levies_for_garrison()
	{
		return def.manpower_province_levies_for_garrison(this);
	}

	public void ManpowerBreakdownByHealth(int total, out int healthy, out int dead)
	{
		ManpowerBreakdownByHealth(total, damage, out healthy, out dead);
	}

	public static void ManpowerBreakdownByHealth(int total, float damage, out int healthy, out int dead)
	{
		dead = (int)Math.Round((float)total * damage);
		healthy = total - dead;
	}

	public int num_healthy()
	{
		int total = max_manpower_modified_locked_in_battle();
		ManpowerBreakdownByHealth(total, out var healthy, out var _);
		return healthy;
	}

	public int num_dead()
	{
		int total = max_manpower_modified_locked_in_battle();
		ManpowerBreakdownByHealth(total, out var _, out var dead);
		return dead;
	}

	public int num_alive()
	{
		int total = max_manpower_modified_locked_in_battle();
		ManpowerBreakdownByHealth(total, out var healthy, out var _);
		return healthy;
	}

	public float stamina_modified()
	{
		return def.stamina_modified(battle, use_battle_bonuses: true, battle_side, level, army?.leader, garrison);
	}

	public float retreat_damage_mod_modified(Unit attack_target)
	{
		return def.retreat_damage_mod_modified(attack_target, battle, use_battle_bonuses: true, battle_side, level, army?.leader, garrison);
	}

	public float siege_strength_modified()
	{
		return def.siege_strength_modified(battle, use_battle_bonuses: true, battle_side, level, army?.leader, garrison);
	}

	public float siege_defenders_resilience_bonus()
	{
		if (simulation == null)
		{
			return 0f;
		}
		if (simulation.battle_side == 1 && simulation.simulation.battle.type == Battle.Type.Siege)
		{
			return simulation.simulation.battle.resilience * simulation.simulation.def.defenders_resilience_bonus_mod;
		}
		return 0f;
	}

	public float bonus_resilience_for_player()
	{
		Kingdom kingdom = ((army != null) ? army.GetKingdom() : ((garrison != null) ? garrison.settlement.GetKingdom() : null));
		if (kingdom == null)
		{
			return 0f;
		}
		if (!kingdom.is_player)
		{
			return 0f;
		}
		DevSettings.Def devSettingsDef = kingdom.game.GetDevSettingsDef();
		return kingdom.game.GetPerDifficultyFloat(devSettingsDef.unit_player_resilience_bonus, null);
	}

	public float resilience_from_initiative()
	{
		float num = 0f;
		if (battle != null && battle.initiative != null && battle.initiative_side == battle_side)
		{
			float num2 = battle.initiative.Get();
			if (num2 > 0f)
			{
				float max = battle.initiative.GetMax();
				float num3 = num2 / max;
				num += battle.simulation.def.bonus_resilience_per_max_initiative * num3;
			}
			else
			{
				num += battle.simulation.def.penalty_resilience_no_initiative;
			}
		}
		return num;
	}

	public float resilience_from_buffs(BattleSimulation.Squad squad)
	{
		float num = 0f;
		if (squad?.squad != null)
		{
			for (int i = 0; i < squad.squad.buffs.Count; i++)
			{
				SquadBuff squadBuff = squad.squad.buffs[i];
				num += squadBuff.GetResilienceFlat();
			}
		}
		return num;
	}

	public float bonus_troops_resilience()
	{
		return def.bonus_troops_resilience.GetValue(this, include_battle_bonuses: false);
	}

	public float resilience_base()
	{
		return def.resilience;
	}

	public float resilience_per_level()
	{
		return (float)level * def.resilience_per_level;
	}

	public float resilience_bonus(BattleSimulation.Squad squad)
	{
		return bonus_troops_resilience() + siege_defenders_resilience_bonus() + resilience_per_level() + bonus_resilience_for_player() + resilience_from_initiative() + resilience_from_buffs(squad);
	}

	public float resilience_total(BattleSimulation.Squad squad)
	{
		return resilience_base() + resilience_bonus(squad);
	}

	public float shock_damage_base()
	{
		return def.shock_damage;
	}

	public float shock_damage_bonus_trample()
	{
		return shock_damage_base() * (def.bonus_trample_shock_damage_perc.GetValue(this, include_battle_bonuses: false) / 100f);
	}

	public float max_health_modified()
	{
		int rid = ((army?.realm_in != null) ? army.realm_in.id : ((garrison?.settlement != null) ? garrison.settlement.realm_id : 0));
		return def.max_health_modified(battle, rid, battle_side, garrison, use_battle_bonuses: true, army?.leader);
	}

	public float CalcModifiedValue(float base_value, BonusDef bonus_def, bool use_battle_bonuses = true)
	{
		if (bonus_def == null)
		{
			return base_value;
		}
		float value = bonus_def.GetValue(battle, battle_side, army?.leader, garrison, use_battle_bonuses);
		return base_value * (1f + value * 0.01f);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "unit":
			return new Value(this);
		case "def":
		case "unit_def":
			return def;
		case "army":
			return army;
		case "garrison":
			return new Value(garrison);
		case "resilience_base":
			return resilience_base();
		case "resilience_bonus":
			return resilience_bonus(simulation);
		case "resilience_total":
			return resilience_total(simulation);
		case "bonus_squad_size_perc":
			if (as_value)
			{
				return def.bonus_squad_size_perc.GetValue(this, include_battle_bonuses: false);
			}
			return new Value(def.bonus_squad_size_perc);
		case "bonus_CTH_perc":
			if (as_value)
			{
				return def.bonus_CTH_perc.GetValue(this, include_battle_bonuses: true);
			}
			return new Value(def.bonus_CTH_perc);
		case "bonus_defense_perc":
			if (as_value)
			{
				return def.bonus_defense_perc.GetValue(this, include_battle_bonuses: true);
			}
			return new Value(def.bonus_defense_perc);
		case "bonus_defense_flat":
			if (as_value)
			{
				return def.bonus_defense_flat.GetValue(this, include_battle_bonuses: true);
			}
			return new Value(def.bonus_defense_flat);
		case "chance_to_shock_base":
			return chance_to_shock_base();
		case "chance_to_shock_bonus":
		{
			float num5 = chance_to_shock_Modified() - chance_to_shock_base();
			if (num5 == 0f)
			{
				return Value.Unknown;
			}
			return num5;
		}
		case "CTH":
			return def.CTH;
		case "CTH_modified":
			return CTH_modified(null, army?.battle?.batte_view_game == null);
		case "CTH_bonus":
		{
			float num6 = CTH_modified(null, army?.battle?.batte_view_game == null) - def.CTH;
			if (num6 == 0f)
			{
				return Value.Null;
			}
			return num6;
		}
		case "defense_against_ranged":
			return def.defense_against_ranged_base();
		case "defense_against_ranged_bonus":
			return defense_against_ranged_modified() - def.defense;
		case "defense_against_ranged_modified":
			return defense_against_ranged_modified();
		case "is_ranged":
			return def.is_ranged;
		case "CTH_ranged":
			return def.CTH * def.CTH_shoot_mod;
		case "CTH_ranged_modified":
			return CTH_ranged_modified(null, army?.battle?.batte_view_game == null);
		case "CTH_ranged_bonus":
		{
			float num = CTH_ranged_modified(null, army?.battle?.batte_view_game == null) - def.CTH_ranged_base();
			if (num == 0f)
			{
				return Value.Null;
			}
			return num;
		}
		case "defense_modified":
			return defense_modified(army?.battle?.batte_view_game == null);
		case "defense_bonus":
		{
			float num4 = defense_modified(army?.battle?.batte_view_game == null) - def.defense;
			if (num4 == 0f)
			{
				return Value.Null;
			}
			return num4;
		}
		case "stamina_bonus":
		{
			float num3 = stamina_modified() - def.stamina_max;
			if (num3 == 0f)
			{
				return Value.Null;
			}
			return num3;
		}
		case "stamina_max":
			return stamina_modified();
		case "siege_strength_bonus":
		{
			float num2 = siege_strength_modified() - def.siege_strength;
			if (num2 == 0f)
			{
				return Value.Null;
			}
			return num2;
		}
		case "defense_bonus_visual":
		{
			float num13 = (float)Math.Round(100f - 10000f / (100f + defense_modified(army?.battle?.batte_view_game == null)) - (float)def.GetVar("defense_visual", vars));
			if (num13 == 0f)
			{
				return Value.Unknown;
			}
			return num13;
		}
		case "health":
			return health;
		case "num_troops":
		{
			float num11 = health;
			float num12 = max_size_modified_locked_in_battle();
			return (int)Math.Ceiling(num11 * num12);
		}
		case "max_size":
			return max_size_modified_locked_in_battle();
		case "size_bonus":
		{
			float num10 = (float)Math.Round((float)max_size_modified_locked_in_battle() - (float)def.size, 3);
			if (num10 == 0f)
			{
				return Value.Unknown;
			}
			return num10;
		}
		case "out_of_supplies_cavalry":
			return def.out_of_supplies_cavalry(OutOfSupplies());
		case "out_of_supplies_ranged":
			return def.out_of_supplies_ranged(OutOfSupplies());
		case "max_manpower_base":
			return def.base_manpower();
		case "max_manpower":
			return max_manpower_modified_locked_in_battle();
		case "manpower":
			return manpower_alive_modified();
		case "max_manpower_bonus":
		{
			float num9 = (float)Math.Round((float)max_manpower_modified_locked_in_battle() - def.base_manpower());
			if (num9 == 0f)
			{
				return Value.Unknown;
			}
			return num9;
		}
		case "manpower_from_additional_troops":
			return def.GetAdditionalTroopsPerc(this, garrison);
		case "manpower_from_governor_levy_troops":
			return def.GetMaxLevyTroops(this, garrison);
		case "manpower_from_garrison_levy_bonus":
			return def.GetRealmLevyGarrisonBonus(this, garrison);
		case "manpower_from_garrison_excess_levy_bonus":
			return def.GetExcessLevyGarrisonBonus(this, garrison);
		case "manpower_from_garrison_excess_town_guard_bonus":
			return def.GetExcessTownGuardGarrisonBonus(this, garrison);
		case "manpower_from_garrison_excess_worker_bonus":
			return def.GetExcessWorkerGarrisonBonus(this, garrison);
		case "manpower_from_siege_defense_garrison":
			return def.GetSiegeDefenseGarrisonBonus(this, garrison) + def.GetSiegeDefenseTempGarrisonBonus(this, garrison);
		case "manpower_from_garrison_realm":
			return def.GetRSGarrisonBonus(this, garrison);
		case "manpower_from_march":
			return def.GetMarchBonusPerc(this);
		case "cth_from_level":
			return def.CTH_from_level(level);
		case "cth_from_initiative":
			return def.CTH_from_initiative(battle, battle_side);
		case "cth_from_buffs":
			return def.CTH_from_buffs(simulation);
		case "naval_strength_perc":
		{
			int rid2 = ((army != null) ? ((army.realm_in != null) ? army.realm_in.id : 0) : 0);
			return def.CTH_from_naval_perc(battle, rid2, battle_side, garrison, use_battle_bonuses: true, army?.leader);
		}
		case "naval_strength_bonus_perc":
		{
			int rid = ((army != null) ? ((army.realm_in != null) ? army.realm_in.id : 0) : 0);
			return def.CTH_from_naval_bonus_perc(battle, rid, battle_side, garrison, use_battle_bonuses: true, army?.leader);
		}
		case "stamina_from_level":
			return def.stamina_from_level(battle, use_battle_bonuses: true, battle_side, level, army?.leader, garrison);
		case "defense_from_level":
			return def.defense_from_level(battle, use_battle_bonuses: true, battle_side, level, simulation, army?.leader, garrison, null, null, OutOfSupplies());
		case "defense_from_castle_defender_bonus":
			return def.defense_from_castle_defender_bonus(battle, battle_side);
		case "defense_from_siege_resilience":
			return def.defense_from_siege_resilience(battle, battle_side);
		case "defense_from_morale_flat":
			return def.defense_from_morale_flat(simulation);
		case "defense_from_morale_perc":
			return def.defense_from_morale_perc(simulation);
		case "defense_from_buffs":
			return def.defense_from_buffs(simulation);
		case "defense_for_noble":
			return def.defense_for_noble(simulation);
		case "defense_from_starvation":
			return def.defense_from_starvation(battle, OutOfSupplies(), battle_side);
		case "shock_chance_from_charging":
			return def.shock_chance_from_charging(simulation);
		case "siege_defenders_resilience_bonus":
			return siege_defenders_resilience_bonus();
		case "resilience_from_level":
			return resilience_per_level();
		case "bonus_resilience_for_player":
			return bonus_resilience_for_player();
		case "resilience_from_initiative":
			return resilience_from_initiative();
		case "resilience_from_buffs":
			return resilience_from_buffs(simulation);
		case "siege_strength_from_level":
			return def.siege_strength_from_level(level);
		case "trample_chance_bonus":
			return trample_chance_modified() - def.trample_chance;
		case "num_dead":
			return num_dead();
		case "num_healty":
			return num_healthy();
		case "move_speed_bonus":
			if (simulation == null)
			{
				return 0;
			}
			return simulation.move_speed_bonus();
		case "state":
			if (simulation == null)
			{
				return Value.Null;
			}
			return "SquadState." + simulation.state;
		case "target":
			if (simulation == null || simulation.state == BattleSimulation.Squad.State.Idle)
			{
				return Value.Null;
			}
			return new Value(simulation.target);
		case "command":
			if (simulation?.squad == null)
			{
				return Value.Null;
			}
			return simulation.squad.command.ToString();
		case "salvos":
			if (simulation == null || !def.is_ranged)
			{
				return Value.Null;
			}
			if (simulation.squad == null)
			{
				return simulation.remaining_salvos;
			}
			return simulation.squad.salvos_left;
		case "salvo_capacity_bonus":
		{
			float num8 = (float)Math.Round((float)GetVar("salvo_capacity", vars) - (float)def.salvo_capacity, 3);
			if (num8 == 0f)
			{
				return Value.Unknown;
			}
			return num8;
		}
		case "max_shoot_range_base":
			return salvo_def.max_shoot_range;
		case "max_shoot_range_bonus":
		{
			float num7 = (float)Math.Round((float)GetVar("max_shoot_range", vars) - salvo_def.max_shoot_range, 3);
			if (num7 == 0f)
			{
				return Value.Unknown;
			}
			return num7;
		}
		case "salvo_capacity":
			return SalvoCapacityModified();
		case "max_shoot_range":
			return CalcModifiedValue(salvo_def.max_shoot_range, def.bonus_max_shoot_range_perc);
		case "move_speed":
			return def.move_speed;
		case "friendly_fire_reduction":
			return def.bonus_friendly_fire_reduction_perc.GetValue(this, include_battle_bonuses: true);
		case "cost":
		{
			Vars vars2 = new Vars(this);
			vars2.Set("ignore_former_kingdom_discount", vars.GetVar("ignore_former_kingdom_discount"));
			vars2.Set("merc", army?.mercenary);
			vars2.Set("buyer", army?.mercenary?.selected_buyer);
			Value var = vars.GetVar("kingdom");
			if (!var.is_unknown)
			{
				vars2.Set("kingdom", var.Get<Kingdom>());
			}
			Value value = vars.GetVar("hire_kingdom");
			if (value.is_unknown)
			{
				value = army?.game?.GetLocalPlayerKingdom();
			}
			if (!value.is_null)
			{
				vars2.Set("hire_kingdom", value.Get<Kingdom>());
			}
			vars2.Set("unit", this);
			return def.GetVar("cost", vars2);
		}
		case "heal_cost":
			return GetHealCost();
		case "upkeep_raw":
			return GetUpkeep(ignore_location: true);
		case "upkeep":
			return GetUpkeep();
		case "morale":
			if (simulation == null)
			{
				return Value.Unknown;
			}
			return simulation.GetMorale();
		case "temporary_morale":
			if (simulation == null)
			{
				return Value.Unknown;
			}
			return simulation.temporary_morale;
		case "initial_morale":
			if (simulation == null)
			{
				return Value.Unknown;
			}
			return simulation.initial_morale;
		case "available":
		{
			Castle castle = army?.castle ?? (garrison?.settlement as Castle);
			if (castle == null)
			{
				return Value.Null;
			}
			Resource resource = new Resource(castle.GetKingdom()?.resources);
			resource.Set(ResourceType.Workers, castle.population.GetWorkers());
			resource.Set(ResourceType.Food, castle.food_storage);
			return resource;
		}
		default:
			return Value.Unknown;
		}
	}

	public int SalvoCapacityModified()
	{
		return (int)CalcModifiedValue(def.salvo_capacity, def.bonus_salvo_capacity_perc);
	}

	public Resource GetUpkeep(bool ignore_location = false)
	{
		if (army != null && army.IsHiredMercenary())
		{
			return GetUpkeepMerc();
		}
		Resource resource = (ignore_location ? def.CalcUpkeep(null, null, -1, game) : def.CalcUpkeep(army, garrison, -1));
		if (resource == null)
		{
			return null;
		}
		return resource;
	}

	public Resource GetUpkeepMerc(Kingdom k = null)
	{
		Resource upkeep_merc = def.upkeep_merc;
		if (upkeep_merc == null)
		{
			return null;
		}
		Resource resource = new Resource(upkeep_merc);
		float num = 0f;
		if (k != null && k.IsRegular())
		{
			num -= k.GetStat(Stats.ks_mercenary_price_reduction_perc);
		}
		else if (army?.GetKingdom() != null && army.GetKingdom().IsRegular())
		{
			num -= army.GetKingdom().GetStat(Stats.ks_mercenary_price_reduction_perc);
		}
		if (army?.leader != null)
		{
			num -= army.leader.GetStat(Stats.cs_mercenary_price_reduction_perc);
		}
		if (num == 0f)
		{
			return resource;
		}
		army?.mercenary.Discount(resource, ResourceType.Gold, num);
		return resource;
	}

	public Resource GetHealCost(bool rounded = true)
	{
		Resource resource = new Resource();
		if (def.type == Type.Noble)
		{
			return resource;
		}
		if (mercenary)
		{
			resource.Add(def.cost_merc, Game.clamp(damage, 0f, 1f));
		}
		else
		{
			resource.Add(def.cost, Game.clamp(damage, 0f, 1f));
		}
		if (army?.leader != null)
		{
			float stat = army.leader.GetStat(Stats.cs_army_refill_cost_perc);
			resource.Mul(1f + stat / 100f);
		}
		if (def.heal_gold_cost_per_level != null)
		{
			float num = def.heal_gold_cost_per_level.GetFloat(level + 1, per_level: false, flat: true, clamp_min: true);
			resource[ResourceType.Gold] *= 1f + num / 100f;
		}
		if (rounded)
		{
			for (int i = 0; i < 13; i++)
			{
				resource[(ResourceType)i] = (float)Math.Ceiling(resource[(ResourceType)i]);
			}
		}
		if (resource.IsZero())
		{
			return null;
		}
		return resource;
	}

	public float EvalStrength(Army override_army = null)
	{
		Army army = this.army;
		Garrison garrison = this.garrison;
		if (override_army != null)
		{
			this.army = override_army;
			this.garrison = null;
		}
		float num = (float)max_size_modified() / (float)def.size;
		float result = def.strength_eval * health * num;
		if (override_army != null)
		{
			this.army = army;
			this.garrison = garrison;
		}
		return result;
	}

	public bool IsValid()
	{
		if (army != null)
		{
			return army.IsValid();
		}
		if (garrison != null)
		{
			return garrison.settlement.IsValid();
		}
		return false;
	}

	public void OnAssignedAnalytics(Army army, Garrison garrison, string location, string assignAction)
	{
		Kingdom kingdom = null;
		Realm realm = null;
		int num = -1;
		if (army != null)
		{
			kingdom = army.GetKingdom();
			realm = army.realm_in;
			num = army.GetNid();
		}
		else if (garrison != null)
		{
			kingdom = garrison.settlement.GetKingdom();
			realm = garrison.settlement.GetRealm();
			num = garrison.settlement.GetNid();
		}
		if (kingdom != null && kingdom.is_player && realm != null && num != -1 && kingdom.game.IsRunning() && def.type != Type.COUNT)
		{
			Vars vars = new Vars();
			vars.Set("armyID", num.ToString());
			vars.Set("unitID", ToString());
			if (army != null && army.leader != null)
			{
				vars.Set("characterName", army.leader.Name);
			}
			vars.Set("unitName", def.name);
			vars.Set("unitType", def.type.ToString());
			vars.Set("unitTier", def.tier);
			vars.Set("province", realm.name);
			vars.Set("unitLocation", location);
			int maxTroops = def.GetMaxTroops(this);
			vars.Set("unitQuantity", maxTroops);
			vars.Set("unitPower", def.TroopsToManpower(maxTroops));
			vars.Set("unitHealth", health);
			vars.Set("unitLevel", level);
			vars.Set("unitXP", (int)experience);
			vars.Set("assignAction", assignAction);
			kingdom.FireEvent("analytics_unit_assigned", vars, kingdom.id);
		}
	}

	public void OnLevelUpAnalytics(Army army, Garrison garrison)
	{
		Kingdom kingdom = null;
		Realm realm = null;
		int num = -1;
		string val = null;
		if (army != null)
		{
			kingdom = army.GetKingdom();
			realm = army.realm_in;
			num = army.GetNid();
			val = "army";
		}
		else if (garrison != null)
		{
			kingdom = garrison.settlement.GetKingdom();
			realm = garrison.settlement.GetRealm();
			num = garrison.settlement.GetNid();
			val = "garrison";
		}
		if (kingdom != null && kingdom.is_local_player && realm != null && num != -1 && def.type != Type.COUNT)
		{
			Vars vars = new Vars();
			vars.Set("armyID", num.ToString());
			vars.Set("unitID", ToString());
			if (army != null && army.leader != null)
			{
				vars.Set("characterName", army.leader.Name);
			}
			else
			{
				vars.Set("characterName", "no-leader");
			}
			vars.Set("unitName", def.name);
			vars.Set("unitType", def.type.ToString());
			vars.Set("unitTier", def.tier);
			vars.Set("province", realm.name);
			vars.Set("unitLocation", val);
			int maxTroops = def.GetMaxTroops(this);
			vars.Set("unitQuantity", maxTroops);
			vars.Set("unitPower", def.TroopsToManpower(maxTroops));
			vars.Set("unitHealth", health);
			vars.Set("unitLevel", level);
			vars.Set("unitXP", (int)experience);
			kingdom.FireEvent("analytics_unit_level_up", vars, kingdom.id);
		}
	}
}
