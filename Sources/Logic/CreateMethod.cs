using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public static class Serialization
{
	public abstract class IReader
	{
		public struct Section : IDisposable
		{
			private IReader reader;

			private string key;

			private int key_idx;

			public Section(IReader reader, string key, int key_idx = int.MaxValue)
			{
				this.reader = reader;
				this.key = key;
				this.key_idx = key_idx;
			}

			public void Dispose()
			{
				reader.CloseSection(key, key_idx);
			}
		}

		public UniqueStrings unique_strings;

		public Defs defs { get; private set; }

		public IReader(Defs defs)
		{
			this.defs = defs;
		}

		public abstract int Position();

		public abstract int Length();

		public abstract void ReadMessageHeader(out byte id, out ObjectType tid, out ObjectTypeInfo ti, out int nid);

		public abstract void ReadMessageHeader(out byte id, out ObjectType tid, out ObjectTypeInfo ti, out int nid, out byte substate_id, out int substate_index);

		public abstract Section OpenSection(string key, int key_idx = int.MaxValue);

		public abstract void CloseSection(string key, int key_idx = int.MaxValue);

		public abstract bool ReadBool(string key, int key_idx = int.MaxValue);

		public abstract byte ReadByte(string key, int key_idx = int.MaxValue);

		public abstract int Read7BitUInt(string key, int key_idx = int.MaxValue);

		public abstract int Read7BitSigned(string key, int key_idx = int.MaxValue);

		public abstract string ReadStr(string key, int key_idx = int.MaxValue);

		public abstract string ReadRawStr(string key, int key_idx = int.MaxValue);

		public abstract float ReadFloat(string key, int key_idx = int.MaxValue);

		public abstract Point ReadPoint(string key, int key_idx = int.MaxValue);

		public abstract PPos ReadPPos(string key, int key_idx = int.MaxValue);

		public abstract byte[] ReadBytes(string key, int key_idx = int.MaxValue);

		public Data ReadData(string key, int key_idx = int.MaxValue)
		{
			using (OpenSection(key, key_idx))
			{
				string text = ReadStr("data_type");
				if (string.IsNullOrEmpty(text))
				{
					return null;
				}
				Data data = Data.CreateByTypename(text);
				if (data == null)
				{
					Game.Log("Could not create Data of type '" + text + "'", Game.LogType.Warning);
					return null;
				}
				data.Load(this);
				return data;
			}
		}

		public abstract NID ReadNID(ObjectTypeInfo ti, int pid, string type, string key, int key_idx = int.MaxValue);

		public NID ReadNID(string key, int key_idx = int.MaxValue)
		{
			return ReadNID(null, -1, "Object", key, key_idx);
		}

		public NID ReadNID<T>(string key, int key_idx = int.MaxValue) where T : Logic.Object
		{
			ObjectTypeInfo objectTypeInfo = ObjectTypeInfo.Get(typeof(T));
			if (objectTypeInfo == null)
			{
				Game.Log("Trying to read object of unknown type " + typeof(T).Name, Game.LogType.Error);
				return NID.Null;
			}
			int pid = (objectTypeInfo.dynamic ? (-1) : 0);
			return ReadNID(objectTypeInfo, pid, objectTypeInfo.name, key, key_idx);
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class Object : Attribute
	{
		public ObjectType tid;

		public bool dynamic = true;

		public Object(ObjectType tid)
		{
			this.tid = tid;
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class State : Attribute
	{
		public delegate bool IsNeededMethod(Logic.Object obj);

		public byte state_id;

		public Type type;

		public Reflection.TypeInfo rtti;

		public string name;

		public IsNeededMethod is_needed_method;

		public Dictionary<byte, Substate> substates_by_id = new Dictionary<byte, Substate>();

		public Dictionary<Type, byte> substate_ids = new Dictionary<Type, byte>();

		public Dictionary<string, byte> str_substate_ids = new Dictionary<string, byte>();

		public State(byte id)
		{
			state_id = id;
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class Event : Attribute
	{
		public byte event_id;

		public Type type;

		public Reflection.TypeInfo rtti;

		public string name;

		public Event(byte id)
		{
			event_id = id;
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class Substate : Attribute
	{
		public byte substate_id;

		public Type type;

		public Reflection.TypeInfo rtti;

		public State state_attr;

		public string name;

		public Substate(byte id)
		{
			substate_id = id;
		}
	}

	public abstract class ObjectMessage : BaseObject
	{
		public delegate ObjectMessage CreateMethod();

		public byte id;

		public ObjectTypeInfo obj_ti;

		public int nid;

		public Multiplayer sender;

		public abstract void WriteBody(IWriter ser);

		public abstract void ReadBody(IReader ser);

		public abstract void ApplyTo(Logic.Object obj);

		public override string ToString()
		{
			return Logic.Object.TypeToStr(GetType()) + "(" + id + "):" + Logic.Object.TypeToStr(obj_ti.type) + "(" + NID.ToString(nid) + ")";
		}

		public static ObjectMessage Read(string msg_type, IReader ser)
		{
			ser.ReadMessageHeader(out var b, out var tid, out var ti, out var num);
			if (num == -1)
			{
				Error("Attempting to read object " + b + " of unknown object type: " + tid.ToString());
				return null;
			}
			return Read(msg_type, ser, b, ti, num, 0);
		}

		public static ObjectMessage Read(string msg_type, IReader ser, byte id, ObjectTypeInfo ti, int nid, byte substate_id = 0, int substate_index = 0)
		{
			ObjectMessage objectMessage = null;
			switch (msg_type)
			{
			case "state":
				objectMessage = CreateState(GetStateAttr(id, ti));
				break;
			case "event":
				objectMessage = CreateEvent(GetEventAttr(id, ti));
				break;
			case "substate":
				objectMessage = CreateSubstate(GetSubstateAttr(id, substate_id, ti));
				break;
			}
			if (objectMessage == null)
			{
				Error("Attempting to read unknown " + ti.type.ToString() + " " + id);
				return null;
			}
			objectMessage.id = id;
			objectMessage.obj_ti = ti;
			objectMessage.nid = nid;
			if (objectMessage is ObjectSubstate objectSubstate)
			{
				objectSubstate.substate_id = substate_id;
				objectSubstate.substate_index = substate_index;
			}
			objectMessage.ReadBody(ser);
			return objectMessage;
		}

		public virtual void Write(IWriter ser, bool includeMessageHeader = true)
		{
			if (includeMessageHeader)
			{
				ser.WriteMessageHeader(id, new NID(obj_ti, nid));
			}
			WriteBody(ser);
		}
	}

	public abstract class ObjectEvent : ObjectMessage
	{
		public Event attr => GetEventAttr(id, obj_ti);

		public string event_name => attr.name;

		public static ObjectEvent Read(IReader ser)
		{
			ser.ReadMessageHeader(out var b, out var _, out var ti, out var num);
			if (num == -1)
			{
				Error("Attempting to read object event of unknown object type");
				return null;
			}
			return ObjectMessage.Read("event", ser, b, ti, num, 0) as ObjectEvent;
		}
	}

	public abstract class ObjectState : ObjectMessage
	{
		public List<ObjectSubstate> substates;

		public State attr => GetStateAttr(id, obj_ti);

		public string state_name => attr.name;

		public abstract bool InitFrom(Logic.Object obj);

		public static ObjectState Read(IReader ser)
		{
			ser.ReadMessageHeader(out var b, out var _, out var ti, out var num);
			if (num == -1)
			{
				Error("Attempting to read object state of unknown object type");
				return null;
			}
			return ObjectMessage.Read("state", ser, b, ti, num, 0) as ObjectState;
		}

		public void AddSubstate(ObjectSubstate substate)
		{
			substate.id = id;
			substate.nid = nid;
			substate.obj_ti = obj_ti;
			if (!obj_ti.states_by_id.TryGetValue(id, out var value))
			{
				Error("Trying to add substate to unknown state: " + ToString());
				return;
			}
			if (!value.substate_ids.TryGetValue(substate.rtti.type, out var value2))
			{
				Error("Trying to add unknown substate: " + substate.ToString());
				return;
			}
			substate.substate_id = value2;
			if (substates == null)
			{
				substates = new List<ObjectSubstate>();
			}
			substates.Add(substate);
		}

		public T GetSubstate<T>(int idx) where T : ObjectSubstate
		{
			if (substates == null)
			{
				return null;
			}
			for (int i = 0; i < substates.Count; i++)
			{
				ObjectSubstate objectSubstate = substates[i];
				if (objectSubstate.substate_index == idx && objectSubstate is T result)
				{
					return result;
				}
			}
			return null;
		}
	}

	public abstract class ObjectSubstate : ObjectMessage
	{
		public byte substate_id;

		public int substate_index;

		public State state_attr => GetStateAttr(id, obj_ti);

		public Substate attr => GetSubstateAttr(id, substate_id, obj_ti);

		public string substate_name => attr.name;

		public override string ToString()
		{
			return GetType().ToString() + "(" + id + "/" + substate_id + ":" + substate_index + "):" + obj_ti.type.ToString() + "(" + NID.ToString(nid) + ")";
		}

		public abstract bool InitFrom(Logic.Object obj);

		public static ObjectSubstate Read(IReader ser)
		{
			ser.ReadMessageHeader(out var b, out var _, out var ti, out var num, out var b2, out var num2);
			if (num == -1)
			{
				Error("Attempting to read object substate of unknown object type");
				return null;
			}
			return ObjectMessage.Read("substate", ser, b, ti, num, b2, num2) as ObjectSubstate;
		}

		public override void Write(IWriter ser, bool includeMessageHeader = true)
		{
			if (includeMessageHeader)
			{
				ser.WriteMessageHeader(id, new NID(obj_ti, nid));
				ser.WriteByte(substate_id, null);
				ser.Write7BitUInt(substate_index, null);
			}
			WriteBody(ser);
		}
	}

	public class ObjectStates
	{
		public NID obj_nid = NID.Null;

		public bool started;

		public string comment;

		public Dictionary<byte, ObjectState> states = new Dictionary<byte, ObjectState>();

		public ObjectStates(Logic.Object obj)
		{
			obj_nid = obj;
			if (obj_nid.ti == null)
			{
				Error("Unknown object type " + obj.ToString());
			}
		}

		public ObjectState Get(byte id)
		{
			if (states.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public T Get<T>() where T : ObjectState
		{
			if (!obj_nid.ti.state_ids.TryGetValue(typeof(T), out var value))
			{
				return null;
			}
			return Get(value) as T;
		}

		public void Set(byte id, ObjectState msg)
		{
			states[id] = msg;
		}

		public void Add(byte id, ObjectState msg)
		{
			if (states.ContainsKey(id))
			{
				Error("Object state " + msg.GetType().ToString() + "(" + id + ") already added");
				states[id] = msg;
			}
			else
			{
				states.Add(id, msg);
			}
		}

		public void Add(ObjectState msg)
		{
			if (!obj_nid.ti.state_ids.TryGetValue(msg.rtti.type, out var value))
			{
				Error("Attempting to add invalid state " + msg.ToString());
			}
			else
			{
				Add(value, msg);
			}
		}

		public void Add(ObjectSubstate msg)
		{
			ObjectState objectState = Get(msg.id);
			if (objectState == null)
			{
				Error("Attempting to add substate to a nonexisting state: " + msg.ToString());
			}
			else
			{
				objectState.AddSubstate(msg);
			}
		}

		public void Del(byte id)
		{
			states.Remove(id);
		}

		public void Del(byte id, byte substate_id, int index)
		{
		}

		public ObjectState Pop(byte id)
		{
			ObjectState objectState = Get(id);
			if (objectState != null)
			{
				Del(id);
			}
			return objectState;
		}

		public T Pop<T>() where T : ObjectState
		{
			T val = Get<T>();
			if (val != null)
			{
				Del(val.id);
			}
			return val;
		}

		public void GenerateStates(Logic.Object obj)
		{
			if (obj == null)
			{
				Error("GenerateStates for null object");
				return;
			}
			if (obj_nid.ti != ObjectTypeInfo.Get(obj) || obj_nid.nid != obj.GetNid(generateNid: false))
			{
				Error("GenerateStates() for wrong object: " + obj.ToString());
				return;
			}
			Game.BeginProfileSection("Generate States");
			started = obj.obj_state == Logic.Object.ObjState.Started || obj.obj_state == Logic.Object.ObjState.Starting;
			try
			{
				comment = obj.ToString();
			}
			catch (Exception ex)
			{
				Game.Log("Error creating comment for object with nid " + obj.GetNid(generateNid: false) + ": " + ex.ToString(), Game.LogType.Error);
				return;
			}
			foreach (KeyValuePair<byte, State> item in obj_nid.ti.states_by_id)
			{
				byte key = item.Key;
				State value = item.Value;
				if (value.is_needed_method != null)
				{
					try
					{
						if (!value.is_needed_method(obj))
						{
							continue;
						}
					}
					catch (Exception ex2)
					{
						Game.Log("Error in " + value.name + ".IsNeeded for " + obj.ToString() + ": " + ex2.ToString(), Game.LogType.Error);
						continue;
					}
				}
				Game.BeginProfileSection("Create state");
				ObjectState objectState = null;
				try
				{
					objectState = CreateState(value);
				}
				catch (Exception ex3)
				{
					Game.Log("Error creating " + value.name + " for " + obj.ToString() + ": " + ex3.ToString(), Game.LogType.Error);
					objectState = null;
				}
				Game.EndProfileSection("Create state");
				if (objectState == null)
				{
					Game.Log("Could not create " + value.name + " for " + obj.ToString(), Game.LogType.Error);
					continue;
				}
				objectState.id = key;
				objectState.obj_ti = obj_nid.ti;
				objectState.nid = obj_nid.nid;
				Game.BeginProfileSection("InitFrom");
				bool flag;
				try
				{
					flag = objectState.InitFrom(obj);
				}
				catch (Exception ex4)
				{
					Game.Log("Error generating " + value.name + " for " + obj.ToString() + ": " + ex4.ToString(), Game.LogType.Error);
					flag = false;
				}
				Game.EndProfileSection("InitFrom");
				if (flag)
				{
					Add(key, objectState);
				}
			}
			Game.EndProfileSection("Generate States");
		}

		public void LoadObject(Logic.Object obj)
		{
			if (obj == null)
			{
				Error("Attempting to load null object");
				return;
			}
			if (obj_nid.ti != ObjectTypeInfo.Get(obj) || obj_nid.nid != obj.GetNid(generateNid: false))
			{
				Error("LoadObject() for wrong object: " + obj.ToString());
				return;
			}
			try
			{
				obj.Load(this);
				foreach (KeyValuePair<byte, ObjectState> state in states)
				{
					_ = state.Key;
					ObjectState value = state.Value;
					value.ApplyTo(obj);
					if (value.substates != null)
					{
						for (int i = 0; i < value.substates.Count; i++)
						{
							value.substates[i].ApplyTo(obj);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Error("Error loading " + obj.ToString() + ": " + ex);
			}
		}
	}

	public class ObjectsRegistry
	{
		public class ObjectTypeRegistry
		{
			public ObjectTypeInfo ti;

			public Dictionary<int, Logic.Object> objects = new Dictionary<int, Logic.Object>();

			public int last_nid;

			public int pid;

			public void SetPID(int pid)
			{
				this.pid = pid;
				last_nid = 0;
			}

			public int NewNID()
			{
				if (pid == 0)
				{
					Game.Log("Generating new nid for " + ti.name, Game.LogType.Warning);
				}
				int num;
				do
				{
					last_nid++;
					if (last_nid >= 4194303)
					{
						last_nid = 1;
					}
					num = NID.Encode(pid, last_nid);
				}
				while (objects.ContainsKey(num));
				return num;
			}
		}

		public Dictionary<ObjectType, ObjectTypeRegistry> registries = new Dictionary<ObjectType, ObjectTypeRegistry>();

		private int pid;

		public void SetPID(int pid)
		{
			this.pid = pid;
			foreach (KeyValuePair<ObjectType, ObjectTypeRegistry> registry in registries)
			{
				ObjectTypeRegistry value = registry.Value;
				if (value.ti.dynamic)
				{
					value.SetPID(pid);
				}
			}
		}

		public ObjectTypeRegistry Registry(ObjectType tid)
		{
			if (!registries.TryGetValue(tid, out var value))
			{
				value = new ObjectTypeRegistry();
				value.ti = ObjectTypeInfo.Get(tid);
				if (value.ti.dynamic)
				{
					value.SetPID(pid);
				}
				registries.Add(tid, value);
			}
			return value;
		}

		private ObjectTypeRegistry Registry(Type type)
		{
			ObjectTypeInfo objectTypeInfo = ObjectTypeInfo.Get(type);
			if (objectTypeInfo == null)
			{
				return null;
			}
			return Registry(objectTypeInfo.tid);
		}

		public Logic.Object Get(ObjectType tid, int nid)
		{
			if (nid == 0)
			{
				return null;
			}
			ObjectTypeRegistry objectTypeRegistry = Registry(tid);
			if (objectTypeRegistry == null)
			{
				return null;
			}
			if (!objectTypeRegistry.objects.TryGetValue(nid, out var value))
			{
				return null;
			}
			return value;
		}

		public T Get<T>(int nid) where T : Logic.Object
		{
			if (nid == 0)
			{
				return null;
			}
			ObjectTypeRegistry objectTypeRegistry = Registry(typeof(T));
			if (objectTypeRegistry == null)
			{
				return null;
			}
			if (!objectTypeRegistry.objects.TryGetValue(nid, out var value))
			{
				return null;
			}
			return value as T;
		}

		public void Add(ObjectType tid, Logic.Object obj)
		{
			ObjectTypeRegistry objectTypeRegistry = Registry(tid);
			if (tid == ObjectType.Kingdom)
			{
				Kingdom kingdom = (Kingdom)obj;
				if (kingdom.id == 0 && kingdom.Name == null)
				{
					return;
				}
			}
			if (objectTypeRegistry == null)
			{
				Error("Attempting to add object of unknown type " + tid.ToString() + ": " + obj.ToString());
				return;
			}
			if (obj.GetNid(generateNid: false) == 0)
			{
				obj.SetNid(objectTypeRegistry.NewNID(), update_registry: false);
			}
			if (objectTypeRegistry.objects.TryGetValue(obj.GetNid(generateNid: false), out var value))
			{
				if (value == obj)
				{
					return;
				}
				Error("Duplicated object id " + obj.GetNid(generateNid: false) + ", original: " + value.ToString() + ", replaced by: " + obj.ToString());
			}
			objectTypeRegistry.objects[obj.GetNid(generateNid: false)] = obj;
		}

		public void Add(Logic.Object obj)
		{
			ObjectTypeRegistry objectTypeRegistry = Registry(obj.rtti.type);
			if (obj is Kingdom && ((Kingdom)obj).id > 430)
			{
				Log("HEX TEMP breakpoint");
			}
			if (objectTypeRegistry == null)
			{
				Error("Attempting to add object of unknown type: " + obj.ToString());
				return;
			}
			obj.GetNid();
			if (objectTypeRegistry.objects.TryGetValue(obj.GetNid(), out var value))
			{
				Error("Duplicated object id " + obj.GetNid() + ", original: " + value.ToString() + ", replaced by: " + obj.ToString());
			}
			objectTypeRegistry.objects[obj.GetNid()] = obj;
		}

		public void DelAndShift(ObjectType tid, Logic.Object obj)
		{
			if (obj.GetNid(generateNid: false) == 0)
			{
				return;
			}
			ObjectTypeRegistry objectTypeRegistry = Registry(tid);
			if (objectTypeRegistry == null)
			{
				Error("Attempting to delete object of unknown type " + tid.ToString() + ": " + obj.ToString());
				obj.SetNid(0, update_registry: false);
				return;
			}
			for (int i = 1; i <= objectTypeRegistry.objects.Keys.Count; i++)
			{
				if (!objectTypeRegistry.objects.ContainsKey(i))
				{
					Error("Attempting to delete and shift object of non consecutive key list: " + obj.ToString() + " (" + tid.ToString() + ")");
					obj.SetNid(0, update_registry: false);
					return;
				}
			}
			objectTypeRegistry.objects.Remove(obj.GetNid());
			for (int j = obj.GetNid() + 1; j <= objectTypeRegistry.objects.Keys.Count + 1; j++)
			{
				objectTypeRegistry.objects.Add(j - 1, objectTypeRegistry.objects[j]);
				objectTypeRegistry.objects[j - 1].SetNid_NoError(j - 1);
				objectTypeRegistry.objects.Remove(j);
			}
			obj.SetNid(0, update_registry: false);
		}

		public void Del(ObjectType tid, Logic.Object obj)
		{
			if (obj.GetNid(generateNid: false) != 0)
			{
				ObjectTypeRegistry objectTypeRegistry = Registry(tid);
				if (objectTypeRegistry == null)
				{
					Error("Attempting to delete object of unknown type " + tid.ToString() + ": " + obj.ToString());
					obj.SetNid(0, update_registry: false);
				}
				else
				{
					objectTypeRegistry.objects.Remove(obj.GetNid());
					obj.SetNid(0, update_registry: false);
				}
			}
		}

		public void Del(Logic.Object obj)
		{
			if (obj.GetNid(generateNid: false) != 0)
			{
				ObjectTypeRegistry objectTypeRegistry = Registry(obj.rtti.type);
				if (objectTypeRegistry == null)
				{
					Error("Attempting to delete object of unknown type: " + obj.ToString());
					obj.SetNid(0, update_registry: false);
				}
				else
				{
					objectTypeRegistry.objects.Remove(obj.GetNid());
					obj.SetNid(0, update_registry: false);
				}
			}
		}
	}

	public class ObjectTypeInfo
	{
		public ObjectType tid;

		public string name;

		public Type type;

		public Reflection.TypeInfo rtti;

		public bool dynamic;

		public Dictionary<byte, State> states_by_id = new Dictionary<byte, State>();

		public Dictionary<Type, byte> state_ids = new Dictionary<Type, byte>();

		public Dictionary<string, byte> str_state_ids = new Dictionary<string, byte>();

		public Dictionary<byte, Event> events_by_id = new Dictionary<byte, Event>();

		public Dictionary<Type, byte> event_ids = new Dictionary<Type, byte>();

		private static Dictionary<ObjectType, ObjectTypeInfo> types_by_id = null;

		private static Dictionary<Type, ObjectTypeInfo> types_by_type = null;

		private static Type[] ctor_params = new Type[1] { typeof(Multiplayer) };

		public override string ToString()
		{
			return name;
		}

		public static ObjectTypeInfo Get(string type)
		{
			ObjectType tID = GetTID(type);
			if (tID == ObjectType.COUNT)
			{
				return null;
			}
			return Get(tID);
		}

		public static ObjectTypeInfo Get(ObjectType tid)
		{
			if (!types_by_id.TryGetValue(tid, out var value))
			{
				return null;
			}
			return value;
		}

		public static ObjectTypeInfo Get(Type type)
		{
			if (!types_by_type.TryGetValue(type, out var value))
			{
				return null;
			}
			return value;
		}

		public static ObjectTypeInfo Get(Logic.Object obj)
		{
			return obj.rtti.ti;
		}

		public Logic.Object CreateObject(Multiplayer multiplayer)
		{
			try
			{
				return Reflection.Create<Logic.Object>(type, multiplayer);
			}
			catch (Exception ex)
			{
				Error("Error creating " + type.ToString() + ": " + ex);
				return null;
			}
		}

		public static void Init()
		{
			if (types_by_id != null)
			{
				return;
			}
			types_by_id = new Dictionary<ObjectType, ObjectTypeInfo>();
			types_by_type = new Dictionary<Type, ObjectTypeInfo>();
			Assembly assembly = typeof(Logic.Object).Assembly;
			Type typeFromHandle = typeof(Data);
			Type typeFromHandle2 = typeof(BaseObject);
			Type[] types = assembly.GetTypes();
			foreach (Type type in types)
			{
				if (typeFromHandle.IsAssignableFrom(type) || typeFromHandle2.IsAssignableFrom(type))
				{
					Reflection.GetTypeInfo(type);
				}
				object[] customAttributes = type.GetCustomAttributes(typeof(Object), inherit: false);
				if (customAttributes.Length != 0)
				{
					if (!typeof(Logic.Object).IsAssignableFrom(type))
					{
						Error("Type " + type.ToString() + " has [Serialization.Object] attribute, but is not derived from Logic.Object");
						continue;
					}
					Object obj = customAttributes[0] as Object;
					RegObjType(obj.tid, type, obj.dynamic);
				}
			}
			for (ObjectType objectType = ObjectType.Game; objectType < ObjectType.COUNT; objectType++)
			{
				if (Get(objectType) == null)
				{
					Error("Object type " + objectType.ToString() + " is not registered");
				}
			}
		}

		private static void RegObjState(ObjectTypeInfo ti, Type type, State state)
		{
			if (!typeof(ObjectState).IsAssignableFrom(type))
			{
				Error("Type " + type.ToString() + " has [Serialization.State] attribute, but is not derived from Serialization.ObjectState");
				return;
			}
			if (ti.states_by_id.TryGetValue(state.state_id, out var value))
			{
				Error("Duplicated object state id " + state.state_id + ": " + value.type.ToString() + " and " + type.ToString());
				return;
			}
			state.type = type;
			state.rtti = Reflection.GetTypeInfo(type);
			state.rtti.state_attr = state;
			state.name = state.rtti.name;
			if (state.rtti.create == null)
			{
				Error(state.name + " has no 'public static " + state.name + " Create()' method");
			}
			state.is_needed_method = Reflection.CreateDelegate(typeof(State.IsNeededMethod), type, "IsNeeded", typeof(bool), typeof(Logic.Object)) as State.IsNeededMethod;
			ti.states_by_id.Add(state.state_id, state);
			ti.state_ids.Add(type, state.state_id);
			ti.str_state_ids.Add(state.name, state.state_id);
			RegSubstates(state);
		}

		private static void RegObjSubstate(State state, Type type, Substate substate)
		{
			if (!typeof(ObjectSubstate).IsAssignableFrom(type))
			{
				Error("Type " + type.ToString() + " has [Serialization.Substate] attribute, but is not derived from Serialization.ObjectSubstate");
				return;
			}
			if (state.substates_by_id.TryGetValue(substate.substate_id, out var value))
			{
				Error("Duplicated object substate id " + substate.substate_id + ": " + value.type.ToString() + " and " + type.ToString());
				return;
			}
			substate.type = type;
			substate.rtti = Reflection.GetTypeInfo(type);
			substate.rtti.substate_attr = substate;
			substate.state_attr = state;
			substate.name = substate.rtti.name;
			if (substate.rtti.create == null)
			{
				Error(substate.name + " has no 'public static " + substate.name + " Create()' method");
			}
			state.substates_by_id.Add(substate.substate_id, substate);
			state.substate_ids.Add(type, substate.substate_id);
			state.str_substate_ids.Add(substate.name, substate.substate_id);
		}

		private static void RegObjEvent(ObjectTypeInfo ti, Type type, Event evt)
		{
			if (!typeof(ObjectEvent).IsAssignableFrom(type))
			{
				Error("Type " + type.ToString() + " has [Serialization.Event] attribute, but is not derived from Serialization.ObjectEvent");
				return;
			}
			if (ti.events_by_id.TryGetValue(evt.event_id, out var value))
			{
				Error("Duplicated object event id " + evt.event_id + ": " + value.type.ToString() + " and " + type.ToString());
				return;
			}
			evt.type = type;
			evt.rtti = Reflection.GetTypeInfo(type);
			evt.rtti.event_attr = evt;
			evt.name = evt.rtti.name;
			if (evt.rtti.create == null)
			{
				Error(evt.name + " has no 'public static " + evt.name + " Create()' method");
			}
			ti.events_by_id.Add(evt.event_id, evt);
			ti.event_ids.Add(type, evt.event_id);
		}

		private static void RegObjMessages(ObjectTypeInfo ti)
		{
			Type baseType = ti.type;
			while (baseType != null)
			{
				Type[] nestedTypes = baseType.GetNestedTypes();
				foreach (Type type in nestedTypes)
				{
					object[] customAttributes = type.GetCustomAttributes(typeof(State), inherit: false);
					if (customAttributes.Length != 0)
					{
						RegObjState(ti, type, customAttributes[0] as State);
					}
					else if (typeof(ObjectState).IsAssignableFrom(type))
					{
						Error("Type " + type.ToString() + " inherits from Serialization.ObjectState, but has no [Serialization.State] attribute");
					}
					object[] customAttributes2 = type.GetCustomAttributes(typeof(Event), inherit: false);
					if (customAttributes2.Length != 0)
					{
						RegObjEvent(ti, type, customAttributes2[0] as Event);
					}
					else if (typeof(ObjectEvent).IsAssignableFrom(type))
					{
						Error("Type " + type.ToString() + " inherits from Serialization.ObjectEvent, but has no [Serialization.Event] attribute");
					}
				}
				baseType = baseType.BaseType;
			}
		}

		private static void RegSubstates(State state)
		{
			Type baseType = state.type;
			while (baseType != null)
			{
				Type[] nestedTypes = baseType.GetNestedTypes();
				foreach (Type type in nestedTypes)
				{
					object[] customAttributes = type.GetCustomAttributes(typeof(Substate), inherit: false);
					if (customAttributes.Length != 0)
					{
						RegObjSubstate(state, type, customAttributes[0] as Substate);
					}
					else if (typeof(ObjectSubstate).IsAssignableFrom(type))
					{
						Error("Type " + type.ToString() + " inherits from Serialization.ObjectSubstate, but has no [Serialization.Substate] attribute");
					}
				}
				baseType = baseType.BaseType;
			}
		}

		private static void RegObjType(ObjectType tid, Type type, bool dynamic)
		{
			if (types_by_id.TryGetValue(tid, out var value))
			{
				Error("Object type " + tid.ToString() + "(" + (int)tid + ") duplicate: " + value.type.ToString() + " and " + type.ToString());
			}
			else if (dynamic && type.GetConstructor(ctor_params) == null)
			{
				Error(type.ToString() + " has no constructor(Multiplayer)");
			}
			else
			{
				ObjectTypeInfo objectTypeInfo = new ObjectTypeInfo();
				objectTypeInfo.tid = tid;
				objectTypeInfo.type = type;
				objectTypeInfo.rtti = Reflection.GetTypeInfo(type);
				objectTypeInfo.rtti.ti = objectTypeInfo;
				objectTypeInfo.name = objectTypeInfo.rtti.name;
				objectTypeInfo.dynamic = dynamic;
				RegObjMessages(objectTypeInfo);
				types_by_id.Add(tid, objectTypeInfo);
				types_by_type.Add(type, objectTypeInfo);
			}
		}
	}

	public abstract class IWriter
	{
		public struct Section : IDisposable
		{
			private IWriter writer;

			private string key;

			private int key_idx;

			public Section(IWriter writer, string key, int key_idx = int.MaxValue)
			{
				this.writer = writer;
				this.key = key;
				this.key_idx = key_idx;
			}

			public void Dispose()
			{
				writer.CloseSection(key, key_idx);
			}
		}

		public UniqueStrings unique_strings;

		public abstract int Position();

		public abstract void Close();

		public abstract void WriteMessageHeader(byte msg_id, NID nid);

		public abstract Section OpenSection(string type, string key, int key_idx = int.MaxValue, bool checkKeys = true);

		public abstract void CloseSection(string key, int key_idx = int.MaxValue);

		public abstract void WriteBool(bool val, string key, int key_idx = int.MaxValue);

		public abstract void WriteByte(byte val, string key, int key_idx = int.MaxValue);

		public abstract void Write7BitUInt(int val, string key, int key_idx = int.MaxValue);

		public abstract void Write7BitSigned(int val, string key, int key_idx = int.MaxValue);

		public abstract void WriteStr(string val, string key, int key_idx = int.MaxValue);

		public abstract void WriteRawStr(string val, string key, int key_idx = int.MaxValue);

		public abstract void WriteFloat(float val, string key, int key_idx = int.MaxValue);

		public abstract void WritePoint(Point val, string key, int key_idx = int.MaxValue);

		public abstract void WritePPos(PPos val, string key, int key_idx = int.MaxValue);

		public abstract void WriteBytes(byte[] bytes, string key, int key_idx = int.MaxValue);

		public void WriteData(Data data, string key, int key_idx = int.MaxValue)
		{
			using (OpenSection("data", key, key_idx))
			{
				if (data == null)
				{
					WriteStr("", "data_type");
					return;
				}
				WriteStr(data.rtti.full_name, "data_type");
				data.Save(this);
			}
		}

		public abstract void WriteNID(int id, ObjectTypeInfo ti, int pid, string type, string key, int key_idx = int.MaxValue);

		public void WriteNID(NID nid, string key, int key_idx = int.MaxValue)
		{
			int pid;
			if (nid.ti != null && nid.ti.dynamic)
			{
				pid = nid.pid;
			}
			else
			{
				if (nid.ti != null && nid.pid != 0)
				{
					Game.Log("Trying to write static " + nid.ti.name + " with pid " + nid.pid + " (must be 0)", Game.LogType.Error);
				}
				pid = -1;
			}
			WriteNID(nid.id, nid.ti, pid, "Object", key, key_idx);
		}

		public void WriteNID<T>(NID nid, string key, int key_idx = int.MaxValue) where T : Logic.Object
		{
			ObjectTypeInfo objectTypeInfo = ObjectTypeInfo.Get(typeof(T));
			if (objectTypeInfo == null)
			{
				Game.Log("Trying to write object of unknown type " + typeof(T).Name, Game.LogType.Error);
				return;
			}
			if (nid.ti == null)
			{
				if (nid.id != 0)
				{
					Game.Log("Trying to write non-null object of null type as " + typeof(T).Name, Game.LogType.Error);
				}
				else
				{
					WriteNID(0, null, -1, objectTypeInfo.tid.ToString(), key, key_idx);
				}
				return;
			}
			if (nid.ti != objectTypeInfo)
			{
				Game.Log("Trying to write " + nid.ti.name + " as " + objectTypeInfo.name, Game.LogType.Error);
				return;
			}
			int pid;
			if (objectTypeInfo.dynamic)
			{
				pid = nid.pid;
			}
			else
			{
				if (nid.pid != 0)
				{
					Game.Log("Trying to write static " + objectTypeInfo.name + " with pid " + nid.pid + " (must be 0)", Game.LogType.Error);
				}
				pid = -1;
			}
			WriteNID(nid.id, null, pid, objectTypeInfo.name, key, key_idx);
		}
	}

	public enum ObjectType
	{
		Game = 0,
		Religions = 1,
		Kingdom = 2,
		Realm = 3,
		Castle = 4,
		Village = 5,
		Army = 6,
		Battle = 7,
		Character = 8,
		Mercenary = 9,
		Rebel = 10,
		Migrant = 11,
		War = 12,
		Marriage = 13,
		Rebellion = 14,
		Crusade = 15,
		EmperorOfTheWorld = 16,
		Pact = 17,
		COUNT = 18,
		Invalid = 18,
		Null = 18
	}

	private static bool initted;

	public const int LATEST_VERSION = 20;

	public const int KINGDOMS_VERSION = 2;

	public static int cur_version { get; private set; }

	public static int last_save_version { get; private set; }

	public static int cur_kingdoms_version { get; private set; }

	public static int last_save_kingdoms_version { get; private set; }

	public static State GetStateAttr(byte id, ObjectTypeInfo ti)
	{
		ti.states_by_id.TryGetValue(id, out var value);
		return value;
	}

	public static Event GetEventAttr(byte id, ObjectTypeInfo ti)
	{
		ti.events_by_id.TryGetValue(id, out var value);
		return value;
	}

	public static Substate GetSubstateAttr(byte id, byte substate_id, ObjectTypeInfo ti)
	{
		if (ti.states_by_id.TryGetValue(id, out var value) && value.substates_by_id.TryGetValue(substate_id, out var value2))
		{
			return value2;
		}
		return null;
	}

	public static List<Type> FindDerivedTypes(Type base_type)
	{
		List<Type> list = new List<Type>();
		Type[] types = Assembly.GetAssembly(base_type).GetTypes();
		foreach (Type type in types)
		{
			if (!(type == base_type) && base_type.IsAssignableFrom(type))
			{
				list.Add(type);
			}
		}
		return list;
	}

	public static void CheckConstructor(Type type, params Type[] ctor_params)
	{
		if (!(type.GetConstructor(ctor_params) == null))
		{
			return;
		}
		string text = "";
		for (int i = 0; i < ctor_params.Length; i++)
		{
			if (text != "")
			{
				text += ", ";
			}
			text += ctor_params[i].Name;
		}
		Error(type.ToString() + " has no constructor(" + text + ")");
	}

	public static void CheckConstructors(Type base_type, params Type[] ctor_params)
	{
		List<Type> list = FindDerivedTypes(base_type);
		for (int i = 0; i < list.Count; i++)
		{
			CheckConstructor(list[i], ctor_params);
		}
	}

	public static ObjectState CreateState(State attr)
	{
		if (attr == null)
		{
			return null;
		}
		return Reflection.Create<ObjectState>(attr.rtti);
	}

	public static ObjectSubstate CreateSubstate(Substate attr)
	{
		if (attr == null)
		{
			return null;
		}
		return Reflection.Create<ObjectSubstate>(attr.rtti);
	}

	public static ObjectEvent CreateEvent(Event attr)
	{
		if (attr == null)
		{
			return null;
		}
		return Reflection.Create<ObjectEvent>(attr.rtti);
	}

	public static byte[] ToBytes<T>(T[] array)
	{
		int num = Buffer.ByteLength(array);
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, num);
		return array2;
	}

	public static byte[] ToBytes<T>(T[,] array)
	{
		int num = Buffer.ByteLength(array);
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, num);
		return array2;
	}

	public static T[] ToArray<T>(byte[] bytes, int len)
	{
		T[] array = new T[len];
		int count = Buffer.ByteLength(array);
		Buffer.BlockCopy(bytes, 0, array, 0, count);
		return array;
	}

	public static T[,] ToArray<T>(byte[] bytes, int len1, int len2)
	{
		T[,] array = new T[len1, len2];
		int count = Buffer.ByteLength(array);
		Buffer.BlockCopy(bytes, 0, array, 0, count);
		return array;
	}

	public static void Init()
	{
		if (!initted)
		{
			initted = true;
			ObjectTypeInfo.Init();
		}
	}

	static Serialization()
	{
		initted = false;
		cur_version = 20;
		cur_kingdoms_version = 2;
		Init();
	}

	private static void Log(string msg)
	{
		Game.Log("[Serialization]: " + msg, Game.LogType.Message);
	}

	private static void Error(string msg)
	{
		Game.Log("[Serialization]: " + msg, Game.LogType.Error);
	}

	public static ObjectType GetTID(string type)
	{
		if (string.IsNullOrEmpty(type))
		{
			return ObjectType.COUNT;
		}
		if (!Enum.TryParse<ObjectType>(type, out var result))
		{
			return ObjectType.COUNT;
		}
		return result;
	}

	public static void PreprocessKingdomsVerison(Game game, DT.Field root)
	{
		DT.Field field = root.FindChild("kingdoms_version");
		if (field != null)
		{
			last_save_kingdoms_version = (cur_kingdoms_version = field.Int());
			root.DelChild(field);
		}
		else
		{
			last_save_kingdoms_version = (cur_kingdoms_version = 0);
		}
		List<string> list = new List<string>();
		if (cur_kingdoms_version < 2)
		{
			list.Add("Great_Wales");
			list.Add("Lotharingia");
			list.Add("Maghreb");
			list.Add("Outremer");
			list.Add("Egypt");
			list.Add("Arabia");
		}
		if (cur_kingdoms_version < 1)
		{
			list.Add("Persia");
		}
		for (int i = 0; i < list.Count; i++)
		{
			Dictionary<int, Logic.Object> objects = game.multiplayer.objects.Registry(ObjectType.Kingdom).objects;
			Kingdom kingdom = null;
			foreach (KeyValuePair<int, Logic.Object> item in objects)
			{
				Kingdom kingdom2 = item.Value as Kingdom;
				if (kingdom2.Name == list[i])
				{
					kingdom = kingdom2;
					break;
				}
			}
			game.multiplayer.objects.DelAndShift(ObjectType.Kingdom, kingdom);
			game.kingdoms.Remove(kingdom);
			game.starts.Remove(kingdom);
		}
		foreach (KeyValuePair<int, Logic.Object> @object in game.multiplayer.objects.Registry(ObjectType.Kingdom).objects)
		{
			(@object.Value as Kingdom).id = @object.Key;
		}
	}

	public static void PreprocessAllFields(Game game, DT.Field root)
	{
		DT.Field field = root.FindChild("save_version");
		if (field != null)
		{
			last_save_version = (cur_version = field.Int());
			root.DelChild(field);
		}
		else
		{
			last_save_version = (cur_version = 0);
		}
		if (root.children == null)
		{
			return;
		}
		for (int i = 0; i < root.children.Count; i++)
		{
			DT.Field field2 = root.children[i];
			if (!string.IsNullOrEmpty(field2.key))
			{
				PreprocessObjectField(game, field2, ref i);
			}
		}
	}

	public static void PreprocessObjectField(Game game, DT.Field obj_field, ref int child_idx)
	{
		if (obj_field.children == null)
		{
			return;
		}
		for (int i = 0; i < obj_field.children.Count; i++)
		{
			DT.Field field = obj_field.children[i];
			if (!string.IsNullOrEmpty(field.key))
			{
				PreprocessStateField(game, field, ref i);
			}
		}
	}

	public static void PreprocessStateField(Game game, DT.Field state_field, ref int child_idx)
	{
	}

	public static void PostProcessAllObjectsAfterLoad(Game game)
	{
		for (ObjectType objectType = ObjectType.Game; objectType < ObjectType.COUNT; objectType++)
		{
			foreach (KeyValuePair<int, Logic.Object> @object in game.multiplayer.objects.Registry(objectType).objects)
			{
				PostProcessObjectAfterLoad(@object.Value);
			}
		}
	}

	public static void PostProcessObjectAfterLoad(Logic.Object obj)
	{
	}

	public static void PostProcessAllObjectsAfterApply(Game game)
	{
		for (ObjectType objectType = ObjectType.Game; objectType < ObjectType.COUNT; objectType++)
		{
			foreach (KeyValuePair<int, Logic.Object> @object in game.multiplayer.objects.Registry(objectType).objects)
			{
				PostProcessObjectAfterApply(@object.Value);
			}
		}
	}

	public static void PostProcessObjectAfterApply(Logic.Object obj)
	{
	}

	public static void OnLoadComplete()
	{
		cur_version = 20;
	}

	public static void SetKingdomsVersion()
	{
		cur_kingdoms_version = 2;
	}
}
