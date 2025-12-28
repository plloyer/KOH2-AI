using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Rebel)]
public class Rebel : Object
{
	[Serialization.State(11)]
	public class DefsState : Serialization.ObjectState
	{
		private string def_id = "";

		private string cond_def_id = "";

		private string agenda_def_id = "";

		public static DefsState Create()
		{
			return new DefsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			def_id = rebel.def.id;
			agenda_def_id = rebel.agenda.def.id;
			if (rebel.condition_def != null)
			{
				cond_def_id = rebel.condition_def.id;
			}
			else
			{
				cond_def_id = "";
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def_id, "def_id");
			ser.WriteStr(agenda_def_id, "agenda_def_id");
			ser.WriteStr(cond_def_id, "cond_def_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def_id = ser.ReadStr("def_id");
			agenda_def_id = ser.ReadStr("agenda_def_id");
			cond_def_id = ser.ReadStr("cond_def_id");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Rebel rebel)
			{
				rebel.ChangeType(rebel.game.defs.Get<Def>(def_id), send_state: false);
				rebel.def = rebel.game.defs.Get<Def>(def_id);
				rebel.agenda = new RebelAgenda(rebel.game.defs.Get<RebelAgenda.Def>(agenda_def_id));
				if (cond_def_id != "")
				{
					rebel.condition_def = rebel.game.defs.Get<RebelSpawnCondition.Def>(cond_def_id);
				}
				else
				{
					rebel.condition_def = null;
				}
				rebel.army?.NotifyListeners("rebel_def_update");
			}
		}
	}

	[Serialization.State(12)]
	public class KingdomState : Serialization.ObjectState
	{
		private int kingdom_id;

		private int loyal_to;

		public static KingdomState Create()
		{
			return new KingdomState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			kingdom_id = rebel.kingdom_id;
			loyal_to = rebel.loyal_to;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_id, "kingdom_id");
			ser.Write7BitUInt(loyal_to, "loyal_to");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdom_id = ser.Read7BitUInt("kingdom_id");
			loyal_to = ser.Read7BitUInt("loyal_to");
		}

