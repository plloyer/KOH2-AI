using System;
using System.Collections.Generic;

namespace Logic;

public class Quests : Component
{
	private List<Quest> quests = new List<Quest>();

	public int last_uqid;

	public int Count
	{
		get
		{
			if (quests == null)
			{
				return 0;
			}
			return quests.Count;
		}
	}

	public Kingdom kingdom
	{
		get
		{
			return obj as Kingdom;
		}
		set
		{
			obj = value;
		}
	}

	public Quest this[int i] => Get(i);

	public Quests(Object owner)
		: base(owner)
	{
	}

	public Quest Find(int uqid)
	{
		if (uqid <= 0)
		{
			for (int i = 0; i < quests.Count; i++)
			{
				Quest quest = quests[i];
				if (quest != null && quest.uqid == uqid)
				{
					return quest;
				}
			}
		}
		return null;
	}

	public Quest Find(string def_id)
	{
		Quest.Def def = base.game.defs.Get<Quest.Def>(def_id);
		if (def == null)
		{
			return null;
		}
		return Find(def);
	}

	public Quest Find(Quest.Def def)
	{
		if (def == null)
		{
			return null;
		}
		if (Count == 0)
		{
			return null;
		}
		for (int i = 0; i < quests.Count; i++)
		{
			Quest quest = quests[i];
			if (quest != null && quest.def == def)
			{
				return quest;
			}
		}
		return null;
	}

	public Quest Find(Type type)
	{
		if (type == null)
		{
			return null;
		}
		if (Count == 0)
		{
			return null;
		}
		for (int i = 0; i < quests.Count; i++)
		{
			Quest quest = quests[i];
			if (quest != null && type.IsAssignableFrom(quest.rtti.type))
			{
				return quest;
			}
		}
		return null;
	}

	public T Find<T>() where T : Quest
	{
		if (Count == 0)
		{
			return null;
		}
		for (int i = 0; i < quests.Count; i++)
		{
			if (quests[i] is T)
			{
				return quests[i] as T;
			}
		}
		return null;
	}

	public Quest Get(int idx)
	{
		if (quests == null)
		{
			return null;
		}
		if (quests.Count == 0)
		{
			return null;
		}
		if (quests.Count <= idx)
		{
			return null;
		}
		return quests[idx];
	}

	public void Set(int idx, Quest quest, bool send_state = true)
	{
		while (quests.Count <= idx)
		{
			quests.Add(null);
		}
		if (quests[idx] != null)
		{
			Game.Log($"Setting quest at non empty slot {idx} current quest {quests[idx]} new {quest}", Game.LogType.Warning);
		}
		quests[idx] = quest;
		obj.NotifyListeners("quest_changed");
		if (send_state)
		{
			SendState();
		}
	}

	public void Add(string def_id, bool send_state = true)
	{
		Quest.Def def = base.game.defs.Get<Quest.Def>(def_id);
		Add(def, send_state);
	}

	public void Add(Quest.Def def, bool send_state = true)
	{
		if (def != null)
		{
			Add(Quest.Create(def, obj, null), send_state);
		}
	}

	public void Add(Quest data, bool send_state = true)
	{
		if (data != null && Find(data.def) == null)
		{
			data.uqid = ++last_uqid;
			kingdom.AddListener(data);
			quests.Add(data);
			obj.NotifyListeners("quest_changed");
			if (send_state)
			{
				SendState();
			}
		}
	}

	public void Remove(Quest data, bool send_state = true)
	{
		if (data != null && Find(data.def) != null)
		{
			kingdom.DelListener(data);
			quests.Remove(data);
			obj.NotifyListeners("quest_changed");
			if (send_state)
			{
				SendState();
			}
		}
	}

	public void RemoveAllQuests()
	{
		while (quests.Count > 0)
		{
			kingdom.DelListener(quests[0]);
			quests.Remove(quests[0]);
		}
		obj.NotifyListeners("quest_changed");
		SendState();
	}

	public void SendState()
	{
		obj.SendState<Object.QuestsState>();
	}

	public List<Quest> GetQuests()
	{
		return quests;
	}

	private void UpdateQuests()
	{
	}

	public void AddBaseQuests()
	{
		List<Quest.Def> defs = base.game.defs.GetDefs<Quest.Def>();
		for (int i = 0; i < defs.Count; i++)
		{
			Quest.Def def = defs[i];
			if (def.CheckActivateConditions(def, obj))
			{
				Quest data = Quest.Create(defs[i], obj as Kingdom, null);
				Add(data);
			}
		}
	}

	public void DestroyAll()
	{
		throw new NotImplementedException();
	}
}
