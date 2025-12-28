using System;
using System.Collections.Generic;
using Logic.ExtensionMethods;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Army)]
public class Army : MapObject, IListener
{
	public class Def : Logic.Def
	{
		public float levy_troop_bonus = 1f;

		public int max_units = 5;

		public int marshal_max_units = 8;

		public int shaman_max_units = 8;

		public int king_max_units = 8;

		public int crusader_max_units = 8;

		public int max_inventory = 1;

		public int king_max_inventory = 2;

		public int marshal_max_inventory = 2;

		public float world_move_speed = 2.25f;

		public float ship_speed_mod = 1.5f;

		public float ship_rotation_speed = 16f;

		public float world_move_speed_neighbor_realm_mod = 0.9f;

		public float world_move_speed_far_realm_mod = 0.8f;

		public int thread_columns = 2;

		public int max_thread_rows = 8;

		public float thread_spacing = 0.5f;

		public float min_marching_dist = 10f;

		public float min_unit_speed_mul = 1f;

		public float max_unit_speed_mul = 2f;

		public float water_enter_duration = 5f;

		public float water_enter_duration_fast = 1f;

		public float water_exit_duration = 5f;

		public float water_exit_duration_fast = 1f;

		public float river_crossing_duration = 2f;

		public float teleport_area_duration = 2f;

		public float enter_ocean_prediction_distance = 5f;

		public float units_healed_post_battle = 20f;

		public float max_units_healed_post_battle = 75f;

		public float defeated_heal_mod_perc = 50f;

		public float army_surrender_marshal_death = 50f;

		public float flee_distance = 25f;

		public bool can_interrupt_flee;

		public float supplies_capacity_base = 100f;

		public float supplies_consumption_interval = 5f;

		public Dictionary<string, float> supplies_consumption;

		public float supplies_consumption_default = 0.1f;

		public float supplies_troop_min = 1000f;

		public float supplies_troop_max = 10000f;

		public float supplies_min_mod_per_troops = 1f;

		public float supplies_max_mod_per_troops = 2f;

		public float bordercross_relationship_treshold;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			levy_troop_bonus = field.GetFloat("levy_troop_bonus", null, levy_troop_bonus);
			max_units = field.GetInt("max_units", null, max_units);
			marshal_max_units = field.GetInt("max_units.marshal", null, marshal_max_units);
			shaman_max_units = field.GetInt("max_units.shaman", null, shaman_max_units);
			king_max_units = field.GetInt("max_units.king", null, king_max_units);
			crusader_max_units = field.GetInt("max_units.crusader", null, crusader_max_units);
			max_inventory = field.GetInt("max_invetory", null, max_inventory);
			marshal_max_inventory = field.GetInt("max_inventory.marshal", null, marshal_max_inventory);
			king_max_inventory = field.GetInt("max_inventory.king", null, king_max_inventory);
			world_move_speed = field.GetFloat("world_move_speed", null, world_move_speed);
			world_move_speed_neighbor_realm_mod = field.GetFloat("world_move_speed_neighbor_realm_mod", null, world_move_speed_neighbor_realm_mod);
			world_move_speed_far_realm_mod = field.GetFloat("world_move_speed_far_realm_mod", null, world_move_speed_far_realm_mod);
			ship_speed_mod = field.GetFloat("ship_speed_mod", null, ship_speed_mod);
			ship_rotation_speed = field.GetFloat("ship_rotation_speed", null, ship_rotation_speed);
			battle_cols = field.GetInt("battle_columns", null, battle_cols);
			thread_columns = field.GetInt("thread_columns", null, thread_columns);
			max_thread_rows = field.GetInt("max_thread_rows", null, max_thread_rows);
			thread_spacing = field.GetFloat("thread_spacing", null, thread_spacing);
			min_marching_dist = field.GetFloat("min_marching_dist", null, min_marching_dist);
			min_unit_speed_mul = field.GetFloat("min_unit_speed_mul", null, min_unit_speed_mul);
			max_unit_speed_mul = field.GetFloat("max_unit_speed_mul", null, max_unit_speed_mul);
			water_exit_duration = field.GetFloat("water_exit_duration", null, water_exit_duration);
			water_exit_duration_fast = field.GetFloat("water_exit_duration_fast", null, water_exit_duration_fast);
			river_crossing_duration = field.GetFloat("river_crossing_duration", null, river_crossing_duration);
			teleport_area_duration = field.GetFloat("teleport_area_duration", null, teleport_area_duration);
			enter_ocean_prediction_distance = field.GetFloat("enter_ocean_prediction_distance", null, enter_ocean_prediction_distance);
			units_healed_post_battle = field.GetFloat("units_healed_post_battle", null, units_healed_post_battle);
			max_units_healed_post_battle = field.GetFloat("max_units_healed_post_battle", null, max_units_healed_post_battle);
			defeated_heal_mod_perc = field.GetFloat("defeated_heal_mod_perc", null, defeated_heal_mod_perc);
			army_surrender_marshal_death = field.GetFloat("army_surrender_marshal_death ", null, army_surrender_marshal_death);
			flee_distance = field.GetFloat("flee_distance", null, flee_distance);
			can_interrupt_flee = field.GetBool("can_interrupt_flee", null, can_interrupt_flee);
			supplies_capacity_base = field.GetFloat("supplies_capacity_base", null, supplies_capacity_base);
			supplies_consumption_interval = field.GetFloat("supplies_consumption_interval", null, supplies_consumption_interval);
			supplies_troop_min = field.GetFloat("supplies_troop_min", null, supplies_troop_min);
			supplies_troop_max = field.GetFloat("supplies_troop_max", null, supplies_troop_max);
			supplies_min_mod_per_troops = field.GetFloat("supplies_min_mod_per_troops", null, supplies_min_mod_per_troops);
			supplies_max_mod_per_troops = field.GetFloat("supplies_max_mod_per_troops", null, supplies_max_mod_per_troops);
			LoadsuppliesConsuption(field.FindChild("supplies_consumption"));
			bordercross_relationship_treshold = field.GetFloat("bordercross_relationship_treshold", null, bordercross_relationship_treshold);
			return true;
		}

		private void LoadsuppliesConsuption(DT.Field f)
		{
			if (f != null)
			{
				supplies_consumption_default = f.Float(null, supplies_consumption_default);
				supplies_consumption = new Dictionary<string, float>();
				for (int i = 0; i < f.children.Count; i++)
				{
					supplies_consumption[f.children[i].key] = f.GetFloat(f.children[i].key);
				}
			}
		}

