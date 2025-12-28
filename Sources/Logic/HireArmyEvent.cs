using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Mercenary)]
public class Mercenary : Object
{
	public class Def : Logic.Def
	{
		public string name;

		public float refresh_interval_min;

		public float refresh_interval_max;

		public int max_mercs = 50;

		public int max_mercs_per_realm = 1;

		public int max_mercs_per_kingdom = 5;

		public float spawn_chance;

		public float spawn_period;

		public int parent_realm_serch_limit = 25;

		public int units_max_tier = 6;

		public int units_max_cnt = 6;

		public int coat_of_arms_index = 6;

		public float unit_cost_overcharge = 1.5f;

		public float rr_merc_rebel_mod = 0.1f;

		public float intender_min_distance = 50f;

		public string kingdom_key;

		public float buyer_max_distance = 7f;

		public float spawn_initial_wait = 1800f;

		public int relocation_realm_depth = 2;

		public float relocate_base_weight = 100f;

		public float relocate_wars_weight = 200f;

		public float relocate_merc_in_k_weight = 100f;

		public float relocate_merc_camps_mod = 0.2f;

		public float price_increase_per_level = 50f;

		public float price_hire_mod = 1f;

		public float price_hire_unit_former_kingdom_discount_mod = 0.25f;

		public float move_chance_per_existing_merc_world = -5f;

		public float chance_become_rebel_per_negative_CA = 5f;

		public float base_chance_move = 100f;

		public float min_chance_move = 50f;

		public float fatigue_min;

		public float fatigue_cap = 1f;

		public float fatigue_loss = 0.01f;

		public float fatigue_field = 0.2f;

		public float fatigue_pillage = 0.2f;

		public float fatigue_siege = 1f;

		public float fatigue_threshold = 0.6f;

		public float exp_per_level = 100f;

		public int max_level = 15;

		public float level_up_chance_castle = 300f;

		public float level_up_chance_settlement = 50f;

		public float level_up_chance_open_field = 100f;

		public float level_up_chance_leader_also_recieves_levels = 50f;

		public int min_starting_level;

		public int max_starting_level = 4;

		public float heal_timer_interval = 10f;

		public float healing_rate = 0.1f;

		public float reinforcement_starting_health = 0.5f;

		public float target_pick_defend_plunder = 2f;

		public float target_pick_defend_keep = 3f;

		public float target_pick_defend_town = 4f;

		public float target_pick_attack_plunder = 1.5f;

		public float target_pick_attack_keep = 1.25f;

		public float target_pick_attack_town = 1f;

		public float target_pick_help_own_kingdom_in_battle = 100f;

		public float target_pick_attack_army = 1f;

		public float target_pick_attack_army_manpower_ratio_max = 2f;

		public float target_pick_distance = 10f;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			refresh_interval_min = field.GetFloat("refresh_interval_min", null, refresh_interval_min);
			refresh_interval_max = field.GetFloat("refresh_interval_max", null, refresh_interval_max);
			max_mercs = field.GetInt("max_mercs", null, max_mercs);
			max_mercs_per_realm = field.GetInt("max_mercs_per_realm", null, max_mercs_per_realm);
			max_mercs_per_kingdom = field.GetInt("max_mercs_per_kingdom", null, max_mercs_per_kingdom);
			spawn_chance = field.GetFloat("spawn_chance");
			spawn_period = field.GetFloat("spawn_period");
			parent_realm_serch_limit = field.GetInt("parent_realm_serch_limit", null, parent_realm_serch_limit);
			units_max_tier = field.GetInt("units_max_tier", null, units_max_tier);
			units_max_cnt = field.GetInt("units_max_cnt", null, units_max_cnt);
			coat_of_arms_index = field.GetInt("coat_of_arms_index", null, coat_of_arms_index);
			unit_cost_overcharge = field.GetFloat("unit_cost_overcharge", null, unit_cost_overcharge);
			rr_merc_rebel_mod = field.GetFloat("rr_merc_rebel_mod", null, rr_merc_rebel_mod);
			intender_min_distance = field.GetFloat("intender_min_distance", null, intender_min_distance);
			buyer_max_distance = field.GetFloat("buyer_max_distance", null, buyer_max_distance);
			kingdom_key = field.GetString("kingdom_key");
			spawn_initial_wait = field.GetFloat("spawn_initial_wait", null, spawn_initial_wait);
			relocation_realm_depth = field.GetInt("relocation_realm_depth", null, relocation_realm_depth);
			relocate_base_weight = field.GetFloat("relocate_base_weight", null, relocate_base_weight);
			relocate_wars_weight = field.GetFloat("relocate_wars_weight", null, relocate_wars_weight);
			relocate_merc_in_k_weight = field.GetFloat("relocate_merc_in_k_weight", null, relocate_merc_in_k_weight);
			relocate_merc_camps_mod = field.GetFloat("relocate_merc_camps_mod", null, relocate_merc_camps_mod);
			price_increase_per_level = field.GetFloat("price_increase_per_level", null, price_increase_per_level);
			price_hire_mod = field.GetFloat("price_hire_mod", null, price_hire_mod);
			price_hire_unit_former_kingdom_discount_mod = field.GetFloat("price_hire_unit_former_kingdom_discount_mod", null, price_hire_unit_former_kingdom_discount_mod);
			move_chance_per_existing_merc_world = field.GetFloat("move_chance_per_existing_merc_world", null, move_chance_per_existing_merc_world);
			chance_become_rebel_per_negative_CA = field.GetFloat("chance_become_rebel_per_negative_CA", null, chance_become_rebel_per_negative_CA);
			base_chance_move = field.GetFloat("base_chance_move", null, base_chance_move);
			min_chance_move = field.GetFloat("min_chance_move");
			fatigue_min = field.GetFloat("fatigue_min", null, fatigue_min);
			fatigue_cap = field.GetFloat("fatigue_cap", null, fatigue_cap);
			fatigue_loss = field.GetFloat("fatigue_loss", null, fatigue_loss);
			fatigue_field = field.GetFloat("fatigue_field", null, fatigue_field);
			fatigue_pillage = field.GetFloat("fatigue_pillage", null, fatigue_pillage);
			fatigue_siege = field.GetFloat("fatigue_siege", null, fatigue_siege);
			fatigue_threshold = field.GetFloat("fatigue_threshold", null, fatigue_threshold);
			exp_per_level = field.GetFloat("exp_per_level ", null, exp_per_level);
			max_level = field.GetInt("max_level ", null, max_level);
			min_starting_level = field.GetInt("min_strating_level", null, min_starting_level);
			max_starting_level = field.GetInt("max_starting_level", null, max_starting_level);
			LoadLevelUpchances(field);
			heal_timer_interval = field.GetFloat("heal_timer_interval", null, heal_timer_interval);
			healing_rate = field.GetFloat("healing_rate", null, healing_rate);
			reinforcement_starting_health = field.GetFloat("reinforcement_starting_health", null, reinforcement_starting_health);
			target_pick_defend_plunder = field.GetFloat("target_pick_defend_plunder", null, target_pick_defend_plunder);
			target_pick_defend_keep = field.GetFloat("target_pick_defend_keep", null, target_pick_defend_keep);
			target_pick_defend_town = field.GetFloat("target_pick_defend_town", null, target_pick_defend_town);
			target_pick_attack_plunder = field.GetFloat("target_pick_attack_plunder", null, target_pick_attack_plunder);
			target_pick_attack_keep = field.GetFloat("target_pick_attack_keep", null, target_pick_attack_keep);
			target_pick_attack_town = field.GetFloat("target_pick_attack_town", null, target_pick_attack_town);
			target_pick_help_own_kingdom_in_battle = field.GetFloat("target_pick_help_own_kingdom_in_battle", null, target_pick_help_own_kingdom_in_battle);
			target_pick_attack_army = field.GetFloat("target_pick_attack_army", null, target_pick_attack_army);
			target_pick_attack_army_manpower_ratio_max = field.GetFloat("target_pick_attack_army_manpower_ratio_max", null, target_pick_attack_army_manpower_ratio_max);
			target_pick_distance = field.GetFloat("target_pick_distance", null, target_pick_distance);
			return true;
		}

