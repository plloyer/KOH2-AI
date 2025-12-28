using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public class Object : Updateable, IVars, ISetVar, IRelationCheck
{
	public class NIDData : Data
	{
		public NID nid;

		public static NIDData Create()
		{
			return new NIDData();
		}

		public override string ToString()
		{
			return base.ToString() + "(" + nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Object obj2))
			{
				return false;
			}
			nid = obj2;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(nid, "nid");
		}

		public override void Load(Serialization.IReader ser)
		{
			nid = ser.ReadNID("nid");
		}

		public override object GetObject(Game game)
		{
			return nid.GetObj(game);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (obj == null)
			{
				return nid.id == 0;
			}
			if (!(obj is Object obj2))
			{
				return false;
			}
			if (obj2.GetNid(generateNid: false) != nid.nid)
			{
				return false;
			}
			return true;
		}
	}

	[Serialization.Event(0)]
	public class BeginObjectEvent : Serialization.ObjectEvent
	{
		public static BeginObjectEvent Create()
		{
			return new BeginObjectEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.Event(1)]
	public class EndObjectEvent : Serialization.ObjectEvent
	{
		public static EndObjectEvent Create()
		{
			return new EndObjectEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.Event(2)]
	public class StartObjectEvent : Serialization.ObjectEvent
	{
		public static StartObjectEvent Create()
		{
			return new StartObjectEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.Event(3)]
	public class FinishObjectEvent : Serialization.ObjectEvent
	{
		public static FinishObjectEvent Create()
		{
			return new FinishObjectEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.Event(4)]
	public class DeleteObjectEvent : Serialization.ObjectEvent
	{
		public static DeleteObjectEvent Create()
		{
			return new DeleteObjectEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.Event(5)]
	public class DeleteStateEvent : Serialization.ObjectEvent
	{
		public static DeleteStateEvent Create()
		{
			return new DeleteStateEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.Event(6)]
	public class DeleteSubStateEvent : Serialization.ObjectEvent
	{
		public static DeleteSubStateEvent Create()
		{
			return new DeleteSubStateEvent();
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	public enum ObjState
	{
		MultiplayerCreated,
		Created,
		Starting,
		Started,
		Finished,
		Destroying,
		Destroyed
	}

	public class DelayedDestroyTimer : Component
	{
		public int delayTime = 900;

		public static DelayedDestroyTimer Start(Object obj)
		{
			ObjState obj_state = obj.obj_state;
			obj.obj_state = ObjState.Started;
			DelayedDestroyTimer result = new DelayedDestroyTimer(obj);
			obj.obj_state = obj_state;
			return result;
		}

		public static void OnGameStarted(Game game, string started_from)
		{
			if (game.IsAuthority())
			{
				for (Object obj = game.first_object; obj != null; obj = obj.next_in_game)
				{
					obj.GetComponent<DelayedDestroyTimer>()?.OnGameStarted();
				}
			}
		}

		private DelayedDestroyTimer(Object obj)
			: base(obj)
		{
			DestroyAfter(delayTime);
		}

		public void OnGameStarted()
		{
			obj.Finish();
			obj.GetComponent<DelayedDestroyTimer>()?.DestroyAfter(delayTime);
		}

		public void DestroyAfter(int seconds)
		{
			ObjState obj_state = obj.obj_state;
			obj.obj_state = ObjState.Started;
			UpdateAfter(seconds, exact: true);
			obj.obj_state = obj_state;
		}

		public override void OnUpdate()
		{
			if (obj.IsAuthority())
			{
				obj.Destroy(forceDestroyInstantly: true);
			}
		}
	}

	[Serialization.State(1)]
	public class ActionsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class ActionState : Serialization.ObjectSubstate
		{
			public Data data;

			public ActionState()
			{
			}

			public ActionState(int idx, Action action)
			{
				substate_index = idx;
				data = Data.CreateFull(action);
			}

			public static ActionState Create()
			{
				return new ActionState();
			}

			public static bool IsNeeded(Object obj)
			{
				Actions component = obj.GetComponent<Actions>();
				if (component == null || component.active == null || component.active.Count == 0)
				{
					return false;
				}
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				Actions component = obj.GetComponent<Actions>();
				if (component == null || component.active == null || substate_index >= component.active.Count)
				{
					return false;
				}
				Action obj2 = component.active[substate_index];
				data = Data.CreateFull(obj2);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "action");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("action");
			}

			public override void ApplyTo(Object obj)
			{
				Actions component = obj.GetComponent<Actions>();
				if (component != null && component.active != null && substate_index < component.active.Count)
				{
					Action action = component.active[substate_index];
					if (action != null && !data.ApplyTo(action, obj.game))
					{
						Game.Log("Could not apply " + data.ToString() + " to " + action.ToString(), Game.LogType.Error);
					}
				}
			}
		}

		[Serialization.Substate(2)]
		public class OpportunityState : Serialization.ObjectSubstate
		{
			public Data data;

			public OpportunityState()
			{
			}

			public OpportunityState(int idx, Opportunity opportunity)
			{
				substate_index = idx;
				data = Data.CreateFull(opportunity);
			}

			public static OpportunityState Create()
			{
				return new OpportunityState();
			}

			public static bool IsNeeded(Object obj)
			{
				Actions component = obj.GetComponent<Actions>();
				if (component == null || component.opportunities == null || component.opportunities.Count == 0)
				{
					return false;
				}
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				Actions component = obj.GetComponent<Actions>();
				if (component == null || component.opportunities == null || substate_index >= component.opportunities.Count)
				{
					return false;
				}
				Opportunity obj2 = component.opportunities[substate_index];
				data = Data.CreateFull(obj2);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "opportunity");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("opportunity");
			}

			public override void ApplyTo(Object obj)
			{
				Actions component = obj.GetComponent<Actions>();
				if (component != null && component.opportunities != null && substate_index < component.opportunities.Count)
				{
					Opportunity opportunity = component.opportunities[substate_index];
					if (opportunity != null && !data.ApplyTo(opportunity, obj.game))
					{
						Game.Log("Could not apply " + data.ToString() + " to " + opportunity.ToString(), Game.LogType.Error);
					}
				}
			}
		}

		private List<string> active_ids;

		private int current;

		private List<string> opportunity_ids;

		private List<NID> opportunity_targets;

		private List<Data> opportunity_args;

		public static ActionsState Create()
		{
			return new ActionsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Actions component = obj.GetComponent<Actions>();
			if (component == null)
			{
				return false;
			}
			if (component.opportunities != null && component.opportunities.Count > 0)
			{
				return true;
			}
			if (component.active == null || component.active.Count == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Actions component = obj.GetComponent<Actions>();
			if (component == null)
			{
				return false;
			}
			if (component.active != null && component.active.Count > 0)
			{
				active_ids = new List<string>(component.active.Count);
				for (int i = 0; i < component.active.Count; i++)
				{
					Action action = component.active[i];
					if (action != null && action.def?.id != null)
					{
						active_ids.Add(action.def.id);
						AddSubstate(new ActionState(i, action));
					}
				}
				current = ((component.current != null) ? (component.active.IndexOf(component.current) + 1) : 0);
			}
			if (component.opportunities != null && component.opportunities.Count > 0)
			{
				opportunity_ids = new List<string>(component.opportunities.Count);
				opportunity_targets = new List<NID>(component.opportunities.Count);
				opportunity_args = new List<Data>(component.opportunities.Count);
				for (int j = 0; j < component.opportunities.Count; j++)
				{
					Opportunity opportunity = component.opportunities[j];
					opportunity_ids.Add(opportunity.action?.def.id);
					opportunity_targets.Add(opportunity.target);
					opportunity_args.Add(Data.CreateFull(opportunity.args));
					AddSubstate(new OpportunityState(j, opportunity));
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt((active_ids != null) ? active_ids.Count : 0, "active");
			if (active_ids != null)
			{
				ser.Write7BitUInt(current, "current");
				for (int i = 0; i < active_ids.Count; i++)
				{
					ser.WriteStr(active_ids[i], "active_id", i);
				}
			}
			ser.Write7BitUInt((opportunity_ids != null) ? opportunity_ids.Count : 0, "opportunities");
			if (opportunity_ids != null)
			{
				for (int j = 0; j < opportunity_ids.Count; j++)
				{
					ser.WriteStr(opportunity_ids[j], "opportunity_id", j);
					ser.WriteNID(opportunity_targets[j], "opportunity_target", j);
					ser.WriteData(opportunity_args[j], "opportunity_args", j);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("active");
			if (num > 0)
			{
				current = ser.Read7BitUInt("current");
				active_ids = new List<string>(num);
				for (int i = 0; i < num; i++)
				{
					string item = ser.ReadStr("active_id", i);
					active_ids.Add(item);
				}
			}
			int num2 = ser.Read7BitUInt("opportunities");
			if (num2 > 0)
			{
				opportunity_ids = new List<string>(num2);
				opportunity_targets = new List<NID>(num2);
				opportunity_args = new List<Data>(num2);
				for (int j = 0; j < num2; j++)
				{
					string item2 = ser.ReadStr("opportunity_id", j);
					NID item3 = ser.ReadNID("opportunity_target", j);
					Data item4 = ser.ReadData("opportunity_args", j);
					opportunity_ids.Add(item2);
					opportunity_targets.Add(item3);
					opportunity_args.Add(item4);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Actions component = obj.GetComponent<Actions>();
			if (component == null)
			{
				return;
			}
			if (component.active != null)
			{
				for (int i = 0; i < component.active.Count; i++)
				{
					Action action = component.active[i];
					if (action == null || active_ids == null || !active_ids.Contains(action.def.id))
					{
						component.active.RemoveAt(i);
						i--;
						action.Cancel();
					}
				}
			}
			if (active_ids == null)
			{
				if (component.active != null)
				{
					component.active.Clear();
				}
				component.current = null;
			}
			else
			{
				component.active = new List<Action>(active_ids.Count);
				for (int j = 0; j < active_ids.Count; j++)
				{
					string text = active_ids[j];
					Action item = component.Find(text);
					component.active.Add(item);
				}
				if (current > 0)
				{
					component.current = component.active[current - 1];
				}
			}
			if (component.opportunities != null)
			{
				component.opportunities.Clear();
			}
			if (opportunity_ids != null)
			{
				if (component.opportunities == null)
				{
					component.opportunities = new List<Opportunity>(opportunity_ids.Count);
				}
				for (int k = 0; k < opportunity_ids.Count; k++)
				{
					string text2 = opportunity_ids[k];
					NID nID = opportunity_targets[k];
					Data data = opportunity_args[k];
					Action action2 = component.Find(text2);
					Object obj2 = nID.GetObj(obj.game);
					Opportunity opportunity = new Opportunity();
					opportunity.action = action2;
					opportunity.target = obj2;
					opportunity.args = Data.RestoreObject<List<Value>>(data, obj.game);
					opportunity.active = action2 != null;
					opportunity.last_time = obj.game.time;
					if (component.opportunities == null)
					{
						component.opportunities = new List<Opportunity>();
					}
					component.opportunities.Add(opportunity);
				}
			}
			component.RescheduleOpportunities();
			obj.NotifyListeners("opportunities_changed");
			obj.UpdateAutomaticStatuses();
		}
	}

	[Serialization.State(2)]
	public class StatusesState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class StatusState : Serialization.ObjectSubstate
		{
			public Data data;

			public StatusState()
			{
			}

			public StatusState(int idx, Status status)
			{
				substate_index = idx;
				data = Data.CreateFull(status);
			}

			public static StatusState Create()
			{
				return new StatusState();
			}

			public override bool InitFrom(Object obj)
			{
				Status status = obj.GetStatus(substate_index);
				data = Data.CreateFull(status);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "status");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("status");
			}

			public override void ApplyTo(Object obj)
			{
				if (!(data is Status.FullData fullData))
				{
					return;
				}
				Statuses statuses = obj.statuses;
				if (statuses == null)
				{
					return;
				}
				Status status = statuses.Get(substate_index);
				if (status == null || status.IsAutomatic())
				{
					status = Status.Create(obj.game.defs.Find<Status.Def>(fullData.status_def_id));
					if (status == null)
					{
						obj.Warning("Could not load unknown status: '" + fullData.status_def_id + "'");
						return;
					}
					status.usid = fullData.usid;
					statuses.Set(substate_index, status, send_state: false);
				}
				if (status.usid != 0 && status.usid != fullData.usid)
				{
					obj.Warning("Replacing status USID of " + status.ToString());
				}
				status.usid = fullData.usid;
				if (!fullData.ApplyTo(status, obj.game))
				{
					Game.Log("Could not apply " + fullData.ToString() + " to " + status.ToString(), Game.LogType.Error);
				}
				obj.NotifyListeners("statuses_changed");
			}
		}

		public List<int> usids;

		public int last_usid;

		public static StatusesState Create()
		{
			return new StatusesState();
		}

		public static bool IsNeeded(Object obj)
		{
			Statuses statuses = obj.statuses;
			if (statuses == null)
			{
				return false;
			}
			if (statuses.PersistentCount() == 0 && statuses.last_usid == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Statuses statuses = obj.statuses;
			if (statuses == null)
			{
				return false;
			}
			last_usid = statuses.last_usid;
			int num = statuses.PersistentCount();
			if (num == 0)
			{
				return last_usid != 0;
			}
			usids = new List<int>(num);
			for (int num2 = 0; num2 < num; num2++)
			{
				Status status = statuses[num2];
				int num3;
				int num4;
				if (status != null)
				{
					num3 = ((!status.IsAutomatic()) ? 1 : 0);
					if (num3 != 0)
					{
						num4 = status.usid;
						goto IL_005f;
					}
				}
				else
				{
					num3 = 0;
				}
				num4 = 0;
				goto IL_005f;
				IL_005f:
				int item = num4;
				usids.Add(item);
				if (num3 != 0)
				{
					AddSubstate(new StatusState(num2, status));
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(last_usid, "last_usid");
			if (usids == null)
			{
				ser.Write7BitUInt(0, "count");
				return;
			}
			ser.Write7BitUInt(usids.Count, "count");
			for (int i = 0; i < usids.Count; i++)
			{
				int val = usids[i];
				ser.Write7BitUInt(val, "usid", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			last_usid = ser.Read7BitUInt("last_usid");
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				usids = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					int item = ser.Read7BitUInt("usid", i);
					usids.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Statuses statuses = obj.statuses;
			if (statuses == null && last_usid != 0)
			{
				statuses = new Statuses(obj);
				statuses.last_usid = last_usid;
			}
			if (usids == null)
			{
				statuses?.DestroyAll(clear_default_status: false);
				obj.UpdateAutomaticStatuses();
				return;
			}
			if (statuses == null)
			{
				statuses = new Statuses(obj);
			}
			for (int num = statuses.Count - 1; num >= 0; num--)
			{
				Status status = statuses[num];
				if (status != null && (status.IsAutomatic() || !usids.Contains(status.usid)) && status.IsValid())
				{
					status.SetOwner(null);
					status.Destroy();
				}
			}
			List<Status> list = new List<Status>(usids.Count);
			for (int i = 0; i < usids.Count; i++)
			{
				int num2 = usids[i];
				Status item = ((num2 <= 0) ? null : statuses.Find(num2));
				list.Add(item);
			}
			statuses.main = null;
			if (statuses.additional != null)
			{
				statuses.additional.Clear();
			}
			else if (list.Count > 1)
			{
				statuses.additional = new List<Status>(list.Count);
			}
			statuses.automatic = null;
			for (int j = 0; j < list.Count; j++)
			{
				Status status2 = list[j];
				if (j == 0)
				{
					statuses.main = status2;
				}
				else
				{
					statuses.additional.Add(status2);
				}
			}
			obj.UpdateAutomaticStatuses();
			obj.NotifyListeners("statuses_changed");
		}
	}

	[Serialization.State(3)]
	public class DefaultStatusTypeState : Serialization.ObjectState
	{
		private Type type;

		public static DefaultStatusTypeState Create()
		{
			return new DefaultStatusTypeState();
		}

		public static bool IsNeeded(Object obj)
		{
			Statuses statuses = obj.statuses;
			if (statuses == null || statuses.default_type == null)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			type = obj.statuses?.default_type;
			return type != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr((type == null) ? "" : type.Name, "type");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			string text = ser.ReadStr("type");
			type = (string.IsNullOrEmpty(text) ? null : Type.GetType("Logic." + text));
		}

		public override void ApplyTo(Object obj)
		{
			Statuses statuses = obj.statuses;
			if (statuses == null)
			{
				if (type == null)
				{
					return;
				}
				statuses = new Statuses(obj);
			}
			statuses.default_type = type;
		}
	}

	[Serialization.State(4)]
	public class OffersState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class OfferState : Serialization.ObjectSubstate
		{
			public Data data;

			public OfferState()
			{
			}

			public OfferState(int idx, Offer offer)
			{
				substate_index = idx;
				data = Data.CreateFull(offer);
			}

			public OfferState(int idx, Offer offer, string answer)
			{
				substate_index = idx;
				data = Data.CreateFull(offer);
			}

			public static OfferState Create()
			{
				return new OfferState();
			}

			public override bool InitFrom(Object obj)
			{
				Offers offers = Offers.Get(obj, create: false);
				if (offers == null)
				{
					return false;
				}
				Offer obj2 = offers.FindIncoming(substate_index);
				data = Data.CreateFull(obj2);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "offer");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("offer");
			}

			public override void ApplyTo(Object obj)
			{
				if (!(data is Offer.FullData fullData))
				{
					return;
				}
				Offers offers = Offers.Get(obj, create: false);
				if (offers == null)
				{
					return;
				}
				Offer offer = fullData.GetObject(obj.game) as Offer;
				if (!fullData.ApplyTo(offer, obj.game))
				{
					Game.Log("Could not apply " + fullData.ToString() + " to " + offer.ToString(), Game.LogType.Error);
				}
				else if (offers.FindIncoming(offer.uoid) == null)
				{
					Offer.Def def = obj.game.defs.Find<Offer.Def>(fullData.def_id);
					if (DBGOffersData.tracking_enabled)
					{
						Offer.dbg_offers_data[def.field.key].RecordSending(offer);
					}
					offers.Add(offer);
					if (offer.from != null)
					{
						Offers.Add(offer.from, offer);
					}
				}
				else if (offer.answer != null)
				{
					offer.Answer(offer.answer, send_event: false);
				}
			}
		}

		public List<int> uoids;

		public int last_uoid;

		public static OffersState Create()
		{
			return new OffersState();
		}

		public static bool IsNeeded(Object obj)
		{
			Offers offers = Offers.Get(obj, create: false);
			if (offers == null || offers.incoming == null)
			{
				return false;
			}
			if (offers.incoming.Count == 0 && offers.last_uoid == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Offers offers = Offers.Get(obj, create: false);
			if (offers == null)
			{
				return false;
			}
			last_uoid = offers.last_uoid;
			if (offers.incoming == null)
			{
				return false;
			}
			int count = offers.incoming.Count;
			if (count == 0)
			{
				return last_uoid != 0;
			}
			uoids = new List<int>(count);
			for (int i = 0; i < count; i++)
			{
				Offer offer = offers.incoming[i];
				int item = offer?.uoid ?? 0;
				uoids.Add(item);
				if (offer != null)
				{
					AddSubstate(new OfferState(i, offer));
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(last_uoid, "last_uoid");
			if (uoids == null)
			{
				ser.Write7BitUInt(0, "count");
				return;
			}
			ser.Write7BitUInt(uoids.Count, "count");
			for (int i = 0; i < uoids.Count; i++)
			{
				int val = uoids[i];
				ser.Write7BitUInt(val, "uoid", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			last_uoid = ser.Read7BitUInt("last_uoid");
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				uoids = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					int item = ser.Read7BitUInt("uoid", i);
					uoids.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Offers offers = Offers.Get(obj, create: true);
			offers.last_uoid = last_uoid;
			if (uoids != null && offers.incoming == null)
			{
				offers.incoming = new List<Offer>();
			}
			if (offers.incoming == null)
			{
				return;
			}
			for (int i = 0; i < offers.incoming.Count; i++)
			{
				Offer offer = offers.incoming[i];
				if (uoids == null || (offer != null && !uoids.Contains(offer.uoid)))
				{
					Offers.Del(offer.to, offer);
					Offers.Del(offer.from, offer);
				}
			}
		}
	}

	[Serialization.State(5)]
	public class TimersState : Serialization.ObjectState
	{
		private struct TimerData
		{
			public string name;

			public float elapsed;

			public float duration;

			public int tick;
		}

		private List<TimerData> timers = new List<TimerData>();

		public static TimersState Create()
		{
			return new TimersState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (obj.components == null)
			{
				return false;
			}
			for (int i = 0; i < obj.components.Count; i++)
			{
				if (obj.components[i] is Timer)
				{
					return true;
				}
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (obj.components == null)
			{
				return false;
			}
			for (int i = 0; i < obj.components.Count; i++)
			{
				if (obj.components[i] is Timer timer && !(timer.start_time == Time.Zero))
				{
					timers.Add(new TimerData
					{
						name = timer.name,
						elapsed = timer.Elapsed(),
						duration = timer.duration,
						tick = timer.tick
					});
				}
			}
			return timers.Count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(timers.Count, "count");
			for (int i = 0; i < timers.Count; i++)
			{
				TimerData timerData = timers[i];
				ser.WriteStr(timerData.name, "name_", i);
				ser.WriteFloat(timerData.elapsed, "elapsed_", i);
				ser.WriteFloat(timerData.duration, "duration_", i);
				ser.Write7BitUInt(timerData.tick, "tick_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					string name = ser.ReadStr("name_", i);
					float elapsed = ser.ReadFloat("elapsed_", i);
					float duration = ser.ReadFloat("duration_", i);
					int tick = ser.Read7BitUInt("tick_", i);
					timers.Add(new TimerData
					{
						name = name,
						elapsed = elapsed,
						duration = duration,
						tick = tick
					});
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (obj.components != null)
			{
				for (int num = obj.components.Count - 1; num >= 0; num--)
				{
					if (obj.components[num] is Timer timer)
					{
						timer.Stop();
					}
				}
			}
			for (int i = 0; i < timers.Count; i++)
			{
				TimerData timerData = timers[i];
				float duration = timerData.duration - timerData.elapsed;
				Timer timer2 = new Timer(obj, timerData.name, duration);
				timer2.def = Timer.Def.Find(obj.game, timerData.name);
				timer2.start_time = obj.game.time - timerData.elapsed;
				timer2.duration = timerData.duration;
				timer2.tick = timerData.tick;
				if (timer2.def?.rtdef != null)
				{
					timer2.def.rtdef.OnStart(timer2);
				}
			}
		}
	}

	[Serialization.State(6)]
	public class QuestsState : Serialization.ObjectState
	{
		public int last_id;

		public List<Data> datas;

		public static QuestsState Create()
		{
			return new QuestsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Quests quests = obj.quests;
			if (quests == null)
			{
				return false;
			}
			if (quests.Count == 0 && quests.last_uqid == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Quests quests = obj.quests;
			if (quests == null)
			{
				return false;
			}
			last_id = quests.last_uqid;
			int count = quests.Count;
			if (count == 0)
			{
				return last_id != 0;
			}
			datas = new List<Data>(count);
			for (int i = 0; i < count; i++)
			{
				Quest obj2 = obj.quests?.Get(i);
				datas.Add(Data.CreateFull(obj2));
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(last_id, "last_uqid");
			if (datas == null || datas.Count == 0)
			{
				ser.Write7BitUInt(0, "count");
				return;
			}
			ser.Write7BitUInt(datas.Count, "count");
			for (int i = 0; i < datas.Count; i++)
			{
				Data data = datas[i];
				ser.WriteData(data, "data_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			last_id = ser.Read7BitUInt("last_uqid");
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				datas = new List<Data>(num);
				for (int i = 0; i < num; i++)
				{
					Data item = ser.ReadData("data_", i);
					datas.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Quests quests = obj.quests;
			if (quests == null && last_id != 0)
			{
				quests = new Quests(obj);
				quests.last_uqid = last_id;
			}
			if (datas == null)
			{
				return;
			}
			for (int num = quests.Count - 1; num >= 0; num--)
			{
				_ = quests[num];
			}
			for (int i = 0; i < datas.Count; i++)
			{
				Quest.FullData fullData = datas[i] as Quest.FullData;
				if (fullData == null)
				{
					Game.Log("Could not apply " + fullData.ToString() + " is null", Game.LogType.Error);
					continue;
				}
				Quest quest = ((fullData.uqid <= 0) ? null : quests.Find(fullData.uqid));
				if (quest == null)
				{
					quest = fullData.GetObject(obj.game) as Quest;
					quests.Set(i, quest, send_state: false);
				}
				fullData.ApplyTo(quest, obj.game);
			}
			obj.NotifyListeners("quest_changed");
		}
	}

	[Serialization.State(7)]
	public class VarsState : Serialization.ObjectState
	{
		public Data data;

		public static VarsState Create()
		{
			return new VarsState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (obj.set_vars == null)
			{
				return false;
			}
			if (obj.set_vars.Empty())
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (obj.set_vars == null || obj.set_vars.Empty())
			{
				return false;
			}
			data = obj.set_vars.CreateFullData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "vars");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("vars");
		}

		public override void ApplyTo(Object obj)
		{
			obj.set_vars = Data.RestoreObject<Vars>(data, obj.game);
			obj.NotifyListeners("vars_changed");
		}
	}

	[Serialization.State(8)]
	public class RulesState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class RuleState : Serialization.ObjectSubstate
		{
			public Data data;

			public RuleState()
			{
			}

			public RuleState(GameRule rule)
			{
				substate_index = rule.urid;
				data = Data.CreateFull(rule);
			}

			public static RuleState Create()
			{
				return new RuleState();
			}

			public override bool InitFrom(Object obj)
			{
				ObjRules objRules = ObjRules.Get(obj, create: false);
				if (objRules == null)
				{
					return false;
				}
				GameRule obj2 = objRules.Find(substate_index);
				data = Data.CreateFull(obj2);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "rule");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("rule");
			}

			public override void ApplyTo(Object obj)
			{
				ObjRules objRules = ObjRules.Get(obj, create: true);
				GameRule gameRule = objRules.Find(substate_index);
				if (gameRule == null)
				{
					gameRule = Data.RestoreObject<GameRule>(data, obj.game);
					if (gameRule == null)
					{
						return;
					}
					objRules.Add(gameRule, send_state: false);
				}
				else
				{
					data.ApplyTo(gameRule, obj.game);
					if (!gameRule.IsActive())
					{
						objRules.Reschedule();
					}
				}
				Challenge.RebindRules(obj as Kingdom);
				if (gameRule.IsActive())
				{
					gameRule.def.activations++;
				}
				obj.game.game_rules.CleanUp();
				obj.NotifyListeners("rules_changed");
			}
		}

		[Serialization.Substate(2)]
		public class DelRuleState : Serialization.ObjectSubstate
		{
			public Data data;

			public DelRuleState()
			{
			}

			public DelRuleState(int index)
			{
				substate_index = index;
			}

			public static DelRuleState Create()
			{
				return new DelRuleState();
			}

			public override bool InitFrom(Object obj)
			{
				if (ObjRules.Get(obj, create: false) == null)
				{
					return false;
				}
				return true;
			}

			public static bool IsNeeded(Object obj)
			{
				return false;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
			}

			public override void ReadBody(Serialization.IReader ser)
			{
			}

			public override void ApplyTo(Object obj)
			{
				ObjRules objRules = ObjRules.Get(obj, create: true);
				if (objRules != null && objRules.rules != null)
				{
					GameRule gameRule = objRules.Find(substate_index);
					if (gameRule != null)
					{
						objRules.Del(gameRule, send_state: false);
						objRules.Reschedule();
						Challenge.RebindRules(obj as Kingdom);
						obj.game.game_rules.CleanUp();
						obj.NotifyListeners("rules_changed");
					}
				}
			}
		}

		public static RulesState Create()
		{
			return new RulesState();
		}

		public static bool IsNeeded(Object obj)
		{
			ObjRules objRules = ObjRules.Get(obj, create: false);
			if (objRules == null || objRules.rules == null)
			{
				return false;
			}
			for (int i = 0; i < objRules.rules.Count; i++)
			{
				if (objRules.rules[i] != null)
				{
					return true;
				}
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			ObjRules objRules = ObjRules.Get(obj, create: false);
			if (objRules == null || objRules.rules == null)
			{
				return false;
			}
			for (int i = 0; i < objRules.rules.Count; i++)
			{
				GameRule gameRule = objRules.rules[i];
				if (gameRule != null)
				{
					AddSubstate(new RuleState(gameRule));
				}
			}
			if (substates != null)
			{
				return substates.Count > 0;
			}
			return false;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			ObjRules.Get(obj, create: true).rules = null;
		}
	}

	[Serialization.State(9)]
	public class FadingModifiersState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class FadingModifierState : Serialization.ObjectSubstate
		{
			public Data data;

			public FadingModifierState()
			{
			}

			public FadingModifierState(FadingModifier mod)
			{
				substate_index = mod.umid;
				data = mod.GetFullData();
			}

			public static FadingModifierState Create()
			{
				return new FadingModifierState();
			}

			public override bool InitFrom(Object obj)
			{
				FadingModifier fadingModifier = obj?.GetStats()?.GetFadingModifier(substate_index);
				if (fadingModifier == null)
				{
					return false;
				}
				data = fadingModifier.GetFullData();
				if (data == null)
				{
					return false;
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "fading_modifier");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("fading_modifier");
			}

			public override void ApplyTo(Object obj)
			{
				obj.GetStats();
				if (!data.ApplyTo(obj, obj.game))
				{
					Game.Log("Error applying data to a fading modifier!", Game.LogType.Error);
				}
			}
		}

		private int last_ufmid;

		public static FadingModifiersState Create()
		{
			return new FadingModifiersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj.GetStats()?.fadingModifiers.Count ?? 0) > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Stats stats = obj.GetStats();
			if (stats == null)
			{
				return false;
			}
			for (int i = 0; i < stats.fadingModifiers.Count; i++)
			{
				AddSubstate(new FadingModifierState(stats.fadingModifiers[i]));
			}
			last_ufmid = stats.last_ufmid;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(last_ufmid, "last_ufmid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			last_ufmid = ser.Read7BitUInt("last_ufmid");
		}

		public override void ApplyTo(Object obj)
		{
			Stats stats = obj.GetStats();
			if (stats == null || stats.stats == null)
			{
				return;
			}
			stats.last_ufmid = last_ufmid;
			foreach (Stat stat in stats.stats)
			{
				if (stat.all_mods == null)
				{
					continue;
				}
				for (int num = stat.all_mods.Count - 1; num >= 0; num--)
				{
					if (stat.all_mods[num] is FadingModifier { def: not null } fadingModifier)
					{
						stat.DelModifier(fadingModifier, notify_changed: false);
					}
				}
			}
		}
	}

	[Serialization.State(10)]
	public class FinishedState : Serialization.ObjectState
	{
		public float delayed_destroyed_time_delta;

		public static FinishedState Create()
		{
			return new FinishedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return obj.finished;
		}

		public override bool InitFrom(Object obj)
		{
			if (obj == null)
			{
				return false;
			}
			DelayedDestroyTimer component = obj.GetComponent<DelayedDestroyTimer>();
			if (component == null && obj.finished)
			{
				Game.Log("FinishedState called on object without a delayed timer", Game.LogType.Error);
				return false;
			}
			delayed_destroyed_time_delta = component.tmNextUpdate - obj.game.time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(delayed_destroyed_time_delta, "delayed_destroyed_time_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			delayed_destroyed_time_delta = ser.ReadFloat("delayed_destroyed_time_delta");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj != null)
			{
				DelayedDestroyTimer.Start(obj).delayTime = (int)((delayed_destroyed_time_delta > 1f) ? delayed_destroyed_time_delta : 1f);
			}
		}
	}

	[Serialization.Event(17)]
	public class ExecuteActionEvent : Serialization.ObjectEvent
	{
		private string cur_action_def;

		private NID target_nid;

		public List<Data> args;

		public ExecuteActionEvent()
		{
		}

		public static ExecuteActionEvent Create()
		{
			return new ExecuteActionEvent();
		}

		public ExecuteActionEvent(Action action, Object target)
		{
			cur_action_def = action.def.id;
			target_nid = target;
			if (action.args != null)
			{
				args = new List<Data>();
				for (int i = 0; i < action.args.Count; i++)
				{
					args.Add(action.args[i].CreateData());
				}
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(cur_action_def, "cur_action_def");
			ser.WriteNID(target_nid, "target_nid");
			if (args != null && args.Count > 0)
			{
				ser.Write7BitUInt(args.Count, "args_count");
				for (int i = 0; i < args.Count; i++)
				{
					ser.WriteData(args[i], "args_", i);
				}
			}
			else
			{
				ser.Write7BitUInt(0, "args_count");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			cur_action_def = ser.ReadStr("cur_action_def");
			target_nid = ser.ReadNID("target_nid");
			int num = ser.Read7BitUInt("args_count");
			if (num > 0)
			{
				args = new List<Data>();
				for (int i = 0; i < num; i++)
				{
					args.Add(ser.ReadData("args_", i));
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Action.Def def = obj.game.defs.Get<Action.Def>(cur_action_def);
			if (!def.valid)
			{
				obj.Error("Unknown action def: " + def);
				return;
			}
			Action action = Action.Find(obj, def);
			if (action == null)
			{
				return;
			}
			Object target = (action.target = target_nid.GetObj(obj.game));
			if (args != null)
			{
				action.args = new List<Value>();
				for (int i = 0; i < args.Count; i++)
				{
					action.args.Add(args[i].GetValue(action.game));
				}
			}
			else
			{
				action.args = null;
			}
			action.Execute(target);
		}
	}

	[Serialization.Event(18)]
	public class DeleteObjectCustomEvent : Serialization.ObjectEvent
	{
		private NID target_nid;

		public DeleteObjectCustomEvent()
		{
		}

		public static DeleteObjectCustomEvent Create()
		{
			return new DeleteObjectCustomEvent();
		}

		public DeleteObjectCustomEvent(Object target)
		{
			target_nid = target;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(target_nid, "target_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			target_nid = ser.ReadNID("target_nid");
		}

		public override void ApplyTo(Object obj)
		{
			target_nid.GetObj(obj.game)?.Destroy();
		}
	}

	[Serialization.Event(19)]
	public class GameEventMessage : Serialization.ObjectEvent
	{
		private Data data;

		public GameEventMessage()
		{
		}

		public static GameEventMessage Create()
		{
			return new GameEventMessage();
		}

		public GameEventMessage(Event evt)
		{
			data = Data.Create(evt);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "event");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("event");
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj.game;
			Event obj2 = Data.RestoreObject<Event>(data, game);
			if (obj2 != null)
			{
				Kingdom kingdom = game.GetKingdom(game.multiplayer.playerData.kingdomId);
				if (obj2.ShouldSendTo(kingdom))
				{
					obj2.SetPlayer(kingdom);
					obj.OnEvent(obj2);
				}
			}
		}
	}

	[Serialization.Event(20)]
	public class SendOfferEvent : Serialization.ObjectEvent
	{
		public Data data;

		public SendOfferEvent()
		{
		}

		public static SendOfferEvent Create()
		{
			return new SendOfferEvent();
		}

		public SendOfferEvent(Offer offer)
		{
			data = Data.CreateFull(offer);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "offer");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("offer");
		}

		public override void ApplyTo(Object obj)
		{
			if (data is Offer.FullData fullData)
			{
				Offer.Def def = obj.game.defs.Find<Offer.Def>(fullData.def_id);
				Object obj2 = fullData.to_nid.GetObj(obj.game);
				Object obj3 = fullData.from_nid.GetObj(obj.game);
				Offer offer = Offer.Create(def, obj3, obj2);
				fullData.ApplyTo(offer, obj.game);
				offer.Send(create_uoid: false);
			}
		}
	}

	[Serialization.Event(21)]
	public class CancelOfferEvent : Serialization.ObjectEvent
	{
		public Data data;

		public CancelOfferEvent()
		{
		}

		public static CancelOfferEvent Create()
		{
			return new CancelOfferEvent();
		}

		public CancelOfferEvent(Offer offer)
		{
			data = Data.Create(offer);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "offer");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("offer");
		}

		public override void ApplyTo(Object obj)
		{
			if (data.GetObject(obj.game) is Offer offer)
			{
				offer.Cancel();
			}
		}
	}

	[Serialization.Event(22)]
	public class AnswerOfferEvent : Serialization.ObjectEvent
	{
		public Data data;

		public string answer;

		public AnswerOfferEvent()
		{
		}

		public static AnswerOfferEvent Create()
		{
			return new AnswerOfferEvent();
		}

		public AnswerOfferEvent(Offer offer, string answer)
		{
			data = Data.Create(offer);
			this.answer = answer;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "offer");
			ser.WriteStr(answer, "answer");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("offer");
			answer = ser.ReadStr("answer");
		}

		public override void ApplyTo(Object obj)
		{
			if (data.GetObject(obj.game) is Offer offer)
			{
				offer.Answer(answer);
			}
		}
	}

	[Serialization.Event(23)]
	public class CompleteQuestEvent : Serialization.ObjectEvent
	{
		public Data data;

		public CompleteQuestEvent()
		{
		}

		public static CompleteQuestEvent Create()
		{
			return new CompleteQuestEvent();
		}

		public CompleteQuestEvent(Quest quest)
		{
			data = Data.Create(quest);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "quest");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("quest");
		}

		public override void ApplyTo(Object obj)
		{
			(data.GetObject(obj.game) as Quest).Complete();
		}
	}

	[Serialization.Event(24)]
	public class SetVarEvent : Serialization.ObjectEvent
	{
		public string key;

		public Data val;

		public SetVarEvent()
		{
		}

		public static SetVarEvent Create()
		{
			return new SetVarEvent();
		}

		public SetVarEvent(string key, Value val)
		{
			this.key = key;
			this.val = val.CreateData();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(key, "key");
			ser.WriteData(val, "val");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			key = ser.ReadStr("key");
			val = ser.ReadData("val");
		}

		public override void ApplyTo(Object obj)
		{
			Value value = ((val == null) ? Value.Null : val.GetValue(obj.game));
			if (obj.IsAuthority())
			{
				obj.Error($"Attempting to SetVar('{key}', {value}) to authority object");
			}
			else
			{
				obj.SetVar(key, value);
			}
		}
	}

	[Serialization.Event(25)]
	public class DelFadingModifierEvent : Serialization.ObjectEvent
	{
		public int umid;

		public DelFadingModifierEvent()
		{
		}

		public static DelFadingModifierEvent Create()
		{
			return new DelFadingModifierEvent();
		}

		public DelFadingModifierEvent(int umid)
		{
			this.umid = umid;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(umid, "umid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			umid = ser.Read7BitUInt("umid");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj.IsAuthority())
			{
				obj.Error("DelFadingModifierEvent sent to authority object");
				return;
			}
			Stats stats = obj.GetStats();
			if (stats != null)
			{
				FadingModifier fadingModifier = stats.GetFadingModifier(umid);
				fadingModifier?.stat.DelModifier(fadingModifier);
			}
		}
	}

	protected int nid;

	public Serialization.ObjectStates state_acc;

	public Game game;

	public uint uid;

	public IListener visuals;

	private uint flags;

	public Vars set_vars;

	public List<Component> components;

	private List<IListener> listeners;

	public Object prev_in_game;

	public Object next_in_game;

	public const uint InNotifyListenersFlag = 8u;

	public const uint ToSendFlag = 16u;

	private const int STATES_IDX = 0;

	private const int EVENTS_IDX = 16;

	public ObjState obj_state
	{
		get
		{
			return (ObjState)(flags & 7);
		}
		private set
		{
			flags = (flags & 0xFFFFFFF8u) | (uint)value;
		}
	}

	public bool started => obj_state == ObjState.Started;

	public bool finished => obj_state == ObjState.Finished;

	public bool destroyed => obj_state == ObjState.Destroyed;

	public Statuses statuses => GetComponent<Statuses>();

	public Quests quests => GetComponent<Quests>();

	public Object(Multiplayer multiplayer)
	{
		game = multiplayer.game;
		obj_state = ObjState.MultiplayerCreated;
		Init();
	}

	public override bool IsRefSerializable()
	{
		if (rtti.ti == null)
		{
			return base.IsRefSerializable();
		}
		return true;
	}

	public override Data CreateRefData()
	{
		if (rtti.ti == null)
		{
			return base.CreateRefData();
		}
		NIDData nIDData = new NIDData();
		nIDData.rtti = rtti.ref_data_rtti;
		if (!nIDData.InitFrom(this))
		{
			return null;
		}
		return nIDData;
	}

	public virtual void Load(Serialization.ObjectStates states)
	{
	}

	public int GetNid(bool generateNid = true)
	{
		if (started && nid == 0)
		{
			return 0;
		}
		if (!IsValid() && !finished)
		{
			return 0;
		}
		if (generateNid && nid == 0)
		{
			game.multiplayer.AddObj(this);
		}
		return nid;
	}

	public void SetNid(int nid, bool update_registry = true)
	{
		bool num = this.nid == 0;
		bool flag = nid == 0;
		if (!num && !flag)
		{
			Warning("Changing nid from " + NID.ToString(this.nid) + " to " + NID.ToString(nid));
		}
		if (flag && update_registry)
		{
			Error("SetNid(0, true) should not be called");
		}
		this.nid = nid;
		if (update_registry && nid != 0 && game != null && game.multiplayer != null)
		{
			game.multiplayer.AddObj(this);
		}
	}

	public void SetNid_NoError(int nid)
	{
		this.nid = nid;
	}

	public void SendState<T>() where T : Serialization.ObjectState
	{
		Type typeFromHandle = typeof(T);
		Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(this);
		byte value;
		if (objectTypeInfo == null)
		{
			Error("Attempting to send " + typeFromHandle.ToString() + " for object of unknown type: " + ToString());
		}
		else if (!objectTypeInfo.state_ids.TryGetValue(typeFromHandle, out value))
		{
			Error("Attempting to send unknown state " + typeFromHandle.ToString() + " for " + ToString());
		}
		else if (IsValid() && game.state != Game.State.LoadingMap && game.multiplayer != null && IsAuthority() && !GetFlag(16u))
		{
			game.multiplayer.SendState<T>(this);
		}
	}

	public void SendSubstate<T>(int idx) where T : Serialization.ObjectSubstate
	{
		if (game.state != Game.State.LoadingMap && game.multiplayer != null && IsAuthority() && !GetFlag(16u))
		{
			game.multiplayer.SendSubstate<T>(this, idx);
		}
	}

	public void SendEvent(Serialization.ObjectEvent objEvent)
	{
		if (game.state != Game.State.LoadingMap && game.multiplayer != null && game.multiplayer.IsOnline())
		{
			game.multiplayer.SendObjEvent(this, objEvent);
		}
	}

	public bool IsValid()
	{
		if (obj_state != ObjState.Finished && obj_state != ObjState.Destroying)
		{
			return obj_state != ObjState.Destroyed;
		}
		return false;
	}

	public static bool IsValid(Object obj)
	{
		return obj?.IsValid() ?? false;
	}

	public bool GetFlag(uint flag)
	{
		return (flags & flag) != 0;
	}

	public void ClrFlag(uint flag)
	{
		flags &= ~flag;
	}

	public void SetFlag(uint flag)
	{
		flags |= flag;
	}

	public void SetFlag(uint flag, bool set)
	{
		if (set)
		{
			SetFlag(flag);
		}
		else
		{
			ClrFlag(flag);
		}
	}

	public virtual string GetNameKey(IVars vars = null, string form = "")
	{
		return null;
	}

	public void AddListener(IListener listener)
	{
		if (listener != null)
		{
			if (listeners == null)
			{
				listeners = new List<IListener>();
			}
			else if (listeners.Contains(listener))
			{
				return;
			}
			listeners.Add(listener);
		}
	}

	public void DelListener(IListener listener)
	{
		if (listener == null || listeners == null)
		{
			return;
		}
		int num = listeners.IndexOf(listener);
		if (num >= 0)
		{
			if (GetFlag(8u))
			{
				listeners[num] = null;
			}
			else
			{
				listeners.RemoveAt(num);
			}
		}
	}

	public void NotifyVisuals(string message, object param = null)
	{
		if (visuals != null)
		{
			visuals.OnMessage(this, message, param);
		}
	}

	public void NotifyListeners(string message, object param = null)
	{
		NotifyListeners(message, param, may_trigger: true);
	}

	public void NotifyListeners(string message, object param, bool may_trigger, bool profile = true)
	{
		if (may_trigger)
		{
			if (game.analytics != null)
			{
				game.analytics.OnMessage(this, message, param);
			}
			if (game.tutorial_listener != null)
			{
				game.tutorial_listener.OnMessage(this, message, param);
			}
			if (IsAuthority() && game?.game_rules != null)
			{
				game.game_rules.OnNotification(this, message, param);
			}
		}
		if (visuals == null && listeners == null)
		{
			return;
		}
		string text = null;
		if (profile && visuals != null && game != null)
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
				Error("Error in NotifyListeners('" + message + "'): " + ex);
			}
		}
		if (listeners != null)
		{
			bool flag = GetFlag(8u);
			if (!flag)
			{
				SetFlag(8u);
			}
			bool flag2 = false;
			for (int num = listeners.Count - 1; num >= 0; num--)
			{
				IListener listener = listeners[num];
				if (listener == null)
				{
					if (!flag)
					{
						flag2 = true;
					}
				}
				else
				{
					try
					{
						listener.OnMessage(this, message, param);
					}
					catch (Exception ex2)
					{
						Error("Error in NotifyListeners('" + message + "'): " + ex2.ToString());
					}
				}
			}
			if (!flag)
			{
				ClrFlag(8u);
				if (!IsValid())
				{
					listeners.Clear();
					flag2 = false;
				}
			}
			if (flag2)
			{
				for (int num2 = listeners.Count - 1; num2 >= 0; num2--)
				{
					if (listeners[num2] == null)
					{
						listeners.RemoveAt(num2);
					}
				}
			}
		}
		if (profile && text != null)
		{
			Game.EndProfileSection(text);
		}
	}

	public void NotifyListenersDelayed(string message, object param, bool process_triggers, bool profile)
	{
		MainThreadUpdates.Perform(delegate
		{
			NotifyListeners(message, param, process_triggers, process_triggers);
		});
	}

	public virtual void OnEvent(Event evt)
	{
		game.NotifyListeners("on_event", evt);
		if (evt.notify_listeners)
		{
			NotifyListeners(evt.id, evt.param);
		}
	}

	public static string TypeToStr(Type type)
	{
		string text = type.ToString();
		if (text.StartsWith("Logic.", StringComparison.Ordinal))
		{
			text = text.Substring(6);
		}
		return text.Replace('+', '.');
	}

	public static string ToString(object obj)
	{
		if (obj == null)
		{
			return "null";
		}
		if (obj is DictionaryEntry dictionaryEntry)
		{
			return "(" + ToString(dictionaryEntry.Key) + " -> " + ToString(dictionaryEntry.Value) + ")";
		}
		string text = obj.ToString();
		if (obj is IList list)
		{
			text += $"[{list.Count}]";
		}
		else if (obj is IDictionary dictionary)
		{
			text += $"[{dictionary.Count}]";
		}
		return text;
	}

	public static string Dump(object obj)
	{
		if (obj == null)
		{
			return "null";
		}
		if (obj is Value value)
		{
			if (!value.is_object)
			{
				return value.ToString();
			}
			obj = value.obj_val;
		}
		MethodInfo method = obj.GetType().GetMethod("Dump", new Type[0]);
		if (method != null)
		{
			try
			{
				return method.Invoke(obj, new object[0]) as string;
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
		}
		if (obj is IList list)
		{
			try
			{
				int count = list.Count;
				string text = "Count: " + count;
				for (int i = 0; i < count; i++)
				{
					text += "\n";
					if (i >= 1000)
					{
						text += "...";
						break;
					}
					object obj2 = list[i];
					text = text + i + ": " + ToString(obj2);
				}
				return text;
			}
			catch (Exception ex2)
			{
				return ex2.Message;
			}
		}
		if (obj is IDictionary dictionary)
		{
			try
			{
				int num = dictionary.Count;
				string text2 = "Count: " + num;
				if (num > 1000)
				{
					num = 1000;
				}
				foreach (DictionaryEntry item in dictionary)
				{
					object key = item.Key;
					object value2 = item.Value;
					text2 += "\n";
					if (num-- <= 0)
					{
						text2 += "...";
						break;
					}
					text2 = text2 + ToString(key) + ": " + ToString(value2);
				}
				return text2;
			}
			catch (Exception ex3)
			{
				return ex3.Message;
			}
		}
		if (obj is IEnumerable enumerable)
		{
			try
			{
				string text3 = "";
				int num2 = 0;
				IEnumerator enumerator = enumerable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					object current = enumerator.Current;
					if (num2 < 1000)
					{
						text3 = text3 + "\n" + ToString(current);
					}
					num2++;
				}
				if (num2 > 1000)
				{
					text3 += "\n...";
				}
				return $"Count: {num2}{text3}";
			}
			catch (Exception ex4)
			{
				return ex4.Message;
			}
		}
		try
		{
			return obj.ToString();
		}
		catch (Exception ex5)
		{
			return ex5.Message;
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		if (components == null || components.Count <= 0)
		{
			return;
		}
		string section = dump.OpenSection("components");
		foreach (Component component in components)
		{
			string dumpStateKey = component.GetDumpStateKey();
			if (dumpStateKey != null)
			{
				Value dumpStateValue = component.GetDumpStateValue();
				string section2 = dump.OpenSection(dumpStateKey, dumpStateValue);
				component.DumpInnerState(dump, verbosity);
				dump.CloseSection(section2);
			}
		}
		dump.CloseSection(section);
	}

	public virtual string TypeToStr()
	{
		string text = GetType().ToString();
		if (text.StartsWith("Logic.", StringComparison.Ordinal))
		{
			text = text.Substring(6);
		}
		return text.Replace('+', '.');
	}

	public override string ToString()
	{
		string text = "[" + NID.ToString(nid) + "] " + TypeToStr(GetType());
		if (obj_state != ObjState.Started)
		{
			text = "(" + obj_state.ToString() + ") " + text;
		}
		return text;
	}

	public override string GetDumpStateKey()
	{
		if (nid == 0)
		{
			return rtti.name;
		}
		return rtti.name + " " + NID.ToString(nid);
	}

	public virtual Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Value result = DefaultGetVar(key, vars, as_value);
		if (!result.is_unknown)
		{
			return result;
		}
		if (set_vars != null)
		{
			result = set_vars.GetVar(key, vars, as_value);
			if (!result.is_unknown)
			{
				return result;
			}
		}
		return Value.Unknown;
	}

	public virtual void SetVar(string key, Value value)
	{
		if (string.IsNullOrEmpty(key))
		{
			Error($"Empty key passed to SetVar('{key}', {value})");
			return;
		}
		if (!value.IsSerializable())
		{
			Error($"Non-serializable value passed to SetVar('{key}', {value})");
		}
		if (set_vars == null)
		{
			if (value.is_unknown)
			{
				return;
			}
			set_vars = new Vars();
		}
		set_vars.SetVar(key, value);
		NotifyListeners("var_changed", key);
		if (IsAuthority())
		{
			SendEvent(new SetVarEvent(key, value));
		}
	}

	public void Log(string msg)
	{
		if (game == null)
		{
			Game.Log(msg, Game.LogType.Message);
		}
		else
		{
			game.Log(this, msg, Game.LogType.Message);
		}
	}

	public void Warning(string msg)
	{
		if (game == null)
		{
			Game.Log(msg, Game.LogType.Warning);
		}
		else
		{
			game.Log(this, msg, Game.LogType.Warning);
		}
	}

	public void Error(string msg)
	{
		if (game == null)
		{
			Game.Log(msg, Game.LogType.Error);
		}
		else
		{
			game.Log(this, msg, Game.LogType.Error);
		}
	}

	public Object(Game game)
	{
		this.game = game;
		obj_state = ObjState.Created;
		Init();
		game?.starts.Add(this);
		SetFlag(16u);
	}

	public void Init()
	{
		if (game == null)
		{
			return;
		}
		game.AddObject(this);
		try
		{
			OnInit();
		}
		catch (Exception ex)
		{
			Error("Error while initializing object " + ToString() + " " + ex.ToString());
		}
	}

	public virtual void OnInit()
	{
	}

	public void Start()
	{
		if (obj_state != ObjState.Created && obj_state != ObjState.MultiplayerCreated)
		{
			Error("Start() called on a " + obj_state.ToString() + " object");
		}
		if (obj_state == ObjState.Created)
		{
			game.starts.Remove(this);
		}
		if (obj_state != ObjState.MultiplayerCreated && game.multiplayer != null)
		{
			game.multiplayer.AddObj(this);
			if (!GetFlag(16u))
			{
				game.multiplayer.SendStartObj(this);
			}
		}
		obj_state = ObjState.Starting;
		try
		{
			OnStart();
			StartComponents();
		}
		catch (Exception ex)
		{
			Error("Exception in OnStart(): " + ex);
		}
		if (obj_state != ObjState.Destroyed)
		{
			if (obj_state != ObjState.Started)
			{
				Error(obj_state.ToString() + " after Start(), did you forget to call base.OnStart()?");
			}
			if (visuals == null && game != null)
			{
				game.NotifyListeners("create_visuals", this, may_trigger: false);
			}
			NotifyListeners("started");
		}
	}

	protected virtual void OnStart()
	{
		obj_state = ObjState.Started;
	}

	public void Finish()
	{
		if (!CanBeDelayDestroyed())
		{
			Error("Finish() called on" + ToString() + " a " + rtti.ToString() + " object with no delayed destroy logic - ShouldDestroyInstantly, and OnFinish should be overriden.");
			return;
		}
		if (!IsValid())
		{
			Error("Finish() called on a " + obj_state.ToString() + " object");
			return;
		}
		if (game != null)
		{
			if (obj_state == ObjState.Created)
			{
				game.starts.Remove(this);
			}
			if (nid != 0 && game.multiplayer != null)
			{
				game.multiplayer.FinishObj(this);
			}
		}
		try
		{
			NotifyListeners("finishing");
		}
		catch (Exception ex)
		{
			Error("NotifiVisuals('finishing'): " + ex);
		}
		ObjRules.Get(this, create: false)?.DelAllRules();
		obj_state = ObjState.Finished;
		try
		{
			DestroyComponents();
		}
		catch (Exception ex2)
		{
			Error("DestroyComponents: " + ex2);
		}
		try
		{
			OnFinish();
			if (GetComponent<DelayedDestroyTimer>() == null)
			{
				Error("no DelayedDestroyTimer after Finish(), did you forget to call base.OnFinish()?");
				DelayedDestroyTimer.Start(this);
			}
		}
		catch (Exception ex3)
		{
			Error("OnFinish: " + ex3);
			obj_state = ObjState.Finished;
		}
		StopUpdating();
		if (listeners != null && !GetFlag(8u))
		{
			listeners.Clear();
		}
	}

	protected virtual bool CanBeDelayDestroyed()
	{
		return false;
	}

	public void Destroy(bool forceDestroyInstantly = false)
	{
		if (!IsValid() && !finished)
		{
			Error("Destroy() called on a " + obj_state.ToString() + " object");
			return;
		}
		if (!forceDestroyInstantly && CanBeDelayDestroyed())
		{
			Finish();
			return;
		}
		if (game != null)
		{
			if (obj_state == ObjState.Created)
			{
				game.starts.Remove(this);
			}
			if (nid != 0 && game.multiplayer != null)
			{
				game.multiplayer.DelObj(this);
			}
		}
		obj_state = ObjState.Destroying;
		try
		{
			NotifyListeners("destroying");
		}
		catch (Exception ex)
		{
			Error("NotifiVisuals('destroying'): " + ex);
		}
		try
		{
			DestroyComponents();
		}
		catch (Exception ex2)
		{
			Error("DestroyComponents: " + ex2);
		}
		try
		{
			OnDestroy();
			if (obj_state != ObjState.Destroyed)
			{
				Error(obj_state.ToString() + " after Destroy(), did you forget to call base.OnDestroy()?");
				obj_state = ObjState.Destroyed;
			}
		}
		catch (Exception ex3)
		{
			Error("OnDestroy: " + ex3);
			obj_state = ObjState.Destroyed;
		}
		StopUpdating();
		if (game != null && game != this)
		{
			if (game.cur_obj == this)
			{
				game.cur_obj = next_in_game;
			}
			game.RemoveObject(this);
		}
		if (listeners != null && !GetFlag(8u))
		{
			listeners.Clear();
		}
		visuals = null;
	}

	protected virtual void OnFinish()
	{
		if (IsValid() || finished)
		{
			DelayedDestroyTimer.Start(this);
		}
	}

	protected virtual void OnDestroy()
	{
		try
		{
			OnFinish();
		}
		catch (Exception ex)
		{
			Error(ex.ToString());
		}
		obj_state = ObjState.Destroyed;
	}

	public virtual void OnUnloadMap()
	{
		Destroy(forceDestroyInstantly: true);
	}

	public virtual void OnDefsReloaded()
	{
	}

	public void UpdateNextFrame()
	{
		if (!IsValid())
		{
			Error("UpdateNextFrame() called on " + obj_state.ToString() + " object");
		}
		else
		{
			game.scheduler.RegisterForNextFrame(this);
		}
	}

	public void UpdateAfter(float seconds, bool exact = false)
	{
		if (!IsValid())
		{
			Error("UpdateAfter() called on " + obj_state.ToString() + " object");
		}
		else
		{
			game.scheduler.RegisterAfterSeconds(this, seconds, exact);
		}
	}

	public void UpdateInBatch(Scheduler.UpdateBatch batch)
	{
		if (!IsValid())
		{
			Error("UpdateInBatch() called on " + obj_state.ToString() + " object");
		}
		else
		{
			game.scheduler.RegisterInBatch(this, batch);
		}
	}

	public void StopUpdating()
	{
		game?.scheduler?.Unregister(this);
	}

	public virtual Stats GetStats()
	{
		return null;
	}

	public void RemoveComponent(Component c)
	{
		if (components != null && c != null)
		{
			components.Remove(c);
		}
	}

	public void AddComponent(Component c)
	{
		if (!IsValid())
		{
			if (!finished)
			{
				Error($"Attempting to add component '{c}' to {obj_state} object");
			}
			return;
		}
		if (components == null)
		{
			components = new List<Component>();
		}
		components.Add(c);
		if (started)
		{
			c.OnStart();
		}
	}

	public T GetComponent<T>() where T : Component
	{
		if (components == null)
		{
			return null;
		}
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] is T result)
			{
				return result;
			}
		}
		return null;
	}

	private void StartComponents()
	{
		if (components != null)
		{
			for (int i = 0; i < components.Count; i++)
			{
				components[i].OnStart();
			}
		}
	}

	private void DestroyComponents()
	{
		if (components != null)
		{
			for (int i = 0; i < components.Count; i++)
			{
				Component component = components[i];
				component.OnDestroy();
				component.StopUpdating();
			}
			components = null;
		}
	}

	public virtual void ClearAllComponents()
	{
		DestroyComponents();
	}

	public virtual void OnTimer(Timer timer)
	{
		if (timer.def == null)
		{
			Warning("Unhandled " + timer.ToString());
		}
	}

	public virtual Kingdom GetKingdom()
	{
		return null;
	}

	public virtual IRelationCheck GetStanceObj()
	{
		return GetKingdom();
	}

	public virtual RelationUtils.Stance GetStance(IRelationCheck i)
	{
		return this.DefaultGetStance(i);
	}

	public virtual RelationUtils.Stance GetStance(Kingdom k)
	{
		return this.DefaultGetStance(k);
	}

	public virtual RelationUtils.Stance GetStance(Rebellion r)
	{
		return this.DefaultGetStance(r);
	}

	public virtual RelationUtils.Stance GetStance(Settlement s)
	{
		return this.DefaultGetStance(s);
	}

	public virtual RelationUtils.Stance GetStance(Crusade c)
	{
		return this.DefaultGetStance(c);
	}

	public virtual RelationUtils.Stance GetWarStance(Kingdom k)
	{
		return this.DefaultGetWarStance(k);
	}

	public bool IsEnemy(int kid)
	{
		return IsEnemy(game.GetKingdom(kid));
	}

	public bool IsEnemy(IRelationCheck obj)
	{
		return this.DefaultIsEnemy(obj);
	}

	public bool IsNeutral(IRelationCheck obj)
	{
		return this.DefaultIsNeutral(obj);
	}

	public bool IsAlly(IRelationCheck obj)
	{
		return this.DefaultIsAlly(obj);
	}

	public bool IsAllyOrVassal(IRelationCheck obj)
	{
		return this.DefaultIsAllyOrVassal(obj);
	}

	public bool IsAllyOrOwn(IRelationCheck obj)
	{
		return this.DefaultIsAllyOrOwn(obj);
	}

	public bool IsOwnStance(IRelationCheck obj)
	{
		return this.DefaultIsOwnStance(obj);
	}

	public bool HasStance(IRelationCheck obj, RelationUtils.Stance stance)
	{
		return this.DefaultHasStance(obj, stance);
	}

	public bool HasStanceAll(IRelationCheck obj, RelationUtils.Stance stance)
	{
		return this.DefaultHasStanceAll(obj, stance);
	}

	public Value DefaultGetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"obj" => this, 
			"nid" => nid, 
			"NID" => new Value((NID)this), 
			"uid" => uid, 
			"set_vars" => new Value(set_vars), 
			"kingdom" => GetKingdom(), 
			"rules" => GetComponent<ObjRules>(), 
			"statuses" => GetComponent<Statuses>(), 
			"actions" => GetComponent<Actions>(), 
			"listeners" => new Value(listeners), 
			"is_valid" => IsValid(), 
			_ => Value.Unknown, 
		};
	}

	public bool IsAuthority()
	{
		if (game == null)
		{
			Error($"{this}: Game is null!");
			return false;
		}
		return game.IsAuthority();
	}

	public bool AssertAuthority()
	{
		if (!IsAuthority())
		{
			Warning("Non-authority!");
			return false;
		}
		return true;
	}

	public void FireEvent(string id, object param = null, params int[] send_to_kingdoms)
	{
		if (game != null && game.dt != null)
		{
			Event obj = new Event(this, id, param);
			if (send_to_kingdoms.Length != 0)
			{
				obj.send_to_kingdoms = new List<int>(send_to_kingdoms);
			}
			FireEvent(obj);
		}
	}

	public void FireEvent(Event evt)
	{
		if (!evt.IsSerializable())
		{
			Game.Log("Non-serializable event: " + evt.ToString(), Game.LogType.Error);
		}
		if (!IsAuthority())
		{
			return;
		}
		Multiplayer.PlayerData playerData = game?.multiplayer?.playerData;
		Kingdom kingdom = game.GetKingdom(playerData?.kingdomId ?? 0);
		List<Multiplayer.PlayerData> all = Multiplayer.CurrentPlayers.GetAll();
		if (all != null)
		{
			List<int> list = null;
			for (int i = 0; i < all.Count; i++)
			{
				Multiplayer.PlayerData playerData2 = all[i];
				if (playerData2 == playerData)
				{
					continue;
				}
				Kingdom kingdom2 = game.GetKingdom(playerData2.kingdomId);
				if (evt.ShouldSendTo(kingdom2))
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(playerData2.pid);
				}
			}
			if (list != null)
			{
				Serialization.ObjectEvent objEvent = new GameEventMessage(evt);
				game.multiplayer.SendObjects();
				for (int j = 0; j < list.Count; j++)
				{
					int pid = list[j];
					game.multiplayer.SendObjEventToPlayer(this, objEvent, pid);
				}
			}
		}
		if (evt.ShouldSendTo(kingdom))
		{
			evt.SetPlayer(kingdom);
			evt.obj.OnEvent(evt);
		}
	}

	public Status GetStatus()
	{
		UpdateAutomaticStatuses(now: true);
		return statuses?.main;
	}

	public Status GetStatus(int idx)
	{
		return statuses?.Get(idx);
	}

	public Status FindStatus(Status.Def def)
	{
		return statuses?.Find(def);
	}

	public Status FindStatus(Type type)
	{
		return statuses?.Find(type);
	}

	public T FindStatus<T>() where T : Status
	{
		Statuses obj = statuses;
		if (obj == null)
		{
			return null;
		}
		return obj.Find<T>();
	}

	public void ClearStatus()
	{
		SetStatus((Status)null, send_state: true);
	}

	public void ClearStatus<T>() where T : Status
	{
		Status status = GetStatus();
		if (status != null && status.rtti.type == typeof(T))
		{
			ClearStatus();
		}
	}

	public void SetStatus(Status status, bool send_state = true)
	{
		Statuses statuses = this.statuses;
		if (statuses == null)
		{
			if (status == null)
			{
				return;
			}
			statuses = new Statuses(this);
		}
		statuses.SetMain(status, send_state);
	}

	public Status SetStatus(string id)
	{
		Status.Def def = game.defs.Find<Status.Def>(id);
		if (def == null)
		{
			Error("Unknown status id: " + id);
			return null;
		}
		return SetStatus(def);
	}

	public Status SetStatus(Status.Def def, bool send_state = true)
	{
		if (def == null)
		{
			Error("Attempting to set status with no def");
			return null;
		}
		Status status = GetStatus();
		if (status != null && status.def == def)
		{
			return status;
		}
		Status status2 = Status.Create(def);
		SetStatus(status2, send_state);
		return status2;
	}

	public Status SetStatus(Type type)
	{
		Status status = GetStatus();
		if (status != null && status.rtti.type == type)
		{
			return status;
		}
		Status status2 = Status.Create(game, type);
		SetStatus(status2);
		return status2;
	}

	public T SetStatus<T>() where T : Status
	{
		return SetStatus(typeof(T)) as T;
	}

	public void ClearDefaultStatus(bool apply = true)
	{
		Statuses statuses = this.statuses;
		if (statuses == null)
		{
			return;
		}
		Type default_type = statuses.default_type;
		if (default_type == null)
		{
			UpdateAutomaticStatuses();
			return;
		}
		statuses.SetDefaultType(null);
		if (apply && statuses.main != null && statuses.main.rtti.type == default_type)
		{
			statuses.SetMain(null);
		}
		UpdateAutomaticStatuses();
	}

	public void ClearDefaultStatus<T>(bool apply = true) where T : Status
	{
		Statuses statuses = this.statuses;
		if (statuses != null)
		{
			Type typeFromHandle = typeof(T);
			if (statuses.default_type == typeFromHandle)
			{
				statuses.SetDefaultType(null);
			}
			if (apply && statuses.main != null && statuses.main.rtti.type == typeFromHandle)
			{
				statuses.SetMain(null);
			}
		}
	}

	public T SetDefaultStatus<T>(bool apply = true) where T : Status
	{
		Statuses statuses = this.statuses;
		if (statuses == null)
		{
			statuses = new Statuses(this);
		}
		Type typeFromHandle = typeof(T);
		statuses.SetDefaultType(typeFromHandle);
		if (!apply)
		{
			return null;
		}
		return SetStatus(typeFromHandle) as T;
	}

	public void AddStatus(Status status)
	{
		if (status != null)
		{
			Statuses statuses = this.statuses;
			if (statuses == null)
			{
				statuses = new Statuses(this);
			}
			statuses.Add(status);
		}
	}

	public Status AddStatus(string id, bool allow_multiple = false)
	{
		Status.Def def = game.defs.Find<Status.Def>(id);
		if (def == null)
		{
			Error("Unknown status id: " + id);
			return null;
		}
		return AddStatus(def, allow_multiple);
	}

	public Status AddStatus(Status.Def def, bool allow_multiple = false)
	{
		if (def == null)
		{
			Error("Attempting to add status with no def");
			return null;
		}
		if (!allow_multiple)
		{
			Status status = FindStatus(def);
			if (status != null)
			{
				return status;
			}
		}
		Status status2 = Status.Create(def);
		AddStatus(status2);
		return status2;
	}

	public Status AddStatus(Type type, bool allow_multiple = false)
	{
		if (!allow_multiple)
		{
			Status status = FindStatus(type);
			if (status != null)
			{
				return status;
			}
		}
		Status status2 = Status.Create(game, type);
		AddStatus(status2);
		return status2;
	}

	public T AddStatus<T>(bool allow_multiple = false) where T : Status
	{
		return AddStatus(typeof(T), allow_multiple) as T;
	}

	public void DelStatus(Status status, bool destroy = true)
	{
		statuses?.Del(status, send_state: true, destroy);
	}

	public void DelStatus<T>() where T : Status
	{
		Statuses statuses = this.statuses;
		if (statuses != null)
		{
			int idx = statuses.FindIndex<T>();
			statuses.Del(idx);
		}
	}

	public void DelStatus(string id)
	{
		Statuses statuses = this.statuses;
		if (statuses == null)
		{
			return;
		}
		for (int num = statuses.Count - 1; num >= 0; num--)
		{
			if (statuses[num] != null && statuses[num].def.id == id)
			{
				DelStatus(statuses[num]);
			}
		}
	}

	public virtual void UpdateAutomaticStatuses(bool now = false, bool force_recalc = false)
	{
	}
}
