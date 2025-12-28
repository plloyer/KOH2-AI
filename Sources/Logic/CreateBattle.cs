using System;
using System.Collections.Generic;
using Logic.ExtensionMethods;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Battle)]
public class Battle : MapObject
{
	public enum Type
	{
		OpenField,
		Plunder,
		Siege,
		Assault,
		BreakSiege,
		Naval,
		PlunderInterrupt
	}

	public enum Stage
	{
		Preparing,
		EnteringBattle,
		Ongoing,
		Finishing,
		Finished
	}

	public enum AftermathOutcome
	{
		Won,
		Defeated,
		Cancelled
	}

	public enum VictoryReason
	{
		Combat,
		Retreat,
		LiftSiege,
		CounterBattle,
		WarOver,
		RealmChange,
		Surrender,
		CapturePoints,
		LeaderKilled,
		IdleLeaveBattle,
		None
	}

	public class Def : Logic.Def
	{
		public float base_experience;

		public float experience_retreat_mod = 0.2f;

		public float experience_per_squad_mod = 0.1f;

		public float experience_gain_speed = 500f;

		public float attack_range = 5f;

		public float tents_range = 3f;

		public float fight_range = 1f;

		public float trebuchet_range = 8.5f;

		public DT.Field preparation_time;

		public float preparation_time_base;

		public float duration;

		public float resume_plunder_progress_mod = 0.5f;

		public bool AI = true;

		public bool AI_retreat = true;

		public float AI_retreat_estimation_threshold = 0.2f;

		public int AI_retreat_min_lost_units = 1;

		public float AI_retreat_min_lost_perc = 50f;

		public int tt_grid_width = 5;

		public int tt_grid_height = 5;

		public float plunder_gold_base;

		public float plunder_per_gold_production;

		public float plunder_gold_per_gold;

		public float plunder_gold_per_book;

		public float plunder_gold_per_piety;

		public float plunder_gold_per_levy;

		public float plunder_gold_per_hammer;

		public float plunder_gold_per_worker;

		public float plunder_gold_per_commerce;

		public float plunder_supplies_per_food;

		public float plunder_supplies_per_levy;

		public float plunder_supplies_per_worker;

		public float plunder_supplies_base;

		public float plunder_books_per_book;

		public float plunder_per_supporter = 0.5f;

		public float plunder_supplies_per_supporter = 0.5f;

		public float attrition_tick = 15f;

		public int req_melee_attrition_troops = 20;

		public int req_ranged_attrition_troops = 4;

		public int req_equipment_attrition_troops = 1;

		public int efficient_siege_squads = 12;

		public float penalty_per_squad = 2f;

		public float resilience_defense_flat_mod = 0.5f;

		public float surrender_ratio = 1.5f;

		public float levy_resilience = 5f;

		public float resilience_damage_mod = 0.2f;

		public float keep_initial_resil_mod = 0.5f;

		public float keep_initial_siege_defense_mod = 0.5f;

		public float keep_resil_damage_mod = 10f;

		public float keep_siege_defense_damage_mod = 1f;

		public float base_siege_damage = 5f;

		public float sacking_gold_mul = 50f;

		public float sacking_worker_deaths = 0.1f;

		public float sacking_rebellious_deaths = 0.25f;

		public float sacking_levy_deaths = 0.5f;

		public float sacking_burned_structures = 0.25f;

		public float sacking_repair_coef = 0.5f;

		public float distance_between_armies = 36f;

		public float distance_between_squads = 2f;

		public float min_siege_defense_additional_defender_mod = 0.25f;

		public int max_guard_squads = 6;

		public int liberating_militia_squads_min = 2;

		public int liberating_militia_squads_max = 5;

		public int defense_levy = 2;

		public int infantry_levy = 1;

		public int ranged_levy = 1;

		public float militia_alive_workers_mod = 5f;

		public float militia_alive_workers_village_mod = 5f;

		public float militia_max_population_mod = 5f;

		public float militia_base;

		public float min_survival_health = 0.05f;

		public float max_initiative = 1000f;

		public float initiative_decay = 1f;

		public float initiative_take_capture_point = 100f;

		public float initiative_lose_capture_point = 100f;

		public float initiative_take_tower = 50f;

		public float initiative_lose_tower = 50f;

		public float initiative_take_gate = 75f;

		public float initiative_lose_gate = 75f;

		public int min_enter_battle_squads_player = 4;

		public int min_enter_battle_squads_ai = 4;

		public float min_enter_battle_attacker_estimation = 0.3f;

		public float min_enter_battle_defender_estimation = 0.3f;

		public float range_estimation_mod = 0.15f;

		public int min_troops = 15;

		public int max_troops = 40;

		public int min_cavalry = 6;

		public int max_cavalry = 15;

		public int min_fighting = 6;

		public int max_fighting = 15;

		public string our_morale_below_half_voice_line;

		public string enemy_morale_below_half_voice_line;

		public float max_reinforcement_distance = 300f;

		public float reinforcement_estimation_arrive_mod = 4f;

