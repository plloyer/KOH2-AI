using System;
using System.Collections.Generic;

namespace Logic;

public class Movement : Component
{
	public class ExtraPath
	{
		public Path path;

		public Path pf_path;

		public PPos dst_pt = PPos.Zero;

		public MapObject dst_obj;

		public float dst_range;

		public ExtraPath()
		{
		}

		public ExtraPath(Path path, PPos pt, float range)
		{
			pf_path = path;
			dst_pt = pt;
			dst_range = range;
		}

		public ExtraPath(Path path, MapObject obj, float range)
		{
			pf_path = path;
			dst_obj = obj;
			dst_range = range;
		}
	}

	public List<ExtraPath> extra_paths = new List<ExtraPath>();

	public Path path;

	public Path pf_path;

	public float speed = 1.5f;

	public float modified_speed = 1.5f;

	public float min_radius;

	public float max_radius;

	public const float flee_mult = 1.5f;

	private PPos last_target_pos;

	public bool reserve_locked;

	public bool is_stopping_due_to_target_too_close;

	public bool is_stopping_due_to_path_state;

	private int _battle_side;

	private Time pause_time;

	public Time tmLastMove;

	public PathData.PassableArea.Type allowed_area_types = PathData.PassableArea.Type.All;

	private int battle_side
	{
		get
		{
			if (obj is Squad squad)
			{
				_battle_side = squad.battle_side;
			}
			else
			{
				_battle_side = 0;
			}
			return _battle_side;
		}
	}

	public MapObject last_dst_obj { get; private set; }

	public PathData.PassableArea.Type allowed_non_ladder_areas => allowed_area_types & (PathData.PassableArea.Type)(-3) & (PathData.PassableArea.Type)(-257);

	public PathData.PassableArea.Type allowed_non_flee_areas => allowed_area_types & (PathData.PassableArea.Type)(-9) & (PathData.PassableArea.Type)(-3) & (PathData.PassableArea.Type)(-129);

	public bool paused { get; private set; }

	public Movement(MapObject obj, PathData.PassableArea.Type allowed_area_types)
		: base(obj)
	{
		this.allowed_area_types = allowed_area_types;
	}

	public void Stop(bool send_state = true, bool clearPendingPf = true, bool clearExtraPaths = true, bool advance = true)
	{
		CalcPosition(advance);
		tmLastMove = base.game.time;
		MapObject obj = base.obj as MapObject;
		obj.ReservePosition();
		if (clearPendingPf && pf_path != null)
		{
			last_dst_obj = pf_path.dst_obj;
			pf_path.Clear();
			pf_path = null;
		}
		if (path != null)
		{
			last_dst_obj = path.dst_obj;
			path.Clear();
			path = null;
		}
		if (clearExtraPaths)
		{
			ClearExtraPaths();
		}
		StopUpdating();
		obj.OnStopMoving();
		base.obj.NotifyListeners("path_changed");
		if (send_state && base.game.multiplayer != null)
		{
			base.obj.SendState<MapObject.PathState>();
		}
	}

	public void ClearExtraPaths()
	{
		while (extra_paths.Count > 0)
		{
			int index = extra_paths.Count - 1;
			if (extra_paths[index].pf_path != null)
			{
				extra_paths[index].pf_path.Clear();
			}
			extra_paths[index].pf_path = null;
			if (extra_paths[index].path != null)
			{
				extra_paths[index].path.Clear();
			}
			extra_paths[index].path = null;
			extra_paths.RemoveAt(index);
		}
	}

	public void RefreshModifiedSpeed()
	{
		MapObject mapObject = obj as MapObject;
		modified_speed = speed;
		if (base.game.path_finding.data.GetNode(mapObject.position).road)
		{
			modified_speed *= base.game.path_finding.settings.road_stickiness;
		}
		if (path != null && path.flee)
		{
			modified_speed *= 1.5f;
		}
	}

	public void StopPathfinding()
	{
		if (pf_path != null)
		{
			last_dst_obj = pf_path.dst_obj;
			pf_path.Clear();
			pf_path = null;
		}
	}

	private bool CheckTargetPos(PPos pt)
	{
		if (!base.game.path_finding.CheckValid())
		{
			return false;
		}
		if (pt.paID != 0 && base.game.path_finding.data.pointers.GetPA(pt.paID - 1).type == PathData.PassableArea.Type.Ladder)
		{
			return false;
		}
		if (path == null)
		{
			return true;
		}
		if (path.dst_pt == pt)
		{
			return false;
		}
		return true;
	}

	private bool CheckTargetPos(MapObject t_obj)
	{
		if (!base.game.path_finding.CheckValid())
		{
			return false;
		}
		PPos position = t_obj.position;
		if (position.paID > 0 && position.paID <= base.game.path_finding.data.pas.Length && base.game.path_finding.data.pointers.GetPA(position.paID - 1).type == PathData.PassableArea.Type.Ladder)
		{
			return false;
		}
		if (path?.dst_obj == null)
		{
			return true;
		}
		if (path.dst_obj.position == last_target_pos && t_obj == path.dst_obj && path.src_obj == null)
		{
			return false;
		}
		return true;
	}

