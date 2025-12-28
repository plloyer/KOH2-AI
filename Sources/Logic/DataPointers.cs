using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Logic;

public class PathData
{
	public struct Node
	{
		public byte clearance;

		public byte slope;

		public byte bits;

		public byte dir;

		public byte lsa;

		public short pa_id;

		public byte weight;

		public byte closed;

		public ushort estimate;

		public uint path_eval;

		public byte river_offset;

		public const byte ROAD_MASK = 1;

		public const byte RIVER_MASK = 2;

		public const byte COAST_MASK = 4;

		public const byte OCEAN_MASK = 8;

		public const byte LAKE_MASK = 16;

		public const byte TOWN_MASK = 32;

		public const byte WALL_MASK = 64;

		public const byte OPEN_MASK = 128;

		public const byte IMPASSABLE_MASK = 26;

		public bool open
		{
			get
			{
				return get_bit(128);
			}
			set
			{
				set_bit(128, value);
			}
		}

		public bool road
		{
			get
			{
				return get_bit(1);
			}
			set
			{
				set_bit(1, value);
			}
		}

		public bool river
		{
			get
			{
				return get_bit(2);
			}
			set
			{
				set_bit(2, value);
			}
		}

		public bool ocean
		{
			get
			{
				return get_bit(8);
			}
			set
			{
				set_bit(8, value);
			}
		}

		public bool coast
		{
			get
			{
				return get_bit(4);
			}
			set
			{
				set_bit(4, value);
			}
		}

		public bool lake
		{
			get
			{
				return get_bit(16);
			}
			set
			{
				set_bit(16, value);
			}
		}

		public bool town
		{
			get
			{
				return get_bit(32);
			}
			set
			{
				set_bit(32, value);
			}
		}

		public bool water
		{
			get
			{
				if (!ocean && !lake)
				{
					return coast;
				}
				return true;
			}
		}

		public bool get_bit(byte mask)
		{
			return (bits & mask) != 0;
		}

		public void set_bit(byte mask, bool val)
		{
			bits = (byte)(val ? (bits | mask) : (bits & ~mask));
		}
	}

	public struct DataPointers
	{
		[NativeDisableUnsafePtrRestriction]
		public unsafe Node* nodes;

		[NativeDisableUnsafePtrRestriction]
		public unsafe PassableArea* pas;

		[NativeDisableUnsafePtrRestriction]
		public unsafe PassableAreaNode* paNodes;

		[NativeDisableUnsafePtrRestriction]
		public unsafe Node* paNormalNodes;

		[NativeDisableUnsafePtrRestriction]
		public unsafe bool* Initted;

		private int numNodesPerArea;

		private int groundWidth;

		private int groundHeight;

		private float tileSize;

		private GCHandle gch_nodes;

		private GCHandle gch_pas;

		private GCHandle gch_paNodes;

		private GCHandle gch_paNormalNodes;

		private GCHandle gch_initted;

		public unsafe DataPointers(ref PathData pfData)
		{
			gch_nodes = AllocationManager.AllocPinned(pfData.nodes);
			gch_pas = AllocationManager.AllocPinned(pfData.pas);
			gch_paNodes = AllocationManager.AllocPinned(pfData.paNodes);
			gch_paNormalNodes = AllocationManager.AllocPinned(pfData.paNormalNodes);
			gch_initted = AllocationManager.AllocPinned(false);
			nodes = (Node*)(void*)gch_nodes.AddrOfPinnedObject();
			pas = (PassableArea*)(void*)gch_pas.AddrOfPinnedObject();
			paNodes = (PassableAreaNode*)(void*)gch_paNodes.AddrOfPinnedObject();
			paNormalNodes = (Node*)(void*)gch_paNormalNodes.AddrOfPinnedObject();
			Initted = (bool*)(void*)gch_initted.AddrOfPinnedObject();
			numNodesPerArea = PassableArea.numNodes;
			tileSize = pfData.settings.tile_size;
			groundWidth = pfData.width;
			groundHeight = pfData.height;
		}

		public void Dispose()
		{
			if (gch_nodes.IsAllocated)
			{
				AllocationManager.Free(ref gch_nodes);
			}
			if (gch_pas.IsAllocated)
			{
				AllocationManager.Free(ref gch_pas);
			}
			if (gch_paNodes.IsAllocated)
			{
				AllocationManager.Free(ref gch_paNodes);
			}
			if (gch_paNormalNodes.IsAllocated)
			{
				AllocationManager.Free(ref gch_paNormalNodes);
			}
			if (gch_initted.IsAllocated)
			{
				AllocationManager.Free(ref gch_initted);
			}
		}

		public int WorldToID(PPos pos)
		{
			pos.x = ((pos.x < 0f) ? 0f : ((pos.x > (float)groundWidth) ? ((float)groundWidth) : pos.x));
			pos.y = ((pos.y < 0f) ? 0f : ((pos.y > (float)groundWidth) ? ((float)groundWidth) : pos.y));
			return (int)(pos.y / tileSize) * groundWidth + (int)(pos.x / tileSize);
		}

		public unsafe Node GetNode(int idx)
		{
			return nodes[idx];
		}

		public unsafe PassableAreaNode GetPANode(int idx)
		{
			return paNodes[idx];
		}

		public unsafe PassableArea GetPA(int idx)
		{
			return pas[idx];
		}

		public unsafe Node GetPANormalNode(int idx)
		{
			return paNormalNodes[idx];
		}

		public unsafe void SetNode(int idx, Node newValue)
		{
			nodes[idx] = newValue;
		}

		public unsafe void SetPANode(int idx, PassableAreaNode newValue)
		{
			paNodes[idx] = newValue;
		}

		public unsafe void SetPA(int idx, PassableArea newValue)
		{
			pas[idx] = newValue;
		}

		public unsafe void SetPANormalNode(int idx, Node newValue)
		{
			paNormalNodes[idx] = newValue;
		}

		private bool RayStep(ref Coord t, ref Point ptl, ref Point dl, float thj)
		{
			return true;
		}

		public bool IsPassable(PPos pt)
		{
			if (pt.paID > 0)
			{
				return true;
			}
			return (float)(int)GetNode(WorldToID(pt)).weight > 0f;
		}

		public bool TraceDir(PPos pt, Point dir_normalized, float max_dist, out PPos result, PassableArea.Type allowed_areas = PassableArea.Type.All, int battle_side = -1, bool was_inside_wall = false)
		{
			PPos to = pt + dir_normalized * max_dist;
			to.paID = 0;
			return Trace(pt, to, out result, ignore_impassable_terrain: false, check_in_area: true, water_is_passable: false, allowed_areas, battle_side, was_inside_wall);
		}

		public bool Trace(PPos from, PPos to, out PPos result, bool ignore_impassable_terrain = false, bool check_in_area = true, bool water_is_passable = false, PassableArea.Type allowed_types = PassableArea.Type.All, int battle_side = -1, bool was_inside_wall = false)
		{
			int idx_hit;
			return Trace(from, to, out result, out idx_hit, ignore_impassable_terrain, check_in_area, water_is_passable, allowed_types, battle_side, was_inside_wall);
		}

		public unsafe bool BurstedTrace(PPos from, PPos to, out PPos result, bool ignore_impassable_terrain = false, bool check_in_area = true, bool water_is_passable = false, PassableArea.Type allowed_types = PassableArea.Type.All, int battle_side = -1, bool was_inside_wall = false)
		{
			fixed (DataPointers* data = &this)
			{
				PPos pPos = default(PPos);
				bool result2 = PathFindingBurst.Trace(data, &from, &to, &pPos, ignore_impassable_terrain, check_in_area, water_is_passable, allowed_types, battle_side, was_inside_wall);
				result = pPos;
				return result2;
			}
		}

