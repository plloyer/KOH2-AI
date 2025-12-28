using System.Collections.Generic;

namespace Logic;

public class MapObject : Object
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
			if (obj is Settlement settlement)
			{
				Realm realm = settlement.GetRealm();
				if (realm != null && settlement.kingdom_id == realm.init_kingdom_id)
				{
					return false;
				}
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			kingdom_id = (obj as MapObject).kingdom_id;
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
			(obj as MapObject).SetKingdom(kingdom_id);
		}
	}

	[Serialization.State(12)]
	public class PositionState : Serialization.ObjectState
	{
		public PPos pos;

		public PositionState()
		{
		}

		public PositionState(MapObject obj)
		{
			pos = obj.position;
		}

		public static PositionState Create()
		{
			return new PositionState();
		}

		public static bool IsNeeded(Object obj)
		{
			return !(obj is Settlement);
		}

		public override bool InitFrom(Object obj)
		{
			pos = (obj as MapObject).position;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WritePPos(pos, "pos");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			pos = ser.ReadPPos("pos");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as MapObject).SetPosition(pos);
		}
	}

	[Serialization.State(13)]
	public class PathState : Serialization.ObjectState
	{
		private class ExtraPathData
		{
			public List<PPos> path_points;

			public NID original_dst_obj_nid;

			public NID dst_obj_nid;

			public float t;

			public float range;

			public float original_range;

			public ExtraPathData()
			{
			}

			public ExtraPathData(Movement.ExtraPath extraPath)
			{
				path_points = new List<PPos>(extraPath.path.segments.Count);
				for (int i = 0; i < extraPath.path.segments.Count; i++)
				{
					path_points.Add(extraPath.path.segments[i].pt);
				}
				t = extraPath.path.t;
				if (extraPath.path.original_dst_obj != null)
				{
					original_dst_obj_nid = extraPath.path.original_dst_obj;
				}
				else
				{
					original_dst_obj_nid = null;
				}
				original_range = extraPath.path.original_range;
				range = extraPath.path.range;
				if (extraPath.path.dst_obj != null)
				{
					dst_obj_nid = extraPath.path.dst_obj;
				}
				else
				{
					dst_obj_nid = null;
				}
			}

			public Movement.ExtraPath CreateExtraPath(MapObject obj)
			{
				Movement.ExtraPath extraPath = new Movement.ExtraPath();
				extraPath.path = new Path(obj.game, obj);
				extraPath.path.SetPath(path_points);
				extraPath.path.range = range;
				extraPath.path.dst_obj = dst_obj_nid.GetObj(obj.game) as MapObject;
				extraPath.path.original_dst_obj = original_dst_obj_nid.GetObj(obj.game) as MapObject;
				extraPath.path.original_range = original_range;
				if (path_points.Count > 0)
				{
					extraPath.path.src_pt = path_points[0];
					extraPath.path.dst_pt = path_points[path_points.Count - 1];
				}
				extraPath.dst_pt = extraPath.path.dst_pt;
				extraPath.dst_obj = extraPath.path.dst_obj;
				extraPath.pf_path = null;
				extraPath.dst_range = range;
				extraPath.pf_path = extraPath.path;
				return extraPath;
			}
		}

		private List<ExtraPathData> extra_paths;

		private NID original_dst_obj_nid;

		private NID dst_obj_nid;

		private List<PPos> path_points;

		private float t;

		private float range;

		private float original_range;

		private bool flee;

		public static PathState Create()
		{
			return new PathState();
		}

		public static bool IsNeeded(Object obj)
		{
			Movement component = obj.GetComponent<Movement>();
			if (component == null || component.path == null)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			MapObject mapObject = obj as MapObject;
			Movement component = obj.GetComponent<Movement>();
			if (component == null || component.path == null)
			{
				path_points = new List<PPos>();
				path_points.Add(mapObject.position);
				return false;
			}
			path_points = new List<PPos>(component.path.segments.Count);
			for (int i = 0; i < component.path.segments.Count; i++)
			{
				path_points.Add(component.path.segments[i].pt);
			}
			t = component.path.t;
			if (component.path.original_dst_obj != null)
			{
				original_dst_obj_nid = component.path.original_dst_obj;
			}
			else
			{
				original_dst_obj_nid = null;
			}
			original_range = component.path.original_range;
			range = component.path.range;
			flee = component.path.flee;
			if (component.path.dst_obj != null)
			{
				dst_obj_nid = component.path.dst_obj;
			}
			else
			{
				dst_obj_nid = null;
			}
			extra_paths = new List<ExtraPathData>();
			for (int j = 0; j < component.extra_paths.Count && component.extra_paths[j] != null && component.extra_paths[j].path != null; j++)
			{
				extra_paths.Add(new ExtraPathData(component.extra_paths[j]));
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(dst_obj_nid, "dst_obj_nid");
			ser.WriteNID(original_dst_obj_nid, "original_dst_obj_nid");
			if (path_points == null)
			{
				ser.Write7BitUInt(0, "cnt");
				return;
			}
			ser.Write7BitUInt(path_points.Count, "cnt");
			int count = path_points.Count;
			if (count <= 0)
			{
				return;
			}
			if (count > 1)
			{
				ser.WriteFloat(t, "t");
			}
			ser.WriteFloat(range, "range");
			ser.WriteFloat(original_range, "original_range");
			ser.WriteBool(flee, "flee");
			for (int i = 0; i < path_points.Count; i++)
			{
				PPos val = path_points[i];
				ser.WritePPos(val, "pt_", i);
			}
			int num = ((extra_paths != null) ? extra_paths.Count : 0);
			ser.Write7BitUInt(num, "num_extra_paths");
			for (int j = 0; j < num; j++)
			{
				ExtraPathData extraPathData = extra_paths[j];
				ser.WriteNID(extraPathData.dst_obj_nid, "extra_dst_obj_nid_", j);
				ser.WriteNID(extraPathData.original_dst_obj_nid, "extra_original_dst_obj_nid_", j);
				if (extraPathData.path_points == null)
				{
					ser.Write7BitUInt(0, "extra_cnt_", j);
					break;
				}
				ser.Write7BitUInt(extraPathData.path_points.Count, "extra_cnt_", j);
				int count2 = extraPathData.path_points.Count;
				if (count2 <= 0)
				{
					break;
				}
				if (count2 > 1)
				{
					ser.WriteFloat(t, "extra_t_", j);
				}
				ser.WriteFloat(extraPathData.range, "extra_range_", j);
				ser.WriteFloat(extraPathData.original_range, "extra_original_range_", j);
				for (int k = 0; k < count2; k++)
				{
					PPos val2 = extraPathData.path_points[k];
					ser.WritePPos(val2, "extra_pt_" + j + "_", k);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			dst_obj_nid = ser.ReadNID("dst_obj_nid");
			original_dst_obj_nid = ser.ReadNID("original_dst_obj_nid");
			int num = ser.Read7BitUInt("cnt");
			if (num <= 0)
			{
				return;
			}
			if (num > 1)
			{
				t = ser.ReadFloat("t");
			}
			range = ser.ReadFloat("range");
			original_range = ser.ReadFloat("original_range");
			flee = ser.ReadBool("flee");
			path_points = new List<PPos>();
			for (int i = 0; i < num; i++)
			{
				PPos item = ser.ReadPPos("pt_", i);
				path_points.Add(item);
			}
			int num2 = 0;
			if (Serialization.cur_version >= 15)
			{
				num2 = ser.Read7BitUInt("num_extra_paths");
			}
			if (num2 <= 0)
			{
				return;
			}
			extra_paths = new List<ExtraPathData>(num2);
			for (int j = 0; j < num2; j++)
			{
				ExtraPathData extraPathData = new ExtraPathData();
				extraPathData.dst_obj_nid = ser.ReadNID("extra_dst_obj_nid_", j);
				extraPathData.original_dst_obj_nid = ser.ReadNID("extra_original_dst_obj_nid_", j);
				int num3 = ser.Read7BitUInt("extra_cnt_", j);
				if (num3 <= 0)
				{
					break;
				}
				if (num3 > 1)
				{
					extraPathData.t = ser.ReadFloat("extra_t_", j);
				}
				extraPathData.range = ser.ReadFloat("extra_range_", j);
				extraPathData.original_range = ser.ReadFloat("extra_original_range_", j);
				extraPathData.path_points = new List<PPos>();
				for (int k = 0; k < num3; k++)
				{
					PPos item2 = ser.ReadPPos("extra_pt_" + j + "_", k);
					extraPathData.path_points.Add(item2);
				}
				extra_paths.Add(extraPathData);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Movement component = obj.GetComponent<Movement>();
			if (component == null)
			{
				return;
			}
			MapObject mapObject = obj as MapObject;
			component.is_stopping_due_to_path_state = true;
			component.Stop(send_state: false);
			component.is_stopping_due_to_path_state = false;
			if (path_points == null)
			{
				return;
			}
			if (path_points.Count == 1)
			{
				mapObject.SetPosition(path_points[0]);
				return;
			}
			Path path = new Path(obj.game, mapObject);
			path.SetPath(path_points);
			path.range = range;
			path.dst_obj = dst_obj_nid.GetObj(obj.game) as MapObject;
			path.original_dst_obj = original_dst_obj_nid.GetObj(obj.game) as MapObject;
			path.original_range = original_range;
			if (path_points.Count > 0)
			{
				path.src_pt = path_points[0];
				path.dst_pt = path_points[path_points.Count - 1];
			}
			path.GetPathPoint(t, out var pt, out var _, advance: true);
			component.pf_path = path;
			component.pf_path.flee = flee;
			if (extra_paths != null)
			{
				for (int i = 0; i < extra_paths.Count; i++)
				{
					component.extra_paths.Add(extra_paths[i].CreateExtraPath(mapObject));
				}
			}
			component.OnPathFindingComplete(path, send_state: false);
			mapObject.SetPosition(pt);
		}
	}

	[Serialization.State(14)]
	public class ReserveState : Serialization.ObjectState
	{
		private Point reserve_pt;

		public static ReserveState Create()
		{
			return new ReserveState();
		}

		public static bool IsNeeded(Object obj)
		{
			MapObject mapObject = obj as MapObject;
			if (!mapObject.CanReserve())
			{
				return false;
			}
			return mapObject.game.path_finding.GetReserved(mapObject.reserve_pt)?.Contains(mapObject) ?? false;
		}

		public override bool InitFrom(Object obj)
		{
			MapObject mapObject = obj as MapObject;
			reserve_pt = mapObject.reserve_pt;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WritePoint(reserve_pt, "reserve_pt");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			reserve_pt = ser.ReadPoint("reserve_pt");
		}

		public override void ApplyTo(Object obj)
		{
			MapObject mapObject = obj as MapObject;
			if (mapObject.game?.path_finding != null)
			{
				mapObject.game.path_finding.ReserveCell(mapObject, reserve_pt);
			}
		}
	}

	public int kingdom_id;

	public PPos position;

	public Point reserve_pt;

	public Movement movement;

	private bool is_teleporting;

	private const int STATES_IDX = 10;

	public Coord reserve_coord => game.path_finding.WorldToReservationGrid(reserve_pt);

	public MapObject(Game game, PPos position, int kingdom_id)
		: base(game)
	{
		this.kingdom_id = kingdom_id;
		SetPosition(position);
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	public virtual void SetKingdom(int kid)
	{
		kingdom_id = kid;
	}

	public override string ToString()
	{
		string text = base.ToString();
		Kingdom kingdom = game.GetKingdom(kingdom_id);
		return text + " (" + kingdom_id + ":" + ((kingdom == null) ? "<no_kingdom>" : kingdom.Name) + ")";
	}

	public virtual void SetPosition(PPos pos)
	{
		position = pos;
	}

	public virtual float GetRadius()
	{
		return 0f;
	}

	public virtual void OnBeginMoving()
	{
		Movement component = GetComponent<Movement>();
		if (component != null && CanReserve())
		{
			if (component.path == null)
			{
				component.AvoidReserves();
			}
			else if (component.path.segments != null && component.path.segments.Count != 0)
			{
				game.path_finding.ReserveCell(this, component.path.segments[component.path.segments.Count - 1].pt);
			}
		}
	}

	public virtual void OnMove(Point prev_pos)
	{
	}

	public virtual void DestinationReached(MapObject dst_obj)
	{
	}

	public virtual bool CanUpdatePath()
	{
		return true;
	}

	public virtual bool ForceRefreshFollowPath()
	{
		return false;
	}

	public virtual void HandleRiverCrossing()
	{
	}

	public virtual bool CanReserve()
	{
		return false;
	}

	public void ReservePosition()
	{
		if (game?.path_finding != null && CanReserve())
		{
			game.path_finding.ReserveCell(this, position);
		}
	}

	public virtual void OnStopMoving()
	{
		movement?.AvoidReserves();
	}

	protected override void OnStart()
	{
		base.OnStart();
		List<MapObject> list = game.path_finding?.GetReserved(reserve_pt);
		if (list == null || !list.Contains(this))
		{
			ReservePosition();
		}
	}

	protected override void OnDestroy()
	{
		ClearReservedForced();
		base.OnDestroy();
	}

	public void ClearReserved()
	{
		if (CanReserve())
		{
			ClearReservedForced();
		}
	}

	private void ClearReservedForced()
	{
		if (game?.path_finding != null)
		{
			game.path_finding.UnreserveCell(this, reserve_coord);
		}
	}

	public virtual void Teleport(PPos pt)
	{
		is_teleporting = true;
		if (movement != null)
		{
			movement.Stop();
		}
		SetPosition(pt);
		ReservePosition();
		is_teleporting = false;
	}

	public virtual bool CanCurrentlyAvoid()
	{
		if (is_teleporting)
		{
			return false;
		}
		return true;
	}

	public virtual float GetReservationRadius()
	{
		return GetRadius();
	}

	public virtual bool AvoidAlreadyReservedPosition(MapObject other)
	{
		return false;
	}

	public virtual PPos VisualPosition()
	{
		return position;
	}

	public virtual bool ForceReserveCheck(MapObject other)
	{
		return false;
	}

	public virtual bool ForceReservePush(MapObject other)
	{
		return false;
	}

	public virtual bool IgnoreCollision(MapObject other)
	{
		float num = game.path_finding.settings.reserve_grid_size * game.path_finding.settings.reserve_grid_avoid_tile_dist_mod;
		return other.reserve_pt.SqrDist(other.position) > num * num;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "pos_x"))
		{
			if (key == "pos_y")
			{
				return position.y;
			}
			return base.GetVar(key, vars, as_value);
		}
		return position.x;
	}

	public MapObject(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new MapObject(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
		PositionState positionState = states.Pop<PositionState>();
		if (positionState != null)
		{
			position = positionState.pos;
		}
		KingdomState kingdomState = states.Pop<KingdomState>();
		if (kingdomState != null)
		{
			kingdom_id = kingdomState.kingdom_id;
		}
	}
}
