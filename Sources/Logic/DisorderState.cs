using System;
using System.Collections.Generic;
using System.Text;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Realm, dynamic = false)]
public class Realm : Object, IListener
{
	[Serialization.State(11)]
	public class KingdomState : Serialization.ObjectState
	{
		public int kingdom_id;

		public static KingdomState Create()
		{
			return new KingdomState();
		}

		public static bool IsNeeded(Object obj)
		{
			Realm realm = obj as Realm;
			return realm.kingdom_id != realm.init_kingdom_id;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			kingdom_id = realm.kingdom_id;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_id, "kingdom_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdom_id = ser.Read7BitUInt("kingdom_id");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Realm).SetKingdom(kingdom_id, ignore_victory: true, check_cancel_battle: false, via_diplomacy: false, send_state: false, from_gameplay: false);
		}
	}

	[Serialization.State(12)]
	public class ReligionState : Serialization.ObjectState
	{
		public string religion_def_id;

		public static ReligionState Create()
		{
			return new ReligionState();
		}

		public static bool IsNeeded(Object obj)
		{
			Realm realm = obj as Realm;
			if (realm.religion == null)
			{
				return false;
			}
			Kingdom kingdom = obj.game.GetKingdom(realm.id);
			if (kingdom == null)
			{
				return false;
			}
			if (kingdom.def == null)
			{
				return true;
			}
			string text = kingdom.def.GetString("religion");
			if (realm.religion.name != text)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			if (realm.religion != null)
			{
				religion_def_id = realm.religion.def.id;
			}
			else
			{
				religion_def_id = "";
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(religion_def_id, "religion_def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			religion_def_id = ser.ReadStr("religion_def");
		}

		public override void ApplyTo(Object obj)
		{
			Realm obj2 = obj as Realm;
			Religion rlg = null;
			if (religion_def_id != null && religion_def_id != "")
			{
				Religion.Def def = obj.game.defs.Find<Religion.Def>(religion_def_id);
				rlg = obj.game.religions.Get(def);
			}
			obj2.SetReligion(rlg, send_state: false);
		}
	}

	[Serialization.State(13)]
	public class PopMajorityState : Serialization.ObjectState
	{
		public int kingdom;

		public float strength;

		public static PopMajorityState Create()
		{
			return new PopMajorityState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			kingdom = ((realm.pop_majority.kingdom != null) ? realm.pop_majority.kingdom.id : 0);
			strength = realm.pop_majority.strength;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom, "pop_maj_kingdom");
			ser.WriteFloat(strength, "pop_maj_strength");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdom = ser.Read7BitUInt("pop_maj_kingdom");
			strength = ser.ReadFloat("pop_maj_strength");
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			realm.pop_majority.kingdom = realm.game.GetKingdom(kingdom);
			realm.pop_majority.strength = strength;
			realm.NotifyListeners("pop_majority_changed");
		}
	}

	[Serialization.State(14)]
	public class FeaturesState : Serialization.ObjectState
	{
		private List<string> features;

		public static FeaturesState Create()
		{
			return new FeaturesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			if (realm.features == null)
			{
				return false;
			}
			features = new List<string>(realm.features.Count);
			for (int i = 0; i < realm.features.Count; i++)
			{
				features.Add(realm.features[i]);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((features != null) ? features.Count : 0);
			ser.Write7BitUInt(num, "count");
			if (num != 0)
			{
				for (int i = 0; i < num; i++)
				{
					string val = features[i];
					ser.WriteStr(val, "feature_", i);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				features = new List<string>(num);
				for (int i = 0; i < num; i++)
				{
					string item = ser.ReadStr("feature_", i);
					features.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			if (features != null)
			{
				if (realm.features == null)
				{
					realm.features = new List<string>(features.Count);
				}
				else
				{
					realm.features.Clear();
				}
				for (int i = 0; i < features.Count; i++)
				{
					realm.features.Add(features[i]);
					realm.RefreshTags();
				}
			}
		}
	}

	[Serialization.State(15)]
	public class SkillsPerBuildingTypeState : Serialization.ObjectState
	{
		public struct BuildingSkills
		{
			public string building_type;

			public List<string> skills;
		}

		public List<BuildingSkills> lst;

		public static SkillsPerBuildingTypeState Create()
		{
			return new SkillsPerBuildingTypeState();
		}

		public static bool IsNeeded(Object obj)
		{
			Realm realm = obj as Realm;
			if (realm.skills_per_building_type == null || realm.skills_per_building_type.Count == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			if (realm.skills_per_building_type == null || realm.skills_per_building_type.Count == 0)
			{
				return false;
			}
			lst = new List<BuildingSkills>(realm.skills_per_building_type.Count);
			foreach (KeyValuePair<string, List<string>> item2 in realm.skills_per_building_type)
			{
				string key = item2.Key;
				List<string> value = item2.Value;
				BuildingSkills item = new BuildingSkills
				{
					building_type = key,
					skills = new List<string>(value)
				};
				lst.Add(item);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((lst != null) ? lst.Count : 0);
			ser.Write7BitUInt(num, "num_types");
			if (num <= 0)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				BuildingSkills buildingSkills = lst[i];
				ser.WriteStr(buildingSkills.building_type, "type", i);
				int num2 = ((buildingSkills.skills != null) ? buildingSkills.skills.Count : 0);
				ser.Write7BitUInt(num2, "num_skills", i);
				for (int j = 0; j < num2; j++)
				{
					string val = buildingSkills.skills[j];
					ser.WriteStr(val, "skill", 1000 * i + j);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("num_types");
			if (num <= 0)
			{
				return;
			}
			lst = new List<BuildingSkills>(num);
			for (int i = 0; i < num; i++)
			{
				BuildingSkills item = new BuildingSkills
				{
					building_type = ser.ReadStr("type", i)
				};
				int num2 = ser.Read7BitUInt("num_skills", i);
				item.skills = new List<string>(num2);
				for (int j = 0; j < num2; j++)
				{
					string item2 = ser.ReadStr("skill", 1000 * i + j);
					item.skills.Add(item2);
				}
				lst.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			if (lst != null && lst.Count > 0)
			{
				if (realm.skills_per_building_type == null)
				{
					realm.skills_per_building_type = new Dictionary<string, List<string>>();
				}
				else
				{
					realm.skills_per_building_type.Clear();
				}
				for (int i = 0; i < lst.Count; i++)
				{
					BuildingSkills buildingSkills = lst[i];
					if (buildingSkills.skills != null)
					{
						realm.skills_per_building_type.Add(buildingSkills.building_type, buildingSkills.skills);
					}
				}
			}
			else
			{
				realm.skills_per_building_type = null;
			}
			realm.RefreshSkills(null);
		}
	}

	[Serialization.State(16)]
	public class CoreToKingdomsState : Serialization.ObjectState
	{
		private List<int> ids = new List<int>();

		public static CoreToKingdomsState Create()
		{
			return new CoreToKingdomsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			ids = new List<int>(realm.coreToKingdoms.Count);
			for (int i = 0; i < realm.coreToKingdoms.Count; i++)
			{
				int kingdom_id = realm.coreToKingdoms[i].kingdom_id;
				ids.Add(kingdom_id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((ids != null) ? ids.Count : 0);
			ser.Write7BitUInt(num, "count");
			if (num != 0)
			{
				for (int i = 0; i < num; i++)
				{
					int val = ids[i];
					ser.Write7BitUInt(val, "k_id_", i);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				ids = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					int item = ser.Read7BitUInt("k_id_", i);
					ids.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			for (int i = 0; i < realm.coreToKingdoms.Count; i++)
			{
				int kingdom_id = realm.coreToKingdoms[i].kingdom_id;
				if (!ids.Contains(kingdom_id))
				{
					obj.game.GetKingdom(realm.coreToKingdoms[i].kingdom_id)?.coreRealmsRaw.Remove(realm);
					realm.coreToKingdoms.RemoveAt(i);
					i--;
				}
			}
			for (int j = 0; j < ids.Count; j++)
			{
				int id = ids[j];
				RealmCoreData realmCoreData = realm.coreToKingdoms.Find((RealmCoreData x) => x.kingdom_id == id);
				if (realmCoreData == null)
				{
					realmCoreData = new RealmCoreData(id, is_core: true, obj.game.time);
					realm.coreToKingdoms.Add(realmCoreData);
					obj.game.GetKingdom(id)?.coreRealmsRaw.Add(realm);
				}
			}
		}
	}

	[Serialization.State(17)]
	public class ControllerState : Serialization.ObjectState
	{
		private NID controller;

		private bool forceOccupation;

		public static ControllerState Create()
		{
			return new ControllerState();
		}

		public static bool IsNeeded(Object obj)
		{
			Realm realm = obj as Realm;
			return realm.GetKingdom() != realm.controller;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			controller = realm.controller;
			forceOccupation = realm.IsOccupied();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(controller, "controller");
			ser.WriteBool(forceOccupation, "is_occupied");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			controller = ser.ReadNID("controller");
			forceOccupation = ser.ReadBool("is_occupied");
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			realm.SetOccupied(controller.GetObj(realm.game), forceOccupation, send_state: false);
		}
	}

	[Serialization.State(18)]
	public class RecentOccupatorsState : Serialization.ObjectState
	{
		private List<int> ids = new List<int>();

		private List<float> deltaTimes = new List<float>();

		public static RecentOccupatorsState Create()
		{
			return new RecentOccupatorsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Realm).recentOccupators.Count != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			foreach (KeyValuePair<int, Time> recentOccupator in realm.recentOccupators)
			{
				ids.Add(recentOccupator.Key);
				deltaTimes.Add(recentOccupator.Value - realm.game.time);
			}
			return realm.recentOccupators.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(ids.Count, "recent_occupators_count");
			for (int i = 0; i < ids.Count; i++)
			{
				ser.Write7BitUInt(ids[i], "recent_occupator_id_", i);
				ser.WriteFloat(deltaTimes[i], "recent_occupator_time_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("recent_occupators_count");
			for (int i = 0; i < num; i++)
			{
				int item = ser.Read7BitUInt("recent_occupator_id_", i);
				ids.Add(item);
				float item2 = ser.ReadFloat("recent_occupator_time_", i);
				deltaTimes.Add(item2);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			realm.recentOccupators.Clear();
			for (int i = 0; i < ids.Count; i++)
			{
				realm.recentOccupators[ids[i]] = realm.game.time + deltaTimes[i];
			}
		}
	}

	[Serialization.State(19)]
	public class PrevOwnersState : Serialization.ObjectState
	{
		private List<int> ids = new List<int>();

		private List<float> deltaTimes = new List<float>();

		public static PrevOwnersState Create()
		{
			return new PrevOwnersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Realm).prev_owners.Count != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			foreach (Tuple<int, Time> prev_owner in realm.prev_owners)
			{
				ids.Add(prev_owner.Item1);
				deltaTimes.Add(prev_owner.Item2 - realm.game.time);
			}
			return realm.prev_owners.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(ids.Count, "prev_owners_count");
			for (int i = 0; i < ids.Count; i++)
			{
				ser.Write7BitUInt(ids[i], "prev_owners_id_", i);
				ser.WriteFloat(deltaTimes[i], "prev_owners_time_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("prev_owners_count");
			for (int i = 0; i < num; i++)
			{
				int item = ser.Read7BitUInt("prev_owners_id_", i);
				ids.Add(item);
				float item2 = ser.ReadFloat("prev_owners_time_", i);
				deltaTimes.Add(item2);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			realm.prev_owners.Clear();
			for (int i = 0; i < ids.Count; i++)
			{
				realm.prev_owners.Add(new Tuple<int, Time>(ids[i], realm.game.time + deltaTimes[i]));
			}
		}
	}

	[Serialization.State(20)]
	public class DisorderState : Serialization.ObjectState
	{
		public bool disorder;

		public static DisorderState Create()
		{
			return new DisorderState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			disorder = realm.IsDisorder();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(disorder, "disorder");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			disorder = ser.ReadBool("disorder");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Realm).SetDisorder(disorder, send_state: false);
		}
	}

	[Serialization.State(21)]
	public class StabilityState : Serialization.ObjectState
	{
		public float[] categories;

		public float value;

		public static StabilityState Create()
		{
			return new StabilityState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Realm).rebellionRisk != null;
		}

		public override bool InitFrom(Object obj)
		{
			Realm realm = obj as Realm;
			categories = new float[realm.rebellionRisk.NumCategories()];
			for (int i = 0; i < categories.Length; i++)
			{
				categories[i] = realm.GetRebellionRisk(i);
			}
			realm.rebellionRisk.CheckStability();
			value = realm.rebellionRisk.value;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(categories.Length, "num_categories");
			for (int i = 0; i < categories.Length; i++)
			{
				ser.WriteFloat(categories[i], "category_", i);
			}
			ser.WriteFloat(value, "value");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("num_categories");
			categories = new float[num];
			for (int i = 0; i < categories.Length; i++)
			{
				categories[i] = ser.ReadFloat("category_", i);
			}
			value = ser.ReadFloat("value");
		}

		public override void ApplyTo(Object obj)
		{
			Realm realm = obj as Realm;
			realm.rebellionRisk.value = value;
			for (int i = 0; i < categories.Length && i < realm.rebellionRisk.NumCategories(); i++)
			{
				realm.rebellionRisk.SetCategory(categories[i], i);
			}
		}
	}

	[Serialization.State(22)]
	public class TownNameState : Serialization.ObjectState
	{
		public string custom_town_name;

		public static TownNameState Create()
		{
			return new TownNameState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Realm)?.custom_town_name != null;
		}

		public override bool InitFrom(Object obj)
		{
			custom_town_name = (obj as Realm)?.custom_town_name;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(custom_town_name, "custom_town_name");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			custom_town_name = ser.ReadStr("custom_town_name");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Realm realm)
			{
				realm.custom_town_name = custom_town_name;
				if (realm.castle != null)
				{
					realm.castle.customName = custom_town_name;
					realm.castle.NotifyListeners("name_changed");
				}
				realm.NotifyListeners("town_name_changed");
			}
		}
	}

	[Serialization.Event(27)]
	public class ChangeTownNameEvent : Serialization.ObjectEvent
	{
		private string name;

		public ChangeTownNameEvent()
		{
		}

		public static ChangeTownNameEvent Create()
		{
			return new ChangeTownNameEvent();
		}

		public ChangeTownNameEvent(string name)
		{
			this.name = name;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(name, "name");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			name = ser.ReadStr("name");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Realm)?.ChangeTownName(name);
		}
	}

	public struct PopMajority
	{
		public Kingdom kingdom;

		public float strength;

		public override string ToString()
		{
			return strength + "% " + kingdom?.Name;
		}

		public bool HasFullMajority(Kingdom k)
		{
			if (k != null && k == kingdom)
			{
				return strength >= 100f;
			}
			return false;
		}
	}

	public struct RegionPopInfModifier
	{
		public enum Type
		{
			Error,
			Raw,
			Resource
		}

		public Type type;

		public string key;

		public float neighborStrength;

		public float neighborThroughSeaStrength;

		public RegionPopInfModifier(Type type, float neighborStrength, float neighborThroughSeaStrength, string name = "")
		{
			this.type = type;
			this.neighborStrength = neighborStrength;
			this.neighborThroughSeaStrength = neighborThroughSeaStrength;
			key = name;
		}

		public override string ToString()
		{
			return $"{type.ToString()} \"{key}\": {neighborStrength}/{neighborThroughSeaStrength}";
		}
	}

	private struct PopInfluence
	{
		public Kingdom kingdom;

		public int land_neighbors;

		public int sea_neighbors;
	}

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public DT.Field def;

	public DT.Field csv_field;

	public string name;

	public string town_name;

	public string custom_town_name;

	public int id;

	public int kingdom_id;

	public List<Tuple<int, Time>> prev_owners = new List<Tuple<int, Time>>();

	public Object controller;

	public Actions actions;

	public Castle castle;

	public List<Settlement> settlements = new List<Settlement>();

	public List<string> features = new List<string>();

	public List<Skill.Def> skills;

	public Dictionary<string, List<string>> skills_per_building_type;

	public List<Army> armies = new List<Army>();

	public List<Army> mercenaries = new List<Army>();

	public List<Migrant> migrants = new List<Migrant>();

	public Dictionary<int, Time> recentOccupators = new Dictionary<int, Time>();

	public Incomes incomes;

	public Incomes upkeeps;

	private Resource _income = new Resource();

	private Resource _expenses = new Resource();

	public bool income_valid;

	public List<Realm> neighbors = new List<Realm>();

	public List<Realm> logicNeighborsRestricted = new List<Realm>();

	public List<Realm> logicNeighborsAll = new List<Realm>();

	public List<RealmCoreData> coreToKingdoms = new List<RealmCoreData>();

	public List<Kingdom> historicalToKingdoms = new List<Kingdom>();

	public float fame;

	private float time_until_core;

	private float core_timeout;

	public List<string> unit_types = new List<string>();

	public float on_siege_convert_timeout;

	public int wave_depth = -1;

	public Realm wave_prev;

	public float wave_eval;

	public Stats stats;

	public AI.ProvinceSpecialization ai_specialization;

	public float normalizedGoldIncome;

	public ReligiousPower religiousPower;

	public RebellionRisk rebellionRisk;

	public List<Rebellion> rebellions = new List<Rebellion>();

	public List<Rebellion> potentialRebellions = new List<Rebellion>();

	public Religion religion;

	public PopMajority pop_majority;

	public string culture;

	public List<RegionPopInfModifier> regPopInfMods = new List<RegionPopInfModifier>();

	private static List<PopInfluence> s_pop_inf = new List<PopInfluence>(10);

	public RealmInfluence influence = new RealmInfluence(0, 0);

	public TradeCenter tradeCenter;

	public int tradeCenterDistance = -1;

	public List<int> tcGoldDistanceTresholds = new List<int>();

	public List<int> tcGoldAmounts = new List<int>();

	public List<Character> merchants = new List<Character>();

	public Resource incomeFromTradeCenterInfluence = new Resource();

	public Resource incomeFromSettlements = new Resource();

	public Resource incomeFromTown = new Resource();

	public Resource incomeFromPopulation = new Resource();

	public Resource incomeFromGovernorSkills = new Resource();

	public Resource upkeepGarrison = new Resource();

	public bool isBorder;

	public int distanceToWar = int.MaxValue;

	private bool disorder_state;

	public int init_kingdom_id;

	public KingdomAI.Threat threat;

	public KingdomAI.Threat attacker_threat;

	public KingdomAI.Threat help_with_rebels_threat;

	public Dictionary<string, int> tags;

	public List<Resource.Def> goods_produced = new List<Resource.Def>();

	private static List<Skill.Def> tmp_skills_list = new List<Skill.Def>(50);

	private static List<Skill.Def> tmp_skills = new List<Skill.Def>();

	private static List<Realm> appealNeighbors = null;

	private static Resource tmp_production = new Resource();

	public Resource income
	{
		get
		{
			RecalcIncomes();
			return _income;
		}
	}

	public Resource expenses
	{
		get
		{
			RecalcIncomes();
			return _expenses;
		}
	}

	public bool is_catholic
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.catholic;
			}
			return false;
		}
	}

	public bool is_orthodox
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.orthodox;
			}
			return false;
		}
	}

	public bool is_christian
	{
		get
		{
			if (religion != null)
			{
				return religion.def.christian;
			}
			return false;
		}
	}

	public bool is_sunni
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.sunni;
			}
			return false;
		}
	}

	public bool is_shia
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.shia;
			}
			return false;
		}
	}

