using System;
using System.Collections.Generic;

namespace Logic;

public class Statuses : Component
{
	public Status main;

	public List<Status> additional;

	public List<Status> automatic;

	public Type default_type;

	public int last_usid;

	public int Count
	{
		get
		{
			obj.UpdateAutomaticStatuses(now: true);
			int num = 1;
			if (additional != null && additional.Count > 0)
			{
				num += additional.Count;
			}
			if (automatic != null && automatic.Count > 0)
			{
				num += automatic.Count;
			}
			if (num == 1 && main == null)
			{
				return 0;
			}
			return num;
		}
	}

	public Status this[int i] => Get(i);

	public Statuses(Object obj)
		: base(obj)
	{
	}

	public int PersistentCount()
	{
		if (additional == null || additional.Count == 0)
		{
			if (main != null && !main.IsAutomatic())
			{
				return 1;
			}
			return 0;
		}
		return 1 + additional.Count;
	}

	public Status Get(int idx)
	{
		obj.UpdateAutomaticStatuses(now: true);
		if (idx == 0)
		{
			return main;
		}
		idx--;
		if (additional != null && additional.Count > 0)
		{
			if (idx < additional.Count)
			{
				return additional[idx];
			}
			idx -= additional.Count;
		}
		if (automatic != null && automatic.Count > 0)
		{
			if (idx < automatic.Count)
			{
				return automatic[idx];
			}
			idx -= automatic.Count;
		}
		return null;
	}

	public void Set(int idx, Status status, bool send_state = true)
	{
		if (idx == 0)
		{
			SetMain(status);
			return;
		}
		if (status != null && status.IsAutomatic())
		{
			Game.Log("Statuses.Set(idx) called for automatic status " + status.ToString(), Game.LogType.Warning);
			return;
		}
		idx--;
		if (additional == null)
		{
			if (status == null)
			{
				return;
			}
			additional = new List<Status>(idx + 1);
		}
		if (status != null)
		{
			while (additional.Count <= idx)
			{
				additional.Add(null);
			}
		}
		else if (idx >= additional.Count)
		{
			return;
		}
		Status status2 = additional[idx];
		if (status2 != status)
		{
			if (status2 != null)
			{
				status2.SetOwner(null);
				status2.Destroy();
			}
			additional[idx] = status;
			status?.SetOwner(obj);
			obj.UpdateAutomaticStatuses();
			obj.NotifyListeners("statuses_changed");
			if (send_state)
			{
				SendState();
			}
		}
	}

	public void SetMain(Status status, bool send_state = true)
	{
		if (main == status)
		{
			return;
		}
		if ((main == null || main.IsAutomatic()) && (status == null || status.IsAutomatic()))
		{
			send_state = false;
		}
		if (main != null)
		{
			if (status == null && default_type != null && main.rtti.type == default_type)
			{
				return;
			}
			main.SetOwner(null);
			if (main.IsValid())
			{
				main.Destroy();
			}
			main = null;
		}
		if (status == null && default_type != null)
		{
			status = Status.Create(base.game, default_type);
		}
		main = status;
		status?.SetOwner(obj);
		obj.UpdateAutomaticStatuses();
		obj.NotifyListeners("status_changed");
		if (send_state)
		{
			SendState();
		}
	}

	public void Add(Status status, bool send_state = true)
	{
		if (status == null || (!status.AllowMultiple() && Find(status.def) != null))
		{
			return;
		}
		if (status.IsAutomatic())
		{
			send_state = false;
			if (automatic == null)
			{
				automatic = new List<Status>();
			}
			automatic.Add(status);
		}
		else
		{
			if (!obj.IsAuthority())
			{
				Warning("Adding non automatic status " + status.ToString() + " for non authority!");
			}
			if (additional == null)
			{
				additional = new List<Status>();
			}
			additional.Add(status);
		}
		status.SetOwner(obj);
		obj.UpdateAutomaticStatuses();
		obj.NotifyListeners("statuses_changed");
		if (send_state)
		{
			SendState();
		}
	}

	public void Del(int idx, bool send_state = true)
	{
		if (idx < 0)
		{
			return;
		}
		if (idx == 0)
		{
			SetMain(null);
			return;
		}
		idx--;
		bool flag = false;
		if (additional != null)
		{
			if (idx < additional.Count)
			{
				Status status = additional[idx];
				if (status != null && status.IsValid())
				{
					status.SetOwner(null);
					if (status.IsValid())
					{
						status.Destroy();
					}
				}
				additional.RemoveAt(idx);
				flag = true;
			}
			if (!flag)
			{
				idx -= additional.Count;
			}
		}
		if (!flag && automatic != null)
		{
			if (idx < automatic.Count)
			{
				Status status2 = automatic[idx];
				if (status2 != null && status2.IsValid())
				{
					status2.SetOwner(null);
					if (status2.IsValid())
					{
						status2.Destroy();
					}
				}
				automatic.RemoveAt(idx);
				flag = true;
			}
			if (!flag)
			{
				idx -= automatic.Count;
			}
		}
		obj.UpdateAutomaticStatuses();
		obj.NotifyListeners("statuses_changed");
		if (send_state)
		{
			SendState();
		}
	}

