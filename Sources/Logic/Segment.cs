using System;
using System.Collections.Generic;

namespace Logic;

public class Path
{
	public enum State
	{
		Initial,
		Pending,
		Failed,
		Succeeded,
		Stopped
	}

	public struct Segment
	{
		public float t;

		public PPos pt;

		public PPos dir;

		public override string ToString()
		{
			return $"pt: {pt}, dir: {dir}, t: {t}";
		}
	}

	public Game game;

	public State state;

	public PPos src_pt = PPos.Invalid;

	public MapObject src_obj;

	public PPos dst_pt = PPos.Invalid;

	public MapObject dst_obj;

	public float range;

	public float original_range;

	public MapObject original_dst_obj;

	public float min_radius;

	public float max_radius;

	public bool flee;

	public bool look_ahead;

	public bool can_enter_water = true;

	public bool low_level_only;

	public PathData.PassableArea.Type allowed_area_types;

	public int battle_side = -1;

	public bool ignore_reserve;

	public bool can_reserve;

	public bool check_reserve;

	public bool check_threat;

	public bool check_anti_cavalry_threat;

	public bool check_anti_ranged_threat;

	public bool force_no_threat;

	public bool waiting_for_area;

	public bool was_inside_wall;

	public bool modified_flee;

	public bool is_updated_path;

	public List<Segment> segments;

	public float path_len;

	public float path_eval;

	public float t;

	public int segment_idx;

	public bool use_baked_paths;

	public bool use_max_steps_possible;

	public System.Action onPathFindingComplete;

	public bool no_realms
	{
		get
		{
			if (game == null)
			{
				return true;
			}
			return !game.IsMain();
		}
	}

	public Path(Game game, PPos src_pt, PathData.PassableArea.Type allowed_areas = PathData.PassableArea.Type.All, bool force_no_threat = false)
	{
		this.game = game;
		this.src_pt = src_pt;
		allowed_area_types = allowed_areas;
		this.force_no_threat = force_no_threat;
	}

	public Path(Game game, MapObject src_obj, PathData.PassableArea.Type allowed_areas = PathData.PassableArea.Type.All, bool force_no_threat = false)
	{
		this.game = game;
		this.src_obj = src_obj;
		src_pt = src_obj.position;
		allowed_area_types = allowed_areas;
		if (src_obj is Squad squad)
		{
			battle_side = squad.battle_side;
		}
		if (src_obj != null)
		{
			can_reserve = src_obj.CanReserve();
		}
		this.force_no_threat = force_no_threat;
	}

	public Path(Game game, MapObject src_obj, PPos src_pt, PathData.PassableArea.Type allowed_areas = PathData.PassableArea.Type.All, bool force_no_threat = false)
	{
		this.game = game;
		this.src_obj = src_obj;
		this.src_pt = src_pt;
		allowed_area_types = allowed_areas;
		if (src_obj != null)
		{
			can_reserve = src_obj.CanReserve();
		}
		this.force_no_threat = force_no_threat;
	}

	public void Find(PPos dst_pt, float range = 0f, bool check_reserve = false)
	{
		using (Game.Profile("FindPath"))
		{
			this.dst_pt = dst_pt;
			dst_obj = null;
			this.range = range;
			if (game?.path_finding == null)
			{
				this.check_reserve = false;
			}
			else
			{
				this.check_reserve = check_reserve || src_pt.SqrDist(this.dst_pt) < game.path_finding.settings.reserve_grid_size * game.path_finding.settings.reserve_grid_avoid_tile_dist_mod * game.path_finding.settings.reserve_grid_size * game.path_finding.settings.reserve_grid_avoid_tile_dist_mod;
			}
			Begin();
		}
	}

	public void Find(MapObject dst_obj, float range = 0f, bool check_reserve = false)
	{
		using (Game.Profile("FindPath"))
		{
			dst_pt = dst_obj.position;
			this.dst_obj = dst_obj;
			this.range = range;
			if (dst_obj is Squad squad)
			{
				if (src_obj is Squad squad2 && squad.climbing && squad2.is_inside_walls_or_on_walls)
				{
					dst_pt = squad.climbing_pos;
				}
				else if (squad.NumTroops() > 1 && squad.position.paID == 0)
				{
					dst_pt = squad.avg_troops_pos;
				}
			}
			if (game?.path_finding == null)
			{
				this.check_reserve = false;
			}
			else
			{
				this.check_reserve = check_reserve || src_pt.SqrDist(dst_pt) < game.path_finding.settings.reserve_grid_size * game.path_finding.settings.reserve_grid_avoid_tile_dist_mod * game.path_finding.settings.reserve_grid_size * game.path_finding.settings.reserve_grid_avoid_tile_dist_mod;
			}
			Begin();
		}
	}

