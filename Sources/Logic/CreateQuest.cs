using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public class Quest : BaseObject, IVars, IListener
{
	public enum State
	{
		Disabled,
		InProgress,
		Complete
	}

	public class Def : Logic.Def
	{
		public Type owner_type;

		public bool game_unique;

		public bool auto_complete;

		public QuestConditionDef activate_conditions;

		public QuestConditionDef complete_conditions;

		public OutcomeDef outcomes;

		public float ai_chance;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			activate_conditions = LoadConditions(field.FindChild("activate_conditions"));
			complete_conditions = LoadConditions(field.FindChild("complete_conditions"));
			string text = field.GetString("owner_type");
			if (!string.IsNullOrEmpty(text))
			{
				owner_type = Type.GetType("Logic." + text);
				if (owner_type == null)
				{
					Game.Log(field.Path(include_file: true) + ".owner_type: unknown type '" + text + "'", Game.LogType.Error);
				}
			}
			ai_chance = field.GetFloat("ai_chance", null, ai_chance);
			outcomes = null;
			LoadOutcomes(game);
			return true;
		}

		private void LoadOutcomes(Game game)
		{
			DT.Field field = base.field.FindChild("outcomes");
			if (field != null)
			{
				outcomes = new OutcomeDef(game, field);
			}
		}

		private QuestConditionDef LoadConditions(DT.Field field)
		{
			if (field == null)
			{
				return null;
			}
			QuestConditionDef questConditionDef = new QuestConditionDef();
			questConditionDef.Load(field);
			return questConditionDef;
		}

		public override bool Validate(Game game)
		{
			return base.Validate(game);
		}

		public bool CheckActivateConditions(Def def, Object obj)
		{
			MethodInfo method = def.obj_type.type.GetMethod("CheckActivateConditions");
			if (method != null)
			{
				return Convert.ToBoolean(method.Invoke(null, new object[2] { def, obj }));
			}
			return false;
		}
	}

	public class RefData : Data
	{
		public NID owner_nid = NID.Null;

		public NID source_nid = NID.Null;

		public int uqid;

		public string def_id;

		public int state;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Quest " + uqid + " to " + owner_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Quest quest))
			{
				return false;
			}
			owner_nid = quest.owner;
			source_nid = quest.source;
			uqid = quest.uqid;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(owner_nid, "owner_nid");
			ser.WriteNID(owner_nid, "source_nid");
			ser.Write7BitUInt(uqid, "uqid");
		}

		public override void Load(Serialization.IReader ser)
		{
			owner_nid = ser.ReadNID("owner_nid");
			source_nid = ser.ReadNID("source_nid");
			uqid = ser.Read7BitUInt("uqid");
		}

		public override object GetObject(Game game)
		{
			Object obj = owner_nid.GetObj(game);
			Object obj2 = source_nid.GetObj(game);
			if (obj == null)
			{
				Game.Log($"Error creating object from {this}. {owner_nid} is invalid ", Game.LogType.Error);
				return null;
			}
			Quests quests = obj.quests;
			Quest quest = null;
			if (quests != null)
			{
				quest = quests.Find(uqid);
			}
			if (quest == null)
			{
				quest = Quest.Create(game.defs.Find<Def>(def_id), obj, obj2);
				quest.uqid = uqid;
			}
			return quest;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Quest quest))
			{
				return false;
			}
			if (quest.uqid != uqid)
			{
				return false;
			}
			quest.owner = owner_nid.GetObj(game);
			quest.source = source_nid.GetObj(game);
			return true;
		}
	}

	public class FullData : RefData
	{
		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Quest quest))
			{
				return false;
			}
			base.InitFrom(obj);
			def_id = quest.def.id;
			state = (int)quest.state;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(def_id, "def");
			ser.Write7BitUInt(state, "state");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			def_id = ser.ReadStr("def");
			state = ser.Read7BitUInt("state");
		}

		public override object GetObject(Game game)
		{
			return base.GetObject(game);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Quest))
			{
				return false;
			}
			if (!(obj is Quest quest))
			{
				return false;
			}
			if (quest.uqid != uqid)
			{
				return false;
			}
			quest.owner = owner_nid.GetObj(game);
			quest.source = source_nid.GetObj(game);
			quest.def = game.defs.Get<Def>(def_id);
			quest.state = (State)state;
			return true;
		}
	}

	public delegate Quest CreateQuest(Type type, Def def, Object o, Object s);

	public delegate Quest CreateQuestByTypeInfo(Reflection.TypeInfo rtti, Def def, Object o, Object s);

	public delegate Quest CreateQuestByDef(Def def, Object o, Object s);

	public delegate Quest CreateQuestByType(Game game, Type type, Object o, Object s);

	public Def def;

	public Object owner;

	public Object source;

	public State state;

	public int uqid;

	public IListener visuals;

	public object message_icon;

	private List<IListener> listeners;

	private float progress;

	public Game game => owner.game;

	public static Quest Create(Reflection.TypeInfo rtti, Def def, Object owner, Object source)
	{
		if (rtti.type == typeof(Quest))
		{
			return new Quest(def, owner, source);
		}
		CreateQuestByDef createQuestByDef = rtti.FindCreateMethod(typeof(Quest), typeof(Def), typeof(Object), typeof(Object))?.func as CreateQuestByDef;
		try
		{
			if (createQuestByDef != null)
			{
				return createQuestByDef(def, owner, source);
			}
			return Reflection.CreateObjectViaReflection<Quest>(rtti.type, new object[3] { def, owner, source });
		}
		catch (Exception ex)
		{
			Game.Log("Error creating " + rtti.name + ": " + ex, Game.LogType.Error);
			return null;
		}
	}

	public static Quest Create(Type type, Def def, Object owner, Object source)
	{
		return Create(Reflection.GetTypeInfo(type), def, owner, source);
	}

	public static Quest Create(Def def, Object owner, Object source)
	{
		if (def == null)
		{
			return null;
		}
		return Create(def.obj_type, def, owner, source);
	}

	public static Quest Create(Game game, Type type, Object owner, Object source)
	{
		Def def = game.defs.Get<Def>(type.Name);
		if (def == null)
		{
			game.Error("Quest def not found: " + type.Name);
			return null;
		}
		return Create(type, def, owner, source);
	}

	public override bool IsRefSerializable()
	{
		return false;
	}

	public Quest(Def def, Object owner, Object source)
	{
		this.def = def;
		this.owner = owner;
		this.source = source;
		state = State.InProgress;
	}

	public void AddListener(IListener listener)
	{
		if (listeners == null)
		{
			listeners = new List<IListener>();
		}
		listeners.Add(listener);
	}

	public void DelListener(IListener listener)
	{
		if (listeners != null)
		{
			listeners.Remove(listener);
		}
	}

	public void NotifyListeners(string message, object param = null, bool profile = true)
	{
		if (visuals == null && listeners == null)
		{
			return;
		}
		string text = null;
		if (profile && visuals != null)
		{
			text = visuals.GetType().ToString() + ".on " + message;
			Game.BeginProfileSection(text);
		}
		if (visuals != null)
		{
			try
			{
				visuals.OnMessage(this, message, param);
			}
			catch (Exception ex)
			{
				Game.Log("Error in NotifyListeners('" + message + "'): " + ex.ToString(), Game.LogType.Error);
			}
		}
		if (listeners != null)
		{
			for (int i = 0; i < listeners.Count; i++)
			{
				IListener listener = listeners[i];
				try
				{
					listener.OnMessage(this, message, param);
				}
				catch (Exception ex2)
				{
					Game.Log("Error in NotifyListeners('" + message + "'): " + ex2.ToString(), Game.LogType.Error);
				}
			}
		}
		if (profile && text != null)
		{
			Game.EndProfileSection(text);
		}
	}

	public virtual Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"source" => owner, 
			"target" => source, 
			"activate_conditions" => new Value(def.activate_conditions), 
			"complete_conditions" => new Value(def.complete_conditions), 
			"outcome" => new Value(def.outcomes), 
			_ => Value.Unknown, 
		};
	}

	public float GetProgress()
	{
		CheckProgress();
		return progress;
	}

	protected virtual void CheckProgress()
	{
		progress = def.complete_conditions.GetProgress(owner, source, null);
	}

	public virtual bool Validate()
	{
		return true;
	}

	public virtual bool CheckConditions()
	{
		return def.complete_conditions.Validate(owner, source, null);
	}

	public void Complete()
	{
		if (owner != null && state == State.InProgress)
		{
			if (!owner.IsAuthority())
			{
				owner.SendEvent(new Object.CompleteQuestEvent(this));
				Game.Log("Quest Complete Request Event Not Implemented", Game.LogType.Warning);
				return;
			}
			state = State.Complete;
			owner.NotifyListeners("quest_complete");
			OnComplete();
			owner.GetComponent<Quests>().RemoveAllQuests();
			owner.FireEvent("quest_changed", this);
		}
	}

	private void ApplyOutcomes(OutcomeDef outcomes_def)
	{
		if (outcomes_def == null)
		{
			return;
		}
		List<OutcomeDef> list = outcomes_def.DecideOutcomes(game, this);
		if (list.Count == 0)
		{
			return;
		}
		List<OutcomeDef> list2 = OutcomeDef.UniqueOutcomes(list);
		Event obj = new Event(owner, "rule_outcomes", this);
		obj.outcomes = list;
		obj.vars = new Vars(this);
		OutcomeDef.PrecalculateValues(list2, game, obj.vars, obj.vars);
		owner.FireEvent(obj);
		for (int i = 0; i < list2.Count; i++)
		{
			OutcomeDef outcomeDef = list2[i];
			if (!outcomeDef.Apply(game, this) && outcomeDef.key != "success")
			{
				Game.Log($"{this}: unhandled outcome: {outcomeDef.id}", Game.LogType.Warning);
			}
		}
	}

	protected virtual void OnComplete()
	{
		ApplyOutcomes(def.outcomes);
	}

	public static bool CheckActivateConditions(Def def, Object obj)
	{
		if (obj == null)
		{
			return false;
		}
		Quests component = obj.GetComponent<Quests>();
		if (component == null)
		{
			return false;
		}
		if (component.Find(def) != null)
		{
			return false;
		}
		return def.activate_conditions.Validate(obj, null, null);
	}

	public override string ToString()
	{
		return Object.ToString(owner) + "." + base.ToString();
	}

	public bool IsValid()
	{
		return true;
	}

	public virtual void OnMessage(object obj, string message, object param)
	{
	}
}
