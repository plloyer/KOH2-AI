using System;
using System.Collections.Generic;

namespace Logic;

public class CharacterFactory
{
	private static Random random = new Random();

	private static List<string> tmp_str_list = new List<string>();

	private const int minNormalClassChars = 2;

	private const int maxNormalClassChars = 5;

	private static List<string> sm_CharacterClassNames = new List<string>();

	private static List<CharacterClass.Def> sm_CharacterClassDefs = new List<CharacterClass.Def>();

	private static List<string> sm_TempNameList = new List<string>(200);

	public static void PopulateRoyalFamily(Character Sovereign)
	{
		if (Sovereign == null || !Sovereign.IsKing() || Sovereign.GetKingdom() == Sovereign.game.religions.catholic.hq_kingdom)
		{
			return;
		}
		Game game = Sovereign.game;
		RoyalFamily royalFamily = Sovereign.GetKingdom().royalFamily;
		RoyalFamily.Def def = royalFamily.def;
		Character spouse = royalFamily.Spouse;
		if (spouse == null)
		{
			return;
		}
		if (Sovereign.age >= Character.Age.Juvenile)
		{
			int age = (int)Sovereign.age;
			int min = royalFamily.def.min_children_per_age[age];
			int num = royalFamily.def.max_children_per_age[age];
			int num2 = game.Random(min, num + 1);
			Character.Age age2 = (Character.Age)Math.Min(age, (int)spouse.age);
			Character character = null;
			for (int i = 0; i < num2; i++)
			{
				Character character2 = null;
				character2 = ((game.Random(0, 2) != 0) ? CreatePrincess(Sovereign.GetKingdom(), Sovereign, spouse) : CreatePrince(Sovereign.GetKingdom(), Sovereign, spouse));
				if (!royalFamily.AddChild(character2, send_state: true, fire_event: false))
				{
					character2.Destroy();
					continue;
				}
				character2.age = (Character.Age)game.Random(0, (int)(age2 + def.child_max_age_mod + 1));
				character2.next_age_check = character2.game.time + game.Random(0f, character2.GetAgeupDuration(character2.age) * 0.15f);
				if (character2.sex == Character.Sex.Male && (royalFamily.Heir == null || character2.age > royalFamily.Heir.age || (character2.age == royalFamily.Heir.age && character2.next_age_check < royalFamily.Heir.next_age_check)))
				{
					royalFamily.Heir = character2;
				}
				if (character2.sex == Character.Sex.Male && (character == null || character2.age > character.age || (character2.age == character.age && character2.next_age_check < character.next_age_check)))
				{
					character = character2;
				}
			}
			for (int j = 0; j < royalFamily.Children.Count; j++)
			{
				Character character3 = royalFamily.Children[j];
				if (character3.sex != Character.Sex.Female)
				{
					if (character3 == character)
					{
						character3.SetClass(Sovereign.class_def);
					}
					else
					{
						character3.SetClass(GetRandomClass(game));
					}
					if (character3.age >= Character.Age.Young)
					{
						character3.SetStatus<AvailableForAssignmentStatus>();
					}
				}
			}
			royalFamily.Children.Sort(delegate(Character c1, Character c2)
			{
				int num3 = c2.age.CompareTo(c1.age);
				return (num3 == 0) ? ((c2.next_age_check < c1.next_age_check) ? 1 : (-1)) : num3;
			});
		}
		royalFamily.SetNextNewbornCheck(royalFamily.GetChildBirthMinTimeAfterMarriageOrBirth());
	}

