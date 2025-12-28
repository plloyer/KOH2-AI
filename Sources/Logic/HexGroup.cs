using System;
using System.Collections.Generic;

namespace Logic;

[Serializable]
public class HexGroup
{
	public List<HexGrid.Coord> hexes = new List<HexGrid.Coord>();

	public int rotation;

	public bool locked;

	public string nodeType;

	public string originalShape = "";

	public int Count => hexes.Count;

	public HexGrid.Coord this[int idx] => hexes[idx];

	public HexGroup(List<HexGrid.Coord> hexes, string originalShape = "", int rotation = 0, bool locked = false, string nodeType = "")
	{
		this.hexes = hexes;
		this.rotation = rotation;
		this.locked = locked;
		this.originalShape = originalShape;
		this.nodeType = nodeType;
	}

	public HexGroup(HexGroup g)
	{
		hexes = g.hexes;
		rotation = g.rotation;
		locked = g.locked;
		originalShape = g.originalShape;
		nodeType = g.nodeType;
	}

	public bool Contains(HexGrid.Coord c)
	{
		return hexes.Contains(c);
	}

	public bool Contains(HexGrid.Coord c, int dir)
	{
		HexGrid.Coord coord = HexGrid.Neighbor(c, dir);
		if (!(coord != c))
		{
			return false;
		}
		return Contains(coord);
	}

	public List<Point> GetEdges()
	{
		return GetEdges(hexes);
	}

	public static List<Point> GetEdges(List<HexGrid.Coord> coords)
	{
		List<Point> list = new List<Point>();
		for (int i = 0; i < coords.Count; i++)
		{
			for (int j = i; j < coords.Count; j++)
			{
				int dir = -1;
				coords[i].IsNeighbor(coords[j], out dir);
				if (dir != -1)
				{
					Point[] hexRowsAndCols = coords[i].GetHexRowsAndCols();
					if (dir == 1)
					{
						list.Add(hexRowsAndCols[1]);
						list.Add(hexRowsAndCols[2]);
					}
					else if (dir == 4)
					{
						list.Add(hexRowsAndCols[5]);
						list.Add(hexRowsAndCols[6]);
					}
					else
					{
						dir = ((dir != 0) ? ((dir > 4) ? 7 : (dir + 1)) : 0);
						list.Add(hexRowsAndCols[dir]);
					}
					coords[j].IsNeighbor(coords[i], out dir);
					hexRowsAndCols = coords[j].GetHexRowsAndCols();
					switch (dir)
					{
					case 1:
						list.Add(hexRowsAndCols[1]);
						list.Add(hexRowsAndCols[2]);
						break;
					case 4:
						list.Add(hexRowsAndCols[5]);
						list.Add(hexRowsAndCols[6]);
						break;
					}
				}
			}
		}
		return list;
	}

	public byte CalculateHexMask(HexGrid.Coord c)
	{
		int num = rotation % 6;
		int num2 = 255;
		for (int i = 0; i < 6; i++)
		{
			HexGrid.Coord c2 = HexGrid.Neighbor(c, i);
			if (Contains(c2))
			{
				num2 &= ~(1 << (i + 6 - num) % 6);
			}
		}
		return (byte)num2;
	}

	public HexGroup Rotate(int rotation)
	{
		HexGroup hexGroup = new HexGroup(this);
		List<HexGrid.Coord> list = new List<HexGrid.Coord>();
		HexGrid.Coord coord = hexes[0];
		for (int i = 0; i < hexes.Count; i++)
		{
			HexGrid.Coord coord2 = (hexes[i] - coord).Rotate(rotation);
			list.Add(coord2 + coord);
		}
		hexGroup.hexes = list;
		return hexGroup;
	}
}
