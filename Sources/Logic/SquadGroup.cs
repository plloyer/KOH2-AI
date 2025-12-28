using System.Collections.Generic;

namespace Logic;

public class SquadGroup
{
	public int group_id;

	public Kingdom kingdom;

	public float range;

	public List<Squad> squads { get; private set; }

	public SquadGroup(int id)
	{
		group_id = id;
		squads = new List<Squad>();
	}

	public bool ContainsSquad(Squad squad)
	{
		return squads.Contains(squad);
	}

	public void AddSquad(Squad squad)
	{
		if (!ContainsSquad(squad))
		{
			squads.Add(squad);
		}
	}

	public void RemoveSquad(Squad squad)
	{
		if (ContainsSquad(squad))
		{
			squads.Remove(squad);
		}
	}

	public PPos GetAveragePosition()
	{
		PPos pPos = default(PPos);
		foreach (Squad squad in squads)
		{
			pPos += squad.position;
		}
		return pPos / squads.Count;
	}

	public PPos MaxPoint(PPos direction)
	{
		PPos position = squads[0].position;
		for (int i = 1; i < squads.Count; i++)
		{
			if (direction.Dot(squads[i].position - position) > 0f)
			{
				position = squads[i].position;
			}
		}
		return position;
	}

	public Squad GetFurthestSquad(PPos direction)
	{
		PPos position = squads[0].position;
		Squad result = squads[0];
		for (int i = 1; i < squads.Count; i++)
		{
			if (direction.Dot(squads[i].position - position) > 0f)
			{
				position = squads[i].position;
				result = squads[i];
			}
		}
		return result;
	}

	public List<Squad> GetFurthestSquads(PPos direction, int max_count)
	{
		List<Squad> list = new List<Squad> { squads[0] };
		List<PPos> list2 = new List<PPos> { squads[0].position };
		for (int i = 1; i < squads.Count; i++)
		{
			for (int j = 0; j < list2.Count; j++)
			{
				if (direction.Dot(squads[i].position - list2[j]) > 0f)
				{
					list2.Insert(j, squads[i].position);
					list.Insert(j, squads[i]);
					while (list2.Count > max_count)
					{
						list2.RemoveAt(list2.Count - 1);
					}
					while (list.Count > max_count)
					{
						list.RemoveAt(list.Count - 1);
					}
					break;
				}
			}
		}
		return list;
	}

	public PPos CalcShape(PPos forward, out PPos max_left, out PPos max_right, out PPos max_forward, out PPos max_backward)
	{
		PPos averagePosition = GetAveragePosition();
		max_forward = MaxPoint(forward);
		max_backward = MaxPoint(-forward);
		max_right = MaxPoint(new PPos(forward.y, 0f - forward.x));
		max_left = MaxPoint(new PPos(0f - forward.y, forward.x));
		return averagePosition;
	}
}