		public unsafe bool Trace(PPos from, PPos to, out PPos result, out int idx_hit, bool ignore_impassable_terrain = false, bool check_in_area = true, bool water_is_passable = false, PassableArea.Type allowed_types = PassableArea.Type.All, int battle_side = -1, bool ignore_from_allowed_type = false, bool was_inside_wall = false)
		{
			idx_hit = -1;
			if (from.paID > 0 && !ignore_from_allowed_type && (GetPA(from.paID - 1).type & allowed_types) == 0)
			{
				result = from;
				return false;
			}
			PPos result2 = from;
			PPos pPos = result2;
			int num = 0;
			int num2 = -1;
			float num3 = Math.Min(from.Dist(to), tileSize);
			if (num3 <= 0f)
			{
				result = to;
				return true;
			}
			while (true)
			{
				if (result2 == to)
				{
					result = to;
					return true;
				}
				if (++num > 100)
				{
					result = pPos;
					return true;
				}
				if (result2.paID > 0)
				{
					int num4 = result2.paID - 1;
					PPos pPos2 = PPos.Invalid;
					PPos pPos3 = PPos.Invalid;
					bool flag = true;
					bool flag2 = true;
					int num5 = -1;
					int num6 = -1;
					for (int i = 0; i < numNodesPerArea; i++)
					{
						Point xz = pas[num4].GetCornerVertex(i).xz;
						Point xz2 = pas[num4].GetCornerVertex((i + 1) % numNodesPerArea).xz;
						Node node = paNormalNodes[num4 * numNodesPerArea + i];
						PassableAreaNode passableAreaNode = paNodes[num4 * numNodesPerArea + i];
						if (!Intersect(pPos.pos, to.pos, xz, xz2, out var resPoint) || !CheckIfPointIsInRectangle(resPoint, xz, xz2, 0.03) || !IsInSameDir(pPos, to, resPoint) || !CheckIfPointIsInRectangle(resPoint, pPos.pos, to.pos) || (node.pa_id == num2 && !IsGroundPAid(num2)))
						{
							continue;
						}
						Point v = xz2 - xz;
						Point point = to - xz;
						Point point2 = point.Project(v);
						Point normalized = (point - point2).GetNormalized();
						Point xz3 = pas[num4].GetCornerVertex((i - 1 < 0) ? (numNodesPerArea - 1) : (i - 1)).xz;
						Point xz4 = pas[num4].GetCornerVertex((i + 2) % numNodesPerArea).xz;
						Point normalized2 = (xz3 - xz).GetNormalized();
						Point normalized3 = (xz4 - xz2).GetNormalized();
						Point point3 = (normalized2 + normalized3) / 2f;
						Point pt = resPoint - pPos;
						bool flag3 = point3.Dot(pt) > 0f && pt.Length() > 0.001f;
						PPos pPos4 = new PPos(resPoint + normalized * 0.031f, node.pa_id);
						bool flag4 = true;
						bool flag5 = false;
						if (node.pa_id != 0)
						{
							flag5 = !GetPA(node.pa_id - 1).enabled;
						}
						if (flag5 || passableAreaNode.type == PassableAreaNode.Type.Unlinked)
						{
							pPos4.pos = resPoint;
							flag4 = false;
						}
						else if (!PointInArea(pPos.pos, result2.paID, allowed_types) && flag3)
						{
							pPos4.paID = result2.paID;
						}
						if (passableAreaNode.type == PassableAreaNode.Type.Ground)
						{
							flag4 = ignore_impassable_terrain || IsPassable(pPos4);
						}
						else
						{
							if (flag3)
							{
								continue;
							}
							if (node.pa_id != 0 && flag4)
							{
								PassableArea pA = GetPA(node.pa_id - 1);
								if ((pA.type & allowed_types) == 0)
								{
									flag4 = false;
								}
								if (flag4 && battle_side != -1 && pA.battle_side != -1)
								{
									flag4 = pA.CanEnter(allowed_types, battle_side, this, node.pa_id, result2.paID, was_inside_wall);
								}
							}
						}
						if (!flag4)
						{
							pPos4.pos -= normalized * 0.062f;
							pPos4.paID = result2.paID;
						}
						if (pPos2 == PPos.Invalid)
						{
							pPos2 = pPos4;
							flag = flag4;
							num5 = i;
						}
						else
						{
							pPos3 = pPos4;
							flag2 = flag4;
							num6 = i;
						}
					}
					if (!flag)
					{
						result = pPos2;
						idx_hit = num5;
						return false;
					}
					if (!flag2)
					{
						result = pPos3;
						idx_hit = num6;
						return false;
					}
					if (pPos2 != PPos.Invalid)
					{
						num2 = result2.paID;
						result2 = ((!(pPos3 != PPos.Invalid)) ? pPos2 : ((!(result2.SqrDist(pPos2) > result2.SqrDist(pPos3))) ? pPos2 : pPos3));
					}
					else
					{
						if (!check_in_area || PointInArea(to.pos, result2.paID))
						{
							result = new PPos(to.pos, result2.paID);
							return true;
						}
						if (num2 != 0)
						{
							if (!CheckNeighbours(pPos, pPos.paID - 1, num2, out result2, allowed_types))
							{
								result = result2;
								return false;
							}
						}
						else
						{
							num2 = result2.paID;
							result2.paID = 0;
							if (result2.Dist(to) <= num3)
							{
								result = new PPos(to);
								return true;
							}
						}
					}
					pPos = result2;
					continue;
				}
				Coord tile = Coord.WorldToGrid(result2, num3);
				Point ptLocal = Coord.WorldToLocal(tile, result2, num3);
				Point destLocal = Coord.WorldToLocal(tile, to, num3);
				Coord t = Coord.Invalid;
				Coord t2 = Coord.Invalid;
				Coord coord = new Coord(result2.pos + ptLocal);
				do
				{
					coord = new Coord(result2.pos);
					if (coord.x < 0 || coord.x >= groundWidth || coord.y < 0 || coord.y >= groundHeight)
					{
						result = pPos;
						return false;
					}
					Node node2 = GetNode(coord.y * groundWidth + coord.x);
					short pa_id = node2.pa_id;
					if (pa_id != num2)
					{
						num2 = result2.paID;
						if (pa_id != 0 && GetPA(pa_id - 1).CanEnter(allowed_types, battle_side, this, pa_id, 0, was_inside_wall))
						{
							result2.paID = pa_id;
						}
					}
					if (!IsGroundPAid(result2.paID))
					{
						break;
					}
					if (node2.weight == 0 && (!water_is_passable || !node2.water) && !ignore_impassable_terrain)
					{
						result = pPos;
						return false;
					}
					pPos = result2;
					result2 = new PPos(Coord.GridToWorld(tile, num3), result2.paID);
				}
				while (Coord.RayStep(ref tile, ref ptLocal, ref destLocal, 0f, out t, out t2));
				if (IsGroundPAid(result2.paID))
				{
					break;
				}
			}
			result = new PPos(to);
			return true;
		}

		public bool TraceManaged(PPos from, PPos to, out PPos result, bool ignore_impassable_terrain = false, bool check_in_area = true, bool water_is_passable = false, PassableArea.Type allowed_types = PassableArea.Type.All, int battle_side = -1, SquadPowerGrid[] power_grids = null, bool was_inside_wall = false)
		{
			int idx_hit;
			return TraceManaged(from, to, out result, out idx_hit, null, ignore_impassable_terrain, check_in_area, water_is_passable, allowed_types, battle_side, power_grids, was_inside_wall);
		}

		public bool TraceManaged(PPos from, PPos to, out PPos result, List<PPos> intersections, bool ignore_impassable_terrain = false, bool check_in_area = true, bool water_is_passable = false, PassableArea.Type allowed_types = PassableArea.Type.All, int battle_side = -1, SquadPowerGrid[] power_grids = null, bool was_inside_wall = false)
		{
			int idx_hit;
			return TraceManaged(from, to, out result, out idx_hit, intersections, ignore_impassable_terrain, check_in_area, water_is_passable, allowed_types, battle_side, power_grids, was_inside_wall);
		}