		public DT.Field supplies_consumed_at_end;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			base_experience = field.GetFloat("base_experience", null, base_experience);
			experience_retreat_mod = field.GetFloat("experience_retreat_mod", null, experience_retreat_mod);
			experience_gain_speed = field.GetFloat("experience_gain_speed", null, experience_gain_speed);
			experience_per_squad_mod = field.GetFloat("experience_per_squad_mod", null, experience_per_squad_mod);
			attack_range = field.GetFloat("attack_range", null, attack_range);
			tents_range = field.GetFloat("tents_range", null, tents_range);
			fight_range = field.GetFloat("fight_range", null, fight_range);
			trebuchet_range = field.GetFloat("trebuchet_range", null, trebuchet_range);
			preparation_time = field.FindChild("preparation_time");
			preparation_time_base = field.GetFloat("preparation_time_base", null, preparation_time_base);
			duration = field.GetFloat("duration", null, duration);
			resume_plunder_progress_mod = field.GetFloat("resume_plunder_progress_mod", null, resume_plunder_progress_mod);
			AI = field.GetBool("AI", null, AI);
			DT.Field field2 = field.FindChild("AI_retreat");
			if (field2 != null)
			{
				AI_retreat = field2.Bool(null, AI_retreat);
				AI_retreat_estimation_threshold = field2.GetFloat("estimation_threshold", null, AI_retreat_estimation_threshold);
				AI_retreat_min_lost_units = field2.GetInt("min_lost_units", null, AI_retreat_min_lost_units);
				AI_retreat_min_lost_perc = field2.GetFloat("min_lost_perc", null, AI_retreat_min_lost_perc);
			}
			tt_grid_width = field.GetInt("tt_grid_width", null, tt_grid_width);
			tt_grid_height = field.GetInt("tt_grid_height", null, tt_grid_height);
			plunder_gold_base = field.GetFloat("plunder_gold_base", null, plunder_gold_base);
			plunder_per_gold_production = field.GetFloat("plunder_per_gold_production", null, plunder_per_gold_production);
			plunder_gold_per_gold = field.GetFloat("plunder_gold_per_gold", null, plunder_gold_per_gold);
			plunder_gold_per_book = field.GetFloat("plunder_gold_per_book", null, plunder_gold_per_book);
			plunder_gold_per_piety = field.GetFloat("plunder_gold_per_piety", null, plunder_gold_per_piety);
			plunder_gold_per_levy = field.GetFloat("plunder_gold_per_levy", null, plunder_gold_per_levy);
			plunder_gold_per_hammer = field.GetFloat("plunder_gold_per_hammer", null, plunder_gold_per_hammer);
			plunder_gold_per_worker = field.GetFloat("plunder_gold_per_worker", null, plunder_gold_per_worker);
			plunder_gold_per_commerce = field.GetFloat("plunder_gold_per_commerce", null, plunder_gold_per_commerce);
			plunder_supplies_per_food = field.GetFloat("plunder_supplies_per_food", null, plunder_supplies_per_food);
			plunder_supplies_per_levy = field.GetFloat("plunder_supplies_per_levy", null, plunder_supplies_per_levy);
			plunder_supplies_per_worker = field.GetFloat("plunder_supplies_per_worker", null, plunder_supplies_per_worker);
			plunder_supplies_base = field.GetFloat("plunder_supplies_base", null, plunder_supplies_base);
			plunder_books_per_book = field.GetFloat("plunder_books_per_book", null, plunder_books_per_book);
			plunder_per_supporter = field.GetFloat("plunder_per_supporter", null, plunder_per_supporter);
			plunder_supplies_per_supporter = field.GetFloat("plunder_supplies_per_supporter", null, plunder_supplies_per_supporter);
			attrition_tick = field.GetFloat("attrition_tick", null, attrition_tick);
			req_melee_attrition_troops = field.GetInt("req_melee_attrition_troops", null, req_melee_attrition_troops);
			req_ranged_attrition_troops = field.GetInt("req_ranged_attrition_troops", null, req_ranged_attrition_troops);
			req_equipment_attrition_troops = field.GetInt("req_equipment_attrition_troops", null, req_equipment_attrition_troops);
			efficient_siege_squads = field.GetInt("efficient_siege_squads", null, efficient_siege_squads);
			penalty_per_squad = field.GetFloat("penalty_per_squad", null, penalty_per_squad);
			resilience_defense_flat_mod = field.GetFloat("resilience_defense_flat_mod", null, resilience_defense_flat_mod);
			surrender_ratio = field.GetFloat("surrender_ratio", null, surrender_ratio);
			levy_resilience = field.GetFloat("levy_resilience", null, levy_resilience);
			resilience_damage_mod = field.GetFloat("resilience_damage_mod", null, resilience_damage_mod);
			keep_initial_resil_mod = field.GetFloat("keep_initial_resil_mod", null, keep_initial_resil_mod);
			keep_initial_siege_defense_mod = field.GetFloat("keep_initial_siege_defense_mod", null, keep_initial_siege_defense_mod);
			keep_resil_damage_mod = field.GetFloat("keep_resil_damage_mod", null, keep_resil_damage_mod);
			keep_siege_defense_damage_mod = field.GetFloat("keep_siege_defense_damage_mod", null, keep_siege_defense_damage_mod);
			base_siege_damage = field.GetFloat("base_siege_damage", null, base_siege_damage);
			sacking_gold_mul = base.field.GetFloat("sacking_gold_mul", null, sacking_gold_mul);
			sacking_worker_deaths = base.field.GetFloat("sacking_worker_deaths", null, sacking_worker_deaths);
			sacking_rebellious_deaths = base.field.GetFloat("sacking_rebellious_deaths", null, sacking_rebellious_deaths);
			sacking_levy_deaths = base.field.GetFloat("sacking_levy_deaths", null, sacking_levy_deaths);
			sacking_burned_structures = base.field.GetFloat("sacking_burned_structures", null, sacking_burned_structures);
			distance_between_armies = base.field.GetFloat("distance_between_armies", null, distance_between_armies);
			distance_between_squads = base.field.GetFloat("distance_between_squads", null, distance_between_squads);
			max_guard_squads = base.field.GetInt("max_guard_squads", null, max_guard_squads);
			liberating_militia_squads_min = base.field.FindChild("liberating_militia_squads").Value(0);
			liberating_militia_squads_max = base.field.FindChild("liberating_militia_squads").Value(1);
			defense_levy = base.field.GetInt("defense_levy", null, defense_levy);
			infantry_levy = base.field.GetInt("infantry_levy", null, infantry_levy);
			ranged_levy = base.field.GetInt("ranged_levy", null, ranged_levy);
			min_siege_defense_additional_defender_mod = base.field.GetFloat("min_siege_defense_additional_defender_mod", null, min_siege_defense_additional_defender_mod);
			militia_alive_workers_mod = base.field.GetFloat("militia_alive_workers_mod", null, militia_alive_workers_mod);
			militia_max_population_mod = base.field.GetFloat("militia_max_population_mod", null, militia_max_population_mod);
			militia_alive_workers_village_mod = base.field.GetFloat("militia_alive_workers_village_mod", null, militia_alive_workers_village_mod);
			militia_base = base.field.GetFloat("militia_base", null, militia_base);
			min_survival_health = base.field.GetFloat("min_survival_health", null, min_survival_health);
			max_initiative = base.field.GetFloat("max_initiative", null, max_initiative);
			initiative_decay = base.field.GetFloat("initiative_decay", null, initiative_decay);
			initiative_take_capture_point = base.field.GetFloat("initiative_take_capture_point", null, initiative_take_capture_point);
			initiative_lose_capture_point = base.field.GetFloat("initiative_lose_capture_point", null, initiative_lose_capture_point);
			initiative_take_tower = base.field.GetFloat("initiative_take_tower", null, initiative_take_tower);
			initiative_lose_tower = base.field.GetFloat("initiative_lose_tower", null, initiative_lose_tower);
			initiative_take_gate = base.field.GetFloat("initiative_take_gate", null, initiative_take_gate);
			initiative_lose_gate = base.field.GetFloat("initiative_lose_gate", null, initiative_lose_gate);
			min_enter_battle_attacker_estimation = field.GetFloat("min_enter_battle_attacker_estimation", null, min_enter_battle_attacker_estimation);
			min_enter_battle_defender_estimation = field.GetFloat("min_enter_battle_defender_estimation", null, min_enter_battle_defender_estimation);
			min_enter_battle_squads_player = field.GetInt("min_enter_battle_squads_player", null, min_enter_battle_squads_player);
			min_enter_battle_squads_ai = field.GetInt("min_enter_battle_squads_ai", null, min_enter_battle_squads_ai);
			range_estimation_mod = field.GetFloat("range_estimation_mod", null, range_estimation_mod);
			min_troops = field.GetInt("Skirmish.min_troops");
			max_troops = field.GetInt("Skirmish.max_troops");
			min_cavalry = field.GetInt("Skirmish.min_cavalry");
			max_cavalry = field.GetInt("Skirmish.max_cavalry");
			min_fighting = field.GetInt("Skirmish.min_fighting");
			max_fighting = field.GetInt("Skirmish.max_fighting");
			our_morale_below_half_voice_line = field.GetString("OurMoraleBelowHalf");
			enemy_morale_below_half_voice_line = field.GetString("EnemyMoraleBelowHalf");
			max_reinforcement_distance = base.field.GetFloat("max_reinforcement_distance", null, max_reinforcement_distance);
			reinforcement_estimation_arrive_mod = base.field.GetFloat("reinforcement_estimation_arrive_mod", null, reinforcement_estimation_arrive_mod);
			supplies_consumed_at_end = field.FindChild("supplies_consumed_at_end");
			return true;
		}
	}

	public class Reinforcement
	{
		public Army army;

		public ComputableValue timer;

		public float estimate_time;

		public bool was_manually_set;

		public Army GetVisualArmy()
		{
			return army;
		}

		public Reinforcement()
		{
		}

		public Reinforcement(Reinforcement other)
		{
			was_manually_set = other.was_manually_set;
			army = other.army;
			if (other.timer != null)
			{
				timer = new ComputableValue(other.timer.Get(), other.timer.GetRate(), other.timer.game, other.timer.GetMin(), other.timer.GetMax());
			}
			estimate_time = other.estimate_time;
		}
	}

	public enum BreakSiegeFrom
	{
		Inside,
		Outside
	}

	public delegate Battle CreateBattle(Army attacker, MapObject defender);

	[Serialization.State(21)]
	public class InitState : Serialization.ObjectState
	{
		public string def_id;

		public byte battle_type;

		public NID attacker_nid;

		public NID defender_nid;

		public NID attacker_kingdom_nid;

		public NID defender_kingdom_nid;

		public NID settlement_nid;

		public int realm_id;

		public byte break_siege_from;

		public static InitState Create()
		{
			return new InitState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			battle_type = (byte)battle.type;
			attacker_nid = battle.attacker;
			defender_nid = battle.defender;
			attacker_kingdom_nid = battle.attacker_kingdom;
			defender_kingdom_nid = battle.defender_kingdom;
			settlement_nid = battle.settlement;
			realm_id = battle.realm_id;
			break_siege_from = (byte)battle.break_siege_from;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteByte(battle_type, "type");
			ser.WriteNID(attacker_nid, "attacker");
			ser.WriteNID(defender_nid, "defender");
			ser.WriteNID(attacker_kingdom_nid, "attacker_kingdom");
			ser.WriteNID(defender_kingdom_nid, "defender_kingdom");
			ser.WriteNID(settlement_nid, "settlement");
			ser.Write7BitSigned(realm_id, "realm_id");
			ser.WriteByte(break_siege_from, "break_siege_from");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			battle_type = ser.ReadByte("type");
			attacker_nid = ser.ReadNID("attacker");
			defender_nid = ser.ReadNID("defender");
			attacker_kingdom_nid = ser.ReadNID("attacker_kingdom");
			defender_kingdom_nid = ser.ReadNID("defender_kingdom");
			settlement_nid = ser.ReadNID("settlement");
			realm_id = ser.Read7BitSigned("realm_id");
			if (Serialization.cur_version >= 11)
			{
				break_siege_from = ser.ReadByte("break_siege_from");
			}
			else
			{
				break_siege_from = 0;
			}
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			Type type = battle.type;
			battle.type = (Type)battle_type;
			battle.break_siege_from = (BreakSiegeFrom)break_siege_from;
			if (type != battle.type)
			{
				battle.NotifyListeners("type_changed", type);
			}
			battle.def = battle.game.defs.Get<Def>(battle.type.ToString());
			battle.attacker = attacker_nid.GetObj(obj.game) as Army;
			battle.defender = defender_nid.GetObj(obj.game) as MapObject;
			battle.attacker_kingdom = attacker_kingdom_nid.GetObj(obj.game) as Kingdom;
			battle.defender_kingdom = defender_kingdom_nid.GetObj(obj.game) as Kingdom;
			if (battle.attacker != null && battle.defender != null)
			{
				battle.CalcDirection(battle.attacker.position, battle.defender.position);
			}
			battle.settlement = settlement_nid.GetObj(obj.game) as Settlement;
			if (battle.settlement != null)
			{
				battle.settlement.SetBattle(battle, send_state: false);
			}
			if (battle.simulation == null)
			{
				battle.simulation = new BattleSimulation(battle);
				battle.simulation.loaded_from_save = true;
			}
			battle.realm_id = realm_id;
			if (battle.type == Type.BreakSiege)
			{
				battle.StopSiegeComponents();
				battle.NotifyListeners("break_siege");
			}
			else if (battle.type == Type.Assault)
			{
				battle.StopSiegeComponents();
				battle.NotifyListeners("assault");
			}
			battle.simulation.ForceCalcTotals();
		}
	}

	[Serialization.State(22)]
	public class StageState : Serialization.ObjectState
	{
		public byte stage;

		public float elapsed;

		public int winner = -1;

		public int victory_reason;

		public NID cancelled_by;

		public float preparation_time;

		public static StageState Create()
		{
			return new StageState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			stage = (byte)battle.stage;
			elapsed = battle.game.time - battle.stage_time;
			if (battle.batte_view_game != null)
			{
				stage = 0;
				elapsed = 0f;
			}
			winner = battle.winner;
			victory_reason = (int)battle.victory_reason;
			cancelled_by = battle.cancelled_by;
			preparation_time = battle.preparation_time_cached;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteByte(stage, "stage");
			ser.WriteFloat(elapsed, "elapsed");
			if (stage == 4)
			{
				ser.WriteByte((byte)(winner + 1), "winner");
				ser.Write7BitUInt(victory_reason, "victory_reason");
				ser.WriteNID(cancelled_by, "cancelled_by");
			}
			ser.WriteFloat(preparation_time, "preparation_time");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			stage = ser.ReadByte("stage");
			elapsed = ser.ReadFloat("elapsed");
			if (stage == 4)
			{
				winner = ser.ReadByte("winner") - 1;
				victory_reason = ser.Read7BitUInt("victory_reason");
				cancelled_by = ser.ReadNID("cancelled_by");
			}
			if (Serialization.cur_version >= 4)
			{
				preparation_time = ser.ReadFloat("preparation_time");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			if (stage == 1)
			{
				stage = 0;
				elapsed = 0f;
			}
			if (stage == 4 && battle.simulation != null)
			{
				battle.winner = winner;
				battle.victory_reason = (VictoryReason)victory_reason;
				battle.cancelled_by = cancelled_by.GetObj(obj.game);
			}
			if (Serialization.cur_version >= 4)
			{
				battle.preparation_time_cached = preparation_time;
			}
			else
			{
				battle.preparation_time_cached = battle.def.preparation_time_base;
			}
			battle.SetStage((Stage)stage, send_state: false, elapsed);
		}
	}

	[Serialization.State(23)]
	public class ArmiesState : Serialization.ObjectState
	{
		public List<NID>[] army_nids = new List<NID>[2];

		public List<bool>[] is_supporter = new List<bool>[2];

		public static ArmiesState Create()
		{
			return new ArmiesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			for (int i = 0; i <= 1; i++)
			{
				List<Army> armies = battle.GetArmies(i);
				army_nids[i] = new List<NID>(armies.Count);
				is_supporter[i] = new List<bool>(armies.Count);
				for (int j = 0; j < armies.Count; j++)
				{
					Army army = armies[j];
					if (battle.batte_view_game == null || !battle.IsReinforcement(army))
					{
						NID item = army;
						army_nids[i].Add(item);
						is_supporter[i].Add(army.is_supporter);
					}
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			for (int i = 0; i <= 1; i++)
			{
				string text = ((i == 0) ? "attacker_" : "defender_");
				ser.Write7BitUInt(army_nids[i].Count, text + "count");
				for (int j = 0; j < army_nids[i].Count; j++)
				{
					ser.WriteNID<Army>(army_nids[i][j], text + "army", j);
					ser.WriteBool(is_supporter[i][j], text + "is_supporter", j);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			for (int i = 0; i <= 1; i++)
			{
				string text = ((i == 0) ? "attacker_" : "defender_");
				int num = ser.Read7BitUInt(text + "count");
				army_nids[i] = new List<NID>(num);
				is_supporter[i] = new List<bool>(num);
				for (int j = 0; j < num; j++)
				{
					NID item = ser.ReadNID<Army>(text + "army", j);
					army_nids[i].Add(item);
					bool item2 = ser.ReadBool(text + "is_supporter", j);
					is_supporter[i].Add(item2);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			for (int i = 0; i <= 1; i++)
			{
				List<Army> armies = battle.GetArmies(i);
				armies.Clear();
				List<NID> list = army_nids[i];
				for (int j = 0; j < list.Count; j++)
				{
					NID nID = list[j];
					Army army = nID.Get<Army>(obj.game);
					if (army == null)
					{
						Game.Log($"Error loading armies for battle {battle}: {nID} resolved to null", Game.LogType.Error);
						continue;
					}
					armies.Add(army);
					army.is_supporter = is_supporter[i][j];
				}
			}
			battle.NotifyListeners("armies_changed");
		}
	}

	[Serialization.State(24)]
	public class SimSquadsState : Serialization.ObjectState
	{
		public struct SquadInfo
		{
			public NID settlement_nid;

			public NID army_nid;

			public int unit_idx;

			public string def_id;

			public int battle_row;

			public int battle_col;

			public Point position;

			public Point initial_position;

			public float heading;

			public bool temporary;

			public float remaining_salvos;

			public float max_damage;

			public float initial_morale;

			public float initial_damage;

			public void InitFrom(BattleSimulation.Squad squad)
			{
				army_nid = squad.army;
				settlement_nid = squad.garrison?.settlement;
				unit_idx = squad.unit_idx;
				max_damage = squad.max_damage;
				def_id = squad.def.id;
				battle_row = squad.battle_row;
				battle_col = squad.battle_col;
				position = squad.position;
				initial_position = squad.initial_position;
				heading = squad.heading;
				temporary = squad.temporary;
				remaining_salvos = squad.remaining_salvos;
				initial_morale = squad.initial_morale;
				initial_damage = squad.initial_damage;
			}

			public void WriteBody(Serialization.IWriter ser, int idx = 0)
			{
				ser.WriteNID<Army>(army_nid, "army_nid", idx);
				ser.WriteNID(settlement_nid, "settlement_nid", idx);
				ser.Write7BitUInt(unit_idx + 1, "unit_idx", idx);
				ser.WriteFloat(max_damage, "max_damage", idx);
				ser.WriteStr(def_id, "def_id", idx);
				ser.Write7BitUInt(battle_row + 1, "battle_row", idx);
				ser.Write7BitUInt(battle_col + 1, "battle_col", idx);
				ser.WritePoint(position, "position", idx);
				ser.WritePoint(initial_position, "initial_position", idx);
				ser.WriteFloat(heading, "heading", idx);
				ser.WriteBool(temporary, "temporary", idx);
				ser.WriteFloat(remaining_salvos, "remaining_salvos", idx);
				ser.WriteFloat(initial_morale, "initial_morale", idx);
				ser.WriteFloat(initial_damage, "initial_damage", idx);
			}

			public void ReadBody(Serialization.IReader ser, int idx = 0)
			{
				army_nid = ser.ReadNID<Army>("army_nid", idx);
				settlement_nid = ser.ReadNID("settlement_nid", idx);
				unit_idx = ser.Read7BitUInt("unit_idx", idx) - 1;
				max_damage = ser.ReadFloat("max_damage", idx);
				def_id = ser.ReadStr("def_id", idx);
				battle_row = ser.Read7BitUInt("battle_row", idx) - 1;
				battle_col = ser.Read7BitUInt("battle_col", idx) - 1;
				position = ser.ReadPoint("position", idx);
				initial_position = ser.ReadPoint("initial_position", idx);
				heading = ser.ReadFloat("heading", idx);
				temporary = ser.ReadBool("temporary", idx);
				remaining_salvos = ser.ReadFloat("remaining_salvos", idx);
				initial_morale = ser.ReadFloat("initial_morale", idx);
				initial_damage = ser.ReadFloat("initial_damage", idx);
			}

			public BattleSimulation.Squad CreateSquad(BattleSimulation simulation, int side)
			{
				if (unit_idx == -1)
				{
					return null;
				}
				Game game = simulation.game;
				Settlement settlement = settlement_nid.GetObj(game) as Settlement;
				Garrison garrison = null;
				if (settlement != null)
				{
					switch (side)
					{
					case 1:
						garrison = settlement?.garrison;
						break;
					case 0:
						garrison = settlement?.attacker_garrison;
						break;
					}
					if (garrison == null)
					{
						return null;
					}
				}
				BattleSimulation.Squad squad = new BattleSimulation.Squad(game.multiplayer, simulation);
				squad.garrison = garrison;
				squad.army = army_nid.Get<Army>(game);
				Battle battle = simulation.obj as Battle;
				squad.unit_idx = unit_idx;
				Unit unit = squad.unit;
				if (battle != null && battle.IsFinishing())
				{
					squad.unit = null;
					if (unit != null)
					{
						unit.simulation = null;
					}
				}
				else
				{
					squad.unit = unit;
					if (squad.unit != null)
					{
						squad.unit.simulation = squad;
					}
				}
				squad.def = game.defs.Get<Unit.Def>(def_id);
				squad.salvo_def = game.defs.Get<SalvoData.Def>(squad.def.salvo_def);
				squad.battle_side = side;
				squad.max_damage = max_damage;
				squad.battle_row = battle_row;
				squad.battle_col = battle_col;
				squad.position = position;
				squad.initial_position = initial_position;
				squad.heading = heading;
				squad.temporary = temporary;
				squad.remaining_salvos = remaining_salvos;
				squad.initial_morale = initial_morale;
				squad.initial_damage = initial_damage;
				return squad;
			}
		}

		[Serialization.Substate(1)]
		public class SquadDamageState : Serialization.ObjectSubstate
		{
			public float damage;

			public SquadDamageState()
			{
			}

			public SquadDamageState(int idx, BattleSimulation.Squad squad)
			{
				substate_index = idx;
				damage = squad.damage;
			}

			public static SquadDamageState Create()
			{
				return new SquadDamageState();
			}

			public static bool IsNeeded(Object obj)
			{
				if ((obj as Battle).simulation == null)
				{
					return false;
				}
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				BattleSimulation simulation = (obj as Battle).simulation;
				if (simulation == null)
				{
					return false;
				}
				BattleSimulation.Squad squadByIndex = simulation.GetSquadByIndex(substate_index);
				if (squadByIndex == null)
				{
					return false;
				}
				damage = squadByIndex.damage;
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteFloat(damage, "damage");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				damage = ser.ReadFloat("damage");
			}

			public override void ApplyTo(Object obj)
			{
				Battle battle = obj as Battle;
				BattleSimulation simulation = (obj as Battle).simulation;
				if (simulation != null)
				{
					BattleSimulation.Squad squadByIndex = simulation.GetSquadByIndex(substate_index);
					if (squadByIndex != null)
					{
						squadByIndex.damage = damage;
						simulation.totals_dirty = true;
						battle.NotifyListeners("changed");
					}
				}
			}
		}

		[Serialization.Substate(2)]
		public class SquadStateInfoState : Serialization.ObjectSubstate
		{
			public BattleSimulation.Squad.State state;

			public Point position;

			public Point tgt_position;

			public float duration;

			public int target_id;

			public SquadStateInfoState()
			{
			}

			public SquadStateInfoState(int idx, BattleSimulation.Squad squad)
			{
				substate_index = idx;
				state = squad.state;
				position = squad.position;
				tgt_position = squad.position;
				state = squad.state;
				duration = squad.state_end_time - squad.simulation.game.time;
				if (squad.target != null && squad.simulation != null)
				{
					target_id = squad.simulation.GetSquadIndex(squad.target);
				}
				else
				{
					target_id = -1;
				}
			}

			public static SquadStateInfoState Create()
			{
				return new SquadStateInfoState();
			}

			public static bool IsNeeded(Object obj)
			{
				if ((obj as Battle).simulation == null)
				{
					return false;
				}
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				BattleSimulation simulation = (obj as Battle).simulation;
				if (simulation == null)
				{
					return false;
				}
				BattleSimulation.Squad squadByIndex = simulation.GetSquadByIndex(substate_index);
				if (squadByIndex == null)
				{
					return false;
				}
				state = squadByIndex.state;
				position = squadByIndex.position;
				tgt_position = squadByIndex.tgt_position;
				state = squadByIndex.state;
				if (squadByIndex.state_end_time != Time.Zero)
				{
					duration = squadByIndex.state_end_time - squadByIndex.simulation.game.time;
				}
				else
				{
					duration = 0f;
				}
				if (squadByIndex.target != null)
				{
					target_id = simulation.GetSquadIndex(squadByIndex.target);
				}
				else
				{
					target_id = -1;
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteByte((byte)state, "state");
				ser.WritePoint(position, "position");
				ser.WritePoint(tgt_position, "tgt_position");
				ser.Write7BitSigned(target_id, "target_id");
				ser.WriteFloat(duration, "duration");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				state = (BattleSimulation.Squad.State)ser.ReadByte("state");
				position = ser.ReadPoint("position");
				tgt_position = ser.ReadPoint("tgt_position");
				target_id = ser.Read7BitSigned("target_id");
				duration = ser.ReadFloat("duration");
			}

			public override void ApplyTo(Object obj)
			{
				BattleSimulation simulation = (obj as Battle).simulation;
				if (simulation == null)
				{
					return;
				}
				BattleSimulation.Squad squadByIndex = simulation.GetSquadByIndex(substate_index);
				if (squadByIndex != null)
				{
					squadByIndex.state = state;
					squadByIndex.position = position;
					squadByIndex.tgt_position = tgt_position;
					squadByIndex.target = simulation.GetSquadByIndex(target_id);
					if (duration > 0f)
					{
						squadByIndex.state_end_time = squadByIndex.simulation.game.time + duration;
					}
					else
					{
						squadByIndex.state_end_time = Time.Zero;
					}
				}
			}
		}

		[Serialization.Substate(3)]
		public class SquadMoraleState : Serialization.ObjectSubstate
		{
			public float temporary_morale;

			public SquadMoraleState()
			{
			}

			public SquadMoraleState(int idx, BattleSimulation.Squad squad)
			{
				substate_index = idx;
				temporary_morale = squad.temporary_morale;
			}

			public static SquadMoraleState Create()
			{
				return new SquadMoraleState();
			}

			public static bool IsNeeded(Object obj)
			{
				if ((obj as Battle).simulation == null)
				{
					return false;
				}
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				BattleSimulation simulation = (obj as Battle).simulation;
				if (simulation == null)
				{
					return false;
				}
				BattleSimulation.Squad squadByIndex = simulation.GetSquadByIndex(substate_index);
				if (squadByIndex == null)
				{
					return false;
				}
				temporary_morale = squadByIndex.temporary_morale;
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteFloat(temporary_morale, "temporary_morale");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				temporary_morale = ser.ReadFloat("temporary_morale");
			}

			public override void ApplyTo(Object obj)
			{
				Battle battle = obj as Battle;
				BattleSimulation simulation = (obj as Battle).simulation;
				if (simulation != null)
				{
					BattleSimulation.Squad squadByIndex = simulation.GetSquadByIndex(substate_index);
					if (squadByIndex != null)
					{
						squadByIndex.temporary_morale = temporary_morale;
						simulation.totals_dirty = true;
						battle.NotifyListeners("changed");
					}
				}
			}
		}

		public SquadInfo[] squad_infos;

		public int attackers;

		public int defenders;

		public static SimSquadsState Create()
		{
			return new SimSquadsState();
		}

		public static bool IsNeeded(Object obj)
		{
			BattleSimulation simulation = (obj as Battle).simulation;
			if (simulation == null)
			{
				return false;
			}
			if (simulation.attacker_squads.Count <= 0 && simulation.defender_squads.Count <= 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			BattleSimulation simulation = (obj as Battle).simulation;
			if (simulation == null)
			{
				return false;
			}
			attackers = simulation.attacker_squads.Count;
			defenders = simulation.defender_squads.Count;
			if (attackers <= 0 && defenders <= 0)
			{
				return false;
			}
			List<SquadInfo> list = new List<SquadInfo>(attackers + defenders);
			int num = 0;
			attackers = 0;
			defenders = 0;
			bool flag = simulation.battle?.batte_view_game != null;
			for (int i = 0; i <= 1; i++)
			{
				List<BattleSimulation.Squad> squads = simulation.GetSquads(i);
				for (int j = 0; j < squads.Count; j++)
				{
					BattleSimulation.Squad squad = squads[j];
					if (!(squad.army != null && (simulation.battle.IsReinforcement(squad.army) || squad.temporary) && flag))
					{
						SquadInfo item = default(SquadInfo);
						item.InitFrom(squad);
						list.Add(item);
						AddSubstate(new SquadDamageState(num, squad));
						AddSubstate(new SquadStateInfoState(num, squad));
						AddSubstate(new SquadMoraleState(num, squad));
						num++;
						if (i == 0)
						{
							attackers++;
						}
						else
						{
							defenders++;
						}
					}
				}
			}
			squad_infos = list.ToArray();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(attackers, "attackers");
			ser.Write7BitUInt(defenders, "defenders");
			if (squad_infos != null)
			{
				for (int i = 0; i < squad_infos.Length; i++)
				{
					squad_infos[i].WriteBody(ser, i);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			attackers = ser.Read7BitUInt("attackers");
			defenders = ser.Read7BitUInt("defenders");
			int num = attackers + defenders;
			if (num > 0)
			{
				squad_infos = new SquadInfo[num];
				for (int i = 0; i < num; i++)
				{
					squad_infos[i].ReadBody(ser, i);
				}
			}
		}

		private void SetSquads(BattleSimulation simulation, int side, int count, ref int idx)
		{
			List<BattleSimulation.Squad> squads = simulation.GetSquads(side);
			for (int i = 0; i < squads.Count; i++)
			{
				BattleSimulation.Squad squad = squads[i];
				if (squad?.unit != null)
				{
					squad.unit.simulation = null;
				}
			}
			squads.Clear();
			for (int j = 0; j < count; j++)
			{
				BattleSimulation.Squad squad2 = squad_infos[idx].CreateSquad(simulation, side);
				if (squad2 == null)
				{
					simulation.Error($"Failed to create squad at idx {idx}");
					idx++;
				}
				else
				{
					idx++;
					squads.Add(squad2);
				}
			}
			for (int k = 0; k < squads.Count; k++)
			{
				squads[k].RecalcPermanentMorale();
			}
		}

		public override void ApplyTo(Object obj)
		{
			BattleSimulation simulation = (obj as Battle).simulation;
			if (simulation != null)
			{
				int idx = 0;
				SetSquads(simulation, 0, attackers, ref idx);
				SetSquads(simulation, 1, defenders, ref idx);
				simulation.totals_dirty = true;
				simulation.CalcTotals();
				obj.NotifyListeners("changed");
			}
		}
	}

	[Serialization.State(25)]
	public class BattleBonusesState : Serialization.ObjectState
	{
		public List<string> defs = new List<string>();

		public static BattleBonusesState Create()
		{
			return new BattleBonusesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Battle).battle_bonuses != null;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			if (battle.battle_bonuses == null)
			{
				return false;
			}
			for (int i = 0; i < battle.battle_bonuses.Count; i++)
			{
				string item = battle.battle_bonuses[i].id;
				defs.Add(item);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(defs.Count, "count");
			for (int i = 0; i < defs.Count; i++)
			{
				ser.WriteStr(defs[i], "def_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				string item = ser.ReadStr("def_", i);
				defs.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			if (battle.battle_bonuses == null)
			{
				battle.battle_bonuses = new List<BattleBonus.Def>();
			}
			for (int i = 0; i < defs.Count; i++)
			{
				BattleBonus.Def item = obj.game.defs.Get<BattleBonus.Def>(defs[i]);
				battle.battle_bonuses.Add(item);
			}
		}
	}

	[Serialization.State(26)]
	public class SiegeStatsState : Serialization.ObjectState
	{
		public float initial_siege_defense_pre_condition;

		public float initial_resilience_pre_condition;

		public float initial_siege_defense;

		public float initial_resilience;

		public float siege_defense;

		public float resilience;

		public static SiegeStatsState Create()
		{
			return new SiegeStatsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			initial_siege_defense_pre_condition = battle.initial_siege_defense_pre_condition;
			initial_resilience_pre_condition = battle.initial_resilience_pre_condition;
			initial_siege_defense = battle.initial_siege_defense;
			initial_resilience = battle.initial_resilience;
			siege_defense = battle.siege_defense;
			resilience = battle.resilience;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(initial_siege_defense_pre_condition, "initial_siege_defense_pre_condition");
			ser.WriteFloat(initial_resilience_pre_condition, "initial_resilience_pre_condition");
			ser.WriteFloat(initial_siege_defense, "initial_siege_defense");
			ser.WriteFloat(initial_resilience, "initial_resilience");
			ser.WriteFloat(siege_defense, "siege_defense");
			ser.WriteFloat(resilience, "resilience");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			initial_siege_defense_pre_condition = ser.ReadFloat("initial_siege_defense_pre_condition");
			initial_resilience_pre_condition = ser.ReadFloat("initial_resilience_pre_condition");
			initial_siege_defense = ser.ReadFloat("initial_siege_defense");
			initial_resilience = ser.ReadFloat("initial_resilience");
			siege_defense = ser.ReadFloat("siege_defense");
			resilience = ser.ReadFloat("resilience");
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			battle.initial_siege_defense_pre_condition = initial_siege_defense_pre_condition;
			battle.initial_resilience_pre_condition = initial_resilience_pre_condition;
			if (initial_resilience <= 0f)
			{
				initial_resilience = 1f;
			}
			if (initial_siege_defense <= 0f)
			{
				initial_siege_defense = 1f;
			}
			battle.initial_siege_defense = initial_siege_defense;
			battle.initial_resilience = initial_resilience;
			if (battle.siege_defense != siege_defense)
			{
				battle.siege_defense = siege_defense;
				battle.NotifyListeners("fortification_health_changed");
			}
			battle.resilience = resilience;
		}
	}

	[Serialization.State(27)]
	public class SimArrowsState : Serialization.ObjectState
	{
		public int attacker_number_of_ranged_attacks;

		public int defender_number_of_ranged_attacks;

		public bool thought_archer_rounds_last_tick;

		public static SimArrowsState Create()
		{
			return new SimArrowsState();
		}

		public static bool IsNeeded(Object obj)
		{
			BattleSimulation simulation = (obj as Battle).simulation;
			if (simulation == null)
			{
				return false;
			}
			if (simulation.attacker_squads.Count <= 0 && simulation.defender_squads.Count <= 0 && !simulation.thought_archer_rounds_last_tick)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			BattleSimulation simulation = (obj as Battle).simulation;
			if (simulation == null)
			{
				return false;
			}
			attacker_number_of_ranged_attacks = simulation.attacker_totals.number_of_ranged_attacks;
			defender_number_of_ranged_attacks = simulation.defender_totals.number_of_ranged_attacks;
			thought_archer_rounds_last_tick = simulation.thought_archer_rounds_last_tick;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(attacker_number_of_ranged_attacks, "attacker_number_of_ranged_attacks");
			ser.Write7BitSigned(defender_number_of_ranged_attacks, "defender_number_of_ranged_attacks");
			ser.WriteBool(thought_archer_rounds_last_tick, "thought_archer_rounds_last_tick");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			attacker_number_of_ranged_attacks = ser.Read7BitSigned("attacker_number_of_ranged_attacks");
			defender_number_of_ranged_attacks = ser.Read7BitSigned("defender_number_of_ranged_attacks");
			thought_archer_rounds_last_tick = ser.ReadBool("thought_archer_rounds_last_tick");
		}

		public override void ApplyTo(Object obj)
		{
			BattleSimulation simulation = (obj as Battle).simulation;
			if (simulation != null)
			{
				simulation.attacker_totals.number_of_ranged_attacks = attacker_number_of_ranged_attacks;
				simulation.defender_totals.number_of_ranged_attacks = defender_number_of_ranged_attacks;
				simulation.thought_archer_rounds_last_tick = thought_archer_rounds_last_tick;
			}
		}
	}

	[Serialization.State(28)]
	public class BattleVarsState : Serialization.ObjectState
	{
		private Data data;

		public static BattleVarsState Create()
		{
			return new BattleVarsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Battle battle))
			{
				return false;
			}
			data = battle.Vars().CreateFullData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "data");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("data");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Battle battle)
			{
				Vars vars = new Vars();
				data.ApplyTo(vars, obj.game);
				battle.SetVars(vars);
			}
		}
	}

	[Serialization.State(29)]
	public class PlunderProgressState : Serialization.ObjectState
	{
		public Data plunder_data;

		public static PlunderProgressState Create()
		{
			return new PlunderProgressState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Battle).plunder_progress != null;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			plunder_data = battle.plunder_progress.CreateData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(plunder_data, "plunder_data");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			plunder_data = ser.ReadData("plunder_data");
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			if (battle.plunder_progress == null)
			{
				battle.plunder_progress = new ComputableValue(0f, 0f, battle.game, 0f, battle.def.duration);
			}
			plunder_data.ApplyTo(battle.plunder_progress, battle.game);
		}
	}

	[Serialization.State(30)]
	public class SpawnedDefendersState : Serialization.ObjectState
	{
		public bool added_guards;

		public static SpawnedDefendersState Create()
		{
			return new SpawnedDefendersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Battle).added_guards;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			added_guards = battle.added_guards;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(added_guards, "added_guards");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			added_guards = ser.ReadBool("added_guards");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Battle).added_guards = added_guards;
		}
	}

	[Serialization.State(31)]
	public class ReinforcementsState : Serialization.ObjectState
	{
		public NID attacker_reinforcement;

		public NID defender_reinforcement;

		public NID attacker_secondary_reinforcement;

		public NID defender_secondary_reinforcement;

		public static ReinforcementsState Create()
		{
			return new ReinforcementsState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (obj is Battle battle && !battle.game.IsMultiplayer())
			{
				return battle.HasReinforcements();
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			if (battle.batte_view_game == null)
			{
				attacker_reinforcement = battle.reinforcements[0].army;
				defender_reinforcement = battle.reinforcements[1].army;
				attacker_secondary_reinforcement = battle.reinforcements[2].army;
				defender_secondary_reinforcement = battle.reinforcements[3].army;
			}
			else
			{
				attacker_reinforcement = battle.reinforcements_at_start_of_battleview[0].army;
				defender_reinforcement = battle.reinforcements_at_start_of_battleview[1].army;
				attacker_secondary_reinforcement = battle.reinforcements_at_start_of_battleview[2].army;
				defender_secondary_reinforcement = battle.reinforcements_at_start_of_battleview[3].army;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(attacker_reinforcement, "attacker_reinforcement");
			ser.WriteNID(defender_reinforcement, "defender_reinforcement");
			ser.WriteNID(attacker_secondary_reinforcement, "attacker_secondary_reinforcement");
			ser.WriteNID(defender_secondary_reinforcement, "defender_secondary_reinforcement");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			attacker_reinforcement = ser.ReadNID("attacker_reinforcement");
			defender_reinforcement = ser.ReadNID("defender_reinforcement");
			if (Serialization.cur_version >= 3)
			{
				attacker_secondary_reinforcement = ser.ReadNID("attacker_secondary_reinforcement");
				defender_secondary_reinforcement = ser.ReadNID("defender_secondary_reinforcement");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			Army army = attacker_reinforcement.Get<Army>(obj.game);
			Army army2 = defender_reinforcement.Get<Army>(obj.game);
			Army army3 = attacker_secondary_reinforcement.Get<Army>(obj.game);
			Army army4 = defender_secondary_reinforcement.Get<Army>(obj.game);
			battle.SetReinforcements(army, 0, battle.CalcReinforcementTime(army), force: false, send_state: false);
			battle.SetReinforcements(army2, 1, battle.CalcReinforcementTime(army2), force: false, send_state: false);
			battle.SetReinforcements(army3, 2, battle.CalcReinforcementTime(army3), force: false, send_state: false);
			battle.SetReinforcements(army4, 3, battle.CalcReinforcementTime(army4), force: false, send_state: false);
		}
	}

	[Serialization.State(32)]
	public class FoodStorageState : Serialization.ObjectState
	{
		public Data food_storage_data;

		public static FoodStorageState Create()
		{
			return new FoodStorageState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Battle)?.settlement_food_copy != null;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			food_storage_data = battle.settlement_food_copy.CreateData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(food_storage_data, "food_storage_data");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			food_storage_data = ser.ReadData("food_storage_data");
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			if (battle.settlement_food_copy == null)
			{
				battle.settlement_food_copy = new ComputableValue(0f, 0f, battle.game, 0f, 0f);
			}
			food_storage_data.ApplyTo(battle.settlement_food_copy, battle.game);
		}
	}

	[Serialization.State(33)]
	public class IntendedReinforcementsState : Serialization.ObjectState
	{
		public NID[] intended_reinforcements;

		public static IntendedReinforcementsState Create()
		{
			return new IntendedReinforcementsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Battle battle = obj as Battle;
			if (battle?.intended_reinforcements != null)
			{
				return battle.intended_reinforcements.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			int count = battle.intended_reinforcements.Count;
			intended_reinforcements = new NID[count];
			for (int i = 0; i < count; i++)
			{
				intended_reinforcements[i] = battle.intended_reinforcements[i];
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(intended_reinforcements.Length, "cnt");
			for (int i = 0; i < intended_reinforcements.Length; i++)
			{
				ser.WriteNID(intended_reinforcements[i], "intended_reinforcements_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("cnt");
			intended_reinforcements = new NID[num];
			for (int i = 0; i < num; i++)
			{
				intended_reinforcements[i] = ser.ReadNID("intended_reinforcements_", i);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Battle battle = obj as Battle;
			battle.intended_reinforcements = new List<Army>();
			for (int i = 0; i < intended_reinforcements.Length; i++)
			{
				Army army = intended_reinforcements[i].Get<Army>(battle.game);
				if (army != null)
				{
					army.last_intended_battle = battle;
					battle.intended_reinforcements.Add(army);
				}
			}
		}
	}

	[Serialization.State(34)]
	public class AssaultGateActionState : Serialization.ObjectState
	{
		public bool assault_gate_action_succeeded;

		public static AssaultGateActionState Create()
		{
			return new AssaultGateActionState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (obj is Battle battle)
			{
				return battle.is_siege;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Battle battle = obj as Battle;
			assault_gate_action_succeeded = battle.assault_gate_action_succeeded;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(assault_gate_action_succeeded, "assault_gate_action_succeeded");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			assault_gate_action_succeeded = ser.ReadBool("assault_gate_action_succeeded");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Battle).SetAssaultGate(assault_gate_action_succeeded, send_state: false);
		}
	}

	[Serialization.Event(37)]
	public class ActionEvent : Serialization.ObjectEvent
	{
		public string action;

		public int side;

		public string param;

		public ActionEvent()
		{
		}

		public static ActionEvent Create()
		{
			return new ActionEvent();
		}

		public ActionEvent(string action, int side, string param = "")
		{
			this.action = action;
			this.side = side;
			this.param = param;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(action, "action");
			ser.WriteByte((byte)side, "side");
			ser.WriteStr(param, "param");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			action = ser.ReadStr("action");
			side = ser.ReadByte("side");
			param = ser.ReadStr("param");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Battle).DoAction(action, side, param);
		}
	}

	public Def def;

	public VictoryReason victory_reason = VictoryReason.None;

	public VictoryReason battle_view_victory_reason = VictoryReason.None;

	public bool battle_map_finished;

	public Type type;

	public BreakSiegeFrom break_siege_from;

	public bool battle_map_only;

	public Point direction;

	public float heading;

	public int realm_id;

	public Stage stage;

	public Time stage_time = Time.Zero;

	public Time battle_view_game_start = Time.Zero;

	public float preparation_time_cached;

	public int winner = -1;

	public Object cancelled_by;

	public List<BattleBonus.Def> battle_bonuses;

	private bool can_add_experience = true;

	public Settlement settlement;

	public Army attacker;

	public MapObject defender;

	public Kingdom attacker_kingdom;

	public Kingdom defender_kingdom;

	public List<Army> attackers = new List<Army>();

	public List<Army> defenders = new List<Army>();

	public int bv_attackers;

	public int bv_defenders;

	public float[] starting_troops = new float[2];

	public int[] starting_squads = new int[2];

	public float[] troops_killed = new float[2];

	private bool can_assault;

	public ComputableValue initiative;

	public int initiative_side;

	public float initiative_cooldown;

	public bool initiative_countdown;

	public bool initiative_auto_leave_battle = true;

	public bool fortification_destroyed;

	public bool assault_gate_action_succeeded;

	private const float siege_tick_base_time = 10f;

	private const float siege_tick_random_min = 0f;

	private const float siege_tick_random_max = 10f;

	public ComputableValue settlement_food_copy;

	public float siege_defense;

	public float initial_resilience_pre_condition;

	public float initial_siege_defense_pre_condition;

	public float initial_siege_defense;

	public float initial_resilience;

	public float resilience;

	private ResilienceDrop resil_drop_component;

	private SiegeDefenseDrop siege_defense_drop_component;

	public ComputableValue plunder_progress;

	private List<Army> intended_reinforcements;

	private Reinforcement[] _reinforcements;

	public Reinforcement[] reinforcements_at_start_of_battleview;

	public SquadPowerGrid[] power_grids;

	public BattleAI[] ai;

	public SquadsList squads = new SquadsList();

	public Game batte_view_game;

	public List<int> battle_view_kingdoms;

	public List<PassableGate> gates = new List<PassableGate>();

	public List<CapturePoint> capture_points;

	public List<Fortification> fortifications;

	public List<Fortification> towers;

	public List<Point> wall_corners;

	public PassableGate closest_gate_to_camps;

	public Point wall_center;

	public Dictionary<int, Ladder> ladders;

	public byte[,] tree_count_grid;

	public int tree_grid_size;

	public int tree_count_grid_width;

	public int tree_count_grid_height;

	public PPos citadel_position;

	public bool has_restarted;

	public bool has_exit_city;

	public bool has_ladder_placed;

	public bool added_guards;

	public int tt_x;

	public int tt_y;

	public TerrainType[,] tt_grid;

	public BattleSimulation simulation;

	public bool player_chosen_tactics;

	private Vars vars;

	private bool idle_leaving_battle;

	private List<Unit>[] retreated_units = new List<Unit>[2];

	private bool retreated;

	private const int STATES_IDX = 20;

	private const int EVENTS_IDX = 36;

	public Garrison garrison
	{
		get
		{
			if (settlement == null)
			{
				return null;
			}
			return settlement.garrison;
		}
	}

	public Army attacker_support
	{
		get
		{
			if (attackers.Count >= 2)
			{
				return attackers[1];
			}
			return null;
		}
	}

	public Army defender_support
	{
		get
		{
			if (defenders.Count >= 2)
			{
				return defenders[1];
			}
			return null;
		}
	}

	public bool is_siege
	{
		get
		{
			if (type != Type.Siege && type != Type.BreakSiege)
			{
				return type == Type.Assault;
			}
			return true;
		}
	}

	public float castle_defender_bonus
	{
		get
		{
			float num = initial_siege_defense_pre_condition * initial_siege_defense_pre_condition;
			if (num == 0f)
			{
				return 0f;
			}
			return simulation.def.castle_defenders_mod * initial_siege_defense_pre_condition * (assault_gate_action_succeeded ? simulation.def.gate_assaulted_defense_mod : 1f) * (simulation.def.min_castle_defender_bonus + (1f - simulation.def.min_castle_defender_bonus) * ((num - (float)Math.Pow(initial_siege_defense_pre_condition - siege_defense, 2.0)) / num));
		}
	}

	public bool is_plunder
	{
		get
		{
			if (type != Type.Plunder)
			{
				return type == Type.PlunderInterrupt;
			}
			return true;
		}
	}

	public List<string> killed_nobles
	{
		get
		{
			Vars vars = Vars();
			if (vars == null)
			{
				return null;
			}
			List<string> list = vars.Get<List<string>>("killed_nobles");
			if (list == null)
			{
				list = new List<string>();
				vars.Set("killed_nobles", list);
			}
			return list;
		}
	}

	public List<Character> imprisoned_at_end_of_battle_characters
	{
		get
		{
			Vars vars = Vars();
			if (vars == null)
			{
				return null;
			}
			List<Character> list = vars.Get<List<Character>>("imprisoned_nobles_characters");
			if (list == null)
			{
				list = new List<Character>();
				vars.Set("imprisoned_nobles_characters", list);
			}
			return list;
		}
	}

	public List<Character> escaped_nobles
	{
		get
		{
			Vars vars = Vars();
			if (vars == null)
			{
				return null;
			}
			List<Character> list = vars.Get<List<Character>>("escaped_nobles");
			if (list == null)
			{
				list = new List<Character>();
				vars.Set("escaped_nobles", list);
			}
			return list;
		}
	}

	public Reinforcement[] reinforcements
	{
		get
		{
			if (_reinforcements == null)
			{
				_reinforcements = new Reinforcement[4];
				for (int i = 0; i < _reinforcements.Length; i++)
				{
					_reinforcements[i] = new Reinforcement();
				}
			}
			return _reinforcements;
		}
	}

	public bool IsFinishing()
	{
		return stage >= Stage.Finishing;
	}

	public bool IsReinforcement(Army army)
	{
		for (int i = 0; i < reinforcements.Length; i++)
		{
			if (reinforcements[i].army == army)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasReinforcements()
	{
		for (int i = 0; i < reinforcements.Length; i++)
		{
			if (reinforcements[i] != null)
			{
				return true;
			}
		}
		return false;
	}

	public static bool Intersect(Point line1V1, Point line1V2, Point line2V1, Point line2V2, out Point resPoint)
	{
		float num = line1V2.y - line1V1.y;
		float num2 = line1V1.x - line1V2.x;
		float num3 = num * line1V1.x + num2 * line1V1.y;
		float num4 = line2V2.y - line2V1.y;
		float num5 = line2V1.x - line2V2.x;
		float num6 = num4 * line2V1.x + num5 * line2V1.y;
		float num7 = num * num5 - num4 * num2;
		if (Math.Abs(num7) <= 0.001f)
		{
			resPoint = line1V1;
			return false;
		}
		float x = (num5 * num3 - num2 * num6) / num7;
		float y = (num * num6 - num4 * num3) / num7;
		resPoint = new Point(x, y);
		return true;
	}

	public bool IsOutsideWall(Point pt)
	{
		if (wall_corners == null)
		{
			return true;
		}
		for (int i = 0; i < wall_corners.Count; i++)
		{
			Point point = wall_corners[i];
			Point point2 = wall_corners[(i + 1) % wall_corners.Count];
			if (Intersect(point, point2, pt, wall_center, out var resPoint) && CheckIfPointIsInRectangle(resPoint, point, point2) && CheckIfPointIsInRectangle(resPoint, pt, wall_center))
			{
				return true;
			}
		}
		return false;
	}

	public static bool CheckIfPointIsInRectangle(Point point, Point recPos1, Point recPos2, double error = 0.0)
	{
		float x = point.x;
		float y = point.y;
		Point point2 = recPos1;
		Point point3 = recPos2;
		if (((double)point2.x - error <= (double)x && (double)x <= (double)point3.x + error) || ((double)point2.x + error >= (double)x && (double)x >= (double)point3.x - error))
		{
			if (!((double)point2.y - error <= (double)y) || !((double)y <= (double)point3.y + error))
			{
				if ((double)point2.y + error >= (double)y)
				{
					return (double)y >= (double)point3.y - error;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public void SetTreeData(byte[,] tree_count_grid, int cell_size, int width, int height)
	{
		this.tree_count_grid = tree_count_grid;
		tree_grid_size = cell_size;
		tree_count_grid_width = width;
		tree_count_grid_height = height;
	}

	public int GetTreeCount(Point pos)
	{
		if (tree_count_grid == null)
		{
			return 0;
		}
		float x = pos.x;
		float y = pos.y;
		x /= (float)tree_grid_size;
		y /= (float)tree_grid_size;
		x -= 0.5f;
		y -= 0.5f;
		int num = (int)Math.Floor(x);
		int num2 = (int)Math.Floor(y);
		if (!InBounds(num, num2, tree_count_grid_height, tree_count_grid_height))
		{
			return 0;
		}
		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		byte b = tree_count_grid[num, num2];
		if (!InBounds(num + 1, num2, tree_count_grid_width, tree_count_grid_height))
		{
			return b;
		}
		byte b2 = tree_count_grid[num + 1, num2];
		if (!InBounds(num, num2 + 1, tree_count_grid_width, tree_count_grid_height))
		{
			return b;
		}
		byte b3 = tree_count_grid[num, num2 + 1];
		if (!InBounds(num + 1, num2 + 1, tree_count_grid_width, tree_count_grid_height))
		{
			return b;
		}
		byte b4 = tree_count_grid[num + 1, num2 + 1];
		float num5 = (float)(int)b + (float)(b2 - b) * num3;
		float num6 = (float)(int)b3 + (float)(b4 - b3) * num3;
		return (byte)(num5 + (num6 - num5) * num4);
	}

	public static bool InBounds(int x, int y, int grid_width, int grid_height)
	{
		if (x > 0 && x < grid_width && y > 0)
		{
			return y < grid_height;
		}
		return false;
	}

	public Vars Vars()
	{
		if (vars == null)
		{
			FillVars();
		}
		return vars;
	}

	public void SetVars(Vars vars)
	{
		this.vars = vars;
		NotifyListeners("vars_changed");
	}

	private Kingdom GetWinnerKingdom()
	{
		return winner switch
		{
			0 => attacker_kingdom, 
			1 => defender_kingdom, 
			_ => null, 
		};
	}

	private Kingdom GetLoserKingdom()
	{
		return winner switch
		{
			0 => defender_kingdom, 
			1 => attacker_kingdom, 
			_ => null, 
		};
	}

	private void FillVars(bool send_state = true)
	{
		if (IsAuthority())
		{
			if (vars == null)
			{
				vars = new Vars();
			}
			MapObject goToObj = GetGoToObj();
			vars.Set("obj", goToObj);
			vars.Set("goto_target", goToObj);
			vars.Set("battle", this);
			if (escaped_nobles != null && Action.get_prisoners_text != null)
			{
				vars.Set("escaped_marshals", Action.get_prisoners_text(escaped_nobles));
			}
			if (winner != -1)
			{
				vars.Set("winner", winner);
				vars.Set("winner_kingdom", GetWinnerKingdom());
				vars.Set("loser_kingdom", GetLoserKingdom());
			}
			else if (!IsFinishing() && settlement != null)
			{
				vars.Set("was_occupied", settlement.IsOccupied());
				vars.Set("settlement_controller", settlement.keep_effects?.GetController());
				vars.Set("settlement_owner", settlement.GetKingdom());
			}
			Army army = GetArmy(0);
			if (army != null)
			{
				vars.Set("attacker_leader", army.leader);
			}
			Army army2 = GetArmy(1);
			if (army2 != null)
			{
				vars.Set("defender_leader", army2.leader);
			}
			Army supporter = GetSupporter(0);
			if (supporter != null)
			{
				vars.Set("attacker_support_leader", supporter.leader);
			}
			Army supporter2 = GetSupporter(1);
			if (supporter2 != null)
			{
				vars.Set("defender_support_leader", supporter2.leader);
			}
			string text = type.ToString();
			if (realm_id == 0)
			{
				text = "FreeLand";
			}
			else if (is_siege && settlement != null && settlement.keep_effects.CanBeTakenOver() && settlement.type == "Keep")
			{
				text += "Alt";
			}
			vars.Set("type_key", text);
			vars.Set("BATTLE", "Battle.caption." + text);
			Realm realm = game.GetRealm(realm_id);
			if (realm != null)
			{
				vars.Set("realm", realm);
			}
			Kingdom kingdom = attacker_kingdom;
			if (kingdom != null)
			{
				vars.Set("attacker_kingdom", kingdom);
			}
			Kingdom kingdom2 = defender_kingdom;
			if (kingdom2 != null)
			{
				vars.Set("defender_kingdom", kingdom2);
			}
			Object obj = cancelled_by;
			if (obj != null)
			{
				vars.Set("cancelled_by", obj);
			}
			if (send_state)
			{
				SendVars();
			}
		}
	}

	private void SendVars()
	{
		NotifyListeners("vars_changed");
		SendState<BattleVarsState>();
	}

	public MapObject GetGoToObj()
	{
		if (settlement != null)
		{
			return settlement;
		}
		Army army = GetArmy(0);
		if (army != null)
		{
			return army;
		}
		Army army2 = GetArmy(1);
		if (army2 != null)
		{
			return army2;
		}
		return null;
	}

	private void SetRealm(Realm realm)
	{
		if (realm == null)
		{
			realm_id = game.GetNearbyRealm(position);
		}
		else
		{
			realm_id = realm.id;
		}
		if (realm_id == 0)
		{
			game.Warning($"NO REALM!!!: {this}");
		}
	}

	private Battle(Type type, Army attacker, Army defender, bool battle_map_only = false)
		: base(defender.game, defender.position, defender.kingdom_id)
	{
		this.battle_map_only = battle_map_only;
		if (type == Type.OpenField || type == Type.Naval)
		{
			position = (attacker.position + defender.position) * 0.5f;
		}
		else
		{
			Warning("Battle Constructor type mismatch");
		}
		CalcDirection(attacker.position, defender.position);
		attacker.ClearInteractors();
		defender.ClearInteractors();
		this.attacker = attacker;
		this.defender = defender;
		SetAttackerKingdom(attacker);
		SetDefenderKingdom(defender);
		Realm realm = game.GetRealm(position);
		if (type == Type.OpenField && realm != null && realm.IsSeaRealm())
		{
			type = Type.Naval;
		}
		SetRealm(realm);
		this.type = type;
		def = game.defs.Get<Def>(type.ToString());
		attackers.Add(attacker);
		defenders.Add(defender);
		attacker.SetBattle(this, set_reinforcement: false, send_state: false);
		defender.SetBattle(this, set_reinforcement: false, send_state: false);
		attacker.battle_side = 0;
		defender.battle_side = 1;
		simulation = new BattleSimulation(this);
		if (def.preparation_time_base > 0f)
		{
			SetStage(Stage.Preparing);
		}
		else
		{
			SetStage(Stage.Ongoing);
		}
		bv_attackers = attackers.Count;
		bv_defenders = defenders.Count;
		attacker.Stop();
		defender.Stop();
		attacker_kingdom?.NotifyListeners("new_open_field_battle", this);
		defender_kingdom?.NotifyListeners("new_open_field_battle", this);
	}

	private Battle(Type type, Army attacker, Settlement settlement, bool battle_map_only = false)
		: base(settlement.game, settlement.position, settlement.kingdom_id)
	{
		this.battle_map_only = battle_map_only;
		this.type = type;
		if (type == Type.OpenField || type == Type.Naval)
		{
			Warning("Battle Constructor type mismatch");
		}
		CalcDirection(attacker.position, settlement.position);
		def = game.defs.Get<Def>(type.ToString());
		this.attacker = attacker;
		defender = settlement;
		SetRealm(settlement?.GetRealm());
		this.settlement = settlement;
		SetAttackerKingdom(attacker);
		SetDefenderKingdom(settlement);
		attackers.Add(attacker);
		attacker.battle = this;
		if (settlement is Castle { army: not null } castle)
		{
			defenders.Add(castle.army);
			castle.army.battle_side = 1;
			castle.army.battle = this;
		}
		settlement.SetBattle(this);
		attacker.battle_side = 0;
		simulation = new BattleSimulation(this);
		if (def.preparation_time_base > 0f)
		{
			SetStage(Stage.Preparing);
		}
		else
		{
			SetStage(Stage.Ongoing);
		}
		if (type == Type.Siege)
		{
			StartSiege();
		}
		if (!battle_map_only && type == Type.Plunder)
		{
			if (settlement.worker_squads > 0 || settlement.town_guard_squads > 0 || settlement.levy_squads > 0)
			{
				PlunderInterrupt(instant: true);
			}
			else
			{
				RefreshPlunderProgress();
				Timer.Start(this, "plunder_interrupt", game.Random(5, 15));
			}
		}
		bv_attackers = attackers.Count;
		bv_defenders = defenders.Count;
		Realm realm = game.GetRealm(realm_id);
		if (settlement == realm.castle)
		{
			realm?.NotifyListeners("battle_started", this);
		}
		attacker_kingdom?.NotifyListeners("new_settlement_battle_attacker", this);
		defender_kingdom?.NotifyListeners("new_settlement_battle_defender", this);
	}

	private void CalcDirection(Point attacker_pos, Point defender_pos)
	{
		direction = defender_pos - attacker_pos;
		if (direction.Normalize() < 0.01f)
		{
			direction = Point.RandomOnUnitCircle(game);
		}
		heading = direction.Heading();
	}

	public void AddIntendedReinforcement(Army army, bool force = false)
	{
		if (!IsAuthority() || army == null || game.IsMultiplayer())
		{
			return;
		}
		int joinSide = GetJoinSide(army);
		if (!ValidReinforcement(army, joinSide))
		{
			return;
		}
		if (intended_reinforcements == null)
		{
			intended_reinforcements = new List<Army>();
		}
		bool flag = intended_reinforcements.Contains(army);
		if (flag && !force)
		{
			return;
		}
		if (!flag)
		{
			intended_reinforcements.Add(army);
		}
		army.last_intended_battle = this;
		float num = CalcReinforcementTime(army);
		int num2 = FreeReinforcementSlot(joinSide);
		if (num2 != -1)
		{
			int num3 = num2 + 2;
			Reinforcement reinforcement = reinforcements[num2];
			Reinforcement reinforcement2 = null;
			if (num2 < 2)
			{
				reinforcement2 = reinforcements[num3];
			}
			if (reinforcement2?.army != army && (reinforcement.army == null || (reinforcement.army != army && reinforcement.estimate_time > num)))
			{
				SetReinforcements(army, num2, num);
			}
			else if (num2 < 2 && reinforcement.army != army && (reinforcement2.army == null || (reinforcement2.army != army && reinforcement2.estimate_time > num)))
			{
				SetReinforcements(army, num3, num);
			}
		}
	}

	public int FreeReinforcementSlot(int battle_side)
	{
		List<Army> armies = GetArmies(battle_side);
		if (armies.Count == 0)
		{
			return battle_side;
		}
		if (armies.Count == 1)
		{
			return battle_side + 2;
		}
		return -1;
	}

	public void GarbageCollectIntendedReinforcement(Army army)
	{
		bool flag = true;
		if (army != null && army.IsValid() && army.movement?.ResolveFinalDestination(army.movement.path?.dst_obj) == this && ValidReinforcement(army, GetJoinSide(army)))
		{
			flag = false;
		}
		if (!flag)
		{
			return;
		}
		intended_reinforcements.Remove(army);
		for (int i = 0; i < reinforcements.Length; i++)
		{
			if (reinforcements[i].army == army && army.battle != this)
			{
				SetReinforcements(null, i, -1f);
				ReplaceValidReinforcement(i % 2, i);
			}
		}
		SendState<IntendedReinforcementsState>();
	}

	private void GarbageCollectIntendedReinforcements()
	{
		if (intended_reinforcements != null)
		{
			for (int num = intended_reinforcements.Count - 1; num >= 0; num--)
			{
				Army army = intended_reinforcements[num];
				GarbageCollectIntendedReinforcement(army);
			}
		}
	}

	public Reinforcement FindReinforcement(Army army)
	{
		for (int i = 0; i < reinforcements.Length; i++)
		{
			if (reinforcements[i]?.army == army)
			{
				return reinforcements[i];
			}
		}
		return null;
	}

	public List<Character> FindValidReinforcements(int battle_side)
	{
		if (game.IsMultiplayer())
		{
			return null;
		}
		battle_side %= 2;
		if (GetSupporter(battle_side) != null)
		{
			return null;
		}
		Realm realm = GetRealm();
		if (realm == null)
		{
			return null;
		}
		List<Character> list = new List<Character>();
		AddValidReinforcements(battle_side, list, intended_reinforcements);
		AddValidReinforcements(battle_side, list, realm);
		for (int i = 0; i < realm.neighbors.Count; i++)
		{
			Realm realm2 = realm.neighbors[i];
			AddValidReinforcements(battle_side, list, realm2);
		}
		AddValidReinforcements(battle_side, list, GetSideKingdom(battle_side));
		return list;
	}

	public bool ValidReinforcement(Army army, int battle_side)
	{
		if (battle_side < 0 || battle_side > 1)
		{
			return false;
		}
		if (army.battle != null)
		{
			return false;
		}
		if (army.leader == null)
		{
			return false;
		}
		if (army.IsFleeing())
		{
			return false;
		}
		if (army.movement?.ResolveFinalDestination(army.movement.path?.dst_obj) != this && army.position.SqrDist(position) > def.max_reinforcement_distance * def.max_reinforcement_distance)
		{
			return false;
		}
		if (GetJoinSide(army) != battle_side)
		{
			return false;
		}
		Kingdom sideKingdom = GetSideKingdom(battle_side);
		if (army.IsOwnStance(sideKingdom))
		{
			return true;
		}
		if (army.IsAlly(sideKingdom))
		{
			return true;
		}
		return false;
	}

	private void AddValidReinforcements(int battle_side, List<Character> res, Realm realm)
	{
		if (realm == null)
		{
			return;
		}
		for (int i = 0; i < realm.armies.Count; i++)
		{
			Army army = realm.armies[i];
			if (!res.Contains(army.leader) && ValidReinforcement(army, battle_side))
			{
				res.Add(army.leader);
			}
		}
	}

	private void AddValidReinforcements(int battle_side, List<Character> res, List<Army> intenders)
	{
		if (intenders == null)
		{
			return;
		}
		for (int i = 0; i < intenders.Count; i++)
		{
			Army army = intenders[i];
			if (!res.Contains(army.leader) && ValidReinforcement(army, battle_side))
			{
				res.Add(army.leader);
			}
		}
	}

	private void AddValidReinforcements(int battle_side, List<Character> res, Kingdom k)
	{
		for (int i = 0; i < k.armies.Count; i++)
		{
			Army army = k.armies[i];
			if (!res.Contains(army.leader) && ValidReinforcement(army, battle_side))
			{
				res.Add(army.leader);
			}
		}
	}

	private void StartReinforcementTimers(bool force = false)
	{
		for (int i = 0; i < reinforcements.Length; i++)
		{
			Reinforcement reinforcement = reinforcements[i];
			if (reinforcement.army != null && (reinforcement.timer == null || force))
			{
				reinforcement.timer = new ComputableValue(reinforcement.estimate_time, -1f, batte_view_game, 0f, reinforcement.estimate_time);
			}
		}
	}

	public void CheckReinforcementTimers()
	{
		if (battle_map_finished)
		{
			return;
		}
		for (int i = 0; i < reinforcements.Length; i++)
		{
			Reinforcement reinforcement = reinforcements[i];
			ComputableValue timer = reinforcement.timer;
			if (timer != null)
			{
				float num = timer.Get();
				if (reinforcement.army != null && num <= 0f && GetSupporter(i) == null)
				{
					Join(reinforcement.army, from_reinf: true);
				}
			}
		}
	}

	public void ReplaceValidReinforcement(int battle_side, int reinf_id)
	{
		for (int i = 0; i < intended_reinforcements.Count; i++)
		{
			Army army = intended_reinforcements[i];
			bool flag = false;
			for (int j = 0; j < reinforcements.Length; j++)
			{
				if (reinforcements[j].army == army)
				{
					flag = true;
					break;
				}
			}
			if (!flag && ValidReinforcement(army, battle_side))
			{
				SetReinforcements(army, reinf_id, CalcReinforcementTime(army), force: true);
				break;
			}
		}
	}

	public void SetReinforcements(Army army, int reinf_id, float estimate_time, bool force = false, bool send_state = true, bool manual = false)
	{
		if (game.IsMultiplayer())
		{
			return;
		}
		switch (reinf_id)
		{
		case 2:
		case 3:
			if (army != null && GetSupporter(reinf_id) != null)
			{
				return;
			}
			break;
		default:
			return;
		case 0:
		case 1:
			break;
		}
		Reinforcement reinforcement = reinforcements[reinf_id];
		if ((reinforcement.army == army && !force) || (army != null && !manual && reinforcement.was_manually_set))
		{
			return;
		}
		if (army != null)
		{
			for (int i = 0; i < reinforcements.Length; i++)
			{
				Reinforcement reinforcement2 = reinforcements[i];
				if (reinforcement2.army == army)
				{
					reinforcement2.army = null;
					reinforcement2.was_manually_set = false;
					reinforcement2.estimate_time = -1f;
					reinforcement2.timer = null;
				}
			}
		}
		reinforcement.army = army;
		reinforcement.was_manually_set = manual;
		reinforcement.estimate_time = estimate_time;
		reinforcement.timer = null;
		if (batte_view_game != null && stage == Stage.Ongoing)
		{
			StartReinforcementTimers();
		}
		if (IsAuthority() && send_state)
		{
			SendState<ReinforcementsState>();
		}
		NotifyListeners("reinforcements_changed");
		if (simulation != null)
		{
			simulation.totals_dirty = true;
			simulation.CalcTotals();
			NotifyListeners("changed");
		}
	}

	public float CalcReinforcementTime(Army army)
	{
		if (army == null)
		{
			return 0f;
		}
		float num = army.position.Dist(position);
		float speed = army.movement.speed;
		return num / speed * def.reinforcement_estimation_arrive_mod;
	}

	public override void OnTimer(Timer timer)
	{
		if (timer.name == "plunder_interrupt")
		{
			float num = (float)GetVar("province_guards") * def.field.GetFloat("interrupt_province_ratio") + (float)GetVar("settlement_guards") * def.field.GetFloat("interrupt_settlement_ratio");
			num *= 1f + defender_kingdom.GetStat(Stats.ks_pillage_interrupt_chance_bonus) / 100f;
			if ((float)game.Random(0, 100) < num)
			{
				PlunderInterrupt();
			}
		}
		else
		{
			base.OnTimer(timer);
		}
	}

	public List<BattleBonus.StatModifier.Def> UnitBonuses(List<BattleBonus.Def> source, Unit.Type unit_type)
	{
		List<BattleBonus.StatModifier.Def> list = new List<BattleBonus.StatModifier.Def>();
		for (int i = 0; i < source.Count; i++)
		{
			for (int j = 0; j < source[i].mods.Count; j++)
			{
				if (source[i].mods[j].unit_type == unit_type)
				{
					list.Add(source[i].mods[j]);
				}
			}
		}
		return list;
	}

	public List<BattleBonus.StatModifier.Def> UnitBonuses(List<BattleBonus.Def> source, Unit.Def unit_def)
	{
		List<BattleBonus.StatModifier.Def> list = new List<BattleBonus.StatModifier.Def>();
		for (int i = 0; i < source.Count; i++)
		{
			for (int j = 0; j < source[i].mods.Count; j++)
			{
				BattleBonus.StatModifier.Def def = source[i].mods[j];
				if (def.unit_def == unit_def || def.unit_def.field == unit_def.field.based_on)
				{
					list.Add(source[i].mods[j]);
				}
			}
		}
		return list;
	}

	public void GetUnitMods(int side, Unit.Def unit_def, string key, out float add, out float perc, out float base_add)
	{
		add = (perc = (base_add = 0f));
		if (batte_view_game != null || battle_bonuses == null)
		{
			return;
		}
		for (int i = 0; i < battle_bonuses.Count; i++)
		{
			BattleBonus.Def def = battle_bonuses[i];
			for (int j = 0; j < def.mods.Count; j++)
			{
				BattleBonus.StatModifier.Def def2 = def.mods[j];
				if (def2.Validate(side, unit_def, key))
				{
					Stat.Modifier.Apply(def2.value, def2.type, ref add, ref perc, ref base_add);
				}
			}
		}
	}

	public float GetUnitMod(float base_val, Unit.Def unit_def, string key, int battle_side)
	{
		GetUnitMods(battle_side, unit_def, key, out var add, out var perc, out var base_add);
		return (base_val + base_add) * (1f + perc * 0.01f) + add;
	}

	public float ArmyStrength()
	{
		float num = 0f;
		float siege_penalty = 0f;
		int num2 = 0;
		if (simulation != null)
		{
			num2 = simulation.attacker_squads.Count;
		}
		if (num2 > def.efficient_siege_squads)
		{
			siege_penalty = (float)(num2 - def.efficient_siege_squads) * def.penalty_per_squad / 100f;
		}
		for (int i = 0; i < attackers.Count; i++)
		{
			num += attackers[i].GetSiegeStrength(siege_penalty);
		}
		return num;
	}

	public Point CalcArmyOffset(Army army, int side, bool supporter, float range)
	{
		Point point = direction * range;
		if (side == 0)
		{
			point = -point;
		}
		int num = CalcHeadingOfs(army, side, supporter);
		if (num != 0)
		{
			point = point.GetRotated((float)num * 60f);
		}
		return point;
	}

	public int CalcHeadingOfs(Army army, int side, bool supporter)
	{
		if (side == 0)
		{
			if (!supporter)
			{
				return 0;
			}
			return 1;
		}
		Army army2 = GetArmy(1);
		if (army == null)
		{
			if (army2 != null)
			{
				return -1;
			}
			return 0;
		}
		if (!supporter)
		{
			return 0;
		}
		if (!HasSettlementDefenders())
		{
			if (army2 != null && army2 != army)
			{
				return 1;
			}
			return 0;
		}
		if (army2 != null && army2 != army)
		{
			return 1;
		}
		return -1;
	}

	public bool HasSettlementDefenders()
	{
		if (settlement?.garrison?.units == null)
		{
			return false;
		}
		return settlement.garrison.units.Count > 0;
	}

	public bool IsKingdomParticipant(Kingdom _kingdom)
	{
		if (_kingdom == null)
		{
			return false;
		}
		if (attacker_kingdom == _kingdom || defender_kingdom == _kingdom)
		{
			return true;
		}
		for (int i = 0; i < attackers.Count; i++)
		{
			Army army = attackers[i];
			if (army != null && army.kingdom_id == _kingdom.id)
			{
				return true;
			}
		}
		for (int j = 0; j < defenders.Count; j++)
		{
			Army army2 = defenders[j];
			if (army2 != null && army2.kingdom_id == _kingdom.id)
			{
				return true;
			}
		}
		return false;
	}

	public void AdjustArmyPosition(Army army)
	{
		if (!IsAuthority() || IsFinishing())
		{
			return;
		}
		if (army == null)
		{
			game.Warning("Missing army in " + this);
			return;
		}
		if (army.battle == null)
		{
			game.Warning(string.Concat(army, " has no battle while being adjusted in ", this));
			return;
		}
		float range = def.fight_range;
		switch (type)
		{
		case Type.OpenField:
			if (stage == Stage.Preparing)
			{
				range = def.tents_range;
			}
			break;
		case Type.Siege:
		case Type.Assault:
		case Type.BreakSiege:
			range = def.trebuchet_range;
			break;
		}
		Point point = CalcArmyOffset(army, army.battle_side, army.is_supporter, range);
		if (!(point == Point.Zero))
		{
			PPos pos = position + point;
			if (type != Type.Naval || game.path_finding.data.GetNode(pos).ocean)
			{
				army.Teleport(position + point);
				army.SendState<PositionState>();
			}
		}
	}

	public void BreakSiege(BreakSiegeFrom break_siege_from, bool send_state = true)
	{
		StopSiegeComponents();
		this.break_siege_from = break_siege_from;
		SetType(Type.BreakSiege, send_state);
		SetStage(Stage.Preparing);
		NotifyListeners("break_siege");
		defender.NotifyListeners("break_siege_defender");
	}

	public void ResumeSiege()
	{
		SetType(Type.Siege);
		InitSiegeComponents();
		SetStage(Stage.Ongoing);
		SetBattleViewVictoryReason(VictoryReason.None, -1);
	}

	public void ResumePlunder(bool send_state = true)
	{
		SetType(Type.Plunder, send_state);
		SetStage(Stage.Ongoing, send_state);
		SetBattleViewVictoryReason(VictoryReason.None, -1);
		RefreshPlunderProgress();
		CheckArmies();
	}

	public void PlunderInterrupt(bool instant = false, bool send_state = true)
	{
		StopSiegeComponents();
		SetType(Type.PlunderInterrupt, send_state);
		if (instant)
		{
			SetStage(Stage.Ongoing);
		}
		else
		{
			SetStage(Stage.Preparing);
		}
		RefreshPlunderProgress();
	}

	public void SetType(Type type, bool send_state = true)
	{
		if (type != this.type)
		{
			Type type2 = this.type;
			this.type = type;
			RefreshDef();
			if (!battle_map_only)
			{
				AddTemporaryDefenders();
			}
			FillVars();
			if (simulation != null)
			{
				simulation.ForceCalcTotals();
			}
			if (send_state)
			{
				SendState<InitState>();
			}
			NotifyListeners("type_changed", type2);
			settlement?.GetRealm()?.NotifyListeners("battle_type_changed", type2);
		}
	}

	private int CalcTownGuards()
	{
		if (settlement.type == "Castle")
		{
			return (int)settlement.GetRealm().income.Get(ResourceType.TownGuards);
		}
		return 0;
	}

	private int CalcLevy()
	{
		return (int)settlement.GetResources().Get(ResourceType.Levy);
	}

	private int CalcMilitia()
	{
		int num = 0;
		int num2 = 0;
		float num3 = def.militia_alive_workers_mod;
		if (settlement.type == "Castle")
		{
			Realm realm = settlement.GetRealm();
			num = realm.castle.population.workers + realm.castle.population.rebels;
			num2 = realm.castle.population.Slots(Population.Type.Worker, check_up_to_date: false);
		}
		else
		{
			num = (int)settlement.GetResources().Get(ResourceType.WorkerSlots);
			if (settlement.type != "Keep")
			{
				num3 = def.militia_alive_workers_village_mod;
			}
		}
		return (int)((float)num * num3 + (float)num2 * def.militia_max_population_mod + def.militia_base);
	}

	private void AddTemporaryDefenders()
	{
		if (IsAuthority() && !added_guards && settlement != null)
		{
			added_guards = true;
			settlement.CacheTempDefenderStats();
			AddTownGuards(settlement.town_guard_squads);
			AddLevy(settlement.levy_squads);
			AddMilitia(settlement.worker_squads);
			simulation.PositionSettlementDefenders();
			settlement.SendState<Settlement.GarrisonUnitsState>();
			SendState<SimSquadsState>();
		}
	}

	private bool AddTemporaryDefender(Unit.Def unit_def, float damage = 0f)
	{
		if (settlement.garrison.units.Count >= 18)
		{
			return false;
		}
		Unit unit = settlement.garrison.AddUnit(unit_def, mercenary: false, send_state: false, check_slots: false);
		unit.SetDamage(damage, send_state: false);
		BattleSimulation.Squad squad = new BattleSimulation.Squad(morale: settlement.GetRealm().castle.morale.GetMorale(), u: unit, garrison: settlement.garrison, simulation: simulation)
		{
			max_damage = 1f - damage
		};
		unit.simulation.temporary = true;
		simulation.AddSquad(squad);
		return true;
	}

	private void AddTownGuards(int squads_to_spawn)
	{
		if (squads_to_spawn <= 0)
		{
			return;
		}
		Unit.Def def = settlement.garrison.GuardDef();
		if (def == null)
		{
			return;
		}
		for (int i = 0; i < squads_to_spawn; i++)
		{
			if (!AddTemporaryDefender(def))
			{
				break;
			}
		}
	}

	private void AddLevy(int squads_to_spawn)
	{
		if (squads_to_spawn <= 0)
		{
			return;
		}
		Unit.Def def = settlement.garrison.LevyDefenseDef();
		Unit.Def def2 = settlement.garrison.LevyInfantryDef();
		Unit.Def def3 = settlement.garrison.LevyRangedDef();
		if (def == null && def2 == null && def3 == null)
		{
			return;
		}
		while (squads_to_spawn > 0)
		{
			if (def != null)
			{
				for (int i = 0; i < this.def.defense_levy; i++)
				{
					if (!AddTemporaryDefender(def))
					{
						return;
					}
					squads_to_spawn--;
					if (squads_to_spawn == 0)
					{
						return;
					}
				}
			}
			if (def2 != null)
			{
				for (int j = 0; j < this.def.infantry_levy; j++)
				{
					if (!AddTemporaryDefender(def2))
					{
						return;
					}
					squads_to_spawn--;
					if (squads_to_spawn == 0)
					{
						return;
					}
				}
			}
			if (def3 == null)
			{
				continue;
			}
			for (int k = 0; k < this.def.ranged_levy; k++)
			{
				if (!AddTemporaryDefender(def3))
				{
					return;
				}
				squads_to_spawn--;
				if (squads_to_spawn == 0)
				{
					return;
				}
			}
		}
	}

	private void AddMilitia(int squads_to_spawn)
	{
		if (squads_to_spawn <= 0)
		{
			return;
		}
		Unit.Def def = settlement.garrison.MilitiaDef();
		if (def == null)
		{
			return;
		}
		squads.Get(1);
		for (int i = 0; i < squads_to_spawn; i++)
		{
			if (!AddTemporaryDefender(def))
			{
				break;
			}
		}
	}

	public void RefreshDef()
	{
		def = game.defs.Get<Def>(type.ToString());
	}

	public bool Assault()
	{
		if (!CanAssault())
		{
			return false;
		}
		StopSiegeComponents();
		SetType(Type.Assault);
		SetStage(Stage.Preparing);
		NotifyListeners("assault");
		return true;
	}

	public void AssaultGate()
	{
		SetAssaultGate(val: true);
		Assault();
	}

	public bool CanAssault()
	{
		if (battle_map_only)
		{
			return true;
		}
		if (assault_gate_action_succeeded)
		{
			return true;
		}
		if (settlement != null && is_siege && (resilience <= initial_resilience_pre_condition * 0.5f || siege_defense <= initial_siege_defense_pre_condition * 0.5f))
		{
			if (settlement.keep_effects != null)
			{
				return settlement.keep_effects.CanBeAssaulted();
			}
			return true;
		}
		return false;
	}

	public bool CanRetreat(Kingdom kingdom)
	{
		if (GetJoinSide(kingdom, check_is_in_battle: false) == 0)
		{
			return true;
		}
		if (resilience > 0f)
		{
			return true;
		}
		if (!is_siege)
		{
			return true;
		}
		if (!(settlement is Castle))
		{
			return false;
		}
		for (int i = 0; i < defenders.Count; i++)
		{
			Army army = defenders[i];
			if (army.castle == null && army.GetKingdom() == kingdom)
			{
				return true;
			}
		}
		return false;
	}

	public bool CanRetreatInside(int battle_side)
	{
		if (battle_side == 0)
		{
			return true;
		}
		if (resilience > 0f)
		{
			return true;
		}
		if (!is_siege)
		{
			return true;
		}
		_ = settlement is Castle;
		return false;
	}

	private void ConsumeSupplies()
	{
		Vars vars = new Vars(this);
		for (int i = 0; i < 2; i++)
		{
			BattleSimulation.Totals totals = simulation.GetTotals(i);
			BattleSimulation.Totals totals2 = simulation.GetTotals(1 - i);
			List<Army> armies = GetArmies(i);
			vars.Set("our_manpower", totals.initial_count);
			vars.Set("their_manpower", totals2.initial_count);
			for (int j = 0; j < armies.Count; j++)
			{
				Army army = armies[j];
				vars.Set("leader", army.leader);
				if (!army.IsCrusadeArmy() && army.rebel == null && !army.IsMercenary())
				{
					army.AddSupplies(0f - def.supplies_consumed_at_end.Float(vars));
				}
			}
		}
	}

	private void UpdateCastleFoodRate()
	{
		if (IsAuthority() && !battle_map_only && settlement != null)
		{
			Realm realm = GetRealm();
			if (settlement_food_copy == null)
			{
				float num = realm.castle.GetMaxFoodStorage() * settlement.def.food_copy_mod;
				float val = (settlement.def.take_max_food ? num : (realm.castle.food_storage * settlement.def.food_copy_mod));
				settlement_food_copy = new ComputableValue(val, 0f, game, 0f, num);
			}
			float num2 = 0f;
			if (settlement.coastal)
			{
				num2 += GetRealm().GetStat(Stats.rs_coastal_town_food_loss_reduction_perc);
			}
			float num3 = 0f;
			if (settlement.garrison != null)
			{
				num3 += (float)settlement.garrison.GetManPower();
			}
			if (defenders != null && defenders.Count > 0 && defenders[0] != null)
			{
				num3 += (float)defenders[0].GetManPower();
			}
			Army.Def def = game.defs.GetBase<Army.Def>();
			float num4 = def.supplies_min_mod_per_troops + (def.supplies_max_mod_per_troops - def.supplies_min_mod_per_troops) * (Math.Max(Math.Min(num3, def.supplies_troop_max), def.supplies_troop_min) - def.supplies_troop_min) / (def.supplies_troop_max - def.supplies_troop_min);
			float num5 = (0f - settlement.def.siege_food_penalty) * num4 * (1f - num2 / 100f);
			num5 /= 5f;
			if (num5 != settlement_food_copy.GetRate())
			{
				settlement_food_copy.SetRate(num5);
				SendState<FoodStorageState>();
			}
		}
	}

	public void SetFood(float val)
	{
		settlement_food_copy.Set(val);
		SendState<FoodStorageState>();
	}

	public void StartSiege()
	{
		InitSiegeComponents();
		Realm realm = settlement.GetRealm();
		initial_siege_defense = realm.GetStat(Stats.rs_siege_defense);
		initial_resilience = realm.GetStat(Stats.rs_siege_resilience);
		if (settlement is Castle castle)
		{
			if (castle.structure_build != null)
			{
				castle.structure_build.CancelBuild();
			}
			castle.sacking_comp?.Reset();
			Garrison garrison = castle.garrison;
			if (garrison != null)
			{
				float morale = 0f;
				if (castle.morale != null)
				{
					morale = castle.morale.GetMorale();
				}
				for (int i = 0; i < garrison.units.Count; i++)
				{
					simulation?.AddSquad(new BattleSimulation.Squad(garrison.units[i], castle.garrison, simulation, 1, morale));
					garrison.units[i].simulation.army = defender as Army;
				}
			}
		}
		else
		{
			initial_resilience *= def.keep_initial_resil_mod;
			initial_siege_defense *= def.keep_initial_siege_defense_mod;
		}
		if (realm.HasTag("levy_join_siege"))
		{
			int num = CalcTownGuards();
			if (num > 0)
			{
				initial_resilience += (float)num * def.levy_resilience;
			}
		}
		if (settlement.GetRealm().IsOccupied() && realm.GetKingdom().IsOwnStance(attacker))
		{
			if (settlement.GetRealm().pop_majority.kingdom == attacker.GetKingdom())
			{
				initial_siege_defense *= (100f + realm.GetStat(Stats.rs_siege_defense_own_attacker_with_pop_majority)) / 100f;
			}
			else
			{
				initial_siege_defense *= (100f + realm.GetStat(Stats.rs_siege_defense_own_attacker)) / 100f;
			}
		}
		initial_resilience_pre_condition = initial_resilience;
		initial_siege_defense_pre_condition = initial_siege_defense;
		initial_resilience = Math.Max(1f, initial_resilience * settlement.keep_effects.resilience_condition.Get() / 100f);
		initial_siege_defense = Math.Max(1f, initial_siege_defense * settlement.keep_effects.siege_defense_condition.Get() / 100f);
		settlement.keep_effects.resilience_condition.SetRate(0f);
		settlement.keep_effects.siege_defense_condition.SetRate(0f);
		settlement.SendState<Settlement.SiegeStatsState>();
		resilience = initial_resilience;
		siege_defense = initial_siege_defense;
		Rebel rebel = attackers[0].rebel;
		if (rebel != null && rebel.IsLoyalist())
		{
			rebel.AddRelLoyal("siege_initiated", settlement.GetKingdom());
		}
		SendState<SiegeStatsState>();
		UpdateCastleFoodRate();
		AddTemporaryDefenders();
	}

	public void StopSiegeComponents()
	{
		resil_drop_component?.Stop();
		siege_defense_drop_component?.Stop();
	}

	public int GetJoinSide(Object obj, bool check_is_in_battle = true)
	{
		if (obj == null)
		{
			return -1;
		}
		if (IsFinishing())
		{
			return -1;
		}
		Army army = obj as Army;
		if (army?.battle != null && check_is_in_battle)
		{
			return -1;
		}
		bool num = obj.IsEnemy(attacker);
		bool flag = obj.IsEnemy(defender);
		int result;
		Object obj2;
		List<Army> list;
		if (num)
		{
			if (flag)
			{
				if (army?.mercenary?.mission_def != null && !army.mercenary.mission_def.can_attack_rebels && attacker.rebel != null)
				{
					return -1;
				}
				if (type == Type.Siege)
				{
					return 2;
				}
				if (is_plunder && defenders.Count == 0)
				{
					return 2;
				}
				return -1;
			}
			result = 1;
			obj2 = defender;
			list = defenders;
		}
		else
		{
			if (!flag)
			{
				return -1;
			}
			result = 0;
			obj2 = attacker;
			list = attackers;
		}
		if (list.Count < 2)
		{
			return result;
		}
		Army army2 = list[1];
		if (army != null && army2 != null && obj2 != null && (!obj.IsOwnStance(army2) || army2.IsHiredMercenary()) && obj.IsOwnStance(obj2) && !army.IsHiredMercenary())
		{
			Kingdom kingdom = obj.GetKingdom();
			if (kingdom != null && kingdom.is_player)
			{
				return result;
			}
			return -1;
		}
		return -1;
	}

	public bool CanJoin(Army army)
	{
		if (GetJoinSide(army) >= 0)
		{
			return true;
		}
		return false;
	}

	public bool CanBreakSiege(Army army)
	{
		if (GetJoinSide(army) == 1 && stage == Stage.Ongoing)
		{
			return type == Type.Siege;
		}
		return false;
	}

	public void CalcStartingTroops()
	{
		for (int i = 0; i < 2; i++)
		{
			starting_squads[i] = 0;
			starting_troops[i] = 0f;
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				Army army = armies[j];
				AddStartingUnits(army, i);
			}
		}
	}

	public void AddStartingUnits(Army army, int side)
	{
		starting_squads[side] += army.units.Count;
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit != null)
			{
				starting_troops[side] += unit.GetVar("num_troops");
			}
		}
	}

	public void AddTroopsKilled(Army army, int side)
	{
		if (army == null || side < 0)
		{
			return;
		}
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit?.simulation != null)
			{
				troops_killed[side] += (unit.damage - unit.simulation.initial_damage) * (float)unit.simulation.max_troops;
			}
		}
	}

	private void CheckSwapArmies()
	{
		for (int i = 0; i < 2; i++)
		{
			List<Army> armies = GetArmies(i);
			if (armies.Count < 2)
			{
				continue;
			}
			Army army = armies[0];
			Army army2 = armies[1];
			if ((i != 1 || defender == army) && army.mercenary != null && army2.mercenary == null && army2.GetKingdom().IsOwnStance(army.GetKingdom()))
			{
				armies[0] = army2;
				armies[1] = army;
				if (i == 0)
				{
					attacker = army2;
				}
				else
				{
					defender = army2;
				}
			}
		}
	}

	public bool Join(Army army, bool from_reinf = false)
	{
		if (!IsAuthority())
		{
			return false;
		}
		if (!army.IsValid())
		{
			Error($"Destroyed army trying to enter battle {army}");
			return false;
		}
		army.ClearInteractors();
		int joinSide = GetJoinSide(army);
		if (joinSide < 0)
		{
			return false;
		}
		if (joinSide == 2)
		{
			List<Army> list = new List<Army>(attackers);
			Cancel(VictoryReason.CounterBattle, null, army);
			Battle battle = new Battle(Type.OpenField, army, list[0]);
			if (list.Count > 1)
			{
				battle.Join(list[1]);
			}
			return false;
		}
		if (batte_view_game == null && stage != Stage.EnteringBattle)
		{
			KickSupporter(joinSide);
		}
		switch (joinSide)
		{
		case 0:
			if (attackers.Count == 0)
			{
				return false;
			}
			if (!army.IsOwnStance(attacker) && attacker_support == null)
			{
				Kingdom kingdom4 = attacker_kingdom;
				Kingdom kingdom5 = army.GetKingdom();
				Kingdom kingdom6 = defender_kingdom;
				if (kingdom5.IsAlly(kingdom4))
				{
					kingdom4.AddRelationModifier(kingdom5, "rel_battle_join_ally", this);
				}
				else
				{
					kingdom4.AddRelationModifier(kingdom5, "rel_battle_join_neutral", this);
				}
				kingdom6.AddRelationModifier(kingdom5, "rel_battle_help_enemy", this);
				if (ai != null)
				{
					ai[0].SetSupporter(army.GetKingdom());
				}
			}
			bv_attackers++;
			attackers.Add(army);
			if (ai != null)
			{
				ai[0].AddArmy(army);
			}
			army.battle_side = 0;
			army.is_supporter = true;
			army.SetBattle(this);
			CheckSwapArmies();
			if (!from_reinf)
			{
				AdjustArmyPosition(army);
			}
			if (simulation != null)
			{
				simulation.OnArmyJoined(army);
			}
			CreateSquads();
			SendState<ArmiesState>();
			SendState<SimSquadsState>();
			FireEvent("armies_changed", army);
			army.leader?.NotifyListeners("statuses_changed");
			army.leader?.NotifyListeners("joined_battle");
			NotifyAllParticipants("battle_updated", this);
			AddStartingUnits(army, 0);
			FillVars();
			OnReinforcementAnalytics(joinSide, army);
			if (!from_reinf)
			{
				army.CancelReinforcement();
			}
			return true;
		case 1:
		{
			if (!army.IsOwnStance(defender) && defender_support == null)
			{
				Kingdom kingdom = defender_kingdom;
				Kingdom kingdom2 = army.GetKingdom();
				Kingdom kingdom3 = attacker_kingdom;
				if (kingdom2.IsAlly(kingdom))
				{
					kingdom.AddRelationModifier(kingdom2, "rel_battle_join_ally", this);
				}
				else
				{
					kingdom.AddRelationModifier(kingdom2, "rel_battle_join_neutral", this);
				}
				kingdom3.AddRelationModifier(kingdom2, "rel_battle_help_enemy", this);
				if (ai != null)
				{
					ai[1].SetSupporter(army.GetKingdom());
				}
			}
			else if (!army.IsOwnStance(defender) && ai != null)
			{
				ai[1].SetSecondSupporter(army.GetKingdom());
			}
			bool flag = defenders.Count == 0;
			defenders.Add(army);
			if (ai != null)
			{
				ai[1].AddArmy(army);
			}
			bv_defenders++;
			army.battle_side = 1;
			army.is_supporter = true;
			army.SetBattle(this);
			CheckSwapArmies();
			if (!from_reinf)
			{
				AdjustArmyPosition(army);
			}
			if (simulation != null)
			{
				simulation.OnArmyJoined(army);
			}
			CreateSquads();
			SendState<ArmiesState>();
			SendState<SimSquadsState>();
			FireEvent("armies_changed", army);
			if (stage == Stage.Ongoing && defenders.Count >= 1)
			{
				if (type == Type.Siege)
				{
					BreakSiege(BreakSiegeFrom.Outside);
				}
				else if (type == Type.Plunder || (type == Type.PlunderInterrupt && flag))
				{
					PlunderInterrupt();
				}
			}
			army.leader?.NotifyListeners("statuses_changed");
			army.leader?.NotifyListeners("joined_battle");
			NotifyAllParticipants("battle_updated", this);
			AddStartingUnits(army, 1);
			FillVars();
			OnReinforcementAnalytics(joinSide, army);
			if (!from_reinf)
			{
				army.CancelReinforcement();
			}
			return true;
		}
		default:
			return false;
		}
	}

	public void NotifyAllParticipants(string notification, object param = null)
	{
		attacker.NotifyListeners(notification, param);
		attacker_support?.NotifyListeners(notification, param);
		defender.NotifyListeners(notification, param);
		defender_support?.NotifyListeners(notification, param);
	}

	public void InitAI()
	{
		if (batte_view_game?.path_finding?.data != null)
		{
			power_grids = new SquadPowerGrid[2];
			for (int i = 0; i < 2; i++)
			{
				power_grids[i] = new SquadPowerGrid(20, 20, batte_view_game.path_finding.data.width, batte_view_game.path_finding.data.height, this, i);
				power_grids[i].MarkDirty();
			}
			ai = new BattleAI[2];
			ai[0] = new BattleAI(this, 0, null, attacker_kingdom, attacker_support?.GetKingdom(), attackers);
			if (garrison != null)
			{
				Kingdom first_army_kingdom = ((defenders.Count <= 0) ? null : defenders[0]?.GetKingdom());
				Kingdom second_army_kingdom = ((defenders.Count <= 1) ? null : defenders[1]?.GetKingdom());
				ai[1] = new BattleAI(this, 1, defender_kingdom, first_army_kingdom, second_army_kingdom, defenders, garrison);
			}
			else
			{
				ai[1] = new BattleAI(this, 1, null, defender_kingdom, defender_support?.GetKingdom(), defenders);
			}
			NotifyListeners("initialized_ai");
		}
	}

	private void RecalcSupporters()
	{
		for (int i = 0; i < 2; i++)
		{
			List<Army> armies = GetArmies(i);
			Army army = null;
			if (armies.Count > 0)
			{
				army = armies[0];
			}
			if (armies.Count > 1)
			{
				_ = armies[1];
			}
			if (i == 0)
			{
				attacker = army;
			}
			else if (settlement != null)
			{
				defender = settlement;
			}
			else
			{
				defender = army;
			}
			if (ai != null && ai[i] != null)
			{
				if (i == 0)
				{
					ai[i].SetUpAI(null, attacker_kingdom, attacker_support?.GetKingdom(), attackers);
				}
				else if (garrison != null)
				{
					Kingdom first_army_kingdom = ((defenders.Count <= 0) ? null : defenders[0]?.GetKingdom());
					Kingdom second_army_kingdom = ((defenders.Count <= 1) ? null : defenders[1]?.GetKingdom());
					ai[i].SetUpAI(defender_kingdom, first_army_kingdom, second_army_kingdom, defenders, garrison);
				}
				else
				{
					ai[i].SetUpAI(null, defender_kingdom, defender_support?.GetKingdom(), defenders);
				}
			}
		}
	}

	public void Leave(Army army, bool check_victory = false, bool is_restart = false, bool force_no_consequences = false)
	{
		if (!IsAuthority() || game.IsUnloadingMap())
		{
			return;
		}
		if (!is_restart)
		{
			if (force_no_consequences || type == Type.Siege || (army.IsCrusadeArmy() && game.religions.catholic.crusade.ended))
			{
				Aftermath(army, AftermathOutcome.Cancelled);
			}
			else
			{
				AddTroopsKilled(army, army.battle_side);
				AftermathRebel(army, won: false);
				AftermathMercenary(army, won: false);
				AftermathCrusader(army, won: false);
				Aftermath(army, AftermathOutcome.Defeated, add_experience: true);
			}
		}
		if (simulation != null)
		{
			simulation.OnArmyLeft(army);
		}
		for (int i = 0; i < 2; i++)
		{
			GetArmies(i).Remove(army);
		}
		RecalcSupporters();
		army.SetBattle(null);
		army.battle_side = -1;
		SendState<ArmiesState>();
		FireEvent("armies_changed", army);
		FillVars();
		if (check_victory && !is_restart)
		{
			CheckVictory();
		}
	}

	public bool LeadersKilled(int side)
	{
		List<Army> armies = GetArmies(side);
		if (armies == null || armies.Count < 1)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < armies.Count; i++)
		{
			armies[i].GetNobleDefeated(out var has_nobles, out var living_nobles);
			flag = flag || has_nobles;
			flag2 = flag2 || living_nobles;
		}
		if (flag)
		{
			return !flag2;
		}
		return false;
	}

	public bool ArmiesDefeated(int side)
	{
		List<Army> armies = GetArmies(side);
		switch (side)
		{
		case 0:
			if ((attacker == null || attacker.GetKingdom() != attacker_kingdom) && (attacker_support == null || attacker_support.GetKingdom() != attacker_kingdom))
			{
				return true;
			}
			break;
		case 1:
			if (settlement == null && (defender == null || defender.GetKingdom() != defender_kingdom) && (defender_support == null || defender_support.GetKingdom() != defender_kingdom))
			{
				return true;
			}
			break;
		}
		if ((is_siege || is_plunder) && (batte_view_game == null || armies.Count == 0))
		{
			if (settlement != null)
			{
				if (side == 1 && settlement.garrison != null)
				{
					for (int i = 0; i < settlement.garrison.units.Count; i++)
					{
						if (!settlement.garrison.units[i].IsDefeated())
						{
							return false;
						}
					}
				}
				else if (settlement.attacker_garrison != null)
				{
					for (int j = 0; j < settlement.attacker_garrison.units.Count; j++)
					{
						if (!settlement.attacker_garrison.units[j].IsDefeated())
						{
							return false;
						}
					}
				}
			}
			else
			{
				game.Warning(ToString() + ": settlement is null!");
			}
		}
		if (armies == null || armies.Count < 1)
		{
			return true;
		}
		for (int k = 0; k < armies.Count; k++)
		{
			if (!armies[k].IsDefeated())
			{
				return false;
			}
		}
		return true;
	}

	public Character BestAntiRebelLeader(int side)
	{
		float num = float.MinValue;
		Character result = null;
		List<Army> armies = GetArmies(side);
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			if (army?.leader != null)
			{
				float stat = army.leader.GetStat(Stats.cs_gold_from_rebellions_perc);
				if (stat > num)
				{
					num = stat;
					result = army.leader;
				}
			}
		}
		return result;
	}

	private bool ImprisonLeader(Character c, Kingdom by)
	{
		if (c == null)
		{
			return false;
		}
		Army army = c.GetArmy();
		bool flag = CheckVictory(apply_results: false);
		if (army == null || army.rebel == null || army.rebel.rebellion == null)
		{
			c.Imprison(by, recall: true, send_state: true, flag ? "battle" : "battle_ongoing");
			return c?.prison_kingdom == by && flag;
		}
		Rebellion rebellion = army.rebel.rebellion;
		if (army.rebel != rebellion?.leader)
		{
			return false;
		}
		rebellion.defeatedBy = BestAntiRebelLeader(1 - army.battle_side);
		if (army.battle_side == 0)
		{
			rebellion.defeatedByKingdom = defender_kingdom;
		}
		else
		{
			rebellion.defeatedByKingdom = attacker_kingdom;
		}
		c.Imprison(by, recall: true, send_state: true, "battle");
		rebellion.defeatedBy = null;
		rebellion.defeatedByKingdom = null;
		return true;
	}

	private void DestroyArmy(Army army, bool force_destroy = false)
	{
		if (army == null)
		{
			return;
		}
		if (army.rebel == null || army.rebel.rebellion == null)
		{
			army.Destroy();
			return;
		}
		Rebellion rebellion = army.rebel.rebellion;
		if (army.rebel != rebellion?.leader)
		{
			if (force_destroy)
			{
				army.Destroy();
			}
			return;
		}
		rebellion.defeatedBy = BestAntiRebelLeader(1 - army.battle_side);
		if (attackers.Contains(army))
		{
			rebellion.defeatedByKingdom = defender_kingdom;
		}
		else
		{
			rebellion.defeatedByKingdom = attacker_kingdom;
		}
		army.Destroy();
		rebellion.defeatedBy = null;
		rebellion.defeatedByKingdom = null;
	}

	public void AftermathNoble(Army army, War war, bool neutralized, Kingdom k, bool won, bool army_must_escape)
	{
		bool flag = false;
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit.def.type == Unit.Type.Noble)
			{
				unit.SetDamage(0f, send_state: false);
				if (unit.IsDefeated())
				{
					flag = true;
				}
			}
		}
		if (army_must_escape || !(army.IsDefeated() || flag))
		{
			return;
		}
		if (war != null && !neutralized && army.leader != null)
		{
			war.AddActivity("KnightNeutralized", k, army.leader.GetKingdom(), null, WarScoreKnightNeutralized(war.def, army.leader));
		}
		army.NotifyListeners("killed");
		if (army.leader != null)
		{
			Character leader = army.leader;
			army.SetLeader(null);
			if (leader.IsRebel())
			{
				killed_nobles.Add("Rebel");
			}
			else if (leader.IsMercenary())
			{
				killed_nobles.Add("Mercenary");
			}
			else
			{
				killed_nobles.Add(leader.title);
			}
			leader.NotifyListeners("killed_in_battle", this);
			leader.Die(new DeadStatus("defeated_in_combat", leader));
		}
		if (army.IsValid())
		{
			DestroyArmy(army, force_destroy: true);
		}
	}

	public void HealUnits(Army army, Kingdom k, AftermathOutcome outcome)
	{
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit.def.type == Unit.Type.Noble)
			{
				continue;
			}
			if (unit.def.type == Unit.Type.Noble)
			{
				unit.damage = 0f;
			}
			else if (unit.simulation != null && unit.simulation.state != BattleSimulation.Squad.State.Dead)
			{
				int max_troops = unit.simulation.max_troops;
				int num = (int)Math.Round((unit.damage - unit.simulation.initial_damage) * (float)max_troops);
				float num2 = army.def.units_healed_post_battle;
				if (num2 > army.def.max_units_healed_post_battle)
				{
					num2 = army.def.max_units_healed_post_battle;
				}
				if (outcome == AftermathOutcome.Defeated)
				{
					num2 *= army.def.defeated_heal_mod_perc / 100f;
				}
				int num3 = (int)Math.Ceiling((float)num * num2 / 100f);
				unit.SetDamage((float)Math.Ceiling(unit.damage * (float)max_troops - (float)num3) / (float)max_troops, send_state: false);
			}
		}
	}

	public void RemoveDeadUnits(Army army)
	{
		for (int num = army.units.Count - 1; num >= 0; num--)
		{
			Unit unit = army.units[num];
			if (!(unit.damage < 1f - def.min_survival_health))
			{
				army.NotifyListeners("unit_killed", unit);
				army.DelUnit(unit, send_state: false);
			}
		}
	}

	public float PlunderProgress()
	{
		if (plunder_progress == null)
		{
			return 0f;
		}
		return plunder_progress.Get() / def.duration;
	}

	private void CheckArmies()
	{
		List<Army> armies = GetArmies(1);
		if (armies.Count == 0)
		{
			return;
		}
		int num = armies.Count - 1;
		while (num >= 0 && num < armies.Count)
		{
			Army army = armies[num];
			if (army.IsDefeated())
			{
				Leave(army);
			}
			num--;
		}
	}

	public bool CheckVictory(bool apply_results = true, bool force = false)
	{
		int num;
		return CheckVictory(apply_results, force, out num);
	}

	public bool CheckVictory(bool apply_results, bool force, out int winner)
	{
		winner = this.winner;
		using (Game.Profile("Battle.CheckVictory"))
		{
			if (IsFinishing() && !force)
			{
				return true;
			}
			if (!IsValid())
			{
				return false;
			}
			if (battle_map_only)
			{
				return false;
			}
			if (batte_view_game != null && stage != Stage.Ongoing)
			{
				return false;
			}
			if (battle_map_finished)
			{
				if (batte_view_game != null)
				{
					return true;
				}
				if (apply_results)
				{
					if (battle_view_victory_reason == VictoryReason.IdleLeaveBattle && !idle_leaving_battle)
					{
						IdleLeaveBattle();
					}
					if (battle_view_victory_reason == VictoryReason.Retreat && !retreated)
					{
						Retreat(1 - winner);
						return false;
					}
					if (!is_plunder && !is_siege)
					{
						Victory(winner == 0, battle_view_victory_reason);
						return true;
					}
				}
			}
			if (batte_view_game != null)
			{
				for (int i = 0; i < 2; i++)
				{
					if (CapturePointsControlled(i))
					{
						winner = i;
						if (apply_results)
						{
							List<Squad> list = squads.Get(1 - i);
							for (int j = 0; j < list.Count; j++)
							{
								Squad squad = list[j];
								if (!squad.IsDefeated())
								{
									squad.simulation.SetState(BattleSimulation.Squad.State.Fled, 0f);
								}
							}
							SetBattleViewVictoryReason(VictoryReason.CapturePoints, winner);
						}
						return true;
					}
					if (ArmiesDefeated(i))
					{
						winner = 1 - i;
						if (apply_results)
						{
							SetBattleViewVictoryReason(LeadersKilled(i) ? VictoryReason.LeaderKilled : VictoryReason.Combat, winner);
						}
						return true;
					}
				}
				return false;
			}
			if (ArmiesDefeated(0))
			{
				if (apply_results)
				{
					Victory(attacker_won: false, VictoryReason.Combat, force);
				}
				winner = 1;
				return true;
			}
			if (is_plunder)
			{
				if (!ArmiesDefeated(1))
				{
					return false;
				}
				if (type == Type.PlunderInterrupt)
				{
					ResumePlunder();
				}
				if (PlunderProgress() < 1f)
				{
					return false;
				}
				if (apply_results)
				{
					Victory(attacker_won: true, VictoryReason.Combat, force);
				}
				winner = 0;
				return true;
			}
			if (type == Type.Siege && batte_view_game == null)
			{
				if (ArmiesDefeated(1))
				{
					if (apply_results)
					{
						Victory(attacker_won: true, VictoryReason.Combat, force);
					}
					winner = 0;
					return true;
				}
				if (resilience <= 0f)
				{
					if (defenders.Count > 0)
					{
						if (apply_results)
						{
							BreakSiege(BreakSiegeFrom.Inside);
						}
						return false;
					}
					if (apply_results)
					{
						Victory(attacker_won: true, VictoryReason.Combat, force);
					}
					winner = 0;
					return true;
				}
			}
			if (ArmiesDefeated(1))
			{
				if (apply_results)
				{
					Victory(attacker_won: true, VictoryReason.Combat, force);
				}
				winner = 0;
				return true;
			}
			return false;
		}
	}

	public bool CapturePointsControlled(int side)
	{
		if (capture_points == null || capture_points.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < capture_points.Count; i++)
		{
			CapturePoint capturePoint = capture_points[i];
			if (capturePoint.def.count_victory && capturePoint.battle_side != side)
			{
				return false;
			}
		}
		return true;
	}

	private void Aftermath()
	{
		CombineSquads();
		PostBattleRetreatDamage();
		ConsumeSupplies();
		if (settlement == null || settlement.garrison == null)
		{
			return;
		}
		if (settlement_food_copy != null && settlement is Castle castle)
		{
			castle.SetFood(settlement_food_copy.Get(), clamp: true);
		}
		for (int i = 0; i <= 1; i++)
		{
			List<BattleSimulation.Squad> list = simulation.GetSquads(i);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				BattleSimulation.Squad squad = list[num];
				if (squad.temporary)
				{
					if (squad.garrison != null)
					{
						squad.garrison.DelUnit(squad.unit, send_state: false);
					}
					else if (squad.army != null)
					{
						squad.army.DelUnit(squad.unit, send_state: false);
						if (squad.equipment != null && squad.IsDefeated())
						{
							squad.army.DelInvetoryItem(squad.equipment);
						}
					}
				}
				else if (squad.garrison != null && winner == 0)
				{
					squad.garrison.DelUnit(squad.unit, send_state: false);
				}
			}
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				armies[j].SendState<Army.UnitsState>();
			}
		}
		_ = settlement;
		for (int k = 0; k < settlement.garrison.units.Count; k++)
		{
			Unit unit = settlement.garrison.units[k];
			if (unit != null && unit.simulation != null)
			{
				if (unit.simulation.state < BattleSimulation.Squad.State.Retreating)
				{
					unit.simulation = null;
					continue;
				}
				settlement.NotifyListeners("unit_killed", unit);
				settlement.garrison.DelUnit(k, send_state: false);
				k--;
			}
		}
		settlement.SendState<Settlement.GarrisonUnitsState>();
		settlement.SendState<Settlement.AttackerGarrisonUnitsState>();
	}

	private void AftermathCrusaders(bool attacker_won)
	{
		for (int i = 0; i < 2; i++)
		{
			bool won = (attacker_won && i == 0) || (!attacker_won && i == 1);
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				Army army = armies[j];
				AftermathCrusader(army, won);
			}
		}
	}

	private void AftermathRebelsMercenaries(bool attacker_won)
	{
		int num = -1;
		Rebellion rebellion = null;
		for (int i = 0; i < 2; i++)
		{
			bool won = (attacker_won && i == 0) || (!attacker_won && i == 1);
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				Army army = armies[j];
				if (army.rebel != null)
				{
					num = i;
					rebellion = army.rebel.rebellion;
				}
				AftermathRebel(army, won);
				AftermathMercenary(army, won);
			}
		}
		if (num == -1 || rebellion == null)
		{
			return;
		}
		Realm realm = GetRealm();
		if (realm == null)
		{
			return;
		}
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		bool flag = true;
		for (int k = 0; k < rebellion.rebels.Count; k++)
		{
			if (!rebellion.rebels[k].army.IsDefeated())
			{
				flag = false;
				break;
			}
		}
		if (num == 0 && !attacker_won)
		{
			if (kingdom != defender_kingdom)
			{
				defender_kingdom.AddRelationModifier(kingdom, "rel_change_kill_foreign_rebel_army", this);
				if (flag)
				{
					defender_kingdom.AddRelationModifier(kingdom, "rel_change_end_foreign_rebellion", this);
				}
			}
			if (defender_support == null)
			{
				return;
			}
			Kingdom kingdom2 = defender_support.GetKingdom();
			if (kingdom != kingdom2 && kingdom2 != defender_kingdom)
			{
				kingdom2.AddRelationModifier(kingdom, "rel_change_kill_foreign_rebel_army", this);
				if (flag)
				{
					kingdom2.AddRelationModifier(kingdom, "rel_change_end_foreign_rebellion", this);
				}
			}
		}
		else
		{
			if (!(num == 1 && attacker_won))
			{
				return;
			}
			if (kingdom != attacker_kingdom)
			{
				attacker_kingdom.AddRelationModifier(kingdom, "rel_change_kill_foreign_rebel_army", this);
				if (flag)
				{
					attacker_kingdom.AddRelationModifier(kingdom, "rel_change_end_foreign_rebellion", this);
				}
			}
			if (attacker_support == null)
			{
				return;
			}
			Kingdom kingdom3 = attacker_support.GetKingdom();
			if (kingdom != kingdom3 && kingdom3 != attacker_kingdom)
			{
				kingdom3.AddRelationModifier(kingdom, "rel_change_kill_foreign_rebel_army", this);
				if (flag)
				{
					kingdom3.AddRelationModifier(kingdom, "rel_change_end_foreign_rebellion", this);
				}
			}
		}
	}

	private void AftermathCrusader(Army army, bool won)
	{
		if (army?.leader == null || !army.leader.IsCrusader() || won)
		{
			return;
		}
		Crusade crusade = game.religions.catholic.crusade;
		Kingdom kingdom = null;
		Kingdom kingdom2;
		if (attackers.Contains(army))
		{
			kingdom2 = defender_kingdom;
			if (defender_support != null && defender_support.kingdom_id != defender_kingdom.id)
			{
				kingdom = defender_support.GetKingdom();
			}
		}
		else
		{
			kingdom2 = attacker_kingdom;
			if (attacker_support != null && attacker_support.kingdom_id != attacker_kingdom.id)
			{
				kingdom = attacker_support.GetKingdom();
			}
		}
		kingdom2.NotifyListeners("defeated_crusader");
		kingdom?.NotifyListeners("defeated_crusader");
		if (army.IsDefeated())
		{
			crusade.target.GetCrownAuthority().AddModifier("stopped_crusade_against_us");
			kingdom2.NotifyListeners("stoped_crusade", crusade.target);
			kingdom?.NotifyListeners("stoped_crusade", crusade.target);
			Kingdom hq_kingdom = game.religions.catholic.hq_kingdom;
			if (hq_kingdom != null && !hq_kingdom.IsDefeated())
			{
				if (kingdom2 != hq_kingdom && kingdom2.is_catholic)
				{
					kingdom2.AddRelationModifier(hq_kingdom, "rel_catholic_stopped_crusade_papacy", null);
				}
				if (kingdom != null && kingdom != hq_kingdom && kingdom2.is_catholic)
				{
					kingdom.AddRelationModifier(hq_kingdom, "rel_catholic_stopped_crusade_papacy", null);
				}
			}
			if (kingdom2 != crusade.target)
			{
				kingdom2.AddRelationModifier(crusade.target, "rel_stopped_crusade_target", null);
			}
			if (kingdom != null && kingdom != crusade.target)
			{
				kingdom.AddRelationModifier(crusade.target, "rel_stopped_crusade_target", null);
			}
		}
		float wealth = crusade.wealth;
		kingdom2.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, crusade.wealth);
		crusade.wealth = 0f;
		crusade.SendState<Crusade.WealthState>();
		crusade.NotifyListeners("wealth_changed");
		if (wealth != 0f)
		{
			Vars vars = new Vars();
			vars.Set("kingdom", kingdom2);
			vars.Set("gold", wealth);
			NotifyListeners("gold_won", vars);
		}
	}

	private void AftermathRebel(Army army, bool won)
	{
		if (army.rebel != null)
		{
			army.rebel.CalcBattleOver(this, won);
		}
	}

	private void AftermathMercenary(Army army, bool won)
	{
		if (army.mercenary != null)
		{
			army.mercenary.CalcBattleOver(this, won);
		}
	}

	private void ReinforcementAftermath(Army army)
	{
		int battle_side = army.battle_side;
		Reinforcement obj = reinforcements[battle_side];
		bool flag = false;
		if (obj.army == army)
		{
			battle_side = army.battle_side;
			flag = true;
		}
		else
		{
			battle_side = army.battle_side + 2;
			if (reinforcements[battle_side].army == army)
			{
				flag = true;
			}
		}
		if (flag && army.battle == this)
		{
			army.LeaveCastle(army.position);
			AdjustArmyPosition(army);
		}
	}

	private void Aftermath(Army army, AftermathOutcome outcome, bool add_experience = false)
	{
		if (!army.IsAuthority() || !army.started)
		{
			return;
		}
		OnBattleEndedAnalytics(army, outcome);
		bool flag = false;
		Kingdom kingdom = army.GetKingdom();
		Kingdom kingdom2 = game.GetKingdom((army.battle_side == 0) ? defender_kingdom.id : attacker_kingdom.id);
		War war = kingdom.FindWarWith(kingdom2);
		bool won = outcome == AftermathOutcome.Won;
		if (outcome != AftermathOutcome.Cancelled)
		{
			AddTemporaryMorale(army, won);
		}
		else
		{
			Kingdom kingdom3 = attacker_kingdom;
			Kingdom kingdom4 = army.GetKingdom();
			Kingdom kingdom5 = defender_kingdom;
			if (kingdom4.IsAlly(kingdom3))
			{
				kingdom3.AddRelationModifier(kingdom4, "rel_battle_join_ally", this, -1f);
			}
			else
			{
				kingdom3.AddRelationModifier(kingdom4, "rel_battle_join_neutral", this, -1f);
			}
			kingdom5.AddRelationModifier(kingdom4, "rel_battle_help_enemy", this, -1f);
		}
		if (add_experience && can_add_experience)
		{
			can_add_experience = false;
			AddUnitsKilled();
			AddExperience(army.battle_side, won, (outcome == AftermathOutcome.Defeated) ? def.experience_retreat_mod : 1f);
			AddExperience(1 - army.battle_side, won);
		}
		bool army_must_escape = false;
		if (outcome == AftermathOutcome.Defeated && army.leader != null && army.units != null && army.units.Count > 0 && army.units[0] != null)
		{
			Character leader = army.leader;
			bool flag2 = true;
			Unit unit = null;
			for (int i = 0; i < army.units.Count; i++)
			{
				Unit unit2 = army.units[i];
				if (unit2.def.type == Unit.Type.Noble)
				{
					unit = unit2;
				}
				else if (!unit2.IsDefeated())
				{
					flag2 = false;
					break;
				}
			}
			if (unit?.simulation == null || !unit.simulation.killed_in_bv)
			{
				float num = game.Random(0, 100);
				if (is_siege && army.battle_side == 1)
				{
					Realm realm = this.settlement.GetRealm();
					float stat = realm.GetStat(Stats.rs_escape_siege_chance);
					if (num < stat)
					{
						Settlement settlement = null;
						for (int num2 = realm.settlements.Count - 1; num2 >= 0; num2--)
						{
							Settlement settlement2 = realm.settlements[num2];
							if (settlement2.IsActiveSettlement() && (settlement2.type == "Keep" || settlement2.type == "Castle") && settlement2.battle == null && settlement2 != this.settlement && settlement2.IsOwnStance(defender))
							{
								settlement = settlement2;
								break;
							}
						}
						if (settlement != null)
						{
							for (int num3 = army.units.Count - 1; num3 >= 0; num3--)
							{
								if (army.units[num3].def.type == Unit.Type.Noble)
								{
									army.units[num3].SetDamage(0f);
								}
								else
								{
									army.DelUnit(army.units[num3]);
								}
							}
							if (army.castle != null)
							{
								army.LeaveCastle(settlement.GetRandomExitPoint());
							}
							else
							{
								army.SetPosition(settlement.GetRandomExitPoint());
								army.NotifyListeners("reset_formation");
							}
							if (army.leader != null)
							{
								escaped_nobles.Add(army.leader);
							}
							return;
						}
					}
				}
				else
				{
					float stat2 = leader.GetStat(Stats.cs_imprison_in_battle_chance);
					float stat3 = leader.GetStat(Stats.cs_imprison_in_battle_retreat_chance);
					if ((flag2 && num <= stat2) || num <= stat3)
					{
						if (war != null && !flag && army.leader != null)
						{
							flag = true;
							war.AddActivity("KnightNeutralized", kingdom2, leader.GetKingdom(), null, WarScoreKnightNeutralized(war.def, army.leader));
						}
						if (kingdom2 != null && kingdom2.type == Kingdom.Type.Regular)
						{
							if (ImprisonLeader(leader, kingdom2))
							{
								imprisoned_at_end_of_battle_characters.Add(leader);
							}
							if (!army.IsMercenary())
							{
								army.units[0].SetDamage(1f);
							}
							leader.NotifyListeners("imprisoned_in_battle", this);
						}
					}
				}
			}
			if (leader.IsCrusader())
			{
				leader.NotifyListeners("crusade_lost_battle");
			}
		}
		if (army != null && army.IsValid())
		{
			AftermathNoble(army, war, flag, kingdom2, won, army_must_escape);
			if (army.IsValid())
			{
				HealUnits(army, kingdom, outcome);
				RemoveDeadUnits(army);
				army.SendState<Army.UnitsState>();
			}
			if (outcome == AftermathOutcome.Defeated && !army.IsFleeing())
			{
				army.NotifyListeners("retreat");
				army.FleeFrom(position, army.def.flee_distance);
			}
			if (army.IsDefeated() && army.IsValid())
			{
				army.SetBattle(null);
				DestroyArmy(army, force_destroy: true);
			}
		}
	}

	public void RetreatDamage(Army army)
	{
		RetreatDamage(army.units, army.battle_side);
	}

	public void RetreatDamage(List<Unit> units, int battle_side)
	{
		if (units.Count == 0 || type == Type.Siege)
		{
			return;
		}
		for (int i = 0; i < units.Count; i++)
		{
			if (!units[i].IsDefeated() && units[i].def.type != Unit.Type.Noble)
			{
				retreated_units[battle_side].Add(units[i]);
			}
		}
	}

	public void Cancel(VictoryReason reason, War war = null, Object cancelled_by = null)
	{
		if (stage < Stage.Finishing)
		{
			try
			{
				CancelEffect(reason, war, cancelled_by);
			}
			catch (Exception ex)
			{
				Error(ex.ToString());
				NotifyVisuals("broken_battle_report");
			}
			CheckBrokenBattle();
		}
	}

	public void CancelEffect(VictoryReason reason, War war = null, Object cancelled_by = null)
	{
		if (stage >= Stage.Finishing)
		{
			return;
		}
		winner = -1;
		this.cancelled_by = cancelled_by;
		SetVictoryReason(reason);
		SetStage(Stage.Finishing);
		if (!IsAuthority())
		{
			return;
		}
		SetSiegePostBattle();
		OnBattleEndedAnalytics(settlement, AftermathOutcome.Cancelled);
		Aftermath();
		FillVars(send_state: false);
		if (war == null)
		{
			war = attacker_kingdom.FindWarWith(defender_kingdom);
		}
		vars.Set("war", war);
		if (war != null)
		{
			vars.Set("war_winner", war.victor_side);
		}
		for (int i = 0; i < 2; i++)
		{
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				Army army = armies[j];
				if (army == null)
				{
					continue;
				}
				Kingdom kingdom = army.GetKingdom();
				if (kingdom != null)
				{
					Kingdom kingdom2 = game.GetKingdom((army.battle_side == 0) ? defender_kingdom.id : attacker_kingdom.id);
					AftermathNoble(army, war, neutralized: false, kingdom2, won: false, army_must_escape: true);
					if (army.IsValid())
					{
						HealUnits(army, kingdom, AftermathOutcome.Cancelled);
						RemoveDeadUnits(army);
						army.SendState<Army.UnitsState>();
					}
					army.SetBattle(null);
					if (simulation != null)
					{
						simulation.OnArmyLeft(army);
					}
					army.battle_side = -1;
				}
			}
			armies.Clear();
		}
		SendVars();
		if (settlement != null)
		{
			settlement.SetBattle(null);
		}
		if (batte_view_game == null && IsValid())
		{
			Destroy();
		}
		SetStage(Stage.Finished);
	}

	private void SetSiegePostBattle()
	{
		if (is_siege && settlement?.keep_effects != null)
		{
			if (winner == 0)
			{
				settlement.keep_effects.resilience_condition.Set(settlement.keep_effects.def.overtaken_keep_effects);
			}
			else
			{
				settlement.keep_effects.resilience_condition.Set(100f * resilience / Math.Max(1f, initial_resilience_pre_condition));
			}
			settlement.keep_effects.siege_defense_condition.Set(100f * siege_defense / Math.Max(1f, initial_siege_defense_pre_condition));
			settlement.keep_effects.UpdateSiegeRates();
		}
	}

	public float GoldFromPlunder(bool is_supporter)
	{
		Realm realm = game.GetRealm(realm_id);
		if (realm == null || attacker == null || attacker.IsHiredMercenary())
		{
			return 0f;
		}
		Resource resources = settlement.GetResources();
		float plunder_gold_base = def.plunder_gold_base;
		float num = def.plunder_gold_per_gold * resources.Get(ResourceType.Gold);
		float num2 = def.plunder_gold_per_book * resources.Get(ResourceType.Books);
		float num3 = def.plunder_gold_per_levy * resources.Get(ResourceType.Levy);
		float num4 = def.plunder_gold_per_hammer * resources.Get(ResourceType.Hammers);
		float num5 = def.plunder_gold_per_worker * resources.Get(ResourceType.WorkerSlots);
		float num6 = def.plunder_gold_per_commerce * resources.Get(ResourceType.Trade);
		return (is_supporter ? def.plunder_per_supporter : 1f) * (plunder_gold_base + realm.income.Get(ResourceType.Gold) * def.plunder_per_gold_production + num + num2 + num3 + num4 + num5 + num6);
	}

	public float SuppliesFromPlunder(bool is_supporter)
	{
		if (game.GetRealm(realm_id) == null || attacker.IsHiredMercenary())
		{
			return 0f;
		}
		Resource resources = settlement.GetResources();
		float plunder_supplies_base = def.plunder_supplies_base;
		float num = def.plunder_supplies_per_food * resources.Get(ResourceType.Food);
		float num2 = def.plunder_supplies_per_levy * resources.Get(ResourceType.Levy);
		float num3 = def.plunder_supplies_per_worker * resources.Get(ResourceType.WorkerSlots);
		return (is_supporter ? def.plunder_supplies_per_supporter : 1f) * (plunder_supplies_base + num + num2 + num3);
	}

	public float BooksFromPlunder(bool is_supporter)
	{
		if (game.GetRealm(realm_id) == null || attacker.IsHiredMercenary())
		{
			return 0f;
		}
		Resource resources = settlement.GetResources();
		float num = def.plunder_books_per_book * resources.Get(ResourceType.Books);
		return (is_supporter ? def.plunder_per_supporter : 1f) * num;
	}

	public void Victory(bool attacker_won, VictoryReason reason, bool force = false)
	{
		if (stage < Stage.Finishing || force)
		{
			try
			{
				VictoryEffect(attacker_won, reason);
			}
			catch (Exception ex)
			{
				Error(ex.ToString());
				NotifyVisuals("broken_battle_report");
			}
			CheckBrokenBattle();
		}
	}

	public void VictoryEffect(bool attacker_won, VictoryReason reason)
	{
		if (!IsValid())
		{
			return;
		}
		Kingdom kingdom = attacker_kingdom;
		Kingdom kingdom2 = defender_kingdom;
		Realm realm = game.GetRealm(realm_id);
		Rebel rebel = null;
		winner = ((!attacker_won) ? 1 : 0);
		SetVictoryReason(reason);
		FillVars(send_state: false);
		SetStage(Stage.Finishing, send_state: false);
		if (attacker_won)
		{
			attacker?.leader?.NotifyListeners("battle_won", this);
			kingdom?.NotifyListeners("battle_won", this);
			(defender as Army)?.leader?.NotifyListeners("battle_lost", this);
			kingdom2?.NotifyListeners("battle_lost", this);
			OnBattleEndedAnalytics(settlement, AftermathOutcome.Defeated);
		}
		else
		{
			attacker?.leader?.NotifyListeners("battle_lost", this);
			kingdom?.NotifyListeners("battle_lost", this);
			(defender as Army)?.leader?.NotifyListeners("battle_won", this);
			kingdom2?.NotifyListeners("battle_won", this);
			OnBattleEndedAnalytics(settlement, AftermathOutcome.Won);
		}
		if (attackers.Count > 0)
		{
			rebel = attackers[0].rebel;
		}
		if (rebel != null && rebel.IsLoyalist())
		{
			if (type == Type.OpenField)
			{
				rebel.AddRelLoyal("field_battle", kingdom2);
			}
			else if (is_plunder)
			{
				rebel.AddRelLoyal("pillage", kingdom2);
			}
		}
		War war = kingdom.FindWarWith(kingdom2);
		Castle castle = settlement as Castle;
		bool flag = castle?.GetRealm().IsOccupied() ?? (settlement != null && settlement.IsOccupied());
		if (!IsAuthority() || battle_map_only)
		{
			return;
		}
		if (attacker_won && settlement != null && is_plunder)
		{
			List<Army> armies = GetArmies(0);
			float num = 0f;
			for (int i = 0; i < armies.Count; i++)
			{
				Army army = armies[i];
				Kingdom kingdom3 = army.GetKingdom();
				if (army == attacker_support || i == 0)
				{
					float num2 = 0f;
					if (army?.leader != null)
					{
						num2 = army.leader.GetStat(Stats.cs_pillage_gold_perc) / 100f + War.GetBonus(kingdom3, defender_kingdom, "pillage_gold_perc");
					}
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			for (int j = 0; j < armies.Count; j++)
			{
				Army army2 = armies[j];
				Kingdom kingdom4 = army2.GetKingdom();
				float num3 = 0f;
				float num4 = 0f;
				float num5 = 0f;
				bool is_supporter = j > 0;
				if (army2 == attacker_support || j == 0)
				{
					float num6 = num;
					num3 = (float)Math.Ceiling(GoldFromPlunder(is_supporter) * (1f + num6));
					num4 = BooksFromPlunder(is_supporter);
				}
				num5 = SuppliesFromPlunder(is_supporter);
				AddPlunder(num3, num5, num4, j);
				kingdom4.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, num3);
				kingdom4.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Books, num4);
				army2.AddSupplies(num5);
			}
		}
		Aftermath();
		Realm realm2 = null;
		Army army3 = null;
		if (attacker_won)
		{
			if (war != null)
			{
				int num7 = WarScoreUnitsKilled(war.def, 1);
				if (num7 > 0)
				{
					war.AddActivity("UnitsKilled", kingdom, kingdom2, realm, num7);
				}
			}
			if (settlement != null)
			{
				if (castle != null)
				{
					realm2 = castle.GetRealm();
					if (rebel != null && rebel.IsLoyalist())
					{
						rebel.AddRelLoyal("town_taken", kingdom2);
					}
					if (realm2 != null)
					{
						kingdom2.AddSupportPenalty("WarSupportLostRealm");
						kingdom.AddRelationModifier(kingdom2, "rel_battle_siege", this);
						if (war != null)
						{
							war.AddActivity("BattleWon", kingdom, kingdom2, realm);
							war.AddActivity("CapturedRealm", kingdom, kingdom2, realm, WarScoreProvinceTaken(war.def));
						}
						if (kingdom2.IsRebelKingdom() && kingdom != castle.GetKingdom() && kingdom.GetWarStance(castle.GetKingdom()).IsPeace())
						{
							kingdom.AddRelationModifier(castle.GetKingdom(), "rel_foreign_occupation_lifted", this);
						}
					}
					army3 = castle.army;
					if (type == Type.Siege && castle.army != null && castle.army.IsValid())
					{
						castle.army.SetBattle(null);
						if (castle.army.leader != null)
						{
							Character leader = castle.army.leader;
							if (ImprisonLeader(leader, kingdom))
							{
								imprisoned_at_end_of_battle_characters.Add(leader);
							}
						}
						else if (castle.army.IsValid())
						{
							DestroyArmy(castle.army);
						}
					}
					if (attackers.Count > 0 && attackers[0].IsValid() && attackers[0].mercenary == null)
					{
						if (attackers[0].IsOwnStance(realm2.GetKingdom()) || attackers[0].IsEnemy(realm2.GetKingdom()))
						{
							attackers[0].EnterCastle(castle);
						}
					}
					else if (castle.army != null)
					{
						castle.army.LeaveCastle(castle.GetRandomExitPoint());
					}
				}
				else
				{
					if (war != null)
					{
						kingdom2.AddSupportPenalty("WarSupportPillagedSettlement");
						if (is_siege)
						{
							kingdom.AddRelationModifier(kingdom2, "rel_battle_siege_keep", this);
						}
						else
						{
							kingdom.AddRelationModifier(kingdom2, "rel_battle_pillage", this);
						}
					}
					kingdom.NotifyListeners("pillaged_settlement", settlement);
					if (settlement is Village { famous_person: not null, famous_person: var famous_person } village && (float)game.Random(0, 100) < famous_person.famous_def.chance_die_pillage)
					{
						village.SetFamous(null);
						famous_person.Die();
					}
					if (settlement.keep_effects != null && settlement.keep_effects.CanBeTakenOver())
					{
						if (rebel != null)
						{
							settlement.keep_effects.SetOccupied(rebel.rebellion);
						}
						else
						{
							settlement.keep_effects.SetOccupied(kingdom);
						}
						if (war != null && settlement.IsOccupied() != flag)
						{
							war.AddActivity("CapturedKeep", kingdom, kingdom2, realm);
						}
					}
					else
					{
						settlement.razedPenaltyPerc += kingdom.GetStat(Stats.ks_settlement_razed_penalty);
						settlement.SetStateDestroyed(isRazed: true);
						war?.AddActivity("PillagedSettlement", kingdom, kingdom2, realm);
					}
				}
			}
			else
			{
				if (war != null)
				{
					war.AddActivity("BattleWon", kingdom, kingdom2, realm);
					kingdom2.AddSupportPenalty("WarSupportFieldBattle");
				}
				kingdom.AddRelationModifier(kingdom2, "rel_battle_field", this);
			}
		}
		else
		{
			if (war != null)
			{
				int num8 = WarScoreUnitsKilled(war.def, 0);
				if (num8 > 0)
				{
					war.AddActivity("UnitsKilled", kingdom2, kingdom, realm, num8);
				}
			}
			if (settlement != null)
			{
				if (castle != null)
				{
					if (war != null)
					{
						if (attackers.Count > 0 || type != Type.Siege)
						{
							war.AddActivity("BattleWon", kingdom2, kingdom, realm);
						}
						kingdom.AddSupportPenalty("WarSupportSigeAssault");
					}
					kingdom2.AddRelationModifier(kingdom, "rel_battle_siege_defended", this);
				}
				else
				{
					if (war != null)
					{
						war.AddActivity("BattleWon", kingdom2, kingdom, realm);
						kingdom.AddSupportPenalty("WarSupportFieldBattle");
					}
					kingdom2.AddRelationModifier(kingdom, "rel_battle_defended_settlement", this);
				}
			}
			else
			{
				if (war != null)
				{
					war.AddActivity("BattleWon", kingdom2, kingdom, realm);
					kingdom.AddSupportPenalty("WarSupportFieldBattle");
				}
				kingdom2.AddRelationModifier(kingdom, "rel_battle_field", this);
			}
		}
		AddExperience(attacker_won);
		if (settlement != null)
		{
			settlement.SetBattle(null);
		}
		if (realm2 != null)
		{
			bool flag2 = rebel != null;
			if (flag2)
			{
				Kingdom kingdom5 = attacker_support?.GetKingdom();
				if (kingdom5 != null && kingdom5.type == Kingdom.Type.Regular && rebel.rebellion.GetLoyalTo() == kingdom5)
				{
					flag2 = false;
				}
			}
			if (kingdom.type == Kingdom.Type.Crusade)
			{
				game.religions.catholic.crusade?.CastleCaptured(realm2);
			}
			else if (flag2)
			{
				realm2.SetOccupied(rebel.rebellion);
				kingdom2.GetCrownAuthority()?.AddModifier("rebelOccupation");
			}
			else
			{
				Kingdom kingdom6 = kingdom;
				if (rebel != null)
				{
					Kingdom kingdom7 = attacker_support?.GetKingdom();
					if (kingdom7 != null)
					{
						kingdom6 = kingdom7;
					}
					else
					{
						Error("Rebels trying to conquer realm instead of occupying");
					}
				}
				Kingdom kingdom8 = realm2.GetKingdom();
				if (flag && (kingdom8 == null || !kingdom8.IsEnemy(kingdom6.GetKingdom())))
				{
					realm2.SetOccupied(kingdom6);
				}
				else
				{
					realm2.SetKingdom(kingdom6.id, ignore_victory: true);
					kingdom6.NotifyListeners("realm_won_by_battle", realm2);
					if (realm2.IsMuslimHolyLand() && !kingdom6.is_muslim)
					{
						for (int k = 0; k < game.kingdoms.Count; k++)
						{
							Kingdom kingdom9 = game.kingdoms[k];
							if (kingdom9 != null && !kingdom9.IsDefeated() && kingdom9.is_muslim)
							{
								kingdom6.AddRelationModifier(kingdom9, "rel_conquered_muslim_holy_city", null);
							}
						}
					}
				}
				if (kingdom8 == game.religions.catholic.hq_kingdom && kingdom8.IsDefeated())
				{
					kingdom6.NotifyListeners("destroyed_papacy");
				}
				if (kingdom2.IsDefeated() || kingdom2.IsDominated())
				{
					kingdom2.HandlePrisonersOnDefeat(kingdom6);
				}
			}
			realm2.GetComponent<OnSiegeConvertReligion>().TryConvertBy(attacker?.leader);
		}
		Kingdom controllingKingdom = realm.GetControllingKingdom();
		if (controllingKingdom != null && realm2 != null && controllingKingdom.type == Kingdom.Type.Regular)
		{
			realm2.SetDisorder(realm2.pop_majority.kingdom != controllingKingdom);
		}
		if (castle != null && castle.GetRealm().IsOccupied() != flag)
		{
			castle.garrison.units.Clear();
			castle.SendState<Settlement.GarrisonUnitsState>();
			castle.NotifyListeners("garisson_changed");
		}
		if (castle != null && castle.army == null && army3 != null && army3.IsValid())
		{
			army3.EnterCastle(castle);
			army3.Stop();
		}
		SetSiegePostBattle();
		SetStage(Stage.Finished);
		AddMoraleKingdoms(attacker_won, realm2 != null && realm2.GetControllingKingdom() != defender_kingdom);
		AftermathRebelsMercenaries(attacker_won);
		AftermathCrusaders(attacker_won);
		for (int l = 0; l < 2; l++)
		{
			List<Army> armies2 = GetArmies(l);
			for (int m = 0; m < armies2.Count; m++)
			{
				Army army4 = armies2[m];
				army4.SetBattle(null);
				Aftermath(army4, ((!attacker_won || l != 0) && (attacker_won || l != 1)) ? AftermathOutcome.Defeated : AftermathOutcome.Won);
				if (simulation != null)
				{
					simulation.OnArmyLeft(army4);
				}
				army4.battle_side = -1;
			}
		}
		for (int n = 0; n < attackers.Count; n++)
		{
			attackers[n].RecalcSuppliesRate();
		}
		for (int num9 = 0; num9 < defenders.Count; num9++)
		{
			defenders[num9].RecalcSuppliesRate();
		}
		SendVars();
		attackers.Clear();
		defenders.Clear();
		settlement = null;
		if (castle != null && castle.sacked)
		{
			castle.sacking_comp.Begin();
		}
		if (!battle_map_only && batte_view_game == null && IsValid())
		{
			Destroy();
		}
	}

	private void AddMoraleKingdoms(bool attacker_won, bool town_captured)
	{
		List<Kingdom> list = new List<Kingdom>();
		List<Kingdom> list2 = new List<Kingdom>();
		if (attacker_kingdom.type != Kingdom.Type.LoyalistsFaction)
		{
			list.Add(attacker_kingdom);
		}
		if (defender_kingdom.type != Kingdom.Type.LoyalistsFaction)
		{
			list2.Add(defender_kingdom);
		}
		for (int i = 0; i < attackers.Count; i++)
		{
			Army army = attackers[0];
			Kingdom kingdom = army.GetKingdom();
			if (army.rebel != null)
			{
				Kingdom kingdom2 = game.GetKingdom(army.rebel.loyal_to);
				if (kingdom2 != null)
				{
					kingdom = kingdom2;
				}
			}
			if (kingdom != null && !list.Contains(kingdom))
			{
				list.Add(kingdom);
			}
		}
		for (int j = 0; j < defenders.Count; j++)
		{
			Army army2 = defenders[0];
			Kingdom kingdom3 = army2.GetKingdom();
			if (army2.rebel != null)
			{
				Kingdom kingdom4 = game.GetKingdom(army2.rebel.loyal_to);
				if (kingdom4 != null)
				{
					kingdom3 = kingdom4;
				}
			}
			if (kingdom3 != null && !list2.Contains(kingdom3))
			{
				list2.Add(kingdom3);
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			AddMoraleKingdom(list[k], attacker_won, town_captured: false);
		}
		for (int l = 0; l < list2.Count; l++)
		{
			AddMoraleKingdom(list2[l], !attacker_won, town_captured: false);
		}
		if (attacker_won && town_captured)
		{
			AddMoraleKingdom(defender_kingdom, won: false, town_captured: true, neighbors: true);
		}
	}

	private void AddMoraleKingdom(Kingdom k, bool won, bool town_captured, bool neighbors = false)
	{
		if (k == null)
		{
			return;
		}
		Realm realm = GetRealm();
		AddMoraleKingdom(realm, k, won, town_captured);
		if (neighbors)
		{
			for (int i = 0; i < realm.neighbors.Count; i++)
			{
				Realm realm2 = realm.neighbors[i];
				AddMoraleKingdom(realm2, k, won, town_captured);
			}
		}
	}

	private void AddMoraleKingdom(Realm realm, Kingdom k, bool won, bool town_captured)
	{
		if (realm == null)
		{
			return;
		}
		for (int i = 0; i < realm.armies.Count; i++)
		{
			Army army = realm.armies[i];
			if (army.battle == this)
			{
				continue;
			}
			if (!army.GetKingdom().IsOwnStance(k))
			{
				if (army.rebel == null)
				{
					continue;
				}
				Kingdom kingdom = army.rebel.rebellion?.GetLoyalTo();
				if (kingdom != null && kingdom != k)
				{
					continue;
				}
			}
			if (town_captured)
			{
				if (!won)
				{
					army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_nearby_town_lost);
				}
			}
			else if (won)
			{
				army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_nearby_won);
			}
			else
			{
				army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_nearby_lost);
			}
		}
	}

	public void AddPlunder(float gold, float supplies, float books, int army)
	{
		if (IsAuthority())
		{
			if (army == 0)
			{
				vars.Set("plunder_gold_amount", (gold > 0f) ? new Value(gold) : Value.Unknown);
				vars.Set("plunder_supplies_amount", (supplies > 0f) ? new Value(supplies) : Value.Unknown);
				vars.Set("plunder_books_amount", (books > 0f) ? new Value(books) : Value.Unknown);
			}
			else
			{
				vars.Set("plunder_gold_amount_supporter", (gold > 0f) ? new Value(gold) : Value.Unknown);
				vars.Set("plunder_supplies_amount_supporter", (supplies > 0f) ? new Value(supplies) : Value.Unknown);
				vars.Set("plunder_books_amount_supporter", (books > 0f) ? new Value(books) : Value.Unknown);
			}
		}
	}

	public static bool CanPillage(Army attacker, Settlement settlement)
	{
		if (settlement is Castle || settlement.keep_effects != null)
		{
			return false;
		}
		if (settlement.level <= 0)
		{
			return false;
		}
		if (!settlement.IsActiveSettlement())
		{
			return false;
		}
		Realm realm = settlement.GetRealm();
		if (realm.IsOccupied() && !realm.GetKingdom().IsEnemy(attacker))
		{
			return false;
		}
		return true;
	}

	public static bool CanSiege(Army attacker)
	{
		if (attacker.supplies == null)
		{
			return true;
		}
		return attacker.supplies.Get() > 0f;
	}

	public static bool ValidAttackerDefender(Army attacker, MapObject defender)
	{
		if (!attacker.IsValid())
		{
			attacker.Error($"Destroyed army trying to enter battle {attacker}");
			return false;
		}
		if (!defender.IsValid())
		{
			defender.Error($"Destroyed army/settlement trying to enter battle {defender}");
			return false;
		}
		return true;
	}

	public static Battle StartBattle(Army attacker, MapObject defender, bool battle_map_only = false)
	{
		if (!attacker.IsValid())
		{
			return null;
		}
		if (!defender.IsValid())
		{
			return null;
		}
		if (!attacker.IsAuthority())
		{
			return null;
		}
		if (attacker.battle != null)
		{
			attacker.game.Warning("Army already in battle");
			return attacker.battle;
		}
		if (!attacker.IsEnemy(defender))
		{
			return null;
		}
		if (defender is Army army)
		{
			if (army.castle == null)
			{
				if (army.battle != null)
				{
					if (army.battle.Join(attacker))
					{
						return army.battle;
					}
					return null;
				}
				if (army.movement.path != null && army.movement.path.flee)
				{
					return null;
				}
				if (ValidAttackerDefender(attacker, army))
				{
					Battle battle = ((army.currently_on_land || attacker.currently_on_land) ? new Battle(Type.OpenField, attacker, army, battle_map_only) : new Battle(Type.Naval, attacker, army, battle_map_only));
					battle.CalcStartingTroops();
					return battle;
				}
			}
			else
			{
				defender = army.castle;
			}
		}
		if (defender is Settlement settlement)
		{
			if (settlement.battle != null)
			{
				if (settlement.battle.Join(attacker))
				{
					return settlement.battle;
				}
				return null;
			}
			if (!(settlement is Castle) && settlement.keep_effects == null)
			{
				if (!CanPillage(attacker, settlement))
				{
					return null;
				}
				if (ValidAttackerDefender(attacker, settlement))
				{
					Battle battle = new Battle(Type.Plunder, attacker, settlement, battle_map_only);
					battle.CalcStartingTroops();
					return battle;
				}
			}
			if (CanSiege(attacker) && ValidAttackerDefender(attacker, settlement))
			{
				Battle battle = new Battle(Type.Siege, attacker, settlement, battle_map_only);
				battle.CalcStartingTroops();
				return battle;
			}
		}
		if (defender is Battle battle2)
		{
			if (battle2.Join(attacker))
			{
				return battle2;
			}
			return null;
		}
		return null;
	}

	public Realm GetRealm()
	{
		return game.GetRealm(realm_id);
	}

	public List<Army> GetArmies(int side)
	{
		return side switch
		{
			0 => attackers, 
			1 => defenders, 
			_ => null, 
		};
	}

	public List<Army> GetEnemies(int side)
	{
		return side switch
		{
			0 => defenders, 
			1 => attackers, 
			_ => null, 
		};
	}

	public Army GetArmy(int side)
	{
		List<Army> armies = GetArmies(side);
		if (armies == null || armies.Count < 1)
		{
			return null;
		}
		return armies[0];
	}

	public Army GetSupporter(int side)
	{
		switch (side)
		{
		case 0:
			if (attackers.Count <= 1)
			{
				return null;
			}
			return attackers[1];
		case 1:
			if (defenders.Count <= 1)
			{
				return null;
			}
			return defenders[1];
		default:
			return null;
		}
	}

	private void EndPreparation()
	{
		if (stage == Stage.Preparing)
		{
			SetStage(Stage.Ongoing);
		}
	}

	private void ClearOldLogic()
	{
		for (int i = 0; i < 2; i++)
		{
			List<BattleSimulation.Squad> list = simulation.GetSquads(i);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				BattleSimulation.Squad squad = list[num];
				if (squad.sub_squads != null)
				{
					for (int j = 0; j < squad.sub_squads.Count; j++)
					{
						BattleSimulation.Squad squad2 = squad.sub_squads[j];
						if (squad2?.squad != null)
						{
							squad2.squad.Destroy();
						}
					}
					squad.sub_squads = null;
				}
			}
			List<InventoryItem> equipment = simulation.GetEquipment(i);
			for (int k = 0; k < equipment.Count; k++)
			{
				InventoryItem u = equipment[k];
				simulation.DelSiegeSquad(u);
			}
		}
		if (fortifications != null)
		{
			fortification_destroyed = false;
			for (int l = 0; l < fortifications.Count; l++)
			{
				fortifications[l].Reset();
			}
			OpenAssaultedGate();
		}
		if (capture_points != null)
		{
			for (int m = 0; m < capture_points.Count; m++)
			{
				CapturePoint capturePoint = capture_points[m];
				capturePoint.SetBattleSide(capturePoint.original_battle_side);
				capturePoint.capture_progress.Set(1f);
				capturePoint.capture_progress.SetRate(0f);
				capturePoint.is_capturing = false;
			}
		}
		Ladder.ClearAll(this);
	}

	public void OpenAssaultedGate()
	{
		if (closest_gate_to_camps != null && assault_gate_action_succeeded)
		{
			closest_gate_to_camps.MainFortification()?.SetGateHealth(0f);
		}
	}

	private void EnterBattle(int kingdom_id, string map_name)
	{
		bool num = battle_view_kingdoms != null && battle_view_kingdoms.IndexOf(kingdom_id) >= 0;
		SetStage(Stage.EnteringBattle, send_state: false);
		SetBattleViewVictoryReason(VictoryReason.None, -1);
		if (!num)
		{
			NotifyListeners("enter_battle", kingdom_id);
			if (batte_view_game == null)
			{
				batte_view_game = new Game("battle_view", game);
				batte_view_game.LoadMap(map_name, null, new_game: true);
				simulation.RefreshDef();
			}
			if (battle_view_kingdoms == null)
			{
				battle_view_kingdoms = new List<int>();
			}
			battle_view_kingdoms.Add(kingdom_id);
		}
		else
		{
			simulation.OnRestart();
		}
		CreateSquads();
		if ((type == Type.Assault || type == Type.BreakSiege) && settlement.type == "Castle")
		{
			initiative = new ComputableValue(def.max_initiative, 0f - def.initiative_decay, batte_view_game, 0f, def.max_initiative);
			if (type == Type.Assault)
			{
				initiative_side = 0;
			}
			else if (type == Type.BreakSiege)
			{
				initiative_side = 1;
			}
			initiative_cooldown = 0f;
		}
		battle_view_game_start = batte_view_game.time;
		NotifyListeners("finished_enter_battle");
	}

	public void RefreshInitiative()
	{
		if (initiative == null)
		{
			return;
		}
		float num = 0f;
		if (stage == Stage.Ongoing && initiative.Get() > 0f)
		{
			num = 0f - def.initiative_decay;
		}
		float rate = initiative.GetRate();
		if (num != rate)
		{
			if (rate == 0f)
			{
				NotifyListeners("losing_initiative");
			}
			initiative.SetRate(num);
		}
	}

	public void UpdateInitiative(float deltaTime)
	{
		if (stage != Stage.Ongoing || initiative == null || deltaTime == 0f)
		{
			return;
		}
		if (initiative_cooldown > 0f)
		{
			initiative_cooldown -= deltaTime;
		}
		bool flag = !IsFightIdle();
		float num = initiative.Get();
		if (num <= 0f)
		{
			if (!initiative_countdown)
			{
				initiative_countdown = true;
				flag = true;
			}
		}
		else
		{
			initiative_countdown = false;
		}
		if (flag)
		{
			initiative_cooldown = (initiative_countdown ? simulation.def.initiative_countdown : simulation.def.initiative_cooldown);
			PauseInitiative();
		}
		else if (initiative_cooldown <= 0f)
		{
			initiative_cooldown = 0f;
			RefreshInitiative();
			if (initiative_auto_leave_battle && initiative_countdown && initiative != null && num <= 0f && IdleLeaveBattle())
			{
				NotifyListeners("initiative_lost");
			}
		}
	}

	private void PauseInitiative()
	{
		if (initiative != null && initiative.GetRate() != 0f)
		{
			initiative.SetRate(0f);
		}
	}

	private bool IsFightIdle()
	{
		if (squads != null)
		{
			for (int i = 0; i < squads.Count; i++)
			{
				Squad squad = squads[i];
				if ((!squad.IsDefeated() && squad.is_fighting) || (squad.battle_side == 0 && squad.is_inside_walls) || (squad.ranged_enemy != null && squad.CanShoot(squad.ranged_enemy)))
				{
					return false;
				}
			}
		}
		if (capture_points != null)
		{
			for (int j = 0; j < capture_points.Count; j++)
			{
				if (capture_points[j].IsValid() && capture_points[j].is_capturing)
				{
					return false;
				}
			}
		}
		if (towers != null)
		{
			for (int k = 0; k < towers.Count; k++)
			{
				if (!towers[k].IsDefeated() && towers[k].shoot_comp.is_shooting)
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool IdleLeaveBattle()
	{
		if (batte_view_game != null)
		{
			if (battle_view_victory_reason == VictoryReason.None)
			{
				SetBattleViewVictoryReason(VictoryReason.IdleLeaveBattle, 1 - initiative_side);
				return true;
			}
			return false;
		}
		idle_leaving_battle = true;
		SetAssaultGate(val: false);
		IdleLeavePenalty();
		for (int num = defenders.Count - 1; num >= 0; num--)
		{
			Army army = defenders[num];
			if (army.castle == null)
			{
				army.LeaveBattle(check: false);
			}
		}
		ResumeSiege();
		return true;
	}

	public void SetAssaultGate(bool val, bool send_state = true)
	{
		assault_gate_action_succeeded = val;
		if (send_state)
		{
			SendState<AssaultGateActionState>();
		}
	}

	private void IdleLeavePenalty()
	{
		List<BattleSimulation.Squad> list = simulation.GetSquads(initiative_side);
		for (int i = 0; i < list.Count; i++)
		{
			BattleSimulation.Squad squad = list[i];
			if (!squad.IsDefeated())
			{
				squad.AddMorale(simulation.def.morale_drop_from_initiative, acc: false);
			}
		}
	}

	private void LeaveBattle(int kingdom_id)
	{
		if (battle_view_kingdoms == null || battle_view_kingdoms.IndexOf(kingdom_id) < 0)
		{
			return;
		}
		NotifyListeners("leave_battle", kingdom_id);
		battle_view_kingdoms.Remove(kingdom_id);
		if (battle_view_kingdoms.Count > 0)
		{
			return;
		}
		battle_view_kingdoms = null;
		if (batte_view_game == null)
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			List<Squad> list = squads.Get(i);
			for (int j = 0; j < list.Count; j++)
			{
				BattleSimulation.Squad squad = list[j].simulation?.main_squad;
				if (squad != null && squad.spawned_in_bv)
				{
					simulation.DelSquad(squad);
					if (squad.army != null)
					{
						squad.army.DelUnit(squad?.unit);
					}
				}
			}
			List<Army> armies = GetArmies(i);
			for (int k = 0; k < armies.Count; k++)
			{
				Army army = armies[k];
				ReinforcementAftermath(army);
			}
		}
		CombineSquads();
		batte_view_game.Destroy();
		batte_view_game = null;
		simulation.RefreshDef();
		initiative = null;
		has_restarted = false;
		if (IsFinishing())
		{
			Destroy();
		}
	}

	private void PreRetreat()
	{
		for (int i = 0; i < 2; i++)
		{
			if (retreated_units[i] == null)
			{
				retreated_units[i] = new List<Unit>();
			}
			else
			{
				retreated_units[i].Clear();
			}
		}
	}

	private void PostRetreat()
	{
		if (type == Type.Siege)
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			List<Unit> list = retreated_units[i];
			if (list.Count == 0)
			{
				continue;
			}
			List<BattleSimulation.Squad> list2 = simulation.GetSquads(1 - i);
			for (int j = 0; j < list2.Count; j++)
			{
				if (list.Count <= 1)
				{
					break;
				}
				BattleSimulation.Squad squad = list2[j];
				if (squad == null || squad.IsDefeated())
				{
					continue;
				}
				Unit unit = null;
				int num = game.Random(0, list.Count);
				for (int k = 0; k < list.Count; k++)
				{
					unit = list[(k + num) % list.Count];
					if (!unit.simulation.IsDefeated())
					{
						break;
					}
				}
				if (unit != null && unit.def.type != Unit.Type.Noble)
				{
					float mult = squad.unit.retreat_damage_mod_modified(unit);
					simulation.Attack(squad, unit.simulation, i, mult);
					if (unit.simulation.damage + unit.simulation.damage_acc >= 1f)
					{
						unit.simulation.SetState(BattleSimulation.Squad.State.Dead);
					}
				}
			}
			for (int l = 0; l < list.Count; l++)
			{
				Unit unit2 = list[l];
				simulation.ApplyDamage(unit2.simulation, force: true);
			}
		}
	}

	public void PostBattleRetreatDamage()
	{
		if (type == Type.Siege)
		{
			return;
		}
		PreRetreat();
		for (int i = 0; i < 2; i++)
		{
			List<BattleSimulation.Squad> list = simulation.GetSquads(i);
			for (int j = 0; j < list.Count; j++)
			{
				BattleSimulation.Squad squad = list[j];
				if ((i != winner && victory_reason == VictoryReason.Surrender) || (squad.state == BattleSimulation.Squad.State.Fled && squad.def.type != Unit.Type.Noble))
				{
					retreated_units[i].Add(squad.unit);
				}
			}
		}
		PostRetreat();
	}

	public void Retreat(int side)
	{
		if (batte_view_game != null)
		{
			SetBattleViewVictoryReason(VictoryReason.Retreat, 1 - side);
			return;
		}
		if (side == 0 && (type == Type.Siege || (type == Type.Plunder && GetArmies(1 - side).Count == 0)))
		{
			Cancel(VictoryReason.LiftSiege);
			return;
		}
		retreated = true;
		if (side == 1)
		{
			if (is_siege)
			{
				List<Army> armies = GetArmies(side);
				bool flag = CanRetreatInside(1);
				PreRetreat();
				for (int num = armies.Count - 1; num >= 0; num--)
				{
					Army army = armies[num];
					if (army.castle == null || flag)
					{
						RetreatDamage(army);
					}
				}
				if (flag)
				{
					RetreatDamage(settlement.garrison.units, 1);
				}
				PostRetreat();
				for (int num2 = armies.Count - 1; num2 >= 0; num2--)
				{
					if (num2 < armies.Count)
					{
						Army army2 = armies[num2];
						if (army2.castle == null)
						{
							Kingdom kingdom = army2.GetKingdom();
							army2.RetreatBattle();
							OnArmyRetreat(army2, num2 != 0 && kingdom != attacker_kingdom, side);
						}
					}
				}
				if (type == Type.BreakSiege && flag)
				{
					ResumeSiege();
				}
				if (!CheckVictory(apply_results: true, force: true))
				{
					SetBattleViewVictoryReason(VictoryReason.None, -1);
				}
				return;
			}
			if (type == Type.PlunderInterrupt)
			{
				if (ArmiesDefeated(side))
				{
					ResumePlunder();
				}
				else
				{
					SetStage(Stage.Ongoing);
				}
			}
		}
		PreRetreat();
		if (type != Type.Plunder && type != Type.PlunderInterrupt)
		{
			SetVictoryReason(VictoryReason.Retreat);
		}
		List<Army> armies2 = GetArmies(side);
		if (armies2 == null)
		{
			CheckVictory(apply_results: true, force: true);
			return;
		}
		for (int num3 = armies2.Count - 1; num3 >= 0; num3--)
		{
			Army army3 = armies2[num3];
			RetreatDamage(army3);
		}
		PostRetreat();
		for (int num4 = armies2.Count - 1; num4 >= 0; num4--)
		{
			if (num4 < armies2.Count)
			{
				Army army4 = armies2[num4];
				Kingdom kingdom2 = army4.GetKingdom();
				army4.RetreatBattle();
				OnArmyRetreat(army4, num4 != 0 && kingdom2 != attacker_kingdom, side);
			}
		}
		CheckVictory(apply_results: true, force: true);
	}

	private void OnArmyRetreat(Army army, bool supporter, int battle_side)
	{
		if (CheckVictory(apply_results: false))
		{
			return;
		}
		Kingdom kingdom = army.GetKingdom();
		if (battle_side == 0)
		{
			if (army.IsOwnStance(attacker_kingdom) || supporter)
			{
				FireEvent("army_retreat", army, kingdom.id);
			}
		}
		else if (army.IsOwnStance(defender_kingdom) || supporter)
		{
			FireEvent("army_retreat", army, kingdom.id);
		}
	}

	public int LeadBattleSide(Kingdom k)
	{
		if (k == attacker_kingdom)
		{
			return 0;
		}
		if (k == defender_kingdom)
		{
			return 1;
		}
		return -1;
	}

	public bool FairBattle(int player_side)
	{
		BattleSimulation.Totals potentialTotals = simulation.GetPotentialTotals(player_side);
		BattleSimulation.Totals potentialTotals2 = simulation.GetPotentialTotals(1 - player_side);
		if (potentialTotals.squad_count < def.min_enter_battle_squads_player)
		{
			return false;
		}
		if (potentialTotals2.squad_count < def.min_enter_battle_squads_ai)
		{
			return false;
		}
		float estimationPotential = simulation.GetEstimationPotential(0);
		float estimationPotential2 = simulation.GetEstimationPotential(1);
		if (estimationPotential < def.min_enter_battle_attacker_estimation)
		{
			return false;
		}
		if (estimationPotential2 < def.min_enter_battle_defender_estimation)
		{
			return false;
		}
		return true;
	}

	public void SetVictoryReason(VictoryReason reason)
	{
		if (victory_reason == VictoryReason.None)
		{
			victory_reason = reason;
		}
	}

	public void SetBattleViewVictoryReason(VictoryReason reason, int winner)
	{
		if (reason == VictoryReason.None)
		{
			battle_view_victory_reason = reason;
			battle_map_finished = false;
			this.winner = winner;
		}
		else if (battle_view_victory_reason == VictoryReason.None && !battle_map_only)
		{
			battle_view_victory_reason = reason;
			battle_map_finished = true;
			this.winner = winner;
			NotifyListeners("battle_view_finished");
		}
	}

	public void RetreatSupporters(int side, bool do_damage = false)
	{
		if (side < 0 || side > 1)
		{
			return;
		}
		List<Army> armies = GetArmies(side);
		if (armies == null || armies.Count < 1)
		{
			return;
		}
		Kingdom sideKingdom = GetSideKingdom(side);
		if (do_damage)
		{
			PreRetreat();
			for (int i = 0; i < armies.Count; i++)
			{
				Army army = armies[i];
				if (army.GetKingdom() != sideKingdom)
				{
					RetreatDamage(army);
				}
			}
			PostRetreat();
		}
		for (int j = 0; j < armies.Count; j++)
		{
			Army army2 = armies[j];
			if (army2.GetKingdom() != sideKingdom)
			{
				army2.RetreatBattle();
			}
		}
	}

	private void KickSupporter(int side)
	{
		List<Army> armies = GetArmies(side);
		if (armies != null && armies.Count >= 2)
		{
			armies[1].RetreatBattle();
			SendState<ArmiesState>();
		}
	}

	public void DoAction(string action, int side, string param = "")
	{
		if (side != 0 && side != 1)
		{
			Error("DoAction('" + action + "', " + side + ", '" + param + "'): invalid battle side");
			return;
		}
		if (!IsAuthority())
		{
			SendEvent(new ActionEvent(action, side, param));
			return;
		}
		switch (action)
		{
		case "assault":
			if (Assault())
			{
				OnBattleActionSelectedAnalytics(action);
			}
			break;
		case "break_siege":
			BreakSiege(BreakSiegeFrom.Inside);
			OnBattleActionSelectedAnalytics(action);
			break;
		case "enter_battle":
			EnterBattle((side == 0) ? attacker_kingdom.id : defender_kingdom.id, param);
			OnBattleActionSelectedAnalytics(action);
			break;
		case "leave_battle":
			OnBattleActionSelectedAnalytics(action);
			LeaveBattle((side == 0) ? attacker_kingdom.id : defender_kingdom.id);
			break;
		case "retreat":
			OnBattleActionSelectedAnalytics(action);
			Retreat(side);
			break;
		case "idle_leave_battle":
			OnBattleActionSelectedAnalytics(action);
			IdleLeaveBattle();
			break;
		case "retreat_supporters":
			OnBattleActionSelectedAnalytics(action);
			RetreatSupporters(side, do_damage: true);
			break;
		case "refuse_supporters":
			OnBattleActionSelectedAnalytics(action);
			KickSupporter(side);
			break;
		case "tactics":
			if (simulation != null)
			{
				if (side == 0)
				{
					simulation.attacker_tactics = param;
				}
				else
				{
					simulation.defender_tactics = param;
				}
			}
			OnBattleActionSelectedAnalytics(action + "(" + param + ")");
			break;
		}
	}

	public void OnBattleActionSelectedAnalytics(string battleAction, string evt = "analytics_battle_changed")
	{
		if (!IsValid() || GetRealm() == null || !IsAuthority() || !game.IsRunning())
		{
			return;
		}
		if (attacker == null || attacker_kingdom == null)
		{
			Game.Log("Invalid attacker kingdom!", Game.LogType.Warning);
			return;
		}
		if (attacker_kingdom.is_player)
		{
			bool flag = attacker_support != null && attacker_support.kingdom_id == attacker_kingdom.id;
			Vars param = CreateBaseAnalyticsVars(attacker.GetNid().ToString());
			FillAnalyticsArmyStats(attacker, flag ? attacker_support : null, settlement?.attacker_garrison, "player", param);
			FillAnalyticsArmyStats(flag ? null : attacker_support, null, null, "allied", param);
			FillAnalyticsArmyStats(GetArmy(1), defender_support, settlement?.garrison, "opponent", param);
			attacker_kingdom.FireEvent(evt, param, attacker_kingdom.id);
		}
		if (attacker_support != null)
		{
			Kingdom kingdom = attacker_support.GetKingdom();
			if (kingdom == null)
			{
				Game.Log("Invalid attacker support kingdom!", Game.LogType.Warning);
				return;
			}
			if (kingdom.is_player && kingdom.id != attacker_kingdom.id)
			{
				Vars param2 = CreateBaseAnalyticsVars(attacker_support.GetNid().ToString());
				FillAnalyticsArmyStats(attacker_support, null, null, "player", param2);
				FillAnalyticsArmyStats(attacker, null, settlement?.attacker_garrison, "allied", param2);
				FillAnalyticsArmyStats(GetArmy(1), defender_support, settlement?.garrison, "opponent", param2);
				kingdom.FireEvent(evt, param2, kingdom.id);
			}
		}
		if (defender == null || defender_kingdom == null)
		{
			Game.Log("Invalid defender kingdom!", Game.LogType.Warning);
			return;
		}
		if (defender_kingdom.is_player)
		{
			bool flag2 = defender_support != null && defender_support.kingdom_id == defender_kingdom.id;
			Vars param3 = CreateBaseAnalyticsVars((GetArmy(1) != null) ? GetArmy(1).GetNid().ToString() : defender.GetNid().ToString());
			FillAnalyticsArmyStats(GetArmy(1), flag2 ? defender_support : null, settlement?.garrison, "player", param3);
			FillAnalyticsArmyStats(flag2 ? null : defender_support, null, null, "allied", param3);
			FillAnalyticsArmyStats(attacker, attacker_support, settlement?.attacker_garrison, "opponent", param3);
			defender_kingdom.FireEvent(evt, param3, defender_kingdom.id);
		}
		if (defender_support != null)
		{
			Kingdom kingdom2 = defender_support.GetKingdom();
			if (kingdom2 == null)
			{
				Game.Log("Invalid defender support kingdom!", Game.LogType.Warning);
			}
			else if (kingdom2.is_player && kingdom2 != defender_kingdom)
			{
				Vars param4 = CreateBaseAnalyticsVars(defender_support.GetNid().ToString());
				FillAnalyticsArmyStats(defender_support, null, null, "player", param4);
				FillAnalyticsArmyStats(GetArmy(1), null, settlement?.garrison, "allied", param4);
				FillAnalyticsArmyStats(GetArmy(0), attacker_support, settlement?.attacker_garrison, "opponent", param4);
				kingdom2.FireEvent(evt, param4, kingdom2.id);
			}
		}
		Vars CreateBaseAnalyticsVars(string armyID)
		{
			Vars vars = new Vars();
			vars.Set("armyID", armyID);
			vars.Set("province", GetRealm().name);
			vars.Set("battleType", type.ToString());
			if (!string.IsNullOrEmpty(battleAction))
			{
				vars.Set("battleAction", battleAction);
			}
			return vars;
		}
	}

	public void OnReinforcementAnalytics(int side, Army reinforcementArmy)
	{
		if (reinforcementArmy != null && IsAuthority() && game.IsRunning())
		{
			if (attacker != null && attacker.GetKingdom().is_player)
			{
				Vars vars = CreateBaseAnalyticsVars();
				bool flag = reinforcementArmy.kingdom_id == attacker.kingdom_id;
				string val = ((side == 1) ? "enemy" : (flag ? "own" : "ally"));
				vars.Set("reinforcementStatus", val);
				attacker.GetKingdom().FireEvent("analytics_army_reinforced", vars, attacker_kingdom.id);
			}
			if (defender != null && defender.GetKingdom().is_player)
			{
				Vars vars2 = CreateBaseAnalyticsVars();
				bool flag2 = reinforcementArmy.kingdom_id == defender.kingdom_id;
				string val2 = ((side == 0) ? "enemy" : (flag2 ? "own" : "ally"));
				vars2.Set("reinforcementStatus", val2);
				defender.GetKingdom().FireEvent("analytics_army_reinforced", vars2, defender_kingdom.id);
			}
		}
		Vars CreateBaseAnalyticsVars()
		{
			Vars vars3 = new Vars();
			vars3.Set("province", GetRealm().name);
			vars3.Set("battleType", type.ToString());
			vars3.Set("reinforcementController", reinforcementArmy.GetKingdom().Name);
			vars3.Set("reinforcementArmyID", reinforcementArmy.GetNid().ToString());
			vars3.Set("reinforcementArmyPower", reinforcementArmy.GetManPower());
			vars3.Set("reinforcementArmyUnits", reinforcementArmy.units.Count);
			if (side == 0)
			{
				vars3.Set("reinforcedKingdom", attacker.GetKingdom().Name);
				vars3.Set("reinforcedArmyID", attacker.GetNid().ToString());
				int val3 = (attacker.GetManPower() + settlement?.attacker_garrison?.GetManPower()) ?? 0;
				vars3.Set("reinforcedArmyPower", val3);
				int val4 = (attacker.units.Count + settlement?.attacker_garrison?.units?.Count) ?? 0;
				vars3.Set("reinforcedArmyUnits", val4);
			}
			else if (side == 1)
			{
				vars3.Set("reinforcedKingdom", defender.GetKingdom().Name);
				vars3.Set("reinforcedArmyID", defender.GetNid().ToString());
				int val5 = GetArmy(side)?.GetManPower() ?? settlement?.garrison?.GetManPower() ?? 0;
				vars3.Set("reinforcedArmyPower", val5);
				int val6 = GetArmy(side)?.units.Count ?? settlement?.garrison?.units?.Count ?? 0;
				vars3.Set("reinforcedArmyUnits", val6);
			}
			return vars3;
		}
	}

	public void OnBattleEndedAnalytics(Object obj, AftermathOutcome outcome)
	{
		if (obj != null && obj.IsValid() && obj.GetKingdom().is_player && IsValid() && GetRealm() != null && IsAuthority() && game.IsRunning())
		{
			if (attackers == null || defenders == null)
			{
				Game.Log("Null attackers or defenders at the end of battle!", Game.LogType.Warning);
			}
			if (obj == attacker)
			{
				bool flag = attacker_support != null && attacker_support.kingdom_id == attacker.kingdom_id;
				Vars vars = CreateBaseAnalyticsVars(attacker.GetNid().ToString());
				FillAnalyticsArmyStats(attacker, flag ? attacker_support : null, settlement?.attacker_garrison, "player", vars, include_units_lost: true);
				FillAnalyticsArmyStats(flag ? null : attacker_support, null, null, "allied", vars, include_units_lost: true);
				FillAnalyticsArmyStats(GetArmy(1), defender_support, settlement?.garrison, "opponent", vars, include_units_lost: true);
				vars.Set("playerArmies", (!flag) ? 1 : 2);
				vars.Set("alliedArmies", (!flag) ? 1 : 0);
				vars.Set("opponentArmies", defenders.Count);
				attacker_kingdom.FireEvent("analytics_battle_ended", vars, attacker_kingdom.id);
			}
			else if (obj == attacker_support)
			{
				Vars vars2 = CreateBaseAnalyticsVars(attacker_support.GetNid().ToString());
				FillAnalyticsArmyStats(attacker_support, null, null, "player", vars2, include_units_lost: true);
				FillAnalyticsArmyStats(attacker, null, settlement?.attacker_garrison, "allied", vars2, include_units_lost: true);
				FillAnalyticsArmyStats(GetArmy(1), defender_support, settlement?.garrison, "opponent", vars2, include_units_lost: true);
				vars2.Set("playerArmies", 1);
				vars2.Set("alliedArmies", 1);
				vars2.Set("opponentArmies", defenders.Count);
				Kingdom kingdom = attacker_support.GetKingdom();
				kingdom.FireEvent("analytics_battle_ended", vars2, kingdom.id);
			}
			else if (obj == defender)
			{
				bool flag2 = defender_support != null && defender_support.kingdom_id == defender.kingdom_id;
				bool flag3 = GetArmy(1) != null;
				Vars vars3 = CreateBaseAnalyticsVars(flag3 ? GetArmy(1).GetNid().ToString() : defender.GetNid().ToString());
				FillAnalyticsArmyStats(GetArmy(1), flag2 ? defender_support : null, settlement?.garrison, "player", vars3, include_units_lost: true);
				FillAnalyticsArmyStats(flag2 ? null : defender_support, null, null, "allied", vars3, include_units_lost: true);
				FillAnalyticsArmyStats(attacker, attacker_support, settlement?.attacker_garrison, "opponent", vars3, include_units_lost: true);
				vars3.Set("playerArmies", (!(flag2 && flag3)) ? 1 : 2);
				vars3.Set("alliedArmies", (!(flag2 && flag3)) ? 1 : 0);
				vars3.Set("opponentArmies", attackers.Count);
				defender_kingdom.FireEvent("analytics_battle_ended", vars3, defender_kingdom.id);
			}
			else if (obj == defender_support)
			{
				Vars vars4 = CreateBaseAnalyticsVars(defender_support.GetNid().ToString());
				FillAnalyticsArmyStats(defender_support, null, null, "player", vars4, include_units_lost: true);
				FillAnalyticsArmyStats(GetArmy(1), null, settlement?.garrison, "allied", vars4, include_units_lost: true);
				FillAnalyticsArmyStats(GetArmy(0), attacker_support, settlement?.attacker_garrison, "opponent", vars4, include_units_lost: true);
				vars4.Set("playerArmies", 1);
				vars4.Set("alliedArmies", (GetArmy(1) != null) ? 1 : 0);
				vars4.Set("opponentArmies", attackers.Count);
				Kingdom kingdom2 = defender_support.GetKingdom();
				defender_support.FireEvent("analytics_battle_ended", vars4, kingdom2.id);
			}
		}
		Vars CreateBaseAnalyticsVars(string armyID)
		{
			Vars obj2 = new Vars();
			obj2.Set("armyID", armyID);
			obj2.Set("province", GetRealm().name);
			obj2.Set("battleType", type.ToString());
			obj2.Set("outcome", outcome.ToString());
			return obj2;
		}
	}

	private static void FillAnalyticsArmyStats(Army army, Army second_army, Garrison garrison, string prefix, Vars vars, bool include_units_lost = false)
	{
		if (string.IsNullOrEmpty(prefix) || vars == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int val = 0;
		int num3 = 0;
		if (army != null)
		{
			num += army.GetManPower();
			num2 += army.units.Count;
			val = (int)Math.Round(army.GetMorale());
			num3 += army.GetManpowerForSquads((Unit u) => u.num_dead());
		}
		if (second_army != null)
		{
			num += second_army.GetManPower();
			num2 += second_army.units.Count;
			num3 += second_army.GetManpowerForSquads((Unit u) => u.num_dead());
		}
		if (garrison != null)
		{
			num += garrison.GetManPower();
			num2 += garrison.units.Count;
			num3 += garrison.GetManpowerForSquads((Unit u) => u.num_dead());
		}
		vars.Set(prefix + "ArmyPower", num);
		vars.Set(prefix + "ArmyUnitCount", num2);
		vars.Set(prefix + "ArmyMorale", val);
		if (include_units_lost)
		{
			vars.Set(prefix + "ArmyPowerLost", num3);
		}
	}

	private void CalcBattleBonuses()
	{
		if (game.terrain_types != null && game.terrain_types.data != null && def.tt_grid_width > 0 && def.tt_grid_height > 0)
		{
			tt_grid = BuildTTGrid(game.terrain_types, def.tt_grid_width, def.tt_grid_height, position, out tt_x, out tt_y);
			CreateBattleBonuses();
		}
	}

	public static TerrainType[,] BuildTTGrid(TerrainTypesInfo info, int tt_grid_width, int tt_grid_height, Point position, out int tt_x, out int tt_y)
	{
		tt_x = 0;
		tt_y = 0;
		if (info == null || info.data == null)
		{
			return null;
		}
		if (tt_grid_width <= 0 || tt_grid_height <= 0)
		{
			return null;
		}
		TerrainType[,] array = new TerrainType[tt_grid_width, tt_grid_height];
		info.WorldToGrid(position, out var x, out var y);
		tt_x = x - tt_grid_width / 2;
		tt_y = y - tt_grid_height / 2;
		for (int i = 0; i < tt_grid_height; i++)
		{
			for (int j = 0; j < tt_grid_width; j++)
			{
				array[j, i] = info.GetTerrainType(tt_x + j, tt_y + i);
			}
		}
		return array;
	}

	private void CreateBattleBonuses()
	{
		if (!IsAuthority())
		{
			return;
		}
		if (battle_bonuses == null)
		{
			battle_bonuses = new List<BattleBonus.Def>();
		}
		DT.Def def = game.dt.FindDef("BattleBonus");
		if (def == null || def.defs == null || def.defs.Count < 1)
		{
			return;
		}
		for (int i = 0; i < def.defs.Count; i++)
		{
			DT.Field field = def.defs[i].field;
			if (!string.IsNullOrEmpty(field.base_path))
			{
				BattleBonus.Def def2 = game.defs.Get<BattleBonus.Def>(field.key);
				if (!battle_bonuses.Contains(def2) && def2.CanSpawn(this))
				{
					battle_bonuses.Add(def2);
				}
			}
		}
		SendState<BattleBonusesState>();
	}

	private void CreateSquad(BattleSimulation.Squad sim_squad)
	{
		if (sim_squad.sub_squads == null)
		{
			float damage = sim_squad.damage;
			float max_damage = sim_squad.max_damage;
			sim_squad.sub_squads = new List<BattleSimulation.Squad>();
			BattleSimulation.Squad squad = new BattleSimulation.Squad(sim_squad, damage, max_damage);
			new Squad(this, squad, Squad.CalcPosition(this, squad));
		}
	}

	private void CreateSquad(InventoryItem u)
	{
		BattleSimulation.Squad squad = u.simulation;
		if (squad?.sub_squads == null)
		{
			simulation.AddSiegeSquad(u);
			squad.sub_squads = new List<BattleSimulation.Squad>();
			BattleSimulation.Squad squad2 = new BattleSimulation.Squad(squad, 0f, 1f);
			new Squad(this, squad2, Squad.CalcPosition(this, squad2));
		}
	}

	public void CreateSquads(bool notify = true)
	{
		if (batte_view_game == null)
		{
			return;
		}
		if (simulation == null)
		{
			Error("no simulation");
			return;
		}
		for (int i = 0; i <= 1; i++)
		{
			List<BattleSimulation.Squad> list = simulation.GetSquads(i);
			for (int j = 0; j < list.Count; j++)
			{
				BattleSimulation.Squad squad = list[j];
				if (!squad.IsDefeated() && !squad.def.is_siege_eq)
				{
					CreateSquad(squad);
				}
			}
			List<InventoryItem> equipment = simulation.GetEquipment(i);
			for (int k = 0; k < equipment.Count; k++)
			{
				InventoryItem inventoryItem = equipment[k];
				if (inventoryItem.def.item_type == Unit.ItemType.SiegeEquipment)
				{
					CreateSquad(inventoryItem);
				}
			}
		}
		if (notify)
		{
			for (int l = 0; l <= 1; l++)
			{
				GetArmy(l)?.NotifyListeners("units_changed");
			}
		}
	}

	public void CombineSquads()
	{
		for (int i = 0; i <= 1; i++)
		{
			List<BattleSimulation.Squad> list = simulation.GetSquads(i);
			bool flag = LeadersKilled(i);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				BattleSimulation.Squad squad = list[num];
				if (squad.sub_squads != null)
				{
					int num2 = (int)Math.Ceiling((float)squad.unit.max_size_modified() * squad.unit.def.bv_squad_size_multiplier);
					int num3 = 0;
					bool flag2 = true;
					for (int j = 0; j < squad.sub_squads.Count; j++)
					{
						BattleSimulation.Squad squad2 = squad.sub_squads[j];
						if (squad2 != null)
						{
							int num4 = squad2.NumTroops();
							if (num4 > 0 && squad2.state != BattleSimulation.Squad.State.Fled && squad2.state != BattleSimulation.Squad.State.Stuck)
							{
								flag2 = false;
							}
							num3 += num4;
						}
					}
					squad.damage = Math.Max(0f, 1f - (float)num3 / (float)num2);
					int num5 = squad.NumTroops();
					int num6 = squad.def.min_troops_in_battle();
					if (squad.damage >= 1f || num5 <= num6)
					{
						squad.SetState(BattleSimulation.Squad.State.Dead);
						squad.damage = 1f;
						squad.killed_in_bv = true;
					}
					else if (flag2 || (i != winner && battle_view_victory_reason == VictoryReason.Surrender) || flag)
					{
						squad.SetState(BattleSimulation.Squad.State.Fled, 0f);
					}
					squad.unit.SetDamage(squad.damage);
					squad.sub_squads = null;
				}
			}
			List<InventoryItem> equipment = simulation.GetEquipment(i);
			for (int num7 = equipment.Count - 1; num7 >= 0; num7--)
			{
				InventoryItem inventoryItem = equipment[num7];
				simulation.DelSiegeSquad(inventoryItem);
				if (inventoryItem.simulation.IsDefeated() && inventoryItem.army != null)
				{
					inventoryItem.simulation = null;
					inventoryItem.army.DelInvetoryItem(inventoryItem);
					equipment.Remove(inventoryItem);
				}
			}
		}
	}

	public Kingdom GetSideKingdom(int side)
	{
		if (side == 0)
		{
			return attacker_kingdom;
		}
		return defender_kingdom;
	}

	public void SetAttackerKingdom(Army a)
	{
		attacker_kingdom = a.GetKingdom();
	}

	public void SetDefenderKingdom(Settlement s)
	{
		if (s.keep_effects != null && s.keep_effects.CanBeTakenOver())
		{
			defender_kingdom = s.keep_effects.GetController().GetKingdom();
		}
		else
		{
			defender_kingdom = s.GetRealm().controller.GetKingdom();
		}
	}

	public void SetDefenderKingdom(Army a)
	{
		defender_kingdom = a.GetKingdom();
	}

	public void CalcPreparationTime()
	{
		if (simulation != null)
		{
			float num = 0f;
			float num2 = 0f;
			if (simulation.attacker_totals_potential.count > simulation.defender_totals_potential.count)
			{
				num = simulation.attacker_totals_potential.count;
				num2 = simulation.defender_totals_potential.count;
			}
			else
			{
				num2 = simulation.attacker_totals_potential.count;
				num = simulation.defender_totals_potential.count;
			}
			Vars vars = new Vars();
			vars.Set("stronger_army_manpower", num);
			vars.Set("weaker_army_manpower", num2);
			vars.Set("preparation_time_base", def.preparation_time_base);
			preparation_time_cached = def.preparation_time.Float(vars);
		}
	}

	public void SetStage(Stage stage, bool send_state = true, float elapsed = 0f)
	{
		if (stage == this.stage && stage_time != Time.Zero)
		{
			return;
		}
		player_chosen_tactics = false;
		string param = this.stage.ToString();
		this.stage = stage;
		if (send_state)
		{
			CalcPreparationTime();
		}
		stage_time = game.time - elapsed;
		RefreshInitiative();
		if (simulation != null)
		{
			simulation.OnRestart();
		}
		if (stage == Stage.Ongoing)
		{
			can_assault = CanAssault();
			if (batte_view_game != null)
			{
				StartReinforcementTimers(force: true);
			}
		}
		if (stage == Stage.Preparing)
		{
			retreated = false;
			idle_leaving_battle = false;
		}
		if (send_state)
		{
			SendState<StageState>();
		}
		NotifyListeners("stage_changed", param);
		settlement?.GetRealm()?.NotifyListeners("battle_stage_changed", param);
		for (int i = 0; i <= 1; i++)
		{
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				Army army = armies[j];
				if (army.battle != null)
				{
					if (!IsReinforcement(army))
					{
						AdjustArmyPosition(army);
					}
					army.NotifyListeners("battle_stage_changed", param);
					army.leader?.NotifyListeners("battle_stage_changed", param);
				}
			}
		}
	}

	public void RefreshPlunderProgress()
	{
		bool flag = false;
		if (is_plunder && plunder_progress == null)
		{
			plunder_progress = new ComputableValue(0f, 1f, game, 0f, def.duration);
			flag = true;
		}
		if (plunder_progress != null)
		{
			float rate = plunder_progress.GetRate();
			if (type == Type.Plunder)
			{
				plunder_progress.Set(plunder_progress.Get() * def.resume_plunder_progress_mod);
				plunder_progress.SetRate(1f);
			}
			else
			{
				plunder_progress.SetRate(0f);
			}
			if (rate != plunder_progress.GetRate() || flag)
			{
				SendState<PlunderProgressState>();
			}
		}
	}

	protected override void OnStart()
	{
		CalcBattleBonuses();
		if (IsAuthority())
		{
			for (int i = 0; i <= 1; i++)
			{
				List<Army> armies = GetArmies(i);
				for (int j = 0; j < armies.Count; j++)
				{
					Army army = armies[j];
					if (army == null)
					{
						game.Warning("Missing army in " + this);
						continue;
					}
					army.battle_side = i;
					army.is_supporter = false;
					army.SetBattle(this);
					if (!IsReinforcement(army))
					{
						AdjustArmyPosition(army);
					}
				}
			}
		}
		InitSiegeComponents();
		if (IsAuthority())
		{
			FillVars();
			OnBattleActionSelectedAnalytics(null, "analytics_battle_started");
		}
		UpdateInBatch(game.update_1sec);
		base.OnStart();
	}

	private void InitSiegeComponents()
	{
		if (!IsFinishing() && type == Type.Siege)
		{
			if (siege_defense_drop_component == null)
			{
				siege_defense_drop_component = new SiegeDefenseDrop(this);
			}
			siege_defense_drop_component.Begin();
			if (resil_drop_component == null)
			{
				resil_drop_component = new ResilienceDrop(this);
			}
			resil_drop_component.Begin();
		}
	}

	public bool CheckBrokenBattle()
	{
		if (!IsAuthority())
		{
			return false;
		}
		if (stage == Stage.Finishing)
		{
			if (batte_view_game == null && IsValid())
			{
				Error("Battle that has finished but hasn't been destroyed, check player log");
				for (int i = 0; i < 2; i++)
				{
					List<Army> armies = GetArmies(i);
					for (int num = armies.Count - 1; num >= 0; num--)
					{
						armies[num].Destroy();
					}
				}
				if (settlement != null)
				{
					settlement.SetBattle(null);
				}
				Destroy();
				return true;
			}
		}
		else
		{
			bool flag = false;
			bool flag2 = false;
			for (int j = 0; j < 2; j++)
			{
				MapObject obj = ((j != 0) ? attacker : defender);
				List<Army> armies2 = GetArmies(j);
				for (int num2 = armies2.Count - 1; num2 >= 0; num2--)
				{
					Army army = armies2[num2];
					if (!army.IsValid())
					{
						flag = true;
					}
					else if (!army.IsEnemy(obj))
					{
						Error($"{army} is not enemy of opposing side in battle");
						army.LeaveBattle();
					}
					else
					{
						for (int k = 0; k < army.units.Count; k++)
						{
							if (army.units[k].simulation == null)
							{
								flag2 = true;
							}
						}
					}
				}
			}
			if (flag2)
			{
				Error("Army in ongoing battle has units not in the battle");
				for (int l = 0; l < 2; l++)
				{
					List<Army> armies3 = GetArmies(l);
					for (int num3 = armies3.Count - 1; num3 >= 0; num3--)
					{
						Army army2 = armies3[num3];
						if (army2.IsValid())
						{
							army2.Destroy();
						}
					}
				}
				if (settlement != null)
				{
					settlement.SetBattle(null);
				}
				Destroy();
				return true;
			}
			if (flag)
			{
				Error("Destroyed army in ongoing battle");
				for (int m = 0; m < 2; m++)
				{
					List<Army> armies4 = GetArmies(m);
					for (int num4 = armies4.Count - 1; num4 >= 0; num4--)
					{
						Army army3 = armies4[num4];
						if (army3.IsValid())
						{
							army3.Destroy();
						}
					}
				}
				if (settlement != null)
				{
					settlement.SetBattle(null);
				}
				Destroy();
				return true;
			}
		}
		return false;
	}

	public void ApplyPowerGrids()
	{
		if (power_grids != null && power_grids.Length != 0)
		{
			for (int i = 0; i < 2; i++)
			{
				power_grids[i]?.Apply();
			}
		}
	}

	public override void OnUpdate()
	{
		if (CheckBrokenBattle())
		{
			return;
		}
		GarbageCollectIntendedReinforcements();
		if (stage == Stage.Ongoing)
		{
			bool flag = CanAssault();
			if (flag && !can_assault)
			{
				NotifyListeners("can_assault");
			}
			can_assault = flag;
		}
		if (!IsAuthority())
		{
			return;
		}
		if (stage == Stage.EnteringBattle)
		{
			if (batte_view_game == null)
			{
				return;
			}
			SetStage(Stage.Ongoing);
		}
		UpdateCastleFoodRate();
		if (stage == Stage.Preparing && game.time - stage_time > preparation_time_cached)
		{
			SetStage(Stage.Ongoing);
		}
		if (IsValid() && !IsFinishing())
		{
			CheckVictory();
		}
	}

	public override string ToString()
	{
		Kingdom kingdom = attacker_kingdom;
		Kingdom kingdom2 = defender_kingdom;
		string text = base.ToString();
		string text2 = ((kingdom == null) ? "unknown" : kingdom.Name);
		string text3 = ((kingdom2 == null) ? "unknown" : kingdom2.Name);
		return text + $" [{stage}] {type} ({text2} vs {text3})";
	}

	private int WarScoreUnitsKilled(War.Def def, int side)
	{
		int num = 0;
		List<Army> armies = GetArmies(side);
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			for (int j = 0; j < army.units.Count; j++)
			{
				Unit unit = army.units[j];
				if (unit.IsDefeated())
				{
					num += def.squad_destroyed_base_score + def.squad_destroyed_level_score * (unit.def.tier + 1);
				}
			}
		}
		return num;
	}

	private int WarScoreProvinceTaken(War.Def def)
	{
		int num = def.province_taken_base_score;
		int province_taken_building_score = def.province_taken_building_score;
		Castle castle = settlement as Castle;
		for (int i = 0; i < castle.buildings.Count; i++)
		{
			if (castle.buildings[i] != null)
			{
				num += province_taken_building_score;
			}
		}
		return (int)((float)num * (castle.GetRealm().IsCore() ? def.core_province_mult : 1f));
	}

	public static int WarScoreKnightNeutralized(War.Def def, Character character)
	{
		int num = def.neutralized_knight_base_score + def.neutralized_knight_level_score * character.GetClassLevel();
		if (character.IsPrince())
		{
			num *= def.royalty_mod_prince;
		}
		else if (character.IsKing())
		{
			num *= def.royalty_mod_king;
		}
		return num;
	}

	private void AddExperience(bool attacker_won)
	{
		if (can_add_experience)
		{
			can_add_experience = false;
			AddUnitsKilled();
			for (int i = 0; i < 2; i++)
			{
				AddExperience(i, (attacker_won && i == 0) || (!attacker_won && i == 1));
			}
		}
	}

	private void AddUnitsKilled()
	{
		for (int i = 0; i < 2; i++)
		{
			List<Army> armies = GetArmies(i);
			for (int j = 0; j < armies.Count; j++)
			{
				AddTroopsKilled(armies[j], i);
			}
		}
	}

	private void AddTemporaryMorale(Army army, bool won)
	{
		if (is_plunder && army.battle_side == 0 && won)
		{
			army.morale.AddTemporaryMorale(army.morale.def.morale_on_plunder_won_as_attacker);
			return;
		}
		Realm realm_in = army.realm_in;
		if (realm_in != null && realm_in.IsAllyOrOwn(army))
		{
			if (won)
			{
				army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_won_own_or_ally);
			}
			else
			{
				army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_lost_own_or_ally);
			}
		}
		else if (realm_in != null && realm_in.IsEnemy(army))
		{
			if (won)
			{
				army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_won_enemy);
			}
			else
			{
				army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_lost_enemy);
			}
		}
		else if (won)
		{
			army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_won_neutral);
		}
		else
		{
			army.morale.AddTemporaryMorale(army.morale.def.morale_on_battle_lost_neutral);
		}
	}

	private void AddExperience(int side, bool won, float mod = 1f)
	{
		float num = CalcExperience(side) * mod;
		if (num <= 0f)
		{
			return;
		}
		List<Army> armies = GetArmies(side);
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			for (int j = 0; j < army.units.Count; j++)
			{
				Unit unit = army.units[j];
				if (unit != null && !(unit.damage >= 1f) && unit.def.type != Unit.Type.Noble)
				{
					unit.AddExperience(num);
				}
			}
		}
	}

	private float CalcExperience(int side)
	{
		int num = 1 - side;
		GetArmies(side);
		GetArmies(num);
		int num2 = starting_squads[side];
		int num3 = starting_squads[num];
		float num4 = starting_troops[side];
		if (num4 == 0f)
		{
			return 0f;
		}
		float num5 = starting_troops[num];
		float num6 = troops_killed[num];
		float num7 = 1f + (float)(num2 + num3) * def.experience_per_squad_mod;
		float num8 = 1f;
		if (num5 > 0f)
		{
			num8 = num6 / num5;
		}
		float num9 = num5 / num4;
		return def.base_experience + def.experience_gain_speed * num7 * num8 * num9;
	}

	private int DefeatedArmiesCount()
	{
		return winner switch
		{
			0 => defenders.Count, 
			1 => attackers.Count, 
			_ => 0, 
		};
	}

	public override float GetReservationRadius()
	{
		return 10f;
	}

	public override float GetRadius()
	{
		return def.tents_range * 0.5f + 4f;
	}

	public override bool CanReserve()
	{
		return true;
	}

	public override bool IgnoreCollision(MapObject other)
	{
		return false;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "realm":
			return GetRealm();
		case "town_guards":
			return CalcTownGuards();
		case "province_guards":
			return settlement.GetRealm().income[ResourceType.TownGuards];
		case "settlement_guards":
			return settlement.GetResources()[ResourceType.TownGuards];
		case "is_preparing":
			return stage == Stage.Preparing;
		case "is_ongoing":
			return stage == Stage.Ongoing;
		case "is_finished":
			return IsFinishing();
		case "is_open_field":
			return type == Type.OpenField;
		case "is_plunder":
			return type == Type.Plunder;
		case "is_siege":
			return type == Type.Siege;
		case "is_assault":
			return type == Type.Assault;
		case "is_break_siege":
			return type == Type.BreakSiege;
		case "is_naval":
			return type == Type.Naval;
		case "is_plunder_interrupt":
			return type == Type.PlunderInterrupt;
		case "is_battle_view":
			return batte_view_game != null;
		case "stage":
			return stage.ToString();
		case "defeated_armies_count":
			return DefeatedArmiesCount();
		case "winner_kingdom":
			return GetWinnerKingdom();
		case "loser_kingdom":
			return GetLoserKingdom();
		case "attacker_kingdom":
			return attacker_kingdom;
		case "defender_kingdom":
			return defender_kingdom;
		case "attacker_support":
			return attacker_support;
		case "defender_support":
			return defender_support;
		case "cancelled_by":
			return cancelled_by;
		case "settlement":
			return settlement;
		case "castle":
			return settlement as Castle;
		case "realm_in":
			return game.GetRealm(realm_id);
		case "resilience":
			return resilience;
		case "initial_resilience_pre_condition":
			return initial_resilience_pre_condition;
		case "siege_defense":
			return siege_defense;
		case "initial_siege_defense_pre_condition":
			return initial_siege_defense_pre_condition;
		case "attacker_morale":
			if (simulation == null)
			{
				return 0;
			}
			return simulation.OverallMorale(0);
		case "defender_morale":
			if (simulation == null)
			{
				return 0;
			}
			return simulation.OverallMorale(1);
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override Value GetDumpStateValue()
	{
		return Value.Null;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("type", type.ToString());
		dump.Append("attacker", attacker?.ToString());
		dump.Append("defender", defender?.ToString());
		dump.Append("settlement", settlement?.ToString());
		dump.Append("simulation", simulation?.ToString());
		dump.Append("stage", stage.ToString());
		dump.Append("winner", winner);
		dump.Append("victory_reason", victory_reason.ToString());
		dump.Append("initial_siege_defense_pre_condition", initial_siege_defense_pre_condition);
		dump.Append("initial_resilience_pre_condition", initial_resilience_pre_condition);
		dump.Append("initial_siege_defense", initial_siege_defense);
		dump.Append("initial_resilience", initial_resilience);
		dump.Append("siege_defense", siege_defense);
		dump.Append("resilience", resilience);
		dump.Append("attacker_support", attacker_support?.ToString());
		dump.Append("defender_support", defender_support?.ToString());
		dump.Append("is_plunder", is_plunder.ToString());
	}

	public void Restart()
	{
		has_restarted = true;
		SetStage(Stage.Finishing);
		has_exit_city = false;
		has_ladder_placed = false;
		winner = -1;
		SendState<ArmiesState>();
		FillVars();
		SetVictoryReason(VictoryReason.None);
		SetBattleViewVictoryReason(VictoryReason.None, -1);
		ClearOldLogic();
		for (int i = 0; i < reinforcements.Length; i++)
		{
			Reinforcement reinforcement = reinforcements[i];
			if (reinforcement.army != null && (GetArmy(i % 2) == reinforcement.army || GetSupporter(i % 2) == reinforcement.army))
			{
				Leave(reinforcement.army, check_victory: false, is_restart: true);
			}
			Reinforcement reinforcement2 = reinforcements_at_start_of_battleview[i];
			SetReinforcements(reinforcement2.army, i, reinforcement2.estimate_time, force: true);
		}
		bv_attackers = attackers.Count;
		bv_defenders = defenders.Count;
		RestartAI();
		NotifyListeners("Restart");
	}

	private void RestartAI()
	{
		BattleAI[] array = ai;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnRestart();
		}
	}

	public void OnSquadExitCity(int battle_side)
	{
		if (is_siege && fortifications != null && fortifications.Count != 0 && !IsFinishing() && !battle_map_finished && battle_side != 0 && !has_exit_city && stage == Stage.Ongoing)
		{
			has_exit_city = true;
			NotifyListeners("squad_exit_city", battle_side);
		}
	}

	public void OnSquadPlaceLadder(int battle_side)
	{
		if (!IsFinishing() && !battle_map_finished)
		{
			NotifyListeners("squad_ladder_placed", battle_side);
		}
	}

	public Battle(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public new static Object Create(Multiplayer multiplayer)
	{
		return new Battle(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		states.Pop<ArmiesState>()?.ApplyTo(this);
		base.Load(states);
	}
}
