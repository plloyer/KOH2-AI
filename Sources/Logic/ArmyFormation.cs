using System;
using System.Collections.Generic;

namespace Logic;

public abstract class ArmyFormation
{
	public const float Guid_Mul = 1E-05f;

	public Dictionary<Squad, PPos> offsets = new Dictionary<Squad, PPos>();

	public FormationLists<Squad> formation_squads = new FormationLists<Squad>();

	public abstract bool CreateFormation(PPos pos, float heading, List<Squad> squads, out Dictionary<Squad, PPos> offsets, Func<PPos, float, bool> IsPassable, bool use_hungarian_algorithm = false);

	public virtual float Order(Squad sq)
	{
		if (sq == null || sq.def == null)
		{
			return 0f;
		}
		return GetSquadTypeInt(sq) + (sq.def.CTH * 0.01f - (float)sq.uid * 1E-05f);
	}

	private float GetSquadTypeInt(Squad sq)
	{
		if (sq == null || sq.def == null)
		{
			return 0f;
		}
		float num = 1f;
		if (sq.def.is_siege_eq)
		{
			return 7f;
		}
		if (sq.def.type == Unit.Type.Noble || sq.def.secondary_type == Unit.Type.Noble)
		{
			return 6f;
		}
		if (!sq.def.is_cavalry)
		{
			if (sq.def.is_defense)
			{
				return 4f;
			}
			if (sq.def.is_ranged)
			{
				return 3f;
			}
			return 5f;
		}
		if (!sq.def.is_ranged)
		{
			return 2f;
		}
		return 1f;
	}

	public void UpdateFormationSquads(List<Squad> squads)
	{
		squads.Sort((Squad x, Squad y) => Order(y).CompareTo(Order(x)));
		formation_squads = new FormationLists<Squad>();
		Squad squad = FindCommander(squads);
		formation_squads.commander = squad;
		foreach (Squad squad2 in squads)
		{
			float squadTypeInt = GetSquadTypeInt(squad2);
			if (!6f.Equals(squadTypeInt))
			{
				if (!5f.Equals(squadTypeInt))
				{
					if (!4f.Equals(squadTypeInt))
					{
						if (!3f.Equals(squadTypeInt))
						{
							if (!2f.Equals(squadTypeInt))
							{
								if (1f.Equals(squadTypeInt) && squad2 != squad)
								{
									formation_squads.ranged_cavalry.Add(squad2);
								}
							}
							else if (squad2 != squad)
							{
								formation_squads.cavalry.Add(squad2);
							}
						}
						else if (squad2 != squad)
						{
							formation_squads.ranged.Add(squad2);
						}
					}
					else if (squad2 != squad)
					{
						formation_squads.defense.Add(squad2);
					}
				}
				else if (squad2 != squad)
				{
					formation_squads.melee.Add(squad2);
				}
			}
			else if (squad2 != squad)
			{
				formation_squads.noble.Add(squad2);
			}
		}
	}

	protected void AssignSquads(List<Squad> squads, List<PPos> offsets, ref Dictionary<Squad, PPos> global_offsets, PPos center, float heading, bool use_hungarian_algorithm = true)
	{
		if (squads == null || offsets == null)
		{
			return;
		}
		if (use_hungarian_algorithm)
		{
			int count = squads.Count;
			int[,] array = new int[count, count];
			int[] array2 = new int[count];
			if (count == 0)
			{
				return;
			}
			for (int i = 0; i < count; i++)
			{
				for (int j = 0; j < count; j++)
				{
					float num = (center + offsets[j].GetRotated(0f - heading)).SqrDist(squads[i].position);
					array[i, j] = (int)(num * 1000f);
				}
			}
			array2 = new HungarianAlgorithm(array).Run();
			for (int k = 0; k < count; k++)
			{
				global_offsets.Add(squads[k], offsets[array2[k]]);
			}
		}
		else
		{
			for (int l = 0; l < squads.Count; l++)
			{
				global_offsets.Add(squads[l], offsets[l]);
			}
		}
	}

	public static Squad FindCommander(List<Squad> squads)
	{
		Squad squad = null;
		foreach (Squad squad2 in squads)
		{
			if (squad2 != null && squad2.IsValid() && !squad2.IsDefeated())
			{
				if (squad2.def.type == Unit.Type.Noble)
				{
					return squad2;
				}
				if (squad == null || squad.def.CTH < squad2.def.CTH)
				{
					squad = squad2;
				}
			}
		}
		return squad;
	}

	public static Squad FindSlowestSquad(List<Squad> squads, out float min_speed)
	{
		if (squads.Count == 0)
		{
			min_speed = 0f;
			return null;
		}
		min_speed = float.MaxValue;
		Squad result = null;
		foreach (Squad squad in squads)
		{
			if (min_speed > squad.normal_move_speed)
			{
				min_speed = squad.normal_move_speed;
				result = squad;
			}
		}
		return result;
	}
}