	public static bool LoadCharacterCSV(Kingdom k, DT.Field csv_field, string column_name, BuildCharacter c)
	{
		if (csv_field == null)
		{
			return false;
		}
		string text = csv_field.GetString(column_name);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num = 0;
		while (num >= 0)
		{
			int num2 = text.IndexOf('.', num);
			string text2;
			if (num2 < 0)
			{
				text2 = text.Substring(num);
				num = -1;
			}
			else
			{
				text2 = text.Substring(num, num2 - num);
				num = num2 + 1;
			}
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			if (c.GetName() == null)
			{
				c.SetName(text2);
				continue;
			}
			switch (text2)
			{
			case "m":
				c.SetSex(Character.Sex.Male);
				continue;
			case "f":
				c.SetSex(Character.Sex.Female);
				continue;
			case "I":
				c.SetAge(Character.Age.Infant);
				continue;
			case "C":
				c.SetAge(Character.Age.Child);
				continue;
			case "J":
				c.SetAge(Character.Age.Juvenile);
				continue;
			case "Y":
				c.SetAge(Character.Age.Young);
				continue;
			case "A":
				c.SetAge(Character.Age.Adult);
				continue;
			case "O":
				c.SetAge(Character.Age.Old);
				continue;
			case "V":
				c.SetAge(Character.Age.Venerable);
				continue;
			}
			if (int.TryParse(text2, out var result))
			{
				c.SetNameIdx(result);
				continue;
			}
			Kingdom kingdom = k.game.GetKingdom(text2);
			if (kingdom != null)
			{
				c.SetOriginalKingdomId(kingdom.id);
				continue;
			}
			Game.Log(csv_field.Path(include_file: true) + "." + column_name + ": Invalid token '" + text2 + "' in '" + text + "'", Game.LogType.Error);
		}
		return true;
	}