		public override void ApplyTo(Object obj)
		{
			Rebel obj2 = obj as Rebel;
			obj2.kingdom_id = kingdom_id;
			obj2.loyal_to = loyal_to;
		}
	}

	[Serialization.State(13)]
	public class OriginState : Serialization.ObjectState
	{
		private int origin_realm_id;

		private int spawn_kingdom_id;

		private float onspawn_rebellion_risk;

		private string religion = "";

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
			Rebel rebel = obj as Rebel;
			origin_realm_id = rebel.origin_realm_id;
			spawn_kingdom_id = rebel.spawn_kingdom_id;
			onspawn_rebellion_risk = rebel.onspawn_rebellion_risk;
			religion = ((rebel.religion == null) ? "" : rebel.religion.name);
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(origin_realm_id, "origin_realm_id");
			ser.Write7BitUInt(spawn_kingdom_id, "spawn_kingdom_id");
			ser.WriteFloat(onspawn_rebellion_risk, "onspawn_rebellion_risk");
			ser.WriteStr(religion, "religion");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			origin_realm_id = ser.Read7BitSigned("origin_realm_id");
			spawn_kingdom_id = ser.Read7BitUInt("spawn_kingdom_id");
			onspawn_rebellion_risk = ser.ReadFloat("onspawn_rebellion_risk");
			religion = ser.ReadStr("religion");
		}

		public override void ApplyTo(Object obj)
		{
			Rebel obj2 = obj as Rebel;
			obj2.origin_realm_id = origin_realm_id;
			obj2.spawn_kingdom_id = spawn_kingdom_id;
			obj2.onspawn_rebellion_risk = onspawn_rebellion_risk;
			obj2.religion = obj.game.religions.Get(religion);
		}
	}

	[Serialization.State(14)]
	public class ArmyState : Serialization.ObjectState
	{
		public NID army_nid;

		public static ArmyState Create()
		{
			return new ArmyState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel).army != null;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = (obj as Rebel).army;
			army_nid = army;
			return army != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Army>(army_nid, "army_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			army_nid = ser.ReadNID<Army>("army_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Rebel rebel = obj as Rebel;
			Army army = rebel.army;
			Army army2 = army_nid.Get<Army>(obj.game);
			if (army != null)
			{
				army.rebel = null;
			}
			rebel.army = army2;
			if (rebel.army != null)
			{
				rebel.army.rebel = rebel;
			}
			rebel.army?.NotifyListeners("rebel_army_update");
		}
	}

	[Serialization.State(15)]
	public class LeaderState : Serialization.ObjectState
	{
		public NID leader_nid;

		public static LeaderState Create()
		{
			return new LeaderState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = (obj as Rebel).character;
			leader_nid = character;
			return character != null;
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
			(obj as Rebel).SetCharacter(leader_nid.Get<Character>(obj.game), send_state: false);
		}
	}

	[Serialization.State(18)]
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
			Rebel rebel = obj as Rebel;
			action = (int)rebel.current_action;
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
			(obj as Rebel).SetAction((Action)action, send_state: false);
		}
	}

	[Serialization.State(19)]
	public class EnemiesState : Serialization.ObjectState
	{
		private List<int> enemies = new List<int>();

		public static EnemiesState Create()
		{
			return new EnemiesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel).enemies.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel obj2 = obj as Rebel;
			if (enemies.Count > 0)
			{
				enemies.Clear();
			}
			foreach (int enemy in obj2.enemies)
			{
				enemies.Add(enemy);
			}
			return enemies.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = enemies.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.Write7BitUInt(enemies[i], "k_id_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (enemies.Count > 0)
			{
				enemies.Clear();
			}
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				enemies.Add(ser.Read7BitUInt("k_id_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Rebel rebel = obj as Rebel;
			if (rebel.enemies.Count > 0)
			{
				rebel.enemies.Clear();
			}
			for (int i = 0; i < enemies.Count; i++)
			{
				rebel.AddEnemy(enemies[i], send_state: false);
			}
		}
	}

	[Serialization.State(20)]
	public class ExperienceState : Serialization.ObjectState
	{
		public float xp;

		public static ExperienceState Create()
		{
			return new ExperienceState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel).experience > 0f;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			xp = rebel.experience;
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
			(obj as Rebel).experience = xp;
		}
	}

	[Serialization.State(21)]
	public class FatigueState : Serialization.ObjectState
	{
		public float fatigue;

		public static FatigueState Create()
		{
			return new FatigueState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel).fatigue != 0f;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			fatigue = rebel.fatigue;
			return fatigue != 0f;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(fatigue, "fatigue");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			fatigue = ser.ReadFloat("fatigue");
		}

		public override void ApplyTo(Object obj)
		{
			Rebel rebel = obj as Rebel;
			rebel.fatigue = fatigue;
			if (rebel.def != null && rebel.fatigue > rebel.def.fatigue_cap)
			{
				rebel.fatigue = rebel.def.fatigue_cap;
			}
		}
	}

	[Serialization.State(22)]
	public class LevelState : Serialization.ObjectState
	{
		public int level;

		public static LevelState Create()
		{
			return new LevelState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel).level != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			level = rebel.level;
			return level != 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(level, "level");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			level = ser.Read7BitUInt("level");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Rebel).level = level;
		}
	}

	[Serialization.State(23)]
	public class MarkDisbandState : Serialization.ObjectState
	{
		public bool marked_for_disband;

		public static MarkDisbandState Create()
		{
			return new MarkDisbandState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel).marked_for_disband;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			marked_for_disband = rebel.marked_for_disband;
			return marked_for_disband;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(marked_for_disband, "marked_for_disband");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			marked_for_disband = ser.ReadBool("marked_for_disband");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Rebel).marked_for_disband = marked_for_disband;
		}
	}

	[Serialization.State(24)]
	public class JoinRebellionState : Serialization.ObjectState
	{
		public static JoinRebellionState Create()
		{
			return new JoinRebellionState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			Rebel rebel = obj as Rebel;
			if (!rebel.IsLeader())
			{
				Rebellion rebellion = rebel.rebellion;
				if (rebellion != null)
				{
					rebellion.NotifyAffectedKingdoms("rebellion_new_rebel", rebel);
					rebellion.GetLoyalTo()?.NotifyListeners("own_rebellion_reinforced", this);
				}
			}
			rebel.army.NotifyListeners("rebellion_changed");
		}
	}

	[Serialization.State(25)]
	public class DefeatedByState : Serialization.ObjectState
	{
		public int defeatedBy = -1;

		public float gold_to_give_defeated_by;

		public static DefeatedByState Create()
		{
			return new DefeatedByState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebel)?.defeatedBy != null;
		}

		public override bool InitFrom(Object obj)
		{
			Rebel rebel = obj as Rebel;
			if (rebel.defeatedBy != null)
			{
				defeatedBy = rebel.defeatedBy.id;
			}
			gold_to_give_defeated_by = rebel.gold_to_give_defeated_by;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(defeatedBy, "defeated_by");
			ser.WriteFloat(gold_to_give_defeated_by, "gold_to_give_defeated_by");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			defeatedBy = ser.Read7BitSigned("defeated_by");
			if (Serialization.cur_version >= 9)
			{
				gold_to_give_defeated_by = ser.ReadFloat("gold_to_give_defeated_by");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Rebel obj2 = obj as Rebel;
			obj2.SetDefeatedBy(obj2.game.GetKingdom(defeatedBy), null, send_state: false);
			obj2.gold_to_give_defeated_by = gold_to_give_defeated_by;
		}
	}

	public class Def : Logic.Def
	{
		public struct ClassWeight
		{
			public string className;

			public float weight;
		}

		public string name;

		public int starting_unit_cnt = 4;

		public int coat_of_arms_index;

		public List<ClassWeight> classWeights;

		public float totalClassWeight;

		public float flee_distance = 15f;

		public float target_search_distance = 25f;

		public float min_settlement_spawn_range = 8f;

		public int units_max_tier;

		public bool can_join_battles = true;

		public bool can_attack_castle = true;

		public bool can_use_cavalry;

		public float exp_per_level = 100f;

		public int max_level = 15;

		public float level_up_chance_castle = 300f;

		public float level_up_chance_settlement = 50f;

		public float level_up_chance_open_field = 100f;

		public float level_up_chance_leader_also_recieves_levels = 50f;

		public string fraction_type;

		public string kingdom_key;

		public bool can_takeover_realm;

		public float chance_sack = 50f;

		public float merc_chance_to_reinforce = 30f;

		public int merc_reinforce_range = 1;

		public float chance_denounce_disband = 50f;

		public float rel_loyalty_change_mult;

		public float supplies_consumption_rate_mod = 1f;

		public int payoff_gold_base = 5000;

		public int payoff_realm_no = 1;

		public float fatigue_min;

		public float fatigue_cap = 1f;

		public float fatigue_loss = 0.01f;

		public float fatigue_field = 0.2f;

		public float fatigue_pillage = 0.2f;

		public float fatigue_siege_keep = 0.5f;

		public float fatigue_siege = 1f;

		public float fatigue_threshold = 0.6f;

		public float heal_timer_interval = 10f;

		public float healing_rate = 0.1f;

		public float reinforcement_starting_health = 0.5f;

		public float fatigue_chance_to_end_rest_below_threshold = 20f;

		public float plunder_rebel_mod = 0.3f;

		public Def promotes_to;

		public float min_promotion_period_time;

		public float max_promotion_period_time;

		public int promotion_general_num;

		public Def change_to_rebel_def;

		public Def change_to_loyalist_def;

		public float chance_join_court_on_independence;

		public float chance_general_taking_leadership = 70f;

		public float inital_wealth;

		public DT.Field max_wealth_field;

		public DT.Field reward_field;

		public float chance_disorder_takeover_realm;

		public MovementRestriction movement_restriction;

		private bool isGeneral;

		private bool isLeader;

		public float target_pick_defend_plunder = 2f;

		public float target_pick_defend_keep = 3f;

		public float target_pick_defend_town = 4f;

		public float target_pick_attack_plunder = 1.5f;

		public float target_pick_attack_keep = 1.25f;

		public float target_pick_attack_town = 1f;

		public float target_pick_distance = 10f;

		private Game game;

		public void LoadClassWeights(DT.Field f)
		{
			DT.Field field = f.FindChild("leader_class_weights");
			if (field == null)
			{
				return;
			}
			classWeights = new List<ClassWeight>();
			totalClassWeight = 0f;
			int count = field.children.Count;
			for (int i = 0; i < count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!string.IsNullOrEmpty(field2.key))
				{
					float num = field2.Float();
					classWeights.Add(new ClassWeight
					{
						className = field2.key,
						weight = num
					});
					totalClassWeight += num;
				}
			}
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

		public override bool Load(Game game)
		{
			name = dt_def.path;
			this.game = game;
			DT.Field field = dt_def.field;
			supplies_consumption_rate_mod = field.GetFloat("supplies_consumption_rate_mod", null, supplies_consumption_rate_mod);
			payoff_gold_base = field.GetInt("payoff_gold_base", null, payoff_gold_base);
			payoff_realm_no = field.GetInt("payoff_realm_no", null, payoff_realm_no);
			fatigue_min = field.GetFloat("fatigue_min", null, fatigue_min);
			fatigue_cap = field.GetFloat("fatigue_cap", null, fatigue_cap);
			fatigue_loss = field.GetFloat("fatigue_loss", null, fatigue_loss);
			fatigue_field = field.GetFloat("fatigue_field", null, fatigue_field);
			fatigue_pillage = field.GetFloat("fatigue_pillage", null, fatigue_pillage);
			fatigue_siege_keep = field.GetFloat("fatigue_siege_keep", null, fatigue_siege_keep);
			fatigue_siege = field.GetFloat("fatigue_siege", null, fatigue_siege);
			fatigue_threshold = field.GetFloat("fatigue_threshold", null, fatigue_threshold);
			fatigue_chance_to_end_rest_below_threshold = field.GetFloat("fatigue_chance_to_end_rest_below_threshold", null, fatigue_chance_to_end_rest_below_threshold);
			flee_distance = field.GetFloat("flee_distance", null, flee_distance);
			target_search_distance = field.GetFloat("target_search_distance", null, target_search_distance);
			starting_unit_cnt = field.GetInt("starting_unit_cnt", null, starting_unit_cnt);
			coat_of_arms_index = field.GetInt("coat_of_arms_index", null, coat_of_arms_index);
			units_max_tier = field.GetInt("units_max_tier");
			min_settlement_spawn_range = field.GetFloat("min_settlement_spawn_range", null, min_settlement_spawn_range);
			can_join_battles = field.GetBool("has_leader", null, can_join_battles);
			can_attack_castle = field.GetBool("can_attack_castle", null, can_attack_castle);
			can_takeover_realm = field.GetBool("can_takeover_realm", null, can_takeover_realm);
			can_use_cavalry = field.GetBool("can_use_cavalry", null, can_use_cavalry);
			exp_per_level = field.GetFloat("exp_per_level ", null, exp_per_level);
			max_level = field.GetInt("max_level ", null, max_level);
			fraction_type = field.GetString("fraction_type");
			kingdom_key = field.GetString("kingdom_key");
			chance_sack = field.GetFloat("chance_sack", null, chance_sack);
			chance_denounce_disband = field.GetFloat("chance_denounce_disband", null, chance_denounce_disband);
			rel_loyalty_change_mult = field.GetFloat("rel_loyalty_change_mult");
			merc_chance_to_reinforce = field.GetFloat("merc_chance_to_reinforce", null, merc_chance_to_reinforce);
			merc_reinforce_range = field.GetInt("merc_reinforce_range", null, merc_reinforce_range);
			plunder_rebel_mod = field.GetFloat("plunder_rebel_mod", null, plunder_rebel_mod);
			min_promotion_period_time = field.GetFloat("min_promotion_period_time", null, min_promotion_period_time);
			max_promotion_period_time = field.GetFloat("max_promotion_period_time", null, max_promotion_period_time);
			promotion_general_num = field.GetInt("promotion_general_num", null, promotion_general_num);
			if (!Enum.TryParse<MovementRestriction>(field.GetString("movement_restrictions", null, "Realm"), out movement_restriction))
			{
				movement_restriction = MovementRestriction.Realm;
			}
			chance_join_court_on_independence = field.GetFloat("chance_join_court_on_independance", null, chance_join_court_on_independence);
			chance_general_taking_leadership = field.GetFloat("chance_general_taking_leadership", null, chance_general_taking_leadership);
			inital_wealth = field.GetFloat("inital_wealth", null, inital_wealth);
			max_wealth_field = field.FindChild("max_wealth");
			reward_field = field.FindChild("rebel_army_defeated_reward");
			isLeader = base.field.key.StartsWith("Leader", StringComparison.Ordinal);
			isGeneral = base.field.key.StartsWith("General", StringComparison.Ordinal) || isLeader;
			chance_disorder_takeover_realm = field.GetFloat("chance_disorder_takeover_realm", null, chance_disorder_takeover_realm);
			LoadClassWeights(field);
			LoadLevelUpchances(field);
			target_pick_defend_plunder = field.GetFloat("target_pick_defend_plunder", null, target_pick_defend_plunder);
			target_pick_defend_keep = field.GetFloat("target_pick_defend_keep", null, target_pick_defend_keep);
			target_pick_defend_town = field.GetFloat("target_pick_defend_town", null, target_pick_defend_town);
			target_pick_attack_plunder = field.GetFloat("target_pick_attack_plunder", null, target_pick_attack_plunder);
			target_pick_attack_keep = field.GetFloat("target_pick_attack_keep", null, target_pick_attack_keep);
			target_pick_attack_town = field.GetFloat("target_pick_attack_town", null, target_pick_attack_town);
			target_pick_distance = field.GetFloat("target_pick_distance", null, target_pick_distance);
			heal_timer_interval = field.GetFloat("heal_timer_interval", null, heal_timer_interval);
			healing_rate = field.GetFloat("healing_rate", null, healing_rate);
			reinforcement_starting_health = field.GetFloat("reinforcement_starting_health", null, reinforcement_starting_health);
			return true;
		}

		public string GetRandomLeaderClass(Game game)
		{
			if (classWeights == null)
			{
				return "Marshal";
			}
			float num = game.Random(0f, totalClassWeight);
			float num2 = 0f;
			for (int i = 0; i < classWeights.Count; i++)
			{
				num2 += classWeights[i].weight;
				if (num < num2)
				{
					return classWeights[i].className;
				}
			}
			return classWeights[0].className;
		}

		public float GetMaxWealth(Rebel r)
		{
			if (r == null)
			{
				return 0f;
			}
			return max_wealth_field.Float(new Vars(r));
		}

		public bool IsGeneral()
		{
			return isGeneral;
		}

		public bool IsLeader()
		{
			return isLeader;
		}

		public Def GetPromotionDef()
		{
			if (promotes_to == null)
			{
				promotes_to = game.defs.Get<Def>(base.field.GetString("promotes_to"));
			}
			return promotes_to;
		}

		public Def GetChangeToRebelDef()
		{
			if (change_to_rebel_def == null)
			{
				change_to_rebel_def = game.defs.Get<Def>(base.field.GetString("change_to_rebel_def"));
			}
			return change_to_rebel_def;
		}

		public Def GetChangeToLoyalistDef()
		{
			if (change_to_loyalist_def == null)
			{
				change_to_loyalist_def = game.defs.Get<Def>(base.field.GetString("change_to_loyalist_def"));
			}
			return change_to_loyalist_def;
		}

		public Def GetLeaderDef()
		{
			if (IsLeader())
			{
				return this;
			}
			return GetPromotionDef()?.GetLeaderDef();
		}
	}

	public enum MovementRestriction
	{
		Realm,
		RebellionZone,
		Kingdom,
		World
	}

	public enum Action
	{
		None,
		Plunder,
		Rest,
		Wait,
		Defend
	}

	private struct TargetAttractivness
	{
		public float Distance;

		private float attractivness;

		public float GetAttractivness()
		{
			return attractivness;
		}
	}

	private const int STATES_IDX = 10;

	public Def def;

	public RebelSpawnCondition.Def condition_def;

	public float fatigue;

	public float experience;

	public int level;

	public float infulace;

	public Army army;

	public Character character;

	public Rebellion rebellion;

	public int origin_realm_id;

	public int spawn_kingdom_id;

	public int kingdom_id;

	public int loyal_to;

	public RebelAgenda agenda;

	public Bounty bounty;

	public Religion religion;

	private bool marked_for_disband;

	public Kingdom defeatedBy;

	public float gold_to_give_defeated_by;

	private float onspawn_rebellion_risk;

	public static bool enabled = true;

	public HashSet<int> enemies = new HashSet<int>();

	private Object last_sacked_castle;

	private static List<FactionArmyTargetPicker.Target> tThreatList = new List<FactionArmyTargetPicker.Target>(100);

	private List<Unit.Def> tmpList = new List<Unit.Def>(100);

	public Action current_action { get; private set; }

	public string rebel_type => def.field.Path() + ".rebel_type";

	public MapObject current_target_object { get; private set; }

	public Rebel(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Rebel(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return "Rebel.name";
	}

	public Rebel(Game game, int kingdom_id, int loyal_to, Realm realm, RebelSpawnCondition.Def cond_def, Def def, Rebellion join_rebellion = null)
		: base(game)
	{
		this.kingdom_id = kingdom_id;
		this.loyal_to = loyal_to;
		this.def = def;
		condition_def = cond_def;
		origin_realm_id = realm.id;
		religion = realm.religion;
		rebellion = join_rebellion;
		UpdateAgenda();
		spawn_kingdom_id = realm.GetKingdom()?.id ?? 0;
		AddFatigue(1f);
	}

	public bool HealValid()
	{
		if (!IsValid() || army == null || !army.IsValid())
		{
			return false;
		}
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
		int num = army.MaxUnits();
		bool flag2 = army.HasNoble();
		if (army.units.Count < num + (flag2 ? 1 : 0))
		{
			return true;
		}
		return false;
	}

	public void SetCharacter(Character character, bool send_state = true)
	{
		character?.SetRebel(def, send_state);
		this.character = character;
		if (send_state)
		{
			SendState<LeaderState>();
		}
	}

	public void SetArmy(Army a, bool send_state = true)
	{
		army = a;
		if (send_state)
		{
			SendState<ArmyState>();
		}
	}

	public bool IsLeader()
	{
		if (def == null)
		{
			return false;
		}
		return def.IsLeader();
	}

	public bool IsGeneral()
	{
		if (def == null)
		{
			return false;
		}
		return def.IsGeneral();
	}

	public bool IsRegular()
	{
		if (!IsGeneral())
		{
			return !IsLeader();
		}
		return false;
	}

	private float GetPromotionPeriodTime()
	{
		if (def == null)
		{
			return 0f;
		}
		return game.Random(def.min_promotion_period_time, def.max_promotion_period_time);
	}

	public void InsertNoble()
	{
		if (def == null || !def.can_use_cavalry || army == null || army.units == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit != null && unit.def.type == Unit.Type.Noble)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return;
		}
		Unit unit2 = army.AddNoble();
		if (unit2 != null)
		{
			Unit value = army.units[0];
			int num = army.units.IndexOf(unit2);
			if (num == -1)
			{
				Warning("Failed to add noble unit");
				return;
			}
			army.units[0] = unit2;
			army.units[num] = value;
			army.SendState<Army.UnitsState>();
			army.NotifyListeners("units_changed");
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsAuthority() || def == null)
		{
			return;
		}
		bounty = new Bounty(this);
		SetCharacter(character ?? army?.leader);
		if (character == null)
		{
			Realm originRealm = GetOriginRealm();
			Kingdom kingdom = game.GetKingdom(loyal_to);
			Kingdom kingdom2 = originRealm.GetKingdom();
			if (kingdom != null && kingdom.id != kingdom_id)
			{
				SetCharacter(CharacterFactory.CreateRebel(game, kingdom_id, loyal_to, def.GetRandomLeaderClass(game)));
			}
			else
			{
				Kingdom kingdom3 = originRealm.pop_majority.kingdom;
				float strength = originRealm.pop_majority.strength;
				if (kingdom3 != kingdom2 && (float)game.Random(0, 100) < strength)
				{
					SetCharacter(CharacterFactory.CreateRebel(game, kingdom_id, kingdom3.id, def.GetRandomLeaderClass(game)));
				}
				else if (kingdom2 != null)
				{
					SetCharacter(CharacterFactory.CreateRebel(game, kingdom_id, kingdom2.id, def.GetRandomLeaderClass(game)));
				}
				else
				{
					SetCharacter(CharacterFactory.CreateRebel(game, kingdom_id, kingdom_id, def.GetRandomLeaderClass(game)));
				}
			}
		}
		if (army == null)
		{
			SpawnArmy(kingdom_id, origin_realm_id, def);
		}
		if (army != null)
		{
			InsertNoble();
		}
		character.rebel_def = def;
		if (character.GetArmy() != army)
		{
			army.SetLeader(character);
		}
		bool num = rebellion != null;
		JoinRebellion();
		if (!num)
		{
			rebellion.AddWealth(def.inital_wealth);
		}
		if (!def.IsGeneral())
		{
			Timer.Start(this, "promotion_period", GetPromotionPeriodTime());
		}
		army?.realm_in?.GetKingdom()?.stability?.SpecialEvent(think_rebel: false);
	}

	public Realm GetOriginRealm()
	{
		return game.GetRealm(origin_realm_id);
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public Kingdom GetSpawnKingdom()
	{
		return game.GetKingdom(spawn_kingdom_id);
	}

	public Kingdom GetLoyalToKingdom()
	{
		return game.GetKingdom(loyal_to);
	}

	public override IRelationCheck GetStanceObj()
	{
		return rebellion;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "rebel":
			return this;
		case "leader":
		case "character":
			return character;
		case "bounty_ammount":
			return GetBounty();
		case "bounty_owner":
			return GetBountyOwner();
		case "is_leader":
			return IsLeader();
		case "is_general":
			return IsGeneral();
		case "is_regular":
			return !IsLeader() && !IsGeneral();
		case "rebellion":
			return rebellion;
		case "origin_realm":
			return GetOriginRealm();
		case "spawn_kingdom":
			return GetSpawnKingdom();
		case "is_loyalist":
			return IsLoyalist();
		case "squad_size_perc":
			return GetSquadSizePerc();
		case "army":
			return army;
		case "defeated_by":
			return defeatedBy;
		case "gold_to_give_defeated_by":
			return gold_to_give_defeated_by;
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public void SetInitalRisk(float risk)
	{
		onspawn_rebellion_risk = risk;
	}

	public bool HasBounty()
	{
		if (bounty == null)
		{
			return false;
		}
		if (bounty.cur_tier == -1)
		{
			return false;
		}
		return true;
	}

	public float GetSquadSizePerc()
	{
		if (rebellion == null)
		{
			return 0f;
		}
		return rebellion.GetManpowerBonus(this);
	}

	public float GetBounty()
	{
		if (bounty == null)
		{
			return 0f;
		}
		if (bounty.cur_tier == -1)
		{
			return 0f;
		}
		if (def == null)
		{
			return 0f;
		}
		return bounty.def.per_tier_amount[bounty.cur_tier];
	}

	public string GetSpawnReasonRaw()
	{
		if (condition_def == null)
		{
			return null;
		}
		return condition_def.reason;
	}

	public Vars CreateSpawnReasonVars()
	{
		Vars vars = new Vars(this);
		Realm realm_in = army.realm_in;
		for (int i = 0; i < realm_in.logicNeighborsRestricted.Count; i++)
		{
			Realm realm = realm_in.logicNeighborsRestricted[i];
			if (realm.GetKingdom().type == Kingdom.Type.RebelFaction || realm.GetKingdom().type == Kingdom.Type.LoyalistsFaction || realm.GetKingdom().type == Kingdom.Type.ReligiousFaction)
			{
				vars.Set("nearbyRebelRealm", realm);
				break;
			}
		}
		return vars;
	}

	protected override void OnDestroy()
	{
		if (army != null)
		{
			if (IsAuthority() && defeatedBy != null && gold_to_give_defeated_by > 0f)
			{
				defeatedBy.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, gold_to_give_defeated_by);
			}
			if (army.realm_in != null)
			{
				army.realm_in.NotifyListeners("rebel_defeated", this);
			}
			if (army.IsValid() && IsAuthority())
			{
				army.Destroy();
			}
		}
		LeaveRebellion(rebellion);
		base.OnDestroy();
	}

	public void Think(Realm realm)
	{
		if (def == null || !enabled || army == null)
		{
			return;
		}
		if (army.battle != null)
		{
			KingdomAI.ThinkAssaultSiege(army);
		}
		else
		{
			if ((marked_for_disband && TryDisband()) || army.IsFleeing() || FactionArmyTargetPicker.CheckIfReinforcement(army) || CheckFatigue())
			{
				return;
			}
			if (current_action == Action.Defend)
			{
				CastleResupply();
				if (fatigue >= def.fatigue_cap)
				{
					SetAction(Action.Rest);
				}
				else
				{
					SetAction(Action.Plunder);
				}
			}
			else
			{
				if (current_action != Action.Plunder)
				{
					return;
				}
				Realm[] relevantRealms = GetRelevantRealms(this);
				if (FactionArmyTargetPicker.ValidateTarget(army, current_target_object) && army.movement.IsMoving() && CheckMovementRestrictions(current_target_object.position))
				{
					return;
				}
				current_target_object = null;
				FactionArmyTargetPicker.Target[] targets = GetTargets(this, relevantRealms);
				if (targets == null)
				{
					return;
				}
				FactionArmyTargetPicker.CalcThreatLevel(army, targets);
				Point fleeVector = GetFleeVector(army, targets, def.target_search_distance);
				FactionArmyTargetPicker.Target t;
				Point wp;
				if (ValidateFleeVector(fleeVector, out var fleePoint))
				{
					army.MoveTo(fleePoint);
					current_target_object = null;
				}
				else if (FactionArmyTargetPicker.ChooseTarget(army, targets, out t))
				{
					army.MoveTo(t.mapObject);
					current_target_object = t.mapObject;
					if (t.mapObject is Settlement)
					{
						int id = (t.mapObject as Settlement).GetKingdom().id;
						AddEnemy(id);
					}
					FireEvent("attack_target", t.mapObject);
				}
				else if (!army.movement.IsMoving() && CalcWanderPoint(out wp))
				{
					army.MoveTo(wp);
				}
			}
		}
	}

	public void SetTarget(MapObject obj)
	{
		if (!enabled || army.movement.IsMoving())
		{
			return;
		}
		if (current_target_object != null)
		{
			Settlement obj2 = obj as Settlement;
			Settlement settlement = current_target_object as Settlement;
			if (obj2 == null || settlement == null || settlement.keep_effects != null)
			{
				return;
			}
		}
		army.MoveTo(obj);
		current_target_object = obj;
		SetAction(Action.Plunder);
	}

	private Realm[] GetRelevantRealms(Rebel rebel)
	{
		if (rebel == null)
		{
			return null;
		}
		if (rebel.army == null)
		{
			return null;
		}
		if (rebel.army.realm_in == null)
		{
			return null;
		}
		Realm[] array = null;
		if (def.movement_restriction == MovementRestriction.RebellionZone)
		{
			array = new Realm[rebellion.zone.Count];
			rebellion.zone.CopyTo(array, 0);
		}
		else
		{
			array = new Realm[rebel.army.realm_in.logicNeighborsRestricted.Count + 1];
			array[0] = rebel.army.realm_in;
			rebel.army.realm_in.logicNeighborsRestricted.CopyTo(array, 1);
		}
		return array;
	}

	private FactionArmyTargetPicker.Target[] GetTargets(Rebel rebel, Realm[] realms)
	{
		if (rebel?.def == null || realms == null || realms.Length == 0)
		{
			return null;
		}
		tThreatList.Clear();
		foreach (Realm realm in realms)
		{
			FactionArmyTargetPicker.Target target;
			if (realm.settlements != null && realm.settlements.Count > 0)
			{
				for (int j = 0; j < realm.settlements.Count; j++)
				{
					if (CalcTarget(realm.settlements[j]?.battle, rebel.def.target_search_distance, out target))
					{
						tThreatList.Add(target);
					}
					if (CalcTarget(realm.settlements[j], rebel.def.target_search_distance, out target))
					{
						tThreatList.Add(target);
					}
				}
			}
			if (realm.armies == null || realm.armies.Count <= 0)
			{
				continue;
			}
			for (int k = 0; k < realm.armies.Count; k++)
			{
				if (CalcTarget(realm.armies[k]?.battle, rebel.def.target_search_distance, out target))
				{
					tThreatList.Add(target);
				}
				if (CalcTarget(realm.armies[k], rebel.def.target_search_distance, out target))
				{
					tThreatList.Add(target);
				}
			}
		}
		return tThreatList.ToArray();
	}

	private bool CalcTarget(MapObject obj, float maxDistance, out FactionArmyTargetPicker.Target target)
	{
		target = default(FactionArmyTargetPicker.Target);
		if (this.army == null || def == null)
		{
			return false;
		}
		if (obj == null)
		{
			return false;
		}
		if (!FactionArmyTargetPicker.ValidateTarget(this.army, obj))
		{
			return false;
		}
		if (!CheckMovementRestrictions(obj.position))
		{
			return false;
		}
		if (obj is Army)
		{
			if (kingdom_id == obj.kingdom_id)
			{
				return false;
			}
			Army army = obj as Army;
			if (army.mercenary != null)
			{
				return false;
			}
			if (army == game.religions.catholic?.crusade?.army)
			{
				return false;
			}
			if ((army.position - this.army.position).Length() > maxDistance)
			{
				return false;
			}
			target.mapObject = obj;
		}
		if (obj is Settlement)
		{
			if (kingdom_id == obj.kingdom_id)
			{
				return false;
			}
			Settlement settlement = obj as Settlement;
			if (!settlement.IsActiveSettlement())
			{
				return false;
			}
			if (settlement.type == "Castle" && !def.can_attack_castle)
			{
				return false;
			}
			if (settlement.type == "Castle" && last_sacked_castle == settlement && (settlement as Castle).sacked && !IsLastNonOccupiedZoneRealm(settlement.GetRealm()))
			{
				return false;
			}
			if (settlement is Castle && game.time - rebellion.start_time < rebellion.def.min_time_before_attacking_towns)
			{
				return false;
			}
			if ((settlement.position - this.army.position).Length() > maxDistance)
			{
				return false;
			}
			target.mapObject = obj;
		}
		if (obj is Battle battle)
		{
			if (battle.settlement != null && battle.settlement is Castle && game.time - rebellion.start_time < rebellion.def.min_time_before_attacking_towns)
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

	public bool IsLastNonOccupiedZoneRealm(Realm r)
	{
		return !rebellion.occupiedRealms.Contains(r) & rebellion.zone.Contains(r) & (rebellion.occupiedRealms.Count == rebellion.zone.Count - 1);
	}

	private Point GetFleeVector(Army army, FactionArmyTargetPicker.Target[] threats, float maxDistance)
	{
		if (army == null || threats == null || threats.Length == 0)
		{
			return Point.Zero;
		}
		Point point = default(Point);
		for (int i = 0; i < threats.Length; i++)
		{
			if (!(threats[i].safety_level >= 0.3f) && threats[i].mapObject is Army)
			{
				Point point2 = army.position - threats[i].mapObject.position;
				float num = (maxDistance - Math.Min(maxDistance, point2.Length())) / maxDistance;
				point2.Normalize();
				point += point2 * num;
			}
		}
		return point.GetNormalized();
	}

	private bool ValidateFleeVector(Point fleeVector, out Point fleePoint)
	{
		if (def == null || (fleeVector.x == 0f && fleeVector.y == 0f))
		{
			fleePoint = default(Point);
			return false;
		}
		fleePoint = army.position + fleeVector * def.flee_distance;
		if (!CheckMovementRestrictions(fleePoint))
		{
			return false;
		}
		return true;
	}

	private void CastleResupply()
	{
		if (army.castle != null && army.supplies.Get() < army.supplies.GetMax())
		{
			army.castle.ResupplyArmy(army);
		}
	}

	private bool CheckFatigue()
	{
		if (character == null)
		{
			return false;
		}
		if (def == null)
		{
			return false;
		}
		if (current_action == Action.Rest)
		{
			fatigue -= def.fatigue_loss;
			if (fatigue < def.fatigue_min)
			{
				fatigue = def.fatigue_min;
			}
			SendState<FatigueState>();
			if (fatigue == 0f || (fatigue < def.fatigue_threshold && (float)game.Random(0, 100) < def.fatigue_chance_to_end_rest_below_threshold))
			{
				CastleResupply();
				SetAction(Action.Plunder);
				return false;
			}
			return true;
		}
		return false;
	}

	public void AddRelLoyal(string type, Kingdom k)
	{
		if (def != null)
		{
			Kingdom kingdom = game.GetKingdom(loyal_to);
			kingdom.AddRelationModifier(k, "rel_loyalists_" + type, kingdom, def.rel_loyalty_change_mult);
		}
	}

	public void AddFatigue(Battle.Type type)
	{
		if (def == null || army?.battle == null || !IsValid() || !army.IsValid())
		{
			return;
		}
		switch (type)
		{
		case Battle.Type.Plunder:
		case Battle.Type.PlunderInterrupt:
			fatigue += def.fatigue_pillage;
			break;
		case Battle.Type.Siege:
		case Battle.Type.Assault:
		case Battle.Type.BreakSiege:
			if (army.battle.settlement.type == "Keep")
			{
				fatigue += def.fatigue_siege_keep;
			}
			else
			{
				fatigue += def.fatigue_siege;
			}
			break;
		default:
			fatigue += def.fatigue_field;
			break;
		}
		if (fatigue >= def.fatigue_cap)
		{
			fatigue = def.fatigue_cap;
			SetAction(Action.Rest);
		}
		SendState<FatigueState>();
	}

	public void AddFatigue(float val)
	{
		if (def != null)
		{
			fatigue += val;
			if (fatigue >= def.fatigue_cap)
			{
				fatigue = def.fatigue_cap;
				SetAction(Action.Rest);
			}
			SendState<FatigueState>();
		}
	}

	public void UpdateAgenda()
	{
		RebelAgenda.Def def = CalcAgenda();
		if (def == null)
		{
			if (this.def == null || condition_def == null)
			{
				return;
			}
			Game.Log("fail to find Agenda for " + this.def.name + " with condition " + condition_def.name + " ", Game.LogType.Message);
		}
		agenda = new RebelAgenda(def);
	}

	private RebelAgenda.Def CalcAgenda()
	{
		if (condition_def != null && condition_def.rebel_agendas != null && condition_def.rebel_agendas.Count != 0)
		{
			return condition_def.rebel_agendas[game.Random(0, condition_def.rebel_agendas.Count)];
		}
		List<RebelAgenda.Def> defs = game.defs.GetDefs<RebelAgenda.Def>();
		if (defs == null || defs.Count == 0)
		{
			return null;
		}
		return defs[game.Random(0, defs.Count)];
	}

	public void Disband()
	{
		NotifyListeners("disband");
		Destroy();
	}

	private bool TryDisband()
	{
		if (def == null)
		{
			return false;
		}
		if ((float)game.Random(0, 100) >= def.chance_denounce_disband)
		{
			return false;
		}
		Disband();
		return true;
	}

	private void RequestMercenaryReinforcement(int range = 1)
	{
		if (IsAuthority() && def != null && (float)game.Random(0, 100) < def.merc_chance_to_reinforce)
		{
			SendMercenaryReinforcement(new List<Realm> { army.realm_in }, range);
		}
	}

	private void SendMercenaryReinforcement(List<Realm> realms, int range)
	{
		if (range == 0)
		{
			return;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			for (int j = 0; j < realms[i].armies.Count; j++)
			{
				if (realms[i].armies[j].mercenary != null)
				{
					if (realms[i].armies[j].units == null)
					{
						Warning(string.Concat(realms[i].armies[j], ".units = null"));
					}
					realms[i].armies[j].mercenary.Reinforce(game.Random(1, realms[i].armies[j].units.Count), army);
					return;
				}
			}
			SendMercenaryReinforcement(realms[i].logicNeighborsRestricted, range - 1);
		}
	}

	public void SetAction(Action action, bool send_state = true)
	{
		if (def == null || action == current_action)
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
		Action action2 = current_action;
		current_action = action;
		if (!IsAuthority())
		{
			return;
		}
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
			if (action2 != Action.None)
			{
				RequestMercenaryReinforcement(def.merc_reinforce_range);
			}
		}
		else
		{
			Timer.Stop(this, "heal_timer");
		}
		if (current_action == Action.Plunder && army != null)
		{
			army.SetOffFromCamp();
		}
		if (current_action == Action.Wait && army != null)
		{
			current_target_object = null;
			army.SetUpCamp();
		}
		if (send_state)
		{
			SendState<ActionState>();
		}
	}

	public bool SpawnArmy(int kingdom_id, int realm_id, Def def)
	{
		Realm realm = game.GetRealm(realm_id);
		if (realm == null)
		{
			return false;
		}
		if (realm.settlements.Count == 0)
		{
			return false;
		}
		Point randomRealmPoint = GetRandomRealmPoint(realm, def);
		if (army == null)
		{
			SetArmy(new Army(realm.game, randomRealmPoint, kingdom_id));
		}
		if (army != null)
		{
			army.SetRebel(this);
			SendState<ArmyState>();
			army.SetLeader(character);
			if (!army.started)
			{
				army.Start();
			}
		}
		FillWithUnits(this, realm);
		AddEquipment();
		AddEnemy(realm.GetKingdom().id);
		army.NotifyListeners("became_rebel");
		return true;
	}

	public int GetMaxUnitCount()
	{
		if (army == null)
		{
			return 0;
		}
		return army.MaxUnits();
	}

	private int GetEligableUnitsQuality()
	{
		if (def == null)
		{
			return 0;
		}
		return def.units_max_tier;
	}

	public static Point GetRandomRealmPoint(Realm realm, Def def, Point? realmCenter = null)
	{
		if (realm == null)
		{
			return Point.Zero;
		}
		if (def == null)
		{
			return Point.Zero;
		}
		if ((realm.settlements == null || realm.settlements.Count == 0) && !realmCenter.HasValue)
		{
			return Point.Zero;
		}
		if (!realmCenter.HasValue)
		{
			realmCenter = realm.castle.position;
		}
		Game game = realm.game;
		Point point = FactionUtils.WorldToMapPoint(game, realmCenter.Value);
		Point point2 = new Point(game.Random(-1f, 1f), game.Random(-1f, 1f));
		point2.Normalize();
		Point point3 = point + point2 * 300f;
		List<Point> list = new List<Point>();
		FactionUtils.Trace((int)point.x, (int)point.y, (int)point3.x, (int)point3.y, (short)realm.id, game.realm_id_map, list);
		PathFinding path_finding = realm.game.path_finding;
		if (path_finding == null || path_finding.data == null || !path_finding.data.initted)
		{
			return Point.Zero;
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Point point4 = FactionUtils.MapToWorldPoint(game, list[num]);
			path_finding.data.WorldToGrid(point4, out var x, out var y);
			PathData.Node node = path_finding.data.GetNode(x, y);
			if (node.lsa == 0 || !path_finding.data.IsPassable(point4) || node.town || node.water || node.river || node.lake || node.ocean)
			{
				list.RemoveAt(num);
			}
			else if (IsNearSettelment(realm, point4, def.min_settlement_spawn_range) || PathData.IsNearRiver(path_finding?.data, point4, 6f) || PathData.IsNearOcean(path_finding?.data, point4, 6f))
			{
				list.RemoveAt(num);
			}
		}
		if (list.Count == 0)
		{
			if (realm.settlements == null || realm.settlements.Count == 0)
			{
				if (!realmCenter.HasValue)
				{
					return Point.Zero;
				}
				return realmCenter.Value;
			}
			int index = realm.game.Random(0, realm.settlements.Count - 1);
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
			if (realm.settlements[i].IsActiveSettlement() && p.Dist(realm.settlements[i].position) < range)
			{
				return true;
			}
		}
		return false;
	}

	public void ChangeLoyalTo(int kid)
	{
		if (!IsAuthority())
		{
			return;
		}
		Kingdom kingdom = game.GetKingdom(kid);
		bool num = (kingdom.IsRegular() && !IsLoyalist()) || (!kingdom.IsRegular() && IsLoyalist());
		bool flag = kingdom.IsRegular();
		Def def = null;
		if (num)
		{
			def = ((!flag) ? this.def.GetChangeToRebelDef() : this.def.GetChangeToLoyalistDef());
			if (def != null)
			{
				kingdom_id = FactionUtils.GetFactionKingdom(game, def.kingdom_key).id;
			}
		}
		loyal_to = kid;
		SendState<KingdomState>();
		if (def != null)
		{
			ChangeType(def);
		}
		army.SetKingdom(kingdom_id);
		character.SetKingdom(kingdom_id);
		army.FireEvent("became_rebel", null);
	}

	public bool IsLoyalist()
	{
		return kingdom_id != loyal_to;
	}

	public bool CheckUnitReplenish(int cnt = 1)
	{
		if (army == null)
		{
			Log("rebel is missing an army " + ToString());
			return false;
		}
		if (army.realm_in == null)
		{
			return false;
		}
		bool result = false;
		if (GetRecruitableUnitIDs(army.realm_in, this, ref tmpList))
		{
			int maxUnitCount = GetMaxUnitCount();
			int num = 0;
			while (num < cnt && army.units.Count - 1 < maxUnitCount)
			{
				int index = army.game.Random(0, tmpList.Count);
				Unit.Def def = tmpList[index];
				army.AddUnit(def);
				NotifyListeners("reinforce");
				num++;
				result = true;
			}
		}
		return result;
	}

	public void FillWithUnits(Rebel rebel, Realm realm)
	{
		if (rebel?.def == null || rebel.army == null || rebel.army.units == null || rebel.army.units.Count > 0)
		{
			return;
		}
		if (rebel.army.leader != null && rebel.def.can_use_cavalry)
		{
			army.AddNoble();
		}
		if (GetRecruitableUnitIDs(realm, rebel, ref tmpList))
		{
			int num = Math.Min(army.MaxUnits(), this.def.starting_unit_cnt) + ((rebel.army.leader != null) ? 1 : 0);
			if (tmpList.Count > 0)
			{
				for (int i = army.units.Count; i < num; i++)
				{
					int index = army.game.Random(0, tmpList.Count);
					Unit.Def def = tmpList[index];
					army.AddUnit(def);
				}
			}
		}
		army.SendState<Army.UnitsState>();
		if (army.started)
		{
			army.NotifyListeners("units_changed");
		}
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

	public bool AddRandomUnit(Kingdom kingdom)
	{
		if (army == null || kingdom == null)
		{
			return false;
		}
		List<Unit.Def> result = new List<Unit.Def>();
		if (!GetRecruitableUnitIDs(army.realm_in, this, ref result))
		{
			return false;
		}
		if (result == null || result.Count == 0)
		{
			return false;
		}
		int num = army.MaxUnits();
		bool flag = army.HasNoble();
		Unit unit = null;
		if (result.Count > 0 && army.units.Count < num + (flag ? 1 : 0))
		{
			int index = army.game.Random(0, result.Count);
			Unit.Def def = result[index];
			unit = army.AddUnit(def, -1, mercenary: true, send_state: false);
		}
		if (unit != null)
		{
			unit.SetDamage(1f - this.def.reinforcement_starting_health, send_state: false);
			army.SendState<Army.UnitsState>();
			if (army.started)
			{
				army.NotifyListeners("units_changed");
			}
		}
		return unit != null;
	}

	public static bool GetRecruitableUnitIDs(Realm realm, Rebel rebel, ref List<Unit.Def> result)
	{
		if (realm == null || realm.IsSeaRealm())
		{
			return false;
		}
		Game game = realm.game;
		result.Clear();
		if (realm != null)
		{
			if (realm.castle == null)
			{
				return false;
			}
			if (realm.castle.available_units == null)
			{
				rebel.Log($"realm {realm} is missing a available_units?");
				return false;
			}
			List<Unit.Def> availableUnitTypes = realm.castle.available_units.GetAvailableUnitTypes();
			int eligableUnitsQuality = rebel.GetEligableUnitsQuality();
			for (int i = 0; i < availableUnitTypes.Count; i++)
			{
				if (!availableUnitTypes[i].IsBase())
				{
					Unit.Def def = game.defs.Find<Unit.Def>(availableUnitTypes[i].field.Path());
					if (def != null && (rebel.def.can_use_cavalry || !def.is_cavalry) && def.buildPrerqusite.Validate(realm.castle, null, is_rebel: true) && def.tier <= eligableUnitsQuality)
					{
						result.Add(def);
					}
				}
			}
		}
		if (result.Count == 0)
		{
			DT.Def def2 = realm.game.dt.FindDef("Unit");
			if (def2 == null || def2.defs == null || def2.defs.Count < 1)
			{
				return false;
			}
			for (int j = 0; j < def2.defs.Count; j++)
			{
				DT.Field field = def2.defs[j].field;
				if (!string.IsNullOrEmpty(field.base_path) && !(field.base_path == "Noble") && field.GetBool("available"))
				{
					Unit.Def def3 = game.defs.Get<Unit.Def>(field.Path());
					if ((rebel.def.can_use_cavalry || !def3.is_cavalry) && def3.type != Unit.Type.InventoryItem)
					{
						result.Add(def3);
					}
				}
			}
		}
		return true;
	}

	private static float CalcAtractivness(Rebel rebel, MapObject obj)
	{
		if (rebel?.def == null)
		{
			return 0f;
		}
		if (obj == null)
		{
			return 0f;
		}
		if (obj is Settlement)
		{
			float num = rebel.army.position.Dist(obj.position);
			return 1f - num / rebel.def.target_search_distance;
		}
		if (obj is Army)
		{
			float num2 = rebel.army.position.Dist(obj.position);
			num2 = 1f - num2 / rebel.def.target_search_distance;
			float num3 = 1 - (obj as Army).EvalStrength() / rebel.army.EvalStrength();
			_ = (num2 + num3) / 2f;
		}
		return 0f;
	}

	private bool CheckMovementRestrictions(Point pt)
	{
		if (def == null)
		{
			return false;
		}
		if (def.movement_restriction == MovementRestriction.Realm)
		{
			return army.realm_in == game.GetRealm(pt);
		}
		if (def.movement_restriction == MovementRestriction.RebellionZone)
		{
			return rebellion.zone.Contains(game.GetRealm(pt));
		}
		if (def.movement_restriction == MovementRestriction.Kingdom)
		{
			return army.realm_in.GetKingdom() == game.GetKingdom(pt);
		}
		return true;
	}

	public void AddEnemy(int e, bool send_state = true)
	{
		if (!enemies.Contains(e))
		{
			enemies.Add(e);
			if (send_state)
			{
				SendState<EnemiesState>();
			}
		}
	}

	public void AddExperience(float val)
	{
		experience += val;
		SendState<ExperienceState>();
	}

	public void AddBounty(Kingdom k, int tier)
	{
		if (k != null)
		{
			if (bounty == null)
			{
				bounty = new Bounty(this);
			}
			bounty.AddBounty(k, tier);
		}
	}

	public void ClaimBounty(Kingdom k)
	{
		if (bounty != null)
		{
			bounty.Claim(k);
		}
	}

	private bool IncreaseOrGiveRandomSkill()
	{
		if (game.Random(0, 99) < 50)
		{
			if (character.ThinkUpgradeSkill(for_free: true))
			{
				return true;
			}
			if (character.ThinkNewSkill(all: true, for_free: true))
			{
				return true;
			}
		}
		else
		{
			if (character.ThinkNewSkill(all: true, for_free: true))
			{
				return true;
			}
			if (character.ThinkUpgradeSkill(for_free: true))
			{
				return true;
			}
		}
		return false;
	}

	private void TryLevelLeader(int numLevels)
	{
		if (!IsLeader() && rebellion?.leader != null && (float)game.Random(0, 100) < def.level_up_chance_leader_also_recieves_levels)
		{
			rebellion.leader.LevelUp(numLevels);
		}
	}

	public bool LevelUp(int numLevels = 1)
	{
		if (numLevels <= 0)
		{
			return false;
		}
		if (!IsLeader())
		{
			TryLevelLeader(numLevels);
		}
		bool result = false;
		while (numLevels > 0 && IncreaseOrGiveRandomSkill())
		{
			result = true;
			numLevels--;
		}
		while (numLevels > 0 && IsLeader() && !rebellion.IsFamous() && !rebellion.TryBecomeFamous())
		{
			result = true;
			numLevels--;
		}
		return result;
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

	public void CalcBattleOver(Battle b, bool won)
	{
		if (def == null || b == null)
		{
			return;
		}
		if (won)
		{
			if (army.battle_side != 0)
			{
				_ = b.attacker;
			}
			else
			{
				_ = b.defender;
			}
			if (b != null && b.settlement != null && b.is_plunder && b.settlement.type != "Castle")
			{
				float val = (float)Math.Ceiling(b.GoldFromPlunder(is_supporter: false) * def.plunder_rebel_mod);
				if ((float)game.Random(0, 100) < rebellion.GetPlunderChance())
				{
					rebellion.AddWealth(val);
				}
			}
			if (b != null && b.settlement != null && b.settlement.type == "Castle")
			{
				last_sacked_castle = b.settlement;
			}
			TryLevelUp(b);
		}
		else if (IsValid() && (army.IsDefeated() || army.leader == null || army.leader.IsDead()))
		{
			SetDefeatedBy(army.battle.GetSideKingdom(1 - army.battle_side), army.battle.BestAntiRebelLeader(1 - army.battle_side));
			return;
		}
		AddExperience(GetBattleExp(b, won));
		AddFatigue(b.type);
		army.SetBattle(null);
		if (won && b.is_siege)
		{
			SetAction(Action.Defend);
		}
	}

	public void SetDefeatedBy(Kingdom k, Character leader, bool send_state = true)
	{
		defeatedBy = k;
		if (defeatedBy != null && def.reward_field != null && send_state)
		{
			gold_to_give_defeated_by = def.reward_field.Float(this);
			if (leader != null)
			{
				gold_to_give_defeated_by *= 1f + leader.GetStat(Stats.cs_gold_from_rebellions_perc) / 100f;
			}
		}
		if (send_state && IsAuthority())
		{
			SendState<DefeatedByState>();
		}
	}

	public void HandleArmyMove()
	{
		current_target_object = null;
		SetAction(Action.Plunder);
	}

	public void Promote(Def def = null)
	{
		if (this.def != null)
		{
			if (def == null)
			{
				def = this.def.GetPromotionDef();
			}
			if (def == null)
			{
				Game.Log("Fail to prmoto rebel. Reason: missing def", Game.LogType.Warning);
				return;
			}
			ChangeType(def);
			InsertNoble();
		}
	}

	public void ChangeType(Def def, bool send_state = true)
	{
		this.def = def;
		if (send_state)
		{
			SendState<DefsState>();
		}
		NotifyListeners("rebel_type_changed");
		rebellion?.NotifyListeners("rebel_type_changed");
	}

	public void JoinRebellion()
	{
		if (rebellion == null || !rebellion.rebels.Contains(this))
		{
			Rebellion.JoinRebel(this);
		}
	}

	public void JoinRebellion(Rebellion rebellion, bool send_state = true)
	{
		if (rebellion != null)
		{
			rebellion.AddRebel(this);
			if (!IsLeader())
			{
				rebellion.NotifyAffectedKingdoms("rebellion_new_rebel", this);
				rebellion.GetLoyalTo()?.NotifyListeners("own_rebellion_reinforced", this);
			}
			if (send_state)
			{
				SendState<JoinRebellionState>();
			}
			army?.NotifyListeners("rebellion_changed");
		}
	}

	public void LeaveRebellion(Rebellion rebellion)
	{
		if (rebellion != null)
		{
			if (rebellion == this.rebellion)
			{
				this.rebellion = null;
			}
			rebellion.DelRebel(this);
		}
	}

	public Character GetBountyOwner()
	{
		if (bounty.cur_tier != -1 && bounty.kingdom_id != -1)
		{
			return game.GetKingdom(bounty.kingdom_id)?.GetKing();
		}
		return null;
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

	private bool CalcWanderPoint(out Point wp)
	{
		wp = Point.Zero;
		if (def == null)
		{
			return false;
		}
		if (army.is_in_water || (army.realm_in != null && army.realm_in.IsSeaRealm()))
		{
			int count = army.realm_in.neighbors.Count;
			int num = game.Random(0, count);
			for (int i = 0; i < count; i++)
			{
				Realm realm = army.realm_in.neighbors[(i + num) / count];
				if (!realm.IsSeaRealm())
				{
					Point randomRealmPoint = GetRandomRealmPoint(realm, def);
					if (CheckMovementRestrictions(randomRealmPoint))
					{
						wp = randomRealmPoint;
						break;
					}
				}
			}
		}
		else if (def.movement_restriction != MovementRestriction.Realm)
		{
			int count2 = army.realm_in.logicNeighborsRestricted.Count;
			int num2 = game.Random(0, count2);
			for (int j = 0; j < count2; j++)
			{
				Point randomRealmPoint2 = GetRandomRealmPoint(army.realm_in.logicNeighborsRestricted[(j + num2) / count2], def);
				if (CheckMovementRestrictions(randomRealmPoint2))
				{
					wp = randomRealmPoint2;
					break;
				}
			}
		}
		if (wp == Point.Zero)
		{
			if (def.movement_restriction == MovementRestriction.RebellionZone && rebellion.zone.Count > 0 && !rebellion.zone.Contains(army.realm_in))
			{
				wp = GetRandomRealmPoint(rebellion.zone[game.Random(0, rebellion.zone.Count)], def);
			}
			else
			{
				wp = GetRandomRealmPoint(army.realm_in, def);
			}
		}
		return wp != Point.Zero;
	}

	public void Denounce(Kingdom k)
	{
		if (!IsAuthority() || !IsLoyalist() || loyal_to != k.id || this.def == null)
		{
			return;
		}
		if (this.def.can_takeover_realm)
		{
			k.GetCrownAuthority().AddModifier("denounced_famous_rebel");
		}
		else
		{
			k.GetCrownAuthority().AddModifier("denounced_rebels");
		}
		if (TryDisband())
		{
			character.Die(new DeadStatus("unknown", character));
			return;
		}
		Kingdom kingdom = army.realm_in.GetKingdom();
		if (kingdom != null && kingdom.id > 0)
		{
			k.AddRelationModifier(kingdom, "rel_denounced_rebels", k);
		}
		Def def = null;
		def = game.defs.Get<Def>("Rebels");
		character.rebel_def = def;
		if (def != null)
		{
			Kingdom factionKingdom = FactionUtils.GetFactionKingdom(game, def.kingdom_key);
			if (factionKingdom != null)
			{
				kingdom_id = factionKingdom.id;
				loyal_to = factionKingdom.id;
				SendState<KingdomState>();
				ChangeType(def);
				army.SetKingdom(kingdom_id);
				JoinRebellion();
			}
		}
	}

	public override void OnTimer(Timer timer)
	{
		string name = timer.name;
		if (!(name == "promotion_period"))
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
				if (!flag && AddRandomUnit(army.realm_in.GetKingdom() ?? army.GetKingdom()))
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
		else
		{
			if (IsGeneral() || def == null)
			{
				return;
			}
			if (army.battle == null)
			{
				int num = 0;
				int num2 = 0;
				for (int j = 0; j < rebellion.rebels.Count; j++)
				{
					Rebel rebel = rebellion.rebels[j];
					if (!rebel.IsLeader())
					{
						if (rebel.IsGeneral())
						{
							num2++;
						}
						else
						{
							num++;
						}
					}
				}
				if (num > num2 + def.promotion_general_num)
				{
					Promote();
					return;
				}
			}
			Timer.Start(this, "promotion_period", GetPromotionPeriodTime());
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

	public override Value GetDumpStateValue()
	{
		return def?.name;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("agenda", agenda?.Name);
		dump.Append("kingdom", game.GetKingdom(kingdom_id)?.Name);
		dump.Append("loyal_to", game.GetKingdom(loyal_to)?.Name);
		dump.Append("origin_realm", game.GetRealm(origin_realm_id)?.name);
		dump.Append("spawn_kingdom", game.GetKingdom(spawn_kingdom_id)?.Name);
		dump.Append("current_action", current_action.ToString());
		dump.Append("onspawn_rebellion_risk", onspawn_rebellion_risk);
		dump.Append("religion", religion?.name);
		dump.Append("army", army?.ToString());
		dump.Append("character", character?.ToString());
		if (enemies != null && enemies.Count > 0)
		{
			dump.OpenSection("enemies");
			foreach (int enemy in enemies)
			{
				dump.Append(game.GetKingdom(enemy)?.Name);
			}
			dump.CloseSection("enemies");
		}
		dump.Append("experience", experience);
		dump.Append("fatigue", fatigue);
		dump.Append("level", level);
		dump.Append("marked_for_disband", marked_for_disband.ToString());
		dump.Append("rebellion", rebellion.ToString());
	}
}
