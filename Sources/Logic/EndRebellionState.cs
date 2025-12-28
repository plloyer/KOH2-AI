using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Rebellion)]
public class Rebellion : Object
{
	[Serialization.State(11)]
	public class DefsState : Serialization.ObjectState
	{
		private string def_id = "";

		private string rebel_def_id = "";

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
			Rebellion rebellion = obj as Rebellion;
			def_id = rebellion.def.id;
			rebel_def_id = rebellion.rebel_def.id;
			agenda_def_id = rebellion.agenda.def.id;
			if (rebellion.condition_def != null)
			{
				cond_def_id = rebellion.condition_def.id;
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
			ser.WriteStr(rebel_def_id, "rebel_def_id");
			ser.WriteStr(cond_def_id, "cond_def_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def_id = ser.ReadStr("def_id");
			agenda_def_id = ser.ReadStr("agenda_def_id");
			rebel_def_id = ser.ReadStr("rebel_def_id");
			cond_def_id = ser.ReadStr("cond_def_id");
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			rebellion.def = rebellion.game.defs.Get<Def>(def_id);
			rebellion.agenda = new RebelAgenda(rebellion.game.defs.Get<RebelAgenda.Def>(agenda_def_id));
			rebellion.rebel_def = rebellion.game.defs.Get<Rebel.Def>(rebel_def_id);
			if (cond_def_id != "")
			{
				rebellion.condition_def = rebellion.game.defs.Get<RebelSpawnCondition.Def>(cond_def_id);
			}
			else
			{
				rebellion.condition_def = null;
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
			Rebellion rebellion = obj as Rebellion;
			kingdom_id = rebellion.kingdom_id;
			loyal_to = rebellion.loyal_to;
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
			Rebellion obj2 = obj as Rebellion;
			obj2.kingdom_id = kingdom_id;
			obj2.loyal_to = loyal_to;
		}
	}

	[Serialization.State(13)]
	public class OriginState : Serialization.ObjectState
	{
		private int origin_realm_id;

		private int spawn_kingdom_id;

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
			Rebellion rebellion = obj as Rebellion;
			origin_realm_id = rebellion.origin_realm_id;
			spawn_kingdom_id = rebellion.spawn_kingdom_id;
			religion = ((rebellion.religion == null) ? "" : rebellion.religion.name);
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(origin_realm_id, "origin_realm_id");
			ser.Write7BitUInt(spawn_kingdom_id, "spawn_kingdom_id");
			ser.WriteStr(religion, "religion");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			origin_realm_id = ser.Read7BitSigned("origin_realm_id");
			spawn_kingdom_id = ser.Read7BitUInt("spawn_kingdom_id");
			religion = ser.ReadStr("religion");
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			rebellion.origin_realm_id = origin_realm_id;
			rebellion.spawn_kingdom_id = spawn_kingdom_id;
			rebellion.religion = obj.game.religions.Get(religion);
			rebellion.RefreshZoneRealms();
			obj.game.GetRealm(origin_realm_id).NotifyListeners("rebellion_started", rebellion);
			obj.game.GetKingdom(rebellion.loyal_to)?.NotifyListeners("own_rebellion_started", rebellion);
		}
	}

	[Serialization.State(14)]
	public class RebelsState : Serialization.ObjectState
	{
		public List<NID> rebel_nids = new List<NID>();