	public static void LoadChildFromCSV(Kingdom kingdom, string column_name)
	{
		if (string.IsNullOrEmpty(kingdom.csv_field?.GetString(column_name)))
		{
			return;
		}
		Game game = kingdom.game;
		RoyalFamily royalFamily = kingdom.royalFamily;
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, null);
		LoadCharacterCSV(kingdom, kingdom.csv_field, column_name, buildCharacter);
		Character character = null;
		if (buildCharacter.GetSex() == Character.Sex.Female)
		{
			for (int i = 0; i < kingdom.marriages.Count; i++)
			{
				Marriage marriage = kingdom.marriages[i];
				Character wife = marriage.wife;
				if (wife.Name == buildCharacter.GetName() && marriage.kingdom_wife == kingdom && wife.GetKingdom() != kingdom)
				{
					character = marriage.wife;
					break;
				}
			}
		}
		if (character == null)
		{
			buildCharacter.SetTitle((buildCharacter.GetSex() == Character.Sex.Male) ? "Prince" : "Princess");
			buildCharacter.SetParents(royalFamily.Sovereign, royalFamily.Spouse);
			buildCharacter.EnableAging(can_age: true);
			character = buildCharacter.Build();
		}
		if (!royalFamily.AddChild(character, send_state: true, fire_event: false))
		{
			character.Destroy();
		}
		else if (character.age >= Character.Age.Child && character.sex == Character.Sex.Male)
		{
			character.SetClass(GetRandomClass(game));
			if (character.age >= Character.Age.Young)
			{
				character.SetStatus<AvailableForAssignmentStatus>();
			}
		}
	}

	public static void LoadChildrenFromCSV(Kingdom kingdom)
	{
		for (int i = 1; i <= 4; i++)
		{
			string column_name = $"Child_{i}_NameSexAge";
			LoadChildFromCSV(kingdom, column_name);
		}
		RoyalFamily royalFamily = kingdom.royalFamily;
		if (royalFamily.Children == null || royalFamily.Children.Count == 0)
		{
			return;
		}
		royalFamily.Children.Sort((Character x, Character y) => y.age.CompareTo(x.age));
		float[,] array = new float[7, 2];
		for (int num = 0; num < kingdom.royalFamily.Children.Count; num++)
		{
			Character character = kingdom.royalFamily.Children[num];
			array[(int)character.age, 0] += 1f;
			array[(int)character.age, 1] = 0f;
		}
		Character character2 = null;
		for (int num2 = 0; num2 < kingdom.royalFamily.Children.Count; num2++)
		{
			Character character3 = kingdom.royalFamily.Children[num2];
			int num3 = (int)array[(int)character3.age, 0];
			float num4 = (int)array[(int)character3.age, 1];
			character3.next_age_check = character3.game.time + num4 + character3.game.Random(0f, character3.GetAgeupDuration(character3.age) / (float)num3);
			array[(int)character3.age, 1] += character3.GetAgeupDuration(character3.age) / (float)num3;
			if (character3.sex == Character.Sex.Male)
			{
				if (character2 == null)
				{
					character2 = character3;
				}
				else if (character2.age < character3.age)
				{
					character2 = character3;
				}
				else if (character2.age == character3.age)
				{
					character2 = ((character2.next_age_check < character3.next_age_check) ? character2 : character3);
				}
			}
		}
		if (royalFamily.Heir == null)
		{
			royalFamily.Heir = character2;
		}
	}

	public static void PopulateInitialRoyalFamily(Kingdom kingdom)
	{
		if (kingdom.IsDefeated())
		{
			return;
		}
		new Random(GetKingdomSeed(kingdom));
		Character character = CreateKing(kingdom, initial: true);
		character.next_age_check = character.game.time + kingdom.game.Random(0f, character.GetAgeupDuration(character.age) / 2f);
		character.EnableAging(can_die: true);
		character.SetStatus<AvailableForAssignmentStatus>();
		kingdom.royalFamily.SetSovereign(character);
		if (kingdom != kingdom.game.religions.catholic.hq_kingdom)
		{
			string value = kingdom.csv_field?.GetString("QueenNameAge");
			string value2 = kingdom.csv_field?.GetString("Child_1_NameSexAge");
			if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(value2))
			{
				Character spouse = CreateQueen(kingdom, character, initial: true);
				kingdom.royalFamily.Spouse = spouse;
			}
			LoadChildrenFromCSV(kingdom);
		}
	}

	public static List<Character> GenerateRelatives(Kingdom kingdom, int min, int max)
	{
		if (min < 0 || max == 0)
		{
			return null;
		}
		int num = random.Next(min, max);
		List<Character> list = new List<Character>(num);
		if (kingdom != null)
		{
			int i = 0;
			for (int num2 = num; i < num2; i++)
			{
				Character character = CreateCharacter(kingdom.game, kingdom.id, null);
				if (kingdom.game.Random() < 0.3)
				{
					character.title = ((character.sex == Character.Sex.Male) ? "Duke" : "Duchess");
				}
				list.Add(character);
			}
		}
		return list;
	}

	public static Character CreatePrince(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return null;
		}
		Character father = kingdom?.royalFamily?.Sovereign;
		Character mother = kingdom?.royalFamily?.Spouse;
		return CreatePrince(kingdom, father, mother);
	}

	public static Character CreatePrince(Kingdom kingdom, Character father, Character mother)
	{
		if (kingdom == null)
		{
			return null;
		}
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, null, "Prince");
		buildCharacter.SetSex(Character.Sex.Male);
		buildCharacter.SetAge(Character.Age.Infant);
		buildCharacter.SetParents(father, mother);
		buildCharacter.EnableAging(can_age: true);
		buildCharacter.SetName(GetUniqueRoyalChildName(kingdom, Character.Sex.Male));
		buildCharacter.SetClass("ClasslessPrince");
		Character character = buildCharacter.Build();
		character.AddStatus<HasRoyalBloodStatus>();
		return character;
	}

	public static Character CreatePrincess(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return null;
		}
		Character sovereign = kingdom.royalFamily.Sovereign;
		Character spouse = kingdom.royalFamily.Spouse;
		return CreatePrincess(kingdom, sovereign, spouse);
	}

	public static Character CreatePrincess(Kingdom kingdom, Character father, Character mother)
	{
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, null, "Princess");
		buildCharacter.SetSex(Character.Sex.Female);
		buildCharacter.SetAge(Character.Age.Infant);
		buildCharacter.SetParents(father, mother);
		buildCharacter.EnableAging(can_age: true);
		buildCharacter.SetName(GetUniqueRoyalChildName(kingdom, Character.Sex.Female));
		return buildCharacter.Build();
	}

	public static Character CreateQueen(Kingdom kingdom, Character king = null, bool initial = false)
	{
		if (kingdom == null)
		{
			return null;
		}
		Character character = null;
		new Random(GetKingdomSeed(kingdom));
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, null, "Queen");
		buildCharacter.SetHistoricalFigure(initial);
		if (initial && LoadCharacterCSV(kingdom, kingdom.csv_field, "QueenNameAge", buildCharacter))
		{
			int originalKingdomId = buildCharacter.GetOriginalKingdomId();
			if (originalKingdomId != kingdom.id)
			{
				RoyalFamily royalFamily = kingdom.game.GetKingdom(originalKingdomId)?.royalFamily;
				if (royalFamily != null)
				{
					for (int i = 0; i < royalFamily.Children.Count; i++)
					{
						Character character2 = royalFamily.Children[i];
						if (character2.Name == buildCharacter.GetName())
						{
							character = character2;
							break;
						}
					}
				}
			}
		}
		else
		{
			buildCharacter.SetName(GetUniqueQueenName(kingdom));
			if (king != null)
			{
				int num = kingdom.game.Random(3, (int)(king.age + 1));
				if (num >= 6)
				{
					num--;
				}
				buildCharacter.SetAge((Character.Age)num);
			}
			else
			{
				buildCharacter.SetAge(Character.Age.Young);
			}
		}
		if (character == null)
		{
			buildCharacter.SetSex(Character.Sex.Female);
			buildCharacter.EnableAging(can_age: true);
			character = buildCharacter.Build();
		}
		if (king != null)
		{
			new Marriage(character, king);
		}
		return character;
	}

	public static Character CreatePope(Game game, Kingdom kingdom = null, bool initial = false)
	{
		Kingdom kingdom2 = game?.religions?.catholic?.hq_kingdom;
		if (kingdom2 == null)
		{
			return null;
		}
		if (kingdom == null)
		{
			kingdom = kingdom2;
		}
		BuildCharacter buildCharacter = new BuildCharacter(game, kingdom2.id, kingdom.id, "Cleric", "Pope");
		buildCharacter.SetHistoricalFigure(initial);
		if (!initial || !LoadCharacterCSV(kingdom, game.religions.catholic.papacy_csv, "Pope", buildCharacter))
		{
			buildCharacter.SetName(kingdom2.religion.NewName());
		}
		buildCharacter.SetAge(Character.Age.Old);
		buildCharacter.SetSex(Character.Sex.Male);
		buildCharacter.EnableAging(can_age: true);
		return buildCharacter.Build();
	}

	public static void GetStringList(string slst, Game game, List<string> result)
	{
		int num = slst.IndexOf('/');
		if (num < 0)
		{
			return;
		}
		int num2 = 0;
		while (true)
		{
			string text = slst.Substring(num2, num - num2).Trim();
			if (!string.IsNullOrEmpty(text))
			{
				result.Add(text);
			}
			num2 = num + 1;
			if (num2 < slst.Length)
			{
				num = slst.IndexOf('/', num2);
				if (num < 0)
				{
					num = slst.Length;
				}
				continue;
			}
			break;
		}
	}

	public static string RandomStringFromList(string slst, Game game)
	{
		int num = slst.IndexOf('/');
		if (num < 0)
		{
			return slst;
		}
		int num2 = 0;
		while (true)
		{
			string text = slst.Substring(num2, num - num2).Trim();
			if (!string.IsNullOrEmpty(text))
			{
				tmp_str_list.Add(text);
			}
			num2 = num + 1;
			if (num2 >= slst.Length)
			{
				break;
			}
			num = slst.IndexOf('/', num2);
			if (num < 0)
			{
				num = slst.Length;
			}
		}
		if (tmp_str_list.Count == 1)
		{
			return tmp_str_list[0];
		}
		int index = game.Random(0, tmp_str_list.Count);
		string result = tmp_str_list[index];
		tmp_str_list.Clear();
		return result;
	}

	public static int GetKingdomSeed(Kingdom k)
	{
		if (k == null)
		{
			return 0;
		}
		return k.Name.GetHashCode() + (k?.game?.map_period?.GetHashCode() ?? 0);
	}

	public static float Random(Random r, float min, float max)
	{
		return (float)((double)min + r.NextDouble() * (double)(max - min));
	}

	public static Character CreateKing(Kingdom kingdom, bool initial = false)
	{
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, null, "King");
		buildCharacter.SetHistoricalFigure(initial);
		int kingdomSeed = GetKingdomSeed(kingdom);
		Random r = new Random(kingdomSeed);
		if (initial)
		{
			initial = LoadCharacterCSV(kingdom, kingdom.csv_field, "KingNameAge", buildCharacter);
		}
		string text = null;
		if (initial)
		{
			text = kingdom.csv_field?.GetString("KingClass");
			if (!string.IsNullOrEmpty(text))
			{
				text = RandomStringFromList(text, kingdom.game);
				if (kingdom.game.defs.Find<CharacterClass.Def>(text) == null)
				{
					Game.Log(kingdom.csv_field.Path(include_file: true) + ".KingdomClass: Invalid class '" + text + "'", Game.LogType.Error);
					text = null;
				}
			}
			else
			{
				text = null;
			}
		}
		else
		{
			buildCharacter.SetName(GetUniqueKingName(kingdom, kingdomSeed));
		}
		if (text == null)
		{
			List<string> characterClassList = GetCharacterClassList(kingdom.game, includeBaseClasses: true, includeSubclasses: false);
			text = characterClassList[kingdom.game.Random(0, characterClassList.Count)];
		}
		buildCharacter.SetClass(text);
		buildCharacter.SetSex(Character.Sex.Male);
		if (buildCharacter.GetAge() == Character.Age.COUNT)
		{
			float ageupDuration = Character.GetAgeupDuration(Character.Age.Young, kingdom.game);
			float ageupDuration2 = Character.GetAgeupDuration(Character.Age.Adult, kingdom.game);
			float num = Random(r, 0f, ageupDuration + ageupDuration2 / 2f - 1f);
			float num2 = 0f;
			Character.Age age;
			if (num < ageupDuration)
			{
				age = Character.Age.Young;
				num2 = ageupDuration - num;
			}
			else
			{
				age = Character.Age.Adult;
				num2 = ageupDuration2 + ageupDuration - num;
			}
			buildCharacter.SetAge(age, num2);
		}
		buildCharacter.SetHasDefaultSkill(has: false);
		Character character = buildCharacter.Build();
		character.AddStatus<HasRoyalBloodStatus>();
		return character;
	}

	public static Character CreateFamousPerson(Kingdom kingdom, FamousPerson.Def def)
	{
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, null, "None");
		buildCharacter.SetSex(def.sex);
		buildCharacter.SetAge((Character.Age)random.Next(3, 5));
		buildCharacter.SetFamousPerson(def);
		List<Skill.Def> list = new List<Skill.Def>();
		int num = random.Next(def.gp_min_skills, def.gp_max_skills);
		for (int i = 0; i < def.skills.Count; i++)
		{
			Skill.Def def2 = kingdom.game.defs.Get<Skill.Def>(def.skills[i].key);
			if (def2 != null)
			{
				list.Add(def2);
			}
		}
		while (list.Count > num && list.Count > 0)
		{
			list.RemoveAt(random.Next(0, list.Count - 1));
		}
		buildCharacter.SetBaseSkills(list);
		return buildCharacter.Build();
	}

	public static Character CreateCourtCandidate(Game game, int kingdom_id, string class_id = null)
	{
		return CreateCourtCandidate(game.GetKingdom(kingdom_id), class_id);
	}

	public static Character CreateRebel(Game game, int kingdom_id, int origin_kingdom_id, string class_id = null)
	{
		BuildCharacter buildCharacter = new BuildCharacter(game, kingdom_id, origin_kingdom_id, class_id);
		buildCharacter.SetSex(Character.Sex.Male);
		buildCharacter.SetAge((Character.Age)random.Next(3, 5));
		if (class_id == null)
		{
			buildCharacter.SetClass(GetRandomClass(game));
		}
		buildCharacter.EnableAging(game.rules.KnightAging());
		Character character = buildCharacter.Build();
		List<Skill.Def> applicableSkills = character.GetApplicableSkills();
		if (applicableSkills == null || applicableSkills.Count == 0)
		{
			return character;
		}
		applicableSkills.RemoveAll(delegate(Skill.Def sd)
		{
			if (sd.row_def == null)
			{
				return true;
			}
			SkillsTable.CellDef cellDef = sd.row_def.FindCell("Marshal");
			if (cellDef == null)
			{
				return true;
			}
			return (cellDef.slot_type != "primary") ? true : false;
		});
		character.AddSkill(applicableSkills[game.Random(0, applicableSkills.Count)]);
		return character;
	}

	public static Character CreateCourtCandidate(Kingdom kingdom, string class_id = null)
	{
		if (kingdom == null)
		{
			return null;
		}
		BuildCharacter buildCharacter = new BuildCharacter(kingdom.game, kingdom.id, kingdom.id, class_id);
		buildCharacter.SetSex(Character.Sex.Male);
		buildCharacter.SetAge((Character.Age)random.Next(3, 5));
		if (class_id == null)
		{
			buildCharacter.SetClass(GetRandomClass(kingdom.game));
		}
		buildCharacter.SetHasDefaultSkill(has: false);
		buildCharacter.EnableAging(kingdom.game.rules.KnightAging());
		buildCharacter.SetName(GetUniqueCourtMemberName(kingdom, Character.Sex.Male));
		return buildCharacter.Build();
	}

	public static Character CreateCharacter(Game game, int kingdom_id, string class_id, string title = null, Character.Age min_age = Character.Age.Young, Character.Age max_age = Character.Age.Adult)
	{
		CharacterClass.Def class_def = (string.IsNullOrEmpty(class_id) ? null : game.defs.Get<CharacterClass.Def>(class_id));
		Character character = new Character(game, kingdom_id, kingdom_id, class_def, historical_figure: false);
		character.title = title;
		if (title == "Princess" || title == "Queen")
		{
			character.sex = Character.Sex.Female;
		}
		else if (string.IsNullOrEmpty(class_id))
		{
			character.sex = ((!(random.NextDouble() < 0.5)) ? Character.Sex.Female : Character.Sex.Male);
		}
		else
		{
			character.sex = Character.Sex.Male;
		}
		if (character.sex == Character.Sex.Female && title == null)
		{
			character.title = "Lady";
		}
		character.age = (Character.Age)random.Next((int)min_age, (int)(max_age + 1));
		if (character.age < Character.Age.Young)
		{
			character.next_age_check = game.time + character.GetAgeupDuration(character.age);
		}
		character.Name = GetCharacterName(character.GetOriginalKingdom(), character.sex);
		return character;
	}

	public static string GetRandomClass(Game game, bool includeSubclasses = true, bool includeBase = false)
	{
		Defs.Registry registry = game.defs.Get(typeof(CharacterClass.Def));
		sm_CharacterClassNames.Clear();
		foreach (string key in registry.defs.Keys)
		{
			if (!(key == "ClasslessPrince"))
			{
				sm_CharacterClassNames.Add(key);
			}
		}
		if (includeBase && registry.base_def != null)
		{
			sm_CharacterClassNames.Insert(0, registry.base_def.id);
		}
		return sm_CharacterClassNames[game.Random(0, sm_CharacterClassNames.Count)];
	}

	public static CharacterClass.Def GetRandomClassDef(Game game)
	{
		Defs.Registry registry = game.defs.Get(typeof(CharacterClass.Def));
		sm_CharacterClassDefs.Clear();
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			if (!(def.Key == "ClasslessPrince"))
			{
				sm_CharacterClassDefs.Add(def.Value as CharacterClass.Def);
			}
		}
		return sm_CharacterClassDefs[game.Random(0, sm_CharacterClassDefs.Count)];
	}

	public static List<CharacterClass.Def> GetCharacterClassDefList(Game game)
	{
		Defs.Registry registry = game.defs.Get(typeof(CharacterClass.Def));
		List<CharacterClass.Def> list = new List<CharacterClass.Def>();
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			if (!(def.Key == "ClasslessPrince"))
			{
				list.Add(def.Value as CharacterClass.Def);
			}
		}
		return list;
	}

	public static List<string> GetCharacterClassList(Game game, bool includeBaseClasses = true, bool includeSubclasses = true, bool includeBase = false, bool filterRandomlySelectableAtStart = false)
	{
		Defs.Registry registry = game.defs.Get(typeof(CharacterClass.Def));
		List<string> list = new List<string>(registry.defs.Count);
		if (includeBase && registry.base_def != null)
		{
			list.Add(registry.base_def.id);
		}
		foreach (KeyValuePair<string, Def> def2 in registry.defs)
		{
			string key = def2.Key;
			CharacterClass.Def def = def2.Value as CharacterClass.Def;
			if (key == "ClasslessPrince" || (filterRandomlySelectableAtStart && !def.can_be_picked_randomly_at_start))
			{
				continue;
			}
			if (def.base_def != registry.base_def)
			{
				if (includeSubclasses)
				{
					list.Add(key);
				}
			}
			else if (includeBaseClasses)
			{
				list.Add(key);
			}
		}
		return list;
	}

	public static string GetCharacterName(Kingdom kingdom, Character.Sex sex, List<string> blacklisted = null, int seed = 0)
	{
		if (kingdom == null)
		{
			return "#UnknownKingdom";
		}
		if (kingdom.game == null)
		{
			return "#NoGame";
		}
		if (kingdom.game.dt == null)
		{
			return "#NoDT";
		}
		DT.Field field = kingdom.game.dt.Find(kingdom.names_key + ((sex == Character.Sex.Male) ? ".male" : ".female"));
		if (field == null)
		{
			return "#Missing def: " + kingdom.names_key;
		}
		if (field.children == null || field.children.Count <= 0)
		{
			return "#Empty def: " + kingdom.names_key;
		}
		if (blacklisted != null)
		{
			List<DT.Field> list = new List<DT.Field>(field.children);
			for (int i = 0; i < blacklisted.Count; i++)
			{
				for (int num = list.Count - 1; num >= 0; num--)
				{
					if (list[num].key == blacklisted[i])
					{
						list.RemoveAt(num);
					}
				}
			}
			if (list.Count == 0)
			{
				return field.children[random.Next(0, list.Count)].key;
			}
			int index = random.Next(0, list.Count);
			return list[index].key;
		}
		int index2 = random.Next(0, field.children.Count);
		return field.children[index2].key;
	}

	public static string GetUniqueRoyalChildName(Kingdom kingdom, Character.Sex sex)
	{
		if (kingdom == null)
		{
			return GetCharacterName(kingdom, sex);
		}
		sm_TempNameList.Clear();
		sm_TempNameList.Add(kingdom.GetKing()?.Name);
		sm_TempNameList.Add(kingdom.GetQueen()?.Name);
		if (kingdom.royalFamily != null)
		{
			for (int i = 0; i < kingdom.royalFamily.Children.Count; i++)
			{
				Character character = kingdom.royalFamily.Children[i];
				if (character != null && character.sex == sex)
				{
					sm_TempNameList.Add(character.Name);
				}
			}
		}
		if (sex == Character.Sex.Male)
		{
			for (int j = 0; j < kingdom.court.Count; j++)
			{
				Character character2 = kingdom.court[j];
				if (character2 != null && character2.sex == sex)
				{
					sm_TempNameList.Add(character2.Name);
				}
			}
		}
		return GetCharacterName(kingdom, sex, sm_TempNameList);
	}

	public static string GetUniqueCourtMemberName(Kingdom kingdom, Character.Sex sex)
	{
		if (kingdom == null)
		{
			return GetCharacterName(kingdom, sex);
		}
		sm_TempNameList.Clear();
		sm_TempNameList.Add(kingdom.GetKing()?.Name);
		if (kingdom.royalFamily != null)
		{
			for (int i = 0; i < kingdom.royalFamily.Children.Count; i++)
			{
				Character character = kingdom.royalFamily.Children[i];
				if (character != null && character.sex == sex)
				{
					sm_TempNameList.Add(character.Name);
				}
			}
		}
		for (int j = 0; j < kingdom.court.Count; j++)
		{
			Character character2 = kingdom.court[j];
			if (character2 != null && character2.sex == sex)
			{
				sm_TempNameList.Add(character2.Name);
			}
		}
		return GetCharacterName(kingdom, sex, sm_TempNameList);
	}

	public static string GetUniqueKingName(Kingdom kingdom, int seed)
	{
		return GetCharacterName(kingdom, Character.Sex.Male, null, seed);
	}

	public static string GetUniqueQueenName(Kingdom kingdom)
	{
		return GetCharacterName(kingdom, Character.Sex.Female);
	}

	public static List<KeyValuePair<string, CharacterClass.Def>> GetBaseCharacterClassDefs(Game game)
	{
		Defs.Registry registry = game.defs.Get(typeof(CharacterClass.Def));
		if (registry == null || registry.defs.Count == 0)
		{
			return null;
		}
		List<KeyValuePair<string, CharacterClass.Def>> list = new List<KeyValuePair<string, CharacterClass.Def>>();
		foreach (KeyValuePair<string, Def> def2 in registry.defs)
		{
			CharacterClass.Def def = def2.Value as CharacterClass.Def;
			if (def.base_def == registry.base_def && !(def2.Key == "ClasslessPrince"))
			{
				list.Add(new KeyValuePair<string, CharacterClass.Def>(def2.Key, def));
			}
		}
		return list;
	}
}