		public void LoadLevelUpchances(DT.Field f)
		{
			DT.Field field = f.FindChild("level_up_chances");
			if (field != null)
			{
				level_up_chance_castle = field.GetFloat("castle", null, level_up_chance_castle);
				level_up_chance_settlement = field.GetFloat("settlement", null, level_up_chance_settlement);
				level_up_chance_open_field = field.GetFloat("open_field", null, level_up_chance_open_field);
				level_up_chance_leader_also_recieves_levels = field.GetFloat("leader_also_recieves_levels", null, level_up_chance_leader_also_recieves_levels);
			}
		}
	}

	public enum Action
	{
		None,
		Attack,
		Rest
	}

	[Serialization.State(11)]
	public class ArmyState : Serialization.ObjectState
	{
		public NID army_nid;

		public int army_kingdom_id;

		public static ArmyState Create()
		{
			return new ArmyState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = (obj as Mercenary).army;
			army_nid = army;
			army_kingdom_id = army.kingdom_id;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Army>(army_nid, "army_nid");
			ser.Write7BitSigned(army_kingdom_id, "army_kingdom_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			army_nid = ser.ReadNID<Army>("army_nid");
			army_kingdom_id = ser.Read7BitSigned("army_kingdom_id");
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			mercenary.army = army_nid.Get<Army>(obj.game);
			if (mercenary.army == null)
			{
				Game.Log($"Mercenary.ArmyState.ApplyTo: army {army_nid} not found!", Game.LogType.Error);
				return;
			}
			if (army_kingdom_id != 0)
			{
				if (mercenary.army.kingdom_id == 0)
				{
					mercenary.army.kingdom_id = army_kingdom_id;
				}
				else if (mercenary.army.kingdom_id != army_kingdom_id)
				{
					Game.Log($"Mercenary.ArmyState.ApplyTo: army kingdom id is {mercenary.army.kingdom_id} instead of {army_kingdom_id}!", Game.LogType.Error);
				}
			}
			bool num = mercenary.army.mercenary != null;
			mercenary.army.mercenary = mercenary;
			if (!num)
			{
				if (mercenary.army.realm_in == null)
				{
					mercenary.army.UpdateRealmIn();
				}
				if (mercenary.army.realm_in != null)
				{
					mercenary.army.realm_in.AddMercenary(mercenary.army);
					mercenary.army.realm_in.GetKingdom()?.AddMercenary(mercenary.army);
				}
				Kingdom kingdom = mercenary.army.GetKingdom();
				if (kingdom.type == Kingdom.Type.Regular && !kingdom.mercenaries.Contains(mercenary.army))
				{
					kingdom.mercenaries.Add(mercenary.army);
				}
			}
			mercenary.army.RecalcSuppliesRate();
			mercenary.army.NotifyListeners("mercenary_changed");
		}
	}

	[Serialization.State(12)]
	public class BuyersState : Serialization.ObjectState
	{
		private List<NID> buyers_nids = new List<NID>();

		public static BuyersState Create()
		{
			return new BuyersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Mercenary).buyers.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			if (buyers_nids.Count > 0)
			{
				buyers_nids.Clear();
			}
			for (int i = 0; i < mercenary.buyers.Count; i++)
			{
				NID item = mercenary.buyers[i];
				buyers_nids.Add(item);
			}
			return buyers_nids.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = buyers_nids.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteNID<Army>(buyers_nids[i], "army_nid_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (buyers_nids.Count > 0)
			{
				buyers_nids.Clear();
			}
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				buyers_nids.Add(ser.ReadNID<Army>("army_nid_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			if (mercenary.buyers.Count > 0)
			{
				mercenary.buyers.Clear();
			}
			for (int i = 0; i < buyers_nids.Count; i++)
			{
				mercenary.buyers.Add(buyers_nids[i].Get<Army>(obj.game));
			}
			mercenary.NotifyListeners("buyers_changed");
		}
	}

	[Serialization.State(13)]
	public class IntendersState : Serialization.ObjectState
	{
		private List<NID> intenders_nids = new List<NID>();

		public static IntendersState Create()
		{
			return new IntendersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Mercenary).intenders.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			if (intenders_nids.Count > 0)
			{
				intenders_nids.Clear();
			}
			for (int i = 0; i < mercenary.intenders.Count; i++)
			{
				NID item = mercenary.intenders[i];
				intenders_nids.Add(item);
			}
			return intenders_nids.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = intenders_nids.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteNID<Army>(intenders_nids[i], "army_nid_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (intenders_nids.Count > 0)
			{
				intenders_nids.Clear();
			}
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				intenders_nids.Add(ser.ReadNID<Army>("army_nid_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			if (mercenary.intenders.Count > 0)
			{
				mercenary.intenders.Clear();
			}
			for (int i = 0; i < intenders_nids.Count; i++)
			{
				mercenary.intenders.Add(intenders_nids[i].Get<Army>(obj.game));
			}
		}
	}

	[Serialization.State(14)]
	public class OriginState : Serialization.ObjectState
	{
		private int parent_kid;

		private int spawn_realm_id;

		private int former_owner_id;

		public static OriginState Create()
		{
			return new OriginState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			parent_kid = mercenary.parent_kingdom_id;
			spawn_realm_id = mercenary.spawn_realm_id;
			former_owner_id = mercenary.former_owner_id;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(parent_kid, "parent_kingdom_id");
			ser.Write7BitSigned(spawn_realm_id, "spawn_realm_id");
			ser.Write7BitUInt(former_owner_id, "former_owner_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			parent_kid = ser.Read7BitUInt("parent_kingdom_id");
			spawn_realm_id = ser.Read7BitSigned("spawn_realm_id");
			former_owner_id = ser.Read7BitUInt("former_owner_id");
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary obj2 = obj as Mercenary;
			obj2.parent_kingdom_id = parent_kid;
			obj2.spawn_realm_id = spawn_realm_id;
			obj2.former_owner_id = former_owner_id;
			obj2.army?.NotifyListeners("mercenary_changed");
		}
	}

	[Serialization.State(15)]
	public class CommandState : Serialization.ObjectState
	{
		private string mission;

		public static CommandState Create()
		{
			return new CommandState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			mission = mercenary.mission_def?.field?.key;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(mission, "mission");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (Serialization.cur_version < 5)
			{
				mission = "HelpInWars";
			}
			else
			{
				mission = ser.ReadStr("mission");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			if (string.IsNullOrEmpty(mission))
			{
				mercenary.SetMission(null, send_state: false);
			}
			else
			{
				mercenary.SetMission(mercenary.game.defs.Find<MercenaryMission.Def>(mission), send_state: false);
			}
		}
	}

	[Serialization.State(16)]
	public class ActionState : Serialization.ObjectState
	{
		private int action;

		public static ActionState Create()
		{
			return new ActionState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			action = (int)mercenary.current_action;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(action, "action");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			action = ser.Read7BitUInt("action");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Mercenary).SetAction((Action)action, send_state: false);
		}
	}

	[Serialization.State(17)]
	public class FatigueState : Serialization.ObjectState
	{
		public Data fatigue;

		public static FatigueState Create()
		{
			return new FatigueState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			fatigue = mercenary.fatigue.CreateData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(fatigue, "fatigue");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			fatigue = ser.ReadData("fatigue");
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			fatigue.ApplyTo(mercenary.fatigue, obj.game);
		}
	}

	[Serialization.State(18)]
	public class ExperienceState : Serialization.ObjectState
	{
		public float xp;

		public static ExperienceState Create()
		{
			return new ExperienceState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Mercenary).experience > 0f;
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			xp = mercenary.experience;
			return xp > 0f;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(xp, "xp");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			xp = ser.ReadFloat("xp");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Mercenary).experience = xp;
		}
	}

	[Serialization.State(19)]
	public class TargetState : Serialization.ObjectState
	{
		private NID target;

		public static TargetState Create()
		{
			return new TargetState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Mercenary).IsHired();
		}

		public override bool InitFrom(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			target = mercenary.cur_target;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(target, "target");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (Serialization.cur_version < 5)
			{
				target = null;
			}
			else
			{
				target = ser.ReadNID("target");
			}
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Mercenary).SetTarget(target.GetObj(obj.game) as MapObject, send_state: false);
		}
	}

	[Serialization.Event(27)]
	public class BuyUnitEvent : Serialization.ObjectEvent
	{
		public NID buyer;

		public int unit_index = -1;

		public BuyUnitEvent()
		{
		}

		public static BuyUnitEvent Create()
		{
			return new BuyUnitEvent();
		}

		public BuyUnitEvent(Army army, int index)
		{
			buyer = army;
			unit_index = index;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(buyer, "buyer");
			ser.Write7BitUInt(unit_index, "unit_index");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			buyer = ser.ReadNID("buyer");
			unit_index = ser.Read7BitUInt("unit_index");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Mercenary).Buy(buyer: buyer.Get<Army>(obj.game), unit_idx: unit_index);
		}
	}

	[Serialization.Event(28)]
	public class HireArmyEvent : Serialization.ObjectEvent
	{
		public NID buyer;

		public string mission;

		public HireArmyEvent()
		{
		}

		public static HireArmyEvent Create()
		{
			return new HireArmyEvent();
		}

		public HireArmyEvent(Kingdom k, MercenaryMission.Def mission)
		{
			buyer = k;
			this.mission = mission?.field?.key;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(buyer, "buyer");
			ser.WriteStr(mission, "mission");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			buyer = ser.ReadNID("buyer");
			mission = ser.ReadStr("mission");
		}

		public override void ApplyTo(Object obj)
		{
			Mercenary mercenary = obj as Mercenary;
			Kingdom k = buyer.Get<Kingdom>(obj.game);
			mercenary.HireForKingdom(k, mercenary.game.defs.Find<MercenaryMission.Def>(mission));
		}
	}

	[Serialization.Event(29)]
	public class DismissEvent : Serialization.ObjectEvent
	{
		public static DismissEvent Create()
		{
			return new DismissEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Mercenary).DismissOrRebel(manual: true);
		}
	}

	public Def def;

	public Army army;

	public int kingdom_id;

	public int parent_kingdom_id;

	public int former_owner_id;

	public int spawn_realm_id;

	public List<Army> buyers = new List<Army>();

	public Army selected_buyer;

	public List<Army> intenders = new List<Army>();

	public ComputableValue fatigue;

	public float experience;

	public MercenaryMission.Def mission_def;

	public MapObject cur_target;

	private static Queue<Realm> tmp_que_realms = new Queue<Realm>();

	private static Queue<int> tmp_distances = new Queue<int>();

	private static HashSet<Realm> tmp_processed_realms = new HashSet<Realm>();

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public Action current_action { get; private set; }

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return "Mercenary.name";
	}

	public Mercenary(Game game, int parent_kingdom_id, Realm realm, Def def)
		: base(game)
	{
		if (def != null)
		{
			Kingdom factionKingdom = FactionUtils.GetFactionKingdom(game, def.kingdom_key);
			kingdom_id = factionKingdom.id;
			this.parent_kingdom_id = parent_kingdom_id;
			if (realm != null)
			{
				spawn_realm_id = realm.id;
			}
			this.def = def;
			fatigue = new ComputableValue(0f, 0f, game, def.fatigue_min, def.fatigue_cap);
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (army == null && IsAuthority())
		{
			SpawnArmy(game.GetKingdom(kingdom_id), game.GetKingdom(parent_kingdom_id), game.GetRealm(spawn_realm_id), def);
		}
		if (army != null)
		{
			if (army.leader == null)
			{
				CreateNewLeader();
				if (!army.HasNoble())
				{
					army.AddNoble();
				}
			}
			UpdateInBatch(game.update_1sec);
			RestartDespawnTimer();
			army.NotifyListeners("became_mercenary");
		}
		else
		{
			Destroy();
		}
	}

	public void RestartDespawnTimer()
	{
		if (IsAuthority())
		{
			Timer.Start(this, "despawn", game.Random(def.refresh_interval_min, def.refresh_interval_max), restart: true);
		}
	}

	public void BecomeRegular(Kingdom kingdom, Character leader = null)
	{
		if (this.army != null && kingdom != null)
		{
			Army army = this.army;
			this.army.SetMercenary(null);
			if (leader != null)
			{
				army.SetLeader(leader);
			}
			army.SetKingdom(kingdom.id);
			Destroy();
			army.NotifyListeners("became_regular");
		}
	}

	protected override void OnDestroy()
	{
		if (IsAuthority() && army != null && army.IsValid())
		{
			army.Destroy();
		}
		army?.SetMercenary(null);
		base.OnDestroy();
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public override void OnUpdate()
	{
		if (IsAuthority())
		{
			if (army == null)
			{
				Disappear();
				return;
			}
			AddEquipment();
			GarbageCollectIntenders();
			Think();
		}
	}

	public void AddFatigue(Battle.Type type)
	{
		if (def != null)
		{
			switch (type)
			{
			case Battle.Type.Plunder:
			case Battle.Type.PlunderInterrupt:
				fatigue.Add(def.fatigue_pillage);
				break;
			case Battle.Type.Siege:
			case Battle.Type.Assault:
			case Battle.Type.BreakSiege:
				fatigue.Add(def.fatigue_siege);
				break;
			default:
				fatigue.Add(def.fatigue_field);
				break;
			}
			CheckFatigue(force_send: true);
		}
	}

	private void CheckFatigue(bool force_send = false)
	{
		if (army?.leader == null || def == null)
		{
			return;
		}
		float num = fatigue.Get();
		float rate = fatigue.GetRate();
		float num2 = 0f;
		if (current_action == Action.Rest)
		{
			if (num <= def.fatigue_threshold && IsHired())
			{
				SetAction(Action.Attack);
			}
			else
			{
				num2 = 0f - def.fatigue_loss;
			}
		}
		else if (num >= def.fatigue_cap)
		{
			SetAction(Action.Rest);
		}
		else
		{
			num2 = 0f;
		}
		if (rate != num2 || force_send)
		{
			fatigue.SetRate(num2);
			SendState<FatigueState>();
		}
	}

	public bool HealValid()
	{
		bool flag = false;
		for (int i = 0; i < army.units.Count; i++)
		{
			if (army.units[i].damage > 0f)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return true;
		}
		if (former_owner_id == 0)
		{
			int units_max_cnt = def.units_max_cnt;
			bool flag2 = army.HasNoble();
			if (army.units.Count < units_max_cnt + (flag2 ? 1 : 0))
			{
				return true;
			}
		}
		return false;
	}

	public void SetAction(Action action, bool send_state = true)
	{
		if (def == null || action == current_action || (army?.battle != null && !army.battle.IsFinishing()))
		{
			return;
		}
		if (action == Action.Rest)
		{
			Realm realm = army?.realm_in;
			if (realm != null)
			{
				for (int i = 0; i < realm.settlements.Count; i++)
				{
					KeepEffects keep_effects = realm.settlements[i].keep_effects;
					if (keep_effects != null && keep_effects.CanAttack() && keep_effects.ValidateTarget(army))
					{
						return;
					}
				}
			}
		}
		current_action = action;
		NotifyListeners("action_changed");
		army.NotifyListeners("merc_action_changed");
		if (!IsAuthority())
		{
			return;
		}
		SetTarget(null);
		if (current_action == Action.Rest && army != null)
		{
			if (army.realm_in != null && army.realm_in.settlements != null)
			{
				for (int j = 0; j < army.realm_in.settlements.Count; j++)
				{
					Settlement settlement = army.realm_in.settlements[j];
					if (settlement.IsActiveSettlement() && settlement.keep_effects != null && settlement.keep_effects.CanAttack() && IsEnemy(settlement))
					{
						army.FleeFrom(settlement.position, settlement.keep_effects.def.max_distance);
						return;
					}
				}
			}
			army.SetUpCamp();
			FireEvent("rest", null);
			if (HealValid())
			{
				Timer.Start(this, "heal_timer", def.heal_timer_interval, restart: true);
			}
		}
		else
		{
			Timer.Stop(this, "heal_timer");
		}
		if (send_state)
		{
			SendState<ActionState>();
		}
	}

	public void Dismiss()
	{
		AssertAuthority();
		if (!CheckEmpty())
		{
			army.Stop();
			army.SetKingdom(kingdom_id);
			RestartDespawnTimer();
			SetAction(Action.Rest);
			SetMission(null);
		}
	}

	public void DismissOrRebel(bool manual = false, string dismiss_reason = null)
	{
		if (!IsAuthority())
		{
			SendEvent(new DismissEvent());
			return;
		}
		if (manual)
		{
			dismiss_reason = "manual";
		}
		ApplyDismissOutcomes(manual, dismiss_reason);
	}

	public bool Think(bool at_end_of_battle = false)
	{
		if (!IsAuthority())
		{
			return false;
		}
		if (army.battle != null)
		{
			KingdomAI.ThinkAssaultSiege(army);
			return false;
		}
		CheckFatigue();
		if (at_end_of_battle)
		{
			return false;
		}
		if (IsHired() && (mission_def == null || !mission_def.Validate(this, army.GetKingdom())))
		{
			DismissOrRebel();
			return false;
		}
		if (army.IsFleeing())
		{
			return false;
		}
		if (FactionArmyTargetPicker.CheckIfReinforcement(army))
		{
			return false;
		}
		if (!IsHired() || current_action == Action.Rest)
		{
			CheckInWater();
			return false;
		}
		if (!mission_def.ValidateKingdomResources(this, army.GetKingdom()))
		{
			DismissOrRebel(manual: false, "no_money");
			return false;
		}
		if (current_action == Action.Rest)
		{
			return false;
		}
		if (FactionArmyTargetPicker.ValidateTarget(army, cur_target) && army.movement.IsMoving())
		{
			return true;
		}
		FactionArmyTargetPicker.Target[] targets = GetTargets().ToArray();
		FactionArmyTargetPicker.CalcThreatLevel(army, targets);
		if (FactionArmyTargetPicker.ChooseTarget(army, targets, out var t))
		{
			army.MoveTo(t.mapObject);
			SetTarget(t.mapObject);
			return true;
		}
		SetTarget(null);
		return false;
	}

	private List<FactionArmyTargetPicker.Target> GetTargets()
	{
		Kingdom kingdom = army.GetKingdom();
		List<FactionArmyTargetPicker.Target> list = new List<FactionArmyTargetPicker.Target>(100);
		if (mission_def.can_attack_war_kingdoms)
		{
			for (int i = 0; i < kingdom.wars.Count; i++)
			{
				War war = kingdom.wars[i];
				AddTarget(war, list);
			}
		}
		AddTarget(kingdom, list);
		return list;
	}

	private void AddTarget(War war, List<FactionArmyTargetPicker.Target> targets)
	{
		int side = war.GetSide(army.GetKingdom());
		if (side != -1)
		{
			List<Kingdom> kingdoms = war.GetKingdoms(1 - side);
			for (int i = 0; i < kingdoms.Count; i++)
			{
				Kingdom k = kingdoms[i];
				AddTarget(k, targets);
			}
		}
	}

	private void AddTarget(Kingdom k, List<FactionArmyTargetPicker.Target> targets)
	{
		for (int i = 0; i < k.realms.Count; i++)
		{
			Realm r = k.realms[i];
			AddTarget(r, targets);
		}
		for (int j = 0; j < k.armies.Count; j++)
		{
			Army army = k.armies[j];
			AddTarget(army, targets);
		}
	}

	private void AddTarget(Realm r, List<FactionArmyTargetPicker.Target> targets)
	{
		for (int i = 0; i < r.armies.Count; i++)
		{
			Army army = r.armies[i];
			AddTarget(army, targets);
		}
		for (int j = 0; j < r.settlements.Count; j++)
		{
			Settlement settlement = r.settlements[j];
			AddTarget(settlement, targets);
		}
	}

	private void AddTarget(Army army, List<FactionArmyTargetPicker.Target> targets)
	{
		if (army.battle != null && CalcTarget(army.battle, targets, out var target))
		{
			targets.Add(target);
		}
		if (CalcTarget(army, targets, out var target2))
		{
			targets.Add(target2);
		}
	}

	private void AddTarget(Settlement settlement, List<FactionArmyTargetPicker.Target> targets)
	{
		if (settlement.battle != null && CalcTarget(settlement.battle, targets, out var target))
		{
			targets.Add(target);
		}
		if (CalcTarget(settlement, targets, out var target2))
		{
			targets.Add(target2);
		}
	}

	private void SetTarget(MapObject t, bool send_state = true)
	{
		if (t != cur_target)
		{
			cur_target = t;
			if (IsAuthority() && send_state)
			{
				SendState<TargetState>();
			}
			NotifyListeners("target_changed");
			army?.NotifyListeners("mercenary_moved");
		}
	}

	private bool RebellionValid(Rebellion r)
	{
		if (r == null)
		{
			return false;
		}
		Kingdom kingdom = army.GetKingdom();
		if (kingdom.rebellions.Contains(r))
		{
			return kingdom.IsEnemy(r);
		}
		return false;
	}

	private bool CalcTarget(MapObject obj, List<FactionArmyTargetPicker.Target> targets, out FactionArmyTargetPicker.Target target)
	{
		target = default(FactionArmyTargetPicker.Target);
		if (!FactionArmyTargetPicker.ValidateTarget(this.army, obj))
		{
			return false;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			if (targets[i].mapObject == obj)
			{
				return false;
			}
		}
		Kingdom kingdom = this.army.GetKingdom();
		if (obj is Army army)
		{
			if (kingdom_id == obj.kingdom_id)
			{
				return false;
			}
			if (!army.IsValid())
			{
				return false;
			}
			if (army == this.army)
			{
				return false;
			}
			if (!army.IsEnemy(this.army))
			{
				return false;
			}
			Kingdom kingdom2 = obj.GetKingdom();
			if (army == game.religions.catholic?.crusade?.army)
			{
				return false;
			}
			if (mission_def.can_attack_war_kingdoms)
			{
				if (kingdom2.FindWarWith(kingdom) == null)
				{
					return false;
				}
			}
			else if (mission_def.can_attack_rebels && !RebellionValid(army.rebel?.rebellion))
			{
				return false;
			}
			target.mapObject = obj;
		}
		if (obj is Settlement settlement)
		{
			if (kingdom_id == obj.kingdom_id)
			{
				return false;
			}
			Object controller = settlement.GetController();
			if (mission_def.can_attack_war_kingdoms)
			{
				if (!(controller is Kingdom kingdom3))
				{
					return false;
				}
				if (kingdom3.FindWarWith(kingdom) == null)
				{
					return false;
				}
			}
			if (mission_def.can_attack_rebels)
			{
				Rebellion r = controller as Rebellion;
				if (!RebellionValid(r))
				{
					return false;
				}
			}
			target.mapObject = obj;
		}
		if (obj is Battle battle)
		{
			bool flag = false;
			if (mission_def.can_attack_war_kingdoms)
			{
				for (int j = 0; j < 2; j++)
				{
					if (battle.GetSideKingdom(j).FindWarWith(kingdom) != null)
					{
						flag = true;
						break;
					}
					List<Army> armies = battle.GetArmies(j);
					for (int k = 0; k < armies.Count; k++)
					{
						if (armies[k].GetKingdom().FindWarWith(kingdom) != null)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			if (mission_def.can_attack_rebels)
			{
				for (int l = 0; l < 2; l++)
				{
					List<Army> armies2 = battle.GetArmies(l);
					for (int m = 0; m < armies2.Count; m++)
					{
						Rebellion rebellion = armies2[m].rebel?.rebellion;
						if (rebellion != null && RebellionValid(rebellion))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			if (!flag)
			{
				return false;
			}
			target.mapObject = obj;
		}
		if (FactionArmyTargetPicker.Target.CalcTargetAttractiveness(this, target.mapObject, out var result))
		{
			target.attractiveness = result;
			return true;
		}
		return false;
	}

	public override void OnTimer(Timer timer)
	{
		string name = timer.name;
		if (!(name == "despawn"))
		{
			if (name == "heal_timer")
			{
				if (IsBusy() && HealValid())
				{
					Timer.Start(this, "heal_timer", def.heal_timer_interval, restart: true);
					return;
				}
				bool flag = false;
				for (int i = 0; i < army.units.Count; i++)
				{
					Unit unit = army.units[i];
					if (unit.damage > 0f)
					{
						unit.SetDamage(unit.damage - def.healing_rate);
						flag = true;
						break;
					}
				}
				if (former_owner_id == 0 && !flag && AddRandomUnit(army.realm_in.GetKingdom() ?? army.GetKingdom()))
				{
					flag = true;
				}
				if (flag)
				{
					Timer.Start(this, "heal_timer", def.heal_timer_interval, restart: true);
				}
			}
			else
			{
				base.OnTimer(timer);
			}
		}
		else if (IsBusy() || army.IsHiredMercenary())
		{
			RestartDespawnTimer();
		}
		else if (TryMove(different_kingdom: true, random: true))
		{
			RestartDespawnTimer();
		}
		else
		{
			Disappear();
		}
	}

	public bool Interact(Army intender)
	{
		if (this.army == null || intender == null || intender == this.army)
		{
			return false;
		}
		if (!this.army.IsValid())
		{
			return false;
		}
		Army army = FindEnemyBuyer(intender);
		if (army != null)
		{
			Battle.StartBattle(intender, army);
			return false;
		}
		if (!buyers.Contains(intender))
		{
			RemoveIntender(intender);
			buyers.Add(intender);
			SendState<IntendersState>();
			SendState<BuyersState>();
		}
		FireEvent("buyers_changed", intender);
		return true;
	}

	public Resource GetUnitCost(Unit unit, Army army = null, Kingdom k = null, bool ignore_former_kingdom_cost = false)
	{
		if (unit == null)
		{
			return null;
		}
		Resource cost_merc = unit.def.cost_merc;
		if (cost_merc == null)
		{
			return null;
		}
		if (k == null)
		{
			k = army?.GetKingdom();
		}
		Resource resource = new Resource(cost_merc);
		float num = 0f;
		resource.Mul(1f + (float)unit.level * def.price_increase_per_level / 100f);
		if (!ignore_former_kingdom_cost && k != null && former_owner_id == k.id)
		{
			resource.Mul(def.price_hire_unit_former_kingdom_discount_mod);
		}
		if (army?.leader != null)
		{
			num = 0f - army.leader.GetStat(Stats.cs_mercenary_price_reduction_perc);
		}
		else if (k != null)
		{
			num = 0f - k.GetStat(Stats.ks_mercenary_price_reduction_perc);
		}
		DevSettings.Def devSettingsDef = game.GetDevSettingsDef();
		resource[ResourceType.Gold] *= devSettingsDef.unit_mercenary_gold_hire_mod;
		if (num != 1f)
		{
			Discount(resource, ResourceType.Gold, num);
		}
		Unit.Def.RoundCost(resource);
		return resource;
	}

	public void Discount(Resource cost, ResourceType rt, float percents)
	{
		if (percents != 0f)
		{
			float num = cost[rt];
			if (!(num <= 0f))
			{
				float num2 = num * (100f - percents) / 100f;
				num2 = (float)Math.Ceiling(num2);
				cost[rt] = num2;
			}
		}
	}

	public bool Buy(Unit unit, Army buyer)
	{
		if (!ValidForHireAsUnit())
		{
			return false;
		}
		if (unit == null || buyer == null || buyer.battle != null)
		{
			return false;
		}
		for (int i = 0; i < army.units.Count; i++)
		{
			if (army.units[i] == unit)
			{
				return Buy(i, buyer);
			}
		}
		return false;
	}

	public bool Buy(int unit_idx, Army buyer, bool send_state = true)
	{
		if (!ValidForHireAsUnit())
		{
			return false;
		}
		if (unit_idx >= army.units.Count || buyer == null || unit_idx < 0)
		{
			return false;
		}
		if (buyer.CountUnits() >= buyer.MaxUnits())
		{
			return false;
		}
		Unit unit = army.units[unit_idx];
		bool flag = buyer.GetKingdom().id != former_owner_id;
		Resource unitCost = GetUnitCost(unit, buyer);
		if (!buyer.GetKingdom().resources.CanAfford(unitCost, 1f))
		{
			return false;
		}
		if (!IsAuthority() && send_state)
		{
			SendEvent(new BuyUnitEvent(buyer, unit_idx));
			return true;
		}
		buyer.GetKingdom().SubResources(KingdomAI.Expense.Category.Military, unitCost);
		unit.mercenary = flag || unit.mercenary;
		army.TransferUnit(buyer, unit, null);
		unit.def.OnHiredAnalytics(buyer, null, unitCost, "mercenary");
		if (!CheckEmpty())
		{
			StopUpdating();
			UpdateInBatch(game.update_1sec);
		}
		buyer.FireEvent("mercenary_unit_hired", null);
		return true;
	}

	public bool Reinforce(int unit_idx, Army buyer)
	{
		if (!ValidForHireAsArmy())
		{
			return false;
		}
		if (!IsAuthority())
		{
			return false;
		}
		if (unit_idx >= army.units.Count || buyer == null)
		{
			return false;
		}
		if (buyers.Count > 0)
		{
			return false;
		}
		if (intenders.Count > 0)
		{
			for (int i = 0; i < intenders.Count; i++)
			{
				if (army.position.Dist(intenders[i].position) < def.intender_min_distance)
				{
					return false;
				}
			}
		}
		if (buyer.units.Count >= buyer.MaxUnits())
		{
			int index = game.Random(0, buyer.units.Count);
			if (buyer.units[index].def.upgrade_to.Count == 0)
			{
				buyer.units[index].SetDamage(0f);
			}
			else
			{
				buyer.DelUnit(buyer.units[index]);
				buyer.AddUnit(army.units[unit_idx].def, -1, mercenary: true);
			}
		}
		else if (buyer.AddUnit(army.units[unit_idx].def, -1, mercenary: true) == null)
		{
			return false;
		}
		buyer.rebel.NotifyListeners("reinforce", (Point)army.position);
		army.DelUnit(army.units[unit_idx]);
		buyer.NotifyListeners("units_changed");
		CheckEmpty();
		return true;
	}

	private bool CheckEmpty()
	{
		bool flag = false;
		for (int i = 0; i < army.units.Count; i++)
		{
			if (army.units[i].def.type != Unit.Type.Noble)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Disappear();
			return true;
		}
		return false;
	}

	public void AddIntender(Army intender)
	{
		if (army != null && intender != null && intender != army && !intenders.Contains(intender))
		{
			intenders.Add(intender);
			SendState<IntendersState>();
		}
	}

	public bool RemoveIntender(Army intender)
	{
		if (army == null || intender == null || intender == army)
		{
			return false;
		}
		return intenders.Remove(intender);
	}

	public Army FindEnemyBuyer(Army intender)
	{
		for (int i = 0; i < buyers.Count; i++)
		{
			if (buyers[i].IsValid() && buyers[i].IsEnemy(intender))
			{
				return buyers[i];
			}
		}
		return null;
	}

	public bool IsAllowedToMove()
	{
		if (intenders.Count == 0)
		{
			return !IsTrading();
		}
		return false;
	}

	public bool IsTrading()
	{
		return buyers.Count > 0;
	}

	public bool IsAbandoned()
	{
		return former_owner_id != 0;
	}

	private void GarbageCollectIntenders()
	{
		for (int i = 0; i < intenders.Count; i++)
		{
			bool flag = true;
			if (intenders[i] != null && intenders[i].IsValid())
			{
				Path path = intenders[i].movement.path;
				if (path != null && path.dst_obj != null && path.dst_obj is Army army && army == this.army)
				{
					flag = false;
				}
			}
			if (flag)
			{
				intenders.RemoveAt(i);
				i--;
				SendState<IntendersState>();
			}
		}
		for (int j = 0; j < buyers.Count; j++)
		{
			bool flag2 = true;
			if (buyers[j] != null && buyers[j].IsValid())
			{
				if (buyers[j].position.Dist(this.army.position) < def.buyer_max_distance)
				{
					flag2 = false;
				}
				Path path2 = buyers[j].movement?.path;
				if (path2 != null && path2.dst_obj != null && path2.dst_obj as Army == this.army)
				{
					flag2 = false;
				}
			}
			if (flag2)
			{
				buyers.RemoveAt(j);
				j--;
				SendState<BuyersState>();
				NotifyListeners("buyers_changed");
			}
		}
	}

	public bool TryMove(bool different_kingdom, bool random = false, bool extended_search = false, bool log_on_fail = false)
	{
		if (parent_kingdom_id == kingdom_id)
		{
			if (log_on_fail)
			{
				Game.Log("Parent kigdom is the same as kingdom", Game.LogType.Warning);
			}
			return false;
		}
		if (random)
		{
			float num = def.base_chance_move;
			MercenarySpawner component = GetKingdom().GetComponent<MercenarySpawner>();
			if (component != null)
			{
				num += (float)component.GetTotalMercenariesCount(def) * def.move_chance_per_existing_merc_world;
			}
			if (num < def.min_chance_move)
			{
				num = def.min_chance_move;
			}
			if ((float)game.Random(0, 100) >= num)
			{
				return false;
			}
		}
		int relocation_realm_depth = def.relocation_realm_depth;
		if (relocation_realm_depth < 1)
		{
			if (log_on_fail)
			{
				Game.Log($"Moving not allowed by relocation realm depth: {def.relocation_realm_depth} ", Game.LogType.Warning);
			}
			return false;
		}
		Realm realm_in = army.realm_in;
		if (realm_in == null || realm_in.id == 0)
		{
			return false;
		}
		Kingdom kingdom = realm_in.GetKingdom();
		int item = 0;
		tmp_que_realms.Clear();
		tmp_distances.Clear();
		tmp_processed_realms.Clear();
		tmp_que_realms.Enqueue(realm_in);
		tmp_distances.Enqueue(item);
		tmp_processed_realms.Add(realm_in);
		WeightedRandom<Realm> temp = WeightedRandom<Realm>.GetTemp(tmp_que_realms.Count);
		while (tmp_que_realms.Count > 0)
		{
			realm_in = tmp_que_realms.Dequeue();
			item = tmp_distances.Dequeue() + 1;
			if (item > relocation_realm_depth)
			{
				continue;
			}
			foreach (Realm neighbor in realm_in.neighbors)
			{
				if (neighbor != null && !tmp_processed_realms.Contains(neighbor))
				{
					Kingdom kingdom2 = neighbor.GetKingdom();
					if ((kingdom2 != kingdom || !different_kingdom) && neighbor.mercenaries.Count < def.max_mercs_per_realm && (kingdom2 == null || kingdom2.mercenaries_in.Count < def.max_mercs_per_kingdom) && item != relocation_realm_depth && neighbor.settlements.Count > 0)
					{
						temp.AddOption(neighbor, GetWeight(neighbor));
					}
					tmp_que_realms.Enqueue(neighbor);
					tmp_distances.Enqueue(item);
					tmp_processed_realms.Add(neighbor);
				}
			}
		}
		if (temp.options.Count == 0 && extended_search)
		{
			for (int i = 0; i < kingdom.externalBorderRealms.Count; i++)
			{
				Realm realm = kingdom.externalBorderRealms[i];
				temp.AddOption(realm, GetWeight(realm));
			}
		}
		Realm realm2 = temp.Choose();
		temp.Clear();
		if (realm2 != null)
		{
			army.MoveTo(Army.GetRandomRealmPoint(realm2));
			return true;
		}
		if (log_on_fail)
		{
			Game.Log($"Fail to find a move-to target realm for {this}", Game.LogType.Warning);
		}
		return false;
		float GetWeight(Realm realm3)
		{
			float num2 = def.relocate_base_weight;
			if (realm3 == null)
			{
				return num2;
			}
			Kingdom kingdom3 = realm3.GetKingdom();
			if (kingdom3 != null)
			{
				if (kingdom3.wars.Count > 0)
				{
					num2 += def.relocate_wars_weight;
				}
				if (kingdom3.mercenaries.Count > 0)
				{
					num2 += def.relocate_merc_in_k_weight;
				}
				int num3 = 0;
				for (int j = 0; j < kingdom3.mercenaries_in.Count; j++)
				{
					if (!kingdom3.mercenaries_in[j].IsHiredMercenary())
					{
						num3++;
					}
				}
				num2 *= Math.Max(0f, 1f - def.relocate_merc_camps_mod * (float)num3);
			}
			return num2;
		}
	}

	public bool IsBusy()
	{
		if (!IsValid())
		{
			return true;
		}
		if (army == null || army.realm_in == null || army.realm_in.neighbors == null)
		{
			return true;
		}
		if (selected_buyer != null)
		{
			return true;
		}
		if (army.battle != null)
		{
			return true;
		}
		if (army.movement.IsMoving())
		{
			return true;
		}
		return false;
	}

	private bool NearArmy(Realm r)
	{
		if (r == null || r.armies == null)
		{
			return false;
		}
		for (int i = 0; i < r.armies.Count; i++)
		{
			Army army = r.armies[i];
			if (army != null && army.rebel == null && army.mercenary == null && army.position.Dist(this.army.position) <= def.intender_min_distance)
			{
				return true;
			}
		}
		return false;
	}

	private void Disappear()
	{
		if (army != null && army.IsValid())
		{
			army.Destroy();
		}
		if (IsValid())
		{
			Destroy();
		}
	}

	public void CalcBattleOver(Battle b, bool won)
	{
		if (def != null && b != null)
		{
			if (won)
			{
				TryLevelUp(b);
			}
			else if (IsValid() && (army.IsDefeated() || army.leader == null || army.leader.IsDead()))
			{
				return;
			}
			AddExperience(GetBattleExp(b, won));
			army.SetBattle(null);
			AddFatigue(b.type);
			Think(at_end_of_battle: true);
		}
	}

	private uint GetBattleExp(Battle b, bool won)
	{
		if (b == null)
		{
			return 0u;
		}
		int num;
		switch (b.type)
		{
		case Battle.Type.Plunder:
		case Battle.Type.PlunderInterrupt:
			num = 100;
			break;
		case Battle.Type.OpenField:
			num = 200;
			break;
		case Battle.Type.Assault:
			num = 200;
			break;
		case Battle.Type.BreakSiege:
			num = 200;
			break;
		case Battle.Type.Siege:
			num = 200;
			break;
		default:
			num = 1;
			break;
		}
		return (uint)(won ? ((float)num) : ((float)num * 0.2f));
	}

	public void AddExperience(float val)
	{
		experience += val;
		SendState<ExperienceState>();
	}

	public void TryLevelUp(Battle b)
	{
		float num = 0f;
		num = ((b.settlement == null) ? def.level_up_chance_open_field : ((!(b.settlement.type == "Castle")) ? def.level_up_chance_settlement : def.level_up_chance_castle));
		int num2 = (int)Math.Floor(num / 100f);
		num %= 100f;
		if ((float)game.Random(0, 100) < num)
		{
			num2++;
		}
		LevelUp(num2);
	}

	public bool LevelUp(int numLevels = 1)
	{
		if (numLevels <= 0)
		{
			return false;
		}
		bool result = false;
		while (numLevels > 0 && IncreaseOrGiveRandomSkill())
		{
			result = true;
			numLevels--;
		}
		while (numLevels > 0)
		{
			result = true;
			numLevels--;
		}
		return result;
	}

	private bool IncreaseOrGiveRandomSkill()
	{
		if (game.Random(0, 99) < 50)
		{
			if (army.leader.ThinkUpgradeSkill(for_free: true))
			{
				return true;
			}
			if (army.leader.ThinkNewSkill(all: true, for_free: true))
			{
				return true;
			}
		}
		else
		{
			if (army.leader.ThinkNewSkill(all: true, for_free: true))
			{
				return true;
			}
			if (army.leader.ThinkUpgradeSkill(for_free: true))
			{
				return true;
			}
		}
		return false;
	}

	public void CreateNewLeader()
	{
		if (IsAuthority())
		{
			army.SetLeader(CharacterFactory.CreateCharacter(game, parent_kingdom_id, "Marshal", "MercenaryLeader"));
		}
	}

	public bool SpawnArmy(Kingdom kingdom, Kingdom parent_kingdom, Realm realm, Def def)
	{
		if (def == null)
		{
			return false;
		}
		if (realm == null || parent_kingdom == null)
		{
			return false;
		}
		if (realm.settlements.Count == 0)
		{
			return false;
		}
		Point randomRealmPoint = Army.GetRandomRealmPoint(realm);
		army = new Army(realm.game, randomRealmPoint, kingdom.id);
		army.SetMercenary(this);
		army.realm_in.AddMercenary(army);
		army.realm_in.GetKingdom()?.AddMercenary(army);
		CreateNewLeader();
		army.RecalcSuppliesRate();
		army.Start();
		army.AddNoble();
		int numLevels = game.Random(def.min_starting_level, def.max_starting_level + 1);
		LevelUp(numLevels);
		FillWithUnits(parent_kingdom);
		army.leader.SetKingdom(FactionUtils.GetFactionKingdom(game, "MercenaryFaction").id);
		AddEquipment();
		SendState<ArmyState>();
		return true;
	}

	public void AddEquipment()
	{
		if (army.siege_equipment.Count != 0)
		{
			return;
		}
		List<string> recruitableSiegeEquipmentIDs = GetRecruitableSiegeEquipmentIDs(game);
		if (recruitableSiegeEquipmentIDs != null && recruitableSiegeEquipmentIDs.Count != 0)
		{
			int index = army.game.Random(0, recruitableSiegeEquipmentIDs.Count);
			Unit.Def def = army.game.defs.Get<Unit.Def>(recruitableSiegeEquipmentIDs[index]);
			if (!def.valid)
			{
				army.Error("Unknown unit def: " + recruitableSiegeEquipmentIDs[index]);
				return;
			}
			InventoryItem inventoryItem = new InventoryItem();
			inventoryItem.def = def;
			inventoryItem.army = army;
			army.siege_equipment.Add(inventoryItem);
		}
	}

	public void FillWithUnits(Kingdom kingdom)
	{
		if (army == null || kingdom == null)
		{
			return;
		}
		List<string> recruitableUnitIDs = GetRecruitableUnitIDs(game, kingdom, this);
		if (recruitableUnitIDs == null)
		{
			return;
		}
		int units_max_cnt = def.units_max_cnt;
		Army obj = army;
		bool flag = obj != null && obj.units[0]?.def?.type == Unit.Type.Noble;
		if (recruitableUnitIDs.Count > 0)
		{
			while (army.units.Count < units_max_cnt + (flag ? 1 : 0))
			{
				int index = army.game.Random(0, recruitableUnitIDs.Count);
				string def_id = recruitableUnitIDs[index];
				army.AddUnit(def_id, -1, mercenary: true, send_state: false);
			}
		}
		army.SendState<Army.UnitsState>();
		if (army.started)
		{
			army.NotifyListeners("units_changed");
		}
	}

	public bool AddRandomUnit(Kingdom kingdom)
	{
		if (army == null || kingdom == null)
		{
			return false;
		}
		List<string> recruitableUnitIDs = GetRecruitableUnitIDs(game, kingdom, this);
		if (recruitableUnitIDs == null)
		{
			return false;
		}
		int units_max_cnt = def.units_max_cnt;
		bool flag = army.HasNoble();
		Unit unit = null;
		if (recruitableUnitIDs.Count > 0 && army.units.Count < units_max_cnt + (flag ? 1 : 0))
		{
			int index = army.game.Random(0, recruitableUnitIDs.Count);
			string def_id = recruitableUnitIDs[index];
			unit = army.AddUnit(def_id, -1, mercenary: true, send_state: false);
		}
		if (unit != null)
		{
			unit.SetDamage(1f - def.reinforcement_starting_health, send_state: false);
			army.SendState<Army.UnitsState>();
			if (army.started)
			{
				army.NotifyListeners("units_changed");
			}
		}
		return unit != null;
	}

	public static List<string> GetRecruitableUnitIDs(Game game, Kingdom kingdom, Mercenary merc)
	{
		if (kingdom == null || game == null || merc == null)
		{
			return null;
		}
		DT.Def def = game.dt.FindDef("Unit");
		if (def == null || def.defs == null || def.defs.Count < 1)
		{
			return null;
		}
		List<string> list = new List<string>();
		foreach (Realm realm in kingdom.realms)
		{
			if (realm.castle == null)
			{
				continue;
			}
			if (realm.castle.available_units == null)
			{
				merc.Log($"realm {realm} is missing a available_units?");
				continue;
			}
			List<Unit.Def> availableUnitTypes = realm.castle.available_units.GetAvailableUnitTypes();
			int units_max_tier = merc.def.units_max_tier;
			for (int i = 0; i < availableUnitTypes.Count; i++)
			{
				if (!availableUnitTypes[i].IsBase() && availableUnitTypes[i].tier <= units_max_tier && availableUnitTypes[i].tier > 0)
				{
					list.Add(availableUnitTypes[i].field.Path());
				}
			}
		}
		if (list.Count == 0)
		{
			for (int j = 0; j < def.defs.Count; j++)
			{
				DT.Field field = def.defs[j].field;
				if (!string.IsNullOrEmpty(field.base_path) && !(field.base_path == "Noble") && !(field.base_path == "SiegeEquipment") && !(field.base_path == "InventoryItem") && field.GetBool("available"))
				{
					list.Add(field.Path());
				}
			}
		}
		return list;
	}

	public List<string> GetRecruitableSiegeEquipmentIDs(Game game)
	{
		if (game == null)
		{
			return null;
		}
		DT.Def def = game.dt.FindDef("Unit");
		if (def == null || def.defs == null || def.defs.Count < 1)
		{
			return null;
		}
		List<string> list = new List<string>();
		if (list.Count == 0)
		{
			for (int i = 0; i < def.defs.Count; i++)
			{
				DT.Field field = def.defs[i].field;
				if (!string.IsNullOrEmpty(field.base_path) && field.GetBool("available") && field.base_path == "SiegeEquipment")
				{
					list.Add(field.Path());
				}
			}
		}
		return list;
	}

	public Resource TotalCost(Kingdom k = null)
	{
		Resource resource = new Resource();
		Vars vars = new Vars();
		if (k != null)
		{
			vars.Set("hire_kingdom", k);
		}
		vars.Set("ignore_former_kingdom_discount", val: true);
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit != null)
			{
				vars.Set("obj", unit);
				resource.Add(unit.GetVar("cost", vars).Get<Resource>(), 1f);
			}
		}
		resource.Mul(def.price_hire_mod);
		resource.Round();
		return resource;
	}

	public bool IsHired()
	{
		if (!ValidForHireAsArmy())
		{
			return !army.IsHeadless();
		}
		return false;
	}

	public bool ValidForHireAsArmy()
	{
		if (army == null)
		{
			return false;
		}
		if (army.GetKingdom() == null)
		{
			return false;
		}
		if (army.GetKingdom().type == Kingdom.Type.Faction)
		{
			return army?.leader != null;
		}
		return false;
	}

	public bool ValidForHireAsUnit()
	{
		if (army == null)
		{
			return false;
		}
		if (army.GetKingdom() == null)
		{
			return false;
		}
		return army.GetKingdom().type == Kingdom.Type.Faction;
	}

	public void SetMission(MercenaryMission.Def mission, bool send_state = true)
	{
		mission_def = mission;
		if (IsAuthority() && send_state)
		{
			SendState<CommandState>();
		}
		if (mission_def == null)
		{
			army?.NotifyListeners("became_mercenary");
		}
		else
		{
			army?.NotifyListeners("became_regular");
		}
	}

	public bool HireForKingdom(Kingdom k, MercenaryMission.Def mission, bool payCost = true)
	{
		if (!ValidForHireAsArmy())
		{
			return false;
		}
		Resource cost = mission.GetCost(this, k);
		if (payCost && !k.resources.CanAfford(cost, 1f))
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new HireArmyEvent(k, mission));
			return true;
		}
		former_owner_id = 0;
		if (payCost)
		{
			k.SubResources(KingdomAI.Expense.Category.Military, cost);
		}
		army.SetKingdom(k.id);
		Timer.Stop(this, "despawn");
		SetMission(mission);
		SendState<CommandState>();
		SetAction(Action.Attack);
		army.FireEvent("mercenary_army_hired", k);
		return true;
	}

	public override Value GetDumpStateValue()
	{
		return Value.Null;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("army", army?.ToString());
		if (buyers != null && buyers.Count > 0)
		{
			dump.OpenSection("buyers");
			for (int i = 0; i < buyers.Count; i++)
			{
				dump.Append(buyers[i]?.ToString());
			}
			dump.CloseSection("buyers");
		}
		if (intenders != null && intenders.Count > 0)
		{
			dump.OpenSection("intenders");
			for (int j = 0; j < intenders.Count; j++)
			{
				dump.Append(intenders[j]?.ToString());
			}
			dump.CloseSection("intenders");
		}
		dump.Append("former_owner", game.GetKingdom(former_owner_id)?.Name);
		dump.Append("parent_kingdom", game.GetKingdom(parent_kingdom_id)?.Name);
		dump.Append("spawn_realm", game.GetRealm(spawn_realm_id)?.name);
	}

	public MapObject GetTarget()
	{
		return cur_target;
	}

	public Battle GetBattleTarget()
	{
		if (cur_target is Battle result)
		{
			return result;
		}
		Army army = cur_target as Army;
		if (army?.battle != null)
		{
			return army?.battle;
		}
		Settlement settlement = cur_target as Settlement;
		if (settlement?.battle != null)
		{
			return settlement?.battle;
		}
		return null;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "upkeep":
			return army.GetUpkeep();
		case "upkeep_merc":
		{
			Kingdom kingdom = vars?.GetVar("kingdom").Get<Kingdom>();
			if (!kingdom.IsRegular())
			{
				kingdom = (vars?.GetVar("obj").obj_val as Mercenary)?.selected_buyer?.GetKingdom();
			}
			return army.GetUpkeepMerc(kingdom);
		}
		case "total_cost":
		{
			Kingdom kingdom2 = vars?.GetVar("hire_kingdom").Get<Kingdom>();
			if (kingdom2 == null)
			{
				kingdom2 = vars?.GetVar("kingdom").Get<Kingdom>();
			}
			return TotalCost(kingdom2);
		}
		case "buyer":
			return selected_buyer;
		case "has_no_buyer":
			return buyers.Count == 0;
		case "mercenary_leader":
			return army.leader;
		case "is_headless":
			return army.IsHeadless();
		case "is_hired":
			return IsHired();
		case "target":
			return GetTarget();
		case "target_castle":
			return cur_target as Castle;
		case "target_army":
			return cur_target as Army;
		case "target_keep":
		{
			Settlement settlement2 = cur_target as Settlement;
			if (settlement2?.type == "Keep")
			{
				return settlement2;
			}
			return Value.Unknown;
		}
		case "target_settlement":
		{
			Settlement settlement = cur_target as Settlement;
			if (settlement?.type != "Keep")
			{
				return settlement;
			}
			return Value.Unknown;
		}
		case "battle_target":
			return GetBattleTarget();
		case "former_owner":
			if (former_owner_id != 0)
			{
				return game.GetKingdom(former_owner_id);
			}
			return Value.Null;
		case "current_owner":
			return army.GetKingdom();
		case "army":
			return army;
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public static void GetHeadlessMercenaries(Kingdom kingdom, List<Mercenary> list)
	{
		list.Clear();
		if (kingdom == null)
		{
			return;
		}
		Def def = kingdom.game.defs.GetBase<Def>();
		if (def == null)
		{
			return;
		}
		Kingdom factionKingdom = FactionUtils.GetFactionKingdom(kingdom.game, def.kingdom_key);
		if (factionKingdom == null)
		{
			return;
		}
		for (int i = 0; i < factionKingdom.armies.Count; i++)
		{
			Mercenary mercenary = factionKingdom.armies[i].mercenary;
			if (mercenary != null && mercenary.IsValid())
			{
				Army obj = mercenary.army;
				if ((obj == null || obj.IsValid()) && mercenary.former_owner_id == kingdom.id && !mercenary.IsHired())
				{
					list.Add(mercenary);
				}
			}
		}
	}

	public static void GetLoyalMercenaries(Kingdom kingdom, List<Mercenary> list)
	{
		list.Clear();
		if (kingdom == null)
		{
			return;
		}
		Def def = kingdom.game.defs.GetBase<Def>();
		if (def == null)
		{
			return;
		}
		Kingdom factionKingdom = FactionUtils.GetFactionKingdom(kingdom.game, def.kingdom_key);
		if (factionKingdom == null)
		{
			return;
		}
		for (int i = 0; i < factionKingdom.armies.Count; i++)
		{
			Mercenary mercenary = factionKingdom.armies[i].mercenary;
			if (mercenary != null && mercenary.IsValid())
			{
				Army obj = mercenary.army;
				if ((obj == null || obj.IsValid()) && mercenary.former_owner_id == kingdom.id && !mercenary.IsHired())
				{
					list.Add(mercenary);
				}
			}
		}
		for (int j = 0; j < kingdom.mercenaries.Count; j++)
		{
			Army army = kingdom.mercenaries[j];
			if (army.IsValid() && army.IsOwnStance(kingdom) && army.IsHiredMercenary())
			{
				list.Add(army.mercenary);
			}
		}
		for (int k = 0; k < kingdom.mercenaries_in.Count; k++)
		{
			Army army2 = kingdom.mercenaries_in[k];
			if (army2.IsValid() && army2.IsOwnStance(kingdom) && !army2.IsHiredMercenary())
			{
				list.Add(army2.mercenary);
			}
		}
	}

	public List<OutcomeDef> DecideOutcomes(Vars vars)
	{
		return mission_def.dismiss_outcomes.DecideOutcomes(game, vars);
	}

	public Vars CreateOutcomeVars(bool manual, string dismiss_reason = null)
	{
		Vars vars = new Vars(this);
		vars.Set("mission", mission_def.field.key);
		vars.Set("spawn_condition", "MercenarySpawnCondition");
		vars.Set("target", army);
		vars.Set("outcomes_def_key", "mercenary_dismiss");
		vars.Set("mission_key", mission_def.field.key);
		vars.Set("mission_valid", mission_def.Validate(this, army.GetKingdom()));
		vars.Set("manually_dismissed", manual);
		vars.Set("dismiss_reason", dismiss_reason);
		return vars;
	}

	public void ApplyDismissOutcomes(bool manual, string dismiss_reason = null)
	{
		Vars vars = CreateOutcomeVars(manual, dismiss_reason);
		List<OutcomeDef> outcomes = DecideOutcomes(vars);
		List<OutcomeDef> list = OutcomeDef.UniqueOutcomes(outcomes);
		OutcomeDef.PrecalculateValues(list, game, vars, vars);
		Event obj = new Event(army, "action_outcomes", army);
		obj.outcomes = outcomes;
		obj.vars = vars;
		obj.send_to_kingdoms = new List<int> { army.kingdom_id };
		FireEvent(obj);
		for (int i = 0; i < list.Count; i++)
		{
			OutcomeDef outcome = list[i];
			ApplyOutcome(outcome, vars);
		}
	}

	public virtual bool ApplyOutcome(OutcomeDef outcome, Vars outcome_vars)
	{
		string key = outcome.key;
		if (key == "become_normal")
		{
			Dismiss();
			return true;
		}
		if (outcome.Apply(game, outcome_vars))
		{
			return true;
		}
		game.Warning(ToString() + ": unhandled outcome: " + outcome.id);
		return false;
	}

	private bool CheckInWater()
	{
		if (army.currently_on_land)
		{
			return false;
		}
		if (army.movement.IsMoving())
		{
			return false;
		}
		if (army.realm_in == null)
		{
			army.FleeFrom(army.position, 5f);
			return true;
		}
		Realm nearbyRealm = MercenarySpawner.GetNearbyRealm(army.realm_in, 25, army.kingdom_id, army);
		if (nearbyRealm != null)
		{
			army.MoveTo(Army.GetRandomRealmPoint(nearbyRealm));
			return true;
		}
		Disappear();
		return false;
	}

	public Mercenary(Multiplayer multiplayer)
		: base(multiplayer)
	{
		def = game.defs.GetBase<Def>();
		if (def != null)
		{
			kingdom_id = FactionUtils.GetFactionKingdom(game, def.kingdom_key).id;
			fatigue = new ComputableValue(0f, 0f, game, def.fatigue_min, def.fatigue_cap);
		}
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Mercenary(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}
}
