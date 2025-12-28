using System;

namespace Logic;

public class Population
{
	public enum Type
	{
		Rebel,
		Worker,
		TOTAL
	}

	private Castle castle;

	public float pop_acc;

	public float rebelion_acc;

	public int workers;

	public int rebels;

	private int[] slots = new int[3];

	private int[] count = new int[3];

	private int[] old_count = new int[3];

	private long last_calc_frame = -1L;

	public int GetRebels()
	{
		return rebels;
	}

	public int GetWorkers()
	{
		return workers;
	}

	public Population(Castle castle)
	{
		this.castle = castle;
		castle.population = this;
		MaxOut();
	}

	public void Update()
	{
		if (castle.game.isInVideoMode || !castle.IsAuthority() || castle.battle != null)
		{
			return;
		}
		if (Recalc(clamp: true))
		{
			castle.SendState<Castle.PopulationState>();
		}
		if (count[1] >= slots[1])
		{
			return;
		}
		Realm realm = castle.GetRealm();
		Kingdom kingdom = realm?.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		float num = realm.income[ResourceType.Food];
		float population_growth_from_surplus_food = castle.def.population_growth_from_surplus_food;
		float sufficentFoodMod = kingdom.GetSufficentFoodMod();
		float num2 = realm.GetStat(Stats.rs_growth_rate_perc) * 0.01f;
		float num3 = (castle.def.population_growth_base + num * population_growth_from_surplus_food) * num2 * sufficentFoodMod;
		pop_acc += num3;
		while (pop_acc >= castle.def.population_growth_threshold)
		{
			pop_acc -= castle.def.population_growth_threshold;
			float stat = realm.GetStat(Stats.rs_growth_chance_perc);
			if (realm.game.Random(0f, 100f) <= stat)
			{
				AddVillagers(1, Type.Worker);
			}
		}
		castle.SendState<Castle.PopulationState>();
	}

	private void CheckUpToDate()
	{
		if (castle.game.scheduler.Frame != last_calc_frame)
		{
			Game.Log("Population queried without being up-to-date", Game.LogType.Warning);
		}
	}

	public int Count(Type type, bool check_up_to_date = true)
	{
		if (check_up_to_date)
		{
			CheckUpToDate();
		}
		return count[(int)type];
	}

	public int Slots(Type type, bool check_up_to_date = true)
	{
		if (check_up_to_date)
		{
			CheckUpToDate();
		}
		return slots[(int)type];
	}

	public void Clear()
	{
		workers = 0;
		Recalc(clamp: true);
	}

	public void MaxOut()
	{
		if (castle.IsAuthority())
		{
			CalcSlots(force: true);
			workers = slots[1];
			rebels = 0;
			CalcCounts();
			castle.SendState<Castle.PopulationState>();
		}
	}

	public bool Recalc(bool clamp = false)
	{
		for (int i = 0; i < old_count.Length; i++)
		{
			old_count[i] = count[i];
		}
		clamp = !Game.isLoadingSaveGame && clamp && castle != null && castle.IsAuthority() && castle.started;
		Vars.ReflectionMode old_mode = Vars.PushReflectionMode(Vars.ReflectionMode.Disabled);
		CalcSlots();
		CalcCounts(clamp);
		Vars.PopReflectionMode(old_mode);
		for (int j = 0; j < old_count.Length; j++)
		{
			if (old_count[j] != count[j])
			{
				return true;
			}
		}
		return false;
	}

	private void CalcSlots(bool force = false)
	{
		last_calc_frame = castle.game.scheduler.Frame;
		for (int i = 0; i <= 2; i++)
		{
			slots[i] = 0;
		}
		Realm realm = castle.GetRealm();
		if (realm != null)
		{
			realm.RecalcIncomes();
			if (realm.incomes != null)
			{
				slots[1] = (int)Math.Ceiling(realm.incomes[ResourceType.WorkerSlots].value.untaxed_value);
				slots[2] = slots[1];
				slots[0] = 0;
			}
		}
	}

