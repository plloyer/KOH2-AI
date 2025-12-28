namespace Logic;

[Serialization.Object(Serialization.ObjectType.Village, dynamic = false)]
public class Village : Settlement
{
	[Serialization.State(51)]
	public class UpgradeProgressState : Serialization.ObjectState
	{
		public bool upgrading;

		public float progress;

		public float delta_time;

		public static UpgradeProgressState Create()
		{
			return new UpgradeProgressState();
		}

		public static bool IsNeeded(Object obj)
		{
			Village village = obj as Village;
			if (village.settlementUpgrade == null)
			{
				return false;
			}
			if (!village.settlementUpgrade.upgrading)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Village village = obj as Village;
			if (village.settlementUpgrade == null)
			{
				return false;
			}
			upgrading = village.settlementUpgrade.upgrading;
			progress = village.settlementUpgrade.progress;
			delta_time = obj.game.time - village.settlementUpgrade.last_time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(upgrading, "upgrading");
			if (upgrading)
			{
				ser.WriteFloat(progress, "progress");
				ser.WriteFloat(delta_time, "delta_time");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			upgrading = ser.ReadBool("upgrading");
			if (upgrading)
			{
				progress = ser.ReadFloat("progress");
				delta_time = ser.ReadFloat("delta_time");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Village village = obj as Village;
			if (village.settlementUpgrade == null)
			{
				village.settlementUpgrade = new SettlementUpgrade(village);
			}
			village.settlementUpgrade.Set(upgrading, progress, delta_time, send_state: false);
		}
	}

	[Serialization.State(52)]
	public class FamousPersonState : Serialization.ObjectState
	{
		public NID famous_person_nid = NID.Null;

		public string famous_def;

		public static FamousPersonState Create()
		{
			return new FamousPersonState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Village village))
			{
				return false;
			}
			if (village.famous_person != null)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Village village))
			{
				return false;
			}
			famous_person_nid = village.famous_person;
			if (village.famous_person != null)
			{
				famous_def = village.famous_person.famous_def.field.key;
			}
			else
			{
				famous_def = "";
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(famous_person_nid, "famous_person");
			ser.WriteStr(famous_def, "famous_def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			famous_person_nid = ser.ReadNID("famous_person");
			famous_def = ser.ReadStr("famous_def");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Village village)
			{
				Character character = famous_person_nid.Get<Character>(village.game);
				if (character != null)
				{
					character.famous_def = village.game.defs.Get<FamousPerson.Def>(famous_def);
				}
				village.SetFamous(character, send_state: false);
			}
		}
	}

	private const int STATES_IDX = 50;

	private const int EVENTS_IDX = 56;

	public Character famous_person;

	public SettlementUpgrade settlementUpgrade;

	public Village(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public new static Object Create(Multiplayer multiplayer)
	{
		return new Village(multiplayer);
	}

	public Village(Game game, Point position, string setType)
		: base(game, position, setType)
	{
	}

	public override void Load(DT.Field field)
	{
		base.Load(field);
	}

	public void SetFamous(Character character, bool send_state = true)
	{
		if (game.GetComponent<FamousPersonSpawner>() == null)
		{
			return;
		}
		Character character2 = famous_person;
		famous_person = character;
		if (IsAuthority())
		{
			Character character3 = null;
			if (character != null)
			{
				character3 = famous_person;
				character.SetKingdom(kingdom_id, send_state);
				character.SetLocation(this, send_state);
				Timer.Start(character, "famous_person_migrate", game.Random(character.famous_def.migrate_time_min, character.famous_def.migrate_time_max), restart: true);
			}
			else
			{
				character3 = character2;
			}
			if (send_state)
			{
				SendState<FamousPersonState>();
				FireEvent("famous_person_changed", character3);
			}
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		InitSettlementUpgrade();
	}

	protected void InitSettlementUpgrade()
	{
		if (settlementUpgrade == null)
		{
			settlementUpgrade = new SettlementUpgrade(this);
		}
	}

	public override string ToString()
	{
		string text = base.ToString();
		Realm realm = GetRealm();
		string text2 = ((realm == null) ? "<nowhere>" : (NID.ToString(realm.GetNid(generateNid: false)) + ":" + realm.name));
		return text.Replace(" Village (", " " + type + " in " + text2 + " (");
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return type + ".name";
	}
}
