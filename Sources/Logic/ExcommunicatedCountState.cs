using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Religions, dynamic = false)]
public class Religions : Object, IListener
{
	[Serialization.State(11)]
	public class CharactersState : Serialization.ObjectState
	{
		public NID pope;

		public NID ecumenical_patriarch;

		public List<NID> cardinals;

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
			Religions religions = obj.game.religions;
			pope = religions.catholic.head;
			ecumenical_patriarch = religions.orthodox.head;
			Catholic catholic = religions.catholic;
			cardinals = new List<NID>(catholic.cardinals.Count);
			for (int i = 0; i < catholic.cardinals.Count; i++)
			{
				Character character = catholic.cardinals[i];
				cardinals.Add(character);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(pope, "pope");
			ser.WriteNID<Character>(ecumenical_patriarch, "ecumenical_patriarch");
			ser.Write7BitUInt(cardinals.Count, "cardinals");
			for (int i = 0; i < cardinals.Count; i++)
			{
				NID nID = cardinals[i];
				ser.WriteNID<Character>(nID, "cardinal", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			pope = ser.ReadNID<Character>("pope");
			ecumenical_patriarch = ser.ReadNID<Character>("ecumenical_patriarch");
			int num = ser.Read7BitUInt("cardinals");
			if (num > 0)
			{
				cardinals = new List<NID>(num);
				for (int i = 0; i < num; i++)
				{
					NID item = ser.ReadNID<Character>("cardinal", i);
					cardinals.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Religions religions = obj.game.religions;
			Character head = pope.Get<Character>(obj.game);
			Character head2 = ecumenical_patriarch.Get<Character>(obj.game);
			religions.catholic.SetHead(head, from_state: true);
			religions.orthodox.SetHead(head2, from_state: true);
			Catholic catholic = religions.catholic;
			catholic.cardinals.Clear();
			if (cardinals != null)
			{
				for (int i = 0; i < cardinals.Count; i++)
				{
					Character item = cardinals[i].Get<Character>(obj.game);
					catholic.cardinals.Add(item);
				}
			}
			religions.NotifyListeners("cardinals_changed");
		}
	}

	[Serialization.State(12)]
	public class HeadKingdomsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class PopeBonusesState : Serialization.ObjectSubstate
		{
			public List<string> pope_bonuses = new List<string>();

			public static PopeBonusesState Create()
			{
				return new PopeBonusesState();
			}

			public PopeBonusesState()
			{
			}

			public PopeBonusesState(Religions r)
			{
				InitFrom(r);
			}

			public static bool IsNeeded(Object obj)
			{
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				Catholic catholic = (obj as Religions)?.catholic;
				if (catholic == null)
				{
					return false;
				}
				List<Religion.CharacterBonus> list = catholic.pope_bonuses;
				if (list == null)
				{
					return false;
				}
				for (int i = 0; i < list.Count; i++)
				{
					pope_bonuses.Add(list[i].field.Path());
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(pope_bonuses.Count, "count");
				for (int i = 0; i < pope_bonuses.Count; i++)
				{
					ser.WriteStr(pope_bonuses[i], "bonus_def_", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				int num = ser.Read7BitUInt("count");
				for (int i = 0; i < num; i++)
				{
					pope_bonuses.Add(ser.ReadStr("bonus_def_", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Religions religions = obj as Religions;
				Catholic catholic = religions?.catholic;
				if (catholic != null)
				{
					Religion.Def def = catholic.def;
					religions?.catholic.DelPopeModifiers(religions.catholic.head_kingdom);
					catholic.pope_bonuses?.Clear();
					for (int i = 0; i < pope_bonuses.Count; i++)
					{
						DT.Field field = obj.game.dt.Find(pope_bonuses[i]);
						Religion.CharacterBonus characterBonus = new Religion.CharacterBonus
						{
							field = field
						};
						Stats.Def stats_def = obj.game.defs.Get<Stats.Def>("KingdomStats");
						def.LoadMods(obj.game, field, characterBonus.mods, stats_def);
						catholic.pope_bonuses.Add(characterBonus);
					}
					catholic.AddPopeModifiers(catholic.head_kingdom);
				}
			}
		}

		public int catholic;

		public int orthodox;

		public static HeadKingdomsState Create()
		{
			return new HeadKingdomsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Religions religions = obj.game.religions;
			if (religions.catholic.head_kingdom != religions.catholic.hq_kingdom)
			{
				return true;
			}
			if (religions.orthodox.head_kingdom != religions.orthodox.hq_kingdom)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Religions religions = obj.game.religions;
			catholic = ((religions.catholic.head_kingdom != null) ? religions.catholic.head_kingdom.id : 0);
			orthodox = ((religions.orthodox.head_kingdom != null) ? religions.orthodox.head_kingdom.id : 0);
			if (PopeBonusesState.IsNeeded(religions))
			{
				AddSubstate(new PopeBonusesState(religions));
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(catholic, "catholic");
			ser.Write7BitUInt(orthodox, "orthodox");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			catholic = ser.Read7BitUInt("catholic");
			orthodox = ser.Read7BitUInt("orthodox");
		}

		public override void ApplyTo(Object obj)
		{
			Religions religions = obj.game.religions;
			religions?.catholic.DelPopeModifiers(religions.catholic.head_kingdom);
			religions.catholic.head_kingdom = obj.game.GetKingdom(catholic);
			religions.orthodox.head_kingdom = obj.game.GetKingdom(orthodox);
		}
	}

	[Serialization.State(13)]
	public class JihadsState : Serialization.ObjectState
	{
		public List<int> jihad_kingdoms_ids = new List<int>();

		public List<int> jihad_targets_ids = new List<int>();

		public static JihadsState Create()
		{
			return new JihadsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Religions religions = obj.game.religions;
			if (religions.jihad_kingdoms.Count == 0)
			{
				return religions.jihad_targets.Count != 0;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Religions religions = obj.game.religions;
			for (int i = 0; i < religions.jihad_kingdoms.Count; i++)
			{
				jihad_kingdoms_ids.Add(religions.jihad_kingdoms[i].id);
			}
			for (int j = 0; j < religions.jihad_targets.Count; j++)
			{
				jihad_targets_ids.Add(religions.jihad_targets[j].id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = jihad_kingdoms_ids.Count;
			ser.Write7BitUInt(count, "jihad_kingdoms_count");
			for (int i = 0; i < count; i++)
			{
				ser.Write7BitUInt(jihad_kingdoms_ids[i], "jihad_kingdom_", i);
			}
			int count2 = jihad_targets_ids.Count;
			ser.Write7BitUInt(count2, "jihad_targets_count");
			for (int j = 0; j < count2; j++)
			{
				ser.Write7BitUInt(jihad_targets_ids[j], "jihad_target_", j);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("jihad_kingdoms_count");
			for (int i = 0; i < num; i++)
			{
				jihad_kingdoms_ids.Add(ser.Read7BitUInt("jihad_kingdom_", i));
			}
			int num2 = ser.Read7BitUInt("jihad_targets_count");
			for (int j = 0; j < num2; j++)
			{
				jihad_targets_ids.Add(ser.Read7BitUInt("jihad_target_", j));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Religions religions = obj.game.religions;
			if (religions.jihad_kingdoms.Count > 0)
			{
				religions.jihad_kingdoms.Clear();
			}
			if (religions.jihad_targets.Count > 0)
			{
				religions.jihad_targets.Clear();
			}
			for (int i = 0; i < jihad_kingdoms_ids.Count; i++)
			{
				Kingdom kingdom = obj.game.GetKingdom(jihad_kingdoms_ids[i]);
				religions.jihad_kingdoms.Add(kingdom);
			}
			for (int j = 0; j < jihad_targets_ids.Count; j++)
			{
				Kingdom kingdom2 = obj.game.GetKingdom(jihad_targets_ids[j]);
				religions.jihad_targets.Add(kingdom2);
			}
		}
	}

	[Serialization.State(14)]
	public class CrusadeState : Serialization.ObjectState
	{
		public int crusadesCount;

		public float crusade_end_delta;

		public static CrusadeState Create()
		{
			return new CrusadeState();
		}

		public static bool IsNeeded(Object obj)
		{
			Religions religions = obj as Religions;
			if (Crusade.Get(obj.game) == null && !(religions.catholic.last_crusade_end != Time.Zero))
			{
				return religions.catholic.crusades_count != 0;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			crusade_end_delta = obj.game.religions.catholic.last_crusade_end - obj.game.time;
			crusadesCount = obj.game.religions.catholic.crusades_count;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(crusadesCount, "crusades_count");
			ser.WriteFloat(crusade_end_delta, "crusade_end_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			crusadesCount = ser.Read7BitUInt("crusades_count");
			crusade_end_delta = ser.ReadFloat("crusade_end_delta");
		}

		public override void ApplyTo(Object obj)
		{
			obj.game.religions.catholic.last_crusade_end = obj.game.time + crusade_end_delta;
			obj.game.religions.catholic.crusades_count = crusadesCount;
		}
	}

	[Serialization.State(15)]
	public class UsedNamesState : Serialization.ObjectState
	{
		public struct UsedNames
		{
			public string name;

			public int idx;
		}

		public List<UsedNames>[] used_names;

		public static UsedNamesState Create()
		{
			return new UsedNamesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Religions { all: not null } religions))
			{
				return false;
			}
			int count = religions.all.Count;
			used_names = new List<UsedNames>[count];
			for (int i = 0; i < count; i++)
			{
				used_names[i] = new List<UsedNames>();
				foreach (KeyValuePair<string, int> used_name in religions.all[i].used_names)
				{
					used_names[i].Add(new UsedNames
					{
						name = used_name.Key,
						idx = used_name.Value
					});
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = used_names.Length;
			ser.Write7BitUInt(num, "length");
			for (int i = 0; i < num; i++)
			{
				int count = used_names[i].Count;
				ser.Write7BitUInt(count, $"{i}_count");
				for (int j = 0; j < used_names[i].Count; j++)
				{
					ser.WriteStr(used_names[i][j].name, $"{i}_name_", j);
					ser.Write7BitUInt(used_names[i][j].idx, $"{i}_idx_", j);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("length");
			used_names = new List<UsedNames>[num];
			for (int i = 0; i < num; i++)
			{
				used_names[i] = new List<UsedNames>();
				int num2 = ser.Read7BitUInt($"{i}_count");
				for (int j = 0; j < num2; j++)
				{
					string name = ser.ReadStr($"{i}_name_", j);
					int idx = ser.Read7BitUInt($"{i}_idx_", j);
					UsedNames item = new UsedNames
					{
						name = name,
						idx = idx
					};
					used_names[i].Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Religions { all: not null } religions))
			{
				return;
			}
			if (religions.all.Count != used_names.Length)
			{
				obj.Error("Religions count mismatch!");
				return;
			}
			for (int i = 0; i < religions.all.Count; i++)
			{
				if (religions.all[i].used_names != null)
				{
					religions.all[i].used_names.Clear();
				}
				else
				{
					religions.all[i].used_names = new Dictionary<string, int>();
				}
				for (int j = 0; j < used_names[i].Count; j++)
				{
					religions.all[i].used_names[used_names[i][j].name] = used_names[i][j].idx;
				}
			}
		}
	}

	[Serialization.State(16)]
	public class CatholicCooldowns : Serialization.ObjectState
	{
		private float next_excommunicate_delta;

		private float next_enemy_excommunicate_delta;

		private float next_court_cleric_delta;

		private float next_ask_crusade_funds_delta;

		public static CatholicCooldowns Create()
		{
			return new CatholicCooldowns();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Catholic catholic = (obj as Religions)?.catholic;
			if (catholic == null)
			{
				return false;
			}
			next_excommunicate_delta = catholic.next_excommunicate - catholic.game.session_time;
			next_enemy_excommunicate_delta = catholic.next_enemy_excommunicate - catholic.game.session_time;
			next_court_cleric_delta = catholic.next_ask_court_cleric - catholic.game.session_time;
			next_ask_crusade_funds_delta = catholic.next_ask_crusade_funds - catholic.game.session_time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(next_excommunicate_delta, "next_excommunicate_delta");
			ser.WriteFloat(next_enemy_excommunicate_delta, "next_enemy_excommunicate_delta");
			ser.WriteFloat(next_court_cleric_delta, "next_court_cleric_delta");
			ser.WriteFloat(next_ask_crusade_funds_delta, "next_ask_crusade_funds_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			next_excommunicate_delta = ser.ReadFloat("next_excommunicate_delta");
			next_enemy_excommunicate_delta = ser.ReadFloat("next_enemy_excommunicate_delta");
			next_court_cleric_delta = ser.ReadFloat("next_court_cleric_delta");
			next_ask_crusade_funds_delta = ser.ReadFloat("next_ask_crusade_funds_delta");
		}

		public override void ApplyTo(Object obj)
		{
			Religions religions = obj as Religions;
			Catholic catholic = religions?.catholic;
			if (catholic != null)
			{
				catholic.next_excommunicate = religions.game.session_time + next_excommunicate_delta;
				catholic.next_enemy_excommunicate = religions.game.session_time + next_enemy_excommunicate_delta;
				catholic.next_ask_court_cleric = religions.game.session_time + next_court_cleric_delta;
				catholic.next_ask_crusade_funds = religions.game.session_time + next_ask_crusade_funds_delta;
			}
		}
	}

	[Serialization.State(17)]
	public class ExcommunicatedCountState : Serialization.ObjectState
	{
		private int excommunicated_count;

		public static ExcommunicatedCountState Create()
		{
			return new ExcommunicatedCountState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Catholic catholic = (obj as Religions)?.catholic;
			if (catholic == null)
			{
				return false;
			}
			excommunicated_count = catholic.GetExcommunicatedCount();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(excommunicated_count, "excommunicated_count");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			excommunicated_count = (int)ser.ReadFloat("excommunicated_count");
		}

		public override void ApplyTo(Object obj)
		{
			((obj as Religions)?.catholic)?.SetExcommunicatedCount(excommunicated_count, send_state: false);
		}
	}

	[Serialization.Event(27)]
	public class StartCrusadeEvent : Serialization.ObjectEvent
	{
		private int target_kid;

		private int helping_kid;

		private string reason = "";

		private NID leader_nid = NID.Null;

		public Data data;

		public StartCrusadeEvent()
		{
		}

		public static StartCrusadeEvent Create()
		{
			return new StartCrusadeEvent();
		}

		public StartCrusadeEvent(Kingdom target, Kingdom helping_kingdom, string reason, Character leader)
		{
			if (target != null)
			{
				target_kid = target.id;
			}
			if (helping_kingdom != null)
			{
				helping_kid = helping_kingdom.id;
			}
			if (reason != null)
			{
				this.reason = reason;
			}
			if (leader == null)
			{
				leader_nid = leader;
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(target_kid, "target_kid");
			ser.Write7BitUInt(helping_kid, "helping_kid");
			ser.WriteStr(reason, "reason");
			ser.WriteNID<Character>(leader_nid, "leader_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			target_kid = ser.Read7BitUInt("target_kid");
			helping_kid = ser.Read7BitUInt("helping_kid");
			reason = ser.ReadStr("reason");
			leader_nid = ser.ReadNID<Character>("leader_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj.game.GetKingdom(target_kid);
			Kingdom kingdom2 = obj.game.GetKingdom(helping_kid);
			Character character = leader_nid.Get<Character>(obj.game);
			if (kingdom != null && character == null)
			{
				Crusade.Start(kingdom, kingdom2, reason, character);
			}
		}
	}

	[Serialization.Event(28)]
	public class RestorePapcyEvent : Serialization.ObjectEvent
	{
		private string reason = "";

		private NID leader_nid = NID.Null;

		public Data data;

		public RestorePapcyEvent()
		{
		}

		public static RestorePapcyEvent Create()
		{
			return new RestorePapcyEvent();
		}

		public RestorePapcyEvent(string reason)
		{
			if (reason != null)
			{
				this.reason = reason;
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(reason, "reason");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			reason = ser.ReadStr("reason");
		}

		public override void ApplyTo(Object obj)
		{
			obj?.game?.religions?.catholic.RestorePapacyAnswer(reason);
		}
	}

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public Religion.Def def;

	public Catholic catholic;

	public Orthodox orthodox;

	public Sunni sunni;

	public Shia shia;

	public Pagan pagan;

	public List<Religion> all = new List<Religion>();

	public List<Kingdom> jihad_kingdoms = new List<Kingdom>();

	public List<Kingdom> jihad_targets = new List<Kingdom>();

	private int religion_to_update;

	public Religions(Multiplayer multiplayer)
		: base(multiplayer)
	{
		Error("created from multiplayer");
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Religions(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	public Religions(Game game)
		: base(game)
	{
		SetNid(1, update_registry: false);
		def = game.defs.GetBase<Religion.Def>();
		all.Add(catholic = new Catholic(game, game.defs.Get<Religion.Def>("Catholic")));
		all.Add(orthodox = new Orthodox(game, game.defs.Get<Religion.Def>("Orthodox")));
		all.Add(sunni = new Sunni(game, game.defs.Get<Religion.Def>("Sunni")));
		all.Add(shia = new Shia(game, game.defs.Get<Religion.Def>("Shia")));
		all.Add(pagan = new Pagan(game, game.defs.Get<Religion.Def>("Pagan")));
	}

	public Religion Get(string name)
	{
		if (name == "Muslim")
		{
			name = "Shia";
		}
		else if (name == "Christian")
		{
			name = "Orthodox";
		}
		for (int i = 0; i < all.Count; i++)
		{
			Religion religion = all[i];
			if (religion.name == name)
			{
				return religion;
			}
		}
		return null;
	}

	public Religion Get(Religion.Def def)
	{
		for (int i = 0; i < all.Count; i++)
		{
			Religion religion = all[i];
			if (religion.def == def)
			{
				return religion;
			}
		}
		return null;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"catholic" => catholic, 
			"orthodox" => orthodox, 
			"sunni" => sunni, 
			"shia" => shia, 
			"pagan" => pagan, 
			"pope" => catholic.head, 
			"papacy" => catholic.hq_kingdom, 
			"rome_kingdom" => catholic.hq_realm.GetKingdom(), 
			"cardinals" => new Value(catholic.cardinals), 
			"ecumenical_patriarch" => orthodox.head, 
			"ecumenical_patriarch_kingdom" => orthodox.head_kingdom, 
			"byzantium" => orthodox.hq_kingdom, 
			"constantinople_kingdom" => orthodox.hq_realm.GetKingdom(), 
			_ => Value.Unknown, 
		};
	}

	public void Init(bool new_game)
	{
		SetNid(1);
		for (int i = 0; i < all.Count; i++)
		{
			all[i].Init(new_game);
		}
		InitRealmsAndKingdoms(new_game);
		if (new_game)
		{
			for (int j = 0; j < all.Count; j++)
			{
				all[j].InitCharacters(new_game);
			}
		}
		UpdateNextFrame();
	}

	public override void OnUpdate()
	{
		UpdateNextFrame();
		catholic?.RemoveInvalidCardinals();
		Religion religion = all[religion_to_update];
		religion_to_update = (religion_to_update + 1) % all.Count;
		religion.OnUpdate();
	}

	public override void OnUnloadMap()
	{
		if (catholic != null && catholic.crusade != null)
		{
			game.scheduler.Unregister(catholic.crusade);
			game.religions.NotifyListeners("crusade_removed", catholic.crusade);
			catholic.crusade = null;
		}
		for (int i = 0; i < all.Count; i++)
		{
			Religion religion = all[i];
			religion.head = null;
			religion.hq_kingdom = null;
			religion.hq_realm = null;
			religion.head_kingdom = null;
		}
		jihad_kingdoms.Clear();
		jihad_targets.Clear();
		SetNid(0, update_registry: false);
	}

	protected override void OnDestroy()
	{
		if (game.religions == this)
		{
			game.religions = null;
		}
		for (int i = 0; i < all.Count; i++)
		{
			all[i].game = null;
		}
		base.OnDestroy();
	}

	private Religion RandomReligion()
	{
		return all[game.Random(0, all.Count)];
	}

	private void InitRealmsAndKingdoms(bool new_game)
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			kingdom.royalFamily = new RoyalFamily(kingdom);
			if (new_game && kingdom.IsAuthority())
			{
				if (kingdom.IsPapacy())
				{
					kingdom.religion = catholic;
					kingdom.excommunicated = false;
					continue;
				}
				CharacterFactory.PopulateInitialRoyalFamily(kingdom);
				Realm capital = kingdom.GetCapital();
				if (capital != null && capital?.castle?.governor == null)
				{
					kingdom.GetKing()?.Govern(capital?.castle);
				}
			}
			if (kingdom == null || kingdom.def == null || kingdom.religion != null || kingdom.type != Kingdom.Type.Regular)
			{
				continue;
			}
			string text = kingdom.csv_field?.GetString("Religion");
			if (string.IsNullOrEmpty(text))
			{
				text = kingdom.def?.GetString("religion");
			}
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			string text2 = "";
			int num = text.IndexOf('.');
			if (num >= 0)
			{
				text2 = text.Substring(num + 1);
				text = text.Substring(0, num);
			}
			kingdom.religion = Get(text);
			if (kingdom.religion == null)
			{
				Game.Log("Invalid religion specified for kingdom " + kingdom.Name + ": " + text, Game.LogType.Error);
			}
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			switch (text2)
			{
			case "I":
				if (text != "Orthodox")
				{
					break;
				}
				if (!game.rules.MapIsShattered())
				{
					kingdom.subordinated = false;
					if (new_game && kingdom != game.religions.orthodox.head_kingdom)
					{
						(kingdom.religion as Orthodox).PatriarchChosen(kingdom, null, notify: false);
					}
				}
				continue;
			case "E":
				if (!(text != "Catholic"))
				{
					kingdom.excommunicated = true;
					continue;
				}
				break;
			case "C":
				if (kingdom.is_muslim)
				{
					if (!game.rules.MapIsShattered())
					{
						kingdom.caliphate = true;
						Religion.RefreshModifiers(kingdom);
					}
					continue;
				}
				break;
			case "H":
				continue;
			}
			Game.Log(kingdom.Name + ": Invalid religion suffix: " + text + "." + text2, Game.LogType.Error);
		}
		for (int j = 0; j < game.landRealmsCount; j++)
		{
			Realm realm = game.realms[j];
			if (realm == null)
			{
				continue;
			}
			if (realm.religiousPower.amount != realm.religiousPower.maxAmount)
			{
				realm.religiousPower.InitAmount(game.Random(realm.religiousPower.maxAmount * 0.7f, realm.religiousPower.maxAmount));
			}
			if (realm.religion != null)
			{
				continue;
			}
			Kingdom kingdom2 = realm.GetKingdom();
			Kingdom kingdom3 = game.GetKingdom(realm.id);
			string text3 = realm.csv_field?.GetString("Religion");
			if (string.IsNullOrEmpty(text3))
			{
				text3 = realm.def?.GetString("religion");
			}
			if (!string.IsNullOrEmpty(text3))
			{
				int num2 = text3.IndexOf('.');
				if (num2 >= 0)
				{
					text3.Substring(num2 + 1);
					text3 = text3.Substring(0, num2);
				}
				realm.religion = Get(text3);
				if (realm.religion == null)
				{
					Game.Log("Invalid religion specified for realm " + realm.name + ": " + text3, Game.LogType.Error);
				}
			}
			if (realm.religion == null && kingdom3 != null && kingdom3.religion != null)
			{
				realm.religion = kingdom3.religion;
			}
			if (realm.religion == null && kingdom2 != null && kingdom2.religion != null)
			{
				realm.religion = kingdom2.religion;
			}
			if (realm.religion == null)
			{
				Game.Log("Picking random religion for realm " + realm.name, Game.LogType.Warning);
				realm.religion = RandomReligion();
			}
			if (kingdom3 != null && kingdom3.religion == null)
			{
				kingdom3.religion = realm.religion;
			}
			if (kingdom2 != null && kingdom2.religion == null)
			{
				kingdom2.religion = realm.religion;
			}
			realm.RefreshTags();
		}
		for (int k = 0; k < game.kingdoms.Count; k++)
		{
			Kingdom kingdom4 = game.kingdoms[k];
			if (kingdom4 != null && kingdom4.type == Kingdom.Type.Regular)
			{
				if (kingdom4.religion == null)
				{
					Game.Log("Picking random religion for kingdom " + kingdom4.Name, Game.LogType.Warning);
					kingdom4.religion = RandomReligion();
				}
				kingdom4.religion.AddModifiers(kingdom4);
				kingdom4.NotifyListeners("religion_changed");
			}
		}
		game.GetKingdom("CrusadeFaction").religion = game.religions.catholic;
		for (int l = 0; l < game.landRealmsCount; l++)
		{
			Realm realm2 = game.realms[l];
			if (realm2 != null)
			{
				realm2.FixSettlementsReligion();
				realm2.FixPietyType();
			}
		}
	}

	public bool IsHolyLandOrHQ(Realm r)
	{
		if (r != catholic.hq_realm && r != catholic.holy_lands_realm && r != orthodox.hq_realm && r != sunni.hq_realm && !sunni.holy_lands_realms.Contains(r) && r != shia.hq_realm && r != shia.holy_lands_realm)
		{
			return r == pagan.hq_realm;
		}
		return true;
	}

	public override Value GetDumpStateValue()
	{
		return Value.Null;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		catholic?.DumpState(dump, verbosity);
		orthodox?.DumpState(dump, verbosity);
		sunni?.DumpState(dump, verbosity);
		shia?.DumpState(dump, verbosity);
		pagan?.DumpState(dump, verbosity);
	}

	public override void OnTimer(Timer timer)
	{
		string name = timer.name;
		if (name == "update_cardinals")
		{
			catholic?.UpdateCardinals();
		}
		else
		{
			base.OnTimer(timer);
		}
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (!(message == "war_concluded"))
		{
			return;
		}
		War war = obj as War;
		string reason = param as string;
		if (war != null && war.IsJihad())
		{
			Kingdom leader = war.GetLeader(0);
			if (!leader.IsCaliphate())
			{
				leader = war.GetLeader(1);
			}
			War.EndJihad(leader, reason, end_war: false);
			war.DelListener(this);
		}
	}
}