	public bool is_muslim
	{
		get
		{
			if (religion != null)
			{
				return religion.def.muslim;
			}
			return false;
		}
	}

	public bool is_pagan
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.pagan;
			}
			return false;
		}
	}

	public Realm(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Realm(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	public void SetReligion(Religion rlg, bool send_state = true)
	{
		religion = rlg;
		if (!FixSettlementsReligion())
		{
			RefreshTags();
			castle?.RefreshBuildableDistricts();
			GetKingdom()?.RecalcBuildingStates();
			InvalidateIncomes();
		}
		if (send_state)
		{
			SendState<ReligionState>();
			OnStatusChangedAnalytics(GetKingdom(), "religion_changed", religion.name);
		}
		UpdateUnitSets();
		rebellionRisk?.Recalc();
		if (GetKingdom().realms.Find((Realm r) => r.religion != rlg) == null)
		{
			GetKingdom().NotifyListeners("all_provinces_are_of_religion", rlg);
		}
		NotifyListeners("religion_changed");
		castle?.governor?.OnGovernedRealmReligionChanged();
	}

	public int FindRegPopInfModIdx(RegionPopInfModifier.Type type, string key)
	{
		for (int i = 0; i < regPopInfMods.Count; i++)
		{
			RegionPopInfModifier regionPopInfModifier = regPopInfMods[i];
			if (regionPopInfModifier.type == type && regionPopInfModifier.key == key)
			{
				return i;
			}
		}
		return -1;
	}

	public void AddRegPopInfMod(RegionPopInfModifier mod, float valueMultiplier = 1f)
	{
		int num = FindRegPopInfModIdx(mod.type, mod.key);
		mod.neighborStrength *= valueMultiplier;
		mod.neighborThroughSeaStrength *= valueMultiplier;
		if (num < 0)
		{
			regPopInfMods.Add(mod);
			return;
		}
		RegionPopInfModifier value = regPopInfMods[num];
		value.neighborStrength += mod.neighborStrength;
		value.neighborThroughSeaStrength += mod.neighborThroughSeaStrength;
		if (value.neighborStrength == 0f && value.neighborThroughSeaStrength == 0f)
		{
			regPopInfMods.RemoveAt(num);
		}
		else
		{
			regPopInfMods[num] = value;
		}
	}

	public void InitPopMajority()
	{
		Kingdom kingdom = GetKingdom();
		pop_majority.kingdom = kingdom;
		pop_majority.strength = 100f;
		string text = csv_field?.GetString("PopulationPerc");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		int num = text.IndexOf('.');
		if (num >= 0)
		{
			if (int.TryParse(text.Substring(num + 1), out var result))
			{
				pop_majority.strength = result;
			}
			text = text.Substring(0, num);
		}
		kingdom = game.GetKingdom(text);
		if (kingdom != null)
		{
			pop_majority.kingdom = kingdom;
		}
		else
		{
			Game.Log(csv_field.Path(include_file: true) + ".PopulationPerc: Unknown kingdom '" + text + "'", Game.LogType.Error);
		}
	}

	private void AddPopInfRealm(Kingdom k, bool land)
	{
		for (int i = 0; i < s_pop_inf.Count; i++)
		{
			PopInfluence value = s_pop_inf[i];
			if (value.kingdom == k)
			{
				if (land)
				{
					value.land_neighbors++;
				}
				else
				{
					value.sea_neighbors++;
				}
				s_pop_inf[i] = value;
				return;
			}
		}
		s_pop_inf.Add(new PopInfluence
		{
			kingdom = k,
			land_neighbors = (land ? 1 : 0),
			sea_neighbors = ((!land) ? 1 : 0)
		});
	}

	private void AddPopInfRealm(Realm neighbor)
	{
		Kingdom kingdom = neighbor.GetKingdom();
		if (kingdom != null && kingdom == neighbor.pop_majority.kingdom && kingdom.religion == neighbor.religion)
		{
			AddPopInfRealm(kingdom, neighbors.Contains(neighbor));
		}
	}

	public void OnStatusChangedAnalytics(Kingdom send_to, string evt, string religion_status = "no_change")
	{
		if (IsAuthority() && send_to != null && send_to.is_player && evt != null && !Game.isLoadingSaveGame)
		{
			Vars vars = new Vars();
			vars.Set("targetKingdom", send_to.Name);
			vars.Set("targetProvince", name);
			vars.Set("conquestStatus", evt);
			vars.Set("remainingProvinces", send_to.realms.Count);
			vars.Set("religionStatus", religion_status);
			send_to.FireEvent("analytics_conquest", vars, send_to.id);
		}
	}

	private float CalcPopInfStrength(Kingdom k, int land_neighbors, int sea_neighbors, StringBuilder sb_pop_inf_dbg = null)
	{
		Cultures.Def def = game.cultures.def;
		Kingdom kingdom = GetKingdom();
		float num3;
		if (k == kingdom)
		{
			if (k.IsEnemy(pop_majority.kingdom))
			{
				sb_pop_inf_dbg?.AppendLine("  At war with " + pop_majority.kingdom.Name);
				return 0f;
			}
			float val = GetLocalAuthority();
			float num = kingdom.GetStat(Stats.ks_influence) / 100f;
			float num2 = Game.map_clamp(val, -50f, 50f, 0.5f, 1.5f);
			num3 = 1f + Game.map_clamp(val, 0f, 50f, 0f, 5f);
			sb_pop_inf_dbg?.AppendLine($"  From raw stability: {num3}");
			num3 += num2 * num;
			if (num3 < 0f)
			{
				num3 = 0f;
			}
			sb_pop_inf_dbg?.AppendLine($"  From influence ({num}) * stability multiplier({num2}) = {num3}");
		}
		else
		{
			if (k.IsEnemy(kingdom))
			{
				sb_pop_inf_dbg?.AppendLine("  At war with " + kingdom.Name);
				return 0f;
			}
			float influenceIn = k.GetInfluenceIn(kingdom);
			num3 = influenceIn / 10f;
			sb_pop_inf_dbg?.AppendLine($"  Influence ({influenceIn}): {num3}");
		}
		if (num3 > 0f)
		{
			float num4 = (float)land_neighbors * def.pop_majority_land_realms_mul + (float)sea_neighbors * def.pop_majority_sea_realms_mul;
			if (num4 > def.pop_majority_max_realms_mul)
			{
				num4 = def.pop_majority_max_realms_mul;
			}
			num3 *= num4;
			sb_pop_inf_dbg?.AppendLine($"  x {num4} neighbors: {num3}");
			float num5 = game.cultures.Dist(pop_majority.kingdom?.culture, k.culture);
			if (num5 > 0f)
			{
				num3 /= 1f + num5;
				sb_pop_inf_dbg?.AppendLine($"  / (1 + {num5}) culture difference: {num3}");
			}
			float num6 = religion.DistTo(k.religion);
			if (num6 > 0f)
			{
				num6 *= (num3 /= 1f + num6);
				sb_pop_inf_dbg?.AppendLine($"  / (1 + {num6}) religion difference: {num3}");
			}
			for (int i = 0; i < k.marriages.Count; i++)
			{
				Marriage marriage = k.marriages[i];
				if (marriage.kingdom_husband == kingdom && marriage.kingdom_wife == k)
				{
					num3 *= def.pop_majority_marriage_mul;
					sb_pop_inf_dbg?.AppendLine($"  * {def.pop_majority_marriage_mul}) marriage: {num3}");
					break;
				}
			}
			if (k != kingdom)
			{
				float stat = k.GetStat(Stats.ks_culture);
				if (stat > 0f)
				{
					num3 *= 1f + stat / 500f;
					sb_pop_inf_dbg?.AppendLine($"  * ({1f + stat / 500f}) from culture: {num3}");
				}
			}
			if (k == kingdom)
			{
				Character cultureBolsterCleric = k.GetCultureBolsterCleric();
				if (cultureBolsterCleric != null)
				{
					float num7 = 1f + def.pop_majority_bolster_cleric_base + (float)cultureBolsterCleric.GetClassLevel() * def.pop_majority_bolster_cleric_level_mul;
					num3 *= num7;
					sb_pop_inf_dbg?.AppendLine($"  * ({num7}) from cleric bolstering: {num3}");
				}
			}
		}
		return num3;
	}

	public bool AdjustPopMajority(float strength, Kingdom kingdom)
	{
		if (strength == 0f || (strength > 0f && pop_majority.strength >= 100f))
		{
			return false;
		}
		pop_majority.strength += strength;
		bool num = pop_majority.strength < game.cultures.def.pop_majority_convert_threshold;
		if (num)
		{
			Vars vars = new Vars(kingdom);
			vars.Set("converting_cleric", kingdom?.GetCultureBolsterCleric());
			FireEvent("PopMajorityKingdomChanged", vars);
			pop_majority.kingdom = kingdom;
			pop_majority.strength = 100f - pop_majority.strength;
		}
		if (pop_majority.strength >= 100f)
		{
			pop_majority.strength = 100f;
			if (culture != pop_majority.kingdom.culture)
			{
				SetCulture(pop_majority.kingdom.culture);
			}
		}
		SendState<PopMajorityState>();
		NotifyListeners("pop_majority_changed");
		return num;
	}

	public void SetCulture(string culture, bool send_state = true)
	{
		Vars vars = new Vars(pop_majority.kingdom);
		vars.Set("converting_cleric", pop_majority.kingdom?.GetCultureBolsterCleric());
		FireEvent("PopMajorityCultureChanged", vars);
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			if (kingdom.id == pop_majority.kingdom.id)
			{
				OnStatusChangedAnalytics(pop_majority.kingdom, "loyal");
			}
			else
			{
				OnStatusChangedAnalytics(kingdom, "disloyal");
			}
		}
	}

	public float CalcPopInf(StringBuilder sb_pop_inf_dbg)
	{
		float def_strength;
		float att_strength;
		Kingdom att_kingdom;
		return CalcPopInf(out def_strength, out att_strength, out att_kingdom, sb_pop_inf_dbg);
	}

	public float CalcPopInf(out float def_strength, out float att_strength, out Kingdom att_kingdom, StringBuilder sb_pop_inf_dbg = null)
	{
		s_pop_inf.Clear();
		AddPopInfRealm(GetKingdom(), land: true);
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			Realm neighbor = logicNeighborsRestricted[i];
			AddPopInfRealm(neighbor);
		}
		def_strength = 0f;
		att_strength = 0f;
		att_kingdom = null;
		for (int j = 0; j < s_pop_inf.Count; j++)
		{
			PopInfluence popInfluence = s_pop_inf[j];
			sb_pop_inf_dbg?.AppendLine($"{popInfluence.kingdom.Name} (provinces: {popInfluence.land_neighbors} land, {popInfluence.sea_neighbors} sea):");
			float num = CalcPopInfStrength(popInfluence.kingdom, popInfluence.land_neighbors, popInfluence.sea_neighbors, sb_pop_inf_dbg);
			if (popInfluence.kingdom == pop_majority.kingdom)
			{
				if (!(num <= def_strength))
				{
					def_strength = num;
				}
			}
			else if (!(num <= att_strength))
			{
				att_strength = num;
				att_kingdom = popInfluence.kingdom;
			}
		}
		sb_pop_inf_dbg?.AppendLine($"Defender: {pop_majority.kingdom.Name}: {def_strength}");
		if (att_kingdom != null)
		{
			sb_pop_inf_dbg?.AppendLine($"Attacker: {att_kingdom.Name}: {att_strength}");
		}
		float num2 = def_strength - att_strength;
		if (num2 < 0f && num2 > 0f - game.cultures.def.pop_majority_min_attack_strength)
		{
			num2 = 0f;
		}
		return num2;
	}

	public bool SpreadHeresy(float chance, Object param = null)
	{
		if (religion.def.pagan)
		{
			return false;
		}
		if (chance <= 0f)
		{
			return false;
		}
		if (chance >= 100f || game.Random(0f, 100f) < chance)
		{
			FireEvent("HeresySpread", param);
			SetReligion(game.religions.pagan);
			return true;
		}
		return false;
	}

	private bool SpreadHeresyFromNearbyRealms()
	{
		if (religion.def.pagan)
		{
			return false;
		}
		float num = 0f;
		Kingdom kingdom = null;
		float num2 = 1f;
		float num3 = 0.5f;
		float num4 = 0.5f;
		DT.Field field = game.dt.Find("Heresy");
		if (field != null)
		{
			num2 = field.GetFloat("land_realm_mul", null, num2);
			num3 = field.GetFloat("sea_realm_mul", null, num3);
			num4 = field.GetFloat("spread_chance_mul", null, num4);
		}
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			Realm realm = logicNeighborsRestricted[i];
			if (realm.religion.def.pagan)
			{
				num += (neighbors.Contains(realm) ? num2 : num3);
				if (kingdom == null || game.Random(0, 100) < 5)
				{
					kingdom = realm.GetKingdom();
				}
			}
		}
		if (num <= 0f)
		{
			return false;
		}
		float stat = GetKingdom().GetStat(Stats.ks_heresy_susceptibility_perc);
		float num5 = 1f + stat * 0.01f;
		float chance = num * num4 * num5;
		return SpreadHeresy(chance, kingdom);
	}

	public void UpdatePopMajority()
	{
		if (IsAuthority() && religion != null && !SpreadHeresyFromNearbyRealms() && !IsDisorder())
		{
			float def_strength;
			float att_strength;
			Kingdom att_kingdom;
			float strength = CalcPopInf(out def_strength, out att_strength, out att_kingdom);
			if (AdjustPopMajority(strength, att_kingdom))
			{
				rebellionRisk.Recalc();
			}
		}
	}

	public bool PopulationNeedsConvert(Kingdom k = null)
	{
		if (k == null)
		{
			k = GetKingdom();
			if (k == null)
			{
				return false;
			}
		}
		if (pop_majority.kingdom == k)
		{
			return pop_majority.strength < 100f;
		}
		return true;
	}

	public void SetDisorder(bool value, bool send_state = true)
	{
		if (value == disorder_state)
		{
			return;
		}
		InvalidateIncomes();
		if (!(!IsAuthority() && send_state) && !(controller is Rebellion && value))
		{
			if (value)
			{
				castle.governor?.StopGoverning(reloadLabel: false, send_state);
			}
			disorder_state = value;
			Kingdom kingdom = GetKingdom();
			if (send_state)
			{
				SendState<DisorderState>();
				OnStatusChangedAnalytics(kingdom, value ? "disorder" : "end_disorder");
			}
			RefreshTags();
			kingdom.RecalcBuildingStates();
			NotifyListeners("disorder_state_changed");
			castle?.NotifyListeners("disorder_changed");
		}
	}

	public void LiftDisorder()
	{
		SetDisorder(value: false);
	}

	public bool IsDisorder()
	{
		return disorder_state;
	}

	private void LoadFromDef()
	{
		time_until_core = def.GetFloat("time_until_core");
		core_timeout = def.GetFloat("core_timeout");
		DT.Field field = def.FindChild("gold_from_trade_center");
		DT.Field field2 = field.FindChild("distance_tresholds");
		DT.Field field3 = field.FindChild("gold_amounts");
		for (int i = 0; i < field2.NumValues(); i++)
		{
			tcGoldDistanceTresholds.Add(field2.Value(i));
			tcGoldAmounts.Add(field3.Value(i));
		}
		on_siege_convert_timeout = def.GetFloat("on_siege_convert_timeout");
	}

	private void LoadFromCSV()
	{
		if (csv_field == null)
		{
			return;
		}
		LoadOwner();
		string text = csv_field.GetString("Culture");
		if (!string.IsNullOrEmpty(text))
		{
			if (!game.cultures.IsValid(text))
			{
				Game.Log(csv_field.Path(include_file: true) + ": Invalid culture '" + text + "' for ream '" + name + "'", Game.LogType.Error);
			}
			else
			{
				culture = text;
			}
		}
		town_name = csv_field.GetString("TownName");
		fame = csv_field.GetFloat("Fame");
		LoadCoreOf();
		LoadHistoricalOf();
		LoadUnitsSet();
	}

	private void LoadOwner()
	{
		string text = csv_field.GetString("Owner");
		if (string.IsNullOrEmpty(text))
		{
			if (init_kingdom_id != 0)
			{
				return;
			}
			text = name;
		}
		Kingdom kingdom = game.GetKingdom(text);
		if (kingdom == null)
		{
			Error("invalid initial kingdom: '" + text + "'");
			kingdom = game.GetKingdom(id);
		}
		kingdom.AssignRealm(this);
	}

	private void LoadCoreOf()
	{
		Game.ProcessStringList(csv_field.GetString("CoreOf"), delegate(string name)
		{
			Kingdom kingdom = game.GetKingdom(name);
			if (kingdom == null)
			{
				Game.Log(csv_field.Path(include_file: true) + ".CoreOf: Invalid kingdom '" + name + "'", Game.LogType.Error);
			}
			else
			{
				kingdom.coreRealms.Add(this);
				coreToKingdoms.Add(new RealmCoreData(kingdom.id, is_core: true, Time.Zero));
			}
		});
	}

	private void LoadHistoricalOf()
	{
		Game.ProcessStringList(csv_field.GetString("HistoricalOf"), delegate(string name)
		{
			Kingdom kingdom = game.GetKingdom(name);
			if (kingdom == null)
			{
				Game.Log(csv_field.Path(include_file: true) + ".HistoricalOf: Invalid kingdom '" + name + "'", Game.LogType.Error);
			}
			else
			{
				kingdom.historicalRealms.Add(this);
				historicalToKingdoms.Add(kingdom);
			}
		});
	}

	private void LoadUnitsSet()
	{
		Game.AddStringsToList(csv_field.GetString("LocalUnits"), unit_types);
		game.ValidateDefIDs(unit_types, "Unit", csv_field, "LocalUnits");
	}

	public void Load()
	{
		LoadFromDef();
		LoadFromCSV();
		if (!IsSeaRealm() && init_kingdom_id == 0)
		{
			if (csv_field != null)
			{
				Error("no initial kingdom");
			}
			game.GetKingdom(id).AssignRealm(this);
		}
	}

	public int GetLevel()
	{
		if (castle == null)
		{
			return 0;
		}
		return castle.level;
	}

	public override void OnDefsReloaded()
	{
		stats?.OnDefsReloaded();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "realm":
			return this;
		case "kingdom":
			return GetKingdom();
		case "castle":
			return castle;
		case "governor":
			return castle?.governor;
		case "town_name":
			return GetTownNameKey();
		case "province_name":
			return GetProvinceNameKey();
		case "province_name_str":
			return name;
		case "towns_count_realm_and_own_neighbors":
			return GetSettlementCount("Town", "neighbors");
		case "settlements_count":
			return GetSettlementCount("All", "realm");
		case "villages_count":
			return GetSettlementCount("Village", "realm");
		case "villages_count_realm_and_own_neighbors":
			return GetSettlementCount("Village", "neighbors");
		case "keeps_count":
			return GetSettlementCount("Keep", "realm");
		case "keeps_count_realm_and_own_neighbors":
			return GetSettlementCount("Keep", "neighbors");
		case "farms_count":
			return GetSettlementCount("Farm", "realm");
		case "sheep_farms_count":
			return GetSettlementCount("SheepFarm", "realm");
		case "horse_farms_count":
			return GetSettlementCount("HorseFarm", "realm");
		case "cattle_farms_count":
			return GetSettlementCount("CattleFarm", "realm");
		case "herbs_gardens_count":
			return GetSettlementCount("HerbsGardens", "realm");
		case "flax_fields_count":
			return GetSettlementCount("FlaxField", "realm");
		case "vineyards_count":
			return GetSettlementCount("Vineyard", "realm");
		case "logging_camps_count":
			return GetSettlementCount("LoggingCamp", "realm");
		case "mines_count":
			return GetSettlementCount("Mine", "realm");
		case "quaries_count":
			return GetSettlementCount("Quarry", "realm");
		case "monasteries_count":
			return GetSettlementCount("Monastery", "realm");
		case "mosques_count":
			return GetSettlementCount("Mosque", "realm");
		case "shrines_count":
			return GetSettlementCount("Shrine", "realm");
		case "religious_settlements_count":
			return GetSettlementCount("Monastery", "realm") + GetSettlementCount("Mosque", "realm") + GetSettlementCount("Shrine", "realm");
		case "coastal_settlements_count":
			return GetCostalSettlementsCount();
		case "num_garrison_units":
			return GetNumGarrisonUnits();
		case "direct_neighbors":
			return new Value(neighbors);
		case "neighbors":
			return new Value(logicNeighborsAll);
		case "logic_neighbors":
			return new Value(logicNeighborsRestricted);
		case "num_rebels":
			return GetNumRebels();
		case "num_population":
			return castle.population.Count(Population.Type.TOTAL, check_up_to_date: false);
		case "num_rebellious_population":
			return castle.population.Count(Population.Type.Rebel, check_up_to_date: false);
		case "worker_slots":
			return castle.population.Slots(Population.Type.Worker, check_up_to_date: false);
		case "income":
			return new Value(income);
		case "incomes":
			return new Value(incomes);
		case "upkeeps":
			return new Value(upkeeps);
		case "income_gold":
			return income[ResourceType.Gold];
		case "income_food":
			return income[ResourceType.Food];
		case "negative_piety":
			return (income[ResourceType.Piety] >= 0f) ? 0f : income[ResourceType.Piety];
		case "prev_owner":
			return GetLastOwner();
		case "majority_kingdom":
			return pop_majority.kingdom;
		case "majority_foreign_kingdom":
			return (pop_majority.kingdom == GetKingdom()) ? null : pop_majority.kingdom;
		case "majority_strength":
			return pop_majority.strength;
		case "majority_debug":
		{
			StringBuilder stringBuilder = new StringBuilder();
			CalcPopInf(stringBuilder);
			string text = stringBuilder.ToString();
			return "#" + text;
		}
		case "religion":
			return religion;
		case "religion_family":
			if (is_christian)
			{
				return "christian";
			}
			if (is_muslim)
			{
				return "muslim";
			}
			if (is_pagan)
			{
				return "pagan";
			}
			Game.Log($"Invalid religion family for {this}", Game.LogType.Error);
			return Value.Unknown;
		case "religious_difference":
			return religion != GetKingdom()?.religion;
		case "religious_power":
			return religiousPower.amount;
		case "religion_distance":
			return religion.DistTo(GetKingdom()?.religion);
		case "culture_distance":
			return game.cultures.Dist(pop_majority.kingdom?.culture, GetKingdom().culture);
		case "piety_icon":
			return GetKingdom()?.GetPietyIcon();
		case "is_catholic":
			return religion == game.religions.catholic;
		case "is_orthodox":
			return religion == game.religions.orthodox;
		case "is_christian":
			return religion?.def != null && religion.def.christian;
		case "is_sunni":
			return religion == game.religions.sunni;
		case "is_shia":
			return religion == game.religions.shia;
		case "is_muslim":
			return religion.def.muslim;
		case "is_catholic_holy_land":
			return IsCatholicHolyLand();
		case "is_shia_holy_land":
			return IsShiaHolyLand();
		case "is_sunni_holy_land":
			return IsSunniHolyLand();
		case "is_religion_important":
			if (game.religions.catholic.hq_realm == this)
			{
				return true;
			}
			if (game.religions.catholic.holy_lands_realm == this)
			{
				return true;
			}
			if (game.religions.orthodox.hq_realm == this)
			{
				return true;
			}
			if (game.religions.shia.holy_lands_realm == this)
			{
				return true;
			}
			if (game.religions.sunni.holy_lands_realms.Contains(this))
			{
				return true;
			}
			return false;
		case "is_same_k_religion":
			return religion == GetKingdom()?.religion;
		case "religious_settlement_type":
			return Religion.ReligiousSettlementType(game, religion);
		case "religious_settlement_name":
		{
			string text2 = Religion.ReligiousSettlementType(game, religion);
			if (text2 == null)
			{
				return Value.Null;
			}
			return "@{" + text2 + ".name}";
		}
		case "is_pagan":
			return religion == game.religions.pagan;
		case "income_from_trade_center":
			return GetGoldFromTradeCenter(recalcRealms: true);
		case "income_from_trade_center_influence":
			return incomeFromTradeCenterInfluence;
		case "income_from_governor_skills":
			return incomeFromGovernorSkills;
		case "num_goods":
			return goods_produced.Count;
		case "goods_produced":
			return new Value(goods_produced);
		case "num_goods_if_governor_is_trading":
			return (castle?.governor != null && castle.governor.IsTrading()) ? goods_produced.Count : 0;
		case "trade_center":
			return (tradeCenter != null) ? tradeCenter.realm : null;
		case "is_trade_center":
			return IsTradeCenter();
		case "level":
			return GetLevel();
		case "is_occupied":
			return IsOccupied();
		case "occupant":
			return controller;
		case "is_rebel_occupied":
			return IsOccupied() && controller is Rebellion;
		case "is_crusade_occupied":
			return IsOccupied() && controller is Crusade;
		case "is_disorder":
			return IsDisorder();
		case "controller":
			return controller;
		case "has_trade_port":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("TradePort"));
		case "has_church":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("Church"));
		case "has_masjid":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("Masjid"));
		case "has_cathedral":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("Cathedral"));
		case "has_great_mosque":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("GreatMosque"));
		case "has_university":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("University"));
		case "has_madrasah":
			return castle != null && castle.HasWorkingBuilding(game.defs.Find<Building.Def>("Madrasah"));
		case "has_governor":
			return castle?.governor != null;
		case "no_governor":
			if (castle?.governor != null)
			{
				return Value.Null;
			}
			return new Value("");
		case "fortification_status_perc":
			return castle.keep_effects.siege_defense_condition.Get();
		case "is_important":
			return IsImportant();
		case "ai_specialization":
			return ai_specialization.ToString();
		case "ai_specialization_idx":
			return (int)ai_specialization;
		case "rebellion_risk":
			return GetTotalRebellionRisk();
		case "rebellion_risk_taxes":
			return rebellionRisk.GetRebellionRisk("taxes");
		case "rebellion_risk_governor":
			return rebellionRisk.GetRebellionRisk("governor");
		case "rebellion_risk_army_presence":
			return rebellionRisk.GetRebellionRisk("army_presence");
		case "rebellion_risk_defeated_rebels":
			return rebellionRisk.GetRebellionRisk("defeated_rebels_local");
		case "rebellion_risk_buildings":
			return rebellionRisk.GetRebellionRisk("buildings");
		case "rebellion_risk_happiness":
			return rebellionRisk.GetRebellionRisk("happiness");
		case "rebellion_risk_disorder":
			return rebellionRisk.GetRebellionRisk("disorder");
		case "rebellion_risk_crown_authority":
			return rebellionRisk.GetRebellionRisk("crown_authority");
		case "rebellion_risk_rebellions":
			return rebellionRisk.GetRebellionRisk("rebel_leaders");
		case "rebellion_risk_wars":
			return rebellionRisk.GetRebellionRisk("wars");
		case "rebellion_risk_dead_king":
			return rebellionRisk.GetRebellionRisk("dead_king");
		case "rebellion_risk_religion":
			return rebellionRisk.GetRebellionRisk("religious_tension");
		case "rebellion_risk_hunger":
			return rebellionRisk.GetRebellionRisk("hunger");
		case "rebellion_risk_culture":
			return rebellionRisk.GetRebellionRisk("culture_tension");
		case "rebellion_risk_rebel_occupations":
			return rebellionRisk.GetRebellionRisk("rebel_occupations");
		case "rebellion_risk_rebel_leaders":
			return rebellionRisk.GetRebellionRisk("rebel_leaders");
		case "rebellion_risk_castle_sacked":
			return rebellionRisk.GetRebellionRisk("castle_sacked");
		case "rebellion_risk_disloyal_pop":
			return rebellionRisk.GetRebellionRisk("disloyal_population");
		case "rebellion_risk_traditions":
			return rebellionRisk.GetRebellionRisk("traditions");
		case "rebellion_risk_stability":
			return rebellionRisk.GetRebellionRisk("stability");
		case "rebellion_risk_cleric":
			return rebellionRisk.GetRebellionRisk("cleric");
		case "rebellion_risk_opinions":
			return rebellionRisk.GetRebellionRisk("opinions");
		case "rebellion_risk_patriarch":
			return rebellionRisk.GetRebellionRisk("patriarch_bonus");
		case "rebellion_risk_establish_order":
			return rebellionRisk.GetRebellionRisk("establish_order");
		case "rebellion_risk_kingdom":
			return GetKingdom()?.stability?.value ?? 0f;
		case "rebellion_risk5":
			return rebellionRisk.GetRebelionRisk_5() * 100f;
		case "rebellion_risk30":
			return rebellionRisk.GetRebelionRisk_30() * 100f;
		case "pop_majority_foreign":
			return pop_majority.kingdom != GetKingdom();
		case "is_sea_realm":
			return IsSeaRealm();
		case "fame":
			return GetFame();
		case "rebel_population":
			return castle.population.GetRebels();
		default:
		{
			Value var = stats.GetVar(key, vars, as_value);
			if (var != Value.Unknown)
			{
				return var;
			}
			int tag = GetTag(key);
			if (tag > 0)
			{
				return tag;
			}
			return base.GetVar(key, vars, as_value);
		}
		}
	}

	public int GetTag(string tag)
	{
		if (tag == null)
		{
			return 0;
		}
		if (tags == null)
		{
			BuildTags();
		}
		int value = 0;
		tags.TryGetValue(tag, out value);
		return value;
	}

	public bool HasTag(string tag, int min_amount = 1)
	{
		return GetTag(tag) >= min_amount;
	}

	public void RefreshTags(bool refreshKingdomTags = true)
	{
		tags = null;
		Kingdom kingdom = GetKingdom();
		if (kingdom != null && refreshKingdomTags)
		{
			kingdom.RefreshRealmTags();
		}
		NotifyListeners("refresh_tags");
	}

	public void BuildTags()
	{
		tags = new Dictionary<string, int>();
		goods_produced.Clear();
		if (religion != null)
		{
			AddTag(religion.def.name);
			if (religion.def.christian)
			{
				AddTag("Christian");
			}
			if (religion.def.muslim)
			{
				AddTag("Muslim");
			}
		}
		for (int i = 0; i < features.Count; i++)
		{
			string tag = features[i];
			AddTag(tag);
		}
		for (int j = 0; j < settlements.Count; j++)
		{
			Settlement settlement = settlements[j];
			AddTag(settlement.type);
			if (settlement.coastal && !(settlement is Castle))
			{
				AddTag("CoastalSettlement");
			}
			if (settlement.coastal && settlement is Village)
			{
				AddTag("CoastalVillage");
			}
		}
		if (castle == null)
		{
			return;
		}
		if (castle.governor != null)
		{
			AddTag("Governed");
		}
		for (int k = 0; k < castle.buildings.Count; k++)
		{
			Building building = castle.buildings[k];
			if (building == null || building.def?.id == null || !building.IsBuilt())
			{
				continue;
			}
			AddTag(building.def.id);
			if (building.def.produces != null && building.IsFullyFunctional())
			{
				int count = building.def.produces.Count;
				for (int l = 0; l < count; l++)
				{
					Building.Def.ProducedResource producedResource = building.def.produces[l];
					AddTag(producedResource.resource);
					Resource.Def def = game.defs.Get<Resource.Def>(producedResource.resource);
					if (def != null && def.Name != null)
					{
						goods_produced.Add(def);
					}
				}
			}
			if (building.def.produces_completed == null || !building.CalcCompleted())
			{
				continue;
			}
			int count2 = building.def.produces_completed.Count;
			for (int m = 0; m < count2; m++)
			{
				Building.Def.ProducedResource producedResource2 = building.def.produces_completed[m];
				AddTag(producedResource2.resource);
				Resource.Def def2 = game.defs.Get<Resource.Def>(producedResource2.resource);
				if (def2 != null && def2.Name != null)
				{
					goods_produced.Add(def2);
				}
			}
		}
		for (int n = 0; n < castle.upgrades.Count; n++)
		{
			Building building2 = castle.upgrades[n];
			if (building2 == null || building2.def?.id == null || !building2.IsBuilt())
			{
				continue;
			}
			AddTag(building2.def.id);
			if (building2.def.variant_of != null)
			{
				AddTag(building2.def.variant_of.id);
			}
			if (building2.def.produces == null || !building2.IsFullyFunctional())
			{
				continue;
			}
			int count3 = building2.def.produces.Count;
			for (int num = 0; num < count3; num++)
			{
				Building.Def.ProducedResource producedResource3 = building2.def.produces[num];
				AddTag(producedResource3.resource);
				Resource.Def def3 = game.defs.Get<Resource.Def>(producedResource3.resource);
				if (def3 != null && def3.Name != null)
				{
					goods_produced.Add(def3);
				}
			}
		}
		void AddTag(string key)
		{
			int value = 0;
			tags.TryGetValue(key, out value);
			tags[key] = value + 1;
		}
	}

	public bool FixSettlementsReligion()
	{
		if (religion == null || settlements == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < settlements.Count; i++)
		{
			if (settlements[i].FixReligion())
			{
				result = true;
			}
		}
		return result;
	}

	public bool FixPietyType()
	{
		bool result = false;
		if (castle?.buildings != null)
		{
			for (int i = 0; i < castle.buildings.Count; i++)
			{
				castle.buildings[i]?.FixPiety();
			}
		}
		if (settlements == null)
		{
			return false;
		}
		for (int j = 0; j < settlements.Count; j++)
		{
			Settlement settlement = settlements[j];
			if (!string.IsNullOrEmpty(settlement.def.piety_type))
			{
				settlement.SetLevel(settlement.level);
				result = true;
			}
		}
		return result;
	}

	private List<string> GenerateSkillsForBuilding(Building building)
	{
		List<string> list = new List<string>();
		if (!AssertAuthority())
		{
			return list;
		}
		tmp_skills_list.Clear();
		if (building.def.skills_pool != null)
		{
			tmp_skills_list.AddRange(building.def.skills_pool);
		}
		else
		{
			Defs.Registry registry = game.defs.Get(typeof(Skill.Def));
			if (registry == null)
			{
				return list;
			}
			foreach (KeyValuePair<string, Def> def2 in registry.defs)
			{
				Skill.Def item = def2.Value as Skill.Def;
				tmp_skills_list.Add(item);
			}
		}
		int num = game.Random(building.def.skills_min, building.def.skills_max + 1);
		if (num > tmp_skills_list.Count)
		{
			num = tmp_skills_list.Count;
		}
		while (num > 0)
		{
			int index = game.Random(0, tmp_skills_list.Count);
			Skill.Def def = tmp_skills_list[index];
			list.Add(def.id);
			tmp_skills_list.RemoveAt(index);
			num--;
		}
		return list;
	}

	private bool AddSkills(Building building)
	{
		if (building == null || building.def.skills_max <= 0)
		{
			return false;
		}
		bool result = false;
		if (skills_per_building_type == null)
		{
			skills_per_building_type = new Dictionary<string, List<string>>();
		}
		if (!skills_per_building_type.TryGetValue(building.def.id, out var value))
		{
			if (!IsAuthority())
			{
				return false;
			}
			result = true;
			value = GenerateSkillsForBuilding(building);
			skills_per_building_type.Add(building.def.id, value);
		}
		for (int i = 0; i < value.Count; i++)
		{
			string text = value[i];
			Skill.Def item = game.defs.Get<Skill.Def>(text);
			if (!skills.Contains(item))
			{
				skills.Add(item);
			}
		}
		return result;
	}

	private bool SkillsChanged(List<Skill.Def> old_skills, List<Skill.Def> new_skills)
	{
		if (old_skills == null || new_skills == null)
		{
			return true;
		}
		int count = new_skills.Count;
		if (count != old_skills.Count)
		{
			return true;
		}
		for (int i = 0; i < count; i++)
		{
			Skill.Def obj = old_skills[i];
			Skill.Def def = new_skills[i];
			if (obj != def)
			{
				return true;
			}
		}
		return false;
	}

	public void RefreshSkills(Building building, bool from_state = false)
	{
		if ((building != null && building.def.skills_max <= 0) || castle == null)
		{
			return;
		}
		List<Skill.Def> old_skills = skills;
		skills = tmp_skills;
		skills.Clear();
		for (int i = 0; i < castle.buildings.Count; i++)
		{
			Building building2 = castle.buildings[i];
			if (building2 != null && building2.IsBuilt())
			{
				AddSkills(building2);
			}
		}
		if (!SkillsChanged(old_skills, skills))
		{
			skills = old_skills;
			tmp_skills.Clear();
			return;
		}
		skills = new List<Skill.Def>(tmp_skills);
		tmp_skills.Clear();
		NotifyListeners("skills_changed");
		if (!from_state)
		{
			SendState<SkillsPerBuildingTypeState>();
		}
	}

	private void FixNeighborsSorting()
	{
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			logicNeighborsRestricted[i].logicNeighborsRestricted.Sort((Realm r1, Realm r2) => r2.kingdom_id.CompareTo(r1.kingdom_id));
			logicNeighborsRestricted[i].neighbors.Sort((Realm r1, Realm r2) => r2.kingdom_id.CompareTo(r1.kingdom_id));
		}
	}

	public void LoadNeighbors()
	{
		DT.Field field = def.FindChild("neighbors");
		if (field == null)
		{
			return;
		}
		if (field.children != null && field.children.Count > 0)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (field2 != null && !string.IsNullOrEmpty(field2.key))
				{
					Realm realm = game.GetRealm(field2.key);
					if (realm != null)
					{
						neighbors.Add(realm);
					}
				}
			}
		}
		neighbors.Sort((Realm r1, Realm r2) => r2.kingdom_id.CompareTo(r1.kingdom_id));
	}

	public void LoadLogicNeighbors()
	{
		if (IsSeaRealm())
		{
			return;
		}
		DT.Field field = def.FindChild("neighbors_through_sea");
		if (field == null)
		{
			return;
		}
		if (field.children != null && field.children.Count > 0)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (field2 != null && !string.IsNullOrEmpty(field2.key))
				{
					Realm realm = game.GetRealm(field2.key);
					if (realm != null)
					{
						logicNeighborsRestricted.Add(realm);
					}
				}
			}
		}
		for (int j = 0; j < neighbors.Count; j++)
		{
			if (!neighbors[j].IsSeaRealm() && !logicNeighborsRestricted.Contains(neighbors[j]))
			{
				logicNeighborsRestricted.Add(neighbors[j]);
			}
		}
		logicNeighborsRestricted.Sort((Realm r1, Realm r2) => r2.kingdom_id.CompareTo(r1.kingdom_id));
	}

	public void LoadNeighborsAndRegions()
	{
		if (def == null)
		{
			Warning("no def, probably map needs re-export");
			return;
		}
		LoadNeighbors();
		LoadLogicNeighbors();
	}

	public void SetLogicNeighborsAll()
	{
		if (IsSeaRealm())
		{
			return;
		}
		for (int i = 0; i < neighbors.Count; i++)
		{
			if (neighbors[i].IsSeaRealm())
			{
				Realm realm = neighbors[i];
				for (int j = 0; j < realm.neighbors.Count; j++)
				{
					Realm realm2 = realm.neighbors[j];
					if (!logicNeighborsAll.Contains(realm2) && realm2 != this && !realm2.IsSeaRealm())
					{
						logicNeighborsAll.Add(realm2);
					}
				}
			}
			else if (neighbors[i].id > 0 && !logicNeighborsAll.Contains(neighbors[i]))
			{
				logicNeighborsAll.Add(neighbors[i]);
			}
		}
		logicNeighborsAll.Sort((Realm r1, Realm r2) => r2.kingdom_id.CompareTo(r1.kingdom_id));
	}

	public void SetLogicNeighborsRestricted()
	{
		if (!IsSeaRealm() && logicNeighborsRestricted.Count <= 0)
		{
			_ = logicNeighborsAll.Count;
		}
	}

	public void UpdateUnitSets()
	{
		castle?.available_units?.Update();
	}

	public void CreateActions()
	{
		if (actions == null)
		{
			actions = new Actions(this);
		}
		actions.AddAll();
		NotifyListeners("actions_created");
	}

	public override void OnInit()
	{
		if (game != null && game.state != Game.State.InLobby)
		{
			stats = new Stats(this);
			religiousPower = new ReligiousPower(this);
			threat = new KingdomAI.Threat(this);
			attacker_threat = new KingdomAI.Threat(this);
			help_with_rebels_threat = new KingdomAI.Threat(this);
			rebellionRisk = (IsSeaRealm() ? null : new RebellionRisk(this));
		}
	}

	public override Stats GetStats()
	{
		return stats;
	}

	public int GetLocalAuthority()
	{
		return (int)stats.Get(Stats.rs_stability);
	}

	public void CalculateInfluence()
	{
		int localAuthority = GetLocalAuthority();
		if (localAuthority >= 0)
		{
			influence.kingdom_id = 0;
			influence.influence = 0;
			return;
		}
		int num = 0;
		float num2 = 0f;
		int num3 = 0;
		float num4 = 0f;
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			Realm realm = logicNeighborsRestricted[i];
			if (kingdom_id != realm.kingdom_id)
			{
				if (num3 != realm.kingdom_id)
				{
					num3 = realm.kingdom_id;
					num4 = realm.GetKingdom().fame / 100f;
				}
				num4 += (float)(realm.castle.level / 5);
				if (num2 < num4)
				{
					num = realm.kingdom_id;
					num2 = num4;
				}
			}
		}
		influence.kingdom_id = num;
		influence.influence = ((num != 0) ? Math.Abs(localAuthority) : 0);
	}

	public float GetAppeal()
	{
		float num = GetCommerce();
		if (appealNeighbors == null)
		{
			appealNeighbors = new List<Realm>();
		}
		else
		{
			appealNeighbors.Clear();
		}
		appealNeighbors.Add(this);
		for (int i = 0; i < neighbors.Count; i++)
		{
			Realm realm = neighbors[i];
			if (realm.castle == null)
			{
				continue;
			}
			num += realm.GetCommerce() * (float)def.GetInt("appeal_neighboring_commerse_perc") / 100f;
			for (int j = 0; j < realm.neighbors.Count; j++)
			{
				Realm realm2 = realm.neighbors[j];
				if (realm2.castle != null && !neighbors.Contains(realm2) && !appealNeighbors.Contains(realm2))
				{
					appealNeighbors.Add(realm2);
					num += realm2.GetCommerce() * (float)def.GetInt("appeal_neighboring_2_commerse_perc") / 100f;
				}
			}
		}
		if (IsTradeCenter())
		{
			num += (float)def.GetInt("appeal_trade_center");
		}
		return num + GetKingdom().GetStat(Stats.ks_commerce) * (float)def.GetInt("appeal_kingdom_commerse_perc") / 100f;
	}

	public Realm NearestNeghborOfKingdom(Kingdom kingdom)
	{
		if (castle == null)
		{
			return null;
		}
		Realm result = null;
		float num = float.MaxValue;
		for (int i = 0; i < neighbors.Count; i++)
		{
			Realm realm = neighbors[i];
			if (realm.castle != null && realm.GetKingdom() == kingdom)
			{
				float num2 = realm.castle.position.SqrDist(castle.position);
				if (!(num2 >= num))
				{
					result = realm;
					num = num2;
				}
			}
		}
		return result;
	}

	public List<Character> GetFamousPeople()
	{
		List<Character> list = new List<Character>();
		for (int i = 0; i < settlements.Count; i++)
		{
			if (settlements[i] is Village { famous_person: not null } village)
			{
				list.Add(village.famous_person);
			}
		}
		return list;
	}

	public void AddSettlement(Settlement s)
	{
		if (s is Castle castle)
		{
			if (this.castle != null)
			{
				Warning("Realm has more than 1 castle");
			}
			this.castle = castle;
			settlements.Insert(0, s);
		}
		else
		{
			settlements.Add(s);
		}
	}

	public void AddPotentialRebellion(Rebellion r)
	{
		if (r != null && !potentialRebellions.Contains(r))
		{
			potentialRebellions.Add(r);
			controller?.GetKingdom()?.AddPotentialRebellion(r);
		}
	}

	public void DelPotentialRebellion(Rebellion r)
	{
		if (r != null && potentialRebellions.Remove(r))
		{
			controller?.GetKingdom()?.DelPotentialRebellion(r);
		}
	}

	public void AddRebellion(Rebellion r)
	{
		if (r != null && !rebellions.Contains(r))
		{
			rebellions.Add(r);
			GetKingdom()?.AddRebellion(r);
		}
	}

	public void DelRebellion(Rebellion r)
	{
		if (r != null && rebellions.Remove(r))
		{
			GetKingdom()?.DelRebellion(r);
		}
	}

	public void AddArmy(Army a)
	{
		armies.Add(a);
		AddMercenary(a);
		if (a.rebel != null)
		{
			rebellionRisk?.Recalc(think_rebel_pop: false, allow_rebel_spawn: false);
		}
	}

	public void AddMercenary(Army a)
	{
		if (a.IsMercenary() && !mercenaries.Contains(a))
		{
			mercenaries.Add(a);
		}
	}

	public void DelMercenary(Army a)
	{
		if (a.IsMercenary())
		{
			mercenaries.Remove(a);
		}
	}

	public void DelArmy(Army a)
	{
		armies.Remove(a);
		DelMercenary(a);
		if (a.rebel != null && game != null && !game.IsUnloadingMap())
		{
			rebellionRisk?.Recalc(think_rebel_pop: false, allow_rebel_spawn: false);
		}
	}

	public void AddPreviousOwner(Kingdom k)
	{
		if (!IsAuthority() || k == null)
		{
			return;
		}
		for (int i = 0; i < prev_owners.Count; i++)
		{
			if (prev_owners[i].Item1 == k.id)
			{
				prev_owners.RemoveAt(i);
				break;
			}
		}
		prev_owners.Add(new Tuple<int, Time>(k.id, game.time));
		if ((float)prev_owners.Count > def.GetFloat("prev_owners_count", null, 5f))
		{
			prev_owners.RemoveAt(0);
		}
		SendState<PrevOwnersState>();
	}

	public Kingdom GetLastOwner()
	{
		if (prev_owners.Count == 0)
		{
			return null;
		}
		return game.GetKingdom(prev_owners[prev_owners.Count - 1].Item1);
	}

	public bool IsPrevoiuslyOwnedBy(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		for (int i = 0; i < prev_owners.Count; i++)
		{
			if (prev_owners[i].Item1 == k.id)
			{
				return true;
			}
		}
		return false;
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public bool IsBorder()
	{
		return isBorder;
	}

	public bool IsSeaRealm()
	{
		return id < 0;
	}

	public bool IsImportant()
	{
		return fame != 0f;
	}

	public bool UpdateIsCoreFor(Kingdom k)
	{
		RealmCoreData realmCoreData = coreToKingdoms.Find((RealmCoreData d) => d.kingdom_id == k.id);
		if (realmCoreData == null)
		{
			return false;
		}
		if (k.id == kingdom_id)
		{
			if (IsAuthority() && game.time - realmCoreData.last_captured >= time_until_core)
			{
				if (!k.coreRealmsRaw.Contains(this))
				{
					k.coreRealmsRaw.Add(this);
				}
				realmCoreData.is_core = true;
				SendState<CoreToKingdomsState>();
				return true;
			}
			return realmCoreData.is_core;
		}
		if (IsAuthority() && game.time - realmCoreData.last_captured >= core_timeout)
		{
			k.coreRealmsRaw.Remove(this);
			coreToKingdoms.Remove(realmCoreData);
			SendState<CoreToKingdomsState>();
			return false;
		}
		return realmCoreData.is_core;
	}

	public bool IsCoreFor(Kingdom k)
	{
		return UpdateIsCoreFor(k);
	}

	public bool IsCore()
	{
		return IsCoreFor(GetKingdom());
	}

	public bool IsHistoricalFor(Kingdom k)
	{
		return historicalToKingdoms.Contains(k);
	}

	public bool IsHistorical()
	{
		return IsHistoricalFor(GetKingdom());
	}

	public bool IsTradeCenter()
	{
		if (tradeCenter != null)
		{
			return tradeCenterDistance == 0;
		}
		return false;
	}

	public bool IsInfluencedByTradeCenter()
	{
		if (tradeCenter != null)
		{
			return tradeCenterDistance != 0;
		}
		return false;
	}

	public void RecheckIfNeighborsAreBorders()
	{
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			logicNeighborsRestricted[i].RecheckIfBorder();
		}
	}

	public bool RecheckIfBorder()
	{
		Kingdom kingdom = GetKingdom();
		kingdom.RemoveExternalBorderRealm(this);
		isBorder = false;
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			if (kingdom_id != logicNeighborsRestricted[i].kingdom_id)
			{
				isBorder = true;
				kingdom.AddExternalBorderRealm(logicNeighborsRestricted[i]);
			}
		}
		return isBorder;
	}

	private void DeactivateBuildings(Castle castle, List<Building> buildings, bool temporary)
	{
		if (buildings == null)
		{
			return;
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			Building building = buildings[i];
			if (building != null && (castle == null || !castle.burned_buildings.Contains(building)) && building.IsWorking())
			{
				building.SetState(temporary ? Building.State.TemporaryDeactivated : Building.State.Invalid);
			}
		}
	}

	public void DeactivateBuildings(bool temporary)
	{
		if (castle != null)
		{
			DeactivateBuildings(castle, castle.buildings, temporary);
			DeactivateBuildings(castle, castle.upgrades, temporary: true);
		}
	}

	public void SetKingdom(int kid, bool ignore_victory = false, bool check_cancel_battle = true, bool via_diplomacy = false, bool send_state = true, bool from_gameplay = true)
	{
		if (kingdom_id == kid)
		{
			return;
		}
		Kingdom kOld = GetKingdom();
		Kingdom kNew = game.GetKingdom(kid);
		using (new Kingdom.CacheRBS("Realm.SetKingdom"))
		{
			castle?.DelResourcesInfoFromKingdom();
			DeactivateBuildings(temporary: true);
			if (kOld != null && kOld.type == Kingdom.Type.Regular)
			{
				AddPreviousOwner(kOld);
			}
			if (castle != null && castle.governor != null)
			{
				if (!IsAuthority())
				{
					Game.Log($"{this}: Trying to change kingdom on a client with governor ({castle.governor}) still active! Please report this accident!", Game.LogType.Error);
				}
				castle.governor.StopGoverning(reloadLabel: false);
			}
			List<Rebellion> list = new List<Rebellion>(potentialRebellions);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				list[num].ClearZone();
			}
			if (kOld != null)
			{
				kOld.DelRealm(this, kid, ignore_victory: true);
			}
			bool flag = kOld.IsEnemy(kNew);
			for (int i = 0; i < settlements.Count; i++)
			{
				Settlement settlement = settlements[i];
				if (check_cancel_battle && settlement.battle != null && !settlement.battle.IsFinishing())
				{
					settlement.battle.Cancel(Battle.VictoryReason.RealmChange);
				}
				settlement.SetKingdom(kid);
			}
			kingdom_id = kid;
			for (int j = 0; j < settlements.Count; j++)
			{
				Settlement settlement2 = settlements[j];
				if (settlement2.type == "Keep")
				{
					bool flag2 = !send_state;
					if (send_state && flag && !settlement2.keep_effects.SetOccupied(kNew))
					{
						flag2 = true;
					}
					if (flag2)
					{
						settlement2.NotifyListeners("controlling_obj_changed");
					}
				}
			}
			castle?.AddResourcesInfoToKingdom();
			SetDisorder(!via_diplomacy && pop_majority.kingdom != kNew);
			SetOccupied(kNew, force: false, send_state: false);
			ClearRecentOccupators();
			if (kNew != null)
			{
				kNew.AddRealm(this, ignore_victory: true);
			}
			DelEstablishOrderMod();
			if (from_gameplay)
			{
				rebellionRisk?.OnKingdomChanged();
			}
			kOld?.NotifyListeners("realm_deleted", this);
			RecheckIfBorder();
			RecheckIfNeighborsAreBorders();
			FixNeighborsSorting();
			InvalidateIncomes();
			if (castle != null)
			{
				castle.available_units = new AvailableUnits(castle);
			}
			if (kOld != null)
			{
				kNew.RecalcKingdomDistances();
				kOld.RecalculateNeighbors();
			}
			if (kNew != null)
			{
				kNew.RecalcKingdomDistances();
				kNew.RecalculateNeighbors();
			}
			if (castle != null && castle.army != null && castle.army.kingdom_id != kNew.id && castle.battle == null)
			{
				castle.army.LeaveCastle(castle.GetRandomExitPoint());
			}
			if (send_state && kOld != null)
			{
				SendState<KingdomState>();
			}
			RealmCoreData realmCoreData = coreToKingdoms.Find((RealmCoreData d) => d.kingdom_id == kOld.id);
			RealmCoreData realmCoreData2 = coreToKingdoms.Find((RealmCoreData d) => d.kingdom_id == kNew.id);
			if (realmCoreData != null)
			{
				realmCoreData.last_captured = game.time;
			}
			if (realmCoreData2 != null)
			{
				realmCoreData2.last_captured = game.time;
			}
			else
			{
				coreToKingdoms.Add(new RealmCoreData(kNew.id, is_core: false, game.time));
				kNew.coreRealmsRaw.Add(this);
			}
			if (IsTradeCenter())
			{
				NotifyListeners("trade_centre_lost", kOld);
			}
			FixPietyType();
			UpdateUnitSets();
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				list[num2].RefreshZoneRealms();
			}
			if (base.started)
			{
				kOld?.RecalcBuildingStates();
				kNew?.RecalcBuildingStates();
			}
			for (int num3 = 0; num3 < armies.Count; num3++)
			{
				Army army = armies[num3];
				army.morale.RecalcPermanentMorale(force_send: false, recalc_dist: true);
				army.ClearBorderCrossRelCache();
				army.leader?.NotifyListeners("location_kingdom_changed");
			}
			NotifyListeners("kingdom_changed", kOld);
			if (from_gameplay)
			{
				OnStatusChangedAnalytics(kNew, "won");
				OnStatusChangedAnalytics(kOld, "lost");
				kNew?.ai?.ConsiderQuests();
			}
		}
		game.ValidateEndGame(kNew, kOld.IsDefeated() ? kOld : null);
	}

	public void DelEstablishOrderMod()
	{
		if (stats == null)
		{
			return;
		}
		Stat stat = stats.Find("rs_stability_establish_order");
		List<Stat.Factor> factors = stat.GetFactors();
		if (factors == null)
		{
			return;
		}
		for (int num = factors.Count - 1; num >= 0; num--)
		{
			Stat.Factor factor = factors[num];
			if (factor.mod?.GetField()?.key == "EstablishOrderModifier")
			{
				stat.DelModifier(factor.mod);
				break;
			}
		}
	}

	public override IRelationCheck GetStanceObj()
	{
		return controller;
	}

	public Kingdom GetControllingKingdom()
	{
		return controller?.GetKingdom();
	}

	public bool IsOccupied()
	{
		return GetKingdom() != controller;
	}

	public bool IsReligionHQ()
	{
		if (game?.religions == null)
		{
			return false;
		}
		if (game.religions.catholic.hq_realm == this)
		{
			return true;
		}
		if (game.religions.orthodox.hq_realm == this)
		{
			return true;
		}
		if (game.religions.sunni.hq_realm == this)
		{
			return true;
		}
		if (game.religions.shia.hq_realm == this)
		{
			return true;
		}
		if (game.religions.pagan.hq_realm == this)
		{
			return true;
		}
		return false;
	}

	public bool IsCatholicHolyLand()
	{
		return game.religions.catholic.holy_lands_realm.id == id;
	}

	public bool IsShiaHolyLand()
	{
		return game.religions.shia.holy_lands_realm.id == id;
	}

	public bool IsSunniHolyLand()
	{
		return game.religions.sunni.holy_lands_realms.Contains(this);
	}

	public bool IsMuslimHolyLand()
	{
		if (!IsShiaHolyLand())
		{
			return IsSunniHolyLand();
		}
		return true;
	}

	private bool CheckMissionKingdom(Character c)
	{
		if (c == null)
		{
			return false;
		}
		Action action = null;
		Realm realm = c.mission_realm;
		if (realm == null)
		{
			action = c.cur_action;
			if (action != null)
			{
				realm = (action.target as Settlement)?.GetRealm();
				if (realm == null)
				{
					realm = action.target as Realm;
				}
				if (realm == null && action.args != null)
				{
					for (int i = 0; i < action.args.Count; i++)
					{
						realm = (action.args[i].obj_val as Settlement)?.GetRealm();
						if (realm == null)
						{
							realm = action.args[i].obj_val as Realm;
						}
						if (realm != null)
						{
							break;
						}
					}
				}
			}
		}
		if (realm != this)
		{
			return false;
		}
		return true;
	}

	private void HandleCharactersOnOccupation(Kingdom kold, bool send_state = true)
	{
		for (int i = 0; i < kold.court.Count; i++)
		{
			Character character = kold.court[i];
			if (character == null)
			{
				continue;
			}
			if (CheckMissionKingdom(character))
			{
				Kingdom kingdom = controller.GetKingdom();
				if (kingdom.type == Kingdom.Type.Regular && (float)game.Random(0, 100) < def.GetFloat("chance_imprison_on_occupation"))
				{
					character.Imprison(kingdom, recall: true, send_state, "realm_occupied");
					character.NotifyListeners("character_imprisoned_occupation", this);
				}
				else if (character.IsCleric())
				{
					character.cur_action.Cancel();
					character.NotifyListeners("cleric_action_interupted_occupation", this);
				}
				else if (character.GetArmy() == null)
				{
					if (IsAuthority())
					{
						character.Recall();
					}
					character.NotifyListeners("character_recalled_occupation", this);
				}
				else
				{
					character.cur_action?.Cancel();
				}
			}
			if (character.masters == null)
			{
				continue;
			}
			for (int j = 0; j < character.masters.Count; j++)
			{
				if (CheckMissionKingdom(character.masters[j]))
				{
					character.masters[j].cur_action?.Cancel();
					Vars vars = new Vars(character.masters[j]);
					vars.Set("realm", this);
					vars.Set("puppet", character);
					character.masters[j].NotifyListeners("spy_action_interupted_occupation", vars);
				}
			}
		}
		if (!IsAuthority())
		{
			return;
		}
		for (int k = 0; k < merchants.Count; k++)
		{
			Character character2 = merchants[k];
			if (character2 != null && character2.IsMerchant() && character2.mission_realm == this && character2.IsEnemy(this))
			{
				character2.Recall();
				k--;
			}
		}
	}

	private void RestoreGovernor()
	{
		if (!IsAuthority())
		{
			return;
		}
		Kingdom kingdom = GetKingdom();
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.governed_castle_banished_from == castle)
			{
				character.UnBanish();
			}
		}
	}

	public void SaveRecentOccupator(Object obj)
	{
		if (IsAuthority() && obj is Kingdom kingdom)
		{
			recentOccupators[kingdom.id] = game.time;
			SendState<RecentOccupatorsState>();
		}
	}

	public void DropRelationsRecentOccupator(Kingdom k)
	{
		Kingdom kingdom = GetKingdom();
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = game.kingdoms[i];
			if (kingdom2 != k && kingdom2 != kingdom && !kingdom2.IsDefeated())
			{
				k.AddRelationModifier(kingdom2, "rel_realm_recent_occupator_with_everyone_else", null);
			}
		}
		k.AddRelationModifier(kingdom, "rel_realm_recent_occupator_with_owner", null);
	}

	public void CheckRecentOccupator(Object obj)
	{
		if (IsAuthority() && obj is Kingdom kingdom && recentOccupators.TryGetValue(kingdom.id, out var value) && value + def.GetFloat("recent_occupator_timeout") >= game.time)
		{
			DropRelationsRecentOccupator(kingdom);
		}
	}

	public void ClearRecentOccupators()
	{
		if (IsAuthority())
		{
			recentOccupators.Clear();
			SendState<RecentOccupatorsState>();
		}
	}

	public void SetOccupied(Object obj, bool force = false, bool send_state = true)
	{
		if (obj == null || (castle?.battle != null && !castle.battle.IsFinishing()))
		{
			return;
		}
		Kingdom kingdom = GetKingdom();
		Kingdom kingdom2 = controller.GetKingdom();
		Object obj2 = controller;
		for (int i = 0; i < potentialRebellions.Count; i++)
		{
			potentialRebellions[i].DelAffectedKingdom(kingdom2);
		}
		if (controller is Rebellion rebellion)
		{
			rebellion.DelOccupiedRealm(this);
		}
		controller.GetKingdom().DelOcuppiedRealm(this, send_state);
		if (!force && !kingdom.IsEnemy(obj))
		{
			SaveRecentOccupator(controller);
			controller = kingdom;
			RestoreGovernor();
		}
		else
		{
			bool num = kingdom.IsDominated();
			CheckRecentOccupator(obj);
			controller = obj;
			controller.GetKingdom()?.AddOcuppiedRealm(this, send_state);
			if (obj is Kingdom)
			{
				Error($"{obj} occupying realms shouldn't be possible, only rebellions can");
			}
			if (castle?.governor != null)
			{
				Character governor = castle.governor;
				governor.BanishFrom(castle);
				NotifyListeners("banished_governor", governor);
			}
			if (!num && kingdom.IsDominated())
			{
				kingdom.NotifyListeners("kingdom_dominated");
			}
			HandleCharactersOnOccupation(kingdom2, send_state);
		}
		if (IsAuthority() && send_state)
		{
			for (int j = 0; j < settlements.Count; j++)
			{
				Settlement settlement = settlements[j];
				if (settlement.battle != null && !settlement.battle.IsFinishing())
				{
					settlement.battle.Cancel(Battle.VictoryReason.RealmChange);
				}
			}
		}
		for (int k = 0; k < settlements.Count; k++)
		{
			Settlement settlement2 = settlements[k];
			if (settlement2.keep_effects != null && settlement2.keep_effects.CanBeTakenOver() && (settlement2.keep_effects.GetController() == obj2 || !settlement2.keep_effects.GetController().IsEnemy(controller)))
			{
				settlements[k].keep_effects.SetOccupied(controller, force, send_state);
			}
		}
		Army army = castle?.army;
		if (army != null && !army.IsOwnStance(controller) && IsAuthority())
		{
			army.TakeAndClearGarrison();
			if (send_state)
			{
				army.LeaveCastle(castle.GetRandomExitPoint(), send_state);
			}
		}
		kingdom.RefreshRealmTags();
		kingdom.RecalcBuildingStates();
		for (int l = 0; l < potentialRebellions.Count; l++)
		{
			potentialRebellions[l].RefreshZoneRealms();
		}
		if (controller is Rebellion rebellion2)
		{
			rebellion2.AddOccupiedRealm(this);
			SetDisorder(value: false);
		}
		NotifyListeners("controlling_obj_changed");
		if (send_state)
		{
			bool flag = IsOccupied();
			OnStatusChangedAnalytics(kingdom, flag ? "occupied" : "end_occupation");
			if (kingdom2 != kingdom)
			{
				OnStatusChangedAnalytics(kingdom2, flag ? "occupied" : "end_occupation");
			}
			SendState<ControllerState>();
		}
	}

	public Kingdom GetIndependenceKingdom(Religion religion = null, List<Kingdom> excludedKingdoms = null)
	{
		Kingdom kingdom = GetIndependenceNewKingdom();
		if (kingdom == null)
		{
			kingdom = GetIndependenceExistingKingdom(religion, excludedKingdoms);
		}
		return kingdom;
	}

	public bool ValidateIndependenceNewKingdom(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (!k.IsDefeated())
		{
			return false;
		}
		if (k == game.religions.catholic.hq_kingdom)
		{
			return false;
		}
		if (game.kingdoms.Find((Kingdom kingdom) => !kingdom.IsDefeated() && k.Name == kingdom.Name) != null)
		{
			return false;
		}
		return true;
	}

	public Kingdom GetIndependenceNewKingdom()
	{
		Kingdom kingdom = game.GetKingdom(init_kingdom_id);
		Kingdom kingdom2 = pop_majority.kingdom;
		Kingdom kingdom3 = game.GetKingdom(id);
		if (!ValidateIndependenceNewKingdom(kingdom))
		{
			kingdom = null;
		}
		if (!ValidateIndependenceNewKingdom(kingdom2))
		{
			kingdom2 = null;
		}
		if (!ValidateIndependenceNewKingdom(kingdom3))
		{
			kingdom3 = null;
		}
		if (kingdom != null && kingdom2 != null)
		{
			if (game.Random(0, 100) < 50)
			{
				return kingdom;
			}
			return kingdom2;
		}
		if (kingdom != null)
		{
			return kingdom;
		}
		if (kingdom2 != null)
		{
			return kingdom2;
		}
		if (kingdom3 != null)
		{
			return kingdom3;
		}
		return null;
	}

	public Kingdom GetIndependenceExistingKingdom(Religion religion = null, List<Kingdom> excludedKingdoms = null)
	{
		Kingdom kingdom = GetKingdom();
		for (int i = 0; i < logicNeighborsRestricted.Count; i++)
		{
			Kingdom kingdom2 = logicNeighborsRestricted[i].GetKingdom();
			if (kingdom2 != kingdom && (excludedKingdoms == null || !excludedKingdoms.Contains(kingdom2)) && (religion == null || kingdom2.religion == religion))
			{
				return kingdom2;
			}
		}
		return null;
	}

	public Kingdom TryBecomeIndependent()
	{
		return game.TryDeclareIndependence(new List<Realm> { this });
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return GetTownNameKey();
	}

	public string GetTownNameKey()
	{
		if (!string.IsNullOrEmpty(custom_town_name))
		{
			return "#" + custom_town_name;
		}
		if (!string.IsNullOrEmpty(town_name))
		{
			return "tn_" + town_name;
		}
		return "tn_" + name;
	}

	public void ChangeTownName(string newName, bool send_state = true)
	{
		if (!IsAuthority())
		{
			SendEvent(new ChangeTownNameEvent(newName));
			return;
		}
		custom_town_name = newName;
		if (castle != null)
		{
			castle.customName = newName;
			castle.NotifyListeners("name_changed");
		}
		NotifyListeners("town_name_changed");
		if (send_state)
		{
			SendState<TownNameState>();
		}
	}

	public string GetProvinceNameKey()
	{
		return "tn_" + name;
	}

	public int CalcVisibleBy(Kingdom k, bool check_neighbors = true)
	{
		if (!game.fow)
		{
			return 2;
		}
		if (k == null)
		{
			return 0;
		}
		if (kingdom_id == k.id)
		{
			return 2;
		}
		if (controller != null && controller.GetKingdom() == k)
		{
			return 2;
		}
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			if (army.IsValid())
			{
				if (army.IsOwnStance(k))
				{
					return 2;
				}
				if (army.mercenary != null && army.mercenary.former_owner_id == k.id)
				{
					return 2;
				}
				if (army.IsCrusadeArmy() && army.leader.IsInCourt(k))
				{
					return 2;
				}
			}
		}
		for (int j = 0; j < k.armies.Count; j++)
		{
			Army army2 = k.armies[j];
			if (army2.IsValid() && army2.battle != null && army2.battle.realm_id == id)
			{
				return 2;
			}
		}
		for (int l = 0; l < settlements.Count; l++)
		{
			Settlement settlement = settlements[l];
			if (settlement.IsOwnStance(k))
			{
				return 2;
			}
			if (settlement.IsAllyOrTeammate(k))
			{
				return 1;
			}
		}
		for (int m = 0; m < armies.Count; m++)
		{
			Army army3 = armies[m];
			if (army3.IsValid())
			{
				if (army3.IsAllyOrTeammate(k))
				{
					return 1;
				}
				if (army3.IsCrusadeArmy())
				{
					return 1;
				}
			}
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom != null && kingdom.IsAllyOrTeammate(k))
		{
			return 1;
		}
		Kingdom kingdom2 = GetKingdom();
		if (kingdom2 != null && kingdom2.GetSpyFrom(k) != null)
		{
			return 1;
		}
		if (!check_neighbors)
		{
			return 0;
		}
		for (int n = 0; n < neighbors.Count; n++)
		{
			if (neighbors[n].CalcVisibleBy(k, check_neighbors: false) == 2)
			{
				return 1;
			}
		}
		return 0;
	}

	public Realm(Game game)
		: base(game)
	{
	}

	public float GetStat(StatName stat_name, bool must_exist = true)
	{
		if (stats == null)
		{
			if (must_exist)
			{
				Error(string.Concat("GetStat('", stat_name, "'): Stats not initialized yet!"));
			}
			return 0f;
		}
		return stats.Get(stat_name, must_exist);
	}

	public float GetStat(string stat_name, bool must_exist = true)
	{
		if (stats == null)
		{
			if (must_exist)
			{
				Error("GetStat('" + stat_name + "'): Stats not initialized yet!");
			}
			return 0f;
		}
		return stats.Get(stat_name, must_exist);
	}

	public float GetGoldFromTradeCenter(bool recalcRealms)
	{
		if (tradeCenter == null)
		{
			return 0f;
		}
		return tradeCenter.GetGoldIncome(recalcRealms);
	}

	public float CalcGoldFromTCInfluence()
	{
		return 0f;
	}

	public void CalcGarrisonUpkep()
	{
		upkeepGarrison.Clear();
		List<Unit> units = castle.garrison.units;
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit.simulation == null || !unit.simulation.temporary)
			{
				upkeepGarrison.Add(unit.def.CalcUpkeep(null, castle.garrison, -1), 1f);
			}
		}
	}

	public float CalcBuildingsCommerceUpkeep()
	{
		if (!Building.Def.SOFT_RESOURCE_REQUIREMENTS)
		{
			return 0f;
		}
		if (castle == null)
		{
			return 0f;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return 0f;
		}
		float num = CalcBuildingsCommerceUpkeep(castle.buildings, kingdom);
		float num2 = CalcBuildingsCommerceUpkeep(castle.upgrades, kingdom);
		return (float)Math.Floor(num + num2);
	}

	private float CalcBuildingsCommerceUpkeep(List<Building> buildings, Kingdom k)
	{
		float num = 0f;
		for (int i = 0; i < buildings.Count; i++)
		{
			Building building = buildings[i];
			if (building?.def != null && building.IsWorking())
			{
				float num2 = building.def.CalcCommerceUpkeep(k);
				num += num2;
			}
		}
		return num;
	}

	public void OnCalcIncomes()
	{
		income_valid = true;
		ClearIncomeVars();
		if (tags == null)
		{
			BuildTags();
		}
		for (int i = 0; i < settlements.Count; i++)
		{
			settlements[i].production_from_Trade_center.Clear();
		}
	}

	public void OnIncomesCalculated()
	{
		incomes?.ToResource(_income);
		for (int i = 0; i < settlements.Count; i++)
		{
			settlements[i].InvalidateResources();
		}
		castle?.CacheTempDefenderStats();
	}

	public void OnUpkeepsCalculated()
	{
		upkeeps?.ToResource(_expenses);
	}

	public void OnApplyIncome()
	{
		castle?.UpdateFoodStorage();
		castle?.population?.Update();
	}

	public Incomes RecalcIncomesNow()
	{
		using (new Stat.ForceCached("Realm.RecalcIncomesNow"))
		{
			OnCalcIncomes();
			incomes?.Calc();
			OnIncomesCalculated();
			upkeeps?.Calc();
			OnUpkeepsCalculated();
		}
		return incomes;
	}

	public Resource RecalcIncomeNow()
	{
		RecalcIncomes(force: true);
		return _income;
	}

	public void InvalidateIncomes(bool force_full_recalc = true)
	{
		income_valid = false;
		incomes?.Invalidate(force_full_recalc);
		upkeeps?.Invalidate(force_full_recalc);
		tradeCenter?.RecalcIncome();
		GetKingdom()?.InvalidateIncomes(force_full_recalc: false);
	}

	public void RecalcIncomes(bool force = false)
	{
		if ((!income_valid || force) && !game.isInVideoMode && !game.IsUnloadingMap())
		{
			income_valid = true;
			RecalcIncomesNow();
			GetKingdom()?.InvalidateIncomes();
		}
	}

	public void ClearIncomeVars()
	{
		_income.Clear();
		_expenses.Clear();
		incomeFromTown.Clear();
		incomeFromSettlements.Clear();
		incomeFromPopulation.Clear();
		incomeFromTradeCenterInfluence.Clear();
		incomeFromGovernorSkills.Clear();
		upkeepGarrison.Clear();
	}

	public float GetCommerce()
	{
		if (GetKingdom() == null)
		{
			return 0f;
		}
		return income[ResourceType.Trade];
	}

	public int GetNumGarrisonUnits()
	{
		if (castle == null || castle.garrison == null)
		{
			return 0;
		}
		return castle.garrison.units.Count;
	}

	public bool HasCostalCastle()
	{
		if (def == null)
		{
			return false;
		}
		return def.GetBool("has_coastal_city");
	}

	public bool HasDistantPort()
	{
		if (def == null)
		{
			return false;
		}
		return def.GetBool("has_distant_port");
	}

	public int GetCostalSettlementsCount(bool include_inactive = false)
	{
		if (settlements == null || settlements.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < settlements.Count; i++)
		{
			Settlement settlement = settlements[i];
			if (settlement != null && settlement != castle && (settlement.IsActiveSettlement() || include_inactive) && settlement.coastal)
			{
				num++;
			}
		}
		return num;
	}

	public bool IsNearSea()
	{
		for (int i = 0; i < neighbors.Count; i++)
		{
			if (neighbors[i].IsSeaRealm())
			{
				return true;
			}
		}
		return false;
	}

	public bool HasNeighbor(Realm r)
	{
		return logicNeighborsRestricted.Contains(r);
	}

	public bool HasNeighborThroughSea(Realm r)
	{
		if (logicNeighborsRestricted.Contains(r))
		{
			return !neighbors.Contains(r);
		}
		return false;
	}

	public bool HasLogicNeighbor(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		for (int i = 0; i < logicNeighborsAll.Count; i++)
		{
			if (logicNeighborsAll[i].kingdom_id == k.id)
			{
				return true;
			}
		}
		return false;
	}

	public int GetNumRebels()
	{
		if (armies == null || armies.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			if (army.leader != null && army.leader.IsRebel())
			{
				num++;
			}
		}
		return num;
	}

	public float GetTotalRebellionRisk()
	{
		if (rebellionRisk == null)
		{
			return 0f;
		}
		return rebellionRisk.value;
	}

	public float GetRebellionRisk(int type)
	{
		if (rebellionRisk == null)
		{
			return 0f;
		}
		return rebellionRisk.GetRebellionRisk(type);
	}

	public float GetRebellionRisk(string stat_type)
	{
		if (rebellionRisk == null)
		{
			return 0f;
		}
		return rebellionRisk.GetRebellionRisk(stat_type);
	}

	public int GetSettlementCount(string type, string location, bool include_inactive = false)
	{
		string text = ((type == "Town") ? "Castle" : type);
		int num = 0;
		if (text == "All")
		{
			if (include_inactive)
			{
				num = settlements.Count;
				if (num > 0 && castle != null)
				{
					num--;
				}
			}
			else
			{
				for (int i = 0; i < settlements.Count; i++)
				{
					if (settlements[i] != castle && settlements[i].IsActiveSettlement())
					{
						num++;
					}
				}
			}
		}
		else
		{
			for (int j = 0; j < settlements.Count; j++)
			{
				Settlement settlement = settlements[j];
				if ((include_inactive || settlement.IsActiveSettlement()) && settlement.MatchType(text))
				{
					num++;
				}
			}
		}
		if ((location == "neighbors" || location == "region") && logicNeighborsAll != null)
		{
			for (int k = 0; k < logicNeighborsAll.Count; k++)
			{
				Realm realm = logicNeighborsAll[k];
				if (realm.kingdom_id == kingdom_id)
				{
					int settlementCount = realm.GetSettlementCount(text, "realm", include_inactive);
					num += settlementCount;
				}
			}
		}
		return num;
	}

	public int GetPotentialCommerse()
	{
		if (castle == null)
		{
			return 0;
		}
		List<Building.Def> defs = game.defs.GetDefs<Building.Def>();
		if (this.def == null)
		{
			return 0;
		}
		tmp_production.Clear();
		for (int i = 0; i < defs.Count; i++)
		{
			Building.Def def = defs[i];
			if (castle.HasWorkingBuilding(def) || castle.MayBuildBuilding(def))
			{
				def.CalcProduction(tmp_production, castle, 3, check_condition: false);
			}
		}
		return (int)tmp_production[ResourceType.Trade];
	}

	public override Value GetDumpStateValue()
	{
		return name;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("owner_kingdom", GetKingdom());
		dump.Append("controller", controller);
		if (castle != null)
		{
			dump.Append("governor", castle.governor?.ToString());
			if (castle.buildings.Count > 0)
			{
				dump.OpenSection("buildings");
				for (int i = 0; i < castle.buildings.Count; i++)
				{
					dump.Append(castle.buildings[i]?.def?.field?.key);
				}
				dump.CloseSection("buildings");
			}
			dump.Append("currently_building", castle.GetCurrentBuildingBuild()?.field?.key);
		}
		dump.Append("income", income.ToString());
		if (goods_produced != null && goods_produced.Count > 0)
		{
			dump.OpenSection("goods produced");
			for (int j = 0; j < goods_produced.Count; j++)
			{
				dump.Append(goods_produced[j]?.Name);
			}
			dump.CloseSection("goods produced");
		}
		dump.Append("religion", religion?.ToString());
		dump.Append("culture_group", game.cultures.GetGroup(culture));
		dump.Append("culture", culture?.ToString());
		dump.Append("occupied", IsOccupied().ToString());
		dump.Append("disorder", IsDisorder().ToString());
		dump.OpenSection("population_majority");
		dump.Append("kingdom", pop_majority.kingdom);
		dump.Append("strength", pop_majority.strength);
		dump.CloseSection("population_majority");
		if (!IsSeaRealm())
		{
			stats?.DumpInnerState(dump, verbosity);
		}
		if (actions?.current != null)
		{
			dump.Append("action", actions.current?.def?.field?.key);
		}
		base.DumpInnerState(dump, verbosity);
	}

	public int GetProducedGoodsCount()
	{
		DT.Def def = game.dt.FindDef("Resource");
		if (def == null || def.defs == null)
		{
			return 0;
		}
		int num = 0;
		List<DT.Def> defs = def.defs;
		for (int i = 0; i < defs.Count; i++)
		{
			DT.Def def2 = defs[i];
			if (def2 != null && HasTag(def2.field.key))
			{
				num++;
			}
		}
		return num;
	}

	public float GetFame()
	{
		float num = 0f;
		num += fame;
		if (castle?.governor == null)
		{
			return num;
		}
		num += GetStat(Stats.rs_fame_town_bonus);
		return num + GetStat(Stats.rs_fame_buildings_bonus);
	}

	public float CalcCost(Kingdom forKingdom = null)
	{
		return RealmCost.CalcRealmCost(this, forKingdom);
	}

	public override string ToString()
	{
		string text = base.ToString();
		return text + " " + id + "(" + (castle?.name ?? name) + ")";
	}

	protected override void OnStart()
	{
		base.OnStart();
		new OnSiegeConvertReligion(this);
		CreateActions();
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (message == "stat_changed" && param is Stat stat)
		{
			string text = stat.def.name;
			if (text == "rs_fame_bonus")
			{
				GetKingdom().NotifyListeners("realm_presige_changed");
			}
		}
	}
}