	private void CalcCounts(bool clamp = true)
	{
		if (clamp && slots[1] > 0)
		{
			rebels = (count[0] = Math.Min(rebels, slots[1]));
			workers = (count[1] = Math.Min(workers, slots[1] - rebels));
		}
		else
		{
			count[0] = rebels;
			count[1] = workers;
		}
		count[2] = workers + rebels;
		slots[0] = rebels;
		slots[1] -= rebels;
	}

	public bool ConvertToRebel(int count, bool send_state = true)
	{
		if (!castle.IsAuthority())
		{
			return false;
		}
		if (castle.game.session_time.minutes < castle.game.GetMinRebelPopTime())
		{
			return false;
		}
		if (workers < count)
		{
			return false;
		}
		workers -= count;
		rebels += count;
		Recalc();
		castle.NotifyListeners("population_changed");
		castle.GetRealm()?.InvalidateIncomes();
		if (send_state)
		{
			castle.SendState<Castle.PopulationState>();
		}
		return true;
	}

	public bool ConvertToWorker(int count, bool send_state = true)
	{
		if (!castle.IsAuthority())
		{
			return false;
		}
		if (rebels < count)
		{
			return false;
		}
		rebels -= count;
		workers += count;
		Recalc();
		castle.NotifyListeners("population_changed");
		castle.GetRealm()?.InvalidateIncomes();
		if (send_state)
		{
			castle.SendState<Castle.PopulationState>();
		}
		return true;
	}

	public float GetNextVilagerProgress(Type type)
	{
		if (type == Type.Worker)
		{
			return Game.map_clamp(pop_acc, 0f, castle.def.population_growth_threshold, 0f, 1f);
		}
		return 0f;
	}

	public bool AddVillagers(int count, Type type, bool send_state = true)
	{
		if (count <= 0)
		{
			return true;
		}
		switch (type)
		{
		case Type.Worker:
			workers += count;
			if (workers > slots[(int)type])
			{
				workers = slots[(int)type];
			}
			break;
		case Type.Rebel:
			rebels += count;
			if (rebels > slots[(int)type])
			{
				rebels = slots[(int)type];
			}
			break;
		}
		Recalc();
		castle.NotifyListeners("population_changed");
		castle.GetRealm()?.InvalidateIncomes();
		if (send_state)
		{
			castle.SendState<Castle.PopulationState>();
		}
		return true;
	}

	public bool RemoveVillagers(int count, Type type, bool send_state = true)
	{
		if (count <= 0)
		{
			return true;
		}
		switch (type)
		{
		case Type.Worker:
		{
			int num2 = Math.Min(count, workers);
			workers -= num2;
			count -= num2;
			break;
		}
		case Type.Rebel:
		{
			int num = Math.Min(count, rebels);
			rebels -= num;
			count -= num;
			break;
		}
		}
		Recalc();
		castle.NotifyListeners("population_changed");
		castle.GetRealm()?.InvalidateIncomes();
		if (send_state)
		{
			castle.SendState<Castle.PopulationState>();
		}
		return true;
	}

	public bool RemoveVillagers(int count, bool send_state = true)
	{
		if (count <= 0)
		{
			return true;
		}
		if (count > this.count[2])
		{
			return false;
		}
		int num = Math.Min(count, workers);
		workers -= num;
		count -= num;
		int num2 = Math.Min(count, rebels);
		rebels -= num2;
		count -= num2;
		Recalc();
		castle.NotifyListeners("population_changed");
		castle.GetRealm()?.InvalidateIncomes();
		if (send_state)
		{
			castle.SendState<Castle.PopulationState>();
		}
		return true;
	}

	public override string ToString()
	{
		return count[2] + "/" + slots[2] + " (R: " + count[0] + "/" + slots[0] + ", W: " + count[1] + "/" + slots[1];
	}
}