		public unsafe bool TraceManaged(PPos from, PPos to, out PPos result, out int idx_hit, List<PPos> intersections, bool ignore_impassable_terrain = false, bool check_in_area = true, bool water_is_passable = false, PassableArea.Type allowed_types = PassableArea.Type.All, int battle_side = -1, SquadPowerGrid[] power_grids = null, bool was_inside_wall = false)
		{
			idx_hit = -1;
			if (from.paID > 0 && (GetPA(from.paID - 1).type & allowed_types) == 0)
			{
				result = from;
				return false;
			}
			PPos result2 = from;
			PPos pPos = result2;
			int num = 0;
			int num2 = -1;
			float num3 = Math.Min(from.Dist(to), tileSize);
			if (num3 <= 0f)
			{
				result = to;
				return true;
			}
			while (true)
			{
				if (num2 != result2.paID)
				{
					intersections?.Add(result2);
				}
				if (result2 == to)
				{
					result = to;
					return true;
				}
				if (++num > 100)
				{
					result = pPos;
					return true;
				}
				if (result2.paID > 0)
				{
					int num4 = result2.paID - 1;
					PPos pPos2 = PPos.Invalid;
					PPos pPos3 = PPos.Invalid;
					bool flag = true;
					bool flag2 = true;
					int num5 = -1;
					int num6 = -1;
					for (int i = 0; i < numNodesPerArea; i++)
					{
						Point xz = pas[num4].GetCornerVertex(i).xz;
						Point xz2 = pas[num4].GetCornerVertex((i + 1) % numNodesPerArea).xz;
						Node node = paNormalNodes[num4 * numNodesPerArea + i];
						PassableAreaNode passableAreaNode = paNodes[num4 * numNodesPerArea + i];
						if (!Intersect(pPos.pos, to.pos, xz, xz2, out var resPoint) || !CheckIfPointIsInRectangle(resPoint, xz, xz2, 0.03) || !IsInSameDir(pPos, to, resPoint) || !CheckIfPointIsInRectangle(resPoint, pPos.pos, to.pos) || (node.pa_id == num2 && !IsGroundPAid(num2)))
						{
							continue;
						}
						Point v = xz2 - xz;
						Point point = to - xz;
						Point point2 = point.Project(v);
						Point normalized = (point - point2).GetNormalized();
						Point xz3 = pas[num4].GetCornerVertex((i - 1 < 0) ? (numNodesPerArea - 1) : (i - 1)).xz;
						Point xz4 = pas[num4].GetCornerVertex((i + 2) % numNodesPerArea).xz;
						Point normalized2 = (xz3 - xz).GetNormalized();
						Point normalized3 = (xz4 - xz2).GetNormalized();
						bool flag3 = ((normalized2 + normalized3) / 2f).Dot(resPoint - pPos) > 0f;
						PPos pPos4 = new PPos(resPoint + normalized * 0.031f, node.pa_id);
						bool flag4 = true;
						bool flag5 = false;
						if (node.pa_id != 0)
						{
							flag5 = !GetPA(node.pa_id - 1).enabled;
						}
						if (flag5 || passableAreaNode.type == PassableAreaNode.Type.Unlinked)
						{
							pPos4.pos = resPoint;
							flag4 = false;
						}
						else if (!PointInArea(pPos.pos, result2.paID, allowed_types) && flag3)
						{
							pPos4.paID = result2.paID;
						}
						if (passableAreaNode.type == PassableAreaNode.Type.Ground)
						{
							flag4 = ignore_impassable_terrain || IsPassable(pPos4);
						}
						else
						{
							if (flag3)
							{
								continue;
							}
							if (node.pa_id != 0 && flag4)
							{
								PassableArea pA = GetPA(node.pa_id - 1);
								if ((pA.type & allowed_types) == 0)
								{
									flag4 = false;
								}
								if (battle_side != -1 && pA.battle_side != -1)
								{
									flag4 = pA.CanEnter(allowed_types, battle_side, this, node.pa_id, result2.paID, was_inside_wall);
								}
							}
						}
						if (!flag4)
						{
							pPos4.pos -= normalized * 0.062f;
							pPos4.paID = result2.paID;
						}
						if (pPos2 == PPos.Invalid)
						{
							pPos2 = pPos4;
							flag = flag4;
							num5 = i;
						}
						else
						{
							pPos3 = pPos4;
							flag2 = flag4;
							num6 = i;
						}
					}
					if (!flag)
					{
						result = pPos2;
						idx_hit = num5;
						return false;
					}
					if (!flag2)
					{
						result = pPos3;
						idx_hit = num6;
						return false;
					}
					if (pPos2 != PPos.Invalid)
					{
						num2 = result2.paID;
						result2 = ((!(pPos3 != PPos.Invalid)) ? pPos2 : ((!(result2.SqrDist(pPos2) > result2.SqrDist(pPos3))) ? pPos2 : pPos3));
					}
					else
					{
						if (!check_in_area || PointInArea(to.pos, result2.paID))
						{
							result = new PPos(to.pos, result2.paID);
							return true;
						}
						if (num2 != 0)
						{
							if (!CheckNeighbours(pPos, pPos.paID - 1, num2, out result2, allowed_types))
							{
								result = result2;
								return false;
							}
						}
						else
						{
							num2 = result2.paID;
							result2.paID = 0;
							if (result2.Dist(to) <= num3)
							{
								result = new PPos(to);
								return true;
							}
						}
					}
					pPos = result2;
					continue;
				}
				Coord tile = Coord.WorldToGrid(result2, num3);
				Point ptLocal = Coord.WorldToLocal(tile, result2, num3);
				Point destLocal = Coord.WorldToLocal(tile, to, num3);
				Coord t = Coord.Invalid;
				Coord t2 = Coord.Invalid;
				Coord coord = new Coord(result2.pos + ptLocal);
				do
				{
					if (power_grids != null && battle_side >= 0 && power_grids[1 - battle_side].GetInterpolatedCell(result2.x, result2.y).base_threat > 0f)
					{
						result = pPos;
						return false;
					}
					coord = new Coord(result2.pos);
					Node node2 = GetNode(coord.y * groundWidth + coord.x);
					short pa_id = node2.pa_id;
					if (pa_id != num2)
					{
						num2 = result2.paID;
						result2.paID = pa_id;
					}
					if (!IsGroundPAid(result2.paID))
					{
						break;
					}
					if (node2.weight == 0 && (!water_is_passable || !node2.water) && !ignore_impassable_terrain)
					{
						result = pPos;
						return false;
					}
					pPos = result2;
					result2 = new PPos(Coord.GridToWorld(tile, num3), result2.paID);
				}
				while (Coord.RayStep(ref tile, ref ptLocal, ref destLocal, 0f, out t, out t2));
				if (IsGroundPAid(result2.paID))
				{
					break;
				}
			}
			result = new PPos(to);
			return true;
		}

		public unsafe bool ClosestIntersection(Point from, Point to, int area, out PPos result, bool ignore_impassable_terrain = false)
		{
			PPos pPos = new PPos(from, area);
			float num = float.MaxValue;
			int num2 = area - 1;
			bool result2 = false;
			for (int i = 0; i < numNodesPerArea; i++)
			{
				Point xz = pas[num2].GetCornerVertex(i).xz;
				Point xz2 = pas[num2].GetCornerVertex((i + 1) % numNodesPerArea).xz;
				Node node = paNormalNodes[num2 * numNodesPerArea + i];
				PassableAreaNode passableAreaNode = paNodes[num2 * numNodesPerArea + i];
				if (Intersect(from, to, xz, xz2, out var resPoint) && CheckIfPointIsInRectangle(resPoint, xz, xz2))
				{
					float num3 = to.Dist(resPoint);
					if (num3 < num)
					{
						num = num3;
						result2 = passableAreaNode.type != PassableAreaNode.Type.Unlinked;
						pPos = ((node.pa_id != 0 || PointInArea(from, area)) ? new PPos(resPoint, node.pa_id) : new PPos(resPoint, area));
					}
				}
			}
			result = pPos;
			return result2;
		}

		public bool IsNearCorner(Point pos, int paID, float dist = 0.475f)
		{
			PassableArea pA = GetPA(paID - 1);
			for (int i = 0; i < numNodesPerArea; i++)
			{
				if (pA.GetCornerVertex(i).Dist(pos) <= dist)
				{
					return true;
				}
			}
			return false;
		}

		public unsafe bool CheckNeighbours(PPos currentPos, int paIdx, int lastPAID, out PPos result, PassableArea.Type allowed_types = PassableArea.Type.All)
		{
			for (int i = 0; i < numNodesPerArea; i++)
			{
				Node node = paNormalNodes[paIdx * numNodesPerArea + i];
				PassableAreaNode passableAreaNode = paNodes[paIdx * numNodesPerArea + i];
				if (node.pa_id == lastPAID || passableAreaNode.type != PassableAreaNode.Type.Normal)
				{
					continue;
				}
				if (PointInArea(currentPos.pos, node.pa_id, allowed_types))
				{
					result = new PPos(currentPos.pos, node.pa_id);
					return true;
				}
				for (int j = 0; j < numNodesPerArea; j++)
				{
					Node node2 = paNormalNodes[(node.pa_id - 1) * numNodesPerArea + j];
					PassableAreaNode passableAreaNode2 = paNodes[(node.pa_id - 1) * numNodesPerArea + j];
					if (node2.pa_id != currentPos.paID && node2.pa_id != lastPAID && passableAreaNode2.type == PassableAreaNode.Type.Normal && PointInArea(currentPos.pos, node2.pa_id, allowed_types))
					{
						result = new PPos(currentPos.pos, node2.pa_id);
						return true;
					}
				}
			}
			result = currentPos;
			return false;
		}

		public unsafe bool IsNeighbour(int pa1, int pa2)
		{
			if (pa1 == pa2)
			{
				return true;
			}
			if (pa1 > 0)
			{
				int num = pa1 - 1;
				for (int i = 0; i < numNodesPerArea; i++)
				{
					if (paNormalNodes[num * numNodesPerArea + i].pa_id == pa2)
					{
						return true;
					}
				}
			}
			if (pa2 > 0)
			{
				int num2 = pa2 - 1;
				for (int j = 0; j < numNodesPerArea; j++)
				{
					if (paNormalNodes[num2 * numNodesPerArea + j].pa_id == pa1)
					{
						return true;
					}
				}
			}
			return false;
		}

