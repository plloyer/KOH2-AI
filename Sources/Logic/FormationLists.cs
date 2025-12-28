using System.Collections.Generic;

namespace Logic;

public class FormationLists<T>
{
	public List<T> ranged = new List<T>();

	public List<T> melee = new List<T>();

	public List<T> defense = new List<T>();

	public List<T> cavalry = new List<T>();

	public List<T> ranged_cavalry = new List<T>();

	public List<T> siege_eq = new List<T>();

	public List<T> noble = new List<T>();

	public T commander;

	public List<T> GetAll()
	{
		List<T> list = new List<T>();
		list.Add(commander);
		foreach (T item in ranged)
		{
			list.Add(item);
		}
		foreach (T item2 in melee)
		{
			list.Add(item2);
		}
		foreach (T item3 in defense)
		{
			list.Add(item3);
		}
		foreach (T item4 in cavalry)
		{
			list.Add(item4);
		}
		foreach (T item5 in ranged_cavalry)
		{
			list.Add(item5);
		}
		foreach (T item6 in siege_eq)
		{
			list.Add(item6);
		}
		foreach (T item7 in noble)
		{
			list.Add(item7);
		}
		return list;
	}

	public List<T> GetAllCavalry()
	{
		List<T> list = new List<T>();
		foreach (T item in cavalry)
		{
			list.Add(item);
		}
		foreach (T item2 in ranged_cavalry)
		{
			list.Add(item2);
		}
		foreach (T item3 in noble)
		{
			list.Add(item3);
		}
		return list;
	}

	public List<T> GetAllCavalryWithoutNoble()
	{
		List<T> list = new List<T>();
		foreach (T item in cavalry)
		{
			list.Add(item);
		}
		foreach (T item2 in ranged_cavalry)
		{
			list.Add(item2);
		}
		return list;
	}

	public List<T> GetAllInfantry()
	{
		List<T> list = new List<T>();
		foreach (T item in ranged)
		{
			list.Add(item);
		}
		foreach (T item2 in melee)
		{
			list.Add(item2);
		}
		foreach (T item3 in defense)
		{
			list.Add(item3);
		}
		return list;
	}

	public List<T> GetAllNotRangedInfantry()
	{
		List<T> list = new List<T>();
		foreach (T item in melee)
		{
			list.Add(item);
		}
		foreach (T item2 in defense)
		{
			list.Add(item2);
		}
		return list;
	}
}
