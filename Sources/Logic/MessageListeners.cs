using System;
using System.Collections.Generic;
using System.Text;

namespace Logic;

public class GameRules : BaseObject
{
	public interface IListener
	{
		void OnTrigger(Trigger trigger, GameRule parent_rule = null, RuleTimer parent_timer = null);
	}

	private struct Listener
	{
		public IListener listener;

		public Trigger.Def tdef;

		public GameRule parent_rule;

		public RuleTimer parent_timer;

		public bool removed;

		public int calls;

		private static Trigger tmp_trigger = new Trigger();

		public Listener(IListener listener)
		{
			this.listener = listener;
			tdef = null;
			parent_rule = null;
			parent_timer = null;
			removed = false;
			calls = 0;
		}

		public Listener(GameRule.Def def, Trigger.Def tdef, GameRule parent_rule = null, RuleTimer parent_timer = null)
		{
			listener = def;
			this.tdef = tdef;
			this.parent_rule = parent_rule;
			this.parent_timer = parent_timer;
			removed = false;
			calls = 0;
		}

		public Listener(GameRule rule, Trigger.Def tdef)
		{
			listener = rule;
			this.tdef = tdef;
			parent_rule = rule.parent_rule;
			parent_timer = rule.parent_timer;
			removed = false;
			calls = 0;
		}

		public bool Eq(Listener l)
		{
			if (l.listener != listener)
			{
				return false;
			}
			if (l.tdef != tdef)
			{
				return false;
			}
			if (l.parent_rule != parent_rule)
			{
				return false;
			}
			if (l.parent_timer != parent_timer)
			{
				return false;
			}
			return true;
		}

		public void Call(Object sender, string message, object param, int id)
		{
			if (removed)
			{
				return;
			}
			tmp_trigger.Set(tdef, sender, message, param, id, null);
			try
			{
				listener.OnTrigger(tmp_trigger, parent_rule, parent_timer);
			}
			catch (Exception arg)
			{
				Game.Log($"Error processing {listener}({tmp_trigger}): {arg}", Game.LogType.Error);
			}
		}

		public override string ToString()
		{
			string text = ((!(listener is TargetType targetType) || parent_rule == null) ? $"{tdef} -> {listener}" : ("validate '" + targetType.name + "' for rule " + parent_rule.name));
			if (removed)
			{
				text = "(removed) " + text;
			}
			return $"[x{calls}] {text}";
		}
	}

	private class MessageListeners
	{
		public Dictionary<Reflection.TypeInfo, ListenersList> per_obj_type = new Dictionary<Reflection.TypeInfo, ListenersList>();

		public Dictionary<Object, ListenersList> per_obj = new Dictionary<Object, ListenersList>();

		public int calls;

		public override string ToString()
		{
			string text = $"[x{calls}]";
			int num = 0;
			foreach (KeyValuePair<Reflection.TypeInfo, ListenersList> item in per_obj_type)
			{
				Reflection.TypeInfo key = item.Key;
				ListenersList value = item.Value;
				text += ", ";
				if (num > 1)
				{
					text += $"... ({per_obj_type.Count - 2})";
					break;
				}
				text += $"{key.name}{value}";
			}
			return text;
		}
	}

	private class ListenersList
	{
		public List<Listener> lst;

		public int removed;

		public int calls;

		public bool Empty
		{
			get
			{
				if (lst != null)
				{
					return lst.Count <= removed;
				}
				return true;
			}
		}

		public int Count
		{
			get
			{
				if (lst != null)
				{
					return lst.Count;
				}
				return 0;
			}
		}

		public Listener this[int idx] => lst[idx];

		public bool Add(Listener l)
		{
			if (lst == null)
			{
				lst = new List<Listener>();
			}
			if (removed > 0)
			{
				for (int i = 0; i < lst.Count; i++)
				{
					Listener value = lst[i];
					if (value.removed && value.Eq(l))
					{
						value.removed = false;
						lst[i] = value;
						removed--;
						if (removed < 0)
						{
							Game.Log("GameRules.ListenersList.removed count messed up", Game.LogType.Error);
						}
						return false;
					}
				}
			}
			lst.Add(l);
			return true;
		}

		public bool Remove(Listener l)
		{
			if (lst == null)
			{
				return false;
			}
			for (int i = 0; i < lst.Count; i++)
			{
				Listener value = lst[i];
				if (!value.removed && value.Eq(l))
				{
					value.removed = true;
					lst[i] = value;
					removed++;
					return true;
				}
			}
			return false;
		}