		private float sign(Point p1, Point p2, Point p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		private bool PointInTriangle(Point P, Point A, Point B, Point C)
		{
			float num = C.y - A.y;
			float num2 = C.x - A.x;
			float num3 = B.y - A.y;
			float num4 = P.y - A.y;
			float num5 = (A.x * num + num4 * num2 - P.x * num) / (num3 * num2 - (B.x - A.x) * num);
			float num6 = (num4 - num5 * num3) / num;
			if (float.IsNaN(num5) || float.IsNaN(num6))
			{
				return false;
			}
			if (num5 >= 0f && num6 >= 0f)
			{
				return num5 + num6 <= 1f;
			}
			return false;
		}

		private bool PointInQuad(Point P, Point A, Point B, Point C, Point D)
		{
			if (!PointInTriangle(P, A, B, C))
			{
				return PointInTriangle(P, A, C, D);
			}
			return true;
		}

		public unsafe bool PointInArea(Point P, int area_id, PassableArea.Type allowed_types = PassableArea.Type.All)
		{
			PassableArea passableArea = pas[area_id - 1];
			if ((passableArea.type & allowed_types) == 0)
			{
				return false;
			}
			return PointInQuad(P, passableArea.cornerVertex1.xz, passableArea.cornerVertex2.xz, passableArea.cornerVertex3.xz, passableArea.cornerVertex4.xz);
		}

		public bool IsInSameDir(Point from, Point p1, Point p2)
		{
			Point point = p1 - from;
			Point point2 = p2 - from;
			if (Math.Sign(point.x) == Math.Sign(point2.x))
			{
				return Math.Sign(point.y) == Math.Sign(point2.y);
			}
			return false;
		}

		public static bool CheckIfPointIsInRectangle(Point point, Point recPos1, Point recPos2, double error = 0.0)
		{
			float x = point.x;
			float y = point.y;
			Point point2 = recPos1;
			Point point3 = recPos2;
			if (((double)point2.x - error <= (double)x && (double)x <= (double)point3.x + error) || ((double)point2.x + error >= (double)x && (double)x >= (double)point3.x - error))
			{
				if (!((double)point2.y - error <= (double)y) || !((double)y <= (double)point3.y + error))
				{
					if ((double)point2.y + error >= (double)y)
					{
						return (double)y >= (double)point3.y - error;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public static bool Intersect(Point line1V1, Point line1V2, Point line2V1, Point line2V2, out Point resPoint)
		{
			float num = line1V2.y - line1V1.y;
			float num2 = line1V1.x - line1V2.x;
			float num3 = num * line1V1.x + num2 * line1V1.y;
			float num4 = line2V2.y - line2V1.y;
			float num5 = line2V1.x - line2V2.x;
			float num6 = num4 * line2V1.x + num5 * line2V1.y;
			float num7 = num * num5 - num4 * num2;
			if (Math.Abs(num7) <= 0.001f)
			{
				resPoint = line1V1;
				return false;
			}
			float x = (num5 * num3 - num2 * num6) / num7;
			float y = (num * num6 - num4 * num3) / num7;
			resPoint = new Point(x, y);
			return true;
		}
	}

	public struct PassableAreaNode
	{
		public enum Type
		{
			Normal,
			Ground,
			Unlinked,
			Edge
		}

		public Type type;

		public PPos pos;

		public int link;
	}

	public struct PassableArea
	{
		public enum Type
		{
			Generic = 1,
			Normal = 245,
			Ladder = 2,
			ForcedGround = 4,
			Ground = 20,
			Teleport = 8,
			Gate = 16,
			Wall = 32,
			Tower = 64,
			Stairs = 128,
			LadderExit = 256,
			None = 0,
			All = -1
		}

		public static readonly int numNodes = 4;

		public Point3 cornerVertex1;

		public Point3 cornerVertex2;

		public Point3 cornerVertex3;

		public Point3 cornerVertex4;

		public const byte friends_can_enter = 1;

		public const byte anyone_can_enter = 2;

		public const byte nobody_can_enter = 4;

		public const byte has_ladder = 8;

		public const byte attacker = 16;

		public const byte defender = 32;

		public byte flags;

		public bool enabled;

		public Point3 normal;

		public bool connected_to_ground;

		public Type type;

		public int battle_side
		{
			get
			{
				if ((flags & 0x10) != 0)
				{
					return 0;
				}
				if ((flags & 0x20) != 0)
				{
					return 1;
				}
				return -1;
			}
			set
			{
				flags = (byte)(flags & -17);
				flags = (byte)(flags & -33);
				switch (value)
				{
				case 0:
					flags |= 16;
					break;
				case 1:
					flags |= 32;
					break;
				}
			}
		}

		public bool FriendsCanEnter
		{
			get
			{
				return (flags & 1) != 0;
			}
			set
			{
				if (value)
				{
					flags |= 1;
					flags = (byte)(flags & -3);
					flags = (byte)(flags & -5);
				}
				else
				{
					flags = (byte)(flags & -2);
				}
			}
		}

		public bool AnyoneCanEnter
		{
			get
			{
				return (flags & 2) != 0;
			}
			set
			{
				if (value)
				{
					flags |= 2;
					flags = (byte)(flags & -2);
					flags = (byte)(flags & -5);
				}
				else
				{
					flags = (byte)(flags & -3);
				}
			}
		}

		public bool NobodyCanEnter
		{
			get
			{
				return (flags & 4) != 0;
			}
			set
			{
				if (value)
				{
					flags |= 4;
					flags = (byte)(flags & -2);
					flags = (byte)(flags & -3);
				}
				else
				{
					flags = (byte)(flags & -5);
				}
			}
		}

		public bool HasLadder
		{
			get
			{
				return (flags & 8) != 0;
			}
			set
			{
				if (value)
				{
					flags |= 8;
				}
				else
				{
					flags = (byte)(flags & -9);
				}
			}
		}

		public float Angle
		{
			get
			{
				Point3 normalUp = GetNormalUp();
				return 90f - Math.Abs(AngleBetween(v: new Point3(normalUp.x, 0f, normalUp.z), u: normalUp));
			}
		}

		public bool CanEnter(Path path, PathFinding pf, int paID, int prev_paID = 0)
		{
			return CanEnter(path.allowed_area_types, path.battle_side, pf.data.pointers, paID, prev_paID, path.was_inside_wall);
		}

		public bool CanEnter(Type allowed_areas, int battle_side, DataPointers data, int paID, int prev_paID = 0, bool was_inside_wall = false)
		{
			if ((allowed_areas & type) == 0)
			{
				return false;
			}
			if (IsGround() && connected_to_ground)
			{
				return true;
			}
			if (NobodyCanEnter)
			{
				return false;
			}
			if (this.battle_side != -1 && battle_side != -1 && !was_inside_wall && FriendsCanEnter && battle_side != this.battle_side)
			{
				return false;
			}
			if (type == Type.Ladder)
			{
				int num = 0;
				for (int i = 0; i < numNodes; i++)
				{
					PassableAreaNode pANode = data.GetPANode((paID - 1) * numNodes + i);
					if (pANode.type == PassableAreaNode.Type.Normal)
					{
						int paID2 = data.GetPANode(pANode.link).pos.paID;
						PassableArea pA = data.GetPA(paID2 - 1);
						if (pA.enabled && !pA.NobodyCanEnter)
						{
							num++;
						}
					}
				}
				if (num < 2)
				{
					return false;
				}
			}
			if (type == Type.LadderExit && prev_paID != 0 && data.GetPA(prev_paID - 1).type != Type.Ladder)
			{
				bool flag = false;
				for (int j = 0; j < numNodes; j++)
				{
					PassableAreaNode pANode2 = data.GetPANode((paID - 1) * numNodes + j);
					if (pANode2.type == PassableAreaNode.Type.Normal)
					{
						int paID3 = data.GetPANode(pANode2.link).pos.paID;
						PassableArea pA2 = data.GetPA(paID3 - 1);
						if (pA2.enabled && pA2.type == Type.Ladder && pA2.HasLadder)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		public bool IsGround()
		{
			return (type & Type.Ground) != 0;
		}

		public bool IsWall()
		{
			return (type & (Type)224) != 0;
		}

		public void SetCorner(int idx, Point3 newCords)
		{
			switch (idx)
			{
			case 0:
				cornerVertex1 = newCords;
				break;
			case 1:
				cornerVertex2 = newCords;
				break;
			case 2:
				cornerVertex3 = newCords;
				break;
			default:
				cornerVertex4 = newCords;
				break;
			}
		}

		public Point3 GetCornerVertex(int idx)
		{
			return idx switch
			{
				0 => cornerVertex1, 
				1 => cornerVertex2, 
				2 => cornerVertex3, 
				_ => cornerVertex4, 
			};
		}

		public Point3 GetNormalUp()
		{
			Point3 point = cornerVertex2 - cornerVertex1;
			Point3 pt = cornerVertex3 - cornerVertex1;
			Point3 result = point.Cross(pt);
			if (result.y < 0f)
			{
				result *= -1f;
			}
			result.Normalize();
			return result;
		}

		private float AngleBetween(Point3 u, Point3 v)
		{
			float num = 0f;
			num += u.x * v.x;
			num += u.y * v.y;
			num += u.z * v.z;
			float num2 = 0f;
			float num3 = 0f + u.x * u.x;
			num2 += v.x * v.x;
			float num4 = num3 + u.y * u.y;
			num2 += v.y * v.y;
			float num5 = num4 + u.z * u.z;
			num2 += v.z * v.z;
			float num6 = 0f;
			num6 = (float)Math.Sqrt(num5 * num2);
			if (num6 == 0f)
			{
				return 90f;
			}
			return (float)Math.Acos(num / num6) * 57.29578f;
		}

		public float GetHeight(Point3 pos)
		{
			float num = float.MinValue;
			float num2 = float.MaxValue;
			for (int i = 0; i < numNodes; i++)
			{
				Point3 cornerVertex = GetCornerVertex(i);
				if (cornerVertex.y > num)
				{
					num = cornerVertex.y;
				}
				if (cornerVertex.y < num2)
				{
					num2 = cornerVertex.y;
				}
			}
			Point3 normalUp = GetNormalUp();
			Point3 point = cornerVertex1;
			float num3 = normalUp.x * pos.x;
			float num4 = normalUp.z * pos.z;
			float num5 = 0f - (normalUp.x * point.x + normalUp.y * point.y + normalUp.z * point.z);
			float num6 = (0f - (num3 + num4 + num5)) / normalUp.y;
			if (float.IsNaN(num6))
			{
				return pos.y;
			}
			float num7 = Math.Abs(num6);
			if (!(num7 < num2))
			{
				if (!(num7 > num))
				{
					return num7;
				}
				return num;
			}
			return num2;
		}

		public bool Contains(Point pos)
		{
			float num = Math.Max(Math.Max(Math.Max(cornerVertex1.x, cornerVertex2.x), cornerVertex3.x), cornerVertex4.x);
			float num2 = Math.Min(Math.Min(Math.Min(cornerVertex1.x, cornerVertex2.x), cornerVertex3.x), cornerVertex4.x);
			float num3 = Math.Max(Math.Max(Math.Max(cornerVertex1.z, cornerVertex2.z), cornerVertex3.z), cornerVertex4.z);
			float num4 = Math.Min(Math.Min(Math.Min(cornerVertex1.z, cornerVertex2.z), cornerVertex3.z), cornerVertex4.z);
			if (pos.x >= num2 && pos.x <= num && pos.y >= num4)
			{
				return pos.y <= num3;
			}
			return false;
		}

		public Point3 Center()
		{
			return (cornerVertex1 + cornerVertex2 + cornerVertex3 + cornerVertex4) / 4f;
		}
	}

	[Serializable]
	public struct HighGridNode
	{
		[NonSerialized]
		public uint path_eval;

		[NonSerialized]
		public ushort estimate;

		[NonSerialized]
		public byte flags;

		[NonSerialized]
		public byte cell_offset;

		public const byte impassable_mask = 15;

		public const byte open_mask = 16;

		public const byte has_pa_mask = 32;

		public const byte more_nodes_mask = 64;

		public const byte came_from_additional_node_mask = 128;

		public byte Dir
		{
			get
			{
				return (byte)(flags & 0xF);
			}
			set
			{
				flags = (byte)(flags & -16);
				flags |= value;
				CameFromAdditionalNode = false;
			}
		}

		public bool CameFromAdditionalNode
		{
			get
			{
				return (flags & 0x80) == 128;
			}
			set
			{
				if (value)
				{
					flags |= 128;
				}
				else
				{
					flags = (byte)(flags & -129);
				}
			}
		}

		public bool Open
		{
			get
			{
				return (flags & 0x10) == 16;
			}
			set
			{
				if (value)
				{
					flags |= 16;
				}
				else
				{
					flags = (byte)(flags & -17);
				}
			}
		}

		public bool HasPassableArea
		{
			get
			{
				return (flags & 0x20) == 32;
			}
			set
			{
				if (value)
				{
					flags |= 32;
				}
				else
				{
					flags = (byte)(flags & -33);
				}
			}
		}

		public bool HasAdditionalNodes
		{
			get
			{
				return (flags & 0x40) == 64;
			}
			set
			{
				if (value)
				{
					flags |= 64;
				}
				else
				{
					flags = (byte)(flags & -65);
				}
			}
		}

		public int GetDirCoords()
		{
			if ((flags & 0xF) == 15)
			{
				return -1;
			}
			return flags & 0xF;
		}

		public Point GetCellOffset()
		{
			int num = (cell_offset & 0xF0) >> 4;
			int num2 = cell_offset & 0xF;
			return new Point(num, num2);
		}

		public static HighGridNode Create()
		{
			return new HighGridNode
			{
				flags = 15,
				path_eval = 0u,
				estimate = 0,
				cell_offset = 136
			};
		}
	}

	public class HighPassableAreaNode
	{
		public PPos pos;

		public List<HighPassableAreaRib> ribs = new List<HighPassableAreaRib>();

		public ushort closed;

		public HighPassableAreaNode bestComeFrom;

		public List<Coord> closest_reachable_terrain;

		public byte bestComeFromTerrain;

		public uint path_eval;

		public uint estimate;
	}

	public class HighAdditionalNode
	{
		public int id;

		public Coord coord;

		public ushort[] terrain_weights;

		public List<HighAdditionalRib> ribs = new List<HighAdditionalRib>();

		public ushort closed;

		public HighAdditionalNode bestComeFrom;

		public byte bestComeFromTerrain;

		public uint path_eval;

		public uint estimate;

		public Coord GetClosestReachableTerrain(PathFinding lpf, byte dir)
		{
			Coord dir2 = lpf.GetDir(dir);
			return coord + dir2;
		}

		public HighAdditionalNode(int id)
		{
			this.id = id;
			terrain_weights = new ushort[9];
			for (int i = 0; i < terrain_weights.Length; i++)
			{
				terrain_weights[i] = ushort.MaxValue;
			}
		}
	}

	public class HighPassableAreaRib
	{
		public ushort weight;

		public HighPassableAreaNode node1;

		public HighPassableAreaNode node2;

		public HighPassableAreaNode GetOther(HighPassableAreaNode node)
		{
			if (node == node1)
			{
				return node2;
			}
			return node1;
		}
	}

	public class HighAdditionalRib
	{
		public ushort weight;

		public HighAdditionalNode node1;

		public HighAdditionalNode node2;

		public HighAdditionalNode GetOther(HighAdditionalNode node)
		{
			if (node == node1)
			{
				return node2;
			}
			return node1;
		}
	}

	public class Portal
	{
		public enum Type
		{
			Instant
		}

		public int id;

		public int pos1Idx;

		public PPos pos1;

		public int pos2Idx;

		public PPos pos2;

		public Type type;

		public PPos GetOtherPoint(PPos pos)
		{
			if (pos.Dist(pos1) < 1f)
			{
				return pos1;
			}
			if (pos.Dist(pos2) < 1f)
			{
				return pos2;
			}
			return new PPos(0f, 0f, 0);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Cell
	{
		public float min_height;

		public float max_height;

		public byte slope;

		public byte bits;

		public byte lsa_level;
	}

	public PathFinding.Settings settings;

	public Node[] nodes;

	public PassableArea[] pas = new PassableArea[0];

	public PassableAreaNode[] paNodes = new PassableAreaNode[0];

	public Node[] paNormalNodes = new Node[0];

	public List<MapObject>[,] reservation_grid;

	public int reservation_width;

	public int reservation_height;

	public List<Portal> portals = new List<Portal>();

	public int width;

	public int height;

	public bool initted;

	public bool processing;

	public int[] NeighborOffset = new int[8];

	public Coord[] GridNeighborOffset = new Coord[8];

	public byte[] GridReverseDir = new byte[8];

	public int highPFGrid_width;

	public int highPFGrid_height;

	public HighGridNode[] highPFGrid;

	public ushort[] highPFGridClosed;

	public ushort[] highPFGridWeights;

	public Dictionary<byte, int> highPFLSACount;

	public HighAdditionalNode[] highPFAdditionalNodes;

	public NativeMultiHashMap<Coord, int> highPFAdditionalGrid;

	public NativeMultiHashMap<Coord, Coord> paIDGrid;

	public List<HighPassableAreaNode> highPassableAreaNodes;

	public DataPointers pointers;

	public byte version;

	public ushort high_pf_version;

	public static float max_weight;

	private const int weights_per_high_pf_node = 4;

	public int GetHighNodeWeightID(int x, int y, int dir)
	{
		if (dir >= 4)
		{
			switch (dir)
			{
			case 4:
				x++;
				break;
			case 5:
				x--;
				y++;
				break;
			case 6:
				y++;
				break;
			case 7:
				x++;
				y++;
				break;
			}
			dir = 7 - dir;
		}
		if (x < 0 || y < 0 || x >= highPFGrid_width || y >= highPFGrid_height)
		{
			return -1;
		}
		return (x + y * highPFGrid_width) * 4 + dir;
	}

	public ushort GetHighNodeWeight(int x, int y, int dir)
	{
		int highNodeWeightID = GetHighNodeWeightID(x, y, dir);
		if (highNodeWeightID == -1)
		{
			return ushort.MaxValue;
		}
		return highPFGridWeights[highNodeWeightID];
	}

	public ushort GetHighNodeClosed(int x, int y)
	{
		return highPFGridClosed[x + y * highPFGrid_width];
	}

	public void SetHighNodeClosed(int x, int y, ushort val)
	{
		highPFGridClosed[x + y * highPFGrid_width] = val;
	}

	public void SetHighNodeWeight(int x, int y, int dir, ushort val)
	{
		int highNodeWeightID = GetHighNodeWeightID(x, y, dir);
		if (highNodeWeightID != -1)
		{
			highPFGridWeights[highNodeWeightID] = val;
		}
	}

	public bool HighCoordsInBounds(int x, int y)
	{
		if (x >= 0 && y >= 0 && x < highPFGrid_width)
		{
			return y < highPFGrid_height;
		}
		return false;
	}

	public HighGridNode GetHighGridNode(int x, int y)
	{
		return highPFGrid[x + y * highPFGrid_width];
	}

	public void SetHighNode(int x, int y, HighGridNode node)
	{
		highPFGrid[x + y * highPFGrid_width] = node;
	}

	public void CalcWeight(ref Node node)
	{
		if (node.lsa == 0 && settings.use_lsa_low_level)
		{
			node.weight = 0;
			return;
		}
		if (node.coast)
		{
			node.weight = (byte)settings.coast_avoidance;
			return;
		}
		if (node.get_bit(26))
		{
			node.weight = 0;
			return;
		}
		if (node.get_bit(32) && !settings.towns_passable)
		{
			node.weight = 0;
			return;
		}
		if (node.slope > settings.max_slope && !node.road && !node.river)
		{
			node.weight = 0;
			return;
		}
		if (node.pa_id > 0 && pas[node.pa_id - 1].type == PassableArea.Type.Teleport)
		{
			node.weight = 1;
			return;
		}
		float num = (node.town ? 1f : ((float)(int)node.slope / (float)(int)settings.max_slope));
		if (num > 1f)
		{
			num = 1f;
		}
		if (settings.slope_power != 1f)
		{
			num = ((settings.slope_power != 2f) ? ((float)Math.Pow(num, settings.slope_power)) : (num * num));
		}
		float num2 = (float)(int)settings.base_weight + num * settings.slope_avoidance;
		if (node.road)
		{
			num2 /= settings.road_stickiness;
		}
		else if (node.river)
		{
			num2 *= settings.river_avoidance;
		}
		if (num2 > max_weight)
		{
			max_weight = num2;
		}
		if (num2 > 255f)
		{
			num2 = 255f;
		}
		else if (num2 < 1f)
		{
			num2 = 1f;
		}
		node.weight = (byte)num2;
	}

	public int GetWeight(Node node, float min_radius, float max_radius)
	{
		int weight = node.weight;
		if (weight <= 0 || max_radius <= 0f)
		{
			return weight;
		}
		float num = (float)(int)node.clearance * settings.max_radius / 255f;
		if (num < min_radius)
		{
			return 0;
		}
		if (num >= max_radius)
		{
			return settings.base_weight;
		}
		float num2 = 1f - (num - min_radius) / (max_radius - min_radius);
		return (int)((float)(int)settings.base_weight + num2 * settings.slope_avoidance);
	}

	public void Init(int width, int height)
	{
		this.width = width;
		this.height = height;
		nodes = new Node[width * height];
		version = 0;
		NeighborOffset[0] = -width - 1;
		NeighborOffset[1] = -width;
		NeighborOffset[2] = -width + 1;
		NeighborOffset[3] = -1;
		NeighborOffset[4] = 1;
		NeighborOffset[5] = width - 1;
		NeighborOffset[6] = width;
		NeighborOffset[7] = width + 1;
		if (settings.reserve_grid_size != -1f)
		{
			reservation_width = (int)((float)width / settings.reserve_grid_size);
			reservation_height = (int)((float)height / settings.reserve_grid_size);
			reservation_grid = new List<MapObject>[reservation_width, reservation_height];
			for (int i = 0; i < reservation_width; i++)
			{
				for (int j = 0; j < reservation_height; j++)
				{
					reservation_grid[i, j] = new List<MapObject>();
				}
			}
		}
		int num = -1;
		for (int k = -1; k <= 1; k++)
		{
			for (int l = -1; l <= 1; l++)
			{
				if (l != 0 || k != 0)
				{
					num++;
					GridNeighborOffset[num] = new Coord(l, k);
					GridReverseDir[num] = PathFinding.GetDir(-l, -k);
				}
			}
		}
	}

	public void Load(string map_name, bool for_generation = false)
	{
		version = 0;
		byte[] array = File.ReadAllBytes(ModManager.GetModdedAssetPath(Game.maps_path + map_name + "/pathfinding.bin", allow_unmodded_path: true));
		width = BitConverter.ToInt32(array, 0);
		height = BitConverter.ToInt32(array, 4);
		Init(width, height);
		int num = 8;
		max_weight = 0f;
		int num2 = 0;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				Node node = new Node
				{
					slope = array[num++],
					bits = array[num++],
					lsa = array[num++],
					river_offset = array[num++],
					pa_id = 0
				};
				if (i < 1 || i + 1 >= height || j < 1 || j + 1 >= width)
				{
					node.weight = 0;
				}
				else
				{
					CalcWeight(ref node);
				}
				nodes[num2++] = node;
			}
		}
		string moddedAssetPath = ModManager.GetModdedAssetPath(Game.maps_path + map_name + "/HighPFGridData.bin", allow_unmodded_path: true);
		if (!for_generation && File.Exists(moddedAssetPath))
		{
			MemStream memStream = new MemStream(File.ReadAllBytes(moddedAssetPath));
			int num3 = memStream.Read7BitUInt();
			int num4 = memStream.Read7BitUInt();
			highPFGrid = new HighGridNode[num3 * num4];
			highPFGridClosed = new ushort[num3 * num4];
			highPFGrid_width = num3;
			highPFGrid_height = num4;
			for (int k = 0; k < num3; k++)
			{
				for (int l = 0; l < num4; l++)
				{
					HighGridNode node2 = HighGridNode.Create();
					node2.cell_offset = memStream.ReadByte();
					SetHighNode(k, l, node2);
				}
			}
			string moddedAssetPath2 = ModManager.GetModdedAssetPath(Game.maps_path + map_name + "/HighPFGridWeights.bin", allow_unmodded_path: true);
			if (File.Exists(moddedAssetPath2))
			{
				MemStream memStream2 = new MemStream(File.ReadAllBytes(moddedAssetPath2));
				highPFGridWeights = new ushort[memStream2.Length / 4];
				for (int m = 0; m < highPFGridWeights.Length; m++)
				{
					highPFGridWeights[m] = (ushort)memStream2.Read7BitUInt();
				}
			}
		}
		string moddedAssetPath3 = ModManager.GetModdedAssetPath(Game.maps_path + map_name + "/HighPFAdditionalData.bin", allow_unmodded_path: true);
		if (for_generation || !File.Exists(moddedAssetPath3))
		{
			return;
		}
		MemStream memStream3 = new MemStream(File.ReadAllBytes(moddedAssetPath3));
		int num5 = memStream3.Length / 44;
		highPFAdditionalGrid = new NativeMultiHashMap<Coord, int>(num5 * 9, Allocator.Persistent);
		highPFAdditionalNodes = new HighAdditionalNode[num5];
		for (int n = 0; n < num5; n++)
		{
			int num6 = memStream3.Read7BitSInt();
			int num7 = memStream3.Read7BitSInt();
			int num8 = n + 1;
			highPFAdditionalGrid.Add(new Coord(num6, num7), num8);
			HighGridNode highGridNode = GetHighGridNode(num6, num7);
			highGridNode.HasAdditionalNodes = true;
			HighAdditionalNode highAdditionalNode = new HighAdditionalNode(num8);
			highPFAdditionalNodes[n] = highAdditionalNode;
			SetHighNode(num6, num7, highGridNode);
			int num9 = -1;
			for (int num10 = -1; num10 <= 1; num10++)
			{
				int num11 = num7 + num10;
				for (int num12 = -1; num12 <= 1; num12++)
				{
					if (num12 != 0 || num10 != 0)
					{
						num9++;
						int num13 = num6 + num12;
						highAdditionalNode.terrain_weights[num9] = (ushort)memStream3.Read7BitUInt();
						if (num13 >= 0 && num13 < highPFGrid_width && num11 >= 0 && num11 < highPFGrid_height)
						{
							Coord key = new Coord(num13, num11);
							highPFAdditionalGrid.Add(key, num8);
							HighGridNode highGridNode2 = GetHighGridNode(num13, num11);
							highGridNode2.HasAdditionalNodes = true;
							SetHighNode(num13, num11, highGridNode2);
						}
					}
				}
			}
			highAdditionalNode.coord = new Coord(num6, num7);
			highAdditionalNode.terrain_weights[8] = (ushort)memStream3.Read7BitUInt();
		}
		string moddedAssetPath4 = ModManager.GetModdedAssetPath(Game.maps_path + map_name + "/HighPFAdditionalRibs.bin", allow_unmodded_path: true);
		if (!for_generation && File.Exists(moddedAssetPath4))
		{
			MemStream memStream4 = new MemStream(File.ReadAllBytes(moddedAssetPath4));
			int num14 = memStream4.Length / 12;
			for (int num15 = 0; num15 < num14; num15++)
			{
				HighAdditionalRib highAdditionalRib = new HighAdditionalRib();
				HighAdditionalNode highAdditionalNode2 = highPFAdditionalNodes[memStream4.Read7BitUInt() - 1];
				HighAdditionalNode highAdditionalNode3 = highPFAdditionalNodes[memStream4.Read7BitUInt() - 1];
				ushort weight = (ushort)memStream4.Read7BitUInt();
				highAdditionalRib.node1 = highAdditionalNode2;
				highAdditionalRib.node2 = highAdditionalNode3;
				highAdditionalRib.weight = weight;
				highAdditionalNode2.ribs.Add(highAdditionalRib);
				highAdditionalNode3.ribs.Add(highAdditionalRib);
			}
		}
	}

	public void Load(int width, int height, Cell[] grid)
	{
		version = 0;
		this.width = width;
		this.height = height;
		Init(width, height);
		max_weight = 0f;
		int num = 0;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num2 = i * width + j;
				Cell cell = grid[num2];
				Node node = new Node
				{
					slope = cell.slope,
					bits = cell.bits,
					lsa = cell.lsa_level,
					river_offset = 0,
					pa_id = 0
				};
				if (i < 1 + settings.map_bounds_width || i + 1 >= height - settings.map_bounds_width || j < 1 + settings.map_bounds_width || j + 1 >= width - settings.map_bounds_width)
				{
					node.weight = 0;
				}
				else
				{
					CalcWeight(ref node);
				}
				nodes[num++] = node;
			}
		}
	}

	public bool OutOfMapBounds(Point pt)
	{
		WorldToGrid(pt, out var x, out var y);
		if (y >= 1 + settings.map_bounds_width && y + 1 < height - settings.map_bounds_width && x >= 1 + settings.map_bounds_width)
		{
			return x + 1 >= width - settings.map_bounds_width;
		}
		return true;
	}

	private void SpreadClearance(int idx, int ofs, int cnt, float ts)
	{
		for (int i = 1; i <= cnt; i++)
		{
			int num = idx + i * ofs;
			if (num < 0 || num >= nodes.Length)
			{
				break;
			}
			Node node = nodes[num];
			if (node.weight == 0)
			{
				break;
			}
			int num2 = (int)((float)(i - 1) * ts * 255f / settings.max_radius);
			if (num2 < node.clearance)
			{
				if (num2 > 255)
				{
					num2 = 255;
				}
				nodes[num].clearance = (byte)num2;
			}
		}
	}

	public void BuildClearance()
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].clearance = byte.MaxValue;
		}
		float tile_size = settings.tile_size;
		int cnt = (int)(settings.max_radius / tile_size + 1f);
		float num = settings.tile_size * 1.4242f;
		int cnt2 = (int)(settings.max_radius / num + 1f);
		for (int j = 0; j < nodes.Length; j++)
		{
			if (nodes[j].weight <= 0)
			{
				nodes[j].clearance = 0;
				SpreadClearance(j, NeighborOffset[0], cnt2, num);
				SpreadClearance(j, NeighborOffset[1], cnt, tile_size);
				SpreadClearance(j, NeighborOffset[2], cnt2, num);
				SpreadClearance(j, NeighborOffset[3], cnt, tile_size);
				SpreadClearance(j, NeighborOffset[4], cnt, tile_size);
				SpreadClearance(j, NeighborOffset[5], cnt2, num);
				SpreadClearance(j, NeighborOffset[6], cnt, tile_size);
				SpreadClearance(j, NeighborOffset[7], cnt2, num);
			}
		}
	}

	public void WorldToGrid(PPos ptw, out int x, out int y)
	{
		if (IsGroundPAid(ptw.paID))
		{
			x = (int)(ptw.x / settings.tile_size);
			y = (int)(ptw.y / settings.tile_size);
			x = ((x >= 0) ? ((x > width - 1) ? (width - 1) : x) : 0);
			y = ((y >= 0) ? ((y > height - 1) ? (height - 1) : y) : 0);
			return;
		}
		int idx = -1;
		float num = float.MaxValue;
		for (int i = 0; i < PassableArea.numNodes; i++)
		{
			int num2 = (ptw.paID - 1) * PassableArea.numNodes + i;
			float num3 = ptw.Dist(GetPaNode(num2).pos);
			if (num3 < num)
			{
				num = num3;
				idx = num2;
			}
		}
		GetPaNodeCords(idx, out x, out y);
	}

	public PPos GridToWorld(int x, int y)
	{
		if (AreGroundNodeCords(x, y))
		{
			return new PPos(((float)x + 0.5f) * settings.tile_size, ((float)y + 0.5f) * settings.tile_size);
		}
		return paNodes[x - width].pos;
	}

	public int GetNodeIdx(int x, int y)
	{
		return y * width + x;
	}

	public void GetNodeCoords(int idx, out int x, out int y)
	{
		x = idx % width;
		y = idx / width;
	}

	public bool AreGroundNodeCords(int x, int y)
	{
		return x < width;
	}

	public static bool IsGroundPAid(int pa_id)
	{
		if (pa_id != 0)
		{
			return pa_id == -1;
		}
		return true;
	}

	public void AdjustPassableCords(ref int x, ref int y)
	{
		x += width;
	}

	public bool IsInBounds(int x, int y)
	{
		if (x >= 0 && x < width && y >= 0)
		{
			return y < height;
		}
		return false;
	}

	public PPos GetNodePos(int idx)
	{
		GetNodeCoords(idx, out var x, out var y);
		return GridToWorld(x, y);
	}

	public void ModifyNode(int x, int y, Node newVal)
	{
		if (AreGroundNodeCords(x, y))
		{
			nodes[GetNodeIdx(x, y)] = newVal;
		}
		else
		{
			paNormalNodes[x - width] = newVal;
		}
	}

	public Node GetNode(int x, int y)
	{
		if (AreGroundNodeCords(x, y))
		{
			int nodeIdx = GetNodeIdx(x, y);
			if (nodeIdx < 0 || nodeIdx >= nodes.Length)
			{
				Game.Log("X: " + x + " Y: " + y, Game.LogType.Error);
			}
			return nodes[nodeIdx];
		}
		return paNormalNodes[x - width];
	}

	public PassableAreaNode GetPaNode(int x, int y)
	{
		if (x < width)
		{
			return default(PassableAreaNode);
		}
		return paNodes[x - width];
	}

	public PassableAreaNode GetPaNode(int idx)
	{
		if (idx < 0 || idx >= paNodes.Length)
		{
			return default(PassableAreaNode);
		}
		return paNodes[idx];
	}

	public void GetPaNodeCords(int idx, out int x, out int y)
	{
		x = idx + width;
		y = idx / PassableArea.numNodes + 1;
	}

	public int GetPANodeAreaIndex(int x, int y)
	{
		if (AreGroundNodeCords(x, y))
		{
			return -1;
		}
		return x - width - (y - 1) * PassableArea.numNodes;
	}

	public int GetPANodeIndex(int x, int y)
	{
		if (AreGroundNodeCords(x, y))
		{
			return -1;
		}
		return x - width;
	}

	public Node GetNode(PPos pos)
	{
		WorldToGrid(pos, out var x, out var y);
		return GetNode(x, y);
	}

	public void IncVersion()
	{
		if (++version != 0)
		{
			return;
		}
		using (Game.Profile("PathData.ClearClosed", log: false, settings.multithreaded ? (-1) : 0, PathFinding.pfs_ClearClosed))
		{
			version = 1;
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i].closed = 0;
			}
			for (int j = 0; j < paNormalNodes.Length; j++)
			{
				paNormalNodes[j].closed = 0;
			}
		}
	}

	public void IncHighPFVersion()
	{
		if (++high_pf_version == 0)
		{
			high_pf_version = 1;
			ClearHighGridPF();
		}
	}

	private void ClearHighGridPF()
	{
		int num = highPFGrid_width;
		int num2 = highPFGrid_height;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				HighGridNode highGridNode = GetHighGridNode(j, i);
				GetHighNodeClosed(j, i);
				highGridNode.estimate = 0;
				highGridNode.path_eval = 0u;
				highGridNode.Open = false;
				highGridNode.Dir = 15;
				SetHighNodeClosed(j, i, 0);
				SetHighNode(j, i, highGridNode);
			}
		}
		if (highPassableAreaNodes != null)
		{
			for (int k = 0; k < highPassableAreaNodes.Count; k++)
			{
				HighPassableAreaNode highPassableAreaNode = highPassableAreaNodes[k];
				highPassableAreaNode.estimate = 0u;
				highPassableAreaNode.path_eval = 0u;
				highPassableAreaNode.closed = 0;
				highPassableAreaNode.bestComeFrom = null;
				highPassableAreaNode.bestComeFromTerrain = byte.MaxValue;
			}
		}
		if (highPFAdditionalNodes != null)
		{
			for (int l = 0; l < highPFAdditionalNodes.Length; l++)
			{
				HighAdditionalNode obj = highPFAdditionalNodes[l];
				obj.estimate = 0u;
				obj.path_eval = 0u;
				obj.closed = 0;
				obj.bestComeFrom = null;
				obj.bestComeFromTerrain = byte.MaxValue;
			}
		}
	}

