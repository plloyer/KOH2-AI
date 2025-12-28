using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Character)]
public class Character : Object, IListener
{
	public struct RoyalAbility : IVars
	{
		public string name;

		public int val;

		public int val_min;

		public int val_max;

		public override string ToString()
		{
			return $"{name}: {val}";
		}

		public string GetNameKey()
		{
			return "CharacterClass.royal_abilities.ability_names." + name;
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"name" => GetNameKey(), 
				"value" => val, 
				"description" => "CharacterClass.royal_abilities.ability_descriptions." + name, 
				_ => Value.Unknown, 
			};
		}
	}

	public struct ImportedGood
	{
		public string name;

		public float discount;

		public override string ToString()
		{
			return $"{name} (-{discount}%)";
		}
	}

	public class SkillReplacement : BaseObject
	{
		public enum Type
		{
			Affinity,
			Book,
			Realm,
			GreatPerson,
			Cleric
		}

		public class FullData : Data
		{
			public string type;

			public string skill;

			public Data param;

			public static FullData Create()
			{
				return new FullData();
			}

			public override bool InitFrom(object obj)
			{
				if (!(obj is SkillReplacement skillReplacement))
				{
					return false;
				}
				type = skillReplacement.type.ToString();
				skill = skillReplacement.skill_def.id;
				if (skillReplacement.type != Type.Book)
				{
					param = Data.CreateRef(skillReplacement.param);
				}
				return true;
			}

			public override void Save(Serialization.IWriter ser)
			{
				ser.WriteStr(type, "type");
				ser.WriteStr(skill, "skill");
				ser.WriteData(param, "param");
			}

			public override void Load(Serialization.IReader ser)
			{
				type = ser.ReadStr("type");
				skill = ser.ReadStr("skill");
				param = ser.ReadData("param");
			}

			public override object GetObject(Game game)
			{
				return new SkillReplacement();
			}

			public override bool ApplyTo(object obj, Game game)
			{
				if (!(obj is SkillReplacement skillReplacement))
				{
					return false;
				}
				if (!Enum.TryParse<Type>(type, out skillReplacement.type))
				{
					Game.Log("Could not parse SkillReplacement type", Game.LogType.Error);
					return false;
				}
				skillReplacement.skill_def = game.defs.Get<Skill.Def>(skill);
				skillReplacement.param = Data.RestoreObject<BaseObject>(param, game);
				if (skillReplacement.param == null && skillReplacement.type == Type.Book)
				{
					skillReplacement.param = game.defs.Get<Book.Def>(skillReplacement.skill_def.name + "Book");
				}
				return true;
			}
		}

		public Type type;

		public Skill.Def skill_def;

		public BaseObject param;

		public Realm realm
		{
			get
			{
				if (type != Type.Realm)
				{
					return null;
				}
				return param as Realm;
			}
		}

		public Book.Def book_def
		{
			get
			{
				if (type != Type.Book)
				{
					return null;
				}
				return param as Book.Def;
			}
		}

		public Character great_person
		{
			get
			{
				if (type != Type.GreatPerson)
				{
					return null;
				}
				return param as Character;
			}
		}

		public Character cleric
		{
			get
			{
				if (type != Type.Cleric)
				{
					return null;
				}
				return param as Character;
			}
		}

		public Resource cost => Resource.Parse(skill_def.field.FindChild("replace_cost." + type));

		public SkillReplacement()
		{
		}

		public SkillReplacement(Type type, Skill.Def skill_def, BaseObject param)
		{
			this.type = type;
			this.skill_def = skill_def;
			this.param = param;
		}

		public override string ToString()
		{
			string text = skill_def.name + " (" + type;
			switch (type)
			{
			case Type.Book:
				text = text + " " + book_def?.name;
				break;
			case Type.Realm:
				text = text + " " + realm?.name;
				break;
			case Type.GreatPerson:
				text = text + " " + great_person?.Name;
				break;
			case Type.Cleric:
				text = text + " " + cleric?.Name;
				break;
			}
			return text + ")";
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (!(key == "skill"))
			{
				if (key == "cost")
				{
					return cost;
				}
				return Value.Unknown;
			}
			return skill_def;
		}
	}

	public enum Age
	{
		Infant,
		Child,
		Juvenile,
		Young,
		Adult,
		Old,
		Venerable,
		COUNT
	}

	public enum Sex
	{
		Male,
		Female
	}

	public enum Ethnicity
	{
		European,
		Nordic,
		Slavic,
		African,
		Mongol,
		Mediterranian,
		Arabic,
		COUNT
	}

	[Serialization.State(11)]
	public class TitleState : Serialization.ObjectState
	{
		public string title;

		public static TitleState Create()
		{
			return new TitleState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (!string.IsNullOrEmpty(character.title))
			{
				return character.title != "Knight";
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			title = character.title;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			if (string.IsNullOrEmpty(title))
			{
				ser.WriteStr("", "title");
			}
			else
			{
				ser.WriteStr(title, "title");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			title = ser.ReadStr("title");
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			if (string.IsNullOrEmpty(title))
			{
				character.SetTitle(null, send_state: false);
			}
			else
			{
				character.SetTitle(title, send_state: false);
			}
		}
	}

	[Serialization.State(12)]
	public class NameState : Serialization.ObjectState
	{
		public string name;

		public string customName;

		public static NameState Create()
		{
			return new NameState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			name = character.Name;
			if (character.name_idx > 0)
			{
				name += $"#{character.name_idx}";
			}
			customName = character.CustomName;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			if (name == null)
			{
				ser.WriteStr("", "name");
			}
			else
			{
				ser.WriteStr(name, "name");
			}
			if (customName == null)
			{
				ser.WriteStr("", "custom_name");
			}
			else
			{
				ser.WriteStr(customName, "custom_name");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			name = ser.ReadStr("name");
			if (Serialization.cur_version >= 13)
			{
				customName = ser.ReadStr("custom_name");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			int num = name.IndexOf('#');
			if (num > 0)
			{
				int.TryParse(name.Substring(num + 1), out var result);
				name = name.Substring(0, num);
				character.name_idx = result;
			}
			else
			{
				character.name_idx = 0;
			}
			character.Name = name;
			character.CustomName = customName;
			character.NotifyListeners("name_changed");
		}
	}

	[Serialization.State(13)]
	public class KingdomState : Serialization.ObjectState
	{
		public int kingdom_id;

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
			Character character = obj as Character;
			kingdom_id = character.kingdom_id;
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
			Character character = obj as Character;
			character.kingdom_id = kingdom_id;
			character.stats?.SetKingdom(character.GetKingdom());
			character.NotifyListeners("kingdom_changed");
		}
	}

	[Serialization.State(14)]
	public class LocationState : Serialization.ObjectState
	{
		public NID location_nid;

		public static LocationState Create()
		{
			return new LocationState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).location != null;
		}

		public override bool InitFrom(Object obj)
		{
			MapObject location = (obj as Character).location;
			location_nid = location;
			return location != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(location_nid, "location_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			location_nid = ser.ReadNID("location_nid");
		}

		public override void ApplyTo(Object obj)
		{
			MapObject location = location_nid.GetObj(obj.game) as MapObject;
			(obj as Character).SetLocation(location, send_state: false);
		}
	}

	[Serialization.State(15)]
	public class SexState : Serialization.ObjectState
	{
		private int sex;

		public static SexState Create()
		{
			return new SexState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).sex != Sex.Male;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			sex = (int)character.sex;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(sex, "sex");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			sex = ser.Read7BitUInt("sex");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Character).sex = (Sex)sex;
		}
	}

	[Serialization.State(16)]
	public class AgeState : Serialization.ObjectState
	{
		public int age;

		public float next_age_check_delta;

		public float next_die_check_delta;

		public bool can_die;

		public static AgeState Create()
		{
			return new AgeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			age = (int)character.age;
			can_die = character.can_die;
			if (character.next_age_check == Time.Zero)
			{
				next_age_check_delta = -1f;
			}
			else if (character.game.time < character.next_age_check)
			{
				next_age_check_delta = character.next_age_check - character.game.time;
			}
			else
			{
				next_age_check_delta = 0f;
			}
			if (character.next_die_check == Time.Zero)
			{
				next_die_check_delta = -1f;
			}
			else if (character.game.time < character.next_die_check)
			{
				next_die_check_delta = character.next_die_check - character.game.time;
			}
			else
			{
				next_die_check_delta = 0f;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(age, "age");
			ser.WriteBool(can_die, "can_die");
			ser.WriteFloat(next_age_check_delta, "next_age_check_delta");
			ser.WriteFloat(next_die_check_delta, "next_die_check_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			age = ser.Read7BitUInt("age");
			can_die = ser.ReadBool("can_die");
			next_age_check_delta = ser.ReadFloat("next_age_check_delta");
			next_die_check_delta = ser.ReadFloat("next_die_check_delta");
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			character.age = (Age)age;
			character.can_die = can_die;
			if (next_age_check_delta == -1f)
			{
				character.next_age_check = Time.Zero;
			}
			else
			{
				character.next_age_check = character.game.time + next_age_check_delta;
			}
			if (next_die_check_delta == -1f)
			{
				character.next_die_check = Time.Zero;
			}
			else
			{
				character.next_die_check = character.game.time + next_die_check_delta;
			}
			character.NotifyListeners("character_age_change");
		}
	}

	[Serialization.State(17)]
	public class PortraitIdState : Serialization.ObjectState
	{
		private int portraitId;

		private int variantId;

		private int ageId;

		public static PortraitIdState Create()
		{
			return new PortraitIdState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			portraitId = character.portraitID;
			variantId = character.portrait_variantID;
			ageId = character.portrait_age;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(portraitId, "portraitId");
			ser.Write7BitSigned(variantId, "variantId");
			ser.Write7BitUInt(ageId, "ageId");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			portraitId = ser.Read7BitSigned("portraitId");
			variantId = ser.Read7BitSigned("variantId");
			ageId = ser.Read7BitUInt("ageId");
		}

		public override void ApplyTo(Object obj)
		{
			Character obj2 = obj as Character;
			obj2.portraitID = portraitId;
			obj2.portrait_variantID = variantId;
			obj2.portrait_age = ageId;
			obj2.NotifyListeners("portrait_changed");
		}
	}

	[Serialization.State(18)]
	public class ClassState : Serialization.ObjectState
	{
		private string class_def;

		public static ClassState Create()
		{
			return new ClassState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character.class_def == null || character.class_def.IsBase())
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			class_def = ((character.class_def == null || character.class_def.IsBase()) ? "" : character.class_def.id);
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(class_def, "class_def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			class_def = ser.ReadStr("class_def");
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			CharacterClass.Def def = (string.IsNullOrEmpty(class_def) ? null : character.game.defs.Get<CharacterClass.Def>(class_def));
			character.SetClass(def, send_state: false);
		}
	}

	[Serialization.State(19)]
	public class SkillsState : Serialization.ObjectState
	{
		private List<string> skills = new List<string>();

		private List<int> ranks = new List<int>();

		private List<string> new_skills = new List<string>();

		public static SkillsState Create()
		{
			return new SkillsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character.skills == null || character.skills.Count <= 0)
			{
				if (character.new_skills != null)
				{
					return character.new_skills.Count > 0;
				}
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			int num = ((character.skills != null) ? character.skills.Count : 0);
			for (int i = 0; i < num; i++)
			{
				Skill skill = character.skills[i];
				if (skill == null)
				{
					skills.Add(null);
					ranks.Add(0);
				}
				else
				{
					skills.Add(skill.def.id);
					ranks.Add(skill.rank);
				}
			}
			int num2 = ((character.new_skills != null) ? character.new_skills.Count : 0);
			for (int j = 0; j < num2; j++)
			{
				Skill.Def def = character.new_skills[j];
				new_skills.Add(def.id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = skills.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteStr(skills[i], "def_id_", i);
				ser.Write7BitUInt(ranks[i], "rank_", i);
			}
			int count2 = new_skills.Count;
			ser.Write7BitUInt(count2, "new_skills_count");
			for (int j = 0; j < count2; j++)
			{
				ser.WriteStr(new_skills[j], "ns_def_id_", j);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				string item = ser.ReadStr("def_id_", i);
				skills.Add(item);
				int item2 = ser.Read7BitUInt("rank_", i);
				ranks.Add(item2);
			}
			int num2 = ser.Read7BitUInt("new_skills_count");
			for (int j = 0; j < num2; j++)
			{
				string item3 = ser.ReadStr("ns_def_id_", j);
				new_skills.Add(item3);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			int count = skills.Count;
			if (count == 0)
			{
				character.skills = null;
			}
			int count2 = new_skills.Count;
			if (count2 == 0)
			{
				character.new_skills = null;
			}
			if (count == 0 && count2 == 0)
			{
				character.UpdateAutomaticStatuses();
				character.NotifyListeners("skills_changed");
				return;
			}
			if (character.skills != null)
			{
				for (int i = 0; i < character.skills.Count; i++)
				{
					Skill skill = character.skills[i];
					if (skill != null && skill.mods_row != null)
					{
						character.skills[i].mods_row.Revert(character);
					}
				}
			}
			character.skills = new List<Skill>(count);
			for (int j = 0; j < count; j++)
			{
				string text = skills[j];
				if (string.IsNullOrEmpty(text))
				{
					character.skills.Add(null);
					continue;
				}
				int rank = ranks[j];
				Skill.Def def = character.game.defs.Get<Skill.Def>(text);
				if (!def.valid)
				{
					character.Error("Unknown skill def: " + text);
					continue;
				}
				Skill item = new Skill(def, character, rank);
				character.skills.Add(item);
			}
			if (count2 > 0)
			{
				character.new_skills = new List<Skill.Def>(count2);
				for (int k = 0; k < count2; k++)
				{
					string text2 = new_skills[k];
					Skill.Def def2 = character.game.defs.Get<Skill.Def>(text2);
					if (!def2.valid)
					{
						character.Error("Unknown new_skill def: " + text2);
					}
					else
					{
						character.new_skills.Add(def2);
					}
				}
			}
			character.UpdateAutomaticStatuses();
			character.NotifyListeners("skills_changed");
		}
	}

	[Serialization.State(20)]
	public class MissionKingdomState : Serialization.ObjectState
	{
		private int kingdom_id;

		public static MissionKingdomState Create()
		{
			return new MissionKingdomState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).mission_kingdom != null;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			kingdom_id = ((character.mission_kingdom != null) ? character.mission_kingdom.id : 0);
			return kingdom_id != 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_id, "kingdom");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdom_id = ser.Read7BitUInt("kingdom");
		}

		public override void ApplyTo(Object obj)
		{
			Character obj2 = obj as Character;
			Kingdom kingdom = obj2.game.GetKingdom(kingdom_id);
			obj2.SetMissionKingdom(kingdom, send_state: false);
		}
	}

	[Serialization.State(21)]
	public class GovernedCastleState : Serialization.ObjectState
	{
		private NID castle_nid;

		private NID banished_from_nid;

		public static GovernedCastleState Create()
		{
			return new GovernedCastleState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character.governed_castle == null)
			{
				return character.governed_castle_banished_from != null;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			castle_nid = character.governed_castle;
			banished_from_nid = character.governed_castle_banished_from;
			if (castle_nid.nid == 0)
			{
				return banished_from_nid.nid != 0;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Castle>(castle_nid, "castle");
			ser.WriteNID<Castle>(banished_from_nid, "banished_from");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			castle_nid = ser.ReadNID<Castle>("castle");
			banished_from_nid = ser.ReadNID<Castle>("banished_from");
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			Castle governed_castle = character.governed_castle;
			character.governed_castle = castle_nid.Get<Castle>(obj.game);
			character.governed_castle_banished_from = banished_from_nid.Get<Castle>(obj.game);
			character.UpdateAutomaticStatuses();
			if (character.governed_castle == null)
			{
				character.NotifyListeners("stoped_governing", governed_castle);
			}
			else
			{
				character.NotifyListeners("started_governing", character.governed_castle);
			}
		}
	}

	[Serialization.State(22)]
	public class PrisonKingdomState : Serialization.ObjectState
	{
		private int kingdom_id;

		private string prison_reason;

		private float prison_time = -1f;

		private float ai_renounce_time = -1f;

		public static PrisonKingdomState Create()
		{
			return new PrisonKingdomState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).prison_kingdom != null;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			kingdom_id = ((character.prison_kingdom != null) ? character.prison_kingdom.id : 0);
			prison_reason = character.prison_reason;
			prison_time = obj.game.time - character.prison_time;
			ai_renounce_time = character.ai_renounce_time - obj.game.time;
			return kingdom_id != 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_id, "kingdom");
			ser.WriteStr(prison_reason, "prison_reason");
			if (kingdom_id != 0)
			{
				ser.WriteFloat(prison_time, "prison_time");
				ser.WriteFloat(ai_renounce_time, "ai_renounce_time");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdom_id = ser.Read7BitUInt("kingdom");
			if (kingdom_id != 0 || Serialization.cur_version >= 10)
			{
				prison_reason = ser.ReadStr("prison_reason");
				if (kingdom_id != 0)
				{
					prison_time = ser.ReadFloat("prison_time");
					ai_renounce_time = ser.ReadFloat("ai_renounce_time");
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Character { prison_kingdom: var prison_kingdom } character))
			{
				return;
			}
			Kingdom kingdom = (character.prison_kingdom = character.game.GetKingdom(kingdom_id));
			character.prison_reason = prison_reason;
			character.prison_time = ((prison_time < 0f) ? Time.Zero : (character.game.time - prison_time));
			character.ai_renounce_time = ((ai_renounce_time < 0f) ? Time.Zero : (character.game.time + ai_renounce_time));
			if (kingdom != null && kingdom != prison_kingdom)
			{
				bool flag = prison_kingdom != null;
				if (flag)
				{
					kingdom.DelPrisoner(character, flag);
				}
				kingdom.AddPrisoner(character, flag);
				if (flag)
				{
					character.NotifyListeners("prison_kingdom_changed", prison_kingdom);
				}
				else
				{
					character.NotifyListeners("imprisoned");
				}
			}
			else if (prison_kingdom != null && kingdom == null)
			{
				prison_kingdom.DelPrisoner(character);
			}
			character.UpdateAutomaticStatuses();
			character.NotifyListeners("prison_changed", character);
		}
	}

	[Serialization.State(23)]
	public class MissionRealmState : Serialization.ObjectState
	{
		private int realm_id;

		public static MissionRealmState Create()
		{
			return new MissionRealmState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).mission_realm != null;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			realm_id = ((character.mission_realm != null) ? character.mission_realm.id : 0);
			return realm_id != 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(realm_id, "realm");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			realm_id = ser.Read7BitUInt("realm");
		}

		public override void ApplyTo(Object obj)
		{
			Character obj2 = obj as Character;
			Realm realm = obj2.game.GetRealm(realm_id);
			obj2.SetMissionRealm(realm, send_state: false);
		}
	}

	[Serialization.State(24)]
	public class TradeState : Serialization.ObjectState
	{
		public int trade_level;

		public float trade_level_time_delta;

		public List<ImportedGood> importing_goods;

		public static TradeState Create()
		{
			return new TradeState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character.trade_level == 0 && (character.importing_goods == null || character.importing_goods.Count == 0))
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			trade_level = character.trade_level;
			trade_level_time_delta = character.trade_level_time - character.game.time;
			if (character.importing_goods != null)
			{
				importing_goods = new List<ImportedGood>(character.importing_goods);
			}
			if (trade_level == 0)
			{
				if (importing_goods != null)
				{
					return importing_goods.Count > 0;
				}
				return false;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(trade_level, "trade_level");
			ser.WriteFloat(trade_level_time_delta, "trade_level_time_delta");
			int num = ((importing_goods != null) ? importing_goods.Count : 0);
			ser.Write7BitUInt(num, "num_importing_goods");
			for (int i = 0; i < num; i++)
			{
				ImportedGood importedGood = importing_goods[i];
				ser.WriteStr(importedGood.name, "importing_good", i);
				if (!string.IsNullOrEmpty(importedGood.name))
				{
					ser.WriteFloat(importedGood.discount, "importing_discount", i);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			trade_level = ser.Read7BitUInt("trade_level");
			trade_level_time_delta = ser.ReadFloat("trade_level_time_delta");
			int num = ser.Read7BitUInt("num_importing_goods");
			if (num <= 0)
			{
				return;
			}
			importing_goods = new List<ImportedGood>(num);
			for (int i = 0; i < num; i++)
			{
				string text = ser.ReadStr("importing_good", i);
				float discount = 0f;
				if (!string.IsNullOrEmpty(text))
				{
					discount = ser.ReadFloat("importing_discount", i);
				}
				importing_goods.Add(new ImportedGood
				{
					name = text,
					discount = discount
				});
			}
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			character.SetTradeLevel(trade_level, send_state: false);
			character.trade_level_time = character.game.time + trade_level_time_delta;
			if (importing_goods == null)
			{
				character.importing_goods = null;
			}
			else
			{
				character.importing_goods = new List<ImportedGood>(importing_goods);
			}
			Kingdom kingdom = character.GetKingdom();
			if (kingdom != null)
			{
				kingdom.RecalcBuildingStates();
				kingdom.RefreshRealmTags();
				kingdom.UpdateRealmTags();
				kingdom.InvalidateIncomes();
				character.NotifyListeners("importing_goods_changed");
			}
		}
	}

	[Serialization.State(25)]
	public class EspionageState : Serialization.ObjectState
	{
		public List<NID> puppets;

		public List<NID> masters;

		public List<int> revealed_in_kingdoms;

		public int reveal_kingdom;

		public NID reveal_master;

		public static EspionageState Create()
		{
			return new EspionageState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character.puppets != null && character.puppets.Count > 0)
			{
				return true;
			}
			if (character.masters != null && character.masters.Count > 0)
			{
				return true;
			}
			if (character.revealed_in_kingdoms != null && character.revealed_in_kingdoms.Count > 0)
			{
				return true;
			}
			if (character.reveal_kingdom != 0)
			{
				return true;
			}
			if (character.reveal_master != null)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			if (character.puppets != null && character.puppets.Count > 0)
			{
				puppets = new List<NID>(character.puppets.Count);
				for (int i = 0; i < character.puppets.Count; i++)
				{
					NID item = character.puppets[i];
					puppets.Add(item);
				}
			}
			if (character.masters != null && character.masters.Count > 0)
			{
				masters = new List<NID>(character.masters.Count);
				for (int j = 0; j < character.masters.Count; j++)
				{
					NID item2 = character.masters[j];
					masters.Add(item2);
				}
			}
			if (character.revealed_in_kingdoms != null)
			{
				revealed_in_kingdoms = new List<int>(character.revealed_in_kingdoms);
			}
			reveal_kingdom = character.reveal_kingdom;
			reveal_master = character.reveal_master;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((puppets != null) ? puppets.Count : 0);
			ser.Write7BitUInt(num, "puppets");
			for (int i = 0; i < num; i++)
			{
				NID nID = puppets[i];
				ser.WriteNID<Character>(nID, "puppet", i);
			}
			int num2 = ((masters != null) ? masters.Count : 0);
			ser.Write7BitUInt(num2, "masters");
			for (int j = 0; j < num2; j++)
			{
				NID nID2 = masters[j];
				ser.WriteNID<Character>(nID2, "master", j);
			}
			int num3 = ((revealed_in_kingdoms != null) ? revealed_in_kingdoms.Count : 0);
			ser.Write7BitUInt(num3, "revealed_in_kingdoms");
			for (int k = 0; k < num3; k++)
			{
				int val = revealed_in_kingdoms[k];
				ser.Write7BitUInt(val, "revealed_in_kingdom", k);
			}
			ser.Write7BitUInt(reveal_kingdom, "reveal_kingdom");
			ser.WriteNID<Character>(reveal_master, "reveal_master");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("puppets");
			if (num > 0)
			{
				puppets = new List<NID>(num);
				for (int i = 0; i < num; i++)
				{
					NID item = ser.ReadNID<Character>("puppet", i);
					puppets.Add(item);
				}
			}
			int num2 = ser.Read7BitUInt("masters");
			if (num2 > 0)
			{
				masters = new List<NID>(num2);
				for (int j = 0; j < num2; j++)
				{
					NID item2 = ser.ReadNID<Character>("master", j);
					masters.Add(item2);
				}
			}
			int num3 = ser.Read7BitUInt("revealed_in_kingdoms");
			if (num3 > 0)
			{
				revealed_in_kingdoms = new List<int>(num3);
				for (int k = 0; k < num3; k++)
				{
					int item3 = ser.Read7BitUInt("revealed_in_kingdom", k);
					revealed_in_kingdoms.Add(item3);
				}
			}
			reveal_kingdom = ser.Read7BitUInt("reveal_kingdom");
			reveal_master = ser.ReadNID<Character>("reveal_master");
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			if (puppets != null && puppets.Count > 0)
			{
				character.puppets = new List<Character>(puppets.Count);
				for (int i = 0; i < puppets.Count; i++)
				{
					Character character2 = puppets[i].Get<Character>(character.game);
					if (character2 != null)
					{
						character.puppets.Add(character2);
					}
				}
			}
			else
			{
				character.puppets = null;
			}
			if (masters != null && masters.Count > 0)
			{
				character.masters = new List<Character>(masters.Count);
				for (int j = 0; j < masters.Count; j++)
				{
					Character character3 = masters[j].Get<Character>(character.game);
					if (character3 != null)
					{
						character.masters.Add(character3);
					}
				}
			}
			else
			{
				character.masters = null;
			}
			if (revealed_in_kingdoms != null)
			{
				character.revealed_in_kingdoms = new List<int>(revealed_in_kingdoms);
			}
			else
			{
				character.revealed_in_kingdoms = null;
			}
			character.SetRevealKingdom(reveal_kingdom, send_state: false);
			character.SetRevealMaster(reveal_master.Get<Character>(character.game), send_state: false);
		}
	}

	[Serialization.State(26)]
	public class RebelState : Serialization.ObjectState
	{
		public string rebel_def_id;

		public static RebelState Create()
		{
			return new RebelState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			if (character.rebel_def != null)
			{
				rebel_def_id = character.rebel_def.id;
			}
			else
			{
				rebel_def_id = "";
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(rebel_def_id, "rebel_def_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			rebel_def_id = ser.ReadStr("rebel_def_id");
		}

		public override void ApplyTo(Object obj)
		{
			Character character = obj as Character;
			if (rebel_def_id != "")
			{
				character.SetRebel(character.game.defs.Get<Rebel.Def>(rebel_def_id), send_state: false);
			}
			else
			{
				character.rebel_def = null;
			}
		}
	}

	[Serialization.State(27)]
	public class OriginState : Serialization.ObjectState
	{
		public int original_kingdom_id;

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
			Character character = obj as Character;
			original_kingdom_id = character.original_kingdom_id;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(original_kingdom_id, "original_kingdom_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			original_kingdom_id = ser.Read7BitUInt("original_kingdom_id");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Character).original_kingdom_id = original_kingdom_id;
		}
	}

	[Serialization.State(28)]
	public class RoyalAbilitiesState : Serialization.ObjectState
	{
		public List<RoyalAbility> abilities;

		public static RoyalAbilitiesState Create()
		{
			return new RoyalAbilitiesState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character?.royal_abilities == null || character.royal_abilities.Count == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			if (character?.royal_abilities == null)
			{
				return false;
			}
			abilities = new List<RoyalAbility>(character.royal_abilities);
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((abilities != null) ? abilities.Count : 0);
			ser.Write7BitUInt(num, "count");
			for (int i = 0; i < num; i++)
			{
				RoyalAbility royalAbility = abilities[i];
				ser.WriteStr(royalAbility.name, "name", i);
				ser.Write7BitSigned(royalAbility.val, "val", i);
				ser.Write7BitSigned(royalAbility.val_min, "min", i);
				ser.Write7BitSigned(royalAbility.val_max, "max", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				abilities = new List<RoyalAbility>(num);
				for (int i = 0; i < num; i++)
				{
					string name = ser.ReadStr("name", i);
					int val = ser.Read7BitSigned("val", i);
					int val_min = ser.Read7BitSigned("min", i);
					int val_max = ser.Read7BitSigned("max", i);
					abilities.Add(new RoyalAbility
					{
						name = name,
						val = val,
						val_min = val_min,
						val_max = val_max
					});
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Character character)
			{
				character.royal_abilities = abilities;
				character.NotifyListeners("royal_abilities_changed");
			}
		}
	}

	[Serialization.State(29)]
	public class BloodLineState : Serialization.ObjectState
	{
		public int bloodLinePower;

		public static BloodLineState Create()
		{
			return new BloodLineState();
		}

		public static bool IsNeeded(Object obj)
		{
			Character character = obj as Character;
			if (character?.royal_abilities == null || character.royalBloodPower == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			if (character != null && character.royalBloodPower == 0)
			{
				return false;
			}
			bloodLinePower = character.royalBloodPower;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(bloodLinePower, "power");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			bloodLinePower = ser.Read7BitUInt("power");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Character character)
			{
				character.SetRoyalBloodPower(bloodLinePower, send_state: false);
			}
		}
	}

	[Serialization.State(30)]
	public class EthnicityState : Serialization.ObjectState
	{
		private int ethnicity;

		public static EthnicityState Create()
		{
			return new EthnicityState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).ethnicity != Ethnicity.European;
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			ethnicity = (int)character.ethnicity;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(ethnicity, "ethnicity");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			ethnicity = ser.Read7BitUInt("ethnicity");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Character).ethnicity = (Ethnicity)ethnicity;
		}
	}

	[Serialization.State(31)]
	public class SpecialCourtKingdomState : Serialization.ObjectState
	{
		private int kid;

		public static SpecialCourtKingdomState Create()
		{
			return new SpecialCourtKingdomState();
		}

		public override bool InitFrom(Object obj)
		{
			Character character = obj as Character;
			kid = character.GetSpecialCourtKingdomId();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kid, "kid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kid = ser.Read7BitUInt("kid");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Character)?.SetSpecialCourtKingdom(kid, send_state: false);
		}
	}

	[Serialization.State(32)]
	public class PaganBeliefState : Serialization.ObjectState
	{
		public string beliefName;

		public static PaganBeliefState Create()
		{
			return new PaganBeliefState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).paganBelief != null;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Character character))
			{
				return false;
			}
			beliefName = character.paganBelief?.name;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(beliefName, "beliefName");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			beliefName = ser.ReadStr("beliefName");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Character character)
			{
				character.paganBelief = obj.game.religions.pagan.def.FindPaganBelief(beliefName);
			}
		}
	}

	[Serialization.State(33)]
	public class MarriageState : Serialization.ObjectState
	{
		public NID marriage;

		public static MarriageState Create()
		{
			return new MarriageState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Character).marriage != null;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Character character))
			{
				return false;
			}
			marriage = character.marriage;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Marriage>(marriage, "marriage");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			marriage = ser.ReadNID<Marriage>("marriage");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Character character)
			{
				character.marriage = marriage.Get<Marriage>(character.game);
			}
		}
	}

	[Serialization.Event(27)]
	public class HireCharacterEvent : Serialization.ObjectEvent
	{
		private int index;

		private int k_id;

		public HireCharacterEvent()
		{
		}

		public static HireCharacterEvent Create()
		{
			return new HireCharacterEvent();
		}

		public HireCharacterEvent(int index, int k_id)
		{
			this.index = index;
			this.k_id = k_id;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(index, "index");
			ser.Write7BitUInt(k_id, "k_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			index = ser.Read7BitUInt("index");
			k_id = ser.Read7BitUInt("k_id");
		}

		public override void ApplyTo(Object obj)
		{
			Character c = obj as Character;
			obj.game.GetKingdom(k_id).AddCourtMember(c, index);
		}
	}

	[Serialization.Event(28)]
	public class AddSkillEvent : Serialization.ObjectEvent
	{
		private string skill_def;

		public AddSkillEvent()
		{
		}

		public static AddSkillEvent Create()
		{
			return new AddSkillEvent();
		}

		public AddSkillEvent(Skill.Def skill_def)
		{
			this.skill_def = skill_def.dt_def.path;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(skill_def, "skill_def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			skill_def = ser.ReadStr("skill_def");
		}

		public override void ApplyTo(Object obj)
		{
			Character obj2 = obj as Character;
			Skill.Def def = obj2.game.defs.Get<Skill.Def>(skill_def);
			obj2.AddSkill(def);
		}
	}

	[Serialization.Event(29)]
	public class ReplaceSkillEvent : Serialization.ObjectEvent
	{
		public int slot;

		public Data replacement;

		public ReplaceSkillEvent()
		{
		}

		public static ReplaceSkillEvent Create()
		{
			return new ReplaceSkillEvent();
		}

		public ReplaceSkillEvent(int slot, SkillReplacement replacement)
		{
			this.slot = slot;
			this.replacement = Data.CreateFull(replacement);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(slot, "slot");
			ser.WriteData(replacement, "replacement");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			slot = ser.Read7BitUInt("slot");
			replacement = ser.ReadData("replacement");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Character).ReplaceSkill(replacement: Data.RestoreObject<SkillReplacement>(replacement, obj.game), slot: slot);
		}
	}

	[Serialization.Event(30)]
	public class AddSkillRankEvent : Serialization.ObjectEvent
	{
		private string skill_def;

		public AddSkillRankEvent()
		{
		}

		public static AddSkillRankEvent Create()
		{
			return new AddSkillRankEvent();
		}

		public AddSkillRankEvent(Skill.Def skill_def)
		{
			this.skill_def = skill_def.dt_def.path;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(skill_def, "skill_def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			skill_def = ser.ReadStr("skill_def");
		}

		public override void ApplyTo(Object obj)
		{
			Character obj2 = obj as Character;
			Skill.Def def = obj2.game.defs.Get<Skill.Def>(skill_def);
			Skill skill = obj2.GetSkill(def.name);
			obj2.AddSkillRank(skill);
		}
	}

	[Serialization.Event(31)]
	public class ChangeClassEvent : Serialization.ObjectEvent
	{
		public string new_class;

		public ChangeClassEvent()
		{
		}

		public static ChangeClassEvent Create()
		{
			return new ChangeClassEvent();
		}

		public ChangeClassEvent(string new_class)
		{
			this.new_class = new_class;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(new_class, "class");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			new_class = ser.ReadStr("class");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Character).SetClass(new_class);
		}
	}

	[Serialization.Event(32)]
	public class ChangeNameEvent : Serialization.ObjectEvent
	{
		private string name;

		public ChangeNameEvent()
		{
		}

		public static ChangeNameEvent Create()
		{
			return new ChangeNameEvent();
		}

		public ChangeNameEvent(string name)
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
			(obj as Character).ChangeName(name);
		}
	}

	public List<KingdomAI.GovernOption> govern_options;

	public int cur_govern_option_idx = -1;

	public string title = "Knight";

	public string Name;

	public string CustomName;

	private int _name_idx;

	public int kingdom_id;

	public int realm_id;

	private MapObject location;

	public Kingdom prison_kingdom;

	public string prison_reason;

	public Time prison_time;

	public Kingdom mission_kingdom;

	public Realm mission_realm;

	public bool select_army_on_spawn;

	public bool historical_figure;

	public Castle governed_castle;

	public Castle governed_castle_banished_from;

	public int original_kingdom_id;

	private int special_court_kingdom_id;

	public Sex sex;

	public Age age = Age.Young;

	public Ethnicity ethnicity;

	public int royalBloodPower;

	public Marriage marriage;

	public int last_portraitID = -1;

	public int last_variantID = -1;

	public int last_portrait_age = -1;

	public int portraitID = -1;

	public int portrait_age;

	public int portrait_variantID = -1;

	public int VO_actor_id = -100;

	public CharacterClass.Def class_def;

	public CharacterClass.Def prefered_class_def;

	public GovernModifiers.Def governModifiers;

	public Actions actions;

	public List<string> additional_titles;

	public List<Skill> skills;

	public List<Skill.Def> new_skills;

	public SkillMods skill_mods;

	public float hire_cost;

	private List<RoyalAbility> royal_abilities;

	public Stats stats;

	public Rebel.Def rebel_def;

	public FamousPerson.Def famous_def;

	public int trade_level;

	public Time trade_level_time;

	public List<ImportedGood> importing_goods;

	public Pact pact;

	public List<Character> puppets;

	public List<Character> masters;

	public List<int> revealed_in_kingdoms;

	public int reveal_kingdom;

	public Character reveal_master;

	public Religion.PaganBelief paganBelief;

	public Time next_age_check;

	public Time next_govern_check;

	public Time next_die_check;

	public Time next_prison_check;

	public Time next_owner_prison_check;

	public Time ai_renounce_time;

	public bool can_die;

	private static List<SkillReplacement> tmp_skill_replace = new List<SkillReplacement>(50);

	private bool auto_statuses_valid;

	private static bool inUpdateAutomaticStatuses = false;

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public KingdomAI.GovernOption ai_selected_govern_option
	{
		get
		{
			if (govern_options != null && cur_govern_option_idx >= 0 && cur_govern_option_idx < govern_options.Count)
			{
				return govern_options[cur_govern_option_idx];
			}
			return default(KingdomAI.GovernOption);
		}
	}

	public int name_idx
	{
		get
		{
			if (!string.IsNullOrEmpty(CustomName))
			{
				return 0;
			}
			return _name_idx;
		}
		set
		{
			_name_idx = value;
		}
	}

	public string class_name
	{
		get
		{
			if (class_def != null && !class_def.IsBase())
			{
				return class_def.id;
			}
			return null;
		}
	}

	public Action cur_action => actions?.current;

	public Status status => GetStatus();

	public string class_title
	{
		get
		{
			Kingdom kingdom = GetSpecialCourtKingdom() ?? GetKingdom();
			if (kingdom != null && kingdom.religion != null)
			{
				string text = kingdom.religion.GetTitle(this);
				if (text != null)
				{
					return text;
				}
			}
			return class_name + ".name";
		}
	}

	public void ClearGovernOptions()
	{
		if (govern_options == null)
		{
			govern_options = new List<KingdomAI.GovernOption>();
		}
		else
		{
			govern_options.Clear();
		}
		cur_govern_option_idx = -1;
	}

	public void SetPact(Pact pact, bool send_state = true)
	{
		Pact param = this.pact;
		this.pact = pact;
		NotifyListeners("pact_changed", param);
	}

	public void DisolvePact(string reason = null)
	{
		if (pact != null)
		{
			pact.Dissolve(reason);
		}
	}

	public void DestroyPact()
	{
		if (pact != null)
		{
			pact.Destroy();
			pact = null;
		}
	}

	public void FillDeadVars(Vars vars, bool is_owner = false)
	{
		if (is_owner)
		{
			vars.Set("owner", GetNameKey(null, ""));
		}
		else
		{
			vars.Set("target", GetNameKey(null, ""));
		}
		vars.Set("class_title", class_title);
		vars.Set("title", GetTitle());
		vars.Set("title_key", title);
		vars.Set("name", GetName());
		if (name_idx > 0)
		{
			vars.Set("name_idx", name_idx);
		}
		if (this?.actions?.current?.def?.field != null)
		{
			vars.Set("action", actions.current.def.field.key);
		}
		if (this?.statuses?.main?.def?.field != null)
		{
			vars.Set("status", statuses.main.def.field.key);
		}
		Army army = GetArmy();
		if (army != null)
		{
			vars.Set("army", army);
		}
	}

	public Character(Game game, int kingdom_id, int original_kingdom_id, CharacterClass.Def class_def, bool historical_figure)
		: base(game)
	{
		title = "Knight";
		this.kingdom_id = kingdom_id;
		this.original_kingdom_id = original_kingdom_id;
		this.historical_figure = historical_figure;
		SetClass(class_def, send_state: false);
		stats?.SetKingdom(GetKingdom());
		if (kingdom_id == 0)
		{
			Warning("no kingdom");
		}
	}

	public Character Clone()
	{
		Character character = new Character(game, kingdom_id, original_kingdom_id, GetClass(), historical_figure);
		character.title = title;
		character.Name = Name;
		character.name_idx = name_idx;
		character.realm_id = realm_id;
		character.prison_kingdom = prison_kingdom;
		character.prison_reason = prison_reason;
		character.prison_time = prison_time;
		character.mission_kingdom = mission_kingdom;
		character.mission_realm = mission_realm;
		character.governed_castle = governed_castle;
		character.sex = sex;
		character.age = age;
		character.ethnicity = ethnicity;
		character.portraitID = portraitID;
		character.portrait_variantID = portrait_variantID;
		character.portrait_age = portrait_age;
		character.last_portrait_age = last_portrait_age;
		character.last_portraitID = last_portraitID;
		character.last_variantID = last_variantID;
		character.VO_actor_id = VO_actor_id;
		character.governModifiers = governModifiers;
		if (additional_titles != null)
		{
			character.additional_titles = new List<string>(additional_titles);
		}
		if (skills != null)
		{
			for (int i = 0; i < skills.Count; i++)
			{
				if (skills[i] != null)
				{
					character.AddSkill(skills[i].def);
				}
			}
		}
		if (new_skills != null)
		{
			character.new_skills = new List<Skill.Def>();
			for (int j = 0; j < new_skills.Count; j++)
			{
				character.new_skills.Add(new_skills[j]);
			}
		}
		character.rebel_def = rebel_def;
		character.famous_def = famous_def;
		if (puppets != null)
		{
			character.puppets = new List<Character>(puppets);
		}
		if (masters != null)
		{
			character.masters = new List<Character>(masters);
		}
		if (revealed_in_kingdoms != null)
		{
			character.revealed_in_kingdoms = new List<int>(revealed_in_kingdoms);
		}
		character.reveal_kingdom = reveal_kingdom;
		character.reveal_master = reveal_master;
		character.next_age_check = next_age_check;
		character.next_govern_check = next_govern_check;
		character.next_die_check = next_die_check;
		character.next_prison_check = next_prison_check;
		character.next_owner_prison_check = next_owner_prison_check;
		character.ai_renounce_time = ai_renounce_time;
		character.can_die = can_die;
		return character;
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public bool IsKing()
	{
		return title == "King";
	}

	public bool IsPrince()
	{
		if (title != "Prince")
		{
			return false;
		}
		return (GetKingdom()?.royalFamily)?.IsFamilyMember(this) ?? false;
	}

	public bool IsRebelPrince()
	{
		if (!IsRebel())
		{
			return false;
		}
		if (FindStatus<HasRoyalBloodStatus>() != null)
		{
			return true;
		}
		return false;
	}

	public bool IsKingOrPrince()
	{
		if (!IsKing())
		{
			return IsPrince();
		}
		return true;
	}

	public bool IsClasslessPrince()
	{
		return class_name == "ClasslessPrince";
	}

	public bool IsMarshal()
	{
		if (class_def != null)
		{
			return class_def.name == "Marshal";
		}
		return false;
	}

	public bool IsDiplomat()
	{
		if (class_def != null)
		{
			return class_def.name == "Diplomat";
		}
		return false;
	}

	public bool IsCleric()
	{
		if (class_def != null)
		{
			return class_def.name == "Cleric";
		}
		return false;
	}

	public bool IsMerchant()
	{
		if (class_def != null)
		{
			return class_def.name == "Merchant";
		}
		return false;
	}

	public bool IsSpy()
	{
		if (class_def != null)
		{
			return class_def.name == "Spy";
		}
		return false;
	}

	public bool IsCrusader()
	{
		return game.religions?.catholic?.crusade?.leader == this;
	}

	public bool IsPope()
	{
		return game.religions?.catholic?.head == this;
	}

	public bool IsPatriarch()
	{
		if (GetKingdom() == null || !GetKingdom().IsRegular())
		{
			return GetSpecialCourtKingdom()?.patriarch == this;
		}
		return GetKingdom().patriarch == this;
	}

	public bool IsEcumenicalPatriarch()
	{
		return game?.religions?.orthodox?.head == this;
	}

	public bool IsCaliph()
	{
		if (IsKing())
		{
			return GetKingdom().IsCaliphate();
		}
		return false;
	}

	public bool IsIdle()
	{
		if (status != null)
		{
			return status.IsIdle();
		}
		return true;
	}

	public bool IsExile()
	{
		return GetKingdom().type == Kingdom.Type.Exile;
	}

	public bool IsRebel()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			if (kingdom.type != Kingdom.Type.RebelFaction && kingdom.type != Kingdom.Type.LoyalistsFaction)
			{
				return kingdom.type == Kingdom.Type.ReligiousFaction;
			}
			return true;
		}
		return false;
	}

	public bool IsRebelLeader()
	{
		return GetArmy()?.rebel?.rebellion?.leader?.character == this;
	}

	public bool IsRebelGeneral()
	{
		return (GetArmy()?.rebel)?.def.IsGeneral() ?? false;
	}

	public bool IsMercenary()
	{
		return title == "MercenaryLeader";
	}

	public bool IsGovernor()
	{
		Realm realm = governed_castle?.GetRealm();
		if (realm != null && !realm.IsOccupied())
		{
			return !realm.IsDisorder();
		}
		return false;
	}

	public bool IsBanishedGovernor()
	{
		return governed_castle_banished_from != null;
	}

	public bool IsPrisoner()
	{
		return prison_kingdom != null;
	}

	public bool IsFamous()
	{
		return famous_def != null;
	}

	public bool IsQueen()
	{
		return title == "Queen";
	}

	public bool IsPrincess()
	{
		return title == "Princess";
	}

	public bool IsQueenOrPrincess()
	{
		if (!IsQueen())
		{
			return IsPrincess();
		}
		return true;
	}

	public bool IsRoyalty()
	{
		if (!IsKing() && !IsPrince() && !IsQueen())
		{
			return IsPrincess();
		}
		return true;
	}

	public bool IsRoyalChild()
	{
		return (GetOriginalKingdom()?.royalFamily?.Children)?.Contains(this) ?? false;
	}

	public bool IsRoyalRelative()
	{
		Kingdom kingdom = GetKingdom();
		if (IsRebel())
		{
			kingdom = game.GetKingdom(original_kingdom_id);
		}
		if (kingdom?.royalFamily?.Relatives == null)
		{
			return false;
		}
		return kingdom.royalFamily.Relatives.Contains(this);
	}

	public bool IsPretenderToTheThrone(Kingdom loyalTo = null)
	{
		if (loyalTo != null && GetKingdom()?.royalFamily?.GetCrownPretenderKingdomLoyalTo() != loyalTo)
		{
			return false;
		}
		return GetKingdom()?.royalFamily?.GetCrownPretender() == this;
	}

	public Kingdom GetPretenderKingdomLoyalTo()
	{
		return GetKingdom()?.royalFamily?.GetCrownPretenderKingdomLoyalTo();
	}

	public bool IsCardinal()
	{
		if (!(title == "Cardinal"))
		{
			return IsCardinalElect();
		}
		return true;
	}

	public bool IsCardinalElect()
	{
		return game?.religions?.catholic?.cardinals?.Contains(this) ?? false;
	}

	public bool IsCardinalOrPope()
	{
		if (!IsCardinal())
		{
			return IsPope();
		}
		return true;
	}

	public int GetClericRank()
	{
		if (!IsCleric())
		{
			return 0;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return 1;
		}
		if (kingdom.is_catholic)
		{
			if (IsPope())
			{
				return 4;
			}
			if (IsCardinalElect())
			{
				return 3;
			}
			if (title == "Cardinal")
			{
				return 2;
			}
			return 1;
		}
		if (kingdom.is_orthodox)
		{
			if (IsEcumenicalPatriarch())
			{
				return 4;
			}
			if (IsPatriarch())
			{
				return 3;
			}
			return 1;
		}
		return 1;
	}

	public Character GetFather()
	{
		if (IsPrince() || IsPrincess())
		{
			return GetKingdom()?.GetKing();
		}
		return null;
	}

	public bool IsInCourt()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || kingdom.court == null)
		{
			return false;
		}
		return kingdom.court.Contains(this);
	}

	public bool IsInLocalPlayerCourt()
	{
		return game.GetLocalPlayerKingdom()?.court.Contains(this) ?? false;
	}

	public bool IsInLocalPlayerSpecialCourt()
	{
		return game.GetLocalPlayerKingdom()?.special_court.Contains(this) ?? false;
	}

	public bool IsPrisonForgivable()
	{
		Kingdom kingdom = prison_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.royalFamily.IsFamilyMember(this))
		{
			return true;
		}
		if (kingdom.royalFamily.IsRelative(this))
		{
			return true;
		}
		if (IsInCourt(kingdom))
		{
			return true;
		}
		if (IsInSpecialCourt(kingdom))
		{
			return true;
		}
		return false;
	}

	private bool IsImportantRelative()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.id != original_kingdom_id && original_kingdom_id != 0)
		{
			kingdom = game.GetKingdom(original_kingdom_id);
		}
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.royalFamily == null)
		{
			return false;
		}
		return kingdom.royalFamily.IsRelative(this);
	}

	private bool IsInLocalImportantRelative()
	{
		return (GetKingdom()?.royalFamily?.Relatives)?.Contains(this) ?? false;
	}

	public bool IsInCourt(Kingdom k)
	{
		if (k == null || k.court == null)
		{
			return false;
		}
		return k.court.Contains(this);
	}

	public bool IsInSpecialCourt()
	{
		return special_court_kingdom_id != 0;
	}

	public bool IsInSpecialCourt(Kingdom k)
	{
		return special_court_kingdom_id == (k?.id ?? 0);
	}

	public bool IsWidowed()
	{
		return FindStatus<WidowedStatus>() != null;
	}

	public bool CanMarry()
	{
		if (IsDead())
		{
			return false;
		}
		if (age < Age.Juvenile)
		{
			return false;
		}
		if (IsEcumenicalPatriarch())
		{
			return false;
		}
		if (IsPope())
		{
			return false;
		}
		if (IsPatriarch())
		{
			return false;
		}
		if (IsCardinal())
		{
			return false;
		}
		if (GetKingdom() == game.religions.catholic.hq_kingdom)
		{
			return false;
		}
		if (IsMarried())
		{
			return false;
		}
		if (IsRebel())
		{
			return false;
		}
		return true;
	}

	public bool IsHeir()
	{
		if (!IsAlive())
		{
			return false;
		}
		return GetKingdom()?.royalFamily?.Heir == this;
	}

	public bool IsOnlyHeir()
	{
		if (!IsHeir())
		{
			return false;
		}
		List<Character> list = GetKingdom()?.royalFamily?.Children;
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Character character = list[i];
			if (character != this && character.CanBeHeir())
			{
				return false;
			}
		}
		return true;
	}

	public bool CanBeHeir()
	{
		return GetKingdom()?.royalFamily?.CanBeHair(this) ?? false;
	}

	public bool CanLeadArmy()
	{
		if (IsPope())
		{
			return false;
		}
		if (actions == null)
		{
			return false;
		}
		Action action = actions.Find("GatherArmyAction");
		if (action != null && action.ValidateSkillOrClassDependancies() == "ok")
		{
			return true;
		}
		return false;
	}

	public Marriage GetWidowedMarriage()
	{
		return FindStatus<WidowedStatus>()?.marriage;
	}

	public Character GetWidowedSpouse()
	{
		return GetWidowedMarriage()?.GetSpouse(this);
	}

	public bool IsMarried()
	{
		return GetMarriage() != null;
	}

	public Marriage GetMarriage()
	{
		return marriage;
	}

	public Character GetSpouse()
	{
		return GetMarriage()?.GetSpouse(this);
	}

	public Kingdom GetOriginalKingdom()
	{
		return game.GetKingdom(original_kingdom_id);
	}

	public Kingdom GetPrincessKingdom()
	{
		if (sex != Sex.Female)
		{
			return null;
		}
		return GetMarriage()?.kingdom_wife;
	}

	public void Divorce(string reason = null)
	{
		Marriage marriage = GetMarriage();
		if (marriage == null || !IsAuthority() || game.IsUnloadingMap())
		{
			return;
		}
		Kingdom otherKingdom = marriage.GetOtherKingdom(this);
		if (otherKingdom == null)
		{
			return;
		}
		Character husband = marriage.husband;
		Character wife = marriage.wife;
		Kingdom kingdom_husband = marriage.kingdom_husband;
		Kingdom kingdom_wife = marriage.kingdom_wife;
		if (husband != null)
		{
			husband.DelStatus<MarriedStatus>();
			husband.marriage = null;
			husband.SendState<MarriageState>();
			kingdom_husband.marriages.Remove(marriage);
			kingdom_husband.SendState<Kingdom.MarriageStates>();
		}
		if (wife != null)
		{
			wife.DelStatus<MarriedStatus>();
			wife.marriage = null;
			wife.SendState<MarriageState>();
			kingdom_wife.marriages.Remove(marriage);
			kingdom_wife.SendState<Kingdom.MarriageStates>();
			Inheritance component = kingdom_wife.GetComponent<Inheritance>();
			component?.princesses?.Remove(wife);
			if (component?.currentPrincess == wife)
			{
				component.HandleNextPrincess();
			}
		}
		if (husband != null && wife != null && kingdom_husband.GetRoyalMarriage(kingdom_wife))
		{
			bool flag = false;
			Kingdom kingdom = GetKingdom();
			if (kingdom != null && kingdom.marriages != null)
			{
				for (int i = 0; i < kingdom.marriages.Count; i++)
				{
					Marriage marriage2 = kingdom.marriages[i];
					if ((marriage2.kingdom_wife == otherKingdom || marriage2.kingdom_husband == otherKingdom) && !kingdom.marriages[i].Has(this))
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				kingdom_husband.UnsetStance(kingdom_wife, RelationUtils.Stance.Marriage);
			}
		}
		if (reason == "death" || reason == "destroy")
		{
			if (sex == Sex.Male && wife != null && wife.IsValid())
			{
				if (wife.IsRoyalChild())
				{
					WidowedStatus widowedStatus = WidowedStatus.Create(marriage);
					if (widowedStatus != null)
					{
						wife.AddStatus(widowedStatus);
					}
					Vars vars = new Vars();
					vars.SetVar("widow", wife);
					vars.SetVar("marriage_kingdom", kingdom_husband);
					kingdom_wife.FireEvent("wife_widowed", vars, kingdom_wife.id);
					wife.SetTitle("Princess");
					wife.SetKingdom(wife.original_kingdom_id);
				}
				else
				{
					wife.Die();
					marriage.Destroy();
				}
			}
			else
			{
				marriage.Destroy();
			}
		}
		else
		{
			marriage.Destroy();
		}
		Vars vars2 = new Vars();
		vars2.SetVar("husband", husband);
		vars2.SetVar("husband_kingdom", kingdom_husband);
		vars2.SetVar("wife", wife);
		vars2.SetVar("wife_kingdom", kingdom_wife);
		kingdom_husband.FireEvent("charcter_divorce", vars2);
		kingdom_wife.FireEvent("charcter_divorce", vars2);
		husband?.NotifyListeners("divorce", vars2);
		wife?.NotifyListeners("divorce", vars2);
	}

	public KingdomAI.Expense.Category GetExpenseCategory()
	{
		if (class_def != null && class_def.ai_category != KingdomAI.Expense.Category.None)
		{
			return class_def.ai_category;
		}
		return KingdomAI.Expense.Category.Economy;
	}

	public int GetClassLevel()
	{
		return GetClassLevel(class_def);
	}

	public float GetClassLevelNormalized()
	{
		return GetClassLevel(class_def) / GetMaxClassLevel();
	}

	public int GetClassLevel(string character_class, float mul_if_not_that_class = 1f)
	{
		return GetClassLevel(game.defs.Find<CharacterClass.Def>(character_class), mul_if_not_that_class);
	}

	public int GetClassLevel(CharacterClass.Def character_class, float mul_if_not_that_class = 1f)
	{
		int num = 0;
		IsKing();
		if (skills != null)
		{
			for (int i = 0; i < skills.Count; i++)
			{
				Skill skill = skills[i];
				if (skill?.def != null)
				{
					int classLevelValue = skill.def.GetClassLevelValue(character_class);
					if (classLevelValue > 0)
					{
						int skillRank = GetSkillRank(skill);
						num += classLevelValue * skillRank;
					}
				}
			}
		}
		if (class_def != character_class)
		{
			num = (int)((float)num * mul_if_not_that_class + 0.5f);
		}
		return num;
	}

	public int GetMaxClassLevel()
	{
		int num = (game?.defs?.GetBase<Skill.Def>())?.max_rank ?? 3;
		return NumSkillSlots() * num;
	}

	public bool IsMaxClassLevel()
	{
		return GetMaxClassLevel() == GetClassLevel();
	}

	public bool CanImproveSkills()
	{
		if (!IsMaxClassLevel() && IsAlive())
		{
			return !IsPrisoner();
		}
		return false;
	}

	public float PrisonerValue()
	{
		return PrisonerValue(class_def);
	}

	public float PrisonerValue(CharacterClass.Def class_def)
	{
		RoyalDungeon.Def def = GetKingdom()?.royal_dungeon?.def;
		if (def == null)
		{
			def = game.defs.Get<RoyalDungeon.Def>("RoyalDungeon");
		}
		if (def == null)
		{
			return 1f;
		}
		float def_val = 0f;
		Vars vars = new Vars(this);
		vars.Set("class_level", (float)GetClassLevel(class_def));
		if (IsPope())
		{
			vars.Set("religious_position", def.prisoner_position_pope);
		}
		else if (IsCardinal())
		{
			vars.Set("religious_position", def.prisoner_position_cardinal);
		}
		else if (IsEcumenicalPatriarch())
		{
			vars.Set("religious_position", def.prisoner_position_ecumenical_patriarch);
		}
		else if (IsPatriarch())
		{
			vars.Set("religious_position", def.prisoner_position_patriarch);
		}
		else if (IsCaliph())
		{
			vars.Set("religious_position", def.prisoner_position_caliph);
		}
		else
		{
			vars.Set("religious_position", 1f);
		}
		switch (title)
		{
		case "King":
		case "Queen":
			vars.Set("royalty_position", def.prisoner_position_king);
			break;
		case "Prince":
		case "Princess":
			vars.Set("royalty_position", def.prisoner_position_prince);
			break;
		case "Knight":
			vars.Set("royalty_position", def.prisoner_position_knight);
			break;
		default:
			vars.Set("royalty_position", 1f);
			break;
		}
		switch (age)
		{
		case Age.Old:
			vars.Set("age", def.prisoner_age_old);
			break;
		case Age.Venerable:
			vars.Set("age", def.prisoner_age_venerable);
			break;
		default:
			vars.Set("age", 1f);
			break;
		}
		return def.field.GetFloat("ransom", vars, def_val);
	}

	public Resource RansomPrice()
	{
		return RansomPrice(class_def);
	}

	public Resource RansomPrice(CharacterClass.Def class_def)
	{
		Resource resource = new Resource();
		if (prison_kingdom == null)
		{
			return resource;
		}
		Kingdom kingdom = prison_kingdom;
		Kingdom kingdom2 = GetKingdom();
		if (kingdom2 == null)
		{
			return resource;
		}
		Vars vars = new Vars();
		vars.Set("prisoner", this);
		vars.Set("relation", kingdom2.GetRelationship(kingdom));
		float num = kingdom.royal_dungeon.def.field.GetFloat("ransom_price", vars);
		float num2 = kingdom2.GetStat(Stats.ks_ransom_cost_perc) / 100f;
		float num3 = num * (1f + num2);
		resource.Add(ResourceType.Gold, (int)num3);
		return resource;
	}

	public Kingdom GetLocationKingdom()
	{
		if (prison_kingdom != null)
		{
			return prison_kingdom;
		}
		if (location != null)
		{
			if (location is Army army)
			{
				return army.realm_in?.GetKingdom();
			}
			return location.GetKingdom();
		}
		if (mission_kingdom != null)
		{
			return mission_kingdom;
		}
		return GetKingdom();
	}

	public void SetRebel(Rebel.Def rebel_def, bool send_state = true)
	{
		this.rebel_def = rebel_def;
		if (send_state)
		{
			SendState<RebelState>();
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "estimation_time":
		{
			Army army = GetArmy();
			if (army == null)
			{
				return -1;
			}
			Battle last_intended_battle = army.last_intended_battle;
			if (last_intended_battle == null)
			{
				return -1;
			}
			if (last_intended_battle.batte_view_game != null)
			{
				Battle.Reinforcement reinforcement = last_intended_battle.FindReinforcement(army);
				if (reinforcement?.timer != null)
				{
					return reinforcement.timer.Get();
				}
			}
			return last_intended_battle.CalcReinforcementTime(army);
		}
		case "estimation_time_real":
		{
			Army army2 = GetArmy();
			if (army2 == null)
			{
				return -1;
			}
			Battle last_intended_battle2 = army2.last_intended_battle;
			if (last_intended_battle2 == null)
			{
				return -1;
			}
			if (last_intended_battle2.batte_view_game != null)
			{
				Battle.Reinforcement reinforcement2 = last_intended_battle2.FindReinforcement(army2);
				if (reinforcement2?.timer != null)
				{
					return reinforcement2.timer.Get();
				}
			}
			if (army2?.movement?.path == null)
			{
				return last_intended_battle2.CalcReinforcementTime(army2);
			}
			return (army2.movement.path.path_len - army2.movement.path.t) / army2.movement.modified_speed;
		}
		case "is_alive":
			return IsAlive();
		case "is_dead":
			return IsDead();
		case "is_king":
			return IsKing();
		case "is_queen":
			return IsQueen();
		case "is_prince":
			return IsPrince();
		case "is_rebel_prince":
			return IsRebelPrince();
		case "is_princess":
			return IsPrincess();
		case "is_queen_or_princess":
			return IsQueenOrPrincess();
		case "is_king_or_prince":
			return IsKingOrPrince();
		case "is_royal_family":
			return IsKingOrPrince() || IsQueenOrPrincess();
		case "is_heir":
			return IsHeir();
		case "is_only_heir":
			return IsOnlyHeir();
		case "is_heir_eligable":
			return CanBeHeir();
		case "is_royal_child":
			return IsPrince() || IsPrincess();
		case "is_pretender_to_the_throne":
			return IsPretenderToTheThrone();
		case "pretender_kingdom_loyal_to":
			return GetPretenderKingdomLoyalTo();
		case "sex":
			return sex.ToString();
		case "is_male":
			return sex == Sex.Male;
		case "age":
			return "Character.age." + age;
		case "character_age":
			return age.ToString();
		case "is_infant":
			return age == Age.Infant;
		case "is_at_least_infant":
			return age >= Age.Infant;
		case "is_juvenile":
			return age == Age.Juvenile;
		case "is_at_least_juvenile":
			return age >= Age.Juvenile;
		case "is_young":
			return age == Age.Young;
		case "is_at_least_young":
			return age >= Age.Young;
		case "is_adult":
			return age == Age.Adult;
		case "is_at_least_adult":
			return age >= Age.Adult;
		case "is_old":
			return age == Age.Old;
		case "is_at_least_old":
			return age >= Age.Old;
		case "is_venerable":
			return age == Age.Venerable;
		case "prisoner_value":
			return PrisonerValue();
		case "ransom_price":
			return RansomPrice();
		case "ransom_price_gold":
			return RansomPrice().Get(ResourceType.Gold);
		case "kingdom":
			return GetKingdom();
		case "king_abilities":
			return new Value(royal_abilities);
		case "class_level":
			return GetClassLevel();
		case "max_class_level":
			return GetMaxClassLevel();
		case "is_max_class_level":
			return IsMaxClassLevel();
		case "class_name":
			return class_name;
		case "name":
			return GetName();
		case "name_str":
			return Name;
		case "hire_cost":
			return hire_cost;
		case "hire_cost_int":
			return (int)hire_cost;
		case "title":
			return GetTitle();
		case "title_keyword":
			return GetTitleKeyword();
		case "disbanding_army":
			return cur_action is DisbandArmyAction;
		case "name_idx":
			if (name_idx > 0)
			{
				return name_idx;
			}
			return Value.Null;
		case "is_marshal":
			return IsMarshal();
		case "is_merchant":
			return IsMerchant();
		case "is_diplomat":
			return IsDiplomat();
		case "is_spy":
			return IsSpy();
		case "is_cleric":
			return IsCleric();
		case "is_cardinal":
			return IsCardinal();
		case "is_cardinal_elect":
			return IsCardinalElect();
		case "cleric_rank":
			return GetClericRank();
		case "class_icon":
			return (class_def?.field?.FindChild("icon"))?.value ?? Value.Null;
		case "SkillCount":
			return GetSkillsCount();
		case "army":
			return GetArmy();
		case "battle":
			return GetArmy()?.battle;
		case "castle_in":
			return GetCastleIn();
		case "governed_castle":
			return GetGovernedCastle();
		case "preparing_to_govern":
			return GetPreparingToGovernCastle();
		case "govern_target":
			return GovernTarget();
		case "governed_castle_banished_from":
			return GetGovernedCastleBanishedFrom();
		case "mission_kingdom":
			return mission_kingdom ?? mission_realm?.GetKingdom();
		case "mission_realm":
			return mission_realm;
		case "prison_kingdom":
			return prison_kingdom;
		case "realm_in":
			return GetArmy()?.realm_in;
		case "kingdom_in":
			return GetLocationKingdom();
		case "original_kingdom":
			return GetOriginalKingdom();
		case "special_court_kingdom":
			return GetSpecialCourtKingdom();
		case "actions":
			return actions;
		case "opportunities":
			return new Value(actions?.opportunities);
		case "cur_action":
			return new Value(cur_action);
		case "total_trade_gold_gain":
			return GetTradeProfit() + CalcExportedFoodGoldGain() - CalcImportedFoodGoldUpkeep() - CalcImportedGoodsGoldUpkeep();
		case "total_trade_commerce":
			return GetTradeCommerce() + CalcImportedGoodsCommerce() + CalcImportedFoodCommerce() + CalcExportedFoodCommerce();
		case "trade_profit":
			return GetTradeProfit();
		case "trade_level":
			return trade_level;
		case "trade_level_text":
			return $"Economy.trade.level_{trade_level}.name";
		case "trade_level_passed_time":
			return (float)Math.Round(game.time - trade_level_time);
		case "trade_commerce":
			return GetTradeCommerce();
		case "import_goods_commerce":
			return CalcImportedGoodsCommerce();
		case "import_goods_gold_upkeep":
			return CalcImportedGoodsGoldUpkeep();
		case "import_food_commerce":
			return CalcImportedFoodCommerce();
		case "import_food_gold_upkeep":
			return CalcImportedFoodGoldUpkeep();
		case "export_food_commerce":
			return CalcExportedFoodCommerce();
		case "export_food_gold_gain":
			return CalcExportedFoodGoldGain();
		case "foreign_trade_profit":
			return GetForeignTradeProfit();
		case "if_favorite":
			return (mission_kingdom?.favoriteDiplomat == this) ? "" : null;
		case "if_fascinating":
			return (mission_kingdom?.royalFamily?.SpouseFacination == this) ? "" : null;
		case "religion":
			return GetKingdom()?.religion;
		case "is_pope":
			return game.religions.catholic.head == this;
		case "is_patriarch":
			return GetKingdom()?.patriarch == this;
		case "is_ecumenical_patriarch":
			return game.religions.orthodox.head == this;
		case "help_the_weak_upkeep":
			return CalcHelpTheWeakUpkeep();
		case "piety_icon":
			return GetKingdom()?.GetPietyIcon();
		case "puppets":
			return new Value(puppets);
		case "num_puppets":
			return (puppets != null) ? puppets.Count : 0;
		case "has_puppet_pope":
			return HasPuppetPope();
		case "masters":
			return new Value(masters);
		case "num_masters":
			return (masters != null) ? masters.Count : 0;
		case "revealed_in":
			return new Value(revealed_in_kingdoms);
		case "reveal_kingdom":
			return game.GetKingdom(reveal_kingdom);
		case "reveal_master":
			return reveal_master;
		case "marshal_level":
			return GetClassLevel("Marshal");
		case "spouse":
			return GetSpouse();
		case "widowed_spouse":
			return GetWidowedSpouse();
		case "is_widowed":
			return IsWidowed();
		case "kingdom_before_marriage":
			return GetOriginalKingdom();
		case "can_learn_new_skill":
			return CanLearnNewSkills();
		case "is_crusader":
			return IsCrusader();
		case "crusade":
		{
			if (!IsCrusader())
			{
				return Value.Unknown;
			}
			Crusade crusade = game.religions.catholic.crusade;
			if (crusade == null)
			{
				return Value.Unknown;
			}
			return crusade;
		}
		case "rebellion":
			return GetArmy()?.rebel?.rebellion;
		case "rebel_loyal_to_kingdom":
			return GetArmy()?.rebel?.rebellion?.GetLoyalTo();
		case "is_married":
			return IsMarried();
		case "is_prisoner":
			return IsPrisoner();
		case "is_exile":
			return IsExile();
		case "is_exiled":
			return IsExile();
		case "is_rebel":
			return IsRebel();
		case "is_rebel_leader":
			return IsRebelLeader();
		case "is_rebel_general":
			return IsRebelGeneral();
		case "is_in_court":
			return IsInCourt();
		case "is_important_relative":
			return IsImportantRelative();
		case "is_mercenary":
			return IsMercenary();
		case "pact":
			return pact;
		case "warfare_ability":
			return GetRoyalAbility("Warfare");
		case "economy_ability":
			return GetRoyalAbility("Economy");
		case "diplomacy_ability":
			return GetRoyalAbility("Diplomacy");
		case "religion_ability":
			return GetRoyalAbility("Religion");
		case "espionage_ability":
			return GetRoyalAbility("Espionage");
		case "king_warfare_ability":
			return GetKingAbility("Warfare");
		case "king_economy_ability":
			return GetKingAbility("Economy");
		case "king_diplomacy_ability":
			return GetKingAbility("Diplomacy");
		case "king_religion_ability":
			return GetKingAbility("Religion");
		case "king_espionage_ability":
			return GetKingAbility("Espionage");
		case "is_in_local_player_court":
			return IsInLocalPlayerCourt();
		case "is_in_local_player_special_court":
			return IsInLocalPlayerSpecialCourt();
		case "is_local_player_forgivable":
			return IsPrisonForgivable();
		case "rebel":
			return GetArmy()?.rebel;
		case "is_trading":
			return IsTrading();
		case "is_improving_relations":
			return IsImprovingRelations();
		case "is_on_spy_mission":
			return IsSpy() && mission_kingdom != null;
		case "is_importing_goods":
			return IsImportingGoods();
		case "is_importing_food":
			return IsImportingFood();
		case "is_exporting_food":
			return IsExportingFood();
		case "is_having_puppets":
			return puppets != null && puppets.Count > 0;
		case "is_on_a_journey":
			return cur_action is GoOnAJourneyAction;
		default:
		{
			Value var = stats.GetVar(key, vars, as_value);
			if (var != Value.Unknown)
			{
				return var;
			}
			CharacterClass.Def def = game.defs.Find<CharacterClass.Def>(key);
			if (def != null)
			{
				return class_def == def;
			}
			Action.Def def2 = game.defs.Find<Action.Def>(key);
			if (def2 != null)
			{
				return new Value(Action.Find(this, def2));
			}
			Skill.Def def3 = game.defs.Find<Skill.Def>(key);
			if (def3 != null)
			{
				return GetSkillRank(def3.name);
			}
			Value tagValue = GetTagValue(key);
			if (tagValue != Value.Unknown)
			{
				return tagValue;
			}
			return base.GetVar(key, vars, as_value);
		}
		}
	}

	public void SetKingdom(int k_id, bool send_state = true)
	{
		if (!(!IsAuthority() && send_state))
		{
			kingdom_id = k_id;
			stats.SetKingdom(GetKingdom());
			if (send_state)
			{
				SendState<KingdomState>();
			}
			NotifyListeners("kingdom_changed");
		}
	}

	public override string ToString()
	{
		string text = "[" + NID.ToString(nid) + "]";
		if (!IsValid())
		{
			text += $"[{base.obj_state}]";
		}
		else if (IsDead())
		{
			text += "[Dead]";
		}
		if (title != null && title != "Knight")
		{
			text = text + title + " ";
		}
		else if (IsEcumenicalPatriarch())
		{
			text += "Ecu Patriarch ";
		}
		else if (IsPatriarch())
		{
			text += "Patriarch ";
		}
		text += ((class_def == null) ? "<no_class>" : class_def.name);
		text = text + " " + Name;
		if (name_idx > 0)
		{
			text += $"#{name_idx}";
		}
		Kingdom kingdom = game?.GetKingdom(kingdom_id);
		return text + " (" + kingdom_id + ":" + ((kingdom == null) ? "<no_kingdom>" : kingdom.Name) + ")";
	}

	public float CalcHelpTheWeakUpkeep()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom.IsDefeated() || kingdom.realms.Count == 0)
		{
			return 0f;
		}
		DT.Field def = kingdom.realms[0].def;
		float num = 0f;
		if (def != null)
		{
			num += def.GetFloat("help_the_weak_upkeep") * (float)kingdom.realms.Count;
		}
		return num + kingdom.def.GetFloat("help_the_weak_upkeep");
	}

	public bool IsTrading()
	{
		if (IsMerchant())
		{
			return mission_kingdom != null;
		}
		return false;
	}

	public bool IsImprovingRelations()
	{
		return FindStatus<ImprovingRelationsStatus>() != null;
	}

	public Resource CalcTotalMerchantUpkeep()
	{
		Resource resource = new Resource();
		if (!IsMerchant() || mission_kingdom == null)
		{
			return resource;
		}
		resource[ResourceType.Gold] += CalcImportedGoodsGoldUpkeep();
		resource[ResourceType.Gold] += CalcImportedFoodGoldUpkeep();
		resource[ResourceType.Food] += CalcExportedFoodUpkeep();
		resource[ResourceType.Trade] += GetTradeCommerce();
		resource[ResourceType.Trade] += CalcImportedGoodsCommerce();
		resource[ResourceType.Trade] += CalcImportedFoodCommerce();
		resource[ResourceType.Trade] += CalcExportedFoodCommerce();
		return resource;
	}

	public Resource CalcTotalMerchantProfit()
	{
		Resource resource = new Resource();
		if (!IsMerchant() || mission_kingdom == null)
		{
			return resource;
		}
		resource[ResourceType.Gold] += GetTradeProfit();
		resource[ResourceType.Gold] += CalcExportedFoodGoldGain();
		resource[ResourceType.Food] += CalcImportedFoodFoodGain();
		return resource;
	}

	public float GetTradeProfit()
	{
		if (!IsMerchant() || mission_kingdom == null)
		{
			return 0f;
		}
		return game.economy.CalcTradeProfit(GetKingdom(), mission_kingdom, this);
	}

	public float GetForeignTradeProfit()
	{
		if (!IsMerchant() || mission_kingdom == null)
		{
			return 0f;
		}
		return game.economy.CalcForeignTradeProfit(GetKingdom(), mission_kingdom, this);
	}

	public float GetTradeCommerce()
	{
		if (!IsMerchant())
		{
			return 0f;
		}
		Kingdom kingdom = mission_kingdom;
		if (kingdom == null && cur_action != null && cur_action.def.field.key == "TradeWithKingdomAction")
		{
			kingdom = cur_action.target as Kingdom;
		}
		if (kingdom == null)
		{
			return 0f;
		}
		int num = ((kingdom != mission_kingdom) ? 1 : trade_level);
		if (kingdom != null && cur_action != null && cur_action.def.field.key == "ExpandTradeAction")
		{
			num++;
		}
		return game.economy.CalcCommerceForTrader(this, kingdom, num);
	}

	public float CalcExpeditionCommerce()
	{
		if (!IsMerchant())
		{
			return 0f;
		}
		if (base.statuses == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < base.statuses.Count; i++)
		{
			if (base.statuses[i] is TradeExpeditionStatus tradeExpeditionStatus)
			{
				DT.Field field = tradeExpeditionStatus.def?.field.FindChild("upkeep");
				if (field != null)
				{
					num += field.GetFloat("trade", tradeExpeditionStatus);
				}
			}
		}
		return (float)Math.Floor(num);
	}

	public bool IsImportingGoods()
	{
		if (importing_goods != null)
		{
			return importing_goods.Count > 0;
		}
		return false;
	}

	public int GetMaxImportGoodsCount()
	{
		return game.economy.def.merchant_max_import_goods_count;
	}

	public Resource CalcImportGoodUpkeep(ImportedGood good)
	{
		return new Resource
		{
			[ResourceType.Trade] = CalcImportedGoodCommerce(good.name, good.discount),
			[ResourceType.Gold] = CalcImportedGoodGoldUpkeep(good.name, good.discount)
		};
	}

	public float CalcImportedGoodsCommerce()
	{
		if (!IsMerchant() || mission_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		if (cur_action != null && cur_action.def.field.key == "LucrativeImportAction")
		{
			Resource upkeep = cur_action.GetUpkeep();
			if (upkeep != null)
			{
				num += upkeep[ResourceType.Trade];
			}
		}
		if (importing_goods == null)
		{
			return num;
		}
		for (int i = 0; i < importing_goods.Count; i++)
		{
			ImportedGood importedGood = importing_goods[i];
			float num2 = CalcImportedGoodCommerce(importedGood.name, importedGood.discount);
			num += num2;
		}
		return num;
	}

	public float CalcImportedGoodCommerce(string name, float discount = 0f)
	{
		return (float)Math.Floor(game.economy.CalcImportGoodCommerce(this, name) * (1f - discount / 100f));
	}

	public bool IsImportingFood()
	{
		return FindStatus<ImportingFoodStatus>() != null;
	}

	public float CalcImportedFoodFoodGain()
	{
		return FindStatus<ImportingFoodStatus>()?.foodAmount ?? 0f;
	}

	public float CalcImportedFoodCommerce()
	{
		ImportingFoodStatus importingFoodStatus = FindStatus<ImportingFoodStatus>();
		if (importingFoodStatus != null)
		{
			return importingFoodStatus.commerceUpkeep;
		}
		if (cur_action != null && cur_action.def.field.key.StartsWith("ImportFoodAction", StringComparison.Ordinal))
		{
			Resource upkeep = cur_action.GetUpkeep();
			if (upkeep != null)
			{
				return (float)Math.Floor(upkeep[ResourceType.Trade]);
			}
		}
		return 0f;
	}

	public float CalcImportedFoodGoldUpkeep()
	{
		return FindStatus<ImportingFoodStatus>()?.CalcGoldUpkeep() ?? 0f;
	}

	public bool IsExportingFood()
	{
		return FindStatus<ExportingFoodStatus>() != null;
	}

	public float CalcExportedFoodUpkeep()
	{
		ExportingFoodStatus exportingFoodStatus = FindStatus<ExportingFoodStatus>();
		if (exportingFoodStatus != null)
		{
			return exportingFoodStatus.foodAmount;
		}
		if (cur_action != null && cur_action.def.field.key == "ExportFoodAction")
		{
			Resource upkeep = cur_action.GetUpkeep();
			if (upkeep != null)
			{
				return upkeep[ResourceType.Food];
			}
		}
		return 0f;
	}

	public float CalcExportedFoodCommerce()
	{
		ExportingFoodStatus exportingFoodStatus = FindStatus<ExportingFoodStatus>();
		if (exportingFoodStatus != null)
		{
			return exportingFoodStatus.commerceUpkeep;
		}
		if (cur_action != null && cur_action.def.field.key == "ExportFoodAction")
		{
			Resource upkeep = cur_action.GetUpkeep();
			if (upkeep != null)
			{
				return (float)Math.Floor(upkeep[ResourceType.Trade]);
			}
		}
		return 0f;
	}

	public float CalcExportedFoodGoldGain()
	{
		ExportingFoodStatus exportingFoodStatus = FindStatus<ExportingFoodStatus>();
		if (exportingFoodStatus != null)
		{
			return exportingFoodStatus.gold * (1f + GetStat(Stats.cs_export_food_gold_increase_perc) / 100f);
		}
		return 0f;
	}

	public float CalcImportedGoodsGoldUpkeep()
	{
		if (!IsMerchant() || mission_kingdom == null)
		{
			return 0f;
		}
		if (importing_goods == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < importing_goods.Count; i++)
		{
			ImportedGood importedGood = importing_goods[i];
			float num2 = CalcImportedGoodGoldUpkeep(importedGood.name, importedGood.discount);
			num += num2;
		}
		return num;
	}

	public float CalcImportedGoodGoldUpkeep(string name, float discount = 0f)
	{
		return (float)Math.Round(game.economy.CalcImportGoodGoldUpkeep(this, name) * (1f - discount / 100f) * (1f - GetKingdom().GetStat(Stats.ks_import_goods_and_food_upkeep_reduction_perc) / 100f));
	}

	public int GetMinTradeLevel()
	{
		if (game?.economy == null)
		{
			return 0;
		}
		return game.economy.GetMinMerchantTradeLevel();
	}

	public int GetMinTradeLevelOnMission()
	{
		if (game?.economy == null)
		{
			return 0;
		}
		return game.economy.GetMinMerchantTradeLevelOnMission();
	}

	public int GetMaxTradeLevel()
	{
		if (game?.economy == null)
		{
			return 0;
		}
		return game.economy.GetMaxMerchantTradeLevel();
	}

	public void DecreaseTradeLevel()
	{
		SetTradeLevel(trade_level - 1);
	}

	public void IncreaseTradeLevel()
	{
		SetTradeLevel(trade_level + 1);
	}

	public void SetTradeLevel(int trade_level, bool send_state = true)
	{
		if (this.trade_level != trade_level && trade_level <= GetMaxTradeLevel() && trade_level >= GetMinTradeLevel())
		{
			int num = this.trade_level;
			this.trade_level = trade_level;
			trade_level_time = game.time;
			if (!IsKing() || num != 0 || trade_level <= 1)
			{
				NotifyListeners("trade_level_changed");
			}
			GetKingdom()?.InvalidateIncomes();
			if (send_state)
			{
				SendState<TradeState>();
			}
		}
	}

	public bool StopTrade()
	{
		if (!IsTrading())
		{
			return false;
		}
		StopImportingGoods();
		if (!IsKing())
		{
			GetKingdom()?.NotifyListeners("trade_stopped", this);
		}
		SetTradeLevel(GetMinTradeLevel());
		DelStatus<ImportingFoodStatus>();
		DelStatus<ExportingFoodStatus>();
		return true;
	}

	public bool HasPuppetPope()
	{
		if (puppets == null)
		{
			return false;
		}
		for (int i = 0; i < puppets.Count; i++)
		{
			if (puppets[i].IsPope())
			{
				return true;
			}
		}
		return false;
	}

	public bool HasEmptyImportGoodSlot()
	{
		if (importing_goods == null)
		{
			return true;
		}
		for (int i = 0; i < importing_goods.Count; i++)
		{
			if (string.IsNullOrEmpty(importing_goods[i].name))
			{
				return true;
			}
		}
		return false;
	}

	private int FindEmptyImportGoodSlot()
	{
		if (importing_goods == null)
		{
			return 0;
		}
		for (int i = 0; i < importing_goods.Count; i++)
		{
			if (string.IsNullOrEmpty(importing_goods[i].name))
			{
				return i;
			}
		}
		return importing_goods.Count;
	}

	public ImportedGood GetImportedGood(int slot = 0)
	{
		if (importing_goods == null || slot < 0 || slot >= importing_goods.Count)
		{
			return new ImportedGood
			{
				name = null,
				discount = 0f
			};
		}
		return importing_goods[slot];
	}

	public int FindImportGoodSlot(string name)
	{
		if (importing_goods == null)
		{
			return -1;
		}
		for (int i = 0; i < importing_goods.Count; i++)
		{
			if (importing_goods[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}

	public int ImportingGoodsCount()
	{
		if (importing_goods == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < importing_goods.Count; i++)
		{
			if (!string.IsNullOrEmpty(importing_goods[i].name))
			{
				num++;
			}
		}
		return num;
	}

	public bool ImportGood(string name, int slot_idx = -1, float discount = 0f, bool recalcTags = true)
	{
		if (name == null)
		{
			if (importing_goods == null)
			{
				return false;
			}
			if (slot_idx < 0 || slot_idx >= importing_goods.Count)
			{
				return false;
			}
			if (string.IsNullOrEmpty(importing_goods[slot_idx].name))
			{
				return false;
			}
		}
		else
		{
			if (!IsTrading())
			{
				return false;
			}
			int maxImportGoodsCount = GetMaxImportGoodsCount();
			if (FindImportGoodSlot(name) >= 0)
			{
				return false;
			}
			if (slot_idx < 0)
			{
				slot_idx = FindEmptyImportGoodSlot();
			}
			if (slot_idx >= maxImportGoodsCount)
			{
				return false;
			}
			if (importing_goods == null)
			{
				importing_goods = new List<ImportedGood>();
				while (importing_goods.Count < maxImportGoodsCount)
				{
					importing_goods.Add(default(ImportedGood));
				}
			}
		}
		importing_goods[slot_idx] = new ImportedGood
		{
			name = name,
			discount = discount
		};
		NotifyListeners("importing_goods_changed");
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			if (recalcTags)
			{
				kingdom.RefreshRealmTags();
			}
			kingdom.RecalcBuildingStates();
			if (recalcTags)
			{
				kingdom.UpdateRealmTags();
			}
			kingdom.InvalidateIncomes();
		}
		SendState<TradeState>();
		return true;
	}

	public void StopImportingGoods()
	{
		if (importing_goods == null)
		{
			return;
		}
		for (int i = 0; i < importing_goods.Count; i++)
		{
			if (!string.IsNullOrEmpty(importing_goods[i].name))
			{
				ImportGood(null, i);
			}
		}
		importing_goods = null;
		NotifyListeners("importing_goods_changed");
	}

	public bool StopImportingGood(string name)
	{
		int num = FindImportGoodSlot(name);
		if (num < 0)
		{
			return false;
		}
		return ImportGood(null, num);
	}

	private void AddAction(Action.Def def)
	{
		if (actions == null)
		{
			actions = new Actions(this);
		}
		actions.Add(def);
	}

	private void CreateActions(CharacterClass.Def class_def)
	{
		if (class_def.base_def != null)
		{
			CreateActions(class_def.base_def);
		}
		if (class_def.actions != null)
		{
			for (int i = 0; i < class_def.actions.Count; i++)
			{
				AddAction(class_def.actions[i].Item1);
			}
		}
		actions?.RescheduleOpportunities();
	}

	private void CreateActions()
	{
		if (class_def != null && !class_def.IsBase())
		{
			CreateActions(class_def);
		}
	}

	private void DestroyActions()
	{
		if (actions != null)
		{
			actions.DestroyAll();
		}
	}

	public Action FindAction(string name)
	{
		if (actions?.all == null)
		{
			return null;
		}
		for (int i = 0; i < actions.all.Count; i++)
		{
			Action action = actions.all[i];
			if (action.def.id.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return action;
			}
		}
		return null;
	}

	public string ValidateAction(string name)
	{
		Action action = FindAction(name);
		if (action == null)
		{
			return "action not found";
		}
		string arg = action.Validate();
		return $"{action}: {arg}";
	}

	public string ExecuteAction(string name, Object target = null)
	{
		Action action = FindAction(name);
		if (action == null)
		{
			return "action not found";
		}
		if (target == null && action.NeedsTarget())
		{
			string text = action.Validate();
			if (text != "ok")
			{
				return $"FAILED to execute {action}: {text}";
			}
			List<Object> possibleTargets = action.GetPossibleTargets();
			if (possibleTargets == null || possibleTargets.Count == 0)
			{
				return $"FAILED to execute {action}: no possible targets";
			}
			action.SetState(Action.State.PickingTarget);
			return string.Format("executed {0} with target {1}", action, target?.ToString() ?? "null");
		}
		if (!action.Execute(target))
		{
			string text2 = action.Validate();
			if (text2 == "ok" && !action.ValidateTarget(target))
			{
				text2 = "invalid_target";
			}
			return string.Format("FAILED to execute {0} with target {1}: {2}", action, target?.ToString() ?? "null", text2);
		}
		return string.Format("executed {0} with target {1}", action, target?.ToString() ?? "null");
	}

	public void SetClass(string class_name)
	{
		if (class_name != null)
		{
			CharacterClass.Def def = game.defs.Get<CharacterClass.Def>(class_name);
			if (def != null)
			{
				SetClass(def);
			}
		}
	}

	public void SetClass(CharacterClass.Def def, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			SendEvent(new ChangeClassEvent(def.id));
			return;
		}
		DestroyActions();
		if (def == null)
		{
			def = game.defs.GetBase<CharacterClass.Def>();
		}
		class_def = def;
		RefreshTags();
		CreateActions();
		if (send_state)
		{
			SendState<ClassState>();
		}
		if (IsAuthority())
		{
			if (def.field.key == "Cleric")
			{
				AddStatus<BrightPersonStatus>();
			}
			GenerateNewSkills();
		}
		RefreshPortraitVariant(base_too: true, check_old_valid: false);
		NotifyListeners("character_class_change");
	}

	public void RefreshPortraitVariant(bool base_too, bool check_old_valid)
	{
		if (IsAuthority())
		{
			Vars vars = new Vars(this);
			vars.Set("base_too", base_too);
			vars.Set("check_old_valid", check_old_valid);
			game.NotifyListeners("character_portrait_refresh", vars);
			NotifyListeners("portrait_changed");
			SendState<PortraitIdState>();
		}
	}

	public void SetPortraitID(int portraitID, int variantID)
	{
		this.portraitID = portraitID;
		portrait_variantID = variantID;
		NotifyListeners("portrait_changed");
		SendState<PortraitIdState>();
	}

	public Army GetArmy()
	{
		if (!(location is Army army) || army.leader != this)
		{
			return null;
		}
		return army;
	}

	public Army SpawnArmy(Castle castle)
	{
		Recall(disband_army: true, from_prison: true, spawn_marshall: false);
		if (castle == null)
		{
			return null;
		}
		Point randomExitPoint = castle.GetRandomExitPoint(try_exit_outside_town: true, check_water: true);
		Army army = new Army(game, randomExitPoint, kingdom_id);
		army.Start();
		army.SetLeader(this);
		army.AddNoble();
		NotifyListeners("assign_army");
		return army;
	}

	public bool DisbandArmy()
	{
		Army army = GetArmy();
		if (army == null)
		{
			return false;
		}
		army.leader = null;
		SetLocation(null);
		ClearStatus();
		Battle battle = army.battle;
		army.Destroy();
		battle?.CheckVictory();
		FireEvent("disband_army", null);
		return true;
	}

	public Castle GetCastleIn()
	{
		Army army = GetArmy();
		if (army != null)
		{
			return army.castle;
		}
		if (location is Castle result)
		{
			return result;
		}
		return null;
	}

	public Castle GetGovernedCastle()
	{
		return governed_castle;
	}

	public Castle GetPreparingToGovernCastle()
	{
		List<Action> list = actions?.active;
		if (list == null)
		{
			return null;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Action action = list[i];
			if (action is GovernCityAction || action is AssignNewGovernCityAction || action is ReassignGovernCityAction)
			{
				return (action.target as Realm)?.castle;
			}
		}
		return null;
	}

	public Castle GetGovernedCastleBanishedFrom()
	{
		return governed_castle_banished_from;
	}

	public override IRelationCheck GetStanceObj()
	{
		if (location is Army army)
		{
			return army.GetStanceObj();
		}
		return base.GetStanceObj();
	}

	public bool CanBeGovernor()
	{
		if (!IsAlive())
		{
			return false;
		}
		if (sex != Sex.Male)
		{
			return false;
		}
		if (IsRebel())
		{
			return false;
		}
		if (age < Age.Young && !IsKing())
		{
			return false;
		}
		if (class_def == null || class_def.IsBase() || IsClasslessPrince())
		{
			return false;
		}
		if (game.religions?.catholic?.crusade?.army?.leader == this)
		{
			return false;
		}
		return true;
	}

	public bool StopGoverning(bool reloadLabel = true, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			return false;
		}
		Castle governedCastle = GetGovernedCastle();
		if (governedCastle == null)
		{
			return false;
		}
		governed_castle = null;
		governedCastle.SetGovernor(null, reloadLabel);
		if (send_state)
		{
			SendState<GovernedCastleState>();
			UpdateAutomaticStatuses();
			GetKingdom()?.stability?.SpecialEvent(think_rebel: false);
		}
		NotifyListeners("stoped_governing", governedCastle);
		return true;
	}

	public void Govern(Castle castle)
	{
		if (!IsAuthority() || governed_castle == castle || (castle != null && !castle.CanSetGovernor(this)))
		{
			return;
		}
		StopGoverning(reloadLabel: true, send_state: false);
		governed_castle = castle;
		governed_castle_banished_from = null;
		if (governed_castle != null)
		{
			if (castle.governor != null)
			{
				castle.governor.StopGoverning();
			}
			castle.SetGovernor(this);
		}
		SendState<GovernedCastleState>();
		UpdateAutomaticStatuses();
		GetKingdom()?.stability?.SpecialEvent(think_rebel: false);
		NotifyListeners("started_governing", castle);
	}

	public Castle GovernTarget()
	{
		if (governed_castle != null)
		{
			return governed_castle;
		}
		return GetPreparingToGovernCastle();
	}

	public MapObject CurLocation()
	{
		if (location != null)
		{
			return location;
		}
		if (mission_realm != null)
		{
			return mission_realm.castle;
		}
		if (mission_kingdom != null)
		{
			return mission_kingdom?.GetCapital()?.castle;
		}
		if (prison_kingdom != null)
		{
			return prison_kingdom?.GetCapital()?.castle;
		}
		return GovernTarget();
	}

	public Object CurTarget()
	{
		if (cur_action != null && cur_action.target != null)
		{
			return cur_action.target;
		}
		if (mission_kingdom != null)
		{
			return mission_kingdom;
		}
		return null;
	}

	public bool BanishFrom(Castle c, bool reloadLabel = true, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			return false;
		}
		StopGoverning(reloadLabel, send_state);
		return true;
	}

	public bool UnBanish()
	{
		Govern(governed_castle_banished_from);
		return true;
	}

	public Kingdom GetSpecialCourtKingdom()
	{
		return game.GetKingdom(special_court_kingdom_id);
	}

	public int GetSpecialCourtKingdomId()
	{
		return special_court_kingdom_id;
	}

	public void SetSpecialCourtKingdom(Kingdom k, bool send_state = true)
	{
		special_court_kingdom_id = k?.id ?? 0;
		if (send_state)
		{
			SendState<SpecialCourtKingdomState>();
		}
	}

	public void SetSpecialCourtKingdom(int id, bool send_state = true)
	{
		special_court_kingdom_id = id;
		if (send_state)
		{
			SendState<SpecialCourtKingdomState>();
		}
	}

	public void SetMissionKingdom(Kingdom k, bool send_state = true)
	{
		if (mission_kingdom == k)
		{
			return;
		}
		Kingdom kingdom = mission_kingdom;
		if (mission_kingdom != null)
		{
			if (mission_kingdom.favoriteDiplomat == this)
			{
				mission_kingdom.favoriteDiplomat = null;
			}
			if (mission_kingdom.royalFamily?.SpouseFacination == this)
			{
				mission_kingdom.royalFamily.SpouseFacination = null;
			}
			mission_kingdom.foreigners.Remove(this);
		}
		StopTrade();
		mission_kingdom = k;
		if (mission_kingdom != null)
		{
			mission_kingdom.foreigners.Add(this);
		}
		RefreshTags();
		NotifyListeners("mission_kingdom_changed", kingdom);
		if (IsSpy())
		{
			kingdom?.NotifyListeners("spy_left", this);
			if (mission_kingdom != null)
			{
				mission_kingdom.NotifyListeners("spy_entered", this);
			}
		}
		if (send_state)
		{
			SendState<MissionKingdomState>();
		}
	}

	public void SetMissionRealm(Realm r, bool send_state = true)
	{
		if (mission_realm == r)
		{
			return;
		}
		if (mission_realm != null)
		{
			Kingdom kingdom = mission_realm.GetKingdom();
			if (kingdom?.foreigners != null)
			{
				kingdom.foreigners.Remove(this);
			}
		}
		StopTrade();
		mission_realm = r;
		if (mission_realm != null)
		{
			mission_realm.GetKingdom().foreigners.Add(this);
		}
		RefreshTags();
		NotifyListeners("mission_realm_changed");
		if (send_state)
		{
			SendState<MissionRealmState>();
		}
	}

	public void AddPuppet(Character puppet, bool send_state = true)
	{
		puppet.AddMaster(this, send_state);
		if (puppets == null)
		{
			puppets = new List<Character>();
		}
		if (!puppets.Contains(puppet))
		{
			puppets.Add(puppet);
			if (send_state)
			{
				SendState<EspionageState>();
			}
			HasPuppetStatus hasPuppetStatus = HasPuppetStatus.Create(puppet);
			if (hasPuppetStatus != null)
			{
				AddStatus(hasPuppetStatus);
			}
			NotifyListeners("puppet_added", puppet);
		}
	}

	public void DelPuppet(Character puppet, bool send_state = true)
	{
		if (puppet == null)
		{
			return;
		}
		puppet.DelMaster(this, send_state);
		if (puppet.IsPretenderToTheThrone(GetKingdom()))
		{
			puppet.GetKingdom().royalFamily.SetPretender(null, null, was_puppet: false);
		}
		if (puppets != null)
		{
			if (!puppets.Remove(puppet))
			{
				send_state = false;
			}
			if (puppets.Count == 0)
			{
				puppets = null;
			}
			if (cur_action != null && cur_action.state == Action.State.Preparing && cur_action.Validate() != "ok")
			{
				cur_action.Cancel();
			}
			if (send_state)
			{
				SendState<EspionageState>();
			}
			NotifyListeners("puppet_removed", puppet);
		}
		HasPuppetStatus hasPuppetStatus = HasPuppetStatus.Find(this, puppet);
		if (hasPuppetStatus != null)
		{
			DelStatus(hasPuppetStatus);
		}
	}

	public void ClearPuppets(bool send_states = true)
	{
		if (IsAuthority())
		{
			Timer.Stop(this, "puppet_rebel_distance_check");
		}
		if (puppets == null)
		{
			return;
		}
		for (int i = 0; i < puppets.Count; i++)
		{
			Character character = puppets[i];
			character.DelMaster(this, send_states);
			HasPuppetStatus hasPuppetStatus = HasPuppetStatus.Find(this, character);
			if (hasPuppetStatus != null)
			{
				base.statuses.Del(hasPuppetStatus, send_states);
			}
		}
		puppets = null;
		if (send_states)
		{
			SendState<EspionageState>();
		}
	}

	private void AddMaster(Character master, bool send_state = true)
	{
		if (masters == null)
		{
			masters = new List<Character>();
		}
		if (!masters.Contains(master))
		{
			masters.Add(master);
			if (send_state)
			{
				SendState<EspionageState>();
			}
		}
	}

	private void DelMaster(Character master, bool send_state = true)
	{
		if (masters != null)
		{
			if (!masters.Remove(master))
			{
				send_state = false;
			}
			if (masters.Count == 0)
			{
				masters = null;
			}
			if (send_state)
			{
				SendState<EspionageState>();
			}
		}
	}

	public void NotifyMasters(string notification, object param = null)
	{
		if (masters != null)
		{
			for (int i = 0; i < masters.Count; i++)
			{
				masters[i].FireEvent(notification, param);
			}
		}
	}

	public void TryRevealMasters(string revealActionName = null)
	{
		if (masters != null && !string.IsNullOrEmpty(revealActionName))
		{
			for (int num = masters.Count - 1; num >= 0; num--)
			{
				masters[num].FindAction(revealActionName)?.Execute(null);
			}
		}
	}

	public void ClearMasters(string reason = null, bool send_states = true)
	{
		if (masters == null)
		{
			return;
		}
		List<Character> list = masters;
		masters = null;
		for (int i = 0; i < list.Count; i++)
		{
			Character character = list[i];
			character.DelPuppet(this, send_states);
			if (reason != null)
			{
				Vars vars = new Vars();
				vars.Set("puppet", this);
				vars.Set("master", character);
				vars.Set("reason", reason);
				character.FireEvent("lost_puppet", vars, character.GetKingdom().id);
			}
		}
		if (send_states)
		{
			SendState<EspionageState>();
		}
	}

	public void SetRevealKingdom(int kid, bool send_state = true)
	{
		reveal_kingdom = kid;
		SetRevealedInKingdom(game?.GetKingdom(kid), send_state: false);
		if (send_state)
		{
			SendState<EspionageState>();
		}
	}

	public void SetRevealKingdom(Kingdom k, bool send_state = true)
	{
		reveal_kingdom = k?.id ?? 0;
		SetRevealedInKingdom(k, send_state: false);
		if (send_state)
		{
			SendState<EspionageState>();
		}
	}

	public void SetRevealMaster(Character spy, bool send_state = true)
	{
		if (reveal_master != null)
		{
			reveal_master.DelListener(this);
		}
		reveal_master = spy;
		if (reveal_master != null)
		{
			reveal_master.AddListener(this);
		}
		if (send_state)
		{
			SendState<EspionageState>();
		}
	}

	public bool IsRevealedIn(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (revealed_in_kingdoms == null)
		{
			return false;
		}
		if (!revealed_in_kingdoms.Contains(k.id))
		{
			return false;
		}
		return true;
	}

	public void SetRevealedInKingdom(Kingdom k, bool send_state = true)
	{
		if (k == null)
		{
			return;
		}
		if (revealed_in_kingdoms == null)
		{
			revealed_in_kingdoms = new List<int>();
		}
		if (!revealed_in_kingdoms.Contains(k.id))
		{
			revealed_in_kingdoms.Add(k.id);
			if (send_state)
			{
				SendState<EspionageState>();
			}
		}
	}

	public void DelRevealedInKingdom(Kingdom k, bool send_state = true)
	{
		if (k != null && revealed_in_kingdoms != null && revealed_in_kingdoms.Remove(k.id) && send_state)
		{
			SendState<EspionageState>();
		}
	}

	private bool IsExMercenary()
	{
		return title == "MercenaryLeader";
	}

	public void Imprison(Kingdom k, bool recall = true, bool send_state = true, string prison_reason = null, bool destroy_if_free = true)
	{
		if ((!IsAuthority() && send_state) || prison_kingdom == k)
		{
			return;
		}
		if (k == null && destroy_if_free && IsValid() && (GetKingdom().type != Kingdom.Type.Regular || IsExMercenary()) && !IsDead() && !IsInSpecialCourt())
		{
			Destroy();
		}
		else
		{
			if (k != null && k.type != Kingdom.Type.Regular)
			{
				return;
			}
			bool flag = k != null && prison_kingdom != null;
			string text = this.prison_reason;
			this.prison_reason = prison_reason;
			Kingdom param = prison_kingdom;
			if (k != null)
			{
				prison_time = game.time;
				ai_renounce_time = game.time + game.Random(k.royal_dungeon.def.renounce_after_min, k.royal_dungeon.def.renounce_after_max);
			}
			else
			{
				prison_time = Time.Zero;
				ai_renounce_time = Time.Zero;
			}
			using (Game.Profile("Imprison del prisoner"))
			{
				if (prison_kingdom != null)
				{
					prison_kingdom.DelPrisoner(this, flag);
				}
			}
			using (Game.Profile("Imprison rest"))
			{
				if (k != null)
				{
					Timer.Stop(this, "ransom_cooldown");
					Timer.Stop(this, "hire_loyalist_cooldown");
					BecomeNormalMercenary();
					if (IsRebel() && GetKingdom().type == Kingdom.Type.LoyalistsFaction)
					{
						SetKingdom(FactionUtils.GetFactionKingdom(game, "RebelsFaction").id);
					}
					Kingdom specialCourtKingdom = GetSpecialCourtKingdom();
					if (text == "hired_as_loyalist" && specialCourtKingdom != null && !specialCourtKingdom.IsDefeated())
					{
						SetKingdom(specialCourtKingdom.id);
						specialCourtKingdom.AddCourtMember(this);
					}
					if (IsRebel() && specialCourtKingdom != null && k != specialCourtKingdom)
					{
						specialCourtKingdom.DelSpecialCourtMember(this);
						Vars vars = new Vars();
						vars.Set("rebel", this);
						vars.Set("other_kingdom", k);
						vars.Set("kingdom", specialCourtKingdom);
						specialCourtKingdom.NotifyListeners("lost_special_court_rebel", vars);
					}
					if (IsCardinal())
					{
						game.religions.catholic.DelCardinal(this, "imprisoned");
					}
					next_prison_check = game.time + k.royal_dungeon.def.ai_min_character_prison_time;
					next_owner_prison_check = next_prison_check;
					k.AddPrisoner(this, flag);
					ClearDefaultStatus();
				}
				else
				{
					ClearDefaultStatus<InPrisonStatus>();
					Pact.RevealPacts(this);
				}
				if (recall)
				{
					DisolvePact("OwnerImprisoned");
					Recall(disband_army: true, from_prison: false, k == null && IsValid(), cancelCurrentAction: true, notifyOnCancel: false);
				}
				RefreshTags();
				if (send_state)
				{
					if (k != null)
					{
						OnPrisonActionAnalytics("imprisoned");
					}
					SendState<PrisonKingdomState>();
				}
				if (k != null)
				{
					if (flag)
					{
						NotifyListeners("prison_kingdom_changed", param);
					}
					else
					{
						NotifyListeners("imprisoned");
					}
				}
			}
		}
	}

	public void OnPrisonActionAnalytics(string prisonAction, Resource cost = null, Resource received = null)
	{
		Kingdom kingdom = GetKingdom();
		Kingdom kingdom2 = prison_kingdom;
		bool flag = kingdom?.is_player ?? false;
		bool flag2 = kingdom2?.is_player ?? false;
		if (game.IsRunning() && (flag || flag2) && !string.IsNullOrEmpty(prisonAction) && !Game.isLoadingSaveGame)
		{
			Vars vars = new Vars();
			vars.Set("prisonerKingdom", kingdom.Name);
			vars.Set("imprisonerKingdom", (kingdom2 != null) ? kingdom2.Name : "none");
			vars.Set("prisonerAction", prisonAction);
			if (cost != null)
			{
				vars.Set("goldCost", cost[ResourceType.Gold]);
			}
			if (received != null)
			{
				vars.Set("goldReceived", received[ResourceType.Gold]);
			}
			if (flag)
			{
				kingdom.FireEvent("analytics_prisoner_change", vars, kingdom.id);
			}
			if (flag2)
			{
				kingdom2.FireEvent("analytics_prisoner_change", vars, kingdom2.id);
			}
		}
	}

	public void OnGovernAnalytics(Kingdom k, Realm r, string assignAction)
	{
		if (k != null && k.is_player && r != null && !string.IsNullOrEmpty(assignAction))
		{
			Vars vars = new Vars();
			vars.Set("province", r.name);
			vars.Set("characterClass", class_name);
			vars.Set("characterName", Name);
			vars.Set("assignAction", assignAction);
			vars.Set("characterLevel", GetClassLevel());
			k.FireEvent("analytics_governor_changed", vars, k.id);
		}
	}

	public bool StopDiplomacy()
	{
		if (mission_kingdom == null)
		{
			return true;
		}
		mission_kingdom.diplomats.Remove(this);
		return true;
	}

	public bool StopImprovingOpinions()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom?.improveOpinionsDiplomat == this)
		{
			kingdom.SetImproveOpinionsDiplomat(null);
			return true;
		}
		return false;
	}

	public bool IsOnMap()
	{
		if (location is Army)
		{
			return true;
		}
		return false;
	}

	public void Recall(bool disband_army = true, bool from_prison = false, bool spawn_marshall = true, bool cancelCurrentAction = true, bool notifyOnCancel = true)
	{
		AssertAuthority();
		if (cancelCurrentAction && cur_action != null)
		{
			cur_action.Cancel(manual: true, notifyOnCancel);
		}
		if (disband_army)
		{
			if (IsPope() && IsAlive() && !IsPrisoner())
			{
				GetArmy()?.BecomeMercenary(kingdom_id);
			}
			else
			{
				DisbandArmy();
			}
		}
		StopTrade();
		StopDiplomacy();
		StopImprovingOpinions();
		SetMissionKingdom(null);
		SetMissionRealm(null);
		ClearPuppets();
		if (from_prison)
		{
			Imprison(null, recall: false);
		}
		else if (prison_kingdom != null)
		{
			spawn_marshall = false;
		}
		Kingdom kingdom = GetKingdom();
		if (spawn_marshall && kingdom != null && IsMarshal() && !kingdom.IsDefeated() && GetArmy() == null)
		{
			ClearStatus();
			SpawnArmy(kingdom.GetCapital()?.castle);
		}
		else
		{
			SetLocation(null);
			ClearDefaultStatus();
			ClearStatus();
		}
	}

	public void ReturnToThroneRoom()
	{
		Recall(disband_army: true, from_prison: true, spawn_marshall: false);
		if (!IsKing())
		{
			StopGoverning();
			StopPromotingPaganBelief();
		}
		if (IsKingOrPrince())
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom != null && !IsKing())
			{
				kingdom.DelCourtMember(this);
			}
		}
	}

	public MapObject GetLocation()
	{
		return location;
	}

	public bool SetLocation(MapObject location, bool send_state = true)
	{
		if (location == this.location)
		{
			return false;
		}
		this.location = location;
		UpdateAutomaticStatuses();
		NotifyListeners("location_changed");
		if (send_state)
		{
			SendState<LocationState>();
		}
		else if (location is Army)
		{
			NotifyListeners("assign_army");
		}
		return true;
	}

	public string ChooseRebelSpawnCondition()
	{
		return "ReligionSpawnCondition";
	}

	public Realm ChooseRebelRealm()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || kingdom.realms == null || kingdom.realms.Count <= 0)
		{
			return null;
		}
		Realm realm = governed_castle?.GetRealm();
		if (realm != null)
		{
			return realm;
		}
		return kingdom.realms[game.Random(0, kingdom.realms.Count)];
	}

	public Rebel TurnIntoRebel(string type = null, string spawn_condition = null, Kingdom loyalTo = null, Realm realm = null)
	{
		AssertAuthority();
		bool flag = IsInCourt();
		Kingdom kingdom = GetKingdom();
		if (IsKing() && kingdom != null && !kingdom.IsDefeated())
		{
			return null;
		}
		if (IsCrusader())
		{
			Crusade crusade = game.religions.catholic.crusade;
			Character leader = game.religions.catholic.crusade.leader;
			if (crusade.end_reason == null)
			{
				crusade.end_reason = ((loyalTo == null) ? "force_go_rogue" : "force_go_rogue_loyalist");
			}
			crusade.force_end_outcomes = crusade.def.end_outcomes.Parse("rogue");
			crusade.force_go_rogue_params = new Crusade.ForceRebelParams(type, spawn_condition, loyalTo, realm);
			crusade.End();
			return leader?.GetArmy()?.rebel;
		}
		Army army = GetArmy();
		if (IsMercenary() && army != null)
		{
			Mercenary mercenary = army.mercenary;
			if (mercenary != null)
			{
				army.SetMercenary(null);
				mercenary.Destroy();
				army.NotifyListeners("became_rebel");
			}
		}
		if (IsRebel())
		{
			Rebel rebel = GetArmy()?.rebel;
			if (rebel != null)
			{
				return rebel;
			}
		}
		if (string.IsNullOrEmpty(spawn_condition))
		{
			spawn_condition = ChooseRebelSpawnCondition();
			if (spawn_condition == null)
			{
				return null;
			}
		}
		if (army != null)
		{
			realm = army.realm_in;
		}
		if (realm == null)
		{
			realm = ChooseRebelRealm();
			if (realm == null)
			{
				return null;
			}
		}
		if (masters != null && masters.Count > 0)
		{
			if (loyalTo == null)
			{
				Character character = masters[game.Random(0, masters.Count)];
				spawn_condition = "LoyalistsSpawnCondition";
				type?.Replace("Rebels", "Loyalists");
				loyalTo = character.GetKingdom();
			}
			ClearMasters("Betray");
		}
		Recall(disband_army: false, from_prison: false, spawn_marshall: false);
		Imprison(null, recall: false);
		RoyalFamily.SovereignUncrownData sovereignUncrownData = null;
		if (IsKing())
		{
			sovereignUncrownData = kingdom?.royalFamily.GetSovereignUncrownData();
		}
		StopGoverning();
		StopPromotingPaganBelief(apply_penalties: false);
		DisolvePact("OwnerRebelled");
		SetMissionKingdom(null);
		Catholic.CheckCardinalTitle(this);
		if (IsKing())
		{
			kingdom?.royalFamily?.Uncrown("become_rebel", sovereignUncrownData);
		}
		if (IsHeir())
		{
			kingdom?.royalFamily?.Unheir();
		}
		RebelSpawnCondition.Def def = game.defs.Get<RebelSpawnCondition.Def>(spawn_condition);
		Rebel.Def def2 = ((!string.IsNullOrEmpty(type)) ? game.defs.Get<Rebel.Def>(type) : def.rebel_types[game.Random(0, def.rebel_types.Count)]);
		if (!def.rebel_types.Contains(def2))
		{
			string text = def2?.field?.based_on?.key;
			bool flag2 = false;
			while (text != null)
			{
				Rebel.Def def3 = game.defs.Get<Rebel.Def>(text);
				if (def.rebel_types.Contains(def3))
				{
					flag2 = true;
					break;
				}
				text = def3?.field?.based_on?.key;
			}
			if (!flag2)
			{
				Game.Log("Turning character into rebel with condition that doesnt support the rebel type. Condition: " + def.name + ". Rebel type: " + def2.name, Game.LogType.Warning);
			}
		}
		Kingdom factionKingdom = FactionUtils.GetFactionKingdom(game, def2.kingdom_key);
		EnableAging(can_die: false);
		Rebel.Def promotionDef = def2.GetPromotionDef();
		if (promotionDef != null)
		{
			def2 = promotionDef;
		}
		Rebel rebel2 = new Rebel(game, factionKingdom.id, factionKingdom.id, realm, def, def2);
		if (realm != null)
		{
			rebel2.SetInitalRisk(realm.GetTotalRebellionRisk());
		}
		rebel2.character = this;
		if (loyalTo != null)
		{
			rebel2.loyal_to = loyalTo.id;
		}
		else
		{
			rebel2.loyal_to = factionKingdom.id;
		}
		if (army != null && army.IsValid())
		{
			rebel2.SetArmy(army);
		}
		else
		{
			army = rebel2.army;
		}
		if (army != null)
		{
			if (army.battle != null)
			{
				army.battle.Cancel(Battle.VictoryReason.None);
			}
			army.Stop();
			army.SetLeader(this);
			army.SetRebel(rebel2);
			army.SetKingdom(factionKingdom.id);
			army.ClearUnitsOverMax();
			if (army.castle != null)
			{
				army.LeaveCastle(army.castle.GetRandomExitPoint());
			}
		}
		RefreshTags();
		SetKingdom(factionKingdom.id);
		if (flag)
		{
			kingdom?.MoveToSpecialCourt(this);
		}
		SetTitle("Knight");
		UpdateAutomaticStatuses();
		FireEvent("turned_into_rebel", null);
		return rebel2;
	}

	public void Exile()
	{
		Kingdom kingdom = GetKingdom();
		if (!IsKing() || kingdom == null || kingdom.IsDefeated())
		{
			AssertAuthority();
			Recall(disband_army: false, from_prison: false, spawn_marshall: false);
			RoyalFamily.SovereignUncrownData sovereignUncrownData = null;
			if (IsKing())
			{
				sovereignUncrownData = kingdom?.royalFamily.GetSovereignUncrownData();
			}
			StopGoverning();
			StopPromotingPaganBelief(apply_penalties: false);
			kingdom?.royalFamily?.RemoveRelative(this);
			DisolvePact("OwnerExiled");
			SetMissionKingdom(null);
			Catholic.CheckCardinalTitle(this);
			if (IsKing())
			{
				kingdom?.royalFamily?.Uncrown("exile", sovereignUncrownData);
				kingdom.royalFamily.Sovereign = null;
			}
			else if (IsRoyalty())
			{
				kingdom?.royalFamily?.RemoveFamilyMember(this);
			}
			if (FindStatus<HasRoyalBloodStatus>() != null)
			{
				DelStatus<HasRoyalBloodStatus>();
			}
			if (IsEcumenicalPatriarch())
			{
				game.religions.orthodox.OnCharacterDied(this);
			}
			else if (IsPatriarch())
			{
				kingdom.religion.OnCharacterDied(this);
			}
			kingdom.DelCourtMember(this, send_state: true, kill_or_throneroom: false);
			kingdom.royalFamily.RemoveFamilyMember(this);
			ClearMasters("Exile");
			SetKingdom(FactionUtils.GetFactionKingdom(game, "ExileFaction").id);
			if (sex == Sex.Male)
			{
				SetTitle("Knight");
			}
			else
			{
				SetTitle("Lady");
			}
			RefreshTags();
			EnableAging(can_die: false);
		}
	}

	public Value GetTagValue(string tag)
	{
		if (string.IsNullOrEmpty(tag))
		{
			return Value.Unknown;
		}
		if (tag[0] == '!')
		{
			Value tagValue = GetTagValue(tag.Substring(1));
			if (tagValue.is_unknown)
			{
				return Value.Unknown;
			}
			return !tagValue.Bool();
		}
		switch (tag)
		{
		case "King":
			return IsKing();
		case "Prince":
			return IsPrince();
		case "Prison":
			return prison_kingdom != null;
		case "Crusader":
			return IsCrusader();
		case "Pope":
			return IsPope();
		case "CardinalElect":
			return IsCardinalElect();
		case "Cardinal":
			return IsCardinal();
		case "Patriarch":
			return IsPatriarch();
		case "Caliph":
			return IsCaliph();
		case "Marshal":
			return IsMarshal();
		case "Merchant":
			return IsMerchant();
		case "Diplomat":
			return IsDiplomat();
		case "Spy":
			return IsSpy();
		case "Cleric":
			return IsCleric();
		case "EcumenicalPatriarch":
			return IsEcumenicalPatriarch();
		case "Rebel":
		{
			Army army2 = GetArmy();
			if (army2 != null && army2.rebel != null)
			{
				return true;
			}
			return false;
		}
		case "Govern":
			return governed_castle != null;
		case "InTown":
		{
			if (location is Castle)
			{
				return true;
			}
			Army army = GetArmy();
			if (army != null && army.castle != null)
			{
				return true;
			}
			return false;
		}
		case "LeadingArmy":
			return GetArmy() != null;
		case "Siege":
			return false;
		default:
			return Value.Unknown;
		}
	}

	public bool HasTag(string tag, bool must_exist = true)
	{
		Value tagValue = GetTagValue(tag);
		if (tagValue.is_unknown && must_exist)
		{
			Warning("Unknown tag: '" + tag + "'");
		}
		return tagValue.Bool();
	}

	public void RefreshTags()
	{
		if (skill_mods == null)
		{
			skill_mods = new SkillMods(this);
		}
		skill_mods.Refresh();
		GetKingdom()?.InvalidateIncomes();
		NotifyListeners("refresh_tags");
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

	public Skill GetSkill(int idx)
	{
		if (skills == null || idx < 0 || idx >= skills.Count)
		{
			return null;
		}
		return skills[idx];
	}

	public Skill GetSkill(string skill_name)
	{
		if (skill_name == null)
		{
			return null;
		}
		if (skills == null)
		{
			return null;
		}
		for (int i = 0; i < skills.Count; i++)
		{
			Skill skill = skills[i];
			if (!(skill?.def?.name != skill_name))
			{
				return skill;
			}
		}
		return null;
	}

	public bool ShouldForceSkillsToMaxRank()
	{
		if (IsKing())
		{
			return true;
		}
		if (IsPatriarch())
		{
			return true;
		}
		if (IsPrince() && GetStat(Stats.cs_princes_skill_upgrade) > 0f)
		{
			return true;
		}
		return false;
	}

	public void ForceSkillsToMaxRank()
	{
		if (!AssertAuthority() || skills == null)
		{
			return;
		}
		for (int i = 0; i < skills.Count; i++)
		{
			while (AddSkillRank(skills[i]))
			{
			}
		}
	}

	public int GetSkillRank(Skill skill)
	{
		return skill?.rank ?? 0;
	}

	public int GetSkillRank(string skill_name)
	{
		Skill skill = GetSkill(skill_name);
		return GetSkillRank(skill);
	}

	public int NewSkillRank(Skill.Def def)
	{
		if (def == null)
		{
			return 0;
		}
		if (!def.IsApplicableTo(class_def))
		{
			return 0;
		}
		if (ShouldForceSkillsToMaxRank())
		{
			return def.max_rank;
		}
		return 1;
	}

	public bool CanAddSkillRank(int idx)
	{
		Skill skill = GetSkill(idx);
		return CanAddSkillRank(skill);
	}

	public bool CanAddSkillRank(Skill.Def skill_def)
	{
		Skill skill = GetSkill(skill_def?.name);
		return CanAddSkillRank(skill);
	}

	public bool CanAddSkillRank(Skill skill)
	{
		if (skill == null)
		{
			return false;
		}
		int skillRank = GetSkillRank(skill);
		int num = skill.MaxRank();
		return skillRank < num;
	}

	public bool AddSkillRank(int idx, bool send_state = true)
	{
		Skill skill = GetSkill(idx);
		return AddSkillRank(skill, send_state);
	}

	private void OnLevelUpAnalytics(Skill.Def skill, Resource cost)
	{
		Kingdom kingdom = GetKingdom();
		if (skill != null && kingdom != null && kingdom.is_player)
		{
			Vars vars = new Vars();
			vars.Set("characterName", Name);
			vars.Set("characterClass", class_name);
			vars.Set("characterLevel", GetClassLevel());
			vars.Set("learningChosen", skill.id);
			int val = ((cost != null) ? ((int)cost[ResourceType.Books]) : 0);
			int val2 = ((cost != null) ? ((int)cost[ResourceType.Gold]) : 0);
			vars.Set("bookCost", val);
			vars.Set("goldCost", val2);
			kingdom.NotifyListeners("analytics_character_level_up", vars);
		}
	}

	public bool AddSkillRank(Skill skill, bool send_state = true)
	{
		if (!CanAddSkillRank(skill))
		{
			return false;
		}
		if (!IsAuthority() && send_state)
		{
			SendEvent(new AddSkillRankEvent(skill.def));
			return true;
		}
		Resource upgardeCost = skill.def.GetUpgardeCost(this);
		skill.rank = GetSkillRank(skill) + 1;
		RefreshTags();
		if (send_state)
		{
			SendState<SkillsState>();
			OnLevelUpAnalytics(skill.def, upgardeCost);
		}
		NotifyListeners("skills_changed");
		return true;
	}

	public int GetSkillsCount()
	{
		if (skills == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < skills.Count; i++)
		{
			if (skills[i] != null)
			{
				num++;
			}
		}
		return num;
	}

	public int NumSkillSlots()
	{
		if (class_def != null)
		{
			return class_def.skill_slots.Count;
		}
		return 0;
	}

	public string SkillSlotType(int idx)
	{
		if (class_def == null)
		{
			return null;
		}
		if (idx < 0 || idx >= class_def.skill_slots.Count)
		{
			return null;
		}
		return class_def.skill_slots[idx];
	}

	public bool CanLearnNewSkills()
	{
		if (IsPrisoner())
		{
			return false;
		}
		if (!CanHaveSkills())
		{
			return false;
		}
		if (GetSkillsCount() >= NumSkillSlots())
		{
			return false;
		}
		return true;
	}

	public bool ValidateSkillReplace(int slot, SkillReplacement replacement)
	{
		if (replacement == null)
		{
			return false;
		}
		if (!CanSetSkill(slot, replacement.skill_def))
		{
			return false;
		}
		if (replacement.cost != null && !GetKingdom().resources.CanAfford(replacement.cost, 1f))
		{
			return false;
		}
		switch (replacement.type)
		{
		case SkillReplacement.Type.Affinity:
		{
			if (!CanLearnNewSkills())
			{
				return false;
			}
			string slot_type = SkillSlotType(slot);
			List<Skill.Def> newSkillOptions = GetNewSkillOptions(slot_type);
			if (newSkillOptions == null)
			{
				return false;
			}
			if (!newSkillOptions.Contains(replacement.skill_def))
			{
				return false;
			}
			return true;
		}
		case SkillReplacement.Type.Book:
		{
			Book.Def book_def = replacement.book_def;
			if (book_def == null)
			{
				return false;
			}
			if (book_def.skill != replacement.skill_def)
			{
				return false;
			}
			Kingdom kingdom = GetKingdom();
			if (kingdom == null || kingdom.FindBookIdx(book_def) < 0)
			{
				return false;
			}
			return true;
		}
		case SkillReplacement.Type.Realm:
			_ = replacement.realm;
			return false;
		case SkillReplacement.Type.GreatPerson:
		{
			Character great_person = replacement.great_person;
			if (great_person == null)
			{
				return false;
			}
			if (!great_person.IsAlive())
			{
				return false;
			}
			if (great_person.GetKingdom() != GetKingdom())
			{
				return false;
			}
			if (great_person.GetSkill(replacement.skill_def.name) == null)
			{
				return false;
			}
			return true;
		}
		case SkillReplacement.Type.Cleric:
		{
			Character cleric = replacement.cleric;
			if (cleric == null)
			{
				return false;
			}
			if (!cleric.IsAlive())
			{
				return false;
			}
			BrightPersonStatus brightPersonStatus = cleric.FindStatus<BrightPersonStatus>();
			if (brightPersonStatus == null)
			{
				return false;
			}
			brightPersonStatus.skill_def_keys.Contains(replacement.skill_def.name);
			return true;
		}
		default:
			return false;
		}
	}

	public bool CanSetSkill(int slot, Skill.Def def)
	{
		if (slot < 0 || slot > GetSkillsCount())
		{
			return false;
		}
		if (def == null)
		{
			return true;
		}
		string text = SkillSlotType(slot);
		if (text == null)
		{
			return false;
		}
		if (def.GetSlotType(this) != text)
		{
			return false;
		}
		if (GetSkill(def.name) != null)
		{
			return false;
		}
		return true;
	}

	public List<SkillReplacement> GetSkillReplaceOptions(int slot, SkillReplacement.Type type, bool add = false)
	{
		if (!add)
		{
			tmp_skill_replace.Clear();
		}
		if (slot < 0 || slot > GetSkillsCount())
		{
			return tmp_skill_replace;
		}
		string text = SkillSlotType(slot);
		if (text == null)
		{
			return tmp_skill_replace;
		}
		List<Skill.Def> newSkillOptions = GetNewSkillOptions(text);
		switch (type)
		{
		case SkillReplacement.Type.Affinity:
		{
			if (slot != GetSkillsCount() || !CanLearnNewSkills() || newSkillOptions == null)
			{
				break;
			}
			for (int num2 = 0; num2 < newSkillOptions.Count; num2++)
			{
				Skill.Def def2 = newSkillOptions[num2];
				if (CanSetSkill(slot, def2))
				{
					tmp_skill_replace.Add(new SkillReplacement(type, def2, null));
				}
			}
			break;
		}
		case SkillReplacement.Type.Book:
		{
			Kingdom kingdom4 = GetKingdom();
			if (kingdom4 == null || kingdom4.books == null)
			{
				break;
			}
			for (int num = 0; num < kingdom4.books.Count; num++)
			{
				Book book = kingdom4.books[num];
				if (book.def.type == Book.Type.Skill && CanSetSkill(slot, book.def.skill) && (slot != GetSkillsCount() || newSkillOptions == null || !newSkillOptions.Contains(book.def.skill)))
				{
					tmp_skill_replace.Add(new SkillReplacement(type, book.def.skill, book.def));
				}
			}
			break;
		}
		case SkillReplacement.Type.Realm:
		{
			Kingdom kingdom3 = GetKingdom();
			if (kingdom3 == null)
			{
				break;
			}
			for (int m = 0; m < kingdom3.realms.Count; m++)
			{
				Realm realm = kingdom3.realms[m];
				if (realm.skills == null)
				{
					continue;
				}
				for (int n = 0; n < realm.skills.Count; n++)
				{
					Skill.Def def = realm.skills[n];
					if (CanSetSkill(slot, def) && (slot != GetSkillsCount() || newSkillOptions == null || !newSkillOptions.Contains(def)))
					{
						tmp_skill_replace.Add(new SkillReplacement(type, def, realm));
					}
				}
			}
			break;
		}
		case SkillReplacement.Type.GreatPerson:
		{
			List<Character> list = game.GetComponent<FamousPersonSpawner>()?.famous_people;
			if (list == null)
			{
				break;
			}
			Kingdom kingdom2 = GetKingdom();
			for (int k = 0; k < list.Count; k++)
			{
				Character character2 = list[k];
				if (character2.skills == null || character2.GetKingdom() != kingdom2)
				{
					continue;
				}
				for (int l = 0; l < character2.skills.Count; l++)
				{
					Skill skill = character2.skills[l];
					if (CanSetSkill(slot, skill.def) && (slot != GetSkillsCount() || newSkillOptions == null || !newSkillOptions.Contains(skill.def)))
					{
						tmp_skill_replace.Add(new SkillReplacement(type, skill.def, character2));
					}
				}
			}
			break;
		}
		case SkillReplacement.Type.Cleric:
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom == null)
			{
				break;
			}
			if (kingdom.court != null)
			{
				for (int i = 0; i < kingdom.court.Count; i++)
				{
					Character character = kingdom.court[i];
					if (character != null)
					{
						EvaluateTeacher(character);
					}
				}
			}
			if (kingdom.royalFamily != null && kingdom.royalFamily.Children != null && kingdom.royalFamily.Children.Count > 0)
			{
				for (int j = 0; j < kingdom.royalFamily.Children.Count; j++)
				{
					EvaluateTeacher(kingdom.royalFamily.Children[j]);
				}
			}
			break;
		}
		}
		return tmp_skill_replace;
		void EvaluateTeacher(Character c)
		{
			if (c != null && c != this)
			{
				BrightPersonStatus brightPersonStatus = c.FindStatus<BrightPersonStatus>();
				if (brightPersonStatus != null)
				{
					if (c.skills != null && c.skills.Count > 0)
					{
						for (int num3 = 0; num3 < c.skills.Count; num3++)
						{
							Skill skill2 = c.skills[num3];
							if (CanSetSkill(slot, skill2.def))
							{
								tmp_skill_replace.Add(new SkillReplacement(type, skill2.def, c));
							}
						}
					}
					if (brightPersonStatus.skill_def_keys != null && brightPersonStatus.skill_def_keys.Count != 0)
					{
						foreach (string skill_def_key in brightPersonStatus.skill_def_keys)
						{
							Skill.Def def3 = game.defs.Get<Skill.Def>(skill_def_key);
							if (def3 != null && CanSetSkill(slot, def3))
							{
								bool flag = false;
								for (int num4 = 0; num4 < tmp_skill_replace.Count; num4++)
								{
									if (tmp_skill_replace[num4].skill_def == def3)
									{
										flag = true;
										break;
									}
								}
								if (!flag)
								{
									tmp_skill_replace.Add(new SkillReplacement(type, def3, c));
								}
							}
						}
					}
				}
			}
		}
	}

	public bool ReplaceSkill(int slot, SkillReplacement replacement)
	{
		if (!IsAlive())
		{
			return false;
		}
		if (!ValidateSkillReplace(slot, replacement))
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new ReplaceSkillEvent(slot, replacement));
			return true;
		}
		SetSkill(slot, replacement.skill_def);
		if (replacement.type == SkillReplacement.Type.Book)
		{
			Kingdom kingdom = GetKingdom();
			Book.Def book_def = replacement.book_def;
			kingdom.DelBook(book_def);
		}
		if (replacement.cost != null)
		{
			Kingdom kingdom2 = GetKingdom();
			KingdomAI.Expense.Category expenseCategory = GetExpenseCategory();
			kingdom2.SubResources(expenseCategory, replacement.cost);
		}
		return true;
	}

	public bool ReplaceSkill(Skill skill, Skill.Def def, bool send_state = true)
	{
		if (!IsAlive())
		{
			return false;
		}
		if (skills == null)
		{
			return false;
		}
		int num = skills.IndexOf(skill);
		if (num < 0)
		{
			return false;
		}
		return SetSkill(num, def, send_state);
	}

	public bool SetSkill(int slot, Skill.Def def, bool send_state = true)
	{
		int num = NumSkillSlots();
		if (slot < 0 || slot > num)
		{
			return false;
		}
		Skill skill = GetSkill(slot);
		if (skill?.def == def)
		{
			return true;
		}
		if (skill != null)
		{
			if (skill.mods_row != null)
			{
				skill.mods_row.Revert(this);
				skill.mods_row = null;
			}
			skill.owner = null;
		}
		if (skills == null)
		{
			skills = new List<Skill>(NumSkillSlots());
		}
		while (skills.Count <= slot)
		{
			skills.Add(null);
		}
		Resource learnCost = def.GetLearnCost(this);
		int num2 = NewSkillRank(def);
		if (num2 == 0)
		{
			Game.Log($"{this} trying to learn invalid skill: {def}", Game.LogType.Error);
			return false;
		}
		Skill value = new Skill(def, this, num2);
		skills[slot] = value;
		if (IsAuthority())
		{
			GenerateNewSkills(force_new: true);
		}
		UpdateAutomaticStatuses();
		if (IsAuthority() && IsKing())
		{
			GetKingdom().RefreshRoyalChildrenSkills();
		}
		NotifyListeners("skills_changed");
		if (send_state)
		{
			SendState<SkillsState>();
			OnLevelUpAnalytics(def, learnCost);
		}
		return true;
	}

	public bool CanAddSkill(Skill.Def skill_def)
	{
		if (!skill_def.IsApplicableTo(class_def))
		{
			return false;
		}
		if (GetNewSkillSlot(skill_def) < 0)
		{
			return false;
		}
		return true;
	}

	public int GetNewSkillSlot(Skill.Def skill_def)
	{
		string slotType = skill_def.GetSlotType(this);
		if (slotType == null)
		{
			return -1;
		}
		if (GetSkill(skill_def.name) != null)
		{
			return -1;
		}
		int num = NumSkillSlots();
		for (int i = 0; i < num; i++)
		{
			if (!(SkillSlotType(i) != slotType) && GetSkill(i) == null)
			{
				return i;
			}
		}
		return -1;
	}

	public void AddSkill(Skill.Def skill_def, bool send_state = true)
	{
		if (!IsAlive())
		{
			return;
		}
		if (!IsAuthority())
		{
			SendEvent(new AddSkillEvent(skill_def));
			return;
		}
		int newSkillSlot = GetNewSkillSlot(skill_def);
		if (newSkillSlot >= 0)
		{
			SetSkill(newSkillSlot, skill_def, send_state);
		}
	}

	public Skill.Def AddRandomNewSkill()
	{
		if (CanLearnNewSkills() && new_skills != null && new_skills.Count > 0)
		{
			Skill.Def def = new_skills[game.Random(0, new_skills.Count)];
			AddSkill(def);
			return def;
		}
		Game.Log($"{this} could not learn random new skill!", Game.LogType.Warning);
		return null;
	}

	public void InitDefaultSkill()
	{
	}

	public List<Skill.Def> GetApplicableSkills()
	{
		int num_primary;
		return GetApplicableSkills(out num_primary);
	}

	public List<Skill.Def> GetApplicableSkills(out int num_primary)
	{
		num_primary = 0;
		if (IsPrisoner())
		{
			return null;
		}
		Defs.Registry registry = game.defs.Get(typeof(Skill.Def));
		if (registry == null || registry.defs.Count == 0)
		{
			return null;
		}
		List<Skill.Def> list = new List<Skill.Def>(registry.defs.Count);
		foreach (KeyValuePair<string, Def> def2 in registry.defs)
		{
			Skill.Def def = def2.Value as Skill.Def;
			if (CanAddSkill(def))
			{
				if (def.GetSlotType(this) == "primary")
				{
					list.Insert(0, def);
					num_primary++;
				}
				else
				{
					list.Add(def);
				}
			}
		}
		return list;
	}

	public bool CanHaveSkills()
	{
		if (!IsInCourt() && !(title == "King") && !(title == "Prince") && !IsRebel() && !IsMercenary())
		{
			return IsPrisoner();
		}
		return true;
	}

	public void GenerateNewSkills(bool force_new = false, bool specialization = false)
	{
		if (game == null || game.state == Game.State.InLobby || !AssertAuthority())
		{
			return;
		}
		if (force_new)
		{
			new_skills = null;
		}
		if (!CanLearnNewSkills() || new_skills != null)
		{
			return;
		}
		int num_primary;
		List<Skill.Def> applicableSkills = GetApplicableSkills(out num_primary);
		if (applicableSkills == null || applicableSkills.Count == 0)
		{
			Game.Log($"Failed to find any applicable skills for {this}!", Game.LogType.Error);
			return;
		}
		int num = game.Random(2, 5) + (int)GetKingdom().GetStat(Stats.ks_character_additional_skill_pick);
		if (num > applicableSkills.Count)
		{
			num = applicableSkills.Count;
		}
		if (specialization)
		{
			new_skills = DecidePrinceSpecializationSkills(applicableSkills, num, num_primary);
		}
		if (new_skills == null)
		{
			new_skills = new List<Skill.Def>(num);
			for (int i = 0; i < num; i++)
			{
				int max = applicableSkills.Count;
				if (i == 0 && num_primary > 0)
				{
					max = num_primary;
				}
				int index = game.Random(0, max);
				new_skills.Add(applicableSkills[index]);
				applicableSkills.RemoveAt(index);
			}
		}
		SendState<SkillsState>();
		NotifyListeners("skills_changed");
	}

	private void AddNewSkill(ref List<Skill.Def> lst, Skill.Def sdef, string slot_type, bool include_invalid = false)
	{
		if (sdef == null || (GetSkill(sdef.name) != null && !include_invalid) || (lst != null && lst.Contains(sdef)))
		{
			return;
		}
		string slotType = sdef.GetSlotType(this);
		if ((slotType != null || include_invalid) && (slot_type == null || !(slotType != slot_type) || !(slot_type != "all")))
		{
			if (lst == null)
			{
				lst = new List<Skill.Def>();
			}
			lst.Add(sdef);
		}
	}

	private void AddNewSkills(ref List<Skill.Def> lst, List<Skill.Def> skill_defs, string slot_type, bool include_invalid = false)
	{
		if (skill_defs != null)
		{
			for (int i = 0; i < skill_defs.Count; i++)
			{
				Skill.Def sdef = skill_defs[i];
				AddNewSkill(ref lst, sdef, slot_type, include_invalid);
			}
		}
	}

	private void AddNewSkills(ref List<Skill.Def> lst, List<Skill> skills, string slot_type, bool include_invalid = false)
	{
		if (skills != null)
		{
			for (int i = 0; i < skills.Count; i++)
			{
				AddNewSkill(ref lst, skills[i]?.def, slot_type, include_invalid);
			}
		}
	}

	private void AddNewSkills(ref List<Skill.Def> lst, List<Tradition> traditions, string slot_type, bool include_invalid = false)
	{
		if (traditions != null)
		{
			for (int i = 0; i < traditions.Count; i++)
			{
				AddNewSkills(ref lst, traditions[i]?.def?.skills, slot_type, include_invalid);
			}
		}
	}

	private void AddNewSkills(ref List<Skill.Def> lst, List<Tradition.Def.SkillInfo> skills, string slot_type, bool include_invalid = false)
	{
		if (skills == null)
		{
			return;
		}
		for (int i = 0; i < skills.Count; i++)
		{
			Tradition.Def.SkillInfo skillInfo = skills[i];
			if (skillInfo.grants)
			{
				AddNewSkill(ref lst, skillInfo.def, slot_type, include_invalid);
			}
		}
	}

	public List<Skill.Def> GetNewSkillOptions(string slot_type, bool include_invalid = false, bool allow_generate_new_skills = true)
	{
		if (!IsAlive())
		{
			return null;
		}
		if (!CanHaveSkills())
		{
			return null;
		}
		if (!CanLearnNewSkills())
		{
			return null;
		}
		if (IsAuthority() && allow_generate_new_skills)
		{
			GenerateNewSkills();
		}
		List<Skill.Def> lst = null;
		AddNewSkills(ref lst, new_skills, slot_type, include_invalid);
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			AddNewSkills(ref lst, kingdom.traditions, slot_type, include_invalid);
		}
		Character father = GetFather();
		if (father != null)
		{
			AddNewSkills(ref lst, father.skills, slot_type, include_invalid);
		}
		return lst;
	}

	private List<Skill.Def> DecidePrinceSpecializationSkills(List<Skill.Def> options, int count, int num_primary)
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			Game.Log("Failed to generate new specialization skills: no kingdom", Game.LogType.Error);
			return null;
		}
		RoyalFamily royalFamily = kingdom.royalFamily;
		if (royalFamily == null)
		{
			Game.Log("Failed to generate new specialization skills: no royal family", Game.LogType.Error);
			return null;
		}
		Dictionary<Skill.Def, float> dictionary = new Dictionary<Skill.Def, float>();
		float num = 0f;
		for (int i = 0; i < options.Count; i++)
		{
			float num2 = royalFamily.GetPrinceSpecializationDefaultSkillWeight();
			Character father = GetFather();
			if (father != null && father.skills != null)
			{
				for (int j = 0; j < father.skills.Count; j++)
				{
					if (father.skills[j]?.def?.name == options[i].name)
					{
						num2 += royalFamily.GetPrinceSpecializationFatherSkillWeight();
						break;
					}
				}
			}
			if (kingdom.traditions != null)
			{
				for (int k = 0; k < kingdom.traditions.Count; k++)
				{
					if (kingdom.traditions[k]?.def?.GrantsSkill(options[i]) ?? false)
					{
						num2 += royalFamily.GetPrinceSpecializationTraditionSkillWeight();
						break;
					}
				}
			}
			if (i < num_primary)
			{
				num2 += royalFamily.GetPrinceSpecializationPrimarySkillWeight();
			}
			dictionary.Add(options[i], num2);
			num += num2;
		}
		List<Skill.Def> list = new List<Skill.Def>();
		while (list.Count < count)
		{
			float num3 = game.Random(0f, num);
			float num4 = 0f;
			foreach (KeyValuePair<Skill.Def, float> item in dictionary)
			{
				num4 += item.Value;
				if (num4 > num3)
				{
					if (!list.Contains(item.Key))
					{
						list.Add(item.Key);
					}
					break;
				}
			}
		}
		return list;
	}

	public Skill.Def ChooseNewSkill(List<Skill.Def> skills)
	{
		if (skills == null)
		{
			return null;
		}
		Skill.Def result = null;
		Kingdom kingdom = GetKingdom();
		if (kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default)
		{
			List<Skill.Def> list = new List<Skill.Def>();
			int num = 0;
			for (int i = 0; i < skills.Count; i++)
			{
				if (skills[i].GetClassLevelValue(class_def) >= num)
				{
					list.Add(skills[i]);
				}
			}
			if (kingdom.ai.personality == KingdomAI.AIPersonality.AntiRebellion)
			{
				float num2 = 0f;
				for (int j = 0; j < list.Count; j++)
				{
					num2 += list[j].ai_eval;
				}
				float num3 = game.Random(0f, num2);
				for (int k = 0; k < list.Count; k++)
				{
					if (num3 < list[k].ai_eval)
					{
						result = list[k];
						break;
					}
					num3 -= list[k].ai_eval;
				}
			}
			else
			{
				float num4 = 0f;
				for (int l = 0; l < list.Count; l++)
				{
					num4 += list[l].ai_eval;
				}
				float num5 = game.Random(0f, num4);
				for (int m = 0; m < list.Count; m++)
				{
					if (num5 < list[m].ai_eval)
					{
						result = list[m];
						break;
					}
					num5 -= list[m].ai_eval;
				}
			}
		}
		else
		{
			int num6 = 0;
			for (int n = 0; n < skills.Count; n++)
			{
				Skill.Def def = skills[n];
				int classLevelValue = def.GetClassLevelValue(class_def);
				if (classLevelValue >= num6 && (classLevelValue != num6 || game.Random(0, 100) >= 50))
				{
					result = def;
					num6 = classLevelValue;
				}
			}
		}
		return result;
	}

	public bool ThinkNewSkill(bool all = false, bool for_free = false)
	{
		if (!IsAlive())
		{
			return false;
		}
		if (!for_free && !CanLearnNewSkills())
		{
			return false;
		}
		Skill.Def def = ChooseNewSkill(all ? GetApplicableSkills() : GetNewSkillOptions(null));
		if (def == null)
		{
			return false;
		}
		if (!for_free)
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom != null)
			{
				Resource learnCost = def.GetLearnCost(this);
				if (!kingdom.resources.CanAfford(learnCost, 1f))
				{
					return false;
				}
				KingdomAI.Expense.Category expenseCategory = GetExpenseCategory();
				kingdom.SubResources(expenseCategory, learnCost);
			}
		}
		AddSkill(def);
		return true;
	}

	public bool ThinkUpgradeSkill(bool for_free = false)
	{
		if (skills == null)
		{
			return false;
		}
		for (int i = 0; i < skills.Count; i++)
		{
			Skill skill = skills[i];
			if (skill == null || !CanAddSkillRank(skill))
			{
				continue;
			}
			if (!for_free)
			{
				Kingdom kingdom = GetKingdom();
				if (kingdom != null)
				{
					Resource upgardeCost = skill.def.GetUpgardeCost(this);
					if (!kingdom.resources.CanAfford(upgardeCost, 1f))
					{
						return false;
					}
					KingdomAI.Expense.Category expenseCategory = GetExpenseCategory();
					Kingdom.in_AI_spend = true;
					kingdom.SubResources(expenseCategory, upgardeCost);
					Kingdom.in_AI_spend = false;
				}
			}
			AddSkillRank(skill);
			return true;
		}
		return false;
	}

	public bool ThinkSkills(bool all = false, bool for_free = false)
	{
		if (CoopThread.current != null)
		{
			if (IsPrisoner())
			{
				return false;
			}
			if (!IsInCourt())
			{
				return false;
			}
			if (!IsKingOrPrince())
			{
				Kingdom kingdom = GetKingdom();
				float stat = kingdom.GetStat(Stats.ks_max_books);
				if (kingdom.resources[ResourceType.Books] < stat * 0.9f)
				{
					return false;
				}
			}
		}
		if (game.Random(0, 100) < 50 && ThinkUpgradeSkill(for_free))
		{
			return true;
		}
		if (ThinkNewSkill(all, for_free))
		{
			return true;
		}
		return false;
	}

	public List<RoyalAbility> GetRoyalAbilities()
	{
		if (royal_abilities != null)
		{
			return royal_abilities;
		}
		if (!IsKingOrPrince() && !IsQueenOrPrincess())
		{
			return null;
		}
		if (!IsAuthority())
		{
			return null;
		}
		GenerateRoyalAbilities();
		SendState<RoyalAbilitiesState>();
		NotifyListeners("royal_abilities_changed");
		return royal_abilities;
	}

	public int GetMaxAbilityCount()
	{
		DT.Field field = game.dt.Find("CharacterClass.royal_abilities");
		DT.Field field2 = ((sex == Sex.Female) ? field.FindChild("female") : field.FindChild("male"));
		if (field2 == null)
		{
			return 0;
		}
		return field2.FindChild("value").Value(1);
	}

	private void GenerateRoyalAbilities()
	{
		royal_abilities = new List<RoyalAbility>();
		DT.Field field = game.dt.Find("CharacterClass.royal_abilities");
		if (field == null || field.children == null)
		{
			return;
		}
		DT.Field field2 = field.FindChild("ability_names");
		if (field2 == null || field2.children == null)
		{
			return;
		}
		DT.Field field3 = null;
		if (sex == Sex.Female)
		{
			field3 = field.FindChild("female");
		}
		if (sex == Sex.Male)
		{
			field3 = field.FindChild("male");
		}
		if (field3 == null)
		{
			return;
		}
		DT.Field field4 = field3.FindChild("value");
		if (field4 == null)
		{
			return;
		}
		Value value = field4.Value(0);
		Value value2 = field4.Value(1);
		int num = field3.GetInt("max_count", null, field2.children.Count);
		if (num < 0)
		{
			num = 0;
		}
		if (num > field2.children.Count)
		{
			num = field2.children.Count;
		}
		List<int> randomIndexes = game.GetRandomIndexes(field2.children.Count);
		randomIndexes.RemoveRange(num, field2.children.Count - num);
		for (int i = 0; i < field2.children.Count; i++)
		{
			DT.Field field5 = field2.children[i];
			if (!(field5.type != "text") && !string.IsNullOrEmpty(field5.key))
			{
				int val = 0;
				if (randomIndexes.Contains(i))
				{
					val = game.Random(value, (int)value2 + 1);
				}
				royal_abilities.Add(new RoyalAbility
				{
					name = field5.key,
					val = val,
					val_min = value,
					val_max = value2
				});
			}
		}
	}

	public int GetRoyalAbility(string name)
	{
		List<RoyalAbility> royalAbilities = GetRoyalAbilities();
		if (royalAbilities == null)
		{
			return 0;
		}
		for (int i = 0; i < royalAbilities.Count; i++)
		{
			RoyalAbility royalAbility = royalAbilities[i];
			if (royalAbility.name == name)
			{
				return royalAbility.val;
			}
		}
		return 0;
	}

	public int GetKingAbility(string name)
	{
		if (!IsKing())
		{
			return 0;
		}
		List<RoyalAbility> royalAbilities = GetRoyalAbilities();
		if (royalAbilities == null)
		{
			return 0;
		}
		List<RoyalAbility> list = GetMarriage()?.GetSpouse(this)?.GetRoyalAbilities();
		for (int i = 0; i < royalAbilities.Count; i++)
		{
			RoyalAbility royalAbility = royalAbilities[i];
			if (royalAbility.name == name)
			{
				int num = royalAbility.val;
				if (list != null)
				{
					num += list[i].val;
				}
				if (num < royalAbility.val_min)
				{
					num = royalAbility.val_min;
				}
				if (num > royalAbility.val_max)
				{
					num = royalAbility.val_max;
				}
				return num;
			}
		}
		return 0;
	}

	public override void OnInit()
	{
		base.OnInit();
		stats = new Stats(this);
		governModifiers = game.defs.Get<GovernModifiers.Def>("GovernModifiers");
		RestartNotGoverningTimer();
	}

	public override Stats GetStats()
	{
		return stats;
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (class_def == null)
		{
			class_def = game.defs.GetBase<CharacterClass.Def>();
		}
		UpdateInBatch(game.update_10sec);
		RefreshPortraitVariant(base_too: true, check_old_valid: true);
		if (!ShouldBeAging())
		{
			EnableAging(can_die: false);
		}
		if (GetKingdom() == null)
		{
			SetKingdom(FactionUtils.GetFactionKingdom(game, "ExileFaction").id);
		}
	}

	public bool ShouldBeAging()
	{
		if (IsKingOrPrince())
		{
			return true;
		}
		if (IsPatriarch())
		{
			return true;
		}
		if (IsPope())
		{
			return true;
		}
		if (IsQueen())
		{
			return true;
		}
		if (IsRoyalChild())
		{
			return true;
		}
		if (IsInCourt() && game.rules.KnightAging())
		{
			return true;
		}
		return false;
	}

	protected override bool CanBeDelayDestroyed()
	{
		return true;
	}

	protected override void OnFinish()
	{
		if (IsFamous())
		{
			FamousPersonSpawner component = game.GetComponent<FamousPersonSpawner>();
			if (component != null)
			{
				component.famous_people.Remove(this);
				if (location is Village village)
				{
					village.famous_person = null;
				}
			}
		}
		Imprison(null, recall: false, send_state: false);
		StopGoverning(reloadLabel: true, send_state: false);
		StopPromotingPaganBelief(apply_penalties: false, !IsKing());
		StopTrade();
		StopDiplomacy();
		DestroyPact();
		SetMissionKingdom(null, send_state: false);
		SetMissionRealm(null, send_state: false);
		ClearPuppets(send_states: false);
		ClearMasters(null, send_states: false);
		Divorce("destroy");
		SetLocation(null, send_state: false);
		stats?.DelRefModifiers();
		if (skill_mods != null)
		{
			skill_mods.RevertAll();
			skill_mods = null;
		}
		bool send_state = IsAuthority();
		Kingdom kingdom = GetKingdom();
		if (kingdom != null)
		{
			kingdom.DelCourtMember(this, send_state, kill_or_throneroom: false);
			kingdom.DelSpecialCourtMember(this, send_state);
			kingdom.royalFamily?.RemoveRelative(this);
		}
		Kingdom specialCourtKingdom = GetSpecialCourtKingdom();
		if (specialCourtKingdom != null)
		{
			specialCourtKingdom.DelCourtMember(this, send_state, kill_or_throneroom: false);
			specialCourtKingdom.DelSpecialCourtMember(this, send_state);
		}
		Army army = GetArmy();
		SetLocation(null);
		if (IsAuthority() && army != null && army.IsValid() && army.leader == this)
		{
			Battle battle = army.battle;
			if (battle == null && !IsCrusader() && !IsRebel() && !IsMercenary() && army.units.Count > 1)
			{
				army.BecomeMercenary(army.GetKingdom().id);
			}
			else if (!IsCrusader())
			{
				army.SetLeader(null);
				army.GetKingdom().DelArmy(army, del_court_member: false);
				army.Destroy();
				if (battle != null && !battle.IsFinishing())
				{
					battle.CheckVictory();
				}
			}
			else if (game.religions.catholic.crusade.IsValid() && string.IsNullOrEmpty(game.religions.catholic.crusade.end_reason))
			{
				game.religions.catholic.crusade.CheckOver();
			}
		}
		game.NotifyListeners("clear_portrait", this);
		base.OnFinish();
	}

	public override void OnEvent(Event evt)
	{
		base.OnEvent(evt);
	}

	public void OnKingdomReligionChanged()
	{
		NotifyListeners("kingdom_religion_changed");
	}

	public void OnGovernedRealmReligionChanged()
	{
		NotifyListeners("governed_realm_religion_changed");
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		CheckAging();
		CheckGoverningWarning();
		CheckMissionPassiveRelIncrease();
	}

	public void CheckMissionPassiveRelIncrease()
	{
		if (sex == Sex.Male && mission_kingdom != null)
		{
			float stat = GetStat(Stats.cs_mission_passive_rel_increase);
			if (stat != 0f)
			{
				float perm = stat / 60f * 10f;
				mission_kingdom.AddRelationship(GetKingdom(), perm, null);
			}
		}
	}

	public override void OnDefsReloaded()
	{
		stats.OnDefsReloaded();
		if (skill_mods != null)
		{
			skill_mods.RevertAll();
			skill_mods = null;
		}
		RefreshTags();
	}

	public bool IsDead()
	{
		if (IsValid())
		{
			if (status != null)
			{
				return status.IsDead();
			}
			return false;
		}
		return true;
	}

	public bool IsAlive()
	{
		if (IsValid())
		{
			return !IsDead();
		}
		return false;
	}

	public virtual bool DesertKingdom(bool createClone = true, Kingdom newKingdom = null, bool dieIfNoNewKingdom = true, bool keep_army = false)
	{
		AssertAuthority();
		Kingdom kingdom = GetKingdom();
		if (!kingdom.court.Contains(this))
		{
			return false;
		}
		if (cur_action != null)
		{
			cur_action.Cancel();
		}
		StopTrade();
		StopDiplomacy();
		StopImprovingOpinions();
		SetMissionKingdom(null);
		SetMissionRealm(null);
		ClearPuppets();
		StopGoverning();
		StopPromotingPaganBelief(apply_penalties: false);
		ClearMasters("Betray");
		DisolvePact("OwnerDeserted");
		DelStatus<TradeExpeditionStatus>();
		if (IsPrepairngToGovern(out var a))
		{
			a.Cancel();
		}
		if (IsKing())
		{
			kingdom.royalFamily.Uncrown("desert_kingdom", kingdom.royalFamily.GetSovereignUncrownData());
		}
		else
		{
			if (this == kingdom?.royalFamily?.Heir)
			{
				kingdom?.royalFamily?.Unheir();
			}
			kingdom.royalFamily.RemoveChild(this);
			kingdom.royalFamily.RemoveRelative(this);
			if (createClone)
			{
				int index = 0;
				for (int i = 0; i < kingdom.court.Count; i++)
				{
					if (this == kingdom.court[i])
					{
						index = i;
						break;
					}
				}
				kingdom.DelCourtMember(this, send_state: true, kill_or_throneroom: false);
				Character character = Clone();
				kingdom.AddCourtMember(character, index);
				character.Die(new EscapedStatus());
			}
			else
			{
				kingdom.DelCourtMember(this, send_state: true, kill_or_throneroom: false);
			}
		}
		SetTitle("Knight");
		kingdom.GetCrownAuthority().AddModifier("knightDesertion");
		if (newKingdom != null)
		{
			SetKingdom(newKingdom.id);
			newKingdom.AddCourtMember(this, -1, is_hire: false, send_state: true, send_event: false, check_foreign: false);
			if (keep_army)
			{
				Army army = GetArmy();
				if (army != null)
				{
					if (army.castle != null)
					{
						army.LeaveCastle(army.castle.GetRandomExitPoint());
					}
					army.SetKingdom(newKingdom.id);
				}
			}
			else
			{
				DisbandArmy();
				if (kingdom != null && IsMarshal() && !kingdom.IsDefeated() && GetArmy() == null)
				{
					ClearStatus();
					SpawnArmy(kingdom.GetCapital()?.castle);
				}
			}
		}
		else if (dieIfNoNewKingdom)
		{
			Die(null, "desertion");
		}
		else
		{
			DisbandArmy();
			if (kingdom != null && IsMarshal() && !kingdom.IsDefeated() && GetArmy() == null)
			{
				ClearStatus();
				SpawnArmy(kingdom.GetCapital()?.castle);
			}
		}
		ClearDefaultStatus();
		return true;
	}

	private void BecomeNormalMercenary()
	{
		if (IsExMercenary())
		{
			SetKingdom(FactionUtils.GetFactionKingdom(game, "MercenaryFaction").id);
		}
	}

	public virtual void Die(Status status = null, string reason = "")
	{
		if (!AssertAuthority())
		{
			return;
		}
		Kingdom kingdom = GetKingdom();
		kingdom.StartRecheckKingTimer();
		if (status == null)
		{
			status = new DeadStatus(reason, this);
		}
		DeadStatus deadStatus = status as DeadStatus;
		if (IsPatriarch() || IsEcumenicalPatriarch())
		{
			status = ((deadStatus == null) ? new DeadPatriarchStatus(reason, this) : new DeadPatriarchStatus(deadStatus.reason, deadStatus.vars));
		}
		kingdom?.FireEvent("character_died", deadStatus?.vars);
		NotifyListeners("dying", status);
		string text = deadStatus?.reason;
		if (!(text == "exile") && !(text == "old_age"))
		{
			NotifyListeners("killed");
		}
		if (!game.IsUnloadingMap())
		{
			if (IsEcumenicalPatriarch())
			{
				game.religions.orthodox.OnCharacterDied(this);
			}
			else
			{
				kingdom?.religion?.OnCharacterDied(this);
				if (IsInSpecialCourt())
				{
					GetSpecialCourtKingdom()?.religion?.OnCharacterDied(this);
				}
			}
		}
		if (cur_action != null)
		{
			cur_action.Cancel(manual: false, notify: false);
		}
		RoyalFamily.SovereignUncrownData sovereignUncrownData = null;
		bool flag = IsKing() || kingdom?.royalFamily?.Sovereign == this;
		if (flag)
		{
			sovereignUncrownData = kingdom.royalFamily.GetSovereignUncrownData();
		}
		StopTrade();
		StopDiplomacy();
		StopImprovingOpinions();
		DisolvePact(((status as DeadStatus)?.reason == "exile") ? "OwnerExiled" : "OwnerDied");
		SetMissionKingdom(null);
		SetMissionRealm(null);
		ClearPuppets();
		ClearMasters("Death");
		Imprison(null, recall: false, send_state: true, null, destroy_if_free: false);
		StopGoverning();
		Divorce("death");
		StopPromotingPaganBelief(deadStatus != null && deadStatus.reason == "exile" && kingdom.GetStat(Stats.ks_discard_exile_abandon_penalties) == 0f, !flag);
		base.statuses?.DestroyAll();
		stats?.DelRefModifiers();
		new_skills = null;
		if (IsValid())
		{
			SetStatus(status);
			base.statuses?.SetDefaultType(status.rtti.type);
		}
		Army army = GetArmy();
		SetLocation(null);
		if (kingdom == null)
		{
			if (IsValid())
			{
				Destroy();
			}
			return;
		}
		if (kingdom.royalFamily != null)
		{
			if (kingdom.is_player && (kingdom.royalFamily.IsFamilyMember(this) || kingdom.royalFamily.Relatives.Contains(this)))
			{
				kingdom.royalFamily.OnFamilyChangedAnalytics(this, "died");
			}
			if (flag)
			{
				string reason2 = ((!string.IsNullOrEmpty(reason)) ? reason : ((!(status is DeadStatus)) ? "unknown" : (status as DeadStatus).reason));
				kingdom.royalFamily.Uncrown(reason2, sovereignUncrownData, army);
				if (game.ValidateEndGame())
				{
					return;
				}
			}
			else
			{
				kingdom.royalFamily.RemoveFamilyMember(this);
			}
			kingdom.royalFamily.RemoveRelative(this);
		}
		Kingdom originalKingdom = GetOriginalKingdom();
		if (originalKingdom != null)
		{
			originalKingdom.royalFamily?.RemoveRelative(this);
			originalKingdom.royalFamily?.RemoveChild(this);
		}
		if (army != null && army.IsValid() && army.leader == this)
		{
			Battle battle = army.battle;
			bool flag2 = reason == "exile" || deadStatus?.reason == "exile";
			if (battle == null && !IsCrusader() && !IsRebel() && !IsMercenary() && army.units.Count > 1 && !flag2)
			{
				army.BecomeMercenary(army.GetKingdom().id);
			}
			else if (!IsCrusader())
			{
				army.SetLeader(null);
				army.GetKingdom().DelArmy(army, del_court_member: false);
				army.Destroy();
				if (battle != null && !battle.IsFinishing())
				{
					battle.CheckVictory();
				}
			}
			else if (game.religions.catholic.crusade.IsValid() && string.IsNullOrEmpty(game.religions.catholic.crusade.end_reason))
			{
				game.religions.catholic.crusade.CheckOver();
			}
		}
		if (!IsInCourt())
		{
			if (IsValid())
			{
				Destroy();
			}
		}
		else
		{
			NotifyListeners("died", status);
		}
	}

	public override void UpdateAutomaticStatuses(bool now = false, bool force_recalc = false)
	{
		if (!IsValid())
		{
			return;
		}
		if (!now)
		{
			if (auto_statuses_valid)
			{
				if (base.statuses == null)
				{
					new Statuses(this);
				}
				else if (base.started)
				{
					base.statuses.UpdateNextFrame();
				}
				auto_statuses_valid = false;
			}
			RefreshTags();
		}
		else
		{
			if (auto_statuses_valid && !force_recalc)
			{
				return;
			}
			auto_statuses_valid = true;
			if (inUpdateAutomaticStatuses)
			{
				return;
			}
			inUpdateAutomaticStatuses = true;
			if (IsAlive())
			{
				if (CanBeGovernor())
				{
					if (IsGovernor())
					{
						DelStatus<NotGoverningStatus>();
						DelStatus<BanishedGovernorStatus>();
						AddStatus<GoverningStatus>();
					}
					else if (IsBanishedGovernor())
					{
						DelStatus<GoverningStatus>();
						DelStatus<NotGoverningStatus>();
						AddStatus<BanishedGovernorStatus>();
					}
					else
					{
						DelStatus<GoverningStatus>();
						DelStatus<BanishedGovernorStatus>();
						AddStatus<NotGoverningStatus>();
					}
				}
				else
				{
					DelStatus<GoverningStatus>();
					DelStatus<NotGoverningStatus>();
					DelStatus<BanishedGovernorStatus>();
				}
				if (IsCleric() && GetKingdom() != null && GetKingdom().is_pagan)
				{
					if (base.statuses?.Find<PaganBeliefStatus>() == null)
					{
						AddStatus<NoPaganBeliefStatus>();
					}
					else
					{
						DelStatus<NoPaganBeliefStatus>();
					}
				}
				else
				{
					DelStatus<NoPaganBeliefStatus>();
				}
				if (IsRebel() && IsInSpecialCourt())
				{
					AddStatus<RebelStatus>();
				}
				else
				{
					DelStatus<RebelStatus>();
				}
			}
			else
			{
				DelStatus<GoverningStatus>();
				DelStatus<NotGoverningStatus>();
				DelStatus<BanishedGovernorStatus>();
				DelStatus<BrightPersonStatus>();
				DelStatus<NoPaganBeliefStatus>();
				DelStatus<RebelStatus>();
			}
			UpdateAutomaticMainStatus();
			inUpdateAutomaticStatuses = false;
		}
	}

	private void UpdateAutomaticMainStatus()
	{
		Status status = base.statuses?.main;
		if (status != null && !status.IsAutomatic())
		{
			return;
		}
		if (IsRebel())
		{
			ClearStatus();
			return;
		}
		if (cur_action != null)
		{
			SetStatus<OngoingActionStatus>();
			return;
		}
		if (prison_kingdom != null)
		{
			SetStatus<InPrisonStatus>();
			return;
		}
		if (GetArmy() != null)
		{
			SetStatus<LeadArmyStatus>();
			return;
		}
		if (AvailableForAssignmentStatus.Validate(this))
		{
			SetStatus<AvailableForAssignmentStatus>();
			return;
		}
		if (base.statuses?.default_type == null)
		{
			if (IsInCourt())
			{
				SetStatus<IdleInCourtStatus>();
				return;
			}
			RoyalFamily royalFamily = GetKingdom()?.royalFamily;
			if (royalFamily != null && (royalFamily.IsFamilyMember(this) || royalFamily.Relatives.Contains(this)))
			{
				SetStatus<IdleThroneRoom>();
				return;
			}
		}
		ClearStatus();
	}

	private void RestartNotGoverningTimer()
	{
		next_govern_check = game.time + (governModifiers.notGoverningRepeatTime + game.Random(1, governModifiers.notGoverningRepeatTimePlusMinus + 1));
	}

	private void CheckGoverningWarning()
	{
		if (game.time < next_govern_check || !IsAlive())
		{
			return;
		}
		RestartNotGoverningTimer();
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || !kingdom.is_player || kingdom.realms == null || kingdom.realms.Count == 0 || actions == null)
		{
			return;
		}
		List<Character> court = kingdom.court;
		if (court != null && court.Contains(this))
		{
			Action action = actions?.Find("GovernCityAction");
			if (action != null && !(action.Validate() != "ok") && HasEligableNonGovornedRealms(kingdom, action))
			{
				NotifyListeners("not_governing");
			}
		}
		static bool HasEligableNonGovornedRealms(Kingdom kingdom2, Action action2)
		{
			if (kingdom2.realms == null)
			{
				return false;
			}
			for (int i = 0; i < kingdom2.realms.Count; i++)
			{
				Realm realm = kingdom2.realms[i];
				if (realm.castle != null && realm.castle.governor == null && realm.castle.GetPreparingGovernor() == null && action2.ValidateTarget(realm))
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool IsAging()
	{
		return next_age_check != Time.Zero;
	}

	public void ForceAge()
	{
		next_age_check = game.time;
		CheckAging();
	}

	public bool ShouldAge()
	{
		if (IsKing())
		{
			return true;
		}
		if (IsPope())
		{
			return true;
		}
		if (IsPatriarch())
		{
			return true;
		}
		if (IsEcumenicalPatriarch())
		{
			return true;
		}
		return false;
	}

	public void EnableAging(bool can_die, bool send_state = true)
	{
		if (this.can_die == can_die)
		{
			if (!can_die)
			{
				next_age_check = Time.Zero;
			}
			return;
		}
		this.can_die = can_die;
		if (!can_die)
		{
			if (age >= Age.Young)
			{
				next_age_check = Time.Zero;
			}
			NotifyAgeChnage();
			return;
		}
		float ageupDuration = GetAgeupDuration(age);
		next_age_check = game.time + ageupDuration;
		NotifyAgeChnage();
		if (IsAuthority() && send_state)
		{
			SendState<AgeState>();
		}
	}

	public void BecomeKingAgeAdjust()
	{
		EnableAging(can_die: true);
		if (age == Age.Adult && can_die && (IsPrince() || IsRoyalRelative()))
		{
			if (next_age_check == Time.Zero)
			{
				next_age_check = game.time + GetAgeupDuration(age);
			}
			next_age_check -= game.Random(0f, GetAgeupDuration(age) / 2f);
			NotifyAgeChnage();
		}
	}

	private void NotifyAgeChnage()
	{
		NotifyListeners("character_age_change");
		if (IsRoyalty())
		{
			GetKingdom()?.royalFamily.NotifyListeners("character_age_change", this);
		}
	}

	private bool CheckCanDieFromOldAge()
	{
		if (GetArmy()?.battle != null)
		{
			return false;
		}
		if (IsPrince() && IsInCourt() && !game.rules.KnightAging())
		{
			return false;
		}
		return true;
	}

	public void CheckAging()
	{
		if (!IsAuthority() || !IsAlive())
		{
			return;
		}
		if (can_die && next_die_check != Time.Zero && game.time > next_die_check)
		{
			if (CheckCanDieFromOldAge() && game.Random(0f, 100f) < GetDieChance(age, game))
			{
				next_die_check = Time.Zero;
				Vars vars = new Vars();
				FillDeadVars(vars, is_owner: true);
				Die(new DeadStatus("old_age", vars));
				return;
			}
			next_die_check = game.time + GetDieCheckInterval(age, game);
		}
		if (next_age_check == Time.Zero || game.time < next_age_check)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (age < Age.Venerable)
		{
			age++;
			if (!can_die && age >= Age.Young)
			{
				next_age_check = Time.Zero;
			}
			else
			{
				next_age_check = game.time + GetAgeupDuration(age);
			}
			if (sex == Sex.Female && age >= Age.Adult)
			{
				next_age_check = Time.Zero;
				age = Age.Adult;
			}
			if (age == Age.Adult && !can_die)
			{
				next_age_check = Time.Zero;
			}
			if (sex == Sex.Male && (class_def == null || class_def.IsBase() || IsClasslessPrince()))
			{
				if (IsHeir())
				{
					SetClass(GetKingdom().royalFamily.Sovereign.class_def);
				}
				else
				{
					SetClass(CharacterFactory.GetRandomClassDef(game));
				}
			}
			if (age == Age.Juvenile && !IsClasslessPrince())
			{
				GenerateNewSkills(force_new: true, specialization: true);
				flag = true;
			}
			if (age == Age.Young && (IsPrince() || IsPrincess() || IsRoyalRelative()))
			{
				if (sex == Sex.Male && (status == null || !(status is AvailableForAssignmentStatus)))
				{
					SetStatus<AvailableForAssignmentStatus>();
				}
				GenerateNewSkills(force_new: true, specialization: true);
				flag2 = true;
			}
		}
		else
		{
			next_age_check = Time.Zero;
		}
		if (age >= Age.Adult && (IsPrince() || IsRoyalRelative()) && !ShouldBeAging())
		{
			EnableAging(can_die: false);
		}
		if (age >= Age.Old && can_die && sex != Sex.Female)
		{
			next_die_check = game.time + GetDieCheckInterval(age, game);
		}
		SendState<AgeState>();
		UpdateAutomaticStatuses();
		NotifyAgeChnage();
		RefreshPortraitVariant(base_too: false, check_old_valid: true);
		Kingdom kingdom = GetKingdom();
		if (kingdom != null && kingdom.ai != null && kingdom.ai.Enabled(KingdomAI.EnableFlags.Characters) && (flag || flag2) && IsRoyalChild())
		{
			ThinkNewSkill(all: false, for_free: true);
			flag = false;
			flag2 = false;
		}
		if (flag)
		{
			FireEvent("character_specialization_query", null, kingdom_id);
		}
		if (flag2)
		{
			FireEvent("character_coming_of_age", null, kingdom_id);
		}
		if (kingdom.is_player && (kingdom.royalFamily.IsFamilyMember(this) || kingdom.royalFamily.Relatives.Contains(this)))
		{
			kingdom.royalFamily.OnFamilyChangedAnalytics(this, "age_changed");
		}
	}

	public void OnSovereignChanged(Character newSovereign)
	{
	}

	public float GetAgeupDuration(Age age)
	{
		return GetAgeupDuration(age, game);
	}

	public static float GetAgeupDuration(Age age, Game game)
	{
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		string path = "age_" + age.ToString().ToLowerInvariant();
		DT.Field field = agingDef.field.FindChild(path);
		if (field == null)
		{
			return 260000f;
		}
		float num = field.Float(0);
		float num2 = field.Float(1);
		if (num2 <= num)
		{
			return num * agingDef.age_multiplier;
		}
		if (age == Age.Venerable)
		{
			float num3 = game.Random(0f, 1f);
			return (num + num3 * num3 * num2) * agingDef.age_multiplier;
		}
		return game.Random(num, num2) * agingDef.age_multiplier;
	}

	public static int CalculateAgeInSeconds(Age age, Game game)
	{
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		int num = 0;
		for (int num2 = (int)age; num2 >= 0; num2--)
		{
			Age age2 = (Age)num2;
			string path = "age_" + age2.ToString().ToLowerInvariant();
			DT.Field field = agingDef.field.FindChild(path);
			if (field == null)
			{
				return -1;
			}
			num += field.Int(0);
		}
		return num;
	}

	public float GetDieCheckInterval(Age age, Game game)
	{
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		if (age == Age.Venerable)
		{
			return (float)game.Random(agingDef.age_venerable_min, agingDef.age_venerable_max) * agingDef.age_multiplier;
		}
		return game.Random(agingDef.die_check_interval_min, agingDef.die_check_interval_max) * agingDef.age_multiplier;
	}

	public float GetDieChance(Age age, Game game)
	{
		if (age == Age.Venerable)
		{
			return 100f;
		}
		return game.rules.GetAgingDef().die_chance_perc;
	}

	public void SetRoyalBloodPower(int bloodLinePower, bool send_state = true)
	{
		if (bloodLinePower >= 0 && bloodLinePower <= GetKingdom().royalFamily.def.max_royal_blood_power)
		{
			royalBloodPower = bloodLinePower;
			if (send_state)
			{
				SendState<BloodLineState>();
			}
		}
	}

	public void SetTitle(string title, bool send_state = true)
	{
		if (!(!IsAuthority() && send_state))
		{
			this.title = title;
			if (send_state)
			{
				SendState<TitleState>();
				RefreshPortraitVariant(base_too: false, check_old_valid: false);
			}
			NotifyListeners("title_changed");
		}
	}

	public string GetTitle()
	{
		Kingdom kingdom = GetKingdom();
		string text = title;
		if (string.IsNullOrEmpty(text))
		{
			text = "Knight";
		}
		if (kingdom != null && kingdom.religion != null)
		{
			string text2 = kingdom.religion.GetTitle(this);
			if (text2 != null)
			{
				return text2;
			}
		}
		if (kingdom == null || string.IsNullOrEmpty(kingdom.nobility_key))
		{
			return "Nobility." + text;
		}
		if (!string.IsNullOrEmpty(kingdom.nobility_level))
		{
			DT.Def def = game.dt.FindDef(kingdom.nobility_key);
			if (def != null && def.field != null)
			{
				DT.Field field = def.field.FindChild("title_keys")?.FindChild(kingdom.nobility_level);
				if (field != null)
				{
					text = field.GetString(text, null, text);
				}
			}
		}
		return kingdom.nobility_key + "." + text;
	}

	public string GetTitleKeyword()
	{
		string text = GetTitle();
		if (text == null)
		{
			return text;
		}
		int num = text.LastIndexOf('.');
		if (num < 0)
		{
			return text;
		}
		return text.Substring(num + 1);
	}

	public string GetName(bool ignore_custom_name = false)
	{
		if (ignore_custom_name || string.IsNullOrEmpty(CustomName))
		{
			return "cn_" + Name + ((sex == Sex.Male) ? ".m" : ".f");
		}
		return "#" + CustomName;
	}

	public void ChangeName(string newName, bool send_state = true)
	{
		if (!IsAuthority())
		{
			SendEvent(new ChangeNameEvent(newName));
			return;
		}
		CustomName = newName;
		NotifyListeners("name_changed");
		if (send_state)
		{
			SendState<NameState>();
		}
	}

	public CharacterClass.Def GetClass()
	{
		return class_def;
	}

	public override string GetNameKey(IVars vars, string form = "")
	{
		if (!string.IsNullOrEmpty(title) && title != "Knight")
		{
			return "Character.title_name";
		}
		if (class_def != null && class_def.base_def != null)
		{
			return "Character.class_name";
		}
		return "Character.title_name";
	}

	public string GetVoiceLine(string key)
	{
		if (class_def == null)
		{
			return null;
		}
		string value = null;
		class_def.voice_lines.TryGetValue(key, out value);
		return value;
	}

	public static string GetSoundEffect(CharacterClass.Def class_def, string key)
	{
		if (class_def == null)
		{
			return null;
		}
		string value = null;
		class_def.sound_effects.TryGetValue(key, out value);
		return value;
	}

	public string GetSoundEffect(string key)
	{
		return GetSoundEffect(class_def, key);
	}

	public override void OnTimer(Timer timer)
	{
		switch (timer.name)
		{
		case "famous_person_migrate":
		{
			if (!IsAuthority())
			{
				break;
			}
			Village village = FamousPersonSpawner.ChooseSpawnVillage(famous_def, game, GetKingdom());
			if (village != null && village != location)
			{
				if (location is Village village2)
				{
					village2.SetFamous(null);
				}
				village.SetFamous(this);
			}
			else
			{
				Timer.Start(this, "famous_person_migrate", game.Random(famous_def.migrate_time_min, famous_def.migrate_time_max), restart: true);
			}
			break;
		}
		case "ransom_cooldown":
		case "hire_loyalist_cooldown":
			if (prison_kingdom != null)
			{
				timer.Stop();
				FireEvent("force_refresh_actions", null);
			}
			break;
		case "puppet_rebel_distance_check":
		{
			if (!IsAuthority())
			{
				break;
			}
			Action action = actions.Find("InfiltrateKingdomAction");
			if (action == null)
			{
				break;
			}
			if (puppets != null)
			{
				int num3 = action.def.field.GetInt("max_rebel_puppet_distance");
				for (int num4 = puppets.Count - 1; num4 >= 0; num4--)
				{
					Character character = puppets[num4];
					if (character != null && character.IsRebel() && character.GetArmy() != null)
					{
						int num5 = game.KingdomAndRealmDistance(GetKingdom().id, character.GetArmy().realm_in.id);
						if (num5 == 0 || num5 > num3)
						{
							DelPuppet(character);
							Vars vars2 = new Vars();
							vars2.Set("puppet", character);
							vars2.Set("master", this);
							vars2.Set("reason", (num5 == 0) ? "RebelDistanceInOwnKingdom" : "RebelDistanceTooFar");
							FireEvent("lost_puppet", vars2, GetKingdom().id);
						}
					}
				}
			}
			float duration = action.def.field.GetFloat("puppet_rebel_distance_check_time");
			Timer.Start(this, "puppet_rebel_distance_check", duration, restart: true);
			break;
		}
		case "pope_convert_religions":
		{
			if (!IsAuthority())
			{
				break;
			}
			Catholic catholic = game.religions.catholic;
			Kingdom kingdom = catholic?.head_kingdom;
			if (kingdom == null)
			{
				break;
			}
			Realm realm = null;
			int num = game.Random(0, kingdom.realms.Count);
			for (int i = 0; i < kingdom.realms.Count; i++)
			{
				Realm realm2 = kingdom.realms[(i + num) % kingdom.realms.Count];
				if (realm2 != null && !realm2.is_catholic && !realm2.IsDisorder() && realm2.castle?.battle == null && !realm2.IsOccupied())
				{
					realm = realm2;
					break;
				}
			}
			if (realm != null)
			{
				int num2 = 0;
				foreach (Realm item in realm.logicNeighborsRestricted)
				{
					if (item.is_catholic)
					{
						num2++;
					}
				}
				Vars vars = new Vars(realm);
				vars.Set("num_neighboring_catholic_realms", num2);
				Value value = (catholic.def.field.FindChild("pope_convert_realms")?.FindChild("chance")).Value(vars);
				if (game.Random(0f, 100f) < (float)value)
				{
					realm.SetReligion(catholic);
					kingdom.FireEvent("pope_converted_realm", realm, kingdom.id);
				}
			}
			catholic.StartPopeConvertRealmsTimer();
			break;
		}
		default:
			base.OnTimer(timer);
			break;
		}
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (obj != reveal_master)
		{
			return;
		}
		switch (message)
		{
		case "destroying":
		case "finishing":
			SetRevealMaster(null);
			break;
		case "mission_kingdom_changed":
			if (reveal_master.mission_kingdom != GetKingdom())
			{
				SetRevealMaster(null);
			}
			break;
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("age", age.ToString());
		dump.Append("can_die", can_die.ToString());
		dump.Append("sex", sex.ToString());
		dump.Append("spouse", GetSpouse());
		dump.Append("trade_level", trade_level);
		dump.Append("pact", pact);
		dump.Append("paganBelief", paganBelief?.name);
		stats?.DumpInnerState(dump, verbosity);
		if (skills != null && skills.Count > 0)
		{
			dump.OpenSection("skills");
			for (int i = 0; i < skills.Count; i++)
			{
				Skill skill = skills[i];
				if (skill != null)
				{
					dump.Append(skill.def?.field?.key, GetSkillRank(skill));
				}
			}
			dump.CloseSection("skills");
		}
		if (new_skills != null && new_skills.Count > 0)
		{
			dump.OpenSection("new_skills");
			for (int j = 0; j < new_skills.Count; j++)
			{
				Skill.Def def = new_skills[j];
				if (def != null)
				{
					dump.Append(def.field?.key);
				}
			}
			dump.CloseSection("new_skills");
		}
		if (actions != null && (actions.current != null || (actions.opportunities != null && actions.opportunities.Count > 0)))
		{
			dump.OpenSection("actions");
			dump.Append("current", actions.current?.def?.field?.key);
			if (actions?.opportunities != null)
			{
				dump.OpenSection("opportunities");
				for (int k = 0; k < actions.opportunities.Count; k++)
				{
					dump.Append(actions.opportunities[k]?.def?.field?.key);
				}
				dump.CloseSection("opportunities");
			}
			dump.CloseSection("actions");
		}
		if (base.statuses != null && base.statuses.Count > 0)
		{
			dump.OpenSection("statuses");
			for (int l = 0; l < base.statuses.Count; l++)
			{
				dump.Append(base.statuses[l]?.def.field.key);
			}
			dump.CloseSection("statuses");
		}
		if (importing_goods != null && importing_goods.Count > 0)
		{
			dump.OpenSection("importing_goods");
			for (int m = 0; m < importing_goods.Count; m++)
			{
				dump.Append(importing_goods[m].ToString());
			}
			dump.CloseSection("importing_goods");
		}
		if (puppets != null && puppets.Count > 0)
		{
			dump.OpenSection("puppets");
			for (int n = 0; n < puppets.Count; n++)
			{
				dump.Append(puppets[n].ToString());
			}
			dump.CloseSection("puppets");
		}
		if (masters != null && masters.Count > 0)
		{
			dump.OpenSection("masters");
			for (int num = 0; num < masters.Count; num++)
			{
				dump.Append(masters[num].ToString());
			}
			dump.CloseSection("masters");
		}
		if (royal_abilities != null && royal_abilities.Count > 0)
		{
			dump.OpenSection("royal_abilities");
			for (int num2 = 0; num2 < royal_abilities.Count; num2++)
			{
				dump.Append(royal_abilities[num2].ToString());
			}
			dump.CloseSection("royal_abilities");
		}
	}

	public bool IsPrepairngToGovern(out Action a)
	{
		Action action = actions?.Find("GovernCityAction");
		if (action == null || !action.is_active)
		{
			action = actions?.Find("AssignNewGovernCityAction");
		}
		if (action == null || !action.is_active)
		{
			action = actions?.Find("ReassignGovernCityAction");
		}
		a = action;
		if (action != null && action.is_active)
		{
			return true;
		}
		return false;
	}

	public bool IsDeserting(out Action a)
	{
		Action action = (a = actions?.Find("PuppetFleeKingdom"));
		if (action != null && action.is_active)
		{
			return true;
		}
		return false;
	}

	public void PromotePaganBelief(string beliefName, bool apply_penalties = true, bool notify = true, bool send_state = true)
	{
		if (!IsAuthority())
		{
			return;
		}
		Religion.PaganBelief paganBelief = game.religions.pagan.def.FindPaganBelief(beliefName);
		if (paganBelief != null)
		{
			StopPromotingPaganBelief(apply_penalties, notify);
			GetKingdom()?.AddPaganBelief(beliefName);
			this.paganBelief = paganBelief;
			AddStatus(PaganBeliefStatus.Create(paganBelief));
			if (send_state)
			{
				SendState<PaganBeliefState>();
			}
		}
	}

	public void StopPromotingPaganBelief(bool apply_penalties = true, bool notify = true, bool send_state = true)
	{
		if (!IsAuthority() || this.paganBelief == null)
		{
			return;
		}
		Kingdom kingdom = GetKingdom();
		Religion.PaganBelief paganBelief = this.paganBelief;
		kingdom?.DelPaganBelief(paganBelief.name);
		DelStatus<PaganBeliefStatus>();
		this.paganBelief = null;
		if (apply_penalties)
		{
			kingdom?.GetCrownAuthority().AddModifier("stopPromotingPaganBelief");
			if (notify)
			{
				FireEvent("stopped_promoting_pagan_belief_penalty", paganBelief.name);
			}
		}
		else if (notify)
		{
			FireEvent("stopped_promoting_pagan_belief_no_penalty", paganBelief.name);
		}
		if (send_state)
		{
			SendState<PaganBeliefState>();
		}
	}

	public Character(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Character(multiplayer);
	}
}