	private void Begin()
	{
		if (dst_pt == Point.Zero)
		{
			Game.Log($"Path finding towards from {src_pt} to 0,0 ; src_obj: {src_obj}, dst_obj: {dst_obj}", Game.LogType.Warning);
			return;
		}
		if (game == null || game.path_finding == null)
		{
			state = State.Failed;
			((src_obj == null) ? null : src_obj.movement)?.OnPathFindingComplete(this);
			onPathFindingComplete?.Invoke();
			return;
		}
		if (src_obj is Squad squad)
		{
			was_inside_wall = squad.is_inside_walls_or_on_walls;
		}
		game.path_finding.Add(this);
	}

	public void Clear()
	{
		if (game != null && game.path_finding != null)
		{
			game.path_finding.Del(this);
		}
	}

	public void SetPath(List<PPos> points)
	{
		if (points != null && points.Count >= 2)
		{
			segments = new List<Segment>(points.Count);
			path_len = 0f;
			Segment item;
			for (int i = 0; i < points.Count - 1; i++)
			{
				item = default(Segment);
				item.t = path_len;
				item.pt = points[i];
				item.dir = points[i + 1] - item.pt;
				float num = item.dir.Normalize();
				path_len += num;
				segments.Add(item);
			}
			item = new Segment
			{
				t = path_len,
				pt = points[points.Count - 1],
				dir = segments[segments.Count - 1].dir
			};
			segments.Add(item);
			t = 0f;
			segment_idx = 0;
		}
	}

	public bool Append(Path newPath, bool clearOldSegments = false)
	{
		if (newPath == null || newPath.segments.Count == 0)
		{
			return false;
		}
		if (segments.Count == 0)
		{
			segments = newPath.segments;
			path_len = newPath.path_len;
			segment_idx = newPath.segment_idx;
			t = 0f;
		}
		else
		{
			int num = segments.Count - 1;
			int num2 = num;
			if (clearOldSegments)
			{
				int num3 = FindSegment(t - 32f) - 2;
				if (num3 > 0 && num3 < segments.Count - 1)
				{
					for (int i = num3; i < segments.Count; i++)
					{
						if (segments[i].pt.paID != 0)
						{
							return false;
						}
					}
					segments = segments.GetRange(num3, segments.Count - num3);
					num -= num3;
					num2 -= num3;
					float num4 = segments[0].t;
					for (int j = 0; j < segments.Count; j++)
					{
						Segment value = segments[j];
						value.t -= num4;
						segments[j] = value;
					}
					path_len -= num4;
					t -= num4;
					segment_idx -= num3;
					if (segment_idx < 0)
					{
						segment_idx = 0;
					}
				}
			}
			segments.AddRange(newPath.segments);
			if (segments[num].pt == segments[num + 1].pt)
			{
				segments.RemoveAt(num);
			}
			else
			{
				Segment value2 = segments[num];
				value2.dir = segments[num + 1].pt - segments[num].pt;
				path_len += value2.dir.Normalize();
				value2.t = path_len;
				num2 = num + 1;
				segments[num] = value2;
			}
			for (int k = num2; k < segments.Count; k++)
			{
				Segment value3 = segments[k];
				value3.t += path_len;
				segments[k] = value3;
			}
			path_len += newPath.path_len;
		}
		flee = newPath.flee;
		src_pt = newPath.src_pt;
		dst_obj = newPath.dst_obj;
		dst_pt = newPath.dst_pt;
		range = newPath.range;
		path_eval = newPath.path_eval;
		original_dst_obj = newPath.original_dst_obj;
		original_range = newPath.original_range;
		return true;
	}

	public void CutTo(int idx)
	{
		if (idx >= segments.Count)
		{
			Game.Log("PathFinding.CutTo(index out of range) ", Game.LogType.Error);
			return;
		}
		segments = segments.GetRange(0, idx + 1);
		path_len = segments[segments.Count - 1].t;
	}

	public PPos CutTo(float cut_t)
	{
		if (cut_t >= path_len)
		{
			return segments[segments.Count - 1].pt;
		}
		GetPathPoint(cut_t, out var pt, out var ptDest);
		int num = FindSegment(cut_t);
		Segment item = segments[num + 1];
		item.pt = pt;
		item.dir = ptDest - pt;
		item.dir.Normalize();
		item.t = cut_t;
		CutTo(num);
		segments.Add(item);
		path_len = item.t;
		dst_pt = item.pt + item.dir;
		return pt;
	}

	public PPos GetCutToFutureT(float offset_t)
	{
		float num = offset_t + t;
		if (num >= path_len)
		{
			return segments[segments.Count - 1].pt;
		}
		GetPathPoint(num, out var pt, out var _);
		return pt;
	}

	public bool IsValid()
	{
		if (segments != null)
		{
			return segments.Count > 1;
		}
		return false;
	}