	public bool IsPassable(PPos pt, float radius = 0f)
	{
		if (nodes == null || nodes.Length == 0)
		{
			return true;
		}
		Node node = GetNode(pt);
		return (float)GetWeight(node, radius, radius) > 0f;
	}

	public float GetClearance(PPos pt)
	{
		return (float)(int)GetNode(pt).clearance * settings.max_radius / 255f;
	}

	public float TraceDir(PPos pt, PPos dir_normalized, float max_dist, float radius = 0f, bool passable = true)
	{
		PPos pPos = dir_normalized * settings.tile_size;
		float num = 0f;
		while (true)
		{
			PPos pPos2 = pt + pPos;
			if (IsPassable(pPos2, radius) != passable)
			{
				return num;
			}
			num += settings.tile_size;
			if (num >= max_dist)
			{
				break;
			}
			pt = pPos2;
		}
		return max_dist;
	}

	public bool Trace(PPos from, PPos to, float radius = 0f, bool passable = true)
	{
		PPos dir_normalized = to - from;
		float num = dir_normalized.Normalize();
		return TraceDir(from, dir_normalized, num, radius, passable) >= num;
	}

	public float TraceExit(PPos pt, PPos dir_normalized, float max_dist)
	{
		PPos pPos = dir_normalized * settings.tile_size;
		float num = 0f;
		while (true)
		{
			PPos pPos2 = pt + pPos;
			Node node = GetNode(pt);
			if (node.river || node.water)
			{
				return num;
			}
			if (node.weight > 0 && !node.town)
			{
				return num;
			}
			num += settings.tile_size;
			if (num >= max_dist)
			{
				break;
			}
			pt = pPos2;
		}
		return max_dist;
	}

