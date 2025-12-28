using System.Collections.Generic;

namespace Logic;

public class SquadsList
{
	private List<Squad>[] lists = new List<Squad>[2]
	{
		new List<Squad>(),
		new List<Squad>()
	};

	public int Count => lists[0].Count + lists[1].Count;

	public Squad this[int idx]
	{
		get
		{
			if (idx < lists[0].Count)
			{
				return lists[0][idx];
			}
			return lists[1][idx - lists[0].Count];
		}
	}

	public int GetCount(int side)
	{
		return lists[side].Count;
	}

	public Squad Get(int side, int idx)
	{
		return lists[side][idx];
	}

	public List<Squad> Get(int side)
	{
		return lists[side];
	}

	public void Add(Squad squad)
	{
		lists[squad.battle_side].Add(squad);
	}

	public bool Del(Squad squad)
	{
		return lists[squad.battle_side].Remove(squad);
	}
}