	public bool IsDone()
	{
		if (waiting_for_area)
		{
			return false;
		}
		if (t >= path_len)
		{
			if (src_obj is Army army)
			{
				return !army.water_crossing.running;
			}
			return true;
		}
		return false;
	}

	public void GetSegmentPoints(int seg_idx, float fOffset, out PPos ptStart, out PPos ptEnd)
	{
		Segment segment = segments[seg_idx];
		Segment segment2 = segments[seg_idx + 1];
		if (fOffset == 0f)
		{
			ptStart = segment.pt;
			ptEnd = segment2.pt;
			return;
		}
		PPos pPos = segment.dir.Right() * fOffset;
		PPos pPos2 = segment2.dir.Right() * fOffset;
		ptStart = segment.pt + pPos;
		ptEnd = segment2.pt + pPos2;
	}

	public void ClampPoint(ref PPos pt)
	{
		if (game?.path_finding?.settings != null)
		{
			pt = game.path_finding.ClampPositionToWorldBounds(pt);
		}
	}

	public int FindSegment(float path_t)
	{
		int num = segment_idx;
		float num2 = segments[num].t;
		bool flag;
		if (path_t < num2)
		{
			if (num2 - path_t < path_t)
			{
				flag = true;
			}
			else
			{
				num2 = 0f;
				num = 0;
				flag = false;
			}
		}
		else if (path_t - num2 < path_len - path_t)
		{
			flag = false;
		}
		else
		{
			num = segments.Count - 1;
			num2 = segments[num].t;
			flag = true;
		}
		if (flag)
		{
			while (true)
			{
				if (num < 0)
				{
					return 0;
				}
				num2 = segments[num].t;
				if (num2 <= path_t)
				{
					break;
				}
				num--;
			}
			return num;
		}
		do
		{
			num++;
			if (num >= segments.Count)
			{
				return num - 1;
			}
			num2 = segments[num].t;
		}
		while (!(num2 > path_t));
		return num - 1;
	}

	public bool GetPathPoint(float path_t, out PPos pt, out PPos ptDest, bool advance = false, float fOffset = 0f)
	{
		if (segments == null)
		{
			pt = (ptDest = PPos.Invalid);
			return false;
		}
		if (path_t < 0f)
		{
			GetSegmentPoints(0, fOffset, out pt, out ptDest);
			if (advance)
			{
				t = 0f;
				segment_idx = 0;
			}
			ClampPoint(ref pt);
			ClampPoint(ref ptDest);
			return false;
		}
		if (path_t >= path_len)
		{
			GetSegmentPoints(segments.Count - 2, fOffset, out pt, out ptDest);
			pt = ptDest;
			if (advance)
			{
				t = path_len;
				segment_idx = segments.Count - 1;
			}
			ClampPoint(ref pt);
			ClampPoint(ref ptDest);
			return false;
		}
		int num = FindSegment(path_t);
		GetSegmentPoints(num, fOffset, out pt, out ptDest);
		Segment segment = segments[num];
		if (fOffset == 0f)
		{
			pt += segment.dir * (path_t - segment.t);
		}
		else
		{
			pt += (ptDest - pt).GetNormalized() * (path_t - segment.t);
		}
		if (advance)
		{
			t = path_t;
			segment_idx = num;
		}
		ClampPoint(ref pt);
		ClampPoint(ref ptDest);
		return true;
	}

	public bool ModifyPathAt(int seg_id, PPos pt, float r, bool check_traces = true)
	{
		if (check_traces)
		{
			if (game == null || game.path_finding == null)
			{
				return false;
			}
			PathData data = game.path_finding.data;
			if (!data.Trace(pt, segments[seg_id - 1].pt, r) || !data.Trace(segments[seg_id + 1].pt, pt, r))
			{
				return false;
			}
		}
		float num = segments[seg_id - 1].t;
		Segment value = default(Segment);
		value.pt = segments[seg_id - 1].pt;
		value.dir = pt - value.pt;
		value.t = segments[seg_id - 1].t;
		float num2 = value.dir.Normalize();
		num += num2;
		segments[seg_id - 1] = value;
		Segment value2 = default(Segment);
		value2.pt = pt;
		value2.dir = segments[seg_id + 1].pt - value2.pt;
		value2.t = num;
		num2 = value2.dir.Normalize();
		num += num2;
		segments[seg_id] = value2;
		float num3 = num - segments[seg_id + 1].t;
		for (int i = seg_id + 1; i < segments.Count; i++)
		{
			Segment value3 = new Segment
			{
				pt = segments[i].pt,
				dir = segments[i].dir,
				t = segments[i].t + num3
			};
			segments[i] = value3;
		}
		path_len += num3;
		return true;
	}
}