	public PPos GetRandomExitPoint(PPos pt, float max_dist, bool check_water = true)
	{
		float num = new Random().Next(360);
		for (int i = 0; i < 360; i++)
		{
			float num2 = num + (float)i;
			if (num2 > 360f)
			{
				num2 -= 360f;
			}
			PPos rotated = PPos.UnitRight.GetRotated(num2);
			float num3 = TraceExit(pt, rotated, max_dist);
			if (num3 < max_dist)
			{
				PPos pPos = pt + rotated * num3;
				if (!check_water || (!IsNearOcean(this, pPos, 6f) && !IsNearRiver(this, pPos, 6f)))
				{
					return pPos;
				}
			}
		}
		return pt;
	}

	public static bool IsNearRiver(PathData pf, Point p, float range)
	{
		int num = 16;
		Point point = new Point(0f, range);
		for (int i = 0; i < num; i++)
		{
			pf.WorldToGrid(p + point, out var x, out var y);
			if (pf.GetNode(x, y).river)
			{
				return true;
			}
			point = point.GetRotated(360 / num);
		}
		return false;
	}

	public static bool IsNearOcean(PathData pf, Point p, float range)
	{
		int num = 16;
		Point point = new Point(0f, range);
		for (int i = 0; i < num; i++)
		{
			pf.WorldToGrid(p + point, out var x, out var y);
			if (pf.GetNode(x, y).ocean)
			{
				return true;
			}
			point = point.GetRotated(360 / num);
		}
		return false;
	}

