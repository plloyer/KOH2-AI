namespace Logic;

[Serialization.Object(Serialization.ObjectType.Migrant)]
public class Migrant : MapObject
{
	public class Def : Logic.Def
	{
		public string name;

		public float spawn_chance;

		public float spawn_period;

		public PerLevelValues reward;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			spawn_chance = field.GetFloat("spawn_chance");
			spawn_period = field.GetFloat("spawn_period");
			reward = PerLevelValues.Parse<Resource>(base.field.FindChild("reward"), null, no_null: true);
			return true;
		}
	}

	[Serialization.State(21)]
	public class InitState : Serialization.ObjectState
	{
		public int origin_realm_id;

		public int dest_realm_id;

		public int rp_count;

		public static InitState Create()
		{
			return new InitState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Migrant migrant = obj as Migrant;
			origin_realm_id = migrant.castle_origin.realm_id;
			dest_realm_id = migrant.castle_destination.realm_id;
			rp_count = migrant.GetRPCount();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(origin_realm_id, "origin_realm_id");
			ser.Write7BitUInt(dest_realm_id, "dest_realm_id");
			ser.Write7BitUInt(rp_count, "rp_count");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			origin_realm_id = ser.Read7BitUInt("origin_realm_id");
			dest_realm_id = ser.Read7BitUInt("dest_realm_id");
			rp_count = ser.Read7BitUInt("rp_count");
		}

		public override void ApplyTo(Object obj)
		{
			Migrant obj2 = obj as Migrant;
			obj2.castle_origin = obj2.game.GetRealm(origin_realm_id).castle;
			obj2.SetObjectives(rp_count, dest_realm_id, send_state: false);
		}
	}

	public Realm realm_in;

	public Castle castle_origin;

	public Castle castle_destination;

	private int rp;

	public Def def;

	private int last_seg_idx = -1;

	private const int STATES_IDX = 20;

	private const int EVENTS_IDX = 36;

	public int GetRPCount()
	{
		return rp;
	}

	public override void OnInit()
	{
		movement = new Movement(this, PathData.PassableArea.Type.None);
	}

	public void ResetSegmentIdxCheck()
	{
		if (movement?.path != null)
		{
			last_seg_idx = movement.path.segment_idx;
		}
		else
		{
			last_seg_idx = -1;
		}
	}

	public override void HandleRiverCrossing()
	{
		if (movement.path == null || movement.paused)
		{
			return;
		}
		int num = last_seg_idx;
		ResetSegmentIdxCheck();
		if (num < 0)
		{
			return;
		}
		for (int i = num; i <= movement.path.segment_idx; i++)
		{
			if (movement.path.segments[i].pt.paID == -1 && movement.path.segments.Count > i + 1)
			{
				Path.Segment segment = movement.path.segments[i + 1];
				movement.path.segment_idx = i + 1;
				movement.path.t = segment.t;
				break;
			}
		}
	}

	public override void SetPosition(PPos position)
	{
		HandleRiverCrossing();
		base.position = position;
		NotifyListeners("moved");
	}

	public float GetTripProgress()
	{
		if (movement == null || movement.path == null)
		{
			return 1f;
		}
		return movement.path.t / movement.path.path_len;
	}

	public void SetObjectives(int rp_count, int realm_id, bool send_state = true)
	{
		rp = rp_count;
		castle_destination = game.GetRealm(realm_id).castle;
		if (visuals == null)
		{
			UpdateAfter(1f);
		}
		NotifyListeners("reward_changed");
		if (send_state && IsAuthority())
		{
			SendState<InitState>();
		}
	}

	public override void OnUpdate()
	{
		if (visuals == null)
		{
			UpdateAfter(1f);
		}
		else
		{
			NotifyListeners("reward_changed");
		}
	}

	public void Stop(bool send_state = true)
	{
		movement.Stop();
	}

	public void MoveTo(Point pt, float range = 0f)
	{
		movement.MoveTo(pt, range);
	}

	public void MoveTo(MapObject dst_obj, float range = 0f)
	{
		if (dst_obj != null)
		{
			movement.MoveTo(dst_obj, range);
		}
	}

	public override void OnBeginMoving()
	{
	}

	public override void DestinationReached(MapObject dst_obj)
	{
		if (dst_obj != null && dst_obj == castle_destination && IsAuthority())
		{
			Kingdom kingdom = castle_destination.GetKingdom();
			kingdom.AddResources(KingdomAI.Expense.Category.Economy, def.reward.GetResources(rp));
			castle_destination.population.AddVillagers(rp, Population.Type.Worker);
			kingdom.SendState<Kingdom.ResourcesState>();
			Destroy();
		}
	}

	public Migrant(Game game, Realm start_realm, Realm target_realm, int rp = 1)
		: base(game, start_realm.castle.GetRandomExitPoint(), target_realm.kingdom_id)
	{
		start_realm.migrants.Add(this);
		castle_origin = start_realm.castle;
		castle_destination = target_realm.castle;
		def = game.defs.GetBase<Def>();
		this.rp = rp;
		castle_origin.population.RemoveVillagers(rp, Population.Type.Rebel);
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (IsAuthority())
		{
			MoveTo(castle_destination);
			SetObjectives(rp, castle_destination.realm_id);
		}
	}

	protected override void OnDestroy()
	{
		if (castle_origin != null)
		{
			castle_origin.GetRealm()?.migrants.Remove(this);
		}
		base.OnDestroy();
	}

	public override Value GetDumpStateValue()
	{
		return Value.Null;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("castle_origin", castle_origin?.ToString());
		dump.Append("castle_destination", castle_destination?.ToString());
		dump.Append("rp", rp);
	}

	public Migrant(Multiplayer multiplayer)
		: base(multiplayer)
	{
		def = game.defs.GetBase<Def>();
	}

	public new static Object Create(Multiplayer multiplayer)
	{
		return new Migrant(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}
}