		public void CleanUp()
		{
			if (lst == null || removed == 0)
			{
				return;
			}
			for (int num = lst.Count - 1; num >= 0; num--)
			{
				if (lst[num].removed)
				{
					lst.RemoveAt(num);
					removed--;
				}
			}
			if (removed != 0)
			{
				Game.Log("GameRules.ListenersList removed count messed up", Game.LogType.Error);
				removed = 0;
			}
		}

		public override string ToString()
		{
			if (removed != 0)
			{
				return $"[x{calls}][{Count} - {removed}]";
			}
			return $"[x{calls}][{Count}]";
		}
	}

	public Game game;

	public int last_trigger_id;

	private Dictionary<string, MessageListeners> message_listeners = new Dictionary<string, MessageListeners>();

	private int in_on_notification;

	private int removed;

	private List<Object> tmp_obj_list = new List<Object>();

	public GameRules(Game game)
	{
		this.game = game;
		AnalyzeTriggers();
	}

	public void OnNotification(Object obj, string message, object param)
	{
		if (!message_listeners.TryGetValue(message, out var value))
		{
			return;
		}
		in_on_notification++;
		value.calls++;
		int id = ++last_trigger_id;
		ListenersList value2;
		for (Reflection.TypeInfo base_rtti = obj.rtti; base_rtti != null; base_rtti = base_rtti.base_rtti)
		{
			if (value.per_obj_type.TryGetValue(base_rtti, out value2))
			{
				value2.calls++;
				for (int i = 0; i < value2.Count; i++)
				{
					Listener value3 = value2[i];
					if (value3.tdef?.sender_type == null || value3.tdef.sender_type.Resolve(obj) != null)
					{
						value3.calls++;
						value2.lst[i] = value3;
						value3.Call(obj, message, param, id);
					}
				}
			}
			if (base_rtti.type == typeof(Object))
			{
				break;
			}
		}
		if (value.per_obj.TryGetValue(obj, out value2))
		{
			value2.calls++;
			for (int j = 0; j < value2.Count; j++)
			{
				Listener value4 = value2[j];
				if (value4.tdef?.sender_type == null || value4.tdef.sender_type.Resolve(obj) != null)
				{
					value4.calls++;
					value2.lst[j] = value4;
					value4.Call(obj, message, param, id);
				}
			}
		}
		in_on_notification--;
		if (in_on_notification <= 0)
		{
			if (in_on_notification < 0)
			{
				Game.Log("GameRules.OnNotification stack messed up", Game.LogType.Error);
				in_on_notification = 0;
			}
			CleanUp();
		}
	}

	private void AnalyzeTriggers()
	{
		List<GameRule.Def> defs = game.defs.GetDefs<GameRule.Def>();
		if (defs != null)
		{
			for (int i = 0; i < defs.Count; i++)
			{
				GameRule.Def def = defs[i];
				AddListeners(def);
			}
		}
	}

	public void AddListener(Reflection.TypeInfo obj_type, string message, IListener listener)
	{
		Listener l = new Listener(listener);
		AddListener(obj_type, message, l);
	}

	public void AddListener(Object obj, string message, IListener listener)
	{
		Listener l = new Listener(listener);
		AddListener(obj, message, l);
	}

	public void DelListener(Reflection.TypeInfo obj_type, string message, IListener listener)
	{
		Listener l = new Listener(listener);
		DelListener(obj_type, message, l);
	}

	public void DelListener(Object obj, string message, IListener listener)
	{
		Listener l = new Listener(listener);
		DelListener(obj, message, l);
	}