	public void Pause(bool pause, bool advance = true)
	{
		if (paused != pause)
		{
			if (pause && !paused)
			{
				pause_time = base.game.time;
			}
			else if (!pause && paused)
			{
				tmLastMove = base.game.time;
			}
			CalcPosition(advance);
			paused = pause;
			if (!paused)
			{
				tmLastMove = base.game.time;
			}
		}
	}

	private bool StopMoveOnPAID()
	{
		bool result = true;
		MapObject mapObject = obj as MapObject;
		if (path != null && path.segments.Count > path.segment_idx + 1)
		{
			PPos pt = path.segments[path.segment_idx + 1].pt;
			if (pt.paID > 0 && base.game.path_finding.data.pas[pt.paID - 1].type == PathData.PassableArea.Type.Teleport)
			{
				Stop(send_state: false, clearPendingPf: true, clearExtraPaths: true, advance: false);
				result = false;
			}
		}
		if (path != null)
		{
			for (int i = 0; i < path.segments.Count; i++)
			{
				PPos pt2 = path.segments[i].pt;
				if (pt2.paID == -1 && pt2.SqrDist(mapObject.position) < 100f)
				{
					Stop(send_state: false, clearPendingPf: true, clearExtraPaths: true, advance: false);
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public void MoveTo(PPos pt, float range = 0f, bool look_ahead = false, bool low_level_only = false, bool ignore_reserve = false, bool force_no_threat = false, bool clear_extra_paths = true)
	{
		if (!CheckTargetPos(pt))
		{
			return;
		}
		ClampIfDestinationOutOfMapBounds(ref pt);
		last_target_pos = pt;
		MapObject mapObject = obj as MapObject;
		bool flag = StopMoveOnPAID();
		if (!obj.IsAuthority())
		{
			if (obj is Army)
			{
				obj.SendEvent(new Army.MoveToPointArmyEvent(pt, range, add: false));
			}
			return;
		}
		if (flag)
		{
			CalcPosition();
			StopPathfinding();
			mapObject.NotifyListeners("path_changed");
		}
		if (mapObject.position.InRange(pt, range))
		{
			mapObject.OnBeginMoving();
			if (!clear_extra_paths && extra_paths.Count > 0)
			{
				Stop(send_state: true, clearPendingPf: true, clearExtraPaths: false);
				PopExtraPath();
			}
			else
			{
				Stop();
				mapObject.DestinationReached(null);
			}
			return;
		}
		tmLastMove = base.game.time;
		if (!ChangeDestination(pt, 0.1f, range, ignore_reserve, force_no_threat, modified_flee: false, clear_extra_paths))
		{
			pf_path = new Path(base.game, mapObject, allowed_area_types, force_no_threat);
			pf_path.original_dst_obj = null;
			pf_path.original_range = range;
			pf_path.min_radius = min_radius;
			pf_path.max_radius = max_radius;
			pf_path.look_ahead = look_ahead;
			pf_path.low_level_only = low_level_only;
			pf_path.ignore_reserve = ignore_reserve;
			pf_path.Find(pt, range);
		}
	}

	public void AddMoveTo(PPos pt, float range = 0f, bool look_ahead = false, bool low_level_only = false, bool ignore_reserve = false, bool force_no_threat = false)
	{
		if (!CheckTargetPos(pt))
		{
			return;
		}
		ClampIfDestinationOutOfMapBounds(ref pt);
		MapObject mapObject = obj as MapObject;
		if (!mapObject.IsAuthority())
		{
			if (mapObject is Army)
			{
				mapObject.SendEvent(new Army.MoveToPointArmyEvent(pt, range, add: true));
			}
			return;
		}
		DropWaypointToGround();
		PPos finalPosition = GetFinalPosition(mapObject);
		Path path = new Path(base.game, mapObject, finalPosition, allowed_area_types, force_no_threat);
		path.original_dst_obj = null;
		path.original_range = range;
		path.min_radius = min_radius;
		path.max_radius = max_radius;
		path.look_ahead = look_ahead;
		path.low_level_only = low_level_only;
		path.ignore_reserve = ignore_reserve;
		path.Find(pt, range);
		extra_paths.Add(new ExtraPath(path, pt, range));
		last_target_pos = pt;
	}

	public void MoveTo(MapObject dst_obj, float range = 0f, bool look_ahead = false, bool ignore_reserve = false, bool force_no_threat = false, bool clear_extra_paths = true)
	{
		MapObject mapObject = obj as MapObject;
		MapObject mapObject2 = ResolveFinalDestination(dst_obj);
		MapObject mapObject3 = ResolveOriginalDestination(mapObject2);
		if (mapObject2 == null || mapObject3 == null || !CheckTargetPos(mapObject2))
		{
			return;
		}
		last_target_pos = mapObject2.position;
		bool flag = StopMoveOnPAID();
		if (!obj.IsAuthority())
		{
			if (obj is Army)
			{
				obj.SendEvent(new Army.MoveToDestinationArmyEvent(dst_obj, range, add: false));
			}
			return;
		}
		if (flag)
		{
			CalcPosition();
			StopPathfinding();
			mapObject.NotifyListeners("path_changed");
		}
		if (!base.game.path_finding.data.OutOfMapBounds(mapObject.VisualPosition()) && !base.game.path_finding.data.OutOfMapBounds(dst_obj.VisualPosition()))
		{
			PathData.Node node = base.game.path_finding.data.GetNode(mapObject.VisualPosition());
			PathData.Node node2 = base.game.path_finding.data.GetNode(dst_obj.VisualPosition());
			if (node.water != node2.water)
			{
				range = 0f;
			}
		}
		if (mapObject.position.InRange(mapObject2.position, range) && PathFinding.CheckReachable(base.game.path_finding.data, mapObject.position, mapObject2.position))
		{
			is_stopping_due_to_target_too_close = true;
			mapObject.OnBeginMoving();
			bool flag2 = CanJoinBattle(mapObject, dst_obj);
			if (!clear_extra_paths && extra_paths.Count > 0 && (dst_obj == null || (!dst_obj.IsEnemy(mapObject) && !flag2)))
			{
				Stop(send_state: true, clearPendingPf: true, clearExtraPaths: false);
				PopExtraPath();
			}
			else
			{
				Stop();
				mapObject.DestinationReached(mapObject2);
			}
			is_stopping_due_to_target_too_close = false;
			return;
		}
		tmLastMove = base.game.time;
		float num = 0.1f;
		if ((path == null || path.GetCutToFutureT(num).paID != -1) && !ChangeDestination(mapObject3, mapObject2, num, range, ignore_reserve, force_no_threat, clear_extra_paths))
		{
			pf_path = new Path(base.game, mapObject, allowed_area_types, force_no_threat);
			pf_path.original_dst_obj = mapObject3;
			pf_path.original_range = range;
			pf_path.min_radius = min_radius;
			pf_path.max_radius = max_radius;
			pf_path.look_ahead = look_ahead;
			pf_path.ignore_reserve = ignore_reserve;
			pf_path.Find(mapObject2, range);
		}
	}

	public void AddMoveTo(MapObject dst_obj, float range = 0f, bool look_ahead = false, bool ignore_reserve = false, bool force_no_threat = false)
	{
		MapObject mapObject = obj as MapObject;
		MapObject mapObject2 = ResolveFinalDestination(dst_obj);
		MapObject mapObject3 = ResolveOriginalDestination(mapObject2);
		if (mapObject2 == null || mapObject3 == null || !CheckTargetPos(mapObject2))
		{
			return;
		}
		if (!mapObject.IsAuthority())
		{
			if (mapObject is Army)
			{
				mapObject.SendEvent(new Army.MoveToDestinationArmyEvent(dst_obj, range, add: true));
			}
			return;
		}
		DropWaypointToGround();
		PPos finalPosition = GetFinalPosition(mapObject);
		Path path = new Path(base.game, mapObject, finalPosition, allowed_area_types, force_no_threat);
		path.original_dst_obj = mapObject3;
		path.original_range = range;
		path.min_radius = min_radius;
		path.max_radius = max_radius;
		path.look_ahead = look_ahead;
		path.ignore_reserve = ignore_reserve;
		path.Find(mapObject2, range);
		extra_paths.Add(new ExtraPath(path, dst_obj, range));
		last_target_pos = mapObject2.position;
	}

	private void DropWaypointToGround()
	{
		if (extra_paths.Count == 0)
		{
			if (path != null)
			{
				if (path.dst_obj is Army)
				{
					path.original_dst_obj = null;
					path.dst_obj = null;
				}
			}
			else if (pf_path != null)
			{
				if (pf_path.dst_obj is Army)
				{
					pf_path.original_dst_obj = null;
					pf_path.dst_obj = null;
				}
			}
			else
			{
				Stop();
			}
		}
		else if (extra_paths[extra_paths.Count - 1].pf_path != null && extra_paths[extra_paths.Count - 1].pf_path.dst_obj is Army)
		{
			if (extra_paths[extra_paths.Count - 1].path != null)
			{
				extra_paths[extra_paths.Count - 1].path.original_dst_obj = null;
				extra_paths[extra_paths.Count - 1].path.dst_obj = null;
			}
			extra_paths[extra_paths.Count - 1].pf_path.original_dst_obj = null;
			extra_paths[extra_paths.Count - 1].pf_path.dst_obj = null;
			extra_paths[extra_paths.Count - 1].dst_obj = null;
			extra_paths[extra_paths.Count - 1].dst_pt = extra_paths[extra_paths.Count - 1].pf_path.dst_pt;
		}
	}

	private PPos GetFinalPosition(MapObject obj, int ignoreLastPaths = 0)
	{
		PPos result = obj.position;
		if (extra_paths.Count > ignoreLastPaths)
		{
			ExtraPath extraPath = extra_paths[extra_paths.Count - 1 - ignoreLastPaths];
			result = ((extraPath.path != null) ? ((extraPath.dst_obj == null) ? extraPath.dst_pt : extraPath.dst_obj.position) : ((extraPath.dst_obj == null) ? extraPath.dst_pt : extraPath.dst_obj.position));
		}
		else if (path != null)
		{
			result = ((path.dst_obj == null) ? path.dst_pt : path.dst_obj.position);
		}
		return result;
	}

	public void PopExtraPath(bool send_state = true)
	{
		if (extra_paths.Count > 0)
		{
			ExtraPath extraPath = extra_paths[0];
			extra_paths.RemoveAt(0);
			if (extraPath.dst_obj != null)
			{
				float num = 0f;
				MapObject mapObject = obj as MapObject;
				MoveTo(range: (!(extraPath.dst_range > 0.001f)) ? (extraPath.dst_obj.GetRadius() + mapObject.GetRadius()) : extraPath.dst_range, dst_obj: extraPath.dst_obj, look_ahead: extraPath.pf_path.look_ahead, ignore_reserve: extraPath.pf_path.ignore_reserve, force_no_threat: extraPath.pf_path.force_no_threat, clear_extra_paths: false);
			}
			else
			{
				MoveTo(extraPath.dst_pt, extraPath.dst_range, extraPath.pf_path.look_ahead, extraPath.pf_path.low_level_only, extraPath.pf_path.ignore_reserve, extraPath.pf_path.force_no_threat, clear_extra_paths: false);
			}
			if (extraPath.pf_path != null)
			{
				extraPath.pf_path.Clear();
			}
			extraPath.pf_path = null;
			if (extraPath.path != null)
			{
				extraPath.path.Clear();
			}
			extraPath.path = null;
			extraPath = null;
			obj.NotifyListeners("path_changed");
		}
	}

	public void ClampIfDestinationOutOfMapBounds(ref PPos pt)
	{
		if (base.game.path_finding != null && base.game.path_finding.data != null)
		{
			PathData data = base.game.path_finding.data;
			if (pt.x < 0f)
			{
				pt.x = 0.1f;
			}
			else if (pt.x / data.settings.tile_size > (float)(data.width - 1))
			{
				pt.x = data.settings.tile_size * (float)data.width - 1f;
			}
			if (pt.y < 0f)
			{
				pt.y = 0.1f;
			}
			else if (pt.y / data.settings.tile_size > (float)(data.height - 1))
			{
				pt.y = data.settings.tile_size * (float)data.height - 1f;
			}
		}
	}

	public void FleeFrom(PPos pt, float range)
	{
		if (!obj.IsAuthority())
		{
			obj.SendEvent(new Army.FleeFromArmyEvent(pt, range));
			return;
		}
		CalcPosition();
		MapObject mapObject = obj as MapObject;
		StopPathfinding();
		mapObject.NotifyListeners("path_changed");
		if (!mapObject.position.InRange(pt, range))
		{
			Stop();
			mapObject.OnBeginMoving();
			mapObject.DestinationReached(null);
			return;
		}
		tmLastMove = base.game.time;
		if (!ChangeDestination(pt, 0.1f, range))
		{
			pf_path = new Path(base.game, mapObject, allowed_non_flee_areas);
			pf_path.min_radius = min_radius;
			pf_path.max_radius = max_radius;
			pf_path.flee = true;
			pf_path.Find(pt, range);
		}
	}

	public void FleeTo(PPos pt, float range)
	{
		if (!obj.IsAuthority())
		{
			obj.SendEvent(new Army.FleeFromArmyEvent(pt, range));
			return;
		}
		CalcPosition();
		MapObject mapObject = obj as MapObject;
		StopPathfinding();
		mapObject.NotifyListeners("path_changed");
		if (mapObject.position.InRange(pt, range))
		{
			Stop();
			mapObject.OnBeginMoving();
			mapObject.DestinationReached(null);
			return;
		}
		tmLastMove = base.game.time;
		pf_path = new Path(base.game, mapObject, allowed_non_flee_areas);
		pf_path.min_radius = min_radius;
		pf_path.max_radius = max_radius;
		pf_path.modified_flee = true;
		pf_path.Find(pt, range);
	}

	private bool ChangeDestination(MapObject newOriginal_dst_obj, MapObject newDst_obj, float future_t, float range, bool ignore_reserve = false, bool force_no_threat = false, bool clear_extra_paths = true)
	{
		if (path == null)
		{
			return false;
		}
		if (this?.obj == null || !obj.IsValid())
		{
			return false;
		}
		if (obj is Squad)
		{
			return false;
		}
		StopPathfinding();
		CalcPosition();
		if (newDst_obj == null)
		{
			Game.Log("Destination object not valid!", Game.LogType.Warning);
			return false;
		}
		if (path == null)
		{
			return false;
		}
		if (clear_extra_paths)
		{
			ClearExtraPaths();
		}
		MapObject mapObject = obj as MapObject;
		mapObject.NotifyListeners("path_changed");
		float cut_t = path.t + future_t;
		PPos src_pt = path.CutTo(cut_t);
		pf_path = new Path(base.game, mapObject, allowed_area_types, force_no_threat);
		pf_path.original_dst_obj = newOriginal_dst_obj;
		if (newOriginal_dst_obj == path.original_dst_obj)
		{
			pf_path.original_range = path.original_range;
		}
		else
		{
			pf_path.original_range = range;
		}
		pf_path.src_pt = src_pt;
		pf_path.min_radius = min_radius;
		pf_path.max_radius = max_radius;
		pf_path.ignore_reserve = ignore_reserve;
		pf_path.Find(newDst_obj, range, check_reserve: true);
		last_target_pos = newDst_obj.position;
		return true;
	}

	private bool ChangeDestination(PPos newDst_pt, float future_t, float range, bool ignore_reserve = false, bool force_no_threat = false, bool modified_flee = false, bool clear_extra_paths = true)
	{
		if (path == null)
		{
			return false;
		}
		if (this?.obj == null || !obj.IsValid())
		{
			return false;
		}
		if (obj is Squad)
		{
			return false;
		}
		StopPathfinding();
		CalcPosition();
		if (path == null)
		{
			return false;
		}
		if (clear_extra_paths)
		{
			ClearExtraPaths();
		}
		MapObject mapObject = obj as MapObject;
		mapObject.NotifyListeners("path_changed");
		float cut_t = path.t + future_t;
		PPos src_pt = path.CutTo(cut_t);
		pf_path = new Path(base.game, mapObject, allowed_area_types, force_no_threat);
		if (path.original_dst_obj == null)
		{
			pf_path.original_range = path.original_range;
		}
		else
		{
			pf_path.original_range = range;
		}
		pf_path.src_pt = src_pt;
		pf_path.min_radius = min_radius;
		pf_path.max_radius = max_radius;
		pf_path.ignore_reserve = ignore_reserve;
		pf_path.modified_flee = modified_flee;
		pf_path.Find(newDst_pt, range, check_reserve: true);
		return true;
	}

	public void OnPathFindingComplete(Path completed_path, bool send_state = true)
	{
		if (!(obj is MapObject mapObject))
		{
			return;
		}
		if (pf_path == null && path == null && extra_paths.Count > 0)
		{
			PopExtraPath(send_state);
			return;
		}
		if (pf_path == completed_path)
		{
			if (!pf_path.IsValid())
			{
				if (path == null)
				{
					Stop();
					return;
				}
				bool flag = false;
				float offset_t = 1f;
				if (path == null || path.GetCutToFutureT(offset_t).paID != -1)
				{
					flag = ((path.dst_obj == null) ? ChangeDestination(path.dst_pt, 1f, path.range, path.ignore_reserve) : ChangeDestination(path.original_dst_obj, path.dst_obj, 1f, path.range, path.ignore_reserve));
					if (!flag && pf_path != null)
					{
						last_dst_obj = pf_path.dst_obj;
						pf_path.Clear();
						pf_path = null;
					}
				}
				return;
			}
			if (path != null && !(mapObject is Squad))
			{
				if (!path.Append(pf_path, clearOldSegments: true))
				{
					path = pf_path;
				}
			}
			else
			{
				path = pf_path;
			}
			path.ignore_reserve = pf_path.ignore_reserve;
			last_dst_obj = path.dst_obj;
			mapObject.OnBeginMoving();
			pf_path = null;
			RefreshModifiedSpeed();
			ScheduleUpdate();
			mapObject.NotifyListeners("path_changed");
			if (send_state && base.game.multiplayer != null)
			{
				mapObject.SendState<MapObject.PathState>();
			}
			return;
		}
		for (int i = 0; i < extra_paths.Count; i++)
		{
			if (extra_paths[i].pf_path == completed_path)
			{
				extra_paths[i].path = completed_path;
			}
		}
		mapObject.NotifyListeners("path_changed");
		if (send_state && base.game.multiplayer != null)
		{
			mapObject.SendState<MapObject.PathState>();
		}
	}

	public bool AvoidReserves()
	{
		using (Game.Profile("AvoidReserves"))
		{
			if (is_stopping_due_to_target_too_close)
			{
				return false;
			}
			if (is_stopping_due_to_path_state)
			{
				return false;
			}
			if (base.game?.path_finding?.data?.reservation_grid == null)
			{
				return false;
			}
			if (reserve_locked)
			{
				return false;
			}
			if (path != null && path.dst_pt.paID != 0)
			{
				if (path.dst_pt.paID < 0)
				{
					return false;
				}
				if ((base.game.path_finding.data.pointers.GetPA(path.dst_pt.paID - 1).type & PathData.PassableArea.Type.Ground) == 0)
				{
					return false;
				}
			}
			if (pf_path != null)
			{
				return false;
			}
			MapObject mapObject = obj as MapObject;
			if (!mapObject.CanCurrentlyAvoid())
			{
				return false;
			}
			float num = base.game.path_finding.settings.reserve_grid_size * base.game.path_finding.settings.reserve_grid_avoid_tile_dist_mod;
			if (mapObject.reserve_pt.SqrDist(mapObject.position) > num * num)
			{
				return false;
			}
			bool flag = reserve_locked;
			reserve_locked = true;
			Coord coord = base.game.path_finding.WorldToReservationGrid(mapObject.reserve_pt);
			bool flag2 = true;
			float reservationRadius = mapObject.GetReservationRadius();
			float num2 = base.game.path_finding.CalcReservePrio(mapObject, mapObject.reserve_pt);
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					Coord coord2 = new Coord(coord.x + i, coord.y + j);
					if (!base.game.path_finding.InReserveBounds(coord2))
					{
						continue;
					}
					List<MapObject> reserved = base.game.path_finding.GetReserved(coord2);
					for (int k = 0; k < reserved.Count; k++)
					{
						MapObject mapObject2 = reserved[k];
						if (mapObject2 == mapObject || mapObject.IgnoreCollision(mapObject2))
						{
							continue;
						}
						float num3 = mapObject.reserve_pt.SqrDist(mapObject2.reserve_pt);
						float reservationRadius2 = mapObject2.GetReservationRadius();
						float num4 = reservationRadius + reservationRadius2;
						float num5 = num4 * num4;
						if (num3 < num5)
						{
							float num6 = base.game.path_finding.CalcReservePrio(mapObject2, mapObject2.reserve_pt);
							if ((!mapObject.ForceReservePush(mapObject2) && num2 > num6) || mapObject.ForceReserveCheck(mapObject2))
							{
								flag2 = false;
								continue;
							}
							Movement movement = mapObject2.movement;
							flag2 = flag2 && movement != null && movement.AvoidReserves();
						}
					}
				}
			}
			if (!flag)
			{
				reserve_locked = false;
			}
			if (!flag2)
			{
				if (pf_path != null)
				{
					return true;
				}
				if (path == null)
				{
					pf_path = new Path(base.game, mapObject, allowed_area_types);
					pf_path.original_dst_obj = null;
					pf_path.original_range = 0f;
					pf_path.min_radius = min_radius;
					pf_path.max_radius = max_radius;
					pf_path.ignore_reserve = false;
					pf_path.Find(mapObject.position, pf_path.range, check_reserve: true);
				}
				else
				{
					MapObject mapObject3 = ResolveFinalDestination(path.original_dst_obj);
					if (mapObject3 != null)
					{
						float original_range = path.original_range;
						float num7 = 1f;
						if (path != null && path.GetCutToFutureT(num7).paID == -1)
						{
							return false;
						}
						if (ChangeDestination(path.original_dst_obj, mapObject3, num7, original_range, path.ignore_reserve, force_no_threat: false, clear_extra_paths: false))
						{
							return true;
						}
						if (path == null)
						{
							return false;
						}
						pf_path = new Path(base.game, mapObject, allowed_area_types);
						pf_path.original_dst_obj = path.original_dst_obj;
						pf_path.original_range = path.original_range;
						pf_path.min_radius = min_radius;
						pf_path.max_radius = max_radius;
						pf_path.ignore_reserve = path.ignore_reserve;
						pf_path.Find(mapObject3, original_range, check_reserve: true);
					}
					else
					{
						float num8 = 1f;
						if (path != null && path.GetCutToFutureT(num8).paID == -1)
						{
							return false;
						}
						if (path != null && path.flee && path.segments.Count > 0)
						{
							PPos pt = path.segments[path.segments.Count - 1].pt;
							float radius = mapObject.GetRadius();
							if (ChangeDestination(pt, num8, radius, path.ignore_reserve, force_no_threat: false, modified_flee: true))
							{
								return true;
							}
							pf_path = new Path(base.game, mapObject, allowed_area_types);
							pf_path.original_dst_obj = null;
							pf_path.original_range = radius;
							pf_path.min_radius = min_radius;
							pf_path.max_radius = max_radius;
							pf_path.ignore_reserve = path.ignore_reserve;
							pf_path.modified_flee = true;
							pf_path.Find(pt, pf_path.original_range, check_reserve: true);
							return true;
						}
						if (ChangeDestination(path.dst_pt, num8, path.range, path.ignore_reserve, force_no_threat: false, modified_flee: false, clear_extra_paths: false))
						{
							return true;
						}
						if (path == null)
						{
							return false;
						}
						pf_path = new Path(base.game, mapObject, allowed_area_types);
						pf_path.original_dst_obj = null;
						pf_path.original_range = path.original_range;
						pf_path.min_radius = min_radius;
						pf_path.max_radius = max_radius;
						pf_path.ignore_reserve = path.ignore_reserve;
						pf_path.Find(path.dst_pt, path.range, check_reserve: true);
					}
				}
				return true;
			}
			return false;
		}
	}

	public MapObject ResolveFinalDestination(MapObject original_dst_obj)
	{
		if (original_dst_obj == null)
		{
			return null;
		}
		Army army = original_dst_obj as Army;
		if (army?.battle != null && army.battle.IsValid())
		{
			return army.battle;
		}
		Settlement settlement = original_dst_obj as Settlement;
		if (settlement?.battle != null && settlement.battle.IsValid())
		{
			return settlement.battle;
		}
		if (original_dst_obj is Squad squad && !squad.IsValid() && squad.main_squad != null && squad.main_squad.IsValid() && !squad.main_squad.IsDefeated())
		{
			return squad.main_squad;
		}
		return original_dst_obj;
	}

	public MapObject ResolveOriginalDestination(MapObject original_dst_obj)
	{
		if (!(original_dst_obj is Battle battle) || !battle.IsValid())
		{
			return original_dst_obj;
		}
		int joinSide = battle.GetJoinSide(obj);
		if (joinSide == 0 || joinSide == 1)
		{
			Army army = battle.GetArmy(1 - joinSide);
			if (army != null && army.IsValid())
			{
				return army;
			}
		}
		if (battle.settlement != null)
		{
			return battle.settlement;
		}
		return battle;
	}

	private bool UpdatePath()
	{
		if (!base.game.IsAuthority())
		{
			return true;
		}
		if (path == null)
		{
			return false;
		}
		MapObject mapObject = obj as MapObject;
		MapObject mapObject2 = ResolveFinalDestination(path.original_dst_obj);
		if (path.dst_obj != null && path.original_dst_obj != null && !path.dst_obj.IsValid() && path.original_dst_obj.IsValid())
		{
			path.original_dst_obj = mapObject2;
			path.dst_obj = mapObject2;
		}
		if (mapObject2 is Battle battle)
		{
			battle.AddIntendedReinforcement(mapObject as Army);
		}
		float original_range = path.original_range;
		if (AvoidReserves())
		{
			return true;
		}
		if (mapObject2 == null)
		{
			return true;
		}
		if (!path.original_dst_obj.IsValid() && mapObject2.IsValid())
		{
			path.original_dst_obj = mapObject2;
			path.dst_obj = mapObject2;
		}
		bool flag = mapObject2.IsValid();
		if (flag && mapObject2 is Squad squad)
		{
			flag = !squad.IsDefeated();
		}
		if (!flag)
		{
			Stop();
			return false;
		}
		bool flag2 = path.src_obj.ForceRefreshFollowPath();
		if (!CheckTargetPos(mapObject2) && !flag2)
		{
			return true;
		}
		if (!path.src_obj.CanUpdatePath())
		{
			return true;
		}
		if (pf_path != null)
		{
			return true;
		}
		if (!flag2)
		{
			float num = path.dst_pt.Dist(mapObject2.position);
			if (num < 0.5f)
			{
				return true;
			}
			if (num < Math.Min(path.path_len - path.t, 100f) / 10f)
			{
				return true;
			}
		}
		bool flag3 = CanJoinBattle(mapObject, mapObject2);
		if (mapObject.position.InRange(mapObject2.position, original_range))
		{
			if (!mapObject2.IsEnemy(mapObject) && !flag3 && extra_paths.Count > 0)
			{
				Stop(send_state: true, clearPendingPf: true, clearExtraPaths: false);
				PopExtraPath();
				return true;
			}
			Stop();
			mapObject.DestinationReached(mapObject2);
			return false;
		}
		float num2 = 1f;
		if (path != null && path.GetCutToFutureT(num2).paID == -1)
		{
			return false;
		}
		if (!mapObject2.IsEnemy(mapObject) && !flag3 && extra_paths.Count > 0)
		{
			PopExtraPath();
			return true;
		}
		if (ChangeDestination(path.original_dst_obj, mapObject2, num2, original_range))
		{
			return true;
		}
		pf_path = new Path(base.game, mapObject, allowed_area_types);
		pf_path.original_dst_obj = path.original_dst_obj;
		pf_path.original_range = path.original_range;
		pf_path.min_radius = min_radius;
		pf_path.max_radius = max_radius;
		pf_path.is_updated_path = true;
		pf_path.Find(mapObject2, original_range);
		last_target_pos = mapObject2.position;
		return true;
	}

	private static bool CanJoinBattle(MapObject obj, MapObject dst_obj)
	{
		bool result = false;
		Army army = obj as Army;
		Battle battle = dst_obj as Battle;
		Army army2 = dst_obj as Army;
		Settlement settlement = dst_obj as Settlement;
		if (battle != null && battle.CanJoin(army))
		{
			result = true;
		}
		else if (army2 != null && army2.battle != null && army2.battle.CanJoin(army))
		{
			result = true;
		}
		else if (settlement != null && settlement.battle != null && settlement.battle.CanJoin(army))
		{
			result = true;
		}
		return result;
	}

	private void UpdateExtraPaths()
	{
		if (!base.game.IsAuthority() || !(obj is MapObject mapObject))
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < extra_paths.Count; i++)
		{
			if (extra_paths[i].pf_path?.dst_obj != null && !extra_paths[i].pf_path.dst_obj.IsValid())
			{
				if (extra_paths[i].pf_path != null)
				{
					extra_paths[i].pf_path.Clear();
				}
				if (extra_paths[i].path != null)
				{
					extra_paths[i].path.Clear();
				}
				extra_paths.RemoveAt(i);
				i--;
				flag = true;
			}
		}
		if (extra_paths.Count > 0)
		{
			ExtraPath extraPath = extra_paths[extra_paths.Count - 1];
			if (extraPath.path != null && extraPath.path.original_dst_obj != null)
			{
				float num = extraPath.path.dst_pt.Dist(extraPath.path.original_dst_obj.position);
				if (num > 0.5f && num > Math.Min(extraPath.path.path_len - extraPath.path.t, 100f) / 10f)
				{
					MapObject original_dst_obj = extraPath.path.original_dst_obj;
					float dst_range = extraPath.dst_range;
					if (extraPath.pf_path != null)
					{
						extraPath.pf_path.Clear();
						extraPath.pf_path = null;
					}
					if (extraPath.path != null)
					{
						extraPath.path.Clear();
						extraPath.path = null;
					}
					PPos finalPosition = GetFinalPosition(mapObject, 1);
					extraPath.pf_path = new Path(base.game, mapObject, finalPosition, allowed_area_types);
					extraPath.pf_path.original_dst_obj = original_dst_obj;
					extraPath.pf_path.original_range = dst_range;
					extraPath.pf_path.src_pt = finalPosition;
					extraPath.pf_path.min_radius = min_radius;
					extraPath.pf_path.max_radius = max_radius;
					extraPath.pf_path.ignore_reserve = false;
					extraPath.pf_path.Find(original_dst_obj, dst_range, check_reserve: true);
					last_target_pos = original_dst_obj.position;
					flag = true;
				}
			}
		}
		if (flag)
		{
			mapObject.NotifyListeners("path_changed");
			if (base.game.multiplayer != null)
			{
				mapObject.SendState<MapObject.PathState>();
			}
		}
	}

	private void CalcPosition(bool advance = true)
	{
		CalcPosition(advance, out var _, out var _);
	}

	public void CalcPosition(out PPos pt, out float path_t)
	{
		CalcPosition(advance_path: false, out pt, out path_t);
	}

	private void CalcPosition(bool advance_path, out PPos pt, out float path_t)
	{
		MapObject mapObject = obj as MapObject;
		if (mapObject?.game?.path_finding?.data != null && mapObject.game.path_finding.data.processing)
		{
			pt = mapObject.position;
			if (path != null)
			{
				path_t = path.t;
			}
			else
			{
				path_t = 0f;
			}
			return;
		}
		if (path == null)
		{
			pt = mapObject.position;
			path_t = 0f;
			return;
		}
		float num = (paused ? 0f : (base.game.time - tmLastMove));
		if (num <= 0f)
		{
			pt = mapObject.position;
			path_t = path.t;
			return;
		}
		if (base.game?.path_finding?.data == null || !base.game.path_finding.data.initted)
		{
			pt = mapObject.position;
			path_t = path.t;
			return;
		}
		if (path.segments.Count > path.segment_idx + 1)
		{
			PPos pt2 = path.segments[path.segment_idx + 1].pt;
			if (pt2.paID > 0 && base.game.path_finding.data.pas.Length > pt2.paID && base.game.path_finding.data.pas[pt2.paID - 1].type == PathData.PassableArea.Type.Teleport)
			{
				pt = mapObject.position;
				path_t = path.t;
				if (mapObject is Army army)
				{
					army.HandleWater(army.realm_in);
				}
				return;
			}
			if (path.segments[path.segment_idx].pt.paID == -1)
			{
				pt = mapObject.position;
				path_t = path.t;
				mapObject.HandleRiverCrossing();
				return;
			}
		}
		if (mapObject.position.paID > 0 && base.game.path_finding.data.pas.Length < mapObject.position.paID)
		{
			pt = mapObject.position;
			path_t = path.t;
			return;
		}
		RefreshModifiedSpeed();
		path_t = path.t + modified_speed * num;
		if (mapObject is Squad)
		{
			int num2 = path.FindSegment(path_t);
			for (int i = path.segment_idx; i <= num2; i++)
			{
				Path.Segment segment = path.segments[i];
				if (segment.pt.paID > 0 && base.game.path_finding.data.pointers.GetPA(segment.pt.paID - 1).type == PathData.PassableArea.Type.Ladder)
				{
					path_t = segment.t;
					pt = segment.pt;
					if (advance_path)
					{
						path.waiting_for_area = true;
						tmLastMove = base.game.time;
						path.t = path_t;
						path.segment_idx = i;
						mapObject.SetPosition(pt);
					}
					return;
				}
			}
		}
		path.GetPathPoint(path_t, out pt, out var _, advance_path);
		if (advance_path)
		{
			if (pt.paID > 0 && base.game.path_finding.data.pas.Length > pt.paID && base.game.path_finding.data.pas[pt.paID - 1].type == PathData.PassableArea.Type.Teleport)
			{
				pt = mapObject.position;
				path_t = path.t;
			}
			else
			{
				tmLastMove = base.game.time;
				mapObject.SetPosition(pt);
			}
		}
	}

	public void AdvanceTo(float path_t)
	{
		MapObject obj = base.obj as MapObject;
		path.GetPathPoint(path_t, out var pt, out var _, advance: true);
		tmLastMove = base.game.time;
		obj.SetPosition(pt);
	}

	public bool IsMoving(bool pf_path_too = true)
	{
		if (path == null || !(path.path_len > 0.1f))
		{
			if (pf_path_too)
			{
				return pf_path != null;
			}
			return false;
		}
		return true;
	}

	public void ScheduleUpdate()
	{
		if (obj.IsValid())
		{
			UpdateInBatch(base.game.update_half_sec);
		}
	}

	public override void OnUpdate()
	{
		MapObject mapObject = obj as MapObject;
		if (!mapObject.IsValid())
		{
			return;
		}
		Point prev_pos = mapObject.position;
		CalcPosition();
		if (mapObject.IsAuthority() && path != null)
		{
			if (path.IsDone() && pf_path == null)
			{
				MapObject dst_obj = path.dst_obj;
				bool flag = CanJoinBattle(mapObject, dst_obj);
				if (extra_paths.Count > 0 && (dst_obj == null || (!dst_obj.IsEnemy(mapObject) && !flag)))
				{
					Stop(send_state: true, clearPendingPf: true, clearExtraPaths: false);
					PopExtraPath();
				}
				else
				{
					Stop();
					mapObject.DestinationReached(dst_obj);
				}
				return;
			}
			ScheduleUpdate();
		}
		UpdateExtraPaths();
		if (UpdatePath())
		{
			mapObject.OnMove(prev_pos);
		}
	}

	public static bool PlanningToMoveThroughPAIDs(Path path, List<int> paids, float max_offset = -1f)
	{
		if (path?.segments == null)
		{
			return false;
		}
		float num = path.t + max_offset;
		for (int i = path.segment_idx; i < path.segments.Count; i++)
		{
			Path.Segment segment = path.segments[i];
			if (max_offset >= 0f && segment.t >= num)
			{
				return false;
			}
			if (segment.pt.paID != 0 && paids.Contains(segment.pt.paID))
			{
				return true;
			}
		}
		return false;
	}

	public bool PlanningToMoveThroughPAIDs(List<int> paids, float max_offset = -1f)
	{
		return PlanningToMoveThroughPAIDs(path, paids, max_offset);
	}
}
