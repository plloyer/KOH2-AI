using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Crusade)]
public class Crusade : Object, IVars, IRelationCheck
{
	[Serialization.State(11)]
	public class MainState : Serialization.ObjectState
	{
		private Data data;

		public static MainState Create()
		{
			return new MainState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Crusade obj2))
			{
				return false;
			}
			data = FullData.Create();
			if (data.InitFrom(obj2))
			{
				return true;
			}
			return false;
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
			if (obj is Crusade crusade)
			{
				obj.game.religions.catholic.crusade = crusade;
				Army army = crusade.army;
				Character leader = crusade.leader;
				Kingdom src_kingdom = crusade.src_kingdom;
				data.ApplyTo(crusade, obj.game);
				if (crusade.ended)
				{
					Vars vars = new Vars(crusade);
					vars.Set("leader_kingdom", src_kingdom);
					vars.Set("leader", leader);
					vars.Set("army", army);
					obj.game.religions.catholic.crusade = null;
					obj.game.religions.catholic.last_crusade_end = obj.game.time;
					crusade.leader?.NotifyListeners("crusade_ended");
					obj.game.religions.NotifyListeners("crusade_ended", vars);
					crusade.leader?.NotifyListeners("crusader_status_changed");
				}
				else
				{
					obj.game.scheduler.RegisterAfterSeconds(crusade, 1f, exact: false);
					crusade.leader.NotifyListeners("crusader_status_changed");
					crusade.army.NotifyListeners("crusader_status_changed");
				}
			}
		}
	}

	[Serialization.State(12)]
	public class WealthState : Serialization.ObjectState
	{
		private float wealth;

		public static WealthState Create()
		{
			return new WealthState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Crusade crusade))
			{
				return false;
			}
			return crusade.wealth != 0f;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Crusade crusade))
			{
				return false;
			}
			wealth = crusade.wealth;
			return false;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(wealth, "wealth");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			wealth = ser.ReadFloat("wealth");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Crusade crusade)
			{
				crusade.wealth = wealth;
				crusade.NotifyListeners("wealth_changed");
			}
		}
	}

	public class Def : Logic.Def
	{
		public float cooldown = 60f;

		public float cooldownForPlayers = 40f;

		public float initialCooldownCrusade = 1800f;

		public int minTroopsOnGoHome = 2;

		public int maxTroopsOnGoHome = 4;

		public int skillCopyPercentage = 70;

		public int crusadeNumKingdoms = 3;

		public float crusadePlayerKingdomsPerc = 30f;

		public float sackGoldMultiplier = 50f;

		public int giveToKingdomMaxDistance = 3;

		public int max_peasants = 4;

		public float supplies_consumption_rate_mod = 0.5f;

		public OutcomeDef capture_realm_outcomes;

		public OutcomeDef end_outcomes;

		public OutcomeDef crusader_death_outcomes;

		public OutcomeDef crusader_exiled_outcomes;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			cooldown = field.GetFloat("cooldown", null, cooldown);
			cooldownForPlayers = field.GetFloat("cooldown_for_players", null, cooldownForPlayers);
			initialCooldownCrusade = field.GetFloat("initial_cooldown_crusade", null, initialCooldownCrusade);
			minTroopsOnGoHome = field.GetInt("minTroopsOnGoHome", null, minTroopsOnGoHome);
			maxTroopsOnGoHome = field.GetInt("maxTroopsOnGoHome", null, maxTroopsOnGoHome);
			skillCopyPercentage = field.GetInt("skillCopyPercentage", null, skillCopyPercentage);
			crusadeNumKingdoms = field.GetInt("crusadeNumKingdoms", null, crusadeNumKingdoms);
			crusadePlayerKingdomsPerc = field.GetFloat("crusadePlayerKingdomsPerc", null, crusadePlayerKingdomsPerc);
			max_peasants = field.GetInt("max_peasants", null, max_peasants);
			supplies_consumption_rate_mod = field.GetFloat("supplies_consumption_rate_mod", null, supplies_consumption_rate_mod);
			giveToKingdomMaxDistance = field.GetInt("giveToKingdomMaxDistance", null, giveToKingdomMaxDistance);
			DT.Field field2 = field.FindChild("capture_realm_outcomes");
			if (field2 != null)
			{
				DT.Field defaults = null;
				capture_realm_outcomes = new OutcomeDef(game, field2, defaults);
			}
			DT.Field field3 = field.FindChild("end_crusade_outcomes");
			if (field3 != null)
			{
				DT.Field defaults2 = null;
				end_outcomes = new OutcomeDef(game, field3, defaults2);
			}
			DT.Field field4 = field.FindChild("crusader_death_outcomes");
			if (field4 != null)
			{
				DT.Field defaults3 = null;
				crusader_death_outcomes = new OutcomeDef(game, field4, defaults3);
			}
			DT.Field field5 = field.FindChild("crusader_exiled_outcomes");
			if (field5 != null)
			{
				DT.Field defaults4 = null;
				crusader_exiled_outcomes = new OutcomeDef(game, field5, defaults4);
			}
			sackGoldMultiplier = field.GetFloat("sackGoldMultiplier", null, sackGoldMultiplier);
			return true;
		}
	}

	public class ForceRebelParams
	{
		public string type;

		public string spawn_condition;

		public Kingdom loyalTo;

		public Realm realm;

		public ForceRebelParams(string type = null, string spawn_condition = null, Kingdom loyalTo = null, Realm realm = null)
		{
			this.type = type;
			this.spawn_condition = spawn_condition;
			this.loyalTo = loyalTo;
			this.realm = realm;
		}
	}

	public class FullData : Data
	{
		public int src_kid;

		public int target_kid;

		public int helping_kid;

		public NID army_nid = NID.Null;

		public NID leader_nid = NID.Null;

		public string reason = "";

		public float camp_time_delta;

		public string end_reason = "";

		public string end_outcome = "";

		public NID last_captured_realm_nid = NID.Null;

		public bool ended;

		public NID last_established_kingdom_nid = NID.Null;

		public static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Crusade crusade))
			{
				return false;
			}
			ended = crusade.ended;
			if (crusade.end_reason != null && crusade.end_reason != "")
			{
				end_reason = crusade.end_reason;
			}
			if (crusade.end_outcome != null && crusade.end_outcome != "")
			{
				end_outcome = crusade.end_outcome;
			}
			target_kid = crusade.target.id;
			if (crusade.helping_kingdom != null)
			{
				helping_kid = crusade.helping_kingdom.id;
			}
			if (crusade.army != null)
			{
				army_nid = crusade.army;
			}
			else
			{
				army_nid = crusade.leader?.GetArmy();
			}
			leader_nid = crusade.leader;
			src_kid = ((crusade?.src_kingdom != null) ? crusade.src_kingdom.id : 0);
			reason = crusade.reason;
			if (crusade.camp_time != Time.Zero)
			{
				camp_time_delta = crusade.camp_time - crusade.game.time;
				if (camp_time_delta < 0f)
				{
					camp_time_delta = 0f;
				}
			}
			else
			{
				camp_time_delta = float.NegativeInfinity;
			}
			if (crusade.last_captured_realm != null)
			{
				last_captured_realm_nid = crusade.last_captured_realm;
			}
			if (crusade.last_established_kingdom != null)
			{
				last_established_kingdom_nid = crusade.last_established_kingdom;
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(target_kid, "target_kid");
			ser.Write7BitUInt(helping_kid, "helping_kid");
			ser.WriteNID<Army>(army_nid, "army_nid");
			ser.WriteNID<Character>(leader_nid, "leader_nid");
			ser.Write7BitUInt(src_kid, "src_kid");
			ser.WriteStr(reason, "reason");
			ser.WriteFloat(camp_time_delta, "camp_time_delta");
			ser.WriteStr(end_reason, "end_reason");
			ser.WriteStr(end_outcome, "end_outcome");
			ser.WriteNID<Realm>(last_captured_realm_nid, "last_captured_realm");
			ser.WriteBool(ended, "ended");
			ser.WriteNID<Kingdom>(last_established_kingdom_nid, "last_established_kingdom");
		}

		public override void Load(Serialization.IReader ser)
		{
			target_kid = ser.Read7BitUInt("target_kid");
			helping_kid = ser.Read7BitUInt("helping_kid");
			army_nid = ser.ReadNID<Army>("army_nid");
			leader_nid = ser.ReadNID<Character>("leader_nid");
			if (Serialization.cur_version > 7)
			{
				src_kid = ser.Read7BitUInt("src_kid");
			}
			else
			{
				src_kid = 0;
			}
			reason = ser.ReadStr("reason");
			camp_time_delta = ser.ReadFloat("camp_time_delta");
			end_reason = ser.ReadStr("end_reason");
			end_outcome = ser.ReadStr("end_outcome");
			last_captured_realm_nid = ser.ReadNID<Realm>("last_captured_realm");
			ended = ser.ReadBool("ended");
			if (Serialization.cur_version > 6)
			{
				last_established_kingdom_nid = ser.ReadNID<Kingdom>("last_established_kingdom");
			}
		}

		public override object GetObject(Game game)
		{
			Crusade crusade = game.religions.catholic.crusade;
			if (crusade == null)
			{
				crusade = new Crusade(game);
			}
			return crusade;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Crusade crusade))
			{
				return false;
			}
			if (end_reason != "")
			{
				crusade.end_reason = end_reason;
			}
			if (end_outcome != "")
			{
				crusade.end_outcome = end_outcome;
			}
			crusade.target = game.GetKingdom(target_kid);
			crusade.helping_kingdom = game.GetKingdom(helping_kid);
			crusade.army = army_nid.Get<Army>(game);
			crusade.leader = leader_nid.Get<Character>(game);
			if (crusade.leader != null)
			{
				crusade.leader.RefreshTags();
			}
			if (Serialization.cur_version > 7)
			{
				crusade.src_kingdom = game.GetKingdom(src_kid);
			}
			if (crusade.src_kingdom == null)
			{
				crusade.src_kingdom = crusade.leader?.GetKingdom();
			}
			crusade.reason = reason;
			if (float.IsNegativeInfinity(camp_time_delta))
			{
				crusade.camp_time = Time.Zero;
			}
			else
			{
				crusade.camp_time = game.time + camp_time_delta;
			}
			crusade.last_captured_realm = last_captured_realm_nid.Get<Realm>(game);
			crusade.last_established_kingdom = last_established_kingdom_nid.Get<Kingdom>(game);
			crusade.army?.NotifyListeners("crusader_status_changed");
			crusade.ended = ended;
			return true;
		}
	}

	public struct CrusaderCandidate
	{
		public Character character;

		public float eval;

		public override string ToString()
		{
			return character.ToString() + ": " + eval;
		}
	}

	public struct CrusadeKingdomCandidate
	{
		public Kingdom kingdom;

		public float eval;

		public override string ToString()
		{
			return kingdom.Name + ": " + eval;
		}
	}

	private const int STATES_IDX = 10;

	public Def def;

	public int kingdom_id;

	public Kingdom target;

	public string reason;

	public Kingdom helping_kingdom;

	public Kingdom last_established_kingdom;

	public Army army;

	public Time camp_time = Time.Zero;

	public string end_reason;

	public string end_outcome;

	public ForceRebelParams force_go_rogue_params;

	public bool crusade_successful;

	public Realm last_captured_realm;

	public List<OutcomeDef> force_capture_realm_outcomes;

	public List<OutcomeDef> capture_realm_outcomes;

	public List<OutcomeDef> capture_realm_unique_outcomes;

	public List<OutcomeDef> resolve_land_unique_outcomes;

	public List<OutcomeDef> force_end_outcomes;

	public List<OutcomeDef> end_outcomes;

	public List<OutcomeDef> end_unique_outcomes;

	public List<OutcomeDef> crusader_death_outcomes;

	public List<OutcomeDef> crusader_exiled_outcomes;

	public List<OutcomeDef> crusader_death_unique_outcomes;

	public List<OutcomeDef> crusader_exiled_unique_outcomes;

	public float wealth;

	public List<Settlement> occupiedKeeps = new List<Settlement>();

	public object message_icon;

	public static bool choose_only_player_leaders;

	public Character leader;

	public Kingdom src_kingdom;

	public bool ended;

	public Catholic religion => game.religions.catholic;

	public Crusade(Multiplayer multiplayer)
		: base(multiplayer)
	{
		if (multiplayer != null && multiplayer.game != null && multiplayer.game.defs != null)
		{
			kingdom_id = multiplayer.game.GetKingdom("CrusadeFaction")?.id ?? 0;
			Def def = multiplayer.game.defs.GetBase<Def>();
			if (def != null)
			{
				this.def = def;
			}
		}
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Crusade(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	protected Crusade(Game game)
		: base(game)
	{
		kingdom_id = game.GetKingdom("CrusadeFaction").id;
		def = game.defs.GetBase<Def>();
	}

	protected Crusade(Kingdom target, string reason, Kingdom helping_kingdom, Army army)
		: base(target.game)
	{
		kingdom_id = game.GetKingdom("CrusadeFaction").id;
		this.target = target;
		this.reason = reason;
		this.helping_kingdom = helping_kingdom;
		this.army = army;
		leader = army.leader;
		last_established_kingdom = null;
		src_kingdom = leader.GetKingdom();
		def = game.defs.GetBase<Def>();
		game.scheduler.RegisterAfterSeconds(this, 1f, exact: false);
	}

	public static Crusade Get(Game game)
	{
		return game.religions.catholic.crusade;
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"target" => target, 
			"reason" => reason, 
			"helping_kingdom" => helping_kingdom, 
			"army" => army, 
			"leader" => leader, 
			"src_kingdom" => src_kingdom, 
			"end_reason" => end_reason, 
			"religion" => religion, 
			"pope" => religion.head, 
			"captured_realm" => last_captured_realm, 
			"wealth" => wealth, 
			"is_active" => !ended && leader != null, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}

	public override string ToString()
	{
		string text = "";
		if (target != null)
		{
			text = text + "Crusade against " + target.Name;
		}
		if (helping_kingdom != null)
		{
			text = text + ", helping " + helping_kingdom.Name;
		}
		if (leader != null)
		{
			text = text + ", led by " + leader.ToString();
		}
		return text;
	}

	public static Crusade Start(Kingdom target, Kingdom helping_kingdom, Character leader = null)
	{
		if (target == null)
		{
			return null;
		}
		string text = PickReason(target, helping_kingdom);
		if (text == null)
		{
			return null;
		}
		if (leader == null)
		{
			leader = PickLeader(target.game, target, helping_kingdom);
		}
		if (leader == null || !IsValidLeader(leader, target, helping_kingdom))
		{
			return null;
		}
		Kingdom kingdom = leader.GetKingdom();
		if (kingdom != null && !kingdom.ai.Enabled(KingdomAI.EnableFlags.Diplomacy))
		{
			new LeadCrusadeOffer(kingdom, target, helping_kingdom, text, leader).Send();
			return null;
		}
		Crusade crusade = Start(target, helping_kingdom, text, leader);
		kingdom?.AddRelationModifier(crusade?.religion.hq_kingdom, "rel_accept_lead_crusade", crusade);
		return crusade;
	}

	public static Crusade Start(Kingdom target, Kingdom helping_kingdom, string reason, Character leader, bool send_state = true)
	{
		if (target.game.religions.catholic.hq_kingdom.IsDefeated())
		{
			Game.Log(string.Concat("Trying to start crusade while Papacy is destoryed! Leader: ", leader, " Target: ", target, " Helping kingdom: ", helping_kingdom), Game.LogType.Error);
			return null;
		}
		if (!IsValidLeader(leader, target, helping_kingdom))
		{
			Game.Log(string.Concat("Trying to start crusade with invalid leader: ", leader, " Target: ", target, " Helping kingdom: ", helping_kingdom), Game.LogType.Error);
			return null;
		}
		if (!target.IsAuthority() && send_state)
		{
			target.game.religions.SendEvent(new Religions.StartCrusadeEvent(target, helping_kingdom, reason, leader));
			return null;
		}
		leader.GetArmy()?.battle?.Leave(leader.GetArmy(), check_victory: true, is_restart: false, force_no_consequences: true);
		Army army = CreateArmy(leader);
		Crusade crusade = new Crusade(target, reason, helping_kingdom, army);
		crusade.religion.crusade = crusade;
		crusade.religion.crusades_count++;
		leader.RefreshTags();
		FillArmy(crusade, army);
		if (send_state)
		{
			crusade.SendState<MainState>();
			target.game.religions.SendState<Religions.CrusadeState>();
		}
		crusade.game.religions.FireEvent("new_crusade", crusade);
		leader.NotifyListeners("crusade_started", crusade);
		leader.NotifyListeners("crusader_status_changed");
		leader.SetMissionKingdom(FactionUtils.GetFactionKingdom(target.game, "CrusadeFaction"));
		army.NotifyListeners("crusader_status_changed");
		helping_kingdom?.NotifyListeners("helped_by_crusade", crusade);
		target.AddRelationModifier(leader.GetKingdom(), "rel_lead_crusade_against_target", null);
		return crusade;
	}

	public void End(bool send_state = true)
	{
		Catholic catholic = religion;
		ended = true;
		game.scheduler.Unregister(catholic.crusade);
		Vars vars = new Vars(this);
		vars.Set("leader_kingdom", src_kingdom);
		vars.Set("leader", leader);
		vars.Set("army", army);
		leader.SetMissionKingdom(null);
		Character character = leader;
		EndOutcome();
		force_end_outcomes = null;
		catholic.crusade = null;
		catholic.last_crusade_end = game.time;
		FreeAllKeeps();
		game.religions.NotifyListeners("crusade_ended", vars);
		character?.NotifyListeners("crusade_ended");
		character?.NotifyListeners("crusader_status_changed");
		if (send_state)
		{
			SendState<MainState>();
			game.religions.SendState<Religions.CrusadeState>();
		}
		Destroy();
	}

	public bool TryEstablishKingdom()
	{
		return true;
	}

	public override void OnUpdate()
	{
		if (src_kingdom != null && !src_kingdom.IsAuthority())
		{
			game.scheduler.RegisterAfterSeconds(this, 1f, exact: false);
			return;
		}
		if (reason == "holy_lands")
		{
			Kingdom kingdom = religion.holy_lands_realm.GetKingdom();
			if (!(ValidateTarget(kingdom, null, new_crusade: false) == "ok"))
			{
				end_reason = "holy_lands_freed";
				End();
				return;
			}
			target = kingdom;
		}
		if (!CheckOver())
		{
			game.scheduler.RegisterAfterSeconds(this, 1f, exact: false);
			Think();
		}
	}

	public bool CheckOver()
	{
		string text = Validate();
		if (text != "ok")
		{
			end_reason = text;
			End();
			return true;
		}
		return false;
	}

	public override IRelationCheck GetStanceObj()
	{
		return this;
	}

	public override RelationUtils.Stance GetStance(Rebellion r)
	{
		return RelationUtils.Stance.Peace;
	}

	public override RelationUtils.Stance GetStance(Crusade r)
	{
		if (r == this)
		{
			return RelationUtils.Stance.Own;
		}
		return RelationUtils.Stance.Alliance;
	}

	public override RelationUtils.Stance GetStance(Kingdom k)
	{
		if (k == null)
		{
			return RelationUtils.Stance.Peace;
		}
		if (k == target)
		{
			return RelationUtils.Stance.War;
		}
		if (k == src_kingdom)
		{
			return RelationUtils.Stance.Peace;
		}
		if (k == helping_kingdom)
		{
			return RelationUtils.Stance.Peace;
		}
		Catholic catholic = religion;
		if (k == catholic.head_kingdom)
		{
			return RelationUtils.Stance.Peace;
		}
		if (k == catholic.hq_kingdom)
		{
			return RelationUtils.Stance.Peace;
		}
		if (catholic.head_kingdom != null && catholic.head_kingdom != catholic.hq_kingdom && catholic.head_kingdom.IsEnemy(k))
		{
			return RelationUtils.Stance.War;
		}
		if (catholic.hq_kingdom != null && !catholic.hq_kingdom.IsDefeated() && catholic.hq_kingdom.IsEnemy(k))
		{
			return RelationUtils.Stance.War;
		}
		if (!k.is_catholic && src_kingdom != null && src_kingdom.IsEnemy(k))
		{
			return RelationUtils.Stance.War;
		}
		return RelationUtils.Stance.Peace;
	}

	public override RelationUtils.Stance GetStance(Settlement s)
	{
		return s.GetStance(this);
	}

	public string Validate()
	{
		if (army == null)
		{
			return "no_army";
		}
		if (!army.IsValid())
		{
			return "defeated";
		}
		if (leader == null)
		{
			return "leader_dead";
		}
		if (leader != null && (leader.IsExile() || (leader.status as DeadStatus)?.reason == "exile"))
		{
			return "leader_exiled";
		}
		if (!leader.IsAlive())
		{
			return "leader_dead";
		}
		string text = ValidateTarget(target, helping_kingdom, new_crusade: false);
		if (text != "ok")
		{
			return text;
		}
		return "ok";
	}

	public static string ValidateTarget(Kingdom k, Kingdom helping_kingdom, bool new_crusade = true, bool forced = false)
	{
		if (k == null || k.IsDefeated())
		{
			return "target_defeated";
		}
		if (k.IsDominated() && k.occupiedRealms.Count == 0)
		{
			return "target_dominated";
		}
		if (k.is_catholic && !k.excommunicated)
		{
			return "catholic";
		}
		if (k.is_orthodox && new_crusade)
		{
			return "orthodox";
		}
		Catholic catholic = k.game.religions.catholic;
		if (k == catholic.head_kingdom || k == catholic.hq_kingdom)
		{
			return "catholic";
		}
		if (!new_crusade)
		{
			return "ok";
		}
		if (PickReason(k, helping_kingdom) == null)
		{
			return "no_reason";
		}
		if (!forced && !FindPossibleLeaders(k.game, k))
		{
			return "no_leaders";
		}
		return "ok";
	}

	public static bool IsOnCooldown(Game game, Kingdom helping_kingdom)
	{
		Catholic catholic = game.religions.catholic;
		Def def = game.defs.GetBase<Def>();
		if (def.cooldown <= 0f)
		{
			return false;
		}
		float num = game.time - catholic.last_crusade_end;
		if (catholic.crusades_count == 0 && num < def.initialCooldownCrusade)
		{
			if (helping_kingdom != null)
			{
				_ = helping_kingdom.is_player;
				return true;
			}
			return true;
		}
		if (helping_kingdom != null && helping_kingdom.is_player)
		{
			if (num < def.cooldownForPlayers)
			{
				return true;
			}
		}
		else if (num < def.cooldown)
		{
			return true;
		}
		return false;
	}

	public static string ValidateNew(Game game, Kingdom helping_kingdom, bool forced = false)
	{
		if (game.religions.catholic.crusade != null)
		{
			return "_active_crusade";
		}
		if (game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return "no_papacy";
		}
		if (LeadCrusadeOffer.Get(game) != null)
		{
			return "_active_lead_offer";
		}
		if (!forced && !FindPossibleLeaders(game, null, helping_kingdom))
		{
			return "_no_possible_leaders";
		}
		return "ok";
	}

	public static string PickReason(Kingdom target, Kingdom helping_kingdom)
	{
		if (target == null)
		{
			return null;
		}
		if (helping_kingdom != null && helping_kingdom.IsEnemy(target))
		{
			return "helping_kingdom";
		}
		Catholic catholic = target.game.religions.catholic;
		if (helping_kingdom == null && target == catholic.holy_lands_realm?.GetKingdom())
		{
			return "holy_lands";
		}
		return "attack_infidels";
	}

	public static bool FindPossibleLeaders(Game game, Kingdom target = null, Kingdom helping_kingdom = null, List<CrusaderCandidate> candidates = null, List<CrusadeKingdomCandidate> kingdoms = null)
	{
		Catholic catholic = game.religions.catholic;
		bool result = false;
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (!IsValidLeaderKingdom(kingdom, target, helping_kingdom) || kingdom.court == null)
			{
				continue;
			}
			float relationship = kingdom.GetRelationship(catholic.hq_kingdom);
			bool flag = false;
			for (int j = 0; j < kingdom.court.Count; j++)
			{
				Character character = kingdom.court[j];
				if (IsValidLeader(character, target, helping_kingdom))
				{
					if (candidates == null)
					{
						return true;
					}
					result = true;
					flag = true;
					float num = character.GetClassLevel();
					if (character.IsKingOrPrince() && !kingdom.is_player)
					{
						num *= 0.5f;
					}
					if (character.GetArmy()?.battle != null)
					{
						num *= 0.5f;
					}
					candidates.Add(new CrusaderCandidate
					{
						character = character,
						eval = num
					});
				}
			}
			if (flag)
			{
				kingdoms.Add(new CrusadeKingdomCandidate
				{
					kingdom = kingdom,
					eval = relationship
				});
			}
		}
		return result;
	}

	public static bool IsValidLeaderKingdom(Kingdom k, Kingdom target, Kingdom helping_kingdom)
	{
		if (k == null || k.IsDefeated())
		{
			return false;
		}
		if (!k.is_catholic || k.excommunicated)
		{
			return false;
		}
		if (!k.is_player && choose_only_player_leaders)
		{
			return false;
		}
		if (k.type != Kingdom.Type.Regular)
		{
			return false;
		}
		return true;
	}

	public static bool IsValidLeader(Character c, Kingdom target, Kingdom helping_kingdom)
	{
		if (c == null)
		{
			return false;
		}
		if (!c.IsAlive())
		{
			return false;
		}
		if (c.IsPrisoner())
		{
			return false;
		}
		if (!c.IsMarshal())
		{
			return false;
		}
		return true;
	}

	private static bool FindKC(int kid, List<CrusadeKingdomCandidate> lst, int N)
	{
		for (int i = 0; i < N; i++)
		{
			if (lst[i].kingdom.id == kid)
			{
				return true;
			}
		}
		return false;
	}

	public static Character PickLeader(Game game, Kingdom target, Kingdom helping_kingdom)
	{
		List<CrusaderCandidate> list = new List<CrusaderCandidate>();
		List<CrusadeKingdomCandidate> list2 = new List<CrusadeKingdomCandidate>();
		if (!FindPossibleLeaders(game, target, helping_kingdom, list, list2))
		{
			return null;
		}
		if (list.Count == 1)
		{
			return list[0].character;
		}
		Def def = game.defs.GetBase<Def>();
		int num = def.crusadeNumKingdoms;
		if (list2.Count > num)
		{
			list2.Sort((CrusadeKingdomCandidate a, CrusadeKingdomCandidate b) => b.eval.CompareTo(a.eval));
		}
		else
		{
			num = list2.Count;
		}
		bool flag = false;
		for (int num2 = 0; num2 < num; num2++)
		{
			if (list2[num2].kingdom.is_player)
			{
				flag = true;
				break;
			}
		}
		bool flag2 = (float)game.Random(0, 100) < def.crusadePlayerKingdomsPerc;
		if (!flag && flag2)
		{
			for (int num3 = num; num3 < list2.Count; num3++)
			{
				CrusadeKingdomCandidate value = list2[num3];
				if (value.kingdom.is_player)
				{
					list2[num++] = value;
				}
			}
		}
		List<CrusaderCandidate> list3 = new List<CrusaderCandidate>();
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			CrusaderCandidate item = list[num4];
			if (FindKC(item.character.kingdom_id, list2, num))
			{
				list3.Add(item);
			}
		}
		int num5 = 2;
		if (list3.Count > num5)
		{
			list3.Sort((CrusaderCandidate a, CrusaderCandidate b) => b.eval.CompareTo(a.eval));
		}
		else
		{
			num5 = list3.Count;
		}
		int index = game.Random(0, num5);
		return list3[index].character;
	}

	public static Army CreateArmy(Character leader)
	{
		Army army = leader.GetArmy();
		Kingdom kingdom = leader.game.GetKingdom("CrusadeFaction");
		if (kingdom == null)
		{
			return null;
		}
		Point position;
		if (army != null)
		{
			position = army.position;
			if (army.castle != null)
			{
				position = army.castle.GetRandomExitPoint();
			}
			if (!army.IsEmpty())
			{
				army.BecomeMercenary(army.GetKingdom().id);
			}
			else
			{
				leader.SetLocation(null);
				army.SetLeader(null);
				army.Destroy();
			}
		}
		else
		{
			position = leader.GetKingdom().GetCapital().castle.position;
		}
		Army army2 = army;
		army = new Army(leader.game, position, kingdom.id);
		army.SetLeader(leader);
		if (army2?.siege_equipment != null)
		{
			for (int num = army2.siege_equipment.Count - 1; num >= 0; num--)
			{
				InventoryItem inventoryItem = army2.siege_equipment[num];
				if (inventoryItem?.def != null)
				{
					army2.DelInvetoryItem(inventoryItem);
					if (inventoryItem.def.transfer_to_crusader)
					{
						army.AddInvetoryItem(inventoryItem, -1);
					}
				}
			}
		}
		return army;
	}

	public static void FillArmy(Crusade c, Army army)
	{
		AvailableUnits.Def def = army.game.defs.Find<AvailableUnits.Def>("CrusadeUnitSet");
		Unit.Def def2 = army.game.defs.Find<Unit.Def>("Militia_CE");
		if (army?.supplies != null)
		{
			army.SetSupplies(army.supplies.GetMax());
		}
		if (def == null)
		{
			army.FillWithRandomUnits();
			return;
		}
		if (army.units.Count < 1)
		{
			army.AddNoble();
		}
		int num = army.MaxUnits() + 1;
		int num2 = c.def.max_peasants - (int)(army.game.session_time.milliseconds / 3600000);
		while (army.units.Count < num)
		{
			Unit.Def def3;
			if (def2 != null && num2 > 0)
			{
				def3 = def2;
				num2--;
			}
			else
			{
				int index = army.game.Random(1, def.avaliable_types.Count);
				def3 = def.avaliable_types[index];
			}
			army.AddUnit(def3, -1, mercenary: false, send_state: false);
		}
		army.SendState<Army.UnitsState>();
		if (army.started)
		{
			army.NotifyListeners("units_changed");
		}
	}

	public void DisbandArmy(Army army)
	{
		army.Stop();
		if (army.battle != null)
		{
			army.battle.Leave(army);
		}
		Character character = army.leader;
		if (!army.IsValid() || character == null)
		{
			return;
		}
		if (army.castle != null)
		{
			army.LeaveCastle(army.castle.GetRandomExitPoint());
		}
		if (character.GetKingdom().IsDefeated())
		{
			end_outcome = "go_loyalist";
			character.TurnIntoRebel("LeaderLoyalists", "CrusadeEndSpawnCondition", character.GetKingdom());
			return;
		}
		army.SetKingdom(character.kingdom_id);
		int num = army.game.Random(this.def.minTroopsOnGoHome, this.def.maxTroopsOnGoHome + 1) + 1;
		character.RefreshTags();
		character.UpdateAutomaticStatuses();
		if (army.units.Count <= num)
		{
			return;
		}
		if (crusade_successful)
		{
			army.ClearUnitsOverMax();
			return;
		}
		Rebel.Def def = game.defs.Get<Rebel.Def>("Rebels");
		Kingdom factionKingdom = FactionUtils.GetFactionKingdom(game, def.kingdom_key);
		RebelSpawnCondition.Def cond_def = game.defs.Get<RebelSpawnCondition.Def>("CrusadeEndSpawnCondition");
		Rebel rebel = new Rebel(game, factionKingdom.id, factionKingdom.id, army.realm_in, cond_def, def);
		rebel.character = CharacterFactory.CreateRebel(game, factionKingdom.id, character.original_kingdom_id, def.GetRandomLeaderClass(game));
		Army army2 = new Army(position: (!army.realm_in.IsSeaRealm()) ? ((PPos)Rebel.GetRandomRealmPoint(army.realm_in, rebel.def)) : ((PPos)Rebel.GetRandomRealmPoint(army.realm_in, rebel.def, army.position)), game: game, kingdom_id: rebel.character.kingdom_id);
		army2.units.Clear();
		army2.AddNoble();
		while (army.units.Count > num)
		{
			Unit unit = army.units[num];
			army.DelUnit(unit, army.units.Count == num + 1);
			army2.AddUnit(unit, -1, army.units.Count == num);
		}
		army2.SetLeader(rebel.character);
		army2.rebel = rebel;
		rebel.army = army2;
		rebel.UpdateAgenda();
		rebel.UpdateInBatch(game.update_5sec);
		rebel.SetInitalRisk(0f);
		if (character.skills != null)
		{
			for (int i = 0; i < character.skills.Count; i++)
			{
				Skill skill = character.skills[i];
				if (skill != null)
				{
					if ((rebel.character.skills != null && rebel.character.skills.Count >= character.skills.Count * this.def.skillCopyPercentage / 100) || !rebel.character.CanLearnNewSkills())
					{
						break;
					}
					if (game.Random(0, 100) < this.def.skillCopyPercentage)
					{
						rebel.character.AddSkill(skill.def);
					}
				}
			}
		}
		else
		{
			Game.Log($"Cusader {character} dont have skills to copy", Game.LogType.Message);
		}
	}

	public bool IsRecievingKingdomTooFar(Kingdom giveToKingdom)
	{
		int num = game.KingdomAndRealmDistance(giveToKingdom.id, army.realm_in.id, def.giveToKingdomMaxDistance + 1);
		if (num < 0 || num > def.giveToKingdomMaxDistance)
		{
			return true;
		}
		return false;
	}

	private bool CanGiveRealmToLeaderKingdom()
	{
		if (src_kingdom.IsDefeated())
		{
			return false;
		}
		if (src_kingdom == game.religions.catholic.hq_kingdom && game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return false;
		}
		if (IsRecievingKingdomTooFar(src_kingdom))
		{
			return false;
		}
		if (src_kingdom.IsEnemy(this))
		{
			return false;
		}
		return true;
	}

	private bool CanGiveRealmToPapacy()
	{
		if (game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return false;
		}
		if (IsRecievingKingdomTooFar(game.religions.catholic.hq_kingdom))
		{
			return false;
		}
		if (game.religions.catholic.hq_kingdom.IsEnemy(this))
		{
			return false;
		}
		return true;
	}

	private bool CanGiveRealmToHelpingKingdom()
	{
		if (helping_kingdom == null || helping_kingdom.IsDefeated())
		{
			return false;
		}
		if (helping_kingdom == game.religions.catholic.hq_kingdom && game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return false;
		}
		if (IsRecievingKingdomTooFar(helping_kingdom))
		{
			return false;
		}
		if (helping_kingdom.IsEnemy(this))
		{
			return false;
		}
		return true;
	}

	private bool CanGiveRealmToLastEstablishedKingdom()
	{
		if (last_established_kingdom == null || last_established_kingdom.IsDefeated())
		{
			return false;
		}
		if (IsRecievingKingdomTooFar(last_established_kingdom))
		{
			return false;
		}
		if (last_established_kingdom.IsEnemy(this))
		{
			return false;
		}
		return true;
	}

	public void AlterRealmCapturedOutcome(OutcomeDef outcome, IVars vars)
	{
		switch (outcome.key)
		{
		case "give_to_kingdom":
			if (!vars.GetVar("can_give_to_leader_kingdom") && !vars.GetVar("can_give_to_papacy") && !vars.GetVar("can_give_to_helping_kingdom") && !vars.GetVar("can_give_to_last_established_kingdom"))
			{
				outcome.chance = 0f;
			}
			break;
		case "give_to_leader_kingdom":
		{
			if (!vars.GetVar("can_give_to_leader_kingdom"))
			{
				outcome.chance = 0f;
				break;
			}
			DT.Field field4 = def.capture_realm_outcomes.field.FindChild("give_to_leader_kingdom");
			if (field4 != null)
			{
				if (leader.IsKing())
				{
					outcome.chance = field4.GetFloat("king");
				}
				else if (leader.IsPrince())
				{
					outcome.chance = field4.GetFloat("prince");
				}
			}
			break;
		}
		case "give_to_papacy":
			if (!vars.GetVar("can_give_to_papacy"))
			{
				outcome.chance = 0f;
			}
			else if (leader.IsKing() && (bool)vars.GetVar("can_give_to_leader_kingdom"))
			{
				outcome.chance = 0f;
			}
			break;
		case "give_to_helping_kingdom":
			if (!vars.GetVar("can_give_to_helping_kingdom"))
			{
				outcome.chance = 0f;
			}
			else if (leader.IsKing() && (bool)vars.GetVar("can_give_to_leader_kingdom"))
			{
				outcome.chance = 0f;
			}
			break;
		case "give_to_established_kingdom":
			if (!vars.GetVar("can_give_to_last_established_kingdom"))
			{
				outcome.chance = 0f;
			}
			else if (leader.IsKing() && (bool)vars.GetVar("can_give_to_leader_kingdom"))
			{
				outcome.chance = 0f;
			}
			break;
		case "give_to_leader_kingdom_ignore_checks":
		{
			DT.Field field2 = def.capture_realm_outcomes.field.FindChild("establish_new_kingdom")?.FindChild("give_to_leader_kingdom_ignore_checks");
			if (game.GetIndependenceNewKindom(new List<Realm> { last_captured_realm }) == null)
			{
				outcome.chance = field2?.GetFloat("cant_establish_new_kingdom") ?? 100f;
			}
			break;
		}
		case "vasal_of_leader_kingdom":
		{
			if (src_kingdom.IsDefeated() || src_kingdom.IsVassal())
			{
				outcome.chance = 0f;
				break;
			}
			DT.Field field5 = def.capture_realm_outcomes.field.FindChild("establish_new_kingdom")?.FindChild("vasal_of_leader_kingdom");
			if (field5 != null)
			{
				if (leader.IsKing())
				{
					outcome.chance = field5.GetFloat("king");
				}
				else if (leader.IsPrince())
				{
					outcome.chance = field5.GetFloat("prince");
				}
			}
			break;
		}
		case "vasal_of_papacy":
		{
			if (leader.IsKing() && !src_kingdom.IsVassal() && !src_kingdom.IsDefeated())
			{
				outcome.chance = 0f;
			}
			Kingdom hq_kingdom = game.religions.catholic.hq_kingdom;
			if (hq_kingdom.IsDefeated() || hq_kingdom.IsVassal())
			{
				outcome.chance = 0f;
			}
			break;
		}
		case "independent":
		{
			DT.Field field3 = def.capture_realm_outcomes.field.FindChild("establish_new_kingdom")?.FindChild("independent");
			if (src_kingdom.IsVassal() && game.religions.catholic.hq_kingdom.IsVassal())
			{
				outcome.chance = field3.GetFloat("leader_and_papacy_are_vassals_of_ther_kingdoms");
			}
			break;
		}
		case "sack":
		{
			DT.Field field = def.capture_realm_outcomes.field.FindChild("sack");
			if (field != null && crusade_successful)
			{
				outcome.chance = field.GetFloat("crusade_successful");
			}
			break;
		}
		}
	}

	private Vars GetCastleCaptureVars(Realm r)
	{
		Vars vars = new Vars(this);
		vars.Set("can_give_to_leader_kingdom", CanGiveRealmToLeaderKingdom());
		vars.Set("can_give_to_papacy", CanGiveRealmToPapacy());
		vars.Set("can_give_to_helping_kingdom", CanGiveRealmToHelpingKingdom());
		vars.Set("can_give_to_last_established_kingdom", CanGiveRealmToLastEstablishedKingdom());
		return vars;
	}

	public void CastleCaptured(Realm r)
	{
		if ((r.kingdom_id == target.id && target.realms.Count == 1) || (reason == "holy_lands" && r == game.religions.catholic.holy_lands_realm))
		{
			crusade_successful = true;
		}
		if (def.capture_realm_outcomes == null)
		{
			r.SetKingdom(src_kingdom.id);
		}
		last_captured_realm = r;
		SendState<MainState>();
		Vars castleCaptureVars = GetCastleCaptureVars(r);
		capture_realm_outcomes = def.capture_realm_outcomes.DecideOutcomes(game, castleCaptureVars, force_capture_realm_outcomes, AlterRealmCapturedOutcome);
		capture_realm_unique_outcomes = OutcomeDef.UniqueOutcomes(capture_realm_outcomes);
		ApplyCaptureRealmOutcomes();
		game.religions.NotifyListeners("crusade_captured_realm", this);
		force_capture_realm_outcomes = null;
		capture_realm_outcomes = null;
		capture_realm_unique_outcomes = null;
	}

	public void FreeAllKeeps()
	{
		if (occupiedKeeps == null || occupiedKeeps.Count <= 0)
		{
			return;
		}
		for (int num = occupiedKeeps.Count - 1; num >= 0; num--)
		{
			if (occupiedKeeps[num].keep_effects != null)
			{
				occupiedKeeps[num].keep_effects.SetOccupied(null);
			}
		}
	}

	private void ApplyCaptureRealmOutcomes()
	{
		for (int i = 0; i < capture_realm_unique_outcomes.Count; i++)
		{
			OutcomeDef outcome = capture_realm_unique_outcomes[i];
			ApplyCaptureRealmOutcome(outcome);
		}
	}

	public void GiveToKingdom(Realm r, Kingdom k)
	{
		r.SetKingdom(k.id);
		game.BroadcastRadioEvent("CrusadeTargetProvinceJoinedKingdomRadioMessage", new Vars(this));
		k.FireEvent("crusade_give_realm_to_kingdom", this);
	}

	private bool ApplyCaptureRealmOutcome(OutcomeDef outcome)
	{
		Realm realm = last_captured_realm;
		if (realm == null)
		{
			return false;
		}
		switch (outcome.key)
		{
		case "sack":
			Sack(realm);
			return true;
		case "give_to_leader_kingdom":
		case "give_to_leader_kingdom_ignore_checks":
			GiveToKingdom(realm, src_kingdom);
			return true;
		case "give_to_papacy":
			GiveToKingdom(realm, game.religions.catholic.hq_kingdom);
			return true;
		case "give_to_helping_kingdom":
			GiveToKingdom(realm, helping_kingdom);
			return true;
		case "give_to_established_kingdom":
			GiveToKingdom(realm, last_established_kingdom);
			return true;
		case "vasal_of_leader_kingdom":
			EstablishNewKingdom(realm, src_kingdom);
			return true;
		case "independent":
			EstablishNewKingdom(realm, null);
			return true;
		case "vasal_of_papacy":
			EstablishNewKingdom(realm, religion.hq_kingdom);
			return true;
		default:
			if (outcome.Apply(game, this))
			{
				return true;
			}
			return false;
		}
	}

	public bool IsConnectedToCrusade(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (k == src_kingdom)
		{
			return true;
		}
		if (k == helping_kingdom)
		{
			return true;
		}
		if (k == k.game.religions.catholic.hq_kingdom)
		{
			return true;
		}
		if (k == last_established_kingdom)
		{
			return true;
		}
		if (k == target)
		{
			return true;
		}
		return false;
	}

	private void Sack(Realm r)
	{
		r.castle.Sack(army);
	}

	private bool EstablishNewKingdom(Realm r, Kingdom vassal_of)
	{
		leader.GetArmy();
		Kingdom kingdom = leader.GetKingdom();
		List<Realm> realms = new List<Realm> { r };
		Kingdom independenceNewKindom = game.GetIndependenceNewKindom(realms);
		if (independenceNewKindom == null)
		{
			return false;
		}
		bool flag = independenceNewKindom.IsDefeated();
		independenceNewKindom.DeclareIndependenceOrJoin(realms, null, null, null, religion);
		if (independenceNewKindom.religion != religion)
		{
			independenceNewKindom.SetReligion(religion);
		}
		independenceNewKindom.AddRelationModifier(religion.hq_kingdom, "rel_new_crusade_kingdom", this);
		independenceNewKindom.AddRelationModifier(kingdom, "rel_new_crusade_kingdom", this);
		if (vassal_of != null && !vassal_of.IsDefeated() && !vassal_of.IsVassal() && flag)
		{
			vassal_of.AddVassalState(independenceNewKindom);
		}
		if (!target.IsDefeated() && War.CanStart(target, independenceNewKindom))
		{
			independenceNewKindom.StartWarWith(target);
		}
		last_established_kingdom = independenceNewKindom;
		independenceNewKindom.FireEvent("crusade_establish_new_kingdom", this);
		SendState<MainState>();
		return true;
	}

	private void EndOutcome()
	{
		end_outcomes = null;
		end_unique_outcomes = null;
		Catholic catholic = religion;
		if (catholic.crusade.army.IsValid() && catholic.crusade.army.leader != null && !(end_reason == "new_kingdom"))
		{
			if (leader.IsExile() || (leader.status as DeadStatus)?.reason == "exile")
			{
				crusader_exiled_outcomes = def.crusader_exiled_outcomes.DecideOutcomes(game, this);
				crusader_exiled_unique_outcomes = OutcomeDef.UniqueOutcomes(crusader_exiled_outcomes);
				ApplyCrusaderExiledOutcomes();
			}
			else if (!leader.IsAlive())
			{
				crusader_death_outcomes = def.crusader_death_outcomes.DecideOutcomes(game, this);
				crusader_death_unique_outcomes = OutcomeDef.UniqueOutcomes(crusader_death_outcomes);
				ApplyCrusaderDeathOutcomes();
			}
			else
			{
				end_outcomes = def.end_outcomes.DecideOutcomes(game, this, force_end_outcomes, AlterEndOutcome);
				end_unique_outcomes = OutcomeDef.UniqueOutcomes(end_outcomes);
				ApplyEndOutcomes();
			}
		}
	}

	private void ApplyCrusaderExiledOutcomes()
	{
		for (int i = 0; i < crusader_exiled_unique_outcomes.Count; i++)
		{
			OutcomeDef outcome = crusader_exiled_unique_outcomes[i];
			NoLeaderApplyCrusaderOutcome(outcome);
		}
	}

	private void ApplyCrusaderDeathOutcomes()
	{
		for (int i = 0; i < crusader_death_unique_outcomes.Count; i++)
		{
			OutcomeDef outcome = crusader_death_unique_outcomes[i];
			NoLeaderApplyCrusaderOutcome(outcome);
		}
	}

	private bool NoLeaderApplyCrusaderOutcome(OutcomeDef outcome)
	{
		_ = religion;
		switch (outcome.key)
		{
		case "army_becomes_merc_camp":
		case "default":
			NoLeaderCrusaderArmyMercenary();
			return true;
		case "army_goes_rogue":
			NoLeaderCrusaderArmyRogue();
			return true;
		default:
			if (outcome.Apply(game, this))
			{
				return true;
			}
			return false;
		}
	}

	private void ApplyEndOutcomes()
	{
		for (int i = 0; i < end_outcomes.Count; i++)
		{
			OutcomeDef outcome = end_unique_outcomes[i];
			ApplyEndOutcome(outcome);
		}
	}

	public void AlterEndOutcome(OutcomeDef outcome, IVars vars)
	{
		string key = outcome.key;
		if (!(key == "return"))
		{
			if (key == "rogue" && leader.IsKing() && leader.GetKingdom() != null && !leader.GetKingdom().IsDefeated())
			{
				outcome.chance = 0f;
			}
			return;
		}
		DT.Field field = def.end_outcomes.field.FindChild("return");
		if (field != null)
		{
			if (crusade_successful)
			{
				outcome.chance = field.GetFloat("crusade_successful");
			}
			else if (leader.GetKingdom().IsDefeated())
			{
				outcome.chance = field.GetFloat("leader_kingdom_destroyed");
			}
			else if (religion.hq_kingdom.IsDefeated())
			{
				outcome.chance = field.GetFloat("papacy_destroyed");
			}
			else if (leader.IsKing())
			{
				outcome.chance = field.GetFloat("king");
			}
			else if (leader.IsPrince())
			{
				outcome.chance = field.GetFloat("prince");
			}
		}
	}

	private bool ApplyEndOutcome(OutcomeDef outcome)
	{
		_ = religion;
		switch (outcome.key)
		{
		case "return":
		case "default":
			GoHome();
			return true;
		case "rogue":
			GoRogue();
			return true;
		default:
			if (outcome.Apply(game, this))
			{
				return true;
			}
			return false;
		}
	}

	private void NoLeaderCrusaderArmyMercenary()
	{
		Army army = this.army;
		Character character = leader;
		leader = null;
		this.army = null;
		end_outcome = "no_leader_mercenary";
		if (army.battle != null)
		{
			army.battle.Leave(army, check_victory: true);
		}
		if (army.castle != null)
		{
			army.LeaveCastle(army.castle.GetRandomExitPoint());
		}
		army.BecomeMercenary(0);
		character?.NotifyListeners("crusader_status_changed");
		army?.NotifyListeners("crusader_status_changed");
	}

	private void NoLeaderCrusaderArmyRogue()
	{
		Army army = this.army;
		Character character = leader;
		leader = null;
		this.army = null;
		end_outcome = "no_leader_rogue";
		if (army.battle != null)
		{
			army.battle.Leave(army, check_victory: true);
		}
		if (army.castle != null)
		{
			army.LeaveCastle(army.castle.GetRandomExitPoint());
		}
		Character c = CharacterFactory.CreateCharacter(game, army.GetKingdom().id, "Marshal");
		army.SetLeader(c);
		army.leader.TurnIntoRebel("LeaderRebels", "CrusadeEndSpawnCondition");
		character?.NotifyListeners("crusader_status_changed");
		army?.NotifyListeners("crusader_status_changed");
	}

	private void GoHome()
	{
		Army army = this.army;
		Character character = leader;
		leader = null;
		this.army = null;
		end_outcome = "go_home";
		DisbandArmy(army);
		character?.NotifyListeners("crusader_status_changed");
		army?.NotifyListeners("crusader_status_changed");
	}

	private void GoRogue()
	{
		Character character = leader;
		Army obj = army;
		leader = null;
		army = null;
		if (force_go_rogue_params != null)
		{
			end_outcome = ((force_go_rogue_params.loyalTo != null) ? "go_loyalist" : "go_rogue");
			character.TurnIntoRebel(force_go_rogue_params.type, force_go_rogue_params.spawn_condition, force_go_rogue_params.loyalTo, force_go_rogue_params.realm);
		}
		else
		{
			end_outcome = "go_rogue";
			character.TurnIntoRebel("LeaderRebels", "CrusadeEndSpawnCondition");
		}
		character?.NotifyListeners("crusader_status_changed");
		obj?.NotifyListeners("crusader_status_changed");
	}

	private void Think()
	{
		if (army != null && army.rebel == null)
		{
			if (army.battle != null)
			{
				KingdomAI.ThinkAssaultSiege(army);
			}
			else if (!ThinkAttack() && !ThinkCamp() && !ThinkMove())
			{
				ThinkUnstuck();
			}
		}
	}

	private bool ThinkUnstuck()
	{
		if (army.realm_in != null)
		{
			return false;
		}
		if (army.movement.IsMoving())
		{
			return false;
		}
		if (target == null)
		{
			return false;
		}
		float num = float.MaxValue;
		Castle castle = null;
		for (int i = 0; i < target.realms.Count; i++)
		{
			Castle castle2 = target.realms[i]?.castle;
			if (castle2 != null)
			{
				float num2 = castle2.position.SqrDist(army.position);
				if (num2 < num)
				{
					num = num2;
					castle = castle2;
				}
			}
		}
		if (castle == null)
		{
			return false;
		}
		army.MoveTo(castle);
		return true;
	}

	private void ThinkAttack(Realm realm, ref float d2nearest, ref MapObject new_tgt)
	{
		realm.controller?.GetKingdom();
		if (realm.castle != null && ValidTarget(this.army, realm.castle))
		{
			float num = this.army.position.SqrDist(realm.castle.position);
			if (num < d2nearest)
			{
				new_tgt = realm.castle;
				d2nearest = num;
			}
		}
		for (int i = 0; i < realm.armies.Count; i++)
		{
			Army army = realm.armies[i];
			if (ValidTarget(this.army, army))
			{
				float num2 = this.army.position.SqrDist(army.position);
				if (!(num2 >= d2nearest))
				{
					new_tgt = army;
					d2nearest = num2;
				}
			}
		}
	}

	private bool ValidTarget(Army army, MapObject obj)
	{
		if (obj is Army { realm_in: var realm_in } army2)
		{
			if (realm_in == null)
			{
				return false;
			}
			Kingdom kingdom = realm_in.controller?.GetKingdom();
			if (kingdom == null)
			{
				return false;
			}
			if (army2 == army)
			{
				return false;
			}
			if (army2.castle != null)
			{
				return false;
			}
			if (army2.battle != null && !army2.battle.CanJoin(army))
			{
				return false;
			}
			if (!IsEnemy(army2))
			{
				return false;
			}
			if (army2.rebel != null && army2.battle == null && kingdom != src_kingdom && kingdom != helping_kingdom)
			{
				return false;
			}
			return true;
		}
		if (obj is Castle castle)
		{
			if (castle.battle != null && !castle.battle.CanJoin(army))
			{
				return false;
			}
			if (!IsEnemy(castle))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private bool ThinkAttack()
	{
		Realm realm_in = army.realm_in;
		if (realm_in == null)
		{
			return false;
		}
		Kingdom kingdom = realm_in.controller?.GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		MapObject mapObject = null;
		Path path = army.movement.pf_path ?? army.movement.path;
		if (path != null)
		{
			mapObject = army.movement.ResolveOriginalDestination(path.dst_obj);
		}
		MapObject new_tgt = null;
		float d2nearest = float.MaxValue;
		if (mapObject != null && ValidTarget(army, mapObject))
		{
			d2nearest = army.position.SqrDist(mapObject.position);
			new_tgt = mapObject;
		}
		ThinkAttack(realm_in, ref d2nearest, ref new_tgt);
		for (int i = 0; i < realm_in.neighbors.Count; i++)
		{
			ThinkAttack(realm_in.neighbors[i], ref d2nearest, ref new_tgt);
		}
		if (new_tgt == null && kingdom == target && realm_in.castle.IsEnemy(army) && (realm_in != last_captured_realm || !realm_in.castle.sacked || kingdom.realms.Count == 1))
		{
			new_tgt = realm_in.castle;
		}
		if (new_tgt == null)
		{
			if (mapObject != null && !mapObject.IsOwnStance(target))
			{
				army.Stop();
			}
			return false;
		}
		if (new_tgt == mapObject)
		{
			return true;
		}
		army.MoveTo(new_tgt);
		return true;
	}

	private bool ThinkCamp()
	{
		if (leader.cur_action is CampArmyAction)
		{
			if (game.time < camp_time)
			{
				return true;
			}
			camp_time = Time.Zero;
		}
		if (camp_time == Time.Zero)
		{
			float num = 0f;
			float num2 = 0f;
			DT.Field field = game.dt.Find("Catholic.crusade_camp_interval");
			if (field != null)
			{
				num = field.Float(0);
				num2 = field.Float(1);
			}
			if (num <= 0f)
			{
				return false;
			}
			float num3 = ((num2 > num) ? game.Random(num, num2) : num);
			camp_time = game.time + num3;
			return false;
		}
		if (game.time >= camp_time)
		{
			float num4 = 0f;
			float num5 = 0f;
			DT.Field field2 = game.dt.Find("Catholic.crusade_camp_duration");
			if (field2 != null)
			{
				num4 = field2.Float(0);
				num5 = field2.Float(1);
			}
			if (num4 <= 0f)
			{
				return false;
			}
			float num6 = ((num5 > num4) ? game.Random(num4, num5) : num4);
			camp_time = game.time + num6;
			leader.actions?.Find("CampArmyAction")?.Execute(null);
			return true;
		}
		return false;
	}

	private bool ThinkMove()
	{
		Path path = army.movement.pf_path ?? army.movement.path;
		if (path != null)
		{
			int rid = game.RealmIDAt(path.dst_pt);
			if (game.GetRealm(rid)?.GetKingdom() == target)
			{
				return false;
			}
		}
		Realm realm = DecideNextRealm();
		if (realm == null)
		{
			return false;
		}
		army.MoveTo(realm.castle);
		return true;
	}

	public void DecideNextRealmCallback(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
	{
		if (r.IsOwnStance(target) && (r != last_captured_realm || !r.castle.sacked || r.GetKingdom().realms.Count <= 1))
		{
			stop = true;
		}
	}

	private Realm DecideNextRealm()
	{
		if (reason == "holy_lands" && religion.holy_lands_realm != null && army.IsEnemy(religion.holy_lands_realm))
		{
			return religion.holy_lands_realm;
		}
		return game.RealmWave(army.realm_in, 0, DecideNextRealmCallback);
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("target", target?.Name);
		dump.Append("helping_kingdom", helping_kingdom?.Name);
		dump.Append("leader", leader?.ToString());
		dump.Append("reason", reason);
		dump.Append("wealth", wealth);
	}
}