	public void Del(Status status, bool send_state = true, bool destroy = true)
	{
		if (status == null)
		{
			return;
		}
		if (main == status)
		{
			SetMain(null, send_state);
			return;
		}
		if (status.IsAutomatic())
		{
			send_state = false;
			if (automatic != null)
			{
				automatic.Remove(status);
			}
		}
		else if (additional == null || !additional.Remove(status))
		{
			obj.Warning("Attempting to delete unknown status: " + status.ToString());
			return;
		}
		if (status.IsValid())
		{
			status.SetOwner(null);
			if (status.IsValid() && destroy)
			{
				status.Destroy();
			}
		}
		obj.UpdateAutomaticStatuses();
		obj.NotifyListeners("statuses_changed");
		if (send_state)
		{
			SendState();
		}
	}

	public Status Find(int usid)
	{
		if (usid <= 0)
		{
			return null;
		}
		if (main != null && main.usid == usid)
		{
			return main;
		}
		if (additional == null)
		{
			return null;
		}
		for (int i = 0; i < additional.Count; i++)
		{
			Status status = additional[i];
			if (status != null && status.usid == usid)
			{
				return status;
			}
		}
		return null;
	}

	public Status Find(Status.Def def)
	{
		obj.UpdateAutomaticStatuses(now: true);
		if (main != null && main.def == def)
		{
			return main;
		}
		if (additional != null)
		{
			for (int i = 0; i < additional.Count; i++)
			{
				Status status = additional[i];
				if (status != null && status.def == def)
				{
					return status;
				}
			}
		}
		if (automatic != null)
		{
			for (int j = 0; j < automatic.Count; j++)
			{
				Status status2 = automatic[j];
				if (status2 != null && status2.def == def)
				{
					return status2;
				}
			}
		}
		return null;
	}

	public int FindIndex<T>() where T : Status
	{
		obj.UpdateAutomaticStatuses(now: true);
		if (main is T)
		{
			return 0;
		}
		int num = 1;
		if (additional != null)
		{
			for (int i = 0; i < additional.Count; i++)
			{
				if (additional[i] as T != null)
				{
					return i + num;
				}
			}
			num += additional.Count;
		}
		if (automatic != null)
		{
			for (int j = 0; j < automatic.Count; j++)
			{
				if (automatic[j] as T != null)
				{
					return j + num;
				}
			}
			num += automatic.Count;
		}
		return -1;
	}

	public Status Find(Type type)
	{
		obj.UpdateAutomaticStatuses(now: true);
		if (main != null && type.IsAssignableFrom(main.rtti.type))
		{
			return main;
		}
		if (additional != null)
		{
			for (int i = 0; i < additional.Count; i++)
			{
				Status status = additional[i];
				if (status != null && type.IsAssignableFrom(status.rtti.type))
				{
					return status;
				}
			}
		}
		if (automatic != null)
		{
			for (int j = 0; j < automatic.Count; j++)
			{
				Status status2 = automatic[j];
				if (status2 != null && type.IsAssignableFrom(status2.rtti.type))
				{
					return status2;
				}
			}
		}
		return null;
	}

	public T Find<T>() where T : Status
	{
		obj.UpdateAutomaticStatuses(now: true);
		if (main is T)
		{
			return (T)main;
		}
		if (additional != null)
		{
			for (int i = 0; i < additional.Count; i++)
			{
				if (additional[i] is T result)
				{
					return result;
				}
			}
		}
		if (automatic != null)
		{
			for (int j = 0; j < automatic.Count; j++)
			{
				if (automatic[j] is T result2)
				{
					return result2;
				}
			}
		}
		return null;
	}

	public void SetDefaultType(Type type, bool send_state = true)
	{
		if (!(default_type == type))
		{
			default_type = type;
			if (send_state)
			{
				SendDefaultTypeState();
			}
		}
	}

	public void DestroyAll(bool clear_default_status = true)
	{
		if (clear_default_status)
		{
			default_type = null;
		}
		if (main != null)
		{
			if (main.IsValid())
			{
				main.Destroy();
			}
			main = null;
		}
		if (additional != null)
		{
			for (int i = 0; i < additional.Count; i++)
			{
				Status status = additional[i];
				if (status != null && status.IsValid())
				{
					Del(status);
					i--;
				}
			}
			additional = null;
		}
		if (automatic != null)
		{
			for (int j = 0; j < automatic.Count; j++)
			{
				Status status2 = automatic[j];
				if (status2 != null && status2.IsValid())
				{
					status2.Destroy();
				}
			}
			automatic = null;
		}
		obj.NotifyListeners("statuses_changed");
	}

	public override void OnDestroy()
	{
		DestroyAll();
		base.OnDestroy();
	}

	public void SendState()
	{
		obj.SendState<Object.StatusesState>();
	}

	public void SendDefaultTypeState()
	{
		obj.SendState<Object.DefaultStatusTypeState>();
	}

	public override void OnStart()
	{
		UpdateNextFrame();
	}

	public override void OnUpdate()
	{
		obj?.UpdateAutomaticStatuses(now: true);
	}
}
