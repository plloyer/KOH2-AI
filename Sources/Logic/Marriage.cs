namespace Logic;

[Serialization.Object(Serialization.ObjectType.Marriage)]
public class Marriage : Object
{
	[Serialization.State(11)]
	public class CharactersState : Serialization.ObjectState
	{
		public NID husband;

		public NID wife;

		public NID husband_kingdom;

		public NID wife_kingdom;

		public static CharactersState Create()
		{
			return new CharactersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Marriage marriage = obj as Marriage;
			husband = marriage.husband;
			wife = marriage.wife;
			husband_kingdom = marriage.kingdom_husband;
			wife_kingdom = marriage.kingdom_wife;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(husband, "husband");
			ser.WriteNID<Character>(wife, "wife");
			ser.WriteNID<Kingdom>(husband_kingdom, "husband_kingdom");
			ser.WriteNID<Kingdom>(wife_kingdom, "wife_kingdom");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			husband = ser.ReadNID<Character>("husband");
			wife = ser.ReadNID<Character>("wife");
			husband_kingdom = ser.ReadNID<Kingdom>("husband_kingdom");
			wife_kingdom = ser.ReadNID<Kingdom>("wife_kingdom");
		}

		public override void ApplyTo(Object obj)
		{
			Marriage marriage = obj as Marriage;
			marriage.husband = husband.Get<Character>(marriage.game);
			marriage.wife = wife.Get<Character>(marriage.game);
			marriage.kingdom_husband = husband_kingdom.Get<Kingdom>(marriage.game);
			marriage.kingdom_wife = wife_kingdom.Get<Kingdom>(marriage.game);
		}
	}

	public Character husband;

	public Character wife;

	public Kingdom kingdom_husband;

	public Kingdom kingdom_wife;

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public Marriage(Character character1, Character character2)
		: base(character1.game)
	{
		AssertAuthority();
		if (character1.sex == Character.Sex.Male)
		{
			husband = character1;
			wife = character2;
		}
		else
		{
			husband = character2;
			wife = character1;
		}
		if (husband.IsMarried())
		{
			Game.Log("Husband already married: " + husband.ToString(), Game.LogType.Error);
		}
		if (wife.IsMarried())
		{
			Game.Log("Wife already married: " + wife.ToString(), Game.LogType.Error);
		}
		Kingdom originalKingdom = wife.GetOriginalKingdom();
		Kingdom kingdom = (kingdom_husband = husband.GetKingdom());
		kingdom_wife = originalKingdom;
		wife.marriage = this;
		wife.SendState<Character.MarriageState>();
		wife.DelStatus<WidowedStatus>();
		wife.AddStatus(MarriedStatus.Create(this));
		husband.marriage = this;
		husband.SendState<Character.MarriageState>();
		husband.DelStatus<WidowedStatus>();
		husband.AddStatus(MarriedStatus.Create(this));
		if (!kingdom.marriages.Contains(this))
		{
			kingdom.marriages.Add(this);
		}
		if (!originalKingdom.marriages.Contains(this))
		{
			originalKingdom.marriages.Add(this);
		}
		kingdom.SendState<Kingdom.MarriageStates>();
		originalKingdom.SendState<Kingdom.MarriageStates>();
		wife.SetKingdom(husband.kingdom_id);
		if (husband.IsKing())
		{
			RoyalFamily royalFamily = kingdom.royalFamily;
			royalFamily.SetSpouse(wife, setTitle: true);
			royalFamily.SetNextNewbornCheck(royalFamily.GetChildBirthMinTimeAfterMarriageOrBirth());
			kingdom.FireEvent("royal_new_spouse", null);
		}
		kingdom.FireEvent("new_marriage", this, kingdom.id);
		originalKingdom.FireEvent("new_marriage", this, kingdom.id);
		kingdom.NotifyListeners("character_married", husband);
		originalKingdom.NotifyListeners("character_married", wife);
		husband.NotifyListeners("got_married", wife);
		wife.NotifyListeners("got_married", husband);
		OnMarriageAnalytics(kingdom, originalKingdom, husband, wife);
		OnMarriageAnalytics(originalKingdom, kingdom, wife, husband);
	}

	private void OnMarriageAnalytics(Kingdom our_kingdom, Kingdom other_kingdom, Character our_character, Character other_character)
	{
		if (our_kingdom.is_player && our_character != null)
		{
			Vars vars = new Vars();
			vars.Set("characterName", our_character.Name);
			vars.Set("relationGender", our_character.sex.ToString());
			vars.Set("relationPosition", our_kingdom.royalFamily?.GetRole(our_character).ToString() ?? RoyalFamily.FamilyRole.Unrelated.ToString());
			vars.Set("relationAge", (int)our_character.age);
			vars.Set("targetKingdom", other_kingdom.Name);
			KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(our_kingdom, other_kingdom, calc_fade: true, create_if_not_found: true);
			vars.Set("kingdomRelation", (int)kingdomAndKingdomRelation.GetRelationship());
			vars.Set("spousePosition", other_kingdom?.royalFamily?.GetRole(other_character).ToString() ?? RoyalFamily.FamilyRole.Unrelated.ToString());
			if (other_kingdom?.royalFamily?.Children.Contains(other_character) ?? false)
			{
				vars.Set("spouseSeniorSiblings", other_kingdom.royalFamily.Children.IndexOf(other_character));
			}
			else
			{
				vars.Set("spouseSeniorSiblings", 0);
			}
			our_kingdom.FireEvent("analytics_marriage", vars, our_kingdom.id);
		}
	}

	public bool Has(Character c)
	{
		if (husband != c)
		{
			return wife == c;
		}
		return true;
	}

	public Character GetSpouse(Character c)
	{
		if (husband == c)
		{
			return wife;
		}
		if (wife == c)
		{
			return husband;
		}
		return null;
	}

	public Kingdom GetOtherKingdom(Kingdom k)
	{
		Kingdom kingdom = kingdom_husband;
		Kingdom kingdom2 = kingdom_wife;
		if (k == kingdom)
		{
			return kingdom2;
		}
		if (k == kingdom2)
		{
			return kingdom;
		}
		return null;
	}

	public Kingdom GetOtherKingdom(Character c)
	{
		if (c == husband)
		{
			return kingdom_wife;
		}
		if (c == wife)
		{
			return kingdom_husband;
		}
		return null;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (IsAuthority())
		{
			kingdom_wife?.marriages?.Remove(this);
			kingdom_husband?.marriages?.Remove(this);
			kingdom_wife?.SendState<Kingdom.MarriageStates>();
			kingdom_husband?.SendState<Kingdom.MarriageStates>();
		}
	}

	public override string ToString()
	{
		return "Marriage(" + husband?.title + " " + husband?.Name + " from " + kingdom_husband?.Name + " and " + wife?.title + " " + wife?.Name + " from " + kingdom_wife?.Name + ")";
	}

	public Marriage(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Marriage(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}
}