		public static RebelsState Create()
		{
			return new RebelsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			List<Rebel> rebels = (obj as Rebellion).rebels;
			for (int i = 0; i < rebels.Count; i++)
			{
				rebel_nids.Add(rebels[i]);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(rebel_nids.Count, "rebel_count");
			for (int i = 0; i < rebel_nids.Count; i++)
			{
				ser.WriteNID<Rebel>(rebel_nids[i], "rebels_nids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("rebel_count");
			for (int i = 0; i < num; i++)
			{
				rebel_nids.Add(ser.ReadNID<Rebel>("rebels_nids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			rebellion.rebels.Clear();
			for (int i = 0; i < rebel_nids.Count; i++)
			{
				Rebel rebel = rebel_nids[i].Get<Rebel>(rebellion.game);
				rebellion.rebels.Add(rebel);
				rebel.rebellion = rebellion;
			}
			rebellion.NotifyListeners("rebels_changed");
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
			Rebel leader = (obj as Rebellion).leader;
			leader_nid = leader;
			return leader != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Rebel>(leader_nid, "leader_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			leader_nid = ser.ReadNID<Rebel>("leader_nid");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Rebellion).SetLeader(leader_nid.Get<Rebel>(obj.game), send_state: false);
		}
	}

	[Serialization.State(16)]
	public class EnemiesState : Serialization.ObjectState
	{
		private List<NID> enemies = new List<NID>();

		public static EnemiesState Create()
		{
			return new EnemiesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebellion).enemies.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Rebellion obj2 = obj as Rebellion;
			if (enemies.Count > 0)
			{
				enemies.Clear();
			}
			foreach (Kingdom enemy in obj2.enemies)
			{
				enemies.Add(enemy);
			}
			return enemies.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = enemies.Count;
			ser.Write7BitUInt(count, "enemy_count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteNID<Kingdom>(enemies[i], "enemy_kingdom_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (enemies.Count > 0)
			{
				enemies.Clear();
			}
			int num = ser.Read7BitUInt("enemy_count");
			for (int i = 0; i < num; i++)
			{
				enemies.Add(ser.ReadNID<Kingdom>("enemy_kingdom_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			if (rebellion.enemies.Count > 0)
			{
				rebellion.enemies.Clear();
			}
			for (int i = 0; i < enemies.Count; i++)
			{
				rebellion.AddEnemy(enemies[i].Get<Kingdom>(rebellion.game), send_state: false);
			}
		}
	}

	[Serialization.State(17)]
	public class AlliesState : Serialization.ObjectState
	{
		private List<NID> allies = new List<NID>();

		public static AlliesState Create()
		{
			return new AlliesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebellion).allies.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Rebellion obj2 = obj as Rebellion;
			if (allies.Count > 0)
			{
				allies.Clear();
			}
			foreach (Kingdom ally in obj2.allies)
			{
				allies.Add(ally);
			}
			return allies.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = allies.Count;
			ser.Write7BitUInt(count, "ally_count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteNID<Kingdom>(allies[i], "ally_kingdom_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			if (allies.Count > 0)
			{
				allies.Clear();
			}
			int num = ser.Read7BitUInt("ally_count");
			for (int i = 0; i < num; i++)
			{
				allies.Add(ser.ReadNID<Kingdom>("ally_kingdom_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			if (rebellion.allies.Count > 0)
			{
				rebellion.allies.Clear();
			}
			for (int i = 0; i < allies.Count; i++)
			{
				rebellion.AddAlly(allies[i].Get<Kingdom>(rebellion.game), send_state: false);
			}
		}
	}

	[Serialization.State(18)]
	public class ControlledRealmsState : Serialization.ObjectState
	{
		public List<NID> realm_nids = new List<NID>();

		public static ControlledRealmsState Create()
		{
			return new ControlledRealmsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			List<Realm> occupiedRealms = (obj as Rebellion).occupiedRealms;
			for (int i = 0; i < occupiedRealms.Count; i++)
			{
				realm_nids.Add(occupiedRealms[i]);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(realm_nids.Count, "realm_count");
			for (int i = 0; i < realm_nids.Count; i++)
			{
				ser.WriteNID<Realm>(realm_nids[i], "realm_nid_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("realm_count");
			for (int i = 0; i < num; i++)
			{
				realm_nids.Add(ser.ReadNID<Realm>("realm_nid_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			rebellion.occupiedRealms.Clear();
			for (int i = 0; i < realm_nids.Count; i++)
			{
				rebellion.occupiedRealms.Add(realm_nids[i].Get<Realm>(rebellion.game));
			}
			rebellion.RefreshZoneRealms();
		}
	}

	[Serialization.State(19)]
	public class EndRebellionState : Serialization.ObjectState
	{
		public NID defeatedBy;

		public NID defeatedByKingdom;

		public static EndRebellionState Create()
		{
			return new EndRebellionState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			defeatedBy = rebellion.defeatedBy;
			defeatedByKingdom = rebellion.defeatedByKingdom;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(defeatedBy, "defeatedBy");
			ser.WriteNID<Kingdom>(defeatedByKingdom, "defeatedByKingdom");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			defeatedBy = ser.ReadNID<Character>("defeatedBy");
			defeatedByKingdom = ser.ReadNID<Kingdom>("defeatedByKingdom");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Rebellion rebellion)
			{
				rebellion.defeatedBy = defeatedBy.Get<Character>(rebellion.game);
				rebellion.defeatedByKingdom = defeatedByKingdom.Get<Kingdom>(rebellion.game);
				rebellion.marked_for_destroy = true;
				rebellion.NotifyAffectedKingdoms("rebellion_ended", rebellion);
				rebellion.ClearZone();
			}
		}
	}

	[Serialization.State(20)]
	public class FamousState : Serialization.ObjectState
	{
		private bool famous;

		public static FamousState Create()
		{
			return new FamousState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebellion).famous;
		}

		public override bool InitFrom(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			famous = rebellion.famous;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(famous, "famous");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			famous = ser.ReadBool("famous");
		}

		public override void ApplyTo(Object obj)
		{
			Rebellion obj2 = obj as Rebellion;
			obj2.famous = famous;
			obj2.RefreshZoneRealms();
			obj2.NotifyAffectedKingdoms("rebellion_famous_state_changed");
			obj2.leader.character.NotifyListeners("rebellion_famous_state_changed");
		}
	}

	[Serialization.State(21)]
	public class WealthState : Serialization.ObjectState
	{
		public float wealth;

		public static WealthState Create()
		{
			return new WealthState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Rebellion).wealth != 0f;
		}

		public override bool InitFrom(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			wealth = rebellion.wealth;
			return wealth != 0f;
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
			if (obj is Rebellion rebellion)
			{
				rebellion.wealth = wealth;
				rebellion.NotifyListeners("wealth_changed");
			}
		}
	}

	[Serialization.State(22)]
	public class StartTimeState : Serialization.ObjectState
	{
		public float delta;

		public static StartTimeState Create()
		{
			return new StartTimeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Rebellion rebellion = obj as Rebellion;
			delta = rebellion.game.time - rebellion.start_time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(delta, "delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			delta = ser.ReadFloat("delta");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Rebellion rebellion)
			{
				rebellion.start_time = rebellion.game.time - delta;
			}
		}
	}

	public class Def : Logic.Def
	{
		public string name;

		public int max_rebels = 8;

		public float time_disband_after_no_zone = 3600f;

		public float become_famous_chance;

		public float plunder_wealth_threshold_lower;

		public float plunder_wealth_threshold_upper;

		public float plunder_chance_perc_lower_threshold;

		public float plunder_chance_perc_upper_threshold;

		public float bonus_manpower_per_level = 20f;

		public float bonus_manpower_per_leader_level = 5f;

		public float famous_bonus_manpower_leader = 15f;

		public float famous_bonus_manpower_general = 10f;

		public float famous_bonus_manpower_normal = 5f;

		public float min_time_before_attacking_towns = 300f;

		public DT.Field despawn_score;

		private void LoadBonuses(DT.Field f)
		{
			DT.Field field = f.FindChild("bonuses").FindChild("manpower");
			bonus_manpower_per_level = field.GetFloat("per_level");
			bonus_manpower_per_leader_level = field.GetFloat("per_leader_level");
			famous_bonus_manpower_leader = field.GetFloat("famous_leader");
			famous_bonus_manpower_general = field.GetFloat("famous_general");
			famous_bonus_manpower_normal = field.GetFloat("famous_normal");
		}

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			max_rebels = field.GetInt("max_rebels", null, max_rebels);
			time_disband_after_no_zone = field.GetFloat("time_disband_after_no_zone", null, time_disband_after_no_zone);
			become_famous_chance = field.GetFloat("become_famous_chance", null, become_famous_chance);
			plunder_wealth_threshold_lower = field.GetFloat("plunder_wealth_threshold_lower", null, plunder_wealth_threshold_lower);
			plunder_wealth_threshold_upper = field.GetFloat("plunder_wealth_threshold_upper", null, plunder_wealth_threshold_upper);
			plunder_chance_perc_lower_threshold = field.GetFloat("plunder_chance_perc_lower_threshold", null, plunder_chance_perc_lower_threshold);
			plunder_chance_perc_upper_threshold = field.GetFloat("plunder_chance_perc_upper_threshold", null, plunder_chance_perc_upper_threshold);
			despawn_score = field.FindChild("despawn_score");
			LoadBonuses(field);
			min_time_before_attacking_towns = field.GetFloat("min_time_before_attacking_towns", null, min_time_before_attacking_towns);
			return true;
		}
	}

	private const int STATES_IDX = 10;

	public Def def;

	public Rebel.Def rebel_def;

	public RebelSpawnCondition.Def condition_def;

	public List<Rebel> rebels = new List<Rebel>();

	public List<Realm> occupiedRealms = new List<Realm>();

	public List<Settlement> occupiedKeeps = new List<Settlement>();

	public List<Realm> zone = new List<Realm>();

	public List<Realm> potentialZone = new List<Realm>();

	public Rebel leader;

	public Character defeatedBy;

	public Kingdom defeatedByKingdom;

	public int origin_realm_id;

	public int spawn_kingdom_id;

	public int kingdom_id;

	public Time start_time;

	public int loyal_to;

	public RebelAgenda agenda;

	public Religion religion;

	public bool famous;

	public float wealth;

	public static bool ai_enabled = true;

	public bool marked_for_destroy;

	public List<Kingdom> enemies = new List<Kingdom>();

	public List<Kingdom> allies = new List<Kingdom>();

	public List<Kingdom> affectedKingdoms = new List<Kingdom>();

	public List<Kingdom> potentialAffectedKingdoms = new List<Kingdom>();

	public List<Kingdom> lastAffectedKingdoms = new List<Kingdom>();

	public Rebellion(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Rebellion(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return def.field.key + ".name";
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public static Rebellion JoinRebel(Rebel rebel)
	{
		if (!rebel.IsAuthority())
		{
			return null;
		}
		Realm realm = rebel.GetOriginRealm();
		if (realm.IsSeaRealm())
		{
			Game.Log("Trying to spawn a rebellion in a sea realm: " + realm.name, Game.LogType.Error);
			foreach (Realm neighbor in realm.neighbors)
			{
				if (!neighbor.IsSeaRealm())
				{
					realm = neighbor;
					break;
				}
			}
			if (realm.IsSeaRealm())
			{
				foreach (Realm neighbor2 in realm.neighbors)
				{
					foreach (Realm neighbor3 in neighbor2.neighbors)
					{
						if (!neighbor3.IsSeaRealm())
						{
							realm = neighbor3;
							break;
						}
					}
				}
			}
		}
		Rebellion rebellion = rebel.rebellion;
		if (!rebel.IsLeader())
		{
			if (rebellion == null)
			{
				for (int i = 0; i < realm.rebellions.Count; i++)
				{
					Rebellion rebellion2 = realm.rebellions[i];
					if (!(rebel.def.fraction_type != rebellion2.rebel_def.fraction_type) && rebel.loyal_to == rebellion2.loyal_to && (rebel.GetKingdom().type != Kingdom.Type.ReligiousFaction || rebel.religion == rebellion2.religion) && rebellion2.rebels.Count < rebellion2.def.max_rebels)
					{
						rebellion = rebellion2;
						break;
					}
				}
			}
			if (rebellion == null && rebel.GetKingdom().type == Kingdom.Type.RebelFaction && realm.rebellions.Count > 0)
			{
				for (int j = 0; j < realm.rebellions.Count; j++)
				{
					Rebellion rebellion3 = realm.rebellions[j];
					if (rebellion3.rebels.Count < rebellion3.def.max_rebels)
					{
						rebellion = rebellion3;
						rebel.kingdom_id = rebellion.kingdom_id;
						rebel.loyal_to = rebellion.loyal_to;
						rebel.SendState<Rebel.KingdomState>();
						rebel.def = rebellion.rebel_def;
						rebel.condition_def = rebellion.condition_def;
						rebel.SendState<Rebel.DefsState>();
						rebel.religion = rebellion.religion;
						rebel.SendState<Rebel.OriginState>();
					}
				}
			}
		}
		if (rebellion == null)
		{
			rebellion = new Rebellion(rebel, realm);
		}
		rebel.JoinRebellion(rebellion);
		return rebellion;
	}

	public Rebellion(Rebel rebel, Realm realm = null)
		: base(rebel.game)
	{
		if (realm == null)
		{
			realm = rebel.GetOriginRealm();
		}
		def = game.defs.Find<Def>("Rebellion");
		kingdom_id = rebel.kingdom_id;
		loyal_to = rebel.loyal_to;
		def = game.defs.Find<Def>("Rebellion");
		rebel_def = rebel.def;
		agenda = rebel.agenda;
		condition_def = rebel.condition_def;
		origin_realm_id = realm.id;
		religion = rebel.religion;
		spawn_kingdom_id = ((realm ?? rebel.GetOriginRealm())?.GetKingdom())?.id ?? 0;
		RefreshZoneRealms();
		SetLeader(rebel);
		realm.NotifyListeners("rebellion_started", this);
		game.GetKingdom(loyal_to)?.NotifyListeners("own_rebellion_started", this);
		game.rebellions.Add(this);
		start_time = game.time;
		TryOccupyRealm();
	}

	public override void OnInit()
	{
		new RebellionIndependence(this);
		base.OnInit();
	}

	public void ClearZone()
	{
		for (int i = 0; i < zone.Count; i++)
		{
			zone[i].DelRebellion(this);
		}
		zone.Clear();
		ClearPotentialZone();
	}

	public void ClearPotentialZone()
	{
		for (int i = 0; i < potentialZone.Count; i++)
		{
			potentialZone[i].DelPotentialRebellion(this);
		}
		potentialZone.Clear();
	}

	public void AddPotentialAffectedKingdom(Kingdom k, bool syncKingdom = true)
	{
		if (k != null)
		{
			if (!potentialAffectedKingdoms.Contains(k))
			{
				potentialAffectedKingdoms.Add(k);
			}
			if (syncKingdom)
			{
				k.AddPotentialRebellion(this);
			}
		}
	}

	public void DelPotentialAffectedKingdom(Kingdom k, bool syncKingdom = true)
	{
		if (k != null)
		{
			potentialAffectedKingdoms.Remove(k);
			if (syncKingdom)
			{
				k.DelPotentialRebellion(this, syncRebellion: false);
			}
		}
	}

	public void AddAffectedKingdom(Kingdom k, bool syncKingdom = true)
	{
		if (k != null)
		{
			if (!affectedKingdoms.Contains(k))
			{
				affectedKingdoms.Add(k);
				OnChangedAnalytics("spread_to_kingdom", k.Name);
			}
			if (syncKingdom)
			{
				k.AddRebellion(this);
			}
		}
	}

	public void DelAffectedKingdom(Kingdom k, bool syncKingdom = true)
	{
		if (k != null)
		{
			affectedKingdoms.Remove(k);
			if (syncKingdom)
			{
				k.DelRebellion(this, syncRebellion: false);
			}
		}
	}

	private void RefreshPotentialZoneRealms()
	{
		ClearPotentialZone();
		for (int i = 0; i < zone.Count; i++)
		{
			potentialZone.Add(zone[i]);
			zone[i].AddPotentialRebellion(this);
		}
	}

	public void PrepareDisband(Rebel r)
	{
		r.SetAction(Rebel.Action.Wait);
	}

	public bool IsDisbanding()
	{
		return Timer.Find(this, "disband") != null;
	}

	public void PrepareDisband()
	{
		if (IsAuthority())
		{
			Timer.Start(this, "disband", def.time_disband_after_no_zone);
			for (int i = 0; i < rebels.Count; i++)
			{
				PrepareDisband(rebels[i]);
			}
		}
	}

	public void StopDisbanding()
	{
		Timer timer = Timer.Find(this, "disband");
		if (timer != null)
		{
			for (int i = 0; i < rebels.Count; i++)
			{
				rebels[i].SetAction(Rebel.Action.Plunder);
			}
			timer.Stop();
		}
	}

	public void MoveOriginRealm(Realm newOrigin)
	{
		if (AssertAuthority())
		{
			origin_realm_id = newOrigin.id;
			spawn_kingdom_id = newOrigin.GetKingdom().id;
			RefreshZoneRealms();
			SendState<OriginState>();
		}
	}

	public void RefreshZoneRealms()
	{
		lastAffectedKingdoms.Clear();
		lastAffectedKingdoms.AddRange(affectedKingdoms);
		ClearZone();
		if (marked_for_destroy)
		{
			return;
		}
		Realm originRealm = GetOriginRealm();
		if (originRealm != null)
		{
			zone.Add(originRealm);
			zone.AddRange(originRealm.logicNeighborsRestricted);
		}
		for (int i = 0; i < occupiedRealms.Count; i++)
		{
			Realm realm = occupiedRealms[i];
			if (realm == null)
			{
				continue;
			}
			if (!zone.Contains(realm))
			{
				zone.Add(realm);
			}
			for (int j = 0; j < realm.logicNeighborsRestricted.Count; j++)
			{
				Realm realm2 = realm.logicNeighborsRestricted[j];
				if (realm2 != null && !zone.Contains(realm2))
				{
					zone.Add(realm2);
				}
			}
		}
		RefreshPotentialZoneRealms();
		if (!IsFamous())
		{
			Kingdom originKingdom = GetOriginKingdom();
			zone.RemoveAll(delegate(Realm r)
			{
				if (occupiedRealms.Contains(r))
				{
					return false;
				}
				if (r.GetKingdom() != originKingdom)
				{
					return true;
				}
				return (r.controller is Kingdom kingdom3 && kingdom3.IsRegular() && kingdom3 != originKingdom) ? true : false;
			});
		}
		if (IsLoyalist())
		{
			Kingdom loyalKingdom = GetLoyalTo();
			zone.RemoveAll((Realm r) => !occupiedRealms.Contains(r) && r.controller == loyalKingdom);
		}
		for (int num = 0; num < zone.Count; num++)
		{
			zone[num].AddRebellion(this);
		}
		if (zone.Count == 0)
		{
			PrepareDisband();
		}
		else
		{
			StopDisbanding();
		}
		if (lastAffectedKingdoms.Count <= 0)
		{
			return;
		}
		for (int num2 = 0; num2 < affectedKingdoms.Count; num2++)
		{
			Kingdom kingdom = affectedKingdoms[num2];
			if (!lastAffectedKingdoms.Contains(kingdom))
			{
				kingdom.NotifyListeners("rebellion_zone_joined", new Vars(this));
			}
		}
		for (int num3 = 0; num3 < lastAffectedKingdoms.Count; num3++)
		{
			Kingdom kingdom2 = lastAffectedKingdoms[num3];
			if (!affectedKingdoms.Contains(kingdom2))
			{
				kingdom2.NotifyListeners("rebellion_zone_left", new Vars(this));
			}
		}
	}

	public Kingdom GetOriginKingdom()
	{
		return game.GetKingdom(spawn_kingdom_id);
	}

	public Realm GetOriginRealm()
	{
		return game.GetRealm(origin_realm_id);
	}

	public int GetPayoffGold()
	{
		int num = 0;
		for (int i = 0; i < rebels.Count; i++)
		{
			num += rebels[i].def.payoff_gold_base;
		}
		return num / 2 + (int)Math.Max((float)(num / 2) - wealth / 5f, 0f);
	}

	public Realm GetPayoffRealm(int idx)
	{
		int num = 0;
		for (int i = 0; i < rebels.Count; i++)
		{
			num += rebels[i].def.payoff_realm_no;
		}
		if (idx >= num)
		{
			return null;
		}
		if (idx < potentialZone.Count)
		{
			return potentialZone[idx];
		}
		return null;
	}

	public void OnChangedAnalytics(string changeType, string target)
	{
		if (Game.isLoadingSaveGame || !IsAuthority() || string.IsNullOrEmpty(changeType) || !game.IsRunning())
		{
			return;
		}
		bool flag = false;
		int num = 0;
		while (affectedKingdoms != null && num < affectedKingdoms.Count && !(flag = affectedKingdoms[num].is_player))
		{
			num++;
		}
		if (!flag)
		{
			return;
		}
		int armiesCount = GetArmiesCount();
		Vars vars = new Vars();
		vars.Set("rebellionEvent", changeType);
		vars.Set("rebellionSize", armiesCount);
		vars.Set("rebelArmyPower", GetManpower());
		vars.Set("eventTarget", target);
		for (int i = 0; i < affectedKingdoms.Count; i++)
		{
			if (affectedKingdoms[i].is_player)
			{
				affectedKingdoms[i].FireEvent("analytics_rebellion_changed", vars, affectedKingdoms[i].id);
			}
		}
	}

	public void SetLeader(Rebel rebel, bool send_state = true)
	{
		if (rebel != null)
		{
			Rebel rebel2 = leader;
			leader = rebel;
			leader.Promote(leader.def.GetLeaderDef());
			AddRebel(leader);
			if (rebel2 != null)
			{
				NotifyAffectedKingdoms("rebellion_new_leader");
			}
			else
			{
				NotifyAffectedKingdoms("rebellion_leader_started");
			}
			NotifyListeners("leader_changed");
			OnChangedAnalytics("leader_changed", rebel.ToString());
			if (send_state)
			{
				SendState<LeaderState>();
			}
		}
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
		if (num)
		{
			if (flag)
			{
				rebel_def = rebel_def.GetChangeToLoyalistDef();
			}
			else
			{
				rebel_def = rebel_def.GetChangeToRebelDef();
			}
			kingdom_id = FactionUtils.GetFactionKingdom(game, rebel_def.kingdom_key).id;
			SendState<DefsState>();
		}
		loyal_to = kid;
		SendState<KingdomState>();
		OnChangedAnalytics("loyalty_changed", kingdom.Name);
		for (int i = 0; i < rebels.Count; i++)
		{
			if (rebels[i].army.battle != null)
			{
				rebels[i].army.battle.Cancel(Battle.VictoryReason.Combat);
			}
			rebels[i].ChangeLoyalTo(kid);
		}
		for (int num2 = occupiedKeeps.Count - 1; num2 >= 0; num2--)
		{
			if (occupiedKeeps[num2].keep_effects != null)
			{
				if (occupiedKeeps[num2].battle != null)
				{
					occupiedKeeps[num2].battle.Cancel(Battle.VictoryReason.Combat);
				}
				if (!IsEnemy(occupiedKeeps[num2].GetRealm().GetKingdom()))
				{
					occupiedKeeps[num2].keep_effects.SetOccupied(null);
				}
				else
				{
					occupiedKeeps[num2].FireEvent("controlling_obj_changed", null);
				}
			}
		}
		for (int num3 = occupiedRealms.Count - 1; num3 >= 0; num3--)
		{
			Realm realm = occupiedRealms[num3];
			if (realm.castle?.battle != null)
			{
				realm.castle.battle.Cancel(Battle.VictoryReason.Combat);
			}
			if (!IsEnemy(realm.GetKingdom()))
			{
				realm.SetOccupied(realm.GetKingdom());
			}
			else
			{
				realm.FireEvent("controlling_obj_changed", null);
			}
		}
	}

	public Kingdom GetLoyalTo()
	{
		return game.GetKingdom(loyal_to);
	}

	public bool IsLoyalist()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.type != Kingdom.Type.LoyalistsFaction)
		{
			return false;
		}
		return kingdom.id != loyal_to;
	}

	public bool IsReligious()
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.type != Kingdom.Type.ReligiousFaction)
		{
			return false;
		}
		return true;
	}

	public override IRelationCheck GetStanceObj()
	{
		return this;
	}

	public override RelationUtils.Stance GetStance(Kingdom k)
	{
		if (k == null)
		{
			return RelationUtils.Stance.Peace;
		}
		if (allies.Contains(k))
		{
			return RelationUtils.Stance.Alliance;
		}
		if (IsLoyalist() && loyal_to == k.id)
		{
			return RelationUtils.Stance.Alliance;
		}
		if (k.type == Kingdom.Type.Faction)
		{
			return RelationUtils.Stance.Peace;
		}
		return RelationUtils.Stance.War;
	}

	public override RelationUtils.Stance GetStance(Rebellion r)
	{
		if (r == null)
		{
			return RelationUtils.Stance.Peace;
		}
		if (r == this)
		{
			return RelationUtils.Stance.Own;
		}
		if (IsLoyalist())
		{
			if (loyal_to == r.loyal_to)
			{
				return RelationUtils.Stance.Alliance;
			}
			if (r.IsLoyalist())
			{
				return RelationUtils.Stance.War;
			}
			return RelationUtils.Stance.Peace;
		}
		return RelationUtils.Stance.Peace;
	}

	public override RelationUtils.Stance GetStance(Crusade c)
	{
		return RelationUtils.Stance.Peace;
	}

	public override RelationUtils.Stance GetStance(Settlement s)
	{
		return s.GetStance(this);
	}

	protected override void OnDestroy()
	{
		game.rebellions.Remove(this);
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		base.OnStart();
		UpdateInBatch(game.update_5sec);
		if (IsAuthority())
		{
			OnChangedAnalytics("started", def.id);
		}
	}

	private void TryOccupyRealm()
	{
		if (!IsLoyalist())
		{
			return;
		}
		Realm originRealm = GetOriginRealm();
		if (originRealm != null && originRealm.IsDisorder())
		{
			float chance_disorder_takeover_realm = rebel_def.chance_disorder_takeover_realm;
			if ((float)game.Random(0, 100) < chance_disorder_takeover_realm)
			{
				originRealm.SetOccupied(this);
			}
		}
	}

	public float DespawnScore()
	{
		if (def?.despawn_score == null)
		{
			return 0f;
		}
		return def.despawn_score.Float(this);
	}

	public string GetSpawnReasonRaw()
	{
		return condition_def.reason;
	}

	public void EndRebellion()
	{
		StopUpdating();
		if (!IsAuthority())
		{
			return;
		}
		marked_for_destroy = true;
		if (defeatedByKingdom != null)
		{
			float num = 0f;
			if (defeatedBy != null)
			{
				num = defeatedBy.GetStat(Stats.cs_gold_from_rebellions_perc);
			}
			defeatedByKingdom.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, wealth * (1f + num / 100f));
			FadingRebellionRiskModifier.Add(defeatedByKingdom, game.defs.Find<FadingRebellionRiskModifier.Def>("FadingRebellionRiskModifier"));
			Realm originRealm = GetOriginRealm();
			originRealm?.stats.AddModifier("rs_defeated_rebellion", new FadingModifier(game, game.defs.Get<FadingModifier.Def>("FadingDefeatedRebellion"), originRealm, originRealm));
			defeatedByKingdom.stability?.SpecialEvent(think_rebel: false);
		}
		NotifyAffectedKingdoms("rebellion_ended", this);
		OnChangedAnalytics("ended", def.id);
		FreeAllKeeps();
		FreeAllRealms();
		ClearZone();
		SendState<EndRebellionState>();
		Destroy();
	}

	public void Payoff()
	{
		if (spawn_kingdom_id != game.GetLocalPlayerKingdomId())
		{
			return;
		}
		Kingdom kingdom = game.GetKingdom(spawn_kingdom_id);
		if (!IsAuthority())
		{
			kingdom.SendEvent(new Kingdom.PayoffRebelion(this));
			return;
		}
		Resource resource = new Resource();
		resource.Add(ResourceType.Gold, GetPayoffGold());
		if (!kingdom.resources.CanAfford(resource, 1f))
		{
			return;
		}
		if (GetPayoffRealm(0) != null)
		{
			AddOccupiedRealm(GetPayoffRealm(0));
		}
		if (GetPayoffRealm(1) != null)
		{
			AddOccupiedRealm(GetPayoffRealm(1));
		}
		if (GetPayoffRealm(2) != null)
		{
			AddOccupiedRealm(GetPayoffRealm(2));
		}
		kingdom.SubResources(KingdomAI.Expense.Category.Diplomacy, resource);
		RebellionIndependence component = GetComponent<RebellionIndependence>();
		if (component != null)
		{
			if (component.GetIndependenceRealms().Count > 0)
			{
				component.DeclareIndependence(forced: true);
			}
			else
			{
				EndRebellion();
			}
		}
	}

	public override void OnUpdate()
	{
		if (!IsAuthority())
		{
			return;
		}
		if (rebels.Count > def.max_rebels)
		{
			rebels.Sort((Rebel x, Rebel y) => y.army.EvalStrength().CompareTo(x.army.EvalStrength()));
			int num = rebels.Count - def.max_rebels;
			for (int num2 = rebels.Count - 1; num2 >= 0; num2--)
			{
				Rebel rebel = rebels[num2];
				if (!rebel.IsLeader())
				{
					rebel.Destroy();
					num--;
					if (num <= 0)
					{
						break;
					}
				}
			}
		}
		for (int num3 = 0; num3 < rebels.Count; num3++)
		{
			Rebel rebel2 = rebels[num3];
			if (rebel2.army == null || rebel2.army.realm_in == null)
			{
				continue;
			}
			rebel2.Think(rebel2.army.realm_in);
			if (!rebel2.IsValid())
			{
				num3--;
				if (rebels.Contains(rebel2))
				{
					DelRebel(rebel2);
				}
			}
		}
	}

	public void Disband()
	{
		if (IsAuthority() && !marked_for_destroy)
		{
			marked_for_destroy = true;
			for (int num = rebels.Count - 1; num >= 0; num--)
			{
				rebels[num].Disband();
			}
		}
	}

	private bool TryDisband()
	{
		if ((float)game.Random(0, 100) >= rebel_def.chance_denounce_disband)
		{
			return false;
		}
		Disband();
		return true;
	}

	public bool SpawnRebel(int kingdom_id, int realm_id)
	{
		return true;
	}

	public void AddOccupiedRealm(Realm r, bool send_state = true)
	{
		if (r != null && !occupiedRealms.Contains(r))
		{
			occupiedRealms.Add(r);
			RefreshZoneRealms();
			AddEnemy(r.GetKingdom());
			GetComponent<RebellionIndependence>().Refresh(r);
			OnChangedAnalytics("occupied_realm", r.name);
		}
	}

	public void DelOccupiedRealm(Realm r, bool send_state = true)
	{
		if (r != null && occupiedRealms.Remove(r))
		{
			RefreshZoneRealms();
			GetComponent<RebellionIndependence>()?.Refresh(r);
			OnChangedAnalytics("unoccupied_realm", r.name);
		}
	}

	public void AddRebel(Rebel r, bool send_state = true)
	{
		if (!rebels.Contains(r))
		{
			rebels.Add(r);
			r.rebellion = this;
			if (IsLoyalist())
			{
				r.ChangeLoyalTo(loyal_to);
			}
			if (IsDisbanding())
			{
				PrepareDisband(r);
			}
			NotifyListeners("rebels_changed");
			if (send_state)
			{
				SendState<RebelsState>();
			}
			OnChangedAnalytics("rebel_added", r.ToString());
		}
	}

	public void AppointNewLeader()
	{
		if (!IsAuthority() || marked_for_destroy)
		{
			return;
		}
		Rebel rebel = null;
		for (int i = 0; i < rebels.Count; i++)
		{
			Rebel rebel2 = rebels[i];
			if (rebel2.IsGeneral() && (float)game.Random(0, 100) < rebel2.def.chance_general_taking_leadership)
			{
				rebel = rebel2;
				break;
			}
		}
		if (rebel == null)
		{
			Disband();
			return;
		}
		SetLeader(rebel);
		OnChangedAnalytics("leader_changed", rebel.ToString());
	}

	public void DelRebel(Rebel r, bool send_state = true)
	{
		if (rebels.Remove(r))
		{
			if (r != null)
			{
				OnChangedAnalytics("rebel_removed", r.ToString());
			}
			NotifyListeners("rebels_changed");
			if (send_state && IsAuthority())
			{
				SendState<RebelsState>();
			}
			if (rebels.Count == 0)
			{
				if (IsValid())
				{
					EndRebellion();
				}
			}
			else if (leader == r)
			{
				AppointNewLeader();
			}
		}
		r?.army?.realm_in?.GetKingdom()?.stability?.SpecialEvent(think_rebel: false);
	}

	public void AddEnemy(Kingdom e, bool send_state = true)
	{
		if (!enemies.Contains(e))
		{
			enemies.Add(e);
			if (send_state)
			{
				SendState<EnemiesState>();
			}
			OnChangedAnalytics("new_enemy", e.Name);
		}
	}

	public void AddAlly(Kingdom e, bool send_state = true)
	{
		if (!allies.Contains(e))
		{
			allies.Add(e);
			if (send_state)
			{
				SendState<AlliesState>();
			}
			OnChangedAnalytics("new_ally", e.Name);
		}
	}

	public float GetWealth()
	{
		return wealth;
	}

	public float GetPlunderChance()
	{
		return Game.map_clamp(wealth, def.plunder_chance_perc_lower_threshold, def.plunder_wealth_threshold_upper, def.plunder_chance_perc_lower_threshold, def.plunder_chance_perc_upper_threshold);
	}

	public void AddWealth(float val, bool send_state = true)
	{
		wealth += val;
		wealth = Game.CeilingByOffset(wealth, 1);
		for (int i = 0; i < rebels.Count; i++)
		{
			rebels[i]?.army?.AddSupplies(val / (float)rebels.Count);
		}
		NotifyListeners("wealth_changed");
		if (send_state)
		{
			SendState<WealthState>();
		}
	}

	public void Support(Kingdom kingdom, int amount)
	{
		if (IsAuthority() && !(kingdom.resources[ResourceType.Gold] < (float)amount))
		{
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, amount);
			AddWealth(amount);
		}
	}

	public void Denounce(Kingdom k)
	{
		for (int i = 0; i < rebels.Count; i++)
		{
			rebels[i].Denounce(k);
		}
		if (rebels.Count == 0)
		{
			EndRebellion();
		}
	}

	public void FreeAllKeeps()
	{
		for (int num = occupiedKeeps.Count - 1; num >= 0; num--)
		{
			if (occupiedKeeps[num].keep_effects != null)
			{
				occupiedKeeps[num].keep_effects.SetOccupied(null);
				if (occupiedKeeps[num].battle != null)
				{
					occupiedKeeps[num].battle.Cancel(Battle.VictoryReason.Combat);
				}
			}
		}
	}

	public void FreeAllRealms()
	{
		for (int num = occupiedRealms.Count - 1; num >= 0; num--)
		{
			Realm realm = occupiedRealms[num];
			if (realm.castle?.battle != null)
			{
				realm.castle.battle.Cancel(Battle.VictoryReason.RealmChange);
			}
			realm.SetOccupied(realm.GetKingdom());
		}
	}

	public override string ToString()
	{
		return (IsLoyalist() ? "Loyalist " : "") + "Rebellion lead by " + leader?.character;
	}

	public void NotifyAffectedKingdoms(string message, object param = null)
	{
		Vars vars = new Vars(this);
		if (param != null)
		{
			vars.Set("param", param);
		}
		for (int i = 0; i < affectedKingdoms.Count; i++)
		{
			affectedKingdoms[i].NotifyListeners(message, vars);
		}
	}

	public float GetPower()
	{
		int num = 1;
		if (rebels != null)
		{
			for (int i = 0; i < rebels.Count; i++)
			{
				Rebel rebel = rebels[i];
				if (rebel != null && rebel.IsValid() && rebel.army != null && rebel.army.IsValid())
				{
					num += rebels[i].army.units.Count;
				}
			}
		}
		return num;
	}

	public int GetManpower()
	{
		int num = 0;
		if (rebels != null)
		{
			for (int i = 0; i < rebels.Count; i++)
			{
				Rebel rebel = rebels[i];
				if (rebel != null && rebel.IsValid() && rebel.army != null && rebel.army.IsValid())
				{
					num += rebels[i].army.GetManPower();
				}
			}
		}
		return num;
	}

	public int GetArmiesCount()
	{
		int num = 0;
		if (rebels != null)
		{
			for (int i = 0; i < rebels.Count; i++)
			{
				Rebel rebel = rebels[i];
				if (rebel != null && rebel.IsValid() && rebel.army != null && rebel.army.IsValid())
				{
					num++;
				}
			}
		}
		return num;
	}

	public float GetManpowerBonus(Rebel r)
	{
		if (!rebels.Contains(r))
		{
			return 0f;
		}
		float num = 0f;
		if (IsFamous())
		{
			num = (r.IsLeader() ? (num + def.famous_bonus_manpower_leader) : ((!r.IsGeneral()) ? (num + def.famous_bonus_manpower_normal) : (num + def.famous_bonus_manpower_general)));
		}
		if (r.IsGeneral() && r.character != null)
		{
			num += (float)r.character.GetClassLevel() * def.bonus_manpower_per_level;
		}
		if (leader?.character != null)
		{
			num += (float)leader.character.GetClassLevel() * def.bonus_manpower_per_leader_level;
		}
		return num;
	}

	public override void OnTimer(Timer timer)
	{
		string name = timer.name;
		if (name == "disband")
		{
			if (!marked_for_destroy)
			{
				Disband();
			}
		}
		else
		{
			base.OnTimer(timer);
		}
	}

	private void NotifyArmies(string notification, object param = null)
	{
		for (int i = 0; i < rebels.Count; i++)
		{
			Rebel rebel = rebels[i];
			if (rebel != null && rebel.IsValid() && rebel.army != null && rebel.army.IsValid())
			{
				rebel.army.NotifyListeners(notification, param);
			}
		}
	}

	public bool IsFamous()
	{
		return famous;
	}

	public void SetFamous(bool famous)
	{
		this.famous = famous;
		RefreshZoneRealms();
		SendState<FamousState>();
		NotifyAffectedKingdoms("rebellion_famous_state_changed");
		leader.character.NotifyListeners("rebellion_famous_state_changed");
		NotifyArmies("rebellion_famous_state_changed");
		if (famous)
		{
			OnChangedAnalytics("became_famous", def.id);
		}
	}

	public bool TryBecomeFamous()
	{
		if (famous)
		{
			return false;
		}
		float become_famous_chance = def.become_famous_chance;
		if ((float)game.Random(0, 100) < become_famous_chance)
		{
			SetFamous(famous: true);
			return true;
		}
		return false;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "rebellion":
			return this;
		case "origin_realm":
			return GetOriginRealm();
		case "origin_kingdom":
			return GetOriginKingdom();
		case "leader":
			return leader.character;
		case "last_rebel":
			return rebels[rebels.Count - 1].character;
		case "loyal_to":
			return game.GetKingdom(loyal_to);
		case "is_loyalist":
			return IsLoyalist();
		case "is_famous":
			return IsFamous();
		case "defeated_by_kingdom":
			return defeatedByKingdom;
		case "time_exist":
			return (game.time - start_time) / 60f;
		case "towns_occupied":
			return occupiedRealms.Count;
		case "payoff_gold":
			return GetPayoffGold();
		case "payoff_gold_missing":
		{
			Resource resource = new Resource();
			resource.Add(ResourceType.Gold, GetPayoffGold());
			return !game.GetKingdom(spawn_kingdom_id).resources.CanAfford(resource, 1f);
		}
		case "payoff_realm_0":
			return GetPayoffRealm(0) != null;
		case "payoff_realm_1":
			return GetPayoffRealm(1) != null;
		case "payoff_realm_2":
			return GetPayoffRealm(2) != null;
		case "payoff_realm_0_name":
			return GetPayoffRealm(0)?.name;
		case "payoff_realm_1_name":
			return GetPayoffRealm(1)?.name;
		case "payoff_realm_2_name":
			return GetPayoffRealm(2)?.name;
		case "in_battle":
		{
			if (RebellionIndependence.IsInNonRDIBattle(leader.army))
			{
				return true;
			}
			for (int i = 0; i < occupiedRealms.Count; i++)
			{
				if (occupiedRealms[i].castle?.battle != null)
				{
					return true;
				}
			}
			for (int j = 0; j < rebels.Count; j++)
			{
				if (RebellionIndependence.IsInNonRDIBattle(rebels[j].army))
				{
					return true;
				}
			}
			return false;
		}
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("def_name", def?.name);
		dump.Append("rebel_def_name", rebel_def?.name);
		dump.Append("condition_def_name", condition_def?.name);
		dump.Append("agenda", agenda?.Name);
		dump.Append("kingdom", game.GetKingdom(kingdom_id)?.Name);
		dump.Append("loyal_to", game.GetKingdom(loyal_to)?.Name);
		dump.Append("origin_realm", game.GetRealm(origin_realm_id)?.name);
		dump.Append("spawn_kingdom", game.GetKingdom(spawn_kingdom_id)?.Name);
		dump.Append("religion", religion?.name);
		dump.Append("leader", leader?.ToString());
		if (rebels.Count > 0)
		{
			dump.OpenSection("rebels");
			for (int i = 0; i < rebels.Count; i++)
			{
				dump.Append(rebels[i]?.ToString());
			}
			dump.CloseSection("rebels");
		}
		if (zone.Count > 0)
		{
			dump.OpenSection("zone");
			for (int j = 0; j < zone.Count; j++)
			{
				dump.Append(zone[j]?.ToString());
			}
			dump.CloseSection("zone");
		}
		if (affectedKingdoms.Count > 0)
		{
			dump.OpenSection("affected_kingdoms");
			for (int k = 0; k < affectedKingdoms.Count; k++)
			{
				dump.Append(affectedKingdoms[k]?.ToString());
			}
			dump.CloseSection("affected_kingdoms");
		}
		if (allies.Count > 0)
		{
			dump.OpenSection("allies");
			for (int l = 0; l < allies.Count; l++)
			{
				dump.Append(allies[l]?.Name);
			}
			dump.CloseSection("allies");
		}
		if (enemies.Count > 0)
		{
			dump.OpenSection("enemies");
			for (int m = 0; m < enemies.Count; m++)
			{
				dump.Append(enemies[m]?.Name);
			}
			dump.CloseSection("enemies");
		}
		if (occupiedRealms.Count > 0)
		{
			dump.OpenSection("occupied_realms");
			for (int n = 0; n < occupiedRealms.Count; n++)
			{
				dump.Append(occupiedRealms[n]?.name);
			}
			dump.CloseSection("occupied_realms");
		}
		if (occupiedKeeps.Count > 0)
		{
			dump.OpenSection("occupied_keeps");
			for (int num = 0; num < occupiedKeeps.Count; num++)
			{
				dump.Append(num.ToString(), occupiedKeeps[num]);
			}
			dump.CloseSection("occupied_keeps");
		}
		dump.Append("famous", famous.ToString());
	}
}