	public void BuildRuntimeHighPF(int area_count, Game game)
	{
		if (paIDGrid.IsCreated)
		{
			paIDGrid.Dispose();
		}
		paIDGrid = new NativeMultiHashMap<Coord, Coord>(area_count * 5, Allocator.Persistent);
		highPassableAreaNodes = new List<HighPassableAreaNode>();
		PathFinding path_finding = game.path_finding;
		for (int i = 1; i <= area_count; i++)
		{
			int idx = i - 1;
			PassableArea pA = path_finding.data.pointers.GetPA(idx);
			Point3 point = (pA.cornerVertex1 + pA.cornerVertex2 + pA.cornerVertex3 + pA.cornerVertex4) / 4f;
			CreatePassableAreaHighNode(new PPos(point.x, point.z, i));
		}
		for (int j = 1; j <= area_count; j++)
		{
			int num = j - 1;
			HighPassableAreaNode highPassableAreaNode = highPassableAreaNodes[num];
			for (int k = 0; k < PassableArea.numNodes; k++)
			{
				PassableAreaNode passableAreaNode = paNodes[num * PassableArea.numNodes + k];
				if (passableAreaNode.type == PassableAreaNode.Type.Normal)
				{
					PassableAreaNode paNode = path_finding.data.GetPaNode(passableAreaNode.link);
					if (paNode.pos.paID > highPassableAreaNodes.Count || !path_finding.data.pointers.GetPA(paNode.pos.paID - 1).enabled)
					{
						continue;
					}
					HighPassableAreaNode b = highPassableAreaNodes[paNode.pos.paID - 1];
					ConnectPassableAreaHighNode(highPassableAreaNode, b);
				}
				if (passableAreaNode.type != PassableAreaNode.Type.Ground)
				{
					continue;
				}
				PPos pos = passableAreaNode.pos;
				Coord coord = new Coord((int)(pos.x / settings.grid_tile_size), (int)(pos.y / settings.grid_tile_size));
				Point point2 = path_finding.GridCoordToWorld(coord);
				if (highPassableAreaNode.closest_reachable_terrain == null)
				{
					highPassableAreaNode.closest_reachable_terrain = new List<Coord>();
				}
				if (highPassableAreaNode.closest_reachable_terrain.Contains(coord))
				{
					continue;
				}
				if (IsPassable(point2) && Trace(new PPos(pos), point2))
				{
					ConnectPAToGround(path_finding, coord, j, highPassableAreaNode);
					continue;
				}
				for (int l = coord.x - 1; l <= coord.x + 1; l++)
				{
					bool flag = false;
					for (int m = coord.y - 1; m <= coord.y + 1; m++)
					{
						Coord coord2 = new Coord(l, m);
						point2 = path_finding.GridCoordToWorld(coord2);
						if (!highPassableAreaNode.closest_reachable_terrain.Contains(coord2) && IsPassable(point2) && Trace(new PPos(pos), point2))
						{
							ConnectPAToGround(path_finding, coord2, j, highPassableAreaNode);
							flag = true;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
		}
	}

	private void ConnectPAToGround(PathFinding lpf, Coord coord, int i, HighPassableAreaNode area_node)
	{
		HighGridNode highGridNode = GetHighGridNode(coord.x, coord.y);
		highGridNode.HasPassableArea = true;
		SetHighNode(coord.x, coord.y, highGridNode);
		uint y = PathFinding.CalcGroundWeight(lpf, lpf.GridCoordToWorld(coord), area_node.pos);
		paIDGrid.Add(coord, new Coord(i, (int)y));
		area_node.closest_reachable_terrain.Add(coord);
	}

	private HighPassableAreaNode CreatePassableAreaHighNode(PPos pos)
	{
		HighPassableAreaNode highPassableAreaNode = new HighPassableAreaNode();
		highPassableAreaNode.pos = pos;
		highPassableAreaNodes.Add(highPassableAreaNode);
		return highPassableAreaNode;
	}

	private void ConnectPassableAreaHighNode(HighPassableAreaNode a, HighPassableAreaNode b, float weight_mod = 1f)
	{
		HighPassableAreaRib highPassableAreaRib = new HighPassableAreaRib();
		highPassableAreaRib.node1 = a;
		highPassableAreaRib.node2 = b;
		PassableArea pA = pointers.GetPA(a.pos.paID - 1);
		PassableArea pA2 = pointers.GetPA(b.pos.paID - 1);
		if (pA.type == PassableArea.Type.Ladder || pA2.type == PassableArea.Type.Ladder || pA.type == PassableArea.Type.Stairs || pA2.type == PassableArea.Type.Stairs)
		{
			highPassableAreaRib.weight = 1;
		}
		else
		{
			highPassableAreaRib.weight = (ushort)PathFinding.CalcPassableAreaWeight(settings.base_weight, a.pos, b.pos);
		}
		a.ribs.Add(highPassableAreaRib);
		bool flag = false;
		for (int i = 0; i < PassableArea.numNodes; i++)
		{
			PassableAreaNode pANode = pointers.GetPANode((b.pos.paID - 1) * PassableArea.numNodes + i);
			if (pANode.type == PassableAreaNode.Type.Normal && pointers.GetPANode(pANode.link).pos.paID == a.pos.paID)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			HighPassableAreaRib highPassableAreaRib2 = new HighPassableAreaRib();
			highPassableAreaRib2.node1 = b;
			highPassableAreaRib2.node2 = a;
			highPassableAreaRib2.weight = 0;
			b.ribs.Add(highPassableAreaRib2);
		}
	}
}