		public int GetMaxUnits(Character leader, bool assume_marshal = false)
		{
			if (leader == null)
			{
				if (!assume_marshal)
				{
					return max_units;
				}
				return marshal_max_units;
			}
			if (leader.IsCrusader())
			{
				return crusader_max_units;
			}
			if (leader.IsMarshal())
			{
				return marshal_max_units;
			}
			if (leader.IsKing())
			{
				return king_max_units;
			}
			if (leader.IsCleric() && leader.GetKingdom().is_pagan)
			{
				return shaman_max_units;
			}
			return max_units;
		}
	}

	[Serialization.State(21)]
	public class LeaderState : Serialization.ObjectState
	{
		private NID leader_nid;

		public static LeaderState Create()
		{
			return new LeaderState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Army).leader != null;
		}

		public override bool InitFrom(Object obj)
		{
			Character leader = (obj as Army).leader;
			leader_nid = leader;
			return leader != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(leader_nid, "leader_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			leader_nid = ser.ReadNID<Character>("leader_nid");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Army).SetLeader(leader_nid.Get<Character>(obj.game), send_state: false);
		}
	}

	[Serialization.State(22)]
	public class CastleState : Serialization.ObjectState
	{
		private NID castle_nid;

		public static CastleState Create()
		{
			return new CastleState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Army).castle != null;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = (obj as Army).castle;
			castle_nid = castle;
			return castle != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Castle>(castle_nid, "castle_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			castle_nid = ser.ReadNID<Castle>("castle_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			Castle castle = army.castle;
			army.castle = castle_nid.Get<Castle>(army.game);
			if (army.castle != null)
			{
				army.is_in_water = false;
				army.NotifyListeners("enter_castle");
			}
			else
			{
				army.NotifyListeners("leave_castle", castle);
			}
			castle?.GetRealm()?.rebellionRisk.Recalc();
			army.castle?.GetRealm()?.rebellionRisk.Recalc();
		}
	}

	[Serialization.State(23)]
	public class BattleState : Serialization.ObjectState
	{
		private NID battle_nid;

		public int battle_side = -1;

		public static BattleState Create()
		{
			return new BattleState();
		}

		public static bool IsNeeded(Object obj)
		{
			Army army = obj as Army;
			Battle battle = army.battle;
			if (battle != null && battle.batte_view_game != null && battle.IsReinforcement(army))
			{
				return false;
			}
			return battle != null;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			Battle battle = army.battle;
			battle_nid = battle;
			battle_side = army.battle_side;
			return battle != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Battle>(battle_nid, "battle_nid");
			if (battle_nid.nid != 0)
			{
				ser.Write7BitUInt(battle_side, "battle_side");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			battle_nid = ser.ReadNID<Battle>("battle_nid");
			if (battle_nid.nid != 0)
			{
				battle_side = ser.Read7BitUInt("battle_side");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			Battle battle = battle_nid.Get<Battle>(army.game);
			army.battle_side = battle_side;
			army.SetBattle(battle, set_reinforcement: false, send_state: false);
		}
	}

	[Serialization.State(24)]
	public class UnitsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class UnitState : Serialization.ObjectSubstate
		{
			public float damage;

			public int level;

			public float experience;

			public bool mercenary;

			public UnitState()
			{
			}

			public UnitState(int idx, Unit unit)
			{
				substate_index = idx;
				damage = unit.damage;
				level = unit.level;
				experience = unit.experience;
				mercenary = unit.mercenary;
			}

			public static UnitState Create()
			{
				return new UnitState();
			}

			public static bool IsNeeded(Object obj)
			{
				return (obj as Army).units.Count > 0;
			}

			public override bool InitFrom(Object obj)
			{
				Army army = obj as Army;
				if (army.units == null || army.units.Count == 0)
				{
					return false;
				}
				Unit unit = army.units[substate_index];
				damage = unit.damage;
				level = unit.level;
				experience = unit.experience;
				mercenary = unit.mercenary;
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteFloat(damage, "damage");
				ser.Write7BitUInt(level, "level");
				ser.WriteFloat(experience, "experience");
				ser.WriteBool(mercenary, "mercenary");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				damage = ser.ReadFloat("damage");
				level = ser.Read7BitUInt("level");
				experience = ser.ReadFloat("experience");
				mercenary = ser.ReadBool("mercenary");
			}

			public override void ApplyTo(Object obj)
			{
				Army army = obj as Army;
				if (substate_index < 0 || substate_index >= army.units.Count)
				{
					Game.Log("Error applying unit damage state #" + substate_index + " / " + army.units.Count + " to " + army.ToString(), Game.LogType.Error);
				}
				else
				{
					Unit unit = army.units[substate_index];
					unit.SetDamage(damage, send_state: false);
					unit.experience = experience;
					unit.level = level;
					unit.mercenary = mercenary;
					unit.garrison = null;
				}
			}
		}

		private List<string> unit_defs = new List<string>();

		private List<bool> mercs = new List<bool>();

		private List<string> item_defs = new List<string>();

		private List<Coord> rowcols = new List<Coord>();

		public static UnitsState Create()
		{
			return new UnitsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Army army = obj as Army;
			if (army.units.Count <= 0)
			{
				return army.siege_equipment.Count > 0;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			int num = 0;
			bool flag = army.battle?.batte_view_game != null;
			for (int i = 0; i < army.units.Count; i++)
			{
				Unit unit = army.units[i];
				if (!flag || unit.simulation == null || !unit.simulation.temporary)
				{
					num++;
					unit_defs.Add(unit.def.dt_def.path);
					mercs.Add(unit.mercenary);
					rowcols.Add(new Coord(unit.battle_row, unit.battle_col));
					AddSubstate(new UnitState(i, unit));
				}
			}
			int count = army.siege_equipment.Count;
			for (int j = 0; j < count; j++)
			{
				InventoryItem inventoryItem = army.siege_equipment[j];
				item_defs.Add(inventoryItem.def.dt_def.path);
			}
			if (num <= 0)
			{
				return count > 0;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = unit_defs.Count;
			ser.Write7BitUInt(count, "unit_count");
			for (int i = 0; i < count; i++)
			{
				string val = unit_defs[i];
				ser.WriteStr(val, "unit_def_id_", i);
			}
			for (int j = 0; j < count; j++)
			{
				bool val2 = mercs[j];
				ser.WriteBool(val2, "unit_merc_", j);
			}
			for (int k = 0; k < count; k++)
			{
				Coord coord = rowcols[k];
				ser.Write7BitSigned(coord.x, "row_", k);
				ser.Write7BitSigned(coord.y, "col_", k);
			}
			int count2 = item_defs.Count;
			ser.Write7BitUInt(count2, "item_count");
			for (int l = 0; l < count2; l++)
			{
				string val3 = item_defs[l];
				ser.WriteStr(val3, "item_def_id_", l);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("unit_count");
			for (int i = 0; i < num; i++)
			{
				string item = ser.ReadStr("unit_def_id_", i);
				unit_defs.Add(item);
			}
			for (int j = 0; j < num; j++)
			{
				bool item2 = ser.ReadBool("unit_merc_", j);
				mercs.Add(item2);
			}
			for (int k = 0; k < num; k++)
			{
				int x = ser.Read7BitSigned("row_", k);
				int y = ser.Read7BitSigned("col_", k);
				rowcols.Add(new Coord(x, y));
			}
			int num2 = ser.Read7BitUInt("item_count");
			for (int l = 0; l < num2; l++)
			{
				string item3 = ser.ReadStr("item_def_id_", l);
				item_defs.Add(item3);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			for (int i = 0; i < army.units.Count; i++)
			{
				army.units[i].army = null;
			}
			army.units.Clear();
			int count = unit_defs.Count;
			for (int j = 0; j < count; j++)
			{
				string text = unit_defs[j];
				Unit.Def def = army.game.defs.Get<Unit.Def>(text);
				if (!def.valid)
				{
					army.Error("Unknown unit def: " + text);
					continue;
				}
				if (def.type == Unit.Type.InventoryItem)
				{
					army.Error("Trying to add inventory item " + text + " to army");
					continue;
				}
				Unit unit = new Unit();
				unit.def = def;
				unit.salvo_def = army.game.defs.Get<SalvoData.Def>(def.salvo_def);
				unit.mercenary = mercs[j];
				unit.battle_row = rowcols[j].x;
				unit.battle_col = rowcols[j].y;
				unit.SetArmy(army);
				army.units.Add(unit);
				army.InvalidateBattleFormation();
			}
			if (army.castle != null)
			{
				army.castle.GetRealm()?.rebellionRisk.Recalc();
			}
			army.siege_equipment.Clear();
			int count2 = item_defs.Count;
			for (int k = 0; k < count2; k++)
			{
				string text2 = item_defs[k];
				Unit.Def def2 = army.game.defs.Get<Unit.Def>(text2);
				if (!def2.valid)
				{
					army.Error("Unknown unit def: " + text2);
					continue;
				}
				InventoryItem inventoryItem = new InventoryItem();
				inventoryItem.def = def2;
				inventoryItem.army = army;
				army.siege_equipment.Add(inventoryItem);
			}
			if (army.started)
			{
				army.NotifyListeners("units_changed");
				army.NotifyListeners("inventory_changed");
			}
		}
	}

	[Serialization.State(25)]
	public class FoodState : Serialization.ObjectState
	{
		private Data data;

		public static FoodState Create()
		{
			return new FoodState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			data = Data.CreateFull(army.supplies);
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
			Army army = obj as Army;
			if (army.supplies == null)
			{
				army.supplies = data.GetObject(obj.game) as ComputableValue;
			}
			data.ApplyTo(army.supplies, army.game);
		}
	}

	[Serialization.State(26)]
	public class SpeedState : Serialization.ObjectState
	{
		private float speed;

		public static SpeedState Create()
		{
			return new SpeedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			if (army.movement == null)
			{
				return false;
			}
			speed = army.movement.speed;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(speed, "speed");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			speed = ser.ReadFloat("speed");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			if (army.movement != null)
			{
				army.movement.speed = speed;
			}
		}
	}

	[Serialization.State(27)]
	public class RebelState : Serialization.ObjectState
	{
		private NID rebel;

		public static RebelState Create()
		{
			return new RebelState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Army).rebel != null;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			rebel = army.rebel;
			return army.rebel != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Rebel>(rebel, "rebel");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			rebel = ser.ReadNID<Rebel>("rebel");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Army).rebel = rebel.Get<Rebel>(obj.game);
		}
	}

	[Serialization.State(28)]
	public class RealmState : Serialization.ObjectState
	{
		private int realm_in;

		public static RealmState Create()
		{
			return new RealmState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			if (army.realm_in != null)
			{
				realm_in = army.realm_in.id;
			}
			else
			{
				realm_in = 0;
			}
			return army.rebel != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(realm_in, "realm_in");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			realm_in = ser.Read7BitSigned("realm_in");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			if (realm_in != 0)
			{
				army.UpdateRealmIn(army.game.GetRealm(realm_in), send_state: false);
			}
			else
			{
				army.UpdateRealmIn(null, send_state: false);
			}
		}
	}

	[Serialization.State(29)]
	public class MoraleState : Serialization.ObjectState
	{
		private Data temporary_morale_data;

		public static MoraleState Create()
		{
			return new MoraleState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			if (army.morale?.temporary_morale == null)
			{
				return false;
			}
			temporary_morale_data = army.morale.temporary_morale.CreateData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(temporary_morale_data, "temporary_morale_data");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			temporary_morale_data = ser.ReadData("temporary_morale_data");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			if (army.morale.temporary_morale != null)
			{
				temporary_morale_data.ApplyTo(army.morale.temporary_morale, army.game);
			}
		}
	}

	[Serialization.State(30)]
	public class RetreatTimeState : Serialization.ObjectState
	{
		public float retreat_time_delta;

		public static RetreatTimeState Create()
		{
			return new RetreatTimeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Army).last_retreat_time != Time.Zero;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			retreat_time_delta = army.game.time - army.last_retreat_time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(retreat_time_delta, "retreat_time_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			retreat_time_delta = ser.ReadFloat("retreat_time_delta");
		}

		public override void ApplyTo(Object obj)
		{
			Army obj2 = obj as Army;
			obj2.last_retreat_time = obj2.game.time - retreat_time_delta;
		}
	}

	[Serialization.State(31)]
	public class InWaterState : Serialization.ObjectState
	{
		public bool is_in_water;

		public static InWaterState Create()
		{
			return new InWaterState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Army).is_in_water;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = obj as Army;
			is_in_water = army.is_in_water;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(is_in_water, "is_in_water");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			is_in_water = ser.ReadBool("is_in_water");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Army).is_in_water = is_in_water;
		}
	}

	[Serialization.State(32)]
	public class InteractorsState : Serialization.ObjectState
	{
		public NID interactor;

		public NID interact_target;

		public static InteractorsState Create()
		{
			return new InteractorsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Army army))
			{
				return false;
			}
			interactor = army.interactor;
			interact_target = army.interact_target;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(interactor, "interactor");
			ser.WriteNID(interact_target, "interact_target");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			interactor = ser.ReadNID("interactor");
			interact_target = ser.ReadNID("interact_target");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Army army)
			{
				army.interactor = interactor.GetObj(obj.game) as Army;
				army.interact_target = interact_target.GetObj(obj.game) as Army;
				army.NotifyListeners("inetractors_changed");
				army.interactor?.NotifyListeners("inetractors_changed");
				army.interact_target?.NotifyListeners("inetractors_changed");
			}
		}
	}

	[Serialization.State(33)]
	public class BorderCrossRelCacheState : Serialization.ObjectState
	{
		public float temp;

		public float perm;

		public static BorderCrossRelCacheState Create()
		{
			return new BorderCrossRelCacheState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Army army))
			{
				return false;
			}
			if (!((double)Math.Abs(army.borderCrossRelCachePerm) > 0.001))
			{
				return Math.Abs(army.borderCrossRelCacheTemp) > 0f;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Army army))
			{
				return false;
			}
			perm = army.borderCrossRelCachePerm;
			temp = army.borderCrossRelCacheTemp;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(perm, "perm");
			ser.WriteFloat(temp, "temp");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			perm = ser.ReadFloat("perm");
			temp = ser.ReadFloat("temp");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Army army)
			{
				army.borderCrossRelCachePerm = perm;
				army.borderCrossRelCacheTemp = temp;
			}
		}
	}

	[Serialization.Event(37)]
	public class MoveToPointArmyEvent : Serialization.ObjectEvent
	{
		private PPos position;

		private float range;

		private bool add;

		public MoveToPointArmyEvent()
		{
		}

		public static MoveToPointArmyEvent Create()
		{
			return new MoveToPointArmyEvent();
		}

		public MoveToPointArmyEvent(PPos position, float range, bool add)
		{
			this.position = position;
			this.range = range;
			this.add = add;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WritePoint(position, "position");
			ser.WriteFloat(range, "range");
			ser.WriteBool(add, "add");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			position = ser.ReadPoint("position");
			range = ser.ReadFloat("range");
			add = ser.ReadBool("add");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			if (position != Point.Zero)
			{
				if (!add)
				{
					army.MoveTo(position, range, stop_water: true);
				}
				else
				{
					army.AddMoveTo(position, range, stop_water: true);
				}
			}
		}
	}

	[Serialization.Event(38)]
	public class MoveToDestinationArmyEvent : Serialization.ObjectEvent
	{
		private NID dst_obj_nid;

		private float range;

		private bool add;

		public MoveToDestinationArmyEvent()
		{
		}

		public static MoveToDestinationArmyEvent Create()
		{
			return new MoveToDestinationArmyEvent();
		}

		public MoveToDestinationArmyEvent(Object dst_obj, float range, bool add)
		{
			dst_obj_nid = dst_obj;
			this.range = range;
			this.add = add;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(dst_obj_nid, "dst_obj_nid");
			ser.WriteFloat(range, "range");
			ser.WriteBool(add, "add");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			dst_obj_nid = ser.ReadNID("dst_obj_nid");
			range = ser.ReadFloat("range");
			add = ser.ReadBool("add");
		}

		public override void ApplyTo(Object obj)
		{
			Object obj2 = dst_obj_nid.GetObj(obj.game);
			Army army = obj as Army;
			if (!add)
			{
				army.MoveTo(obj2 as MapObject, range, stop_water: true);
			}
			else
			{
				army.AddMoveTo(obj2 as MapObject, range, stop_water: true);
			}
		}
	}

	[Serialization.Event(39)]
	public class FleeFromArmyEvent : Serialization.ObjectEvent
	{
		private PPos point;

		private float range;

		public FleeFromArmyEvent()
		{
		}

		public static FleeFromArmyEvent Create()
		{
			return new FleeFromArmyEvent();
		}

		public FleeFromArmyEvent(PPos pt, float range)
		{
			point = pt;
			this.range = range;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WritePoint(point, "point");
			ser.WriteFloat(range, "range");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			point = ser.ReadPoint("point");
			range = ser.ReadFloat("range");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			army.FleeFrom(point, army.def.flee_distance);
		}
	}

	[Serialization.Event(40)]
	public class AddUnitEvent : Serialization.ObjectEvent
	{
		private string def;

		private int slot_index;

		private bool mercenary;

		public AddUnitEvent()
		{
		}

		public static AddUnitEvent Create()
		{
			return new AddUnitEvent();
		}

		public AddUnitEvent(Unit.Def def, int slot_index, bool mercenary)
		{
			this.def = def.field.key;
			this.slot_index = slot_index + 1;
			this.mercenary = mercenary;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def, "def");
			ser.Write7BitUInt(slot_index, "slot_index");
			ser.WriteBool(mercenary, "mercenary");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def = ser.ReadStr("def");
			slot_index = ser.Read7BitUInt("slot_index");
			mercenary = ser.ReadBool("mercenary");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Army).AddUnit(def, slot_index - 1, mercenary);
		}
	}

	[Serialization.Event(41)]
	public class DelUnitEvent : Serialization.ObjectEvent
	{
		private int idx;

		public DelUnitEvent()
		{
		}

		public static DelUnitEvent Create()
		{
			return new DelUnitEvent();
		}

		public DelUnitEvent(Unit unit)
		{
			idx = unit.army.units.FindIndex((Unit x) => x == unit);
			idx++;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(idx, "index");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			idx = ser.Read7BitUInt("index");
		}

		public override void ApplyTo(Object obj)
		{
			idx--;
			Army army = obj as Army;
			if (idx != -1 && army.units != null && army.units.Count > idx)
			{
				army.DelUnit(army.units[idx]);
			}
			else
			{
				Game.Log($"(DelUnitEvent) Invalid unit index {idx} at {army}", Game.LogType.Error);
			}
		}
	}

	[Serialization.Event(42)]
	public class AddItemEvent : Serialization.ObjectEvent
	{
		private string def;

		public AddItemEvent()
		{
		}

		public static AddItemEvent Create()
		{
			return new AddItemEvent();
		}

		public AddItemEvent(Unit.Def def)
		{
			this.def = def.field.key;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def, "def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def = ser.ReadStr("def");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Army).AddInvetoryItem(def, -1);
		}
	}

	[Serialization.Event(43)]
	public class DelItemEvent : Serialization.ObjectEvent
	{
		private int idx;

		public DelItemEvent()
		{
		}

		public static DelItemEvent Create()
		{
			return new DelItemEvent();
		}

		public DelItemEvent(InventoryItem unit)
		{
			idx = unit.army.siege_equipment.FindIndex((InventoryItem x) => x == unit);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(idx + 1, "index");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			idx = ser.Read7BitUInt("index") - 1;
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			if (army.siege_equipment != null && army.siege_equipment.Count > 0 && idx >= 0 && idx < army.siege_equipment.Count)
			{
				army.DelInvetoryItem(army.siege_equipment[idx]);
			}
			else
			{
				Game.Log($"(DelUnitEvent) Invalid unit index {idx} at {army}", Game.LogType.Error);
			}
		}
	}

	[Serialization.Event(44)]
	public class MoveUnitGarrisonEvent : Serialization.ObjectEvent
	{
		private int unit_idx;

		private bool from_garrison;

		private NID castle_nid;

		public MoveUnitGarrisonEvent()
		{
		}

		public static MoveUnitGarrisonEvent Create()
		{
			return new MoveUnitGarrisonEvent();
		}

		public MoveUnitGarrisonEvent(int unit_idx, bool from_garrison, Castle castle)
		{
			this.unit_idx = unit_idx;
			this.from_garrison = from_garrison;
			castle_nid = castle;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(unit_idx, "unit_idx");
			ser.WriteBool(from_garrison, "from_garrison");
			ser.WriteNID(castle_nid, "castle_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			unit_idx = ser.Read7BitUInt("unit_idx");
			from_garrison = ser.ReadBool("from_garrison");
			castle_nid = ser.ReadNID("castle_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Army army = obj as Army;
			Castle castle = castle_nid.Get<Castle>(obj.game);
			if (from_garrison)
			{
				if (unit_idx >= 0 && castle.garrison.units.Count > unit_idx)
				{
					army.MoveUnitFromGarrison(castle.garrison.units[unit_idx]);
				}
			}
			else if (unit_idx >= 0 && army.units.Count > unit_idx)
			{
				army.MoveUnitToGarrison(army.units[unit_idx]);
			}
		}
	}

	[Serialization.Event(45)]
	public class TransferUnitEvent : Serialization.ObjectEvent
	{
		private int unit_idx;

		private int swap_unit_idx;

		private NID dest_nid;

		public TransferUnitEvent()
		{
		}

		public static TransferUnitEvent Create()
		{
			return new TransferUnitEvent();
		}

		public TransferUnitEvent(MapObject dest, int unit_idx, int swap_unit_idx)
		{
			this.unit_idx = unit_idx;
			this.swap_unit_idx = swap_unit_idx;
			dest_nid = dest;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(dest_nid, "dest_nid");
			ser.Write7BitSigned(unit_idx, "unit_idx");
			ser.Write7BitSigned(swap_unit_idx, "swap_unit_idx");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			dest_nid = ser.ReadNID("dest_nid");
			unit_idx = ser.Read7BitSigned("unit_idx");
			swap_unit_idx = ser.Read7BitSigned("swap_unit_idx");
		}

		public override void ApplyTo(Object obj)
		{
			Army obj2 = obj as Army;
			Unit unit = obj2.GetUnit(unit_idx);
			Unit swap_target = null;
			MapObject mapObject = dest_nid.GetObj(obj.game) as MapObject;
			if (mapObject is Army)
			{
				swap_target = (mapObject as Army).GetUnit(swap_unit_idx);
			}
			if (mapObject is Castle)
			{
				swap_target = (mapObject as Castle).garrison.GetUnit(swap_unit_idx);
			}
			obj2.TransferUnit(mapObject, unit, swap_target);
		}
	}

	[Serialization.Event(46)]
	public class MergeUnitsEvent : Serialization.ObjectEvent
	{
		private int source_unit_idx;

		private int dest_unit_idx;

		private NID dest_nid;

		public MergeUnitsEvent()
		{
		}

		public static MergeUnitsEvent Create()
		{
			return new MergeUnitsEvent();
		}

		public MergeUnitsEvent(int source_unit_idx, int dest_unit_idx, MapObject target_owner)
		{
			this.source_unit_idx = source_unit_idx;
			this.dest_unit_idx = dest_unit_idx;
			dest_nid = target_owner;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(source_unit_idx, "source_unit_idx");
			ser.Write7BitUInt(dest_unit_idx, "dest_unit_idx");
			ser.WriteNID(dest_nid, "dest_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			source_unit_idx = ser.Read7BitUInt("source_unit_idx");
			dest_unit_idx = ser.Read7BitUInt("dest_unit_idx");
			dest_nid = ser.ReadNID("dest_nid");
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Army army))
			{
				return;
			}
			Object obj2 = dest_nid.GetObj(obj.game);
			if (obj2 != null)
			{
				if (army.units != null && army.units.Count > source_unit_idx)
				{
					_ = army.units[source_unit_idx];
				}
				Unit target = null;
				if (obj2 is Army)
				{
					Army army2 = obj2 as Army;
					target = ((army2.units != null && army2.units.Count > dest_unit_idx) ? army2.units[dest_unit_idx] : null);
				}
				if (obj2 is Castle)
				{
					Castle castle = obj2 as Castle;
					target = ((castle?.garrison?.units == null || !(castle?.garrison?.units.Count > dest_unit_idx)) ? null : castle?.garrison?.units[dest_unit_idx]);
				}
				army.MergeUnits(army.units[source_unit_idx], target, send_state: true);
			}
		}
	}

	public const float Radius = 4f;

	public Army interactor;

	public Army interact_target;

	public Character leader;

	public Rebel rebel;

	public Mercenary mercenary;

	public List<Unit> units = new List<Unit>();

	public List<InventoryItem> siege_equipment = new List<InventoryItem>();

	public Realm realm_in;

	public float borderCrossRelCachePerm;

	public float borderCrossRelCacheTemp;

	public Realm tgt_realm;

	private ArmyResupplyCastleAction _resupply_action;

	public string ai_status;

	public int ai_thinks;

	public int ai_idles;

	public Realm prev_tgt_realm;

	public Realm last_tgt_realm;

	public int ai_oscillations;

	public Castle castle;

	public Battle battle;

	public Battle last_intended_battle;

	public Morale morale;

	public ComputableValue supplies;

	public bool is_in_water;

	public WaterCross water_crossing;

	private float last_ships_speed_perc;

	private float last_army_speed_perc;

	public int battle_side = -1;

	public bool is_supporter;

	public bool had_supplies_at_start_of_battle;

	public StandardArmyFormation battleview_army_formation;

	public bool act_separately;

	public const int battle_rows = 5;

	public static int battle_cols = 5;

	public bool battle_formation_valid;

	private int last_message_kingdom_id = -1;

	private bool thinking_at_end_of_path;

	public Time last_retreat_time;

	private bool is_starving;

	private int last_seg_idx = -1;

	private static List<Unit> tmp_units = new List<Unit>(100);

	private static List<Unit> tmp_units2 = new List<Unit>(100);

	private ValueCache levy_bonus_cache;

	private bool is_in_on_start;

	private const int STATES_IDX = 20;

	private const int EVENTS_IDX = 36;

	public ArmyResupplyCastleAction resupply_action
	{
		get
		{
			if (_resupply_action?.own_character != leader)
			{
				_resupply_action = null;
			}
			if (_resupply_action == null)
			{
				_resupply_action = leader?.FindAction("ArmyResupplyCastleAction") as ArmyResupplyCastleAction;
			}
			return _resupply_action;
		}
	}

	public KingdomAI.Threat tgt_threat => GetKingdom()?.ai?.GetThreat(tgt_realm);

	public bool currently_on_land
	{
		get
		{
			if (is_in_water && (water_crossing == null || !water_crossing.running) && (battle == null || battle.type == Battle.Type.Naval))
			{
				return castle != null;
			}
			return true;
		}
	}

	public Def def
	{
		get
		{
			if (game != null)
			{
				return game.defs.GetBase<Def>();
			}
			return null;
		}
	}

	public bool isCamping
	{
		get
		{
			Action action = leader?.cur_action;
			if (action != null && action.state != Action.State.Finishing && action.state != Action.State.Inactive && action is CampArmyAction)
			{
				return true;
			}
			if (rebel != null && rebel.current_action == Rebel.Action.Rest && !movement.IsMoving())
			{
				return true;
			}
			if (mercenary != null && !mercenary.army.movement.IsMoving())
			{
				return true;
			}
			return false;
		}
	}

	public bool IsMercenary()
	{
		return mercenary != null;
	}

	public bool IsHiredMercenary()
	{
		if (mercenary != null && !mercenary.ValidForHireAsArmy())
		{
			return leader != null;
		}
		return false;
	}

	public override string ToString()
	{
		string text = base.ToString();
		text = $"{text}, Units: [{EvalStrength()}] {CountUnits()} / {MaxUnits()}";
		if (!string.IsNullOrEmpty(ai_status))
		{
			text = "[" + ai_status + "] " + text;
		}
		if (tgt_threat != null)
		{
			text += $" -> {tgt_threat}";
		}
		return text;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "is_fleeing":
			return IsFleeing();
		case "army":
			return this;
		case "leader":
			return leader;
		case "castle":
			return castle;
		case "kingdom":
			return GetKingdom();
		case "realm_in":
			return realm_in;
		case "kingdom_in":
			return realm_in?.GetKingdom();
		case "battle":
			return battle;
		case "is_in_battle":
			return battle != null;
		case "battle_enemy_kingdom":
			if (battle == null)
			{
				return Value.Unknown;
			}
			return battle.attackers.Contains(this) ? battle.defender_kingdom : battle.attacker_kingdom;
		case "battle_enemy_leader":
			if (battle == null)
			{
				return Value.Unknown;
			}
			return battle.attackers.Contains(this) ? battle.defender : battle.attacker;
		case "rebel":
			return rebel;
		case "mercenary":
			return mercenary;
		case "no_supplies":
			if (castle != null)
			{
				return false;
			}
			return (supplies?.Get() ?? 0f) <= 0f;
		case "is_home":
			if (realm_in == null)
			{
				return false;
			}
			return realm_in.IsOwnStance(this);
		case "cur_supplies":
		case "supplies":
			return (float)Math.Ceiling(supplies?.Get() ?? 0f);
		case "max_supplies":
		case "supplies_max":
			return supplies?.GetMax() ?? 0f;
		case "supplies_rate":
		{
			float num2 = supplies?.GetRate() ?? 0f;
			if (num2 == 0f)
			{
				return Value.Unknown;
			}
			return num2;
		}
		case "supplies_rate_per_tick":
		{
			float num = supplies?.GetRate() ?? 0f;
			if (num == 0f)
			{
				return Value.Unknown;
			}
			return num * def.supplies_consumption_interval;
		}
		case "missing_supplies":
			return MissingSupplies();
		case "can_resupply":
			return CanResupply();
		case "resupply_potential":
			if (resupply_action == null)
			{
				return 0;
			}
			return resupply_action.PotentialSupplies(resupply_action.GetCost(resupply_action.target));
		case "resupply_cost":
			return resupply_action?.GetCost();
		case "full_resupply_cost":
			return resupply_action?.GetFullCost();
		case "full_resupply_requirements_not_met":
		{
			Resource resource5 = resupply_action?.GetFullCost();
			if (resource5 == null)
			{
				return false;
			}
			Resource resource6 = resupply_action?.GetCost();
			if (resource6 == null)
			{
				return true;
			}
			return !resource5.Equals(resource6);
		}
		case "missing_resupply":
		{
			Resource resource3 = resupply_action?.GetFullCost();
			if (resource3 == null)
			{
				return Value.Unknown;
			}
			Resource resource4 = resupply_action?.GetCost();
			if (resource4 == null)
			{
				return resource3;
			}
			resource3.Sub(resource4);
			return resource3;
		}
		case "is_missing_resupply":
		{
			Resource resource = resupply_action?.GetFullCost();
			if (resource == null)
			{
				return false;
			}
			Resource resource2 = resupply_action?.GetCost();
			if (resource2 == null)
			{
				return false;
			}
			return !resource.Equals(resource2);
		}
		case "no_supplies_defense_penalty":
			if (units.Count > 0 && IsStarving() && battle != null && battle_side == 0 && battle.type == Battle.Type.Siege)
			{
				return units[0].def.defense_starvation_penalty;
			}
			return 0;
		case "siege_equipment_count":
			return siege_equipment.Count;
		case "is_mercenary":
			return IsMercenary();
		case "is_crusader":
			return leader != null && leader.IsCrusader();
		case "is_hired_mercenary":
			return IsHiredMercenary();
		case "morale":
			return morale.GetMorale();
		case "permanent_morale":
			return morale.GetMorale() - morale.temporary_morale.Get();
		case "temporary_morale":
			return morale.temporary_morale.Get();
		case "max_manpower":
			return GetMaxManPower();
		case "manpower":
			return GetManPower();
		case "is_rebel":
			return rebel != null;
		case "is_moving":
			return movement != null && movement.IsMoving();
		case "levies_manpower_perc":
			return GetLevyBonus();
		case "manpower_base":
			return GetManpowerForSquads((Unit u) => u.manpower_base_size());
		case "leader_manpower_base":
			return GetManpowerForSquads((Unit u) => u.manpower_base_size(), 0, 1);
		case "units_manpower_base":
			return GetManpowerForSquads((Unit u) => u.manpower_base_size(), 1);
		case "manpower_levies":
			return GetManpowerForSquads((Unit u) => u.manpower_base_levies());
		case "leader_manpower_levies":
			return GetManpowerForSquads((Unit u) => u.manpower_base_levies(), 0, 1);
		case "units_manpower_levies":
			return GetManpowerForSquads((Unit u) => u.manpower_base_levies(), 1);
		case "manpower_bonus":
			return GetManpowerForSquads((Unit u) => u.manpower_bonus());
		case "max_manpower_healthy":
			return GetManpowerForSquads((Unit u) => u.num_healthy());
		case "max_manpower_dead":
			return GetManpowerForSquads((Unit u) => u.num_dead());
		case "additional_troops_perc":
			return GetAdditionalTroopsPerc();
		case "interact_target":
			return interact_target;
		case "interactor":
			return interactor;
		case "tgt_realm":
			return tgt_realm;
		case "tgt_threat":
			return new Value(tgt_threat);
		case "last_tgt_realm":
			return last_tgt_realm;
		case "prev_tgt_realm":
			return prev_tgt_realm;
		case "ai_oscillations":
			return ai_oscillations;
		case "has_free_slots":
			return CountUnits() < MaxUnits();
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override void OnInit()
	{
		movement = new Movement(this, PathData.PassableArea.Type.All);
		movement.speed = def.world_move_speed;
		water_crossing = new WaterCross(this);
		morale = new Morale(this);
		InitSupplies();
		UpdateInBatch(game.update_10sec);
	}

	public bool IsStarving()
	{
		if (supplies.Get() == 0f && leader != null && !leader.IsMercenary() && !leader.IsRebel() && !leader.IsCrusader())
		{
			if (realm_in == null || realm_in.kingdom_id == kingdom_id)
			{
				if (battle != null)
				{
					if (castle != null)
					{
						return castle.food_storage == 0f;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void OnUpdate()
	{
		if (kingdom_id == 0 && IsAuthority())
		{
			Warning("Army at kingdom id 0, destroying");
			if (this.castle == null)
			{
				Castle castle = realm_in?.castle;
				if (castle?.army == this)
				{
					castle.SetArmy(null);
				}
			}
			Destroy();
			return;
		}
		bool flag = is_starving;
		is_starving = IsStarving();
		if (is_starving != flag)
		{
			leader?.NotifyListeners("army_is_starving", is_starving);
		}
		Kingdom kingdom = GetKingdom();
		if (IsValid() && IsAuthority() && kingdom != null && kingdom.is_player)
		{
			if (leader != null)
			{
				leader.NotifyListeners("player_army_update");
			}
			else
			{
				NotifyListeners("player_army_update");
			}
		}
		base.OnUpdate();
	}

	public override void OnMove(Point prev_pos)
	{
		base.OnMove(prev_pos);
		if (movement?.path?.dst_obj == null)
		{
			return;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || (kingdom.ai != null && kingdom.ai.Enabled(KingdomAI.EnableFlags.Armies)))
		{
			float num = movement.path.range + 20f;
			if (!(position.SqrDist(movement.path.dst_obj.position) > num * num) && !ValidateDestination(movement.path.dst_obj))
			{
				movement.Stop();
			}
		}
	}

	public override void SetPosition(PPos position)
	{
		if (!IsValid())
		{
			return;
		}
		if (position.paID == 0 && game?.path_finding?.data != null && game.path_finding.data.OutOfMapBounds(position.pos))
		{
			Warning("Trying to move out of bounds");
			return;
		}
		if (position.pos == Point.Zero)
		{
			Warning("moved to 0,0");
		}
		if (game?.path_finding != null)
		{
			base.position = game.path_finding.ClampPositionToWorldBounds(position);
		}
		Realm realm = realm_in;
		UpdateRealmIn();
		if (battle == null)
		{
			HandleWater(realm);
			if (IsAuthority())
			{
				Army army = CheckEnemyClash();
				if (army != null)
				{
					Battle.StartBattle(this, army);
				}
			}
			if (realm != realm_in)
			{
				SetWorldSpeed();
				UpdateOccupyRealmCheck(realm);
			}
			else if (is_in_water)
			{
				float num = ((leader == null) ? GetKingdom().GetStat(Stats.ks_ships_speed_perc) : leader.GetStat(Stats.cs_ships_speed_perc));
				if (num != last_ships_speed_perc)
				{
					SetWorldSpeed();
					last_ships_speed_perc = num;
				}
			}
			else
			{
				float num2 = leader?.GetStat(Stats.cs_army_speed_world_perc) ?? 0f;
				if (num2 != last_army_speed_perc)
				{
					SetWorldSpeed();
					last_army_speed_perc = num2;
				}
			}
		}
		else
		{
			UpdateWaterInstant();
		}
		NotifyListeners("moved");
	}

	public void UpdateOccupyRealmCheck(Realm last_realm)
	{
		if (!IsAuthority() || last_realm == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < last_realm.armies.Count; i++)
		{
			Army army = last_realm.armies[i];
			if (army.IsValid() && army.kingdom_id == kingdom_id)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return;
		}
		for (int j = 0; j < last_realm.settlements.Count; j++)
		{
			Settlement settlement = last_realm.settlements[j];
			if (settlement.IsActiveSettlement() && settlement != null && !(settlement.type != "Keep"))
			{
				Realm realm = settlement.GetRealm();
				if (realm != null && (settlement.keep_effects.GetController()?.GetKingdom()?.id ?? 0) == kingdom_id && realm.controller.GetKingdom().id != kingdom_id)
				{
					settlement.StartKeepOccupyCheck();
					break;
				}
			}
		}
	}

	public bool NearWater()
	{
		for (int i = -3; i <= 3; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				if (game.path_finding.data.GetNode(position + new Point(j, i)).water)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool NearCoast()
	{
		for (int i = -3; i <= 3; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				PPos pos = position + new Point(j, i);
				if (!game.path_finding.data.GetNode(pos).water)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsCrusadeArmy()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		return kingdom.type == Kingdom.Type.Crusade;
	}

	public bool IsOwnOrCrusader(Kingdom obj)
	{
		if (IsOwnStance(obj))
		{
			return true;
		}
		return Crusade.Get(game)?.army == this;
	}

	public bool IsAllyOrTeammate(Kingdom obj)
	{
		if (IsAlly(obj))
		{
			return true;
		}
		Kingdom kingdom = GetKingdom();
		Game.Team team = game.teams.Get(kingdom);
		if (team != null)
		{
			return team == game.teams.Get(obj);
		}
		return false;
	}

	public void ResetSegmentIdxCheck()
	{
		if (movement?.path != null)
		{
			last_seg_idx = movement.path.segment_idx;
		}
		else
		{
			last_seg_idx = -1;
		}
	}

	private void HandleTeleportArea()
	{
		if (game.path_finding?.data?.pas == null || game.path_finding.data.pas.Length == 0 || movement.path == null || movement.paused)
		{
			return;
		}
		int num = movement.path.segment_idx + 1;
		if (num >= movement.path.segments.Count)
		{
			return;
		}
		int num2 = num;
		Path.Segment segment = movement.path.segments[num];
		if (segment.pt.paID <= 0 || game.path_finding.data.pas[segment.pt.paID - 1].type != PathData.PassableArea.Type.Teleport)
		{
			return;
		}
		for (int num3 = num - 1; num3 >= 0; num3--)
		{
			if (movement.path.segments[num3].pt.paID != segment.pt.paID)
			{
				num2 = num3 + 1;
				segment = movement.path.segments[num2];
				break;
			}
		}
		for (int i = num + 1; i < movement.path.segments.Count; i++)
		{
			if (movement.path.segments[i].pt.paID != segment.pt.paID)
			{
				water_crossing.Begin(num2, def.teleport_area_duration, can_interrupt: true, i, is_fast: false, teleport: true);
				NotifyListeners("moved_in_water");
				break;
			}
		}
	}

	public override void HandleRiverCrossing()
	{
		if (movement.path == null || movement.paused || water_crossing == null || water_crossing.running)
		{
			return;
		}
		int num = last_seg_idx;
		ResetSegmentIdxCheck();
		if (num < 0)
		{
			return;
		}
		for (int i = num; i <= movement.path.segment_idx; i++)
		{
			if (movement.path.segments[i].pt.paID == -1 && movement.path.segments.Count > i + 1)
			{
				water_crossing.Begin(i, def.river_crossing_duration, can_interrupt: true, i + 1, is_fast: false, teleport: true);
				NotifyListeners("moved_in_water");
				break;
			}
		}
	}

	private void UpdateWaterInstant()
	{
		if (game?.path_finding?.data != null && castle == null)
		{
			PathData.Node node = game.path_finding.data.GetNode(position);
			bool flag = node.ocean || node.coast;
			if (flag != is_in_water)
			{
				is_in_water = flag;
				NotifyListeners("moved_in_water");
			}
		}
	}

	private void HandleSeaEmbark(Realm last_realm)
	{
		if (game?.path_finding?.data == null)
		{
			return;
		}
		PathData.Node node = game.path_finding.data.GetNode(position);
		bool flag = node.ocean || node.coast;
		float offset = 0f;
		if (!is_in_water && movement?.path != null && movement.path.GetPathPoint(movement.path.t + def.enter_ocean_prediction_distance, out var pt, out var _))
		{
			if (pt.paID != 0)
			{
				return;
			}
			node = game.path_finding.data.GetNode(pt);
			flag = node.ocean || node.coast;
			offset = def.enter_ocean_prediction_distance;
		}
		if (flag == is_in_water)
		{
			return;
		}
		is_in_water = flag;
		if (base.obj_state == ObjState.Started)
		{
			if (flag)
			{
				float num = 1f + GetKingdom().GetStat(Stats.ks_embark_speed_perc) / 100f;
				if (leader != null && last_realm != null && last_realm.kingdom_id == leader.kingdom_id)
				{
					water_crossing.Begin(def.water_enter_duration_fast * num, can_interrupt: false, is_fast: true, teleport: false, offset);
				}
				else
				{
					water_crossing.Begin(def.water_enter_duration * num, can_interrupt: false, is_fast: false, teleport: false, offset);
				}
			}
			else
			{
				float num2 = 1f + GetKingdom().GetStat(Stats.ks_embark_speed_perc) / 100f;
				if (leader != null && realm_in != null && realm_in.kingdom_id == leader.kingdom_id)
				{
					water_crossing.Begin(def.water_exit_duration_fast * num2, can_interrupt: true, is_fast: true);
				}
				else
				{
					water_crossing.Begin(def.water_exit_duration * num2, can_interrupt: true);
				}
			}
		}
		NotifyListeners("moved_in_water");
	}

	public void HandleWater(Realm last_realm)
	{
		if (!water_crossing.running && game?.path_finding?.data != null)
		{
			HandleTeleportArea();
			if (PathData.IsGroundPAid(position.paID))
			{
				HandleRiverCrossing();
				HandleSeaEmbark(last_realm);
			}
		}
	}

	public void SetLeader(Character c, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			return;
		}
		if (leader != null)
		{
			if (IsAuthority())
			{
				leader.SetLocation(null);
				leader.ClearDefaultStatus<LeadArmyStatus>();
			}
			leader.stats?.DelListener(this);
		}
		leader = c;
		leader?.stats?.AddListener(this);
		if (send_state)
		{
			SendState<LeaderState>();
		}
		if (IsAuthority() && leader != null)
		{
			leader.SetLocation(this);
			leader.ClearDefaultStatus();
		}
		NotifyListeners("leader_changed");
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		if (form == "short")
		{
			return "Army.short_name";
		}
		return "Army.name";
	}

	public override void SetKingdom(int k_id)
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			kingdom.DelArmy(this, del_court_member: false);
			if (mercenary != null && mercenary.kingdom_id != k_id)
			{
				mercenary.GetKingdom().AddArmy(this, !IsMercenary());
			}
		}
		base.SetKingdom(k_id);
		GetKingdom()?.AddArmy(this, !IsMercenary());
		NotifyListeners("kingdom_changed");
		if (IsAuthority())
		{
			SendState<KingdomState>();
		}
	}

	public void SetMercenary(Mercenary merc)
	{
		if (merc != mercenary)
		{
			if (merc == null && mercenary != null)
			{
				mercenary.GetKingdom()?.DelArmy(this);
				Kingdom kingdom = GetKingdom();
				realm_in?.GetKingdom()?.DelMercenaryIn(this);
				kingdom?.DelMercenary(this);
			}
			if (merc != null)
			{
				merc.army = this;
				NewMercenaryInKingdomMessage();
			}
			else if (mercenary != null)
			{
				mercenary.army = null;
			}
			mercenary = merc;
		}
	}

	public void SetRebel(Rebel r, bool send_state = true)
	{
		rebel = r;
		if (rebel != null)
		{
			SetSupplies(supplies.GetMax());
		}
		if (send_state)
		{
			SendState<RebelState>();
		}
	}

	public void ClearBorderCrossRelCache()
	{
		borderCrossRelCachePerm = (borderCrossRelCacheTemp = 0f);
	}

	public bool IsBorderCrossKingdomFriendly(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return false;
		}
		Kingdom kingdom2 = GetKingdom();
		RelationUtils.Stance stance = kingdom2.GetStance(kingdom);
		if (stance.IsAlliance())
		{
			return true;
		}
		if (stance.IsMarriage())
		{
			return true;
		}
		if (stance.IsNonAgression())
		{
			return true;
		}
		if (kingdom.sovereignState == kingdom2)
		{
			return true;
		}
		if (kingdom2.GetRelationship(kingdom) > def.bordercross_relationship_treshold)
		{
			if (stance.IsTrade())
			{
				return true;
			}
			if (kingdom2.sovereignState == kingdom)
			{
				return true;
			}
			if (kingdom2.pacts.Find((Pact p) => p.members.Contains(kingdom)) != null)
			{
				return true;
			}
		}
		if (kingdom2.IsFriend(kingdom))
		{
			return true;
		}
		if (kingdom2?.game?.teams?.Get(kingdom2)?.players?.Find((Game.Player p) => p.kingdom_id == kingdom.id) != null)
		{
			return true;
		}
		return false;
	}

	public void UpdateRealmIn(Realm r, bool send_state = true)
	{
		if (r == realm_in)
		{
			return;
		}
		Kingdom kingdom = null;
		if (realm_in != null)
		{
			realm_in.DelArmy(this);
			kingdom = realm_in.GetKingdom();
		}
		Realm realm = realm_in;
		realm_in = r;
		Kingdom kingdom2 = null;
		if (realm_in != null)
		{
			realm_in.AddArmy(this);
			kingdom2 = realm_in.GetKingdom();
		}
		if (kingdom != kingdom2)
		{
			kingdom?.DelArmyIn(this);
			kingdom2?.AddArmyIn(this, kingdom != null);
		}
		Kingdom kingdom3 = realm?.GetKingdom();
		Kingdom kingdom4 = realm_in?.GetKingdom();
		Kingdom kingdom5 = GetKingdom();
		if (kingdom5 != null && realm_in != null && !Game.isLoadingSaveGame && !is_in_on_start)
		{
			bool flag = kingdom3 == kingdom4;
			if (IsAuthority() && !flag)
			{
				if (kingdom3 != null && ((double)Math.Abs(borderCrossRelCachePerm) > 0.001 || (double)Math.Abs(borderCrossRelCacheTemp) > 0.001))
				{
					Vars vars = new Vars(this);
					vars.Set("realm", (Object)realm_in);
					vars.Set("perm_cache_value", borderCrossRelCachePerm);
					vars.Set("temp_cache_value", borderCrossRelCacheTemp);
					kingdom5.AddRelationModifier(kingdom3, "rel_bordercross_leave", vars);
				}
				ClearBorderCrossRelCache();
				Vars vars2 = new Vars(this);
				vars2.Set("realm", (Object)realm_in);
				if (IsEnemy(kingdom4))
				{
					kingdom5.AddRelationModifier(kingdom4, "rel_bordercross_enter_enemy", vars2);
					KingdomAndKingdomRelation.GetValuesOfModifier(game, "rel_bordercross_enter_enemy", vars2, out borderCrossRelCachePerm, out borderCrossRelCacheTemp);
				}
				else if (kingdom_id != realm_in.kingdom_id && !IsBorderCrossKingdomFriendly(kingdom4))
				{
					kingdom5.AddRelationModifier(kingdom4, "rel_bordercross_enter_neutral", vars2);
					KingdomAndKingdomRelation.GetValuesOfModifier(game, "rel_bordercross_enter_neutral", vars2, out borderCrossRelCachePerm, out borderCrossRelCacheTemp);
				}
				SendState<BorderCrossRelCacheState>();
			}
			if (GetKingdom().is_local_player && !flag && kingdom3 != null)
			{
				NotifyListeners("realm_crossed_analytics", kingdom5.GetStance(kingdom3).ToString());
			}
		}
		NotifyListeners("realm_crossed");
		RecalcSuppliesRate();
		morale.RecalcPermanentMorale(force_send: false, recalc_dist: true);
		if (send_state && IsAuthority())
		{
			SendState<RealmState>();
		}
	}

	public void UpdateRealmIn()
	{
		Realm realm = null;
		if (realm == null)
		{
			realm = game.GetRealm(position);
		}
		if (realm == null && battle != null)
		{
			realm = battle.GetRealm();
		}
		UpdateRealmIn(realm);
	}

	public Army CheckEnemyClash()
	{
		if (castle != null || battle != null || realm_in == null || movement == null)
		{
			return null;
		}
		if (movement.pf_path == null && movement.path == null)
		{
			return null;
		}
		if (IsFleeing())
		{
			return null;
		}
		if (water_crossing.running)
		{
			return null;
		}
		Army army = CheckEnemyClash(realm_in.armies);
		if (army != null)
		{
			return army;
		}
		for (int i = 0; i < realm_in.neighbors.Count; i++)
		{
			army = CheckEnemyClash(realm_in.neighbors[i].armies);
			if (army != null)
			{
				return army;
			}
		}
		return null;
	}

	public Army CheckEnemyClash(List<Army> armies)
	{
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			if (army.IsValid() && army.castle == null && army.battle == null && IsEnemy(army) && (rebel == null || army.mercenary == null) && (mercenary == null || army.rebel == null) && !army.IsFleeing() && position.InRange(armies[i].position, 4f) && is_in_water == !army.currently_on_land)
			{
				return army;
			}
		}
		return null;
	}

	public void EnterCastle(Castle c, bool send_state = true)
	{
		if (mercenary == null)
		{
			if (c.army != null)
			{
				c.army.LeaveCastle(position);
			}
			c.SetArmy(this, send_state);
			castle = c;
			movement.ClearExtraPaths();
			ClearReserved();
			if (leader != null)
			{
				leader.RefreshTags();
			}
			if (send_state)
			{
				SendState<CastleState>();
			}
			SetPosition(c.position);
			is_in_water = false;
			UpdateRealmIn(c.GetRealm());
			RecalcSuppliesRate();
			ai_oscillations = 0;
			c.GetRealm().rebellionRisk.Recalc();
			NotifyListeners("enter_castle");
			leader?.NotifyListeners("enter_castle");
		}
	}

	private void CalcLeaveCastlePos(ref PPos pos, out bool stop, Castle castle = null)
	{
		stop = false;
		if (castle == null)
		{
			castle = this.castle;
		}
		if (movement.path == null || !movement.path.IsValid())
		{
			if (castle != null)
			{
				pos = castle.GetRandomExitPoint(try_exit_outside_town: true, check_water: true);
			}
			return;
		}
		float num = 0f;
		PPos pPos = pos;
		PPos pt;
		PPos ptDest;
		while (true)
		{
			num += 1f;
			if (!movement.path.GetPathPoint(num, out pt, out ptDest, advance: true))
			{
				break;
			}
			PathData.Node node = game.path_finding.data.GetNode(pt);
			if (node.water)
			{
				pos = pPos;
				return;
			}
			pPos = pt;
			if (!node.town)
			{
				pos = pt;
				return;
			}
		}
		if (castle != null)
		{
			pos = movement.path.dst_pt;
			if (castle != null)
			{
				pos = castle.GetRandomExitPoint();
			}
			stop = true;
		}
		else
		{
			movement.path.GetPathPoint(0f, out pt, out ptDest, advance: true);
			pos = pt;
		}
	}

	public void LeaveCastle(PPos pt, bool send_state = true)
	{
		if (!IsAuthority() || this.castle == null)
		{
			return;
		}
		this.castle.SetArmy(null, send_state);
		Castle castle = this.castle;
		this.castle = null;
		if (leader != null)
		{
			if (leader.cur_action is CampArmyAction campArmyAction)
			{
				campArmyAction.Cancel();
			}
			leader.RefreshTags();
		}
		CalcLeaveCastlePos(ref pt, out var stop, castle);
		if (stop)
		{
			Teleport(pt);
		}
		else
		{
			SetPosition(pt);
		}
		RecalcSuppliesRate();
		castle.GetRealm().rebellionRisk.Recalc();
		NotifyListeners("leave_castle", castle);
		leader?.NotifyListeners("leave_castle", castle);
		if (send_state)
		{
			SendState<PositionState>();
			SendState<CastleState>();
		}
	}

	public void ClearInteractors()
	{
		Army army = interactor;
		Army army2 = interact_target;
		if (interactor != null)
		{
			interactor.interact_target = null;
		}
		if (interact_target != null)
		{
			interact_target.interactor = null;
		}
		interactor = null;
		interact_target = null;
		SendState<InteractorsState>();
		army?.SendState<InteractorsState>();
		army2?.SendState<InteractorsState>();
		army?.NotifyListeners("inetractors_changed");
		army2?.NotifyListeners("inetractors_changed");
		NotifyListeners("inetractors_changed");
	}

	public int EvalStrength()
	{
		using (new Stat.ForceCached("Army.EvalStrength"))
		{
			float num = 0f;
			for (int i = 0; i < units.Count; i++)
			{
				float num2 = units[i].EvalStrength();
				num += num2;
			}
			return 1 + (int)num;
		}
	}

	public MapObject GetTarget()
	{
		return (movement.pf_path ?? movement.path)?.dst_obj;
	}

	public Realm GetTargetRealm()
	{
		Path path = movement.pf_path ?? movement.path;
		if (path == null)
		{
			return realm_in;
		}
		Point pt = ((path.dst_obj != null) ? path.dst_obj.position : path.dst_pt);
		return game.GetRealm(pt);
	}

	public void SetAIStatus(string ai_status)
	{
		if (!(ai_status == this.ai_status))
		{
			this.ai_status = ai_status;
			if (ai_status == "idle" || ai_status == "wait_orders")
			{
				ai_idles++;
			}
			else
			{
				ai_idles = 0;
			}
			NotifyListeners("ai_status_changed");
		}
	}

	public string GetAIStatusKey()
	{
		if (ai_status == null)
		{
			return null;
		}
		return "Army.ai_status." + ai_status;
	}

	public int MaxUnits()
	{
		return def.GetMaxUnits(leader);
	}

	public void ClearUnitsOverMax(bool send_state = true)
	{
		int num = units.Count - (MaxUnits() + 1);
		if (num <= 0)
		{
			return;
		}
		units.Sort(delegate(Unit u1, Unit u2)
		{
			if (u1.def.type == Unit.Type.Noble)
			{
				return -1;
			}
			if (u2.def.type == Unit.Type.Noble)
			{
				return 1;
			}
			if (u1.level > u2.level)
			{
				return -1;
			}
			if (u1.level > u2.level)
			{
				return 1;
			}
			float maxCost = u1.def.GetMaxCost(ResourceType.Gold);
			float maxCost2 = u2.def.GetMaxCost(ResourceType.Gold);
			if (maxCost > maxCost2)
			{
				return -1;
			}
			if (maxCost > maxCost2)
			{
				return 1;
			}
			if (u1.damage < u2.damage)
			{
				return -1;
			}
			return (u1.damage < u2.damage) ? 1 : 0;
		});
		int num2 = units.Count - num;
		for (int num3 = 0; num3 < num; num3++)
		{
			DelUnit(units[num2], units.Count == num2 + 1);
		}
	}

	public int MaxItems()
	{
		int num = def.max_inventory;
		if (leader != null)
		{
			if (leader.IsMarshal())
			{
				num = def.marshal_max_inventory;
			}
			else if (leader.IsKing())
			{
				num = def.king_max_inventory;
			}
			num += (int)leader.GetStat(Stats.cs_army_item_bonus, must_exist: false);
		}
		return num;
	}

	public int CountUnits()
	{
		if (units == null || units.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < units.Count; i++)
		{
			if (units[i].def.type != Unit.Type.Noble)
			{
				num++;
			}
		}
		return num;
	}

	public void AddUnit(Unit unit, int slot_index = -1, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			SendEvent(new AddUnitEvent(unit.def, slot_index, unit.mercenary));
		}
		else
		{
			if (battle != null && !battle.battle_map_only)
			{
				return;
			}
			if (unit.def.type == Unit.Type.InventoryItem && battle?.batte_view_game == null)
			{
				Error($"Trying to add inventory item {unit.def} as unit outside of battleview");
				return;
			}
			unit.SetArmy(this);
			unit.SetGarrison(null);
			if (slot_index >= 0 && slot_index < units.Count)
			{
				units.Insert(slot_index, unit);
			}
			else
			{
				units.Add(unit);
			}
			InvalidateBattleFormation();
			if (send_state)
			{
				SendState<UnitsState>();
			}
			if (base.started && send_state)
			{
				castle?.GetRealm()?.rebellionRisk.Recalc();
				NotifyListeners("units_changed");
				if (castle != null)
				{
					castle.NotifyListeners("units_changed");
				}
			}
			SetWorldSpeed();
			GetKingdom()?.InvalidateIncomes();
		}
	}

	private void ValidateMarshalIndex(string step)
	{
		if (battle != null && battle.battle_map_only)
		{
			return;
		}
		for (int i = 1; i < units.Count; i++)
		{
			if (units[i].def.type == Unit.Type.Noble)
			{
				Error("Marshal at wrong index " + step);
				break;
			}
		}
	}

	public void DelUnit(Unit unit, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			SendEvent(new DelUnitEvent(unit));
			return;
		}
		unit.army = null;
		units.Remove(unit);
		InvalidateBattleFormation();
		if (send_state)
		{
			SendState<UnitsState>();
		}
		if (base.started && send_state)
		{
			castle?.GetRealm()?.rebellionRisk.Recalc();
			NotifyListeners("units_changed");
			if (castle != null)
			{
				castle.NotifyListeners("units_changed");
			}
		}
		SetWorldSpeed();
	}

	public void SwapUnits(int idx1, int idx2, bool send_state = true)
	{
		Unit unit = units[idx1];
		Unit unit2 = units[idx2];
		if ((unit?.def != null && unit.def.type == Unit.Type.Noble) || (unit2?.def != null && unit2.def.type == Unit.Type.Noble))
		{
			Error("Trying to swap marshal unit");
			return;
		}
		units[idx1] = unit2;
		units[idx2] = unit;
		if (send_state)
		{
			SendState<UnitsState>();
		}
		if (base.started && send_state)
		{
			NotifyListeners("units_changed");
			if (castle != null)
			{
				castle.NotifyListeners("units_changed");
			}
		}
	}

	public string GetNobleDefKey()
	{
		if (!IsAuthority())
		{
			return null;
		}
		string id = ((rebel == null) ? GetKingdom() : ((rebel.rebellion == null) ? rebel.army?.realm_in?.GetKingdom() : ((rebel.rebellion.loyal_to == -1) ? game.GetKingdom(rebel.rebellion.spawn_kingdom_id) : game.GetKingdom(rebel.rebellion.loyal_to))))?.units_set ?? "DefaultUnitSet";
		AvailableUnits.Def def = game.defs.Find<AvailableUnits.Def>(id);
		if (def == null)
		{
			return null;
		}
		if (rebel == null)
		{
			return def.marhsal?.name;
		}
		return def.rebel_marshal?.name;
	}

	[Obsolete("Use GetNobleDefKey() Method")]
	public string GetNobleDef()
	{
		if (!IsAuthority())
		{
			return null;
		}
		DT.Field field = game.dt.Find("culture_models");
		if (field == null)
		{
			return null;
		}
		DT.Field field2 = ((rebel == null) ? field.FindChild("marshal_defs") : field.FindChild("rebel_marshal_defs"));
		if (field2 == null)
		{
			return null;
		}
		DT.Field field3 = field.FindChild("models_per_culture");
		if (field3 != null)
		{
			Kingdom kingdom = GetKingdom();
			string text = kingdom.culture;
			if (rebel != null)
			{
				string text2 = rebel.army?.realm_in?.GetKingdom()?.culture;
				if (text2 != null)
				{
					text = text2;
				}
			}
			string text3 = field3.GetString(text);
			if (string.IsNullOrEmpty(text3) && text != null)
			{
				text3 = field3.GetString(kingdom.game.cultures.GetGroup(text) ?? "");
			}
			string text4 = field2.GetString(text3);
			if (string.IsNullOrEmpty(text4))
			{
				text4 = field2.GetString("european");
			}
			return text4;
		}
		return null;
	}

	public Unit AddNoble(bool send_state = true)
	{
		if (!IsAuthority())
		{
			return null;
		}
		string nobleDefKey = GetNobleDefKey();
		if (string.IsNullOrEmpty(nobleDefKey))
		{
			Warning("Failed to add noble unit");
			return null;
		}
		return AddUnit(nobleDefKey, 0, mercenary: false, send_state);
	}

	public Unit AddUnit(string def_id, int slot_index = -1, bool mercenary = false, bool send_state = true)
	{
		Unit.Def def = game.defs.Get<Unit.Def>(def_id);
		if (!IsAuthority() && send_state)
		{
			SendEvent(new AddUnitEvent(def, slot_index, mercenary));
			return null;
		}
		return AddUnit(def, slot_index, mercenary, send_state);
	}

	public Unit AddUnit(Unit.Def def, int slot_index = -1, bool mercenary = false, bool send_state = true, bool send_event = false)
	{
		if (!IsAuthority() && send_state)
		{
			if (send_event)
			{
				SendEvent(new AddUnitEvent(def, slot_index, mercenary));
			}
			return null;
		}
		if (def == null)
		{
			return null;
		}
		if (def.type == Unit.Type.InventoryItem && battle?.batte_view_game == null)
		{
			Error($"Trying to add inventory item {def} as unit outside of battleview");
			return null;
		}
		Unit unit = new Unit();
		unit.def = def;
		unit.salvo_def = game.defs.Get<SalvoData.Def>(def.salvo_def);
		unit.SetArmy(this);
		unit.mercenary = mercenary;
		AddUnit(unit, slot_index, send_state);
		return unit;
	}

	public bool TransferUnit(MapObject dest, Unit unit, Unit swap_target)
	{
		if (unit == null)
		{
			return false;
		}
		if (dest == null)
		{
			return false;
		}
		if (unit.def.type == Unit.Type.Noble)
		{
			return false;
		}
		if (battle != null)
		{
			return false;
		}
		if (!IsAuthority())
		{
			int swap_unit_idx = -1;
			if (dest is Army)
			{
				swap_unit_idx = (dest as Army).units.IndexOf(swap_target);
			}
			else if (dest is Castle)
			{
				swap_unit_idx = (dest as Castle).garrison.units.IndexOf(swap_target);
			}
			SendEvent(new TransferUnitEvent(dest, units.IndexOf(unit), swap_unit_idx));
			return false;
		}
		if (dest != null)
		{
			if (dest is Army army)
			{
				Army army2 = army;
				if (swap_target == null && army2.units.Count - 1 >= army2.MaxUnits())
				{
					return false;
				}
				int num = units.IndexOf(unit);
				int num2 = ((swap_target != null) ? army2.units.IndexOf(swap_target) : (-1));
				if (army2 == this && num >= 0 && num2 >= 0)
				{
					SwapUnits(num, num2);
					return true;
				}
				DelUnit(unit);
				if (swap_target != null)
				{
					army2.DelUnit(swap_target);
				}
				army2.AddUnit(unit, num2);
				if (swap_target != null)
				{
					AddUnit(swap_target, num);
					unit.OnAssignedAnalytics(army2, null, "army", "swapped");
					swap_target.OnAssignedAnalytics(this, null, "army", "swapped");
				}
				else
				{
					unit.OnAssignedAnalytics(army2, null, "army", "moved");
				}
				return true;
			}
			if (dest is Castle { garrison: var garrison })
			{
				if (garrison == null)
				{
					return false;
				}
				if (swap_target == null && garrison.units.Count >= garrison.SlotCount())
				{
					return false;
				}
				int slot_index = units.IndexOf(unit);
				int num3 = ((swap_target != null) ? garrison.units.IndexOf(swap_target) : (-1));
				DelUnit(unit);
				if (num3 != -1)
				{
					garrison.DelUnit(num3);
				}
				garrison.AddUnit(unit, send_state: true, check_slots: false, num3);
				if (num3 != -1)
				{
					AddUnit(swap_target, slot_index);
					unit.OnAssignedAnalytics(null, garrison, "garrison", "swapped");
					swap_target.OnAssignedAnalytics(this, null, "army", "swapped");
				}
				else
				{
					unit.OnAssignedAnalytics(null, garrison, "garrison", "moved");
				}
				return true;
			}
		}
		Game.Log($"Transferring units to a {dest?.GetType()} is not supported!", Game.LogType.Error);
		return false;
	}

	public bool CanMergeUnits(Unit source, Unit target)
	{
		if (source == null || target == null)
		{
			return false;
		}
		if (source == target)
		{
			return false;
		}
		if (source.def != target.def)
		{
			return false;
		}
		if (source.def.type == Unit.Type.Noble)
		{
			return false;
		}
		if (target.def.type == Unit.Type.Noble)
		{
			return false;
		}
		if (source.army == null && source.garrison == null)
		{
			return false;
		}
		if (target.army == null && target.garrison == null)
		{
			return false;
		}
		if (!source.army.IsValid() || source.army.IsDefeated() || !target.army.IsValid() || target.army.IsDefeated())
		{
			return false;
		}
		if (source.damage <= 0f || target.damage <= 0f)
		{
			return false;
		}
		return true;
	}

	public void MergeUnits(Unit source, Unit target, bool send_state)
	{
		if (!CanMergeUnits(source, target))
		{
			return;
		}
		if (!IsAuthority() && send_state)
		{
			MapObject target_owner = (MapObject)(((object)target.army) ?? ((object)target.garrison?.settlement));
			SendEvent(new MergeUnitsEvent(source.Index(), target.Index(), target_owner));
			return;
		}
		float num = source.damage + target.damage;
		float num2 = Math.Min(2f - num, 1f);
		target.damage = 1f - num2;
		source.damage = num - target.damage;
		int num3 = (int)((float)source.max_manpower_modified() * source.health);
		if (source.damage >= 1f || num3 == 0)
		{
			if (source.experience > target.experience)
			{
				target.AddExperience(source.experience - target.experience);
			}
			DelUnit(source, send_state: false);
		}
		castle?.GetRealm()?.rebellionRisk.Recalc();
		if (IsAuthority())
		{
			if (send_state)
			{
				SendState<UnitsState>();
			}
			target.OnAssignedAnalytics(this, null, "army", "merge");
		}
		NotifyListeners("units_changed");
	}

	public bool AIMergeUnits()
	{
		if (!IsAuthority())
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom?.ai == null || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Units))
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < units.Count - 1; i++)
		{
			Unit unit = units[i];
			if (unit.damage == 0f)
			{
				continue;
			}
			for (int j = i + 1; j < units.Count; j++)
			{
				Unit unit2 = units[j];
				if (unit2.damage != 0f && CanMergeUnits(unit, unit2))
				{
					MergeUnits(unit, unit2, send_state: true);
					result = true;
					if (unit.army == null)
					{
						break;
					}
				}
			}
		}
		return result;
	}

	public bool MoveUnitFromGarrison(Unit unit, bool send_event = true)
	{
		if (castle.battle != null)
		{
			return false;
		}
		if (units.Count - 1 >= MaxUnits())
		{
			return false;
		}
		if (unit.damage > 0f && leader?.cur_action is CampArmyAction)
		{
			return false;
		}
		if (!IsAuthority() && send_event)
		{
			SendEvent(new MoveUnitGarrisonEvent(castle.garrison.units.IndexOf(unit), from_garrison: true, castle));
			return true;
		}
		castle.garrison.DelUnit(unit);
		AddUnit(unit);
		if (IsAuthority())
		{
			unit.OnAssignedAnalytics(this, castle.garrison, "army", "move");
		}
		return true;
	}

	public void TakeAndClearGarrison()
	{
		if (IsAuthority() && castle != null && castle.garrison != null && castle.garrison.units != null)
		{
			while (castle.garrison.units.Count > 0)
			{
				bool send_state = castle.garrison.units.Count == 1;
				Unit unit = castle.garrison.units[0];
				AddUnit(unit, -1, send_state);
				castle.garrison.DelUnit(unit, send_state);
			}
			ClearUnitsOverMax();
		}
	}

	public void MoveUnitToGarrison(Unit unit, bool send_state = true)
	{
		if (castle.battle == null && castle.garrison.units.Count < castle.garrison.SlotCount() && (!(unit.damage > 0f) || !(leader?.cur_action is CampArmyAction)))
		{
			if (!IsAuthority() && send_state)
			{
				SendEvent(new MoveUnitGarrisonEvent(units.IndexOf(unit), from_garrison: false, castle));
				return;
			}
			DelUnit(unit);
			castle.garrison.AddUnit(unit);
		}
	}

	public bool SwapUnitWithGarrison(Unit army_unit, Unit garrison_unit)
	{
		return TransferUnit(castle, army_unit, garrison_unit);
	}

	public static List<string> GetRecruitableUnitIDs(DT dt, string base_type = null)
	{
		DT.Def def = dt.FindDef("Unit");
		if (def == null || def.defs == null || def.defs.Count < 1)
		{
			return null;
		}
		List<string> list = new List<string>();
		for (int i = 0; i < def.defs.Count; i++)
		{
			DT.Field field = def.defs[i].field;
			if (string.IsNullOrEmpty(field.base_path) || field.base_path == "InventoryItem" || field.base_path == "SiegeEquipment")
			{
				continue;
			}
			if (base_type != null)
			{
				if (field.base_path != base_type)
				{
					continue;
				}
			}
			else if (field.base_path == "Noble")
			{
				continue;
			}
			if (field.GetBool("available"))
			{
				list.Add(field.Path());
			}
		}
		return list;
	}

	public int CountInventory(Unit.Def def, int stop_on = -1)
	{
		if (def == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < siege_equipment.Count && i != stop_on; i++)
		{
			if (siege_equipment[i]?.def == def)
			{
				num++;
			}
		}
		return num;
	}

	public void FillWithRandomUnits()
	{
		if (units.Count < 1)
		{
			AddNoble();
		}
		List<string> recruitableUnitIDs = GetRecruitableUnitIDs(game.dt);
		int num = MaxUnits() + 1;
		while (units.Count < num)
		{
			int index = game.Random(1, recruitableUnitIDs.Count);
			string def_id = recruitableUnitIDs[index];
			AddUnit(def_id, -1, mercenary: false, send_state: false);
		}
		SendState<UnitsState>();
		if (base.started)
		{
			NotifyListeners("units_changed");
		}
	}

	public void AddInvetoryItem(InventoryItem unit, int slotIndex, bool send_state = true, bool force_battle = false)
	{
		if (!IsAuthority() && send_state)
		{
			SendEvent(new AddItemEvent(unit.def));
		}
		else if ((battle == null || force_battle) && (siege_equipment.Count < MaxItems() || force_battle))
		{
			unit.army = this;
			siege_equipment.Add(unit);
			if (send_state)
			{
				SendState<UnitsState>();
			}
			if (base.started && send_state)
			{
				NotifyListeners("inventory_changed");
			}
			if (unit.def.add_supplies > 0f)
			{
				RecalcMaxSupplies();
				AddSupplies(unit.def.add_supplies);
			}
			SetWorldSpeed();
		}
	}

	public InventoryItem AddInvetoryItem(string def_id, int slotIndex, bool send_state = true)
	{
		Unit.Def def = game.defs.Get<Unit.Def>(def_id);
		if (!IsAuthority() && send_state)
		{
			SendEvent(new AddItemEvent(def));
			return null;
		}
		return AddInvetoryItem(def, slotIndex, send_state);
	}

	public InventoryItem AddInvetoryItem(Unit.Def def, int slotIndex, bool send_state = true, bool send_event = false, bool force_battle = false)
	{
		if (!IsAuthority() && send_state)
		{
			if (send_event)
			{
				SendEvent(new AddItemEvent(def));
			}
			return null;
		}
		if (def == null)
		{
			return null;
		}
		InventoryItem inventoryItem = new InventoryItem();
		inventoryItem.def = def;
		AddInvetoryItem(inventoryItem, slotIndex, send_state, force_battle);
		return inventoryItem;
	}

	public void DelInvetoryItem(InventoryItem unit, bool send_state = true)
	{
		if (unit == null)
		{
			return;
		}
		if (!IsAuthority() && send_state)
		{
			SendEvent(new DelItemEvent(unit));
			return;
		}
		unit.army = null;
		siege_equipment.Remove(unit);
		if (send_state)
		{
			SendState<UnitsState>();
		}
		if (base.started && send_state)
		{
			NotifyListeners("inventory_changed");
		}
		if (unit.def.add_supplies > 0f)
		{
			RecalcMaxSupplies();
		}
		SetWorldSpeed();
	}

	public void GetNobleDefeated(out bool has_nobles, out bool living_nobles)
	{
		living_nobles = false;
		has_nobles = false;
		if (units.Count < 1)
		{
			return;
		}
		bool flag = battle != null;
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit.army == this && (!flag || unit.simulation != null))
			{
				if (!unit.IsDefeated() && unit.def.type == Unit.Type.Noble)
				{
					living_nobles = true;
				}
				if (unit.def.type == Unit.Type.Noble)
				{
					has_nobles = true;
				}
			}
		}
	}

	public bool IsDefeated()
	{
		if (!IsValid())
		{
			return true;
		}
		if (units.Count < 1)
		{
			return true;
		}
		bool flag = true;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = battle != null;
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit.army != this || (flag4 && unit.simulation == null))
			{
				continue;
			}
			if (!unit.IsDefeated())
			{
				flag = false;
				if (unit.def.type == Unit.Type.Noble)
				{
					flag2 = true;
				}
			}
			if (unit.def.type == Unit.Type.Noble)
			{
				flag3 = true;
			}
		}
		if (flag3)
		{
			return !flag2;
		}
		if (flag)
		{
			return true;
		}
		return false;
	}

	public bool IsHeadless()
	{
		return leader == null;
	}

	public Unit GetUnit(int idx)
	{
		if (idx < 0 || idx >= units.Count)
		{
			return null;
		}
		return units[idx];
	}

	public InventoryItem GetEquipment(int idx)
	{
		if (idx < 0 || idx >= siege_equipment.Count)
		{
			return null;
		}
		return siege_equipment[idx];
	}

	public Unit GetUnitAtBattlePos(int row, int col)
	{
		if (row < 0 || col < 0)
		{
			return null;
		}
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit.battle_row == row && unit.battle_col == col)
			{
				return unit;
			}
		}
		return null;
	}

	public void InvalidateBattleFormation()
	{
		battle_formation_valid = false;
	}

	private static int CompareUnitsByBattleRow(Unit u1, Unit u2)
	{
		return u1.def.battle_row.CompareTo(u2.def.battle_row);
	}

	private static int CompareUnitsByBattleCol(Unit u1, Unit u2)
	{
		return u1.def.battle_col.CompareTo(u2.def.battle_col);
	}

	public static int CalcBattleFormation(List<Unit> units, int start_idx, List<Unit> add_units)
	{
		tmp_units.Clear();
		if (units != null)
		{
			for (int i = start_idx; i < units.Count; i++)
			{
				Unit item = units[i];
				tmp_units.Add(item);
			}
		}
		if (add_units != null)
		{
			tmp_units.AddRange(add_units);
		}
		if (tmp_units.Count == 0)
		{
			return 0;
		}
		tmp_units.Sort(CompareUnitsByBattleRow);
		int num = battle_cols / 2;
		int num2 = 0;
		int num3 = 0;
		while (true)
		{
			int num4 = battle_cols;
			if (num3 + num4 > tmp_units.Count)
			{
				num4 = tmp_units.Count - num3;
			}
			tmp_units2.Clear();
			for (int j = 0; j < num4; j++)
			{
				Unit item2 = tmp_units[num3 + j];
				tmp_units2.Add(item2);
			}
			tmp_units2.Sort(CompareUnitsByBattleCol);
			int num5 = num;
			for (int k = 0; k < num4; k++)
			{
				Unit unit = tmp_units2[k];
				unit.battle_row = num2;
				unit.battle_col = num5;
				int num6 = num5 - num;
				num6 = ((num6 < 0) ? (-num6) : (-(num6 + 1)));
				num5 = num + num6;
			}
			num3 += num4;
			if (num3 >= tmp_units.Count)
			{
				break;
			}
			num2++;
		}
		tmp_units.Clear();
		tmp_units2.Clear();
		return num2;
	}

	public void CalcBattleFormation(bool forced = false)
	{
		if (!battle_formation_valid || forced)
		{
			battle_formation_valid = true;
			int num = CalcBattleFormation(units, 1, null);
			if (units.Count > 0)
			{
				Unit unit = units[0];
				unit.battle_row = num + 1;
				unit.battle_col = battle_cols / 2;
			}
		}
	}

	public void SetBattle(Battle battle, bool set_reinforcement = true, bool send_state = true)
	{
		if (battle == null)
		{
			UpdateWaterInstant();
		}
		this.battle = battle;
		if (battle != null)
		{
			ai_oscillations = 0;
		}
		if (set_reinforcement && battle?.batte_view_game == null)
		{
			CancelReinforcement();
		}
		UpdateRealmIn();
		RecalcSuppliesRate();
		NotifyListeners("battle_changed");
		if (battle == null && !send_state)
		{
			for (int i = 0; i < units.Count; i++)
			{
				units[i].simulation = null;
			}
		}
		morale.RecalcPermanentMorale();
		had_supplies_at_start_of_battle = supplies.Get() > 0f;
		if (send_state)
		{
			SendState<BattleState>();
			if (battle != null && mercenary != null)
			{
				mercenary.SetAction(Mercenary.Action.Attack);
			}
		}
		if (battle != null)
		{
			CancelDisband();
			if (battle.batte_view_game == null)
			{
				Stop();
			}
			if (water_crossing.running)
			{
				water_crossing.Stop();
			}
		}
		else
		{
			AIMergeUnits();
		}
	}

	public bool RetreatBattle()
	{
		if (battle == null)
		{
			return true;
		}
		battle.Leave(this);
		if (leader != null && leader.IsValid())
		{
			leader.NotifyListeners("battle_retreat");
		}
		last_retreat_time = game.time;
		return true;
	}

	public bool CanLeaveBattle()
	{
		if (battle == null)
		{
			return false;
		}
		if (battle.is_siege && battle.defender.IsOwnStance(this))
		{
			return false;
		}
		return true;
	}

	public bool LeaveBattle(bool check = true)
	{
		if (battle == null)
		{
			return true;
		}
		if (check && !CanLeaveBattle())
		{
			return false;
		}
		battle.Leave(this);
		return true;
	}

	public void CancelDisband()
	{
		if (leader != null && leader.cur_action is DisbandArmyAction disbandArmyAction)
		{
			disbandArmyAction.Cancel();
		}
	}

	public void Stop(bool send_state = true)
	{
		movement.Stop();
	}

	public void SetWorldSpeed()
	{
		if (!IsAuthority() || movement == null)
		{
			return;
		}
		float world_move_speed = def.world_move_speed;
		float num = float.MaxValue;
		Kingdom kingdom = realm_in?.GetKingdom();
		Kingdom kingdom2 = GetKingdom();
		if (kingdom2 == null)
		{
			return;
		}
		if (is_in_water)
		{
			num = def.ship_speed_mod;
			if (leader != null)
			{
				num *= 1f + leader.GetStat(Stats.cs_ships_speed_perc) / 100f;
			}
		}
		else
		{
			if (units.Count > 0 || siege_equipment.Count > 0)
			{
				for (int i = 0; i < units.Count; i++)
				{
					if (units[i].def.world_speed_mod < num)
					{
						num = units[i].def.world_speed_mod;
					}
				}
				for (int j = 0; j < siege_equipment.Count; j++)
				{
					if (siege_equipment[j].def.world_speed_mod < num)
					{
						num = siege_equipment[j].def.world_speed_mod;
					}
				}
			}
			else
			{
				num = game.defs.Get<Unit.Def>("Noble").world_speed_mod;
			}
			if (realm_in != null && !realm_in.IsOwnStance(this))
			{
				bool flag = false;
				for (int k = 0; k < realm_in.neighbors.Count; k++)
				{
					if (realm_in.neighbors[k].IsOwnStance(this))
					{
						flag = true;
						break;
					}
				}
				num = ((!flag) ? (num * def.world_move_speed_far_realm_mod) : (num * def.world_move_speed_neighbor_realm_mod));
			}
			num *= 1f + (leader?.GetStat(Stats.cs_army_speed_world_perc) ?? 0f) / 100f;
		}
		if (kingdom != null && kingdom.IsEnemy(kingdom2))
		{
			num *= 1f - realm_in.GetStat(Stats.rs_enemy_move_speed_reduction_perc) / 100f;
		}
		float num2 = 1f;
		if (movement != null && ((movement.pf_path != null && (movement.pf_path.flee || movement.pf_path.modified_flee)) || (movement.path != null && (movement.path.flee || movement.path.modified_flee))))
		{
			num2 *= 1.5f;
		}
		world_move_speed *= num;
		for (int l = 0; l < units.Count; l++)
		{
			units[l].speed_mod = num * num2 / units[l].def.world_speed_mod;
		}
		movement.speed = world_move_speed;
		SendState<SpeedState>();
	}

	public void MoveTo(PPos pt, float range = 0f, bool stop_water = false)
	{
		if (IsFleeing() && !def.can_interrupt_flee)
		{
			return;
		}
		PathData.Node node = game.path_finding.data.GetNode(pt);
		if (node.river)
		{
			return;
		}
		if ((water_crossing.running && !node.water && !water_crossing.can_interrupt) || water_crossing.teleport)
		{
			if (!stop_water)
			{
				return;
			}
			water_crossing.Stop();
		}
		if (battle == null)
		{
			CancelDisband();
			SetOffFromCamp();
			if (rebel != null)
			{
				rebel.HandleArmyMove();
			}
			movement.MoveTo(pt, range);
		}
	}

	public void AddMoveTo(PPos pt, float range = 0f, bool stop_water = false)
	{
		if (IsFleeing())
		{
			return;
		}
		PathData.Node node = game.path_finding.data.GetNode(pt);
		if (node.river)
		{
			return;
		}
		if ((water_crossing.running && !node.water && !water_crossing.can_interrupt) || water_crossing.teleport)
		{
			if (!stop_water)
			{
				return;
			}
			water_crossing.Stop();
		}
		if (battle == null && rebel == null)
		{
			movement.AddMoveTo(pt, range);
		}
	}

	public float CalcMoveToRange(MapObject dst_obj)
	{
		return dst_obj.GetRadius() + GetRadius();
	}

	public void MoveTo(MapObject dst_obj, float range = -1f, bool stop_water = false)
	{
		if (dst_obj == null || game.path_finding?.data == null)
		{
			Stop();
		}
		else
		{
			if (IsFleeing() && !def.can_interrupt_flee)
			{
				return;
			}
			PathData.Node node = game.path_finding.data.GetNode(dst_obj.position);
			if (water_crossing.running && ((!node.water && !water_crossing.can_interrupt) || water_crossing.teleport))
			{
				if (!stop_water)
				{
					return;
				}
				water_crossing.Stop();
			}
			if (battle != null)
			{
				return;
			}
			CancelDisband();
			SetOffFromCamp();
			if (range < 0f)
			{
				range = CalcMoveToRange(dst_obj);
			}
			if (dst_obj is Army)
			{
				Army army = dst_obj as Army;
				if (army.mercenary != null)
				{
					army.mercenary.AddIntender(this);
				}
			}
			if (rebel != null)
			{
				rebel.HandleArmyMove();
			}
			movement.MoveTo(dst_obj, range);
		}
	}

	public void AddMoveTo(MapObject dst_obj, float range = 0f, bool stop_water = false)
	{
		if (IsFleeing())
		{
			return;
		}
		PathData.Node node = game.path_finding.data.GetNode(dst_obj.position);
		if (water_crossing.running && ((!node.water && !water_crossing.can_interrupt) || water_crossing.teleport))
		{
			if (!stop_water)
			{
				return;
			}
			water_crossing.Stop();
		}
		if (battle != null)
		{
			return;
		}
		if (range < 0f)
		{
			range = CalcMoveToRange(dst_obj);
		}
		if (dst_obj is Army)
		{
			Army army = dst_obj as Army;
			if (army.mercenary != null)
			{
				army.mercenary.AddIntender(this);
			}
		}
		movement.AddMoveTo(dst_obj, range);
	}

	public void CancelReinforcement()
	{
		if (!IsAuthority())
		{
			return;
		}
		Battle battle = last_intended_battle;
		if (battle == null)
		{
			return;
		}
		battle.GarbageCollectIntendedReinforcement(this);
		for (int i = 0; i < battle.reinforcements.Length; i++)
		{
			if (battle.reinforcements[i].army == this)
			{
				battle.SetReinforcements(null, i, -1f, force: true);
				battle.ReplaceValidReinforcement(i % 2, i);
			}
		}
	}

	public void FleeFrom(PPos pt, float range)
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom.type == Kingdom.Type.Regular && kingdom.realms.Count > 0 && (realm_in == null || !realm_in.IsOwnStance(kingdom)))
		{
			Realm realm = kingdom.realms[0];
			float num = float.MaxValue;
			for (int i = 1; i < kingdom.realms.Count; i++)
			{
				Realm realm2 = kingdom.realms[i];
				float num2 = realm2.castle.position.SqrDist(position);
				if (num2 < num)
				{
					num = num2;
					realm = realm2;
				}
			}
			pt = GetRandomRealmPoint(realm);
			float num3 = pt.Dist(position);
			movement.FleeTo(pt, num3 - range);
		}
		else
		{
			movement.FleeFrom(pt, range);
		}
		SetWorldSpeed();
	}

	public static Point GetRandomRealmPoint(Realm realm)
	{
		if (realm == null)
		{
			return Point.Zero;
		}
		List<Point> list = new List<Point>();
		Game game = realm.game;
		Point point = FactionUtils.WorldToMapPoint(game, realm.castle.position);
		for (int i = 0; i < 5; i++)
		{
			Point point2 = new Point(game.Random(-1f, 1f), game.Random(-1f, 1f));
			point2.Normalize();
			Point point3 = point + point2 * 300f;
			list = new List<Point>();
			FactionUtils.Trace((int)point.x, (int)point.y, (int)point3.x, (int)point3.y, (short)realm.id, game.realm_id_map, list);
			PathFinding path_finding = realm.game.path_finding;
			int num = 2;
			if (list.Count > num)
			{
				list.RemoveRange(list.Count - num - 1, num);
			}
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				Point point4 = FactionUtils.MapToWorldPoint(game, list[num2]);
				path_finding.data.WorldToGrid(point4, out var x, out var y);
				PathData.Node node = path_finding.data.GetNode(x, y);
				if (node.lsa == 0 || node.town || node.river || node.water || node.coast)
				{
					list.RemoveAt(num2);
				}
				else if (IsNearSettelment(realm, point4, 10f) || PathData.IsNearRiver(path_finding?.data, point4, 6f) || PathData.IsNearOcean(path_finding?.data, point4, 6f))
				{
					list.RemoveAt(num2);
				}
			}
			if (list.Count != 0)
			{
				break;
			}
		}
		if (list.Count == 0)
		{
			int min = ((realm.settlements.Count > 1) ? 1 : 0);
			int index = realm.game.Random(min, realm.settlements.Count - 1);
			return realm.settlements[index].GetRandomExitPoint(try_exit_outside_town: true, check_water: true);
		}
		if (list.Count == 1)
		{
			return FactionUtils.MapToWorldPoint(game, list[0]);
		}
		return FactionUtils.MapToWorldPoint(game, list[game.Random(0, list.Count - 1)]);
	}

	private static bool IsNearSettelment(Realm realm, Point p, float range)
	{
		for (int i = 0; i < realm.settlements.Count; i++)
		{
			Settlement settlement = realm.settlements[i];
			if (settlement.IsActiveSettlement() && p.Dist(settlement.position) < range)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsFleeing()
	{
		return (movement.pf_path ?? movement.path)?.flee ?? false;
	}

	public override IRelationCheck GetStanceObj()
	{
		if (rebel != null && rebel.rebellion != null)
		{
			return rebel.rebellion;
		}
		if (game?.religions?.catholic?.crusade?.army == this)
		{
			return game.religions.catholic.crusade;
		}
		return base.GetStanceObj();
	}

	public override void OnBeginMoving()
	{
		base.OnBeginMoving();
		ClearInteractors();
		ResetSegmentIdxCheck();
		HandleWater(realm_in);
		LeaveCastle(position);
		if (IsAuthority())
		{
			MapObject mapObject = movement.ResolveFinalDestination(movement.path?.dst_obj);
			if (mapObject != last_intended_battle)
			{
				CancelReinforcement();
			}
			if (mapObject is Battle battle)
			{
				battle.AddIntendedReinforcement(this, force: true);
			}
		}
		SetWorldSpeed();
	}

	public override void OnStopMoving()
	{
		base.OnStopMoving();
		if (IsAuthority() && battle?.batte_view_game == null)
		{
			CancelReinforcement();
		}
	}

	public void NewMercenaryInKingdomMessage()
	{
		if (realm_in != null && last_message_kingdom_id != realm_in.kingdom_id && !IsHiredMercenary())
		{
			last_message_kingdom_id = realm_in.kingdom_id;
			if (IsAuthority())
			{
				realm_in.GetKingdom()?.FireEvent("mercenary_spawned", this);
			}
		}
	}

	public override void DestinationReached(MapObject dst_obj)
	{
		if (thinking_at_end_of_path || !IsValid())
		{
			return;
		}
		if (mercenary != null)
		{
			mercenary.RestartDespawnTimer();
			NewMercenaryInKingdomMessage();
		}
		if (dst_obj == null || !dst_obj.IsValid())
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom?.ai != null && kingdom.type == Kingdom.Type.Regular && kingdom.ai.Enabled(KingdomAI.EnableFlags.Armies))
			{
				thinking_at_end_of_path = true;
				kingdom.ai.ThinkArmy(this);
				thinking_at_end_of_path = false;
			}
		}
		else if (dst_obj is Settlement settlement)
		{
			if (settlement.position.SqrDist(position) > 1600f)
			{
				return;
			}
			if (settlement.battle != null)
			{
				settlement.battle.Join(this);
			}
			else if (IsEnemy(settlement) && settlement.IsActiveSettlement())
			{
				Battle.StartBattle(this, settlement);
			}
			else if (settlement is Castle castle)
			{
				if (IsOwnStance(castle))
				{
					EnterCastle(castle);
				}
				else if (IsEnemy(castle))
				{
					Battle.StartBattle(this, castle);
				}
			}
		}
		else if (dst_obj is Army army)
		{
			if (!army.IsValid())
			{
				return;
			}
			if (army.battle != null)
			{
				army.battle.Join(this);
			}
			else
			{
				if ((rebel != null && army.mercenary != null && army.mercenary.ValidForHireAsArmy()) || (mercenary != null && mercenary.ValidForHireAsArmy() && army.rebel != null))
				{
					return;
				}
				if (IsEnemy(army))
				{
					if (is_in_water == !army.currently_on_land)
					{
						Battle.StartBattle(this, army);
					}
				}
				else if (army.mercenary != null && !army.movement.IsMoving())
				{
					army.mercenary.Interact(this);
				}
				else if (IsOwnStance(army) && mercenary == null && rebel == null && !army.movement.IsMoving())
				{
					interact_target = army;
					army.interactor = this;
					SendState<InteractorsState>();
					army.SendState<InteractorsState>();
					NotifyListeners("inetractors_changed");
					army.NotifyListeners("inetractors_changed");
				}
			}
		}
		else if (dst_obj is Battle battle)
		{
			battle.Join(this);
		}
	}

	public bool ValidateDestination(MapObject dst_obj)
	{
		if (dst_obj == null)
		{
			return true;
		}
		if (!dst_obj.IsValid())
		{
			return false;
		}
		Battle battle = null;
		if (dst_obj != null)
		{
			if (!(dst_obj is Settlement settlement))
			{
				if (!(dst_obj is Army army))
				{
					if (dst_obj is Battle battle2)
					{
						battle = battle2;
					}
				}
				else
				{
					Army army2 = army;
					battle = army2.battle;
					if (battle == null)
					{
						if (army2.kingdom_id == kingdom_id)
						{
							return true;
						}
						if (IsEnemy(army2))
						{
							return true;
						}
						if (army2.IsMercenary())
						{
							return true;
						}
						return false;
					}
				}
			}
			else
			{
				Settlement settlement2 = settlement;
				battle = settlement2.battle;
				if (battle == null)
				{
					if (!(settlement2 is Castle castle))
					{
						return true;
					}
					if (castle.kingdom_id == kingdom_id)
					{
						return true;
					}
					if (IsEnemy(castle))
					{
						return true;
					}
					return false;
				}
			}
		}
		if (battle == null)
		{
			return true;
		}
		if (battle.CanJoin(this))
		{
			return true;
		}
		return false;
	}

	public void BecomeMercenary(int former_owner)
	{
		Mercenary.Def def = game.defs.GetBase<Mercenary.Def>();
		if (def == null)
		{
			return;
		}
		Stop();
		if (IsEmpty())
		{
			Destroy();
			return;
		}
		for (int num = siege_equipment.Count - 1; num >= 0; num--)
		{
			InventoryItem unit = siege_equipment[num];
			DelInvetoryItem(unit);
		}
		int parent_kingdom_id = GetKingdom().id;
		if (former_owner != 0 && game.GetKingdom(former_owner).type != Kingdom.Type.Regular && leader != null)
		{
			former_owner = leader.kingdom_id;
			parent_kingdom_id = former_owner;
		}
		Mercenary mercenary = new Mercenary(game, parent_kingdom_id, realm_in, def);
		mercenary.former_owner_id = former_owner;
		mercenary.army = this;
		mercenary.CreateNewLeader();
		this.mercenary = mercenary;
		RecalcSuppliesRate();
		SetSupplies(supplies.GetMax());
		SetKingdom(game.GetKingdom(mercenary.kingdom_id).id);
		if (realm_in != null && realm_in.IsSeaRealm())
		{
			Realm nearbyRealm = MercenarySpawner.GetNearbyRealm(realm_in, 25, former_owner, this);
			if (nearbyRealm?.castle != null)
			{
				MoveTo(nearbyRealm.castle.GetRandomExitPoint(try_exit_outside_town: true, check_water: true));
			}
		}
		else
		{
			realm_in?.AddMercenary(this);
			realm_in?.GetKingdom()?.AddMercenary(this);
			if (castle != null)
			{
				LeaveCastle(castle.position);
			}
		}
		NotifyListeners("became_mercenary");
		mercenary.SetAction(Mercenary.Action.Rest);
	}

	public bool HasNoble()
	{
		for (int i = 0; i < units.Count; i++)
		{
			if (units[i].def.type == Unit.Type.Noble)
			{
				return true;
			}
		}
		return false;
	}

	public void SetUpCamp()
	{
		RecalcSuppliesRate();
		NotifyListeners("camp_setup");
	}

	public void SetOffFromCamp()
	{
		RecalcSuppliesRate();
		NotifyListeners("camp_setoff");
		if (leader != null)
		{
			Action cur_action = leader.cur_action;
			if (cur_action != null && !cur_action.def.secondary)
			{
				cur_action.Cancel();
			}
		}
	}

	public bool IsEmpty()
	{
		if (units == null)
		{
			return true;
		}
		if (units.Count == 0)
		{
			return true;
		}
		if (units.Count == 1 && units[0].def.type == Unit.Type.Noble)
		{
			return true;
		}
		return false;
	}

	public void RestUnits()
	{
		for (int i = 0; i < units.Count; i++)
		{
			units[i].SetDamage(0f, send_state: false);
		}
		SendState<UnitsState>();
		NotifyListeners("units_changed");
	}

	public void RestUnits(float elapsed)
	{
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			float damage = unit.damage;
			damage -= elapsed;
			unit.SetDamage(damage);
		}
		NotifyListeners("units_changed");
	}

	public int GetMaxManPower()
	{
		int num = 0;
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				int num2 = unit.max_manpower_modified_locked_in_battle();
				num += num2;
			}
		}
		return num;
	}

	public int GetManpowerForSquads(Func<Unit, int> manpower_func, int from = 0, int to = int.MaxValue)
	{
		to = Math.Min(to, units.Count);
		int num = 0;
		for (int i = from; i < to; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				num += manpower_func(unit);
			}
		}
		return num;
	}

	public float GetAdditionalTroopsPerc()
	{
		float num = 0f;
		for (int i = 0; i < siege_equipment.Count; i++)
		{
			InventoryItem inventoryItem = siege_equipment[i];
			if (inventoryItem?.def != null)
			{
				num += inventoryItem.def.add_squad_size_perc;
			}
		}
		return num;
	}

	public int GetManPower()
	{
		int num = 0;
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				int num2 = unit.manpower_alive_modified();
				num += num2;
			}
		}
		return num;
	}

	public int GetLevyBonus()
	{
		if (game == null)
		{
			return 0;
		}
		if (levy_bonus_cache == null)
		{
			levy_bonus_cache = new ValueCache(delegate
			{
				if (leader == null || leader.IsCrusader())
				{
					return 0;
				}
				Realm realm = leader?.governed_castle?.GetRealm();
				if (realm == null)
				{
					return 0;
				}
				int num = (int)realm.income.Get(ResourceType.Levy);
				num = (int)((float)num * def.levy_troop_bonus);
				return Math.Min(100, num);
			}, game);
		}
		return levy_bonus_cache.GetValue();
	}

	public float GetDamage()
	{
		int maxManPower = GetMaxManPower();
		int manPower = GetManPower();
		return 1f - (float)manPower / (float)maxManPower;
	}

	public Resource GetUpkeep()
	{
		if (IsHiredMercenary())
		{
			return mercenary?.mission_def?.GetUpkeep(mercenary, GetKingdom());
		}
		Resource resource = new Resource();
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				Resource upkeep = unit.GetUpkeep();
				resource.Add(upkeep, 1f);
			}
		}
		for (int j = 0; j < siege_equipment.Count; j++)
		{
			InventoryItem inventoryItem = siege_equipment[j];
			if (inventoryItem != null)
			{
				Resource upkeep2 = inventoryItem.GetUpkeep(j);
				resource.Add(upkeep2, 1f);
			}
		}
		return resource;
	}

	public Resource GetUpkeepMerc(Kingdom k = null)
	{
		Resource resource = new Resource();
		if (units == null || units.Count == 0)
		{
			return resource;
		}
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				resource.Add(unit.GetUpkeepMerc(k), 1f);
			}
		}
		return resource;
	}

	public Army(Game game, Point position, int kingdom_id)
		: base(game, position, kingdom_id)
	{
	}

	protected override void OnStart()
	{
		is_in_on_start = true;
		base.OnStart();
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			kingdom.AddArmy(this, !IsMercenary() && rebel == null);
		}
		else
		{
			Warning("Army has no kingdom");
		}
		UpdateRealmIn();
		is_in_on_start = false;
	}

	protected override void OnDestroy()
	{
		if (battle != null && !battle.destroyed && battle.stage < Battle.Stage.Finishing)
		{
			battle.Leave(this, check_victory: true);
		}
		if (leader != null && IsAuthority())
		{
			leader.SetLocation(null);
			leader.stats?.DelListener(this);
		}
		GetKingdom()?.DelArmy(this);
		if (realm_in != null)
		{
			realm_in.DelArmy(this);
			realm_in.GetKingdom()?.DelArmyIn(this);
			NotifyListeners("realm_crossed");
		}
		if (IsAuthority())
		{
			CancelReinforcement();
			UpdateOccupyRealmCheck(realm_in);
			if (castle != null)
			{
				castle.SetArmy(null);
			}
			if (leader != null && leader.IsAlive() && game.map_name != null && leader.prison_kingdom == null)
			{
				leader.Die(new DeadStatus("defeated_in_combat", leader));
			}
			if (rebel != null && rebel.IsValid())
			{
				rebel.Destroy();
			}
			if (mercenary != null && mercenary.IsValid())
			{
				mercenary.Destroy();
			}
			ClearInteractors();
		}
		base.OnDestroy();
	}

	public float GetSiegeStrength(float siege_penalty)
	{
		float num = 1f;
		float num2 = 0f;
		for (int i = 0; i < units.Count; i++)
		{
			num += units[i].siege_strength_modified();
		}
		return num * (1f + num2 - siege_penalty);
	}

	public void InitSupplies()
	{
		float num = CalcMaxSupplies();
		supplies = new ComputableValue(num, 0f - def.supplies_consumption_default, game, 0f, num);
		RecalcSuppliesRate();
	}

	public float CalcMaxSupplies()
	{
		float num = ((def != null) ? def.supplies_capacity_base : 100f);
		for (int i = 0; i < siege_equipment.Count; i++)
		{
			InventoryItem inventoryItem = siege_equipment[i];
			if (inventoryItem?.def != null)
			{
				num += inventoryItem.def.add_supplies;
			}
		}
		return num;
	}

	public void RecalcMaxSupplies(bool send_state = true)
	{
		if (supplies == null)
		{
			InitSupplies();
			return;
		}
		float num = CalcMaxSupplies();
		if (num != supplies.GetMax())
		{
			supplies.SetMinMax(0f, num);
			if (send_state)
			{
				SendState<FoodState>();
			}
		}
	}

	public void AddSupplies(float add, bool send_state = true)
	{
		if (!IsMercenary())
		{
			if (supplies == null)
			{
				InitSupplies();
			}
			else
			{
				supplies.Add(add);
			}
			if (send_state)
			{
				SendState<FoodState>();
			}
			NotifyListeners("army_food_changed");
		}
	}

	public void SetSupplies(float val, bool send_state = true)
	{
		if (!IsMercenary())
		{
			if (supplies == null)
			{
				InitSupplies();
			}
			else
			{
				supplies.Set(val);
			}
			if (send_state)
			{
				SendState<FoodState>();
			}
			NotifyListeners("army_food_changed");
		}
	}

	public float MissingSupplies()
	{
		if (supplies == null)
		{
			return 0f;
		}
		return (float)Math.Floor(supplies.GetMax() - supplies.Get());
	}

	public void RecalcSuppliesRate()
	{
		if (!IsAuthority())
		{
			return;
		}
		float num = 0f;
		float num2 = 1f;
		float num3 = 1f;
		float num4 = 1f;
		float num5 = 1f;
		float num6 = 1f;
		float num7 = 1f;
		float num8 = 1f;
		if (mercenary != null)
		{
			num = 0f;
		}
		else
		{
			int manPower = GetManPower();
			if (battle != null)
			{
				string key = ((battle.type != Battle.Type.PlunderInterrupt) ? battle.type.ToString() : Battle.Type.Plunder.ToString());
				num2 = def.supplies_consumption[key];
			}
			num6 = def.supplies_min_mod_per_troops + (def.supplies_max_mod_per_troops - def.supplies_min_mod_per_troops) * (Math.Max(Math.Min(manPower, def.supplies_troop_max), def.supplies_troop_min) - def.supplies_troop_min) / (def.supplies_troop_max - def.supplies_troop_min);
			if (leader != null && leader.IsCrusader())
			{
				num7 = game.religions.catholic.crusade.def.supplies_consumption_rate_mod;
			}
			if (rebel != null)
			{
				num8 = rebel.def.supplies_consumption_rate_mod;
			}
			if (isCamping)
			{
				num5 += def.supplies_consumption["Camping"];
			}
			if (realm_in != null)
			{
				if (realm_in.IsSeaRealm())
				{
					num3 = def.supplies_consumption["Sailing"];
				}
				else
				{
					for (int i = 0; i < realm_in.neighbors.Count; i++)
					{
						if (realm_in.neighbors[i].IsOwnStance(this))
						{
							num4 = def.supplies_consumption["NeighbouringProvinceMod"];
							break;
						}
					}
					RelationUtils.Stance stance = GetStance(realm_in);
					float num9 = 1f + (leader?.GetStat(Stats.cs_army_supply_usage_perc) ?? 0f) / 100f;
					if (stance.IsWar())
					{
						num3 = def.supplies_consumption[RelationUtils.Stance.War.ToString()] * num9;
					}
					else if (stance.IsPeace())
					{
						num3 = def.supplies_consumption[RelationUtils.Stance.Peace.ToString()] * num9;
					}
					else if (stance.IsNonAgression())
					{
						num3 = def.supplies_consumption[RelationUtils.Stance.NonAggression.ToString()] * num9;
					}
					else if (stance.IsAlliance())
					{
						num3 = def.supplies_consumption[RelationUtils.Stance.Alliance.ToString()] * num9;
					}
					else if (stance.IsAnyVassalage())
					{
						num3 = def.supplies_consumption[RelationUtils.Stance.AnyVassalage.ToString()] * num9;
					}
					else if (stance.IsOwn())
					{
						num3 = ((castle == null) ? (def.supplies_consumption[RelationUtils.Stance.Own.ToString()] * num9) : def.supplies_consumption["OwnTown"]);
					}
				}
			}
			num5 += ((FindStatus<ArmyDisorginizedStatus>() != null) ? def.supplies_consumption["Disorganized"] : 0f);
			num = def.supplies_consumption_default * num3 * num2 * num5 * num4 * num6 * num7 * num8;
		}
		float rate = supplies.GetRate();
		float num10 = 0f - num / def.supplies_consumption_interval;
		if (num10 != rate)
		{
			supplies.SetRate(0f - num / def.supplies_consumption_interval);
			SendState<FoodState>();
		}
		if (num10 != 0f || rate != 0f)
		{
			NotifyListeners("army_food_changed");
		}
	}

	public bool CanResupply()
	{
		if (supplies == null)
		{
			return false;
		}
		if (resupply_action == null)
		{
			return false;
		}
		return resupply_action.Validate() == "ok";
	}

	public float GetMorale(bool recalc = true)
	{
		return morale.GetMorale(recalc);
	}

	public float GetPermanentMorale()
	{
		return morale.permanent_morale;
	}

	public float GetTemporaryMorale()
	{
		return morale.temporary_morale.Get();
	}

	public override bool CanUpdatePath()
	{
		if (water_crossing != null && water_crossing.running)
		{
			return false;
		}
		return base.CanUpdatePath();
	}

	public override bool CanReserve()
	{
		return true;
	}

	public override bool CanCurrentlyAvoid()
	{
		if (!base.CanCurrentlyAvoid())
		{
			return false;
		}
		if (water_crossing == null || !water_crossing.running)
		{
			return battle == null;
		}
		return false;
	}

	public override bool IgnoreCollision(MapObject other)
	{
		if (base.IgnoreCollision(other))
		{
			return true;
		}
		if (other is Army army)
		{
			if (army.mercenary != null && !army.mercenary.IsHired())
			{
				return true;
			}
			if (army.currently_on_land != currently_on_land)
			{
				return true;
			}
			if (army.battle != null)
			{
				return army.battle.CanJoin(this);
			}
			if (army.IsEnemy(this))
			{
				if (movement?.path?.original_dst_obj != army && movement?.pf_path?.original_dst_obj != army && army.movement?.path?.original_dst_obj != this)
				{
					return army.movement?.pf_path?.original_dst_obj == this;
				}
				return true;
			}
			if (army.castle != null)
			{
				return true;
			}
			if (army.IsOwnStance(this) && army.battle == null && (army.movement?.last_dst_obj == this || movement?.last_dst_obj == army))
			{
				return true;
			}
			return false;
		}
		if (other is Battle battle)
		{
			return battle.CanJoin(this);
		}
		return true;
	}

	public override float GetRadius()
	{
		return 4f;
	}

	public void CheckSiegeEquipmentCapacity()
	{
		if (!IsAuthority())
		{
			return;
		}
		int num = MaxItems();
		bool flag = false;
		for (int num2 = siege_equipment.Count - 1; num2 >= 0; num2--)
		{
			InventoryItem inventoryItem = siege_equipment[num2];
			if (!inventoryItem.def.buildPrerqusite.Validate(leader))
			{
				DelInvetoryItem(inventoryItem, send_state: false);
				flag = true;
			}
		}
		for (int num3 = siege_equipment.Count - 1; num3 >= num; num3--)
		{
			InventoryItem unit = siege_equipment[num3];
			DelInvetoryItem(unit, send_state: false);
			flag = true;
		}
		if (flag)
		{
			SendState<UnitsState>();
			if (base.started)
			{
				NotifyListeners("inventory_changed");
			}
		}
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (!(message == "stat_changed") || !(param is Stat stat))
		{
			return;
		}
		string name = stat.def.name;
		if (!(name == "cs_army_speed_world_perc"))
		{
			if (name == "cs_army_supply_usage_perc")
			{
				RecalcSuppliesRate();
			}
		}
		else
		{
			SetWorldSpeed();
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("food", supplies.Get());
		if (units.Count > 0)
		{
			dump.OpenSection("units");
			for (int i = 0; i < units.Count; i++)
			{
				dump.Append(units[i]?.def?.field?.key);
			}
			dump.CloseSection("units");
		}
		if (siege_equipment.Count > 0)
		{
			dump.OpenSection("siege_equipment");
			for (int j = 0; j < siege_equipment.Count; j++)
			{
				dump.Append(siege_equipment[j]?.def?.field?.key);
			}
			dump.CloseSection("siege_equipment");
		}
		dump.Append("morale", GetMorale());
		dump.Append("supplies", supplies);
		dump.Append("morale", GetMorale());
		dump.Append("leader", leader?.ToString());
		dump.Append("castle", castle?.ToString());
		dump.Append("battle", battle?.ToString());
		dump.Append("battle_side", battle_side);
		dump.Append("speed", movement?.speed);
		dump.Append("rebellion", rebel?.rebellion?.ToString());
		dump.Append("mercenary", IsMercenary().ToString());
		dump.Append("realm", realm_in?.name);
		dump.Append("iteractor", interactor);
		dump.Append("interact_target", interact_target);
	}

	public Army(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public new static Object Create(Multiplayer multiplayer)
	{
		return new Army(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}
}