	public void AddListeners(Reflection.TypeInfo obj_type, List<string> messages, IListener listener)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				AddListener(obj_type, message, listener);
			}
		}
	}

	public void AddListeners(Object obj, List<string> messages, IListener listener)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				AddListener(obj, message, listener);
			}
		}
	}

	public void DelListeners(Reflection.TypeInfo obj_type, List<string> messages, IListener listener)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				DelListener(obj_type, message, listener);
			}
		}
	}

	public void DelListeners(Object obj, List<string> messages, IListener listener)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				DelListener(obj, message, listener);
			}
		}
	}

	private void AddListener(Reflection.TypeInfo obj_type, string message, Listener l)
	{
		if (obj_type == null)
		{
			return;
		}
		if (!message_listeners.TryGetValue(message, out var value))
		{
			value = new MessageListeners();
			message_listeners.Add(message, value);
		}
		if (!value.per_obj_type.TryGetValue(obj_type, out var value2))
		{
			value2 = new ListenersList();
			value.per_obj_type.Add(obj_type, value2);
		}
		if (!value2.Add(l))
		{
			removed--;
			if (removed < 0)
			{
				Game.Log("GameRules.removed count messed up", Game.LogType.Error);
			}
		}
	}

	private void AddListener(Object obj, string message, Listener l)
	{
		if (obj == null)
		{
			return;
		}
		if (!obj.IsValid())
		{
			obj.Error($"Attempting to add listener '{l}' to {obj.obj_state} object");
			return;
		}
		if (!message_listeners.TryGetValue(message, out var value))
		{
			value = new MessageListeners();
			message_listeners.Add(message, value);
		}
		if (!value.per_obj.TryGetValue(obj, out var value2))
		{
			value2 = new ListenersList();
			value.per_obj.Add(obj, value2);
		}
		if (!value2.Add(l))
		{
			removed--;
			if (removed < 0)
			{
				Game.Log("GameRules.removed count messed up", Game.LogType.Error);
			}
		}
	}

	private void DelListener(Reflection.TypeInfo obj_type, string message, Listener l)
	{
		if (obj_type != null && message_listeners.TryGetValue(message, out var value) && value.per_obj_type.TryGetValue(obj_type, out var value2) && value2.Remove(l))
		{
			removed++;
		}
	}

	private void DelListener(Object obj, string message, Listener l)
	{
		if (obj != null && message_listeners.TryGetValue(message, out var value) && value.per_obj.TryGetValue(obj, out var value2) && value2.Remove(l))
		{
			removed++;
		}
	}

	private void AddListeners(Reflection.TypeInfo obj_type, List<string> messages, Listener l)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				AddListener(obj_type, message, l);
			}
		}
	}

	private void AddListeners(Object obj, List<string> messages, Listener l)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				AddListener(obj, message, l);
			}
		}
	}

	private void DelListeners(Reflection.TypeInfo obj_type, List<string> messages, Listener l)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				DelListener(obj_type, message, l);
			}
		}
	}

	private void DelListeners(Object obj, List<string> messages, Listener l)
	{
		if (messages != null)
		{
			for (int i = 0; i < messages.Count; i++)
			{
				string message = messages[i];
				DelListener(obj, message, l);
			}
		}
	}

	public void AddListeners(GameRule.Def def, GameRule parent_rule = null, RuleTimer parent_timer = null)
	{
		if (def.start_triggers == null)
		{
			return;
		}
		for (int i = 0; i < def.start_triggers.Count; i++)
		{
			Trigger.Def def2 = def.start_triggers[i];
			Listener l = new Listener(def, def2, parent_rule, parent_timer);
			if (def2.type == "target")
			{
				if (parent_rule != null)
				{
					AddListeners(parent_rule.target_obj, l.tdef.messages, l);
				}
				else
				{
					AddListeners(def.targets, l);
				}
				continue;
			}
			if (parent_rule != null)
			{
				Object senderObj = def2.GetSenderObj(parent_rule);
				if (senderObj != null)
				{
					AddListeners(senderObj, l.tdef.messages, l);
					continue;
				}
			}
			if (def2.sender_type != null)
			{
				AddListeners(def2.sender_type.obj_type, l.tdef.messages, l);
			}
		}
	}

	private void AddListeners(List<GameRule.Def.Target> targets, Listener l)
	{
		if (targets != null)
		{
			for (int i = 0; i < targets.Count; i++)
			{
				GameRule.Def.Target target = targets[i];
				AddListeners(target.type.obj_type, l.tdef.messages, l);
			}
		}
	}

	public void AddListeners(GameRule rule)
	{
		AddTargetTypeListeners(rule);
		AddStopTriggers(rule);
		AddChildRulesListeners(rule);
	}

	private void AddStopTriggers(GameRule rule)
	{
		if (rule.def.stop_triggers == null)
		{
			return;
		}
		for (int i = 0; i < rule.def.stop_triggers.Count; i++)
		{
			Trigger.Def def = rule.def.stop_triggers[i];
			Listener l = new Listener(rule, def);
			Object senderObj = def.GetSenderObj(rule);
			if (senderObj != null)
			{
				AddListeners(senderObj, l.tdef.messages, l);
			}
			else if (def.type == "target")
			{
				AddListeners(rule.target_obj, l.tdef.messages, l);
			}
			else if (def.sender_type != null)
			{
				AddListeners(def.sender_type.obj_type, l.tdef.messages, l);
			}
		}
	}

	private void AddTargetTypeListeners(GameRule rule)
	{
		if (rule?.target_def?.type != null)
		{
			Listener l = new Listener(rule.target_def.type);
			l.parent_rule = rule;
			AddListeners(rule.target_obj, rule.target_def.type.trigger_messages, l);
		}
	}

	private void AddChildRulesListeners(GameRule rule)
	{
		if (rule.def.child_rules != null)
		{
			for (int i = 0; i < rule.def.child_rules.Count; i++)
			{
				GameRule.Def def = rule.def.child_rules[i];
				AddListeners(def, rule);
			}
		}
	}

	public void AddTimerChildRulesListeners(GameRule rule, RuleTimer timer)
	{
		if (timer.def.child_rules != null)
		{
			for (int i = 0; i < timer.def.child_rules.Count; i++)
			{
				GameRule.Def def = timer.def.child_rules[i];
				AddListeners(def, rule, timer);
			}
		}
	}

	public void DelListeners(GameRule rule)
	{
		DelTargetTypeListeners(rule);
		DelStopTriggers(rule);
		DelChildRulesListeners(rule);
	}

	public void DelListeners(GameRule.Def def, GameRule parent_rule = null, RuleTimer parent_timer = null)
	{
		if (def.start_triggers == null)
		{
			return;
		}
		for (int i = 0; i < def.start_triggers.Count; i++)
		{
			Trigger.Def def2 = def.start_triggers[i];
			Listener l = new Listener(def, def2, parent_rule, parent_timer);
			if (def2.type == "target")
			{
				if (parent_rule != null)
				{
					DelListeners(parent_rule.target_obj, l.tdef.messages, l);
				}
				else
				{
					DelListeners(def.targets, l, parent_rule?.target_obj);
				}
			}
			else if (def2.sender_type != null)
			{
				DelListeners(def2.sender_type.obj_type, l.tdef.messages, l);
			}
		}
	}

	private void DelListeners(List<GameRule.Def.Target> targets, Listener l, Object target_obj = null)
	{
		if (targets == null)
		{
			return;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			GameRule.Def.Target target = targets[i];
			if (target_obj != null)
			{
				DelListeners(target_obj, l.tdef.messages, l);
			}
			else
			{
				DelListeners(target.type.obj_type, l.tdef.messages, l);
			}
		}
	}

	private void DelStopTriggers(GameRule rule)
	{
		if (rule.def.stop_triggers == null)
		{
			return;
		}
		for (int i = 0; i < rule.def.stop_triggers.Count; i++)
		{
			Trigger.Def def = rule.def.stop_triggers[i];
			Listener l = new Listener(rule, def);
			if (def.type == "target")
			{
				DelListeners(rule.target_obj, l.tdef.messages, l);
			}
			else if (def.sender_type != null)
			{
				DelListeners(def.sender_type.obj_type, l.tdef.messages, l);
			}
		}
	}

	private void DelTargetTypeListeners(GameRule rule)
	{
		Listener l = new Listener(rule.target_def.type);
		l.parent_rule = rule;
		DelListeners(rule.target_obj, rule.target_def.type.trigger_messages, l);
	}

	private void DelChildRulesListeners(GameRule rule)
	{
		if (rule.def.child_rules == null)
		{
			return;
		}
		for (int i = 0; i < rule.def.child_rules.Count; i++)
		{
			GameRule.Def def = rule.def.child_rules[i];
			if (GameRule.FindRule(rule.child_rules, def) == null)
			{
				DelListeners(def, rule);
			}
		}
	}

	public void DelTimerChildRulesListeners(GameRule rule, RuleTimer timer)
	{
		if (timer.def.child_rules != null)
		{
			for (int i = 0; i < timer.def.child_rules.Count; i++)
			{
				GameRule.Def def = timer.def.child_rules[i];
				DelListeners(def, rule, timer);
			}
		}
	}

	public void CleanUp()
	{
		if (removed == 0)
		{
			return;
		}
		if (removed < 0)
		{
			Game.Log("GameRules.OnNotification removed count messed up", Game.LogType.Error);
		}
		removed = 0;
		foreach (KeyValuePair<string, MessageListeners> message_listener in message_listeners)
		{
			_ = message_listener.Key;
			MessageListeners value = message_listener.Value;
			CleanUp(value);
		}
	}

	private void CleanUp(MessageListeners ml)
	{
		foreach (KeyValuePair<Reflection.TypeInfo, ListenersList> item in ml.per_obj_type)
		{
			item.Value.CleanUp();
		}
		foreach (KeyValuePair<Object, ListenersList> item2 in ml.per_obj)
		{
			Object key = item2.Key;
			ListenersList value = item2.Value;
			value.CleanUp();
			if (value.Empty)
			{
				tmp_obj_list.Add(key);
			}
		}
		if (tmp_obj_list.Count != 0)
		{
			for (int i = 0; i < tmp_obj_list.Count; i++)
			{
				Object key2 = tmp_obj_list[i];
				ml.per_obj.Remove(key2);
			}
			tmp_obj_list.Clear();
		}
	}

	public string DumpRules(string filter = null)
	{
		List<GameRule.Def> defs = game.defs.GetDefs<GameRule.Def>();
		if (defs == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < defs.Count; i++)
		{
			GameRule.Def def = defs[i];
			if (filter == null || def.id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string value = def.Dump();
				stringBuilder.Append(value);
				stringBuilder.AppendLine("\n----------------------------------------");
			}
		}
		return stringBuilder.ToString();
	}

	public string Dump()
	{
		return Dump(null);
	}

	public string Dump(string filter)
	{
		string txt = "\n";
		foreach (KeyValuePair<string, MessageListeners> message_listener in message_listeners)
		{
			string key = message_listener.Key;
			MessageListeners value = message_listener.Value;
			string line = $"[x{value.calls}] {key}:";
			string filter2 = (Filter(line, filter) ? null : filter);
			string children = Dump(value, filter2);
			AddLines(ref txt, line, children);
		}
		return txt;
	}

	private string Dump(MessageListeners ml, string filter)
	{
		string txt = "";
		foreach (KeyValuePair<Reflection.TypeInfo, ListenersList> item in ml.per_obj_type)
		{
			Reflection.TypeInfo key = item.Key;
			ListenersList value = item.Value;
			string line = $"  [x{value.calls}] {key.name}: {value.Count}";
			string filter2 = (Filter(line, filter) ? null : filter);
			string children = Dump(value, filter2);
			AddLines(ref txt, line, children);
		}
		foreach (KeyValuePair<Object, ListenersList> item2 in ml.per_obj)
		{
			Object key2 = item2.Key;
			ListenersList value2 = item2.Value;
			string line2 = $"  [x{value2.calls}] {key2}: {value2.Count}";
			string filter3 = (Filter(line2, filter) ? null : filter);
			string children2 = Dump(value2, filter3);
			AddLines(ref txt, line2, children2);
		}
		return txt;
	}

	private string Dump(ListenersList ll, string filter)
	{
		string txt = "";
		for (int i = 0; i < ll.Count; i++)
		{
			Listener listener = ll[i];
			AddLine(ref txt, $"    {listener}", filter);
		}
		return txt;
	}

	private bool Filter(string line, string filter)
	{
		if (string.IsNullOrEmpty(line))
		{
			return false;
		}
		if (string.IsNullOrEmpty(filter))
		{
			return true;
		}
		if (line.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}
		return true;
	}

	private void AddLine(ref string txt, string line, string filter)
	{
		if (Filter(line, filter))
		{
			txt = txt + line + "\n";
		}
	}

	private void AddLines(ref string txt, string line, string children)
	{
		if (!string.IsNullOrEmpty(children))
		{
			txt = txt + line + "\n" + children;
		}
	}

	public static void RegisterTargetTypes()
	{
		if (TargetType.all.Count != 0)
		{
			return;
		}
		TargetType.Add("object", typeof(Object), null);
		TargetType.Add("any_character", typeof(Character), null);
		TargetType.Add("character", typeof(Character), delegate(object obj)
		{
			if (!(obj is Character character))
			{
				return (object)null;
			}
			return (character.IsDead() && !Game.isLoadingSaveGame && character.IsAuthority()) ? null : character;
		}, "died");
		TargetType.Add("dead_character", typeof(Character), delegate(object obj)
		{
			if (!(obj is Character character))
			{
				return (object)null;
			}
			return (!character.IsDead()) ? null : character;
		}, "status_changed");
		TargetType.Add("any_kingdom", typeof(Kingdom), (object obj) => (obj as Object)?.GetKingdom());
		TargetType.Add("kingdom", typeof(Kingdom), delegate(object obj)
		{
			Kingdom kingdom = (obj as Object)?.GetKingdom();
			if (kingdom == null)
			{
				return (object)null;
			}
			return (kingdom.IsDefeated() && !Game.isLoadingSaveGame && kingdom.IsAuthority()) ? null : kingdom;
		}, "realm_added", "realm_deleted");
		TargetType.Add("any_player_kingdom", typeof(Kingdom), delegate(object obj)
		{
			Kingdom kingdom = (obj as Object)?.GetKingdom();
			if (kingdom == null)
			{
				return (object)null;
			}
			return (!kingdom.is_player && Game.isLoadingSaveGame && kingdom.IsAuthority()) ? null : kingdom;
		}, "players_changed");
		TargetType.Add("player_kingdom", typeof(Kingdom), delegate(object obj)
		{
			Kingdom kingdom = (obj as Object)?.GetKingdom();
			if (kingdom == null)
			{
				return (object)null;
			}
			return (!kingdom.is_local_player && Game.isLoadingSaveGame && kingdom.IsAuthority()) ? null : kingdom;
		}, "local_player_changed");
		TargetType.Add("mission_kingdom", typeof(Kingdom), delegate(object obj)
		{
			Kingdom kingdom = (obj as Character)?.mission_kingdom;
			if (kingdom == null)
			{
				kingdom = (obj as Object)?.GetKingdom();
			}
			if (kingdom == null)
			{
				return (object)null;
			}
			return kingdom.IsDefeated() ? null : kingdom;
		});
		TargetType.Add("defeated_kingdom", typeof(Kingdom), delegate(object obj)
		{
			Kingdom kingdom = (obj as Object)?.GetKingdom();
			if (kingdom == null)
			{
				return (object)null;
			}
			return (!kingdom.IsDefeated()) ? null : kingdom;
		}, "realm_added", "realm_deleted");
		TargetType.Add("realm", typeof(Realm), delegate(object obj)
		{
			if (obj is Realm result)
			{
				return result;
			}
			return (obj is Settlement settlement) ? settlement.GetRealm() : null;
		});
		TargetType.Add("castle", typeof(Castle), delegate(object obj)
		{
			if (obj is Castle result)
			{
				return result;
			}
			if (obj is Realm realm)
			{
				return realm.castle;
			}
			return (obj is Settlement settlement) ? settlement.GetRealm()?.castle : null;
		});
		TargetType.Add("village", typeof(Village), null);
		TargetType.Add("settlement", typeof(Settlement), null);
		TargetType.Add("army", typeof(Army), null);
		TargetType.Add("battle", typeof(Battle), null);
		TargetType.Add("player_battle", typeof(Battle), delegate(object obj)
		{
			if (!(obj is Battle battle))
			{
				return (object)null;
			}
			if (battle.attacker_kingdom != null && battle.attacker_kingdom.is_local_player)
			{
				return battle;
			}
			if (battle.defender_kingdom != null && battle.defender_kingdom.is_local_player)
			{
				return battle;
			}
			Kingdom kingdom = battle.attacker_support?.GetKingdom();
			if (kingdom != null && kingdom.is_local_player)
			{
				return battle;
			}
			Kingdom kingdom2 = battle.defender_support?.GetKingdom();
			return (kingdom2 != null && kingdom2.is_local_player) ? battle : null;
		});
		TargetType.Add("war", typeof(War), null);
		TargetType.Add("pact", typeof(Pact), null);
		TargetType.Add("defensive_pact", typeof(Pact), delegate(object obj)
		{
			if (!(obj is Pact pact))
			{
				return (object)null;
			}
			return (pact.type != Pact.Type.Defensive) ? null : pact;
		});
		TargetType.Add("offensive_pact", typeof(Pact), delegate(object obj)
		{
			if (!(obj is Pact pact))
			{
				return (object)null;
			}
			return (pact.type != Pact.Type.Offensive) ? null : pact;
		});
		TargetType.Add("crusader", typeof(Character), delegate(object obj)
		{
			if (!(obj is Character character))
			{
				return (object)null;
			}
			return (!character.IsCrusader()) ? null : character;
		}, "crusade_ended", "crusade_started");
		TargetType.Add("game", typeof(Game), (object obj) => (!(obj is Game game)) ? null : game);
	}
}
