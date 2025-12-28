using System;
using System.Collections.Generic;

namespace Logic;

[Serializable]
public class HexGroups
{
	public List<HexGroup> groups = new List<HexGroup>();

	public Dictionary<HexGrid.Coord, HexGroup> lookup = new Dictionary<HexGrid.Coord, HexGroup>();

	public int Count => groups.Count;

	public HexGroup this[int idx] => groups[idx];

	public HexGroups()
	{
	}

	public HexGroups(List<HexGroup> groups)
	{
		this.groups = groups;
		for (int i = 0; i < groups.Count; i++)
		{
			HexGroup hexGroup = groups[i];
			for (int j = 0; j < hexGroup.Count; j++)
			{
				lookup[hexGroup[j]] = hexGroup;
			}
		}
	}

	public bool Contains(HexGrid.Coord c)
	{
		lookup.TryGetValue(c, out var value);
		return value != null;
	}

	public bool Contains(HexGroup g)
	{
		return groups.Contains(g);
	}

	public void Remove(HexGroup g)
	{
		for (int i = 0; i < g.hexes.Count; i++)
		{
			lookup.Remove(g.hexes[i]);
		}
		groups.Remove(g);
	}

	public HexGroup GetGroup(HexGrid.Coord c)
	{
		lookup.TryGetValue(c, out var value);
		return value;
	}

	public void Add(HexGroup grp)
	{
		groups.Add(grp);
		for (int i = 0; i < grp.Count; i++)
		{
			lookup[grp[i]] = grp;
		}
	}

	public bool HaveSameGroup(HexGrid.Coord c1, HexGrid.Coord c2)
	{
		HexGroup hexGroup = GetGroup(c1);
		HexGroup hexGroup2 = GetGroup(c2);
		if (hexGroup != null)
		{
			return hexGroup == hexGroup2;
		}
		return false;
	}

	public byte CalculateHexMask(HexGrid.Coord c)
	{
		byte b = byte.MaxValue;
		return GetGroup(c)?.CalculateHexMask(c) ?? b;
	}
}
