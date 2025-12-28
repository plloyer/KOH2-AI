using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Logic;

public class Scheduler
{
	public class UpdateQueue
	{
		public enum Type
		{
			PerFrame,
			AtMS,
			Batch,
			CurPF,
			CurMS
		}

		public Type type;

		public long timestamp;

		public UpdateBatch pBatch;

		private int count;

		private Updateable first;

		private Updateable last;

		public bool Empty => first == null;

		public int Count => count;

		public Updateable First => first;

		public UpdateQueue(Type type, long timestamp = 0L)
		{
			this.type = type;
			this.timestamp = timestamp;
		}

		public UpdateQueue(UpdateBatch pBatch)
		{
			this.pBatch = pBatch;
		}

		public void Drain(UpdateQueue lst)
		{
			Assert(first == null, "Queue not empty");
			Updateable pNextUpdateable = lst.first;
			while (pNextUpdateable != null)
			{
				Updateable obj = pNextUpdateable;
				pNextUpdateable = pNextUpdateable.pNextUpdateable;
				lst.Del(obj);
				Push(obj);
			}
		}

		public void Drain(UpdateQueue lst, long timestamp)
		{
			Assert(first == null, "Queue not empty");
			Updateable pNextUpdateable = lst.first;
			while (pNextUpdateable != null)
			{
				if (pNextUpdateable.tmNextUpdate.milliseconds != timestamp)
				{
					pNextUpdateable = pNextUpdateable.pNextUpdateable;
					continue;
				}
				Updateable obj = pNextUpdateable;
				pNextUpdateable = pNextUpdateable.pNextUpdateable;
				lst.Del(obj);
				Push(obj);
			}
		}

		public void Push(Updateable obj)
		{
			Assert(obj.pUpdateQueue == null && obj.pPrevUpdateable == null && obj.pNextUpdateable == null, "Object already in queue");
			count++;
			obj.pUpdateQueue = this;
			obj.pPrevUpdateable = last;
			obj.pNextUpdateable = null;
			if (last == null)
			{
				first = obj;
			}
			else
			{
				last.pNextUpdateable = obj;
			}
			last = obj;
		}

		public Updateable Pop()
		{
			Updateable updateable = first;
			if (updateable == null)
			{
				return null;
			}
			if (count-- == 0)
			{
				Error("Update queue underflow");
			}
			updateable.pUpdateQueue = null;
			first = updateable.pNextUpdateable;
			updateable.pNextUpdateable = null;
			if (first == null)
			{
				last = null;
			}
			else
			{
				first.pPrevUpdateable = null;
			}
			return updateable;
		}

		public void Del(Updateable obj)
		{
			if (obj.pUpdateQueue != this)
			{
				Error("Object not in queue");
				return;
			}
			if (count-- == 0)
			{
				Error("Update queue underflow");
			}
			obj.pUpdateQueue = null;
			if (obj.pPrevUpdateable == null)
			{
				first = obj.pNextUpdateable;
			}
			else
			{
				obj.pPrevUpdateable.pNextUpdateable = obj.pNextUpdateable;
			}
			if (obj.pNextUpdateable == null)
			{
				last = obj.pPrevUpdateable;
			}
			else
			{
				obj.pNextUpdateable.pPrevUpdateable = obj.pPrevUpdateable;
			}
			obj.pPrevUpdateable = (obj.pNextUpdateable = null);
		}

		public override string ToString()
		{
			string text = type.ToString();
			text = ((type != Type.Batch) ? (text + "(" + timestamp + ")") : (text + "(" + pBatch.ToString() + ")"));
			text = text + "[" + count + "]";
			if (count > 0)
			{
				text = text + ": (" + first.ToString() + ") .. (" + last.ToString() + ")";
			}
			return text;
		}
	}

	public class UpdateBatch : Updateable
	{
		private Scheduler scheduler;

		private string name;

		private long iCycleLength;

		private long iCycle;

		private Time tmCycleStart = Time.Zero;

		private Time tmCycleEnd = Time.Zero;

		private int iProcessed;

		private int iRemaining;

		private UpdateQueue lst;

		private Updateable pNext;

		public int Count => lst.Count;

		public UpdateBatch(Scheduler scheduler, string name, float cycle)
		{
			this.scheduler = scheduler;
			this.name = name;
			lst = new UpdateQueue(this);
			iCycleLength = (int)(cycle * 1000f);
			NewCycle();
			scheduler.RegisterForNextFrame(this);
		}

		public void Add(Updateable obj)
		{
			Assert(obj.pUpdateQueue == null && obj.tmNextUpdate == Time.Zero, "Object already registered for update");
			iRemaining++;
			lst.Push(obj);
			if (pNext == null)
			{
				pNext = obj;
			}
		}

		public void Del(Updateable obj)
		{
			Assert(obj.pUpdateQueue == lst, "Wrong update batch");
			if (obj == pNext)
			{
				pNext = obj.pNextUpdateable;
			}
			lst.Del(obj);
			if (obj.tmNextUpdate.milliseconds != iCycle && iRemaining-- == 0)
			{
				Error("Update batch underflow");
			}
			obj.pUpdateQueue = null;
			obj.tmNextUpdate = Time.Zero;
		}

		private void NewCycle()
		{
			iCycle++;
			tmCycleStart = scheduler.Time;
			tmCycleEnd.milliseconds = tmCycleStart.milliseconds + iCycleLength;
			iProcessed = 0;
			iRemaining = lst.Count;
			pNext = lst.First;
		}

		public override void OnUpdate()
		{
			scheduler.RegisterForNextFrame(this);
			long num = scheduler.Time.milliseconds - tmCycleStart.milliseconds;
			if (num > iCycleLength)
			{
				num = iCycleLength;
			}
			while (iProcessed * iCycleLength < num * (iProcessed + iRemaining) && pNext != null)
			{
				Updateable updateable = pNext;
				pNext = updateable.pNextUpdateable;
				iProcessed++;
				if (iRemaining-- == 0)
				{
					Error("Update batch underflow in update");
				}
				Assert(updateable.tmNextUpdate.milliseconds != iCycle, "Object already processed in batch");
				Time tm = updateable.tmNextUpdate;
				updateable.tmNextUpdate.milliseconds = iCycle;
				scheduler.iChecked++;
				scheduler.Update(updateable, tm);
			}
			if (num >= iCycleLength)
			{
				NewCycle();
			}
		}

		public override string ToString()
		{
			return name + ": (" + iProcessed + " + " + iRemaining + ") / " + Count;
		}
	}

	public Game game;

	public bool profile = true;

	private Time tmCur = Time.Zero;

	private long iCurFrame;

	private Time tmFrameStart = Time.Zero;

	private Time tmFrameEnd = Time.Zero;

	private float fFrameTime;

	private bool bInUpdate;

	public int iUpdated;

	public int iChecked;

	public int iTotal;

	private UpdateQueue lstPerFrame;

	private UpdateQueue lstCurPerFrame;

	private UpdateQueue[] lstPerMS = new UpdateQueue[1000];

	private UpdateQueue lstCurMS;

	private int iCurMS;

	public Dictionary<string, int> updates_per_type;

	public Time Time => tmCur;

	public long Frame => iCurFrame;

	public Time FrameStart => tmFrameStart;

	public Time FrameEnd => tmFrameEnd;

	public float FrameTime => fFrameTime;

	public bool InUpdate => bInUpdate;

	public bool Profile(int profile)
	{
		return this.profile = profile != 0;
	}

	public void RegisterForNextFrame(Updateable obj)
	{
		Unregister(obj);
		iTotal++;
		AddNextFrame(obj);
	}

	public void RegisterAfterSeconds(Updateable obj, float after_seconds, bool exact)
	{
		Unregister(obj);
		if (after_seconds <= 0f)
		{
			Error("Object " + obj.ToString() + " registering for update after " + after_seconds);
			AddNextFrame(obj);
		}
		else
		{
			iTotal++;
			AddAtMS(obj, (exact ? tmCur : tmFrameEnd) + after_seconds);
		}
	}

	public void RegisterInBatch(Updateable obj, UpdateBatch batch)
	{
		if (batch != obj.GetBatch())
		{
			Unregister(obj);
			iTotal++;
			batch.Add(obj);
		}
	}

	public void Unregister(Updateable obj)
	{
		UpdateQueue pUpdateQueue = obj.pUpdateQueue;
		if (pUpdateQueue != null)
		{
			if (iTotal-- == 0)
			{
				Error("Total Count underflow");
			}
			if (pUpdateQueue.pBatch != null)
			{
				pUpdateQueue.pBatch.Del(obj);
				return;
			}
			pUpdateQueue.Del(obj);
			obj.tmNextUpdate = Time.Zero;
		}
	}

	public void Update(float elapsed)
	{
		iUpdated = (iChecked = 0);
		if (elapsed <= 0f)
		{
			if (elapsed < 0f)
			{
				Error("Scheduler.Update called with " + elapsed + " seconds");
			}
			return;
		}
		if (bInUpdate)
		{
			Error("Schedule.Update reentrant call!");
			return;
		}
		bInUpdate = true;
		try
		{
			tmFrameStart = tmCur;
			tmFrameEnd = tmFrameStart + elapsed;
			fFrameTime = elapsed;
			iChecked += lstPerFrame.Count;
			lstCurPerFrame.Drain(lstPerFrame);
			lstCurPerFrame.timestamp = iCurFrame;
			iCurFrame++;
			lstPerFrame.timestamp = iCurFrame;
			while (tmCur < tmFrameEnd)
			{
				UpdateQueue updateQueue = lstPerMS[iCurMS];
				if (!updateQueue.Empty)
				{
					iChecked += updateQueue.Count;
					lstCurMS.Drain(updateQueue, tmCur.milliseconds);
					lstCurMS.timestamp = iCurMS;
					Update(lstCurMS);
				}
				tmCur.milliseconds++;
				iCurMS = (iCurMS + 1) % 1000;
			}
			Update(lstCurPerFrame);
		}
		catch (Exception ex)
		{
			Game.Log("Exception during update: " + ex.ToString(), Game.LogType.Error);
		}
		bInUpdate = false;
	}

	private static void Error(string msg)
	{
		msg = "Scheduler Error: " + msg;
		Console.WriteLine(msg);
		Game.Log(msg, Game.LogType.Error);
	}

	private static bool Assert(bool cond, string msg)
	{
		if (!cond)
		{
			Error(msg);
		}
		return cond;
	}

	public Scheduler(Game game)
	{
		this.game = game;
		lstPerFrame = new UpdateQueue(UpdateQueue.Type.PerFrame, 0L);
		lstCurPerFrame = new UpdateQueue(UpdateQueue.Type.CurPF, 0L);
		lstCurMS = new UpdateQueue(UpdateQueue.Type.CurMS, 0L);
		for (int i = 0; i < 1000; i++)
		{
			lstPerMS[i] = new UpdateQueue(UpdateQueue.Type.AtMS, i);
		}
		Update(1f);
	}

	private void AddNextFrame(Updateable obj)
	{
		obj.tmNextUpdate.milliseconds = -(iCurFrame + 1);
		lstPerFrame.Push(obj);
	}

	private void AddAtMS(Updateable obj, Time tm)
	{
		try
		{
			Assert(tm.milliseconds > 0, "Absolute time is non-positive");
			obj.tmNextUpdate = tm;
			int num = (int)(tm.milliseconds % 1000);
			lstPerMS[num].Push(obj);
		}
		catch
		{
		}
	}

	private void Update(UpdateQueue lst)
	{
		while (true)
		{
			Updateable updateable = lst.Pop();
			if (updateable != null)
			{
				if (iTotal-- == 0)
				{
					Error("Total Count underflow");
				}
				Time tmNextUpdate = updateable.tmNextUpdate;
				updateable.tmNextUpdate = Time.Zero;
				Update(updateable, tmNextUpdate);
				continue;
			}
			break;
		}
	}

	public void Update(Updateable obj, Time tm)
	{
		iUpdated++;
		string text = null;
		try
		{
			if (updates_per_type != null)
			{
				updates_per_type.TryGetValue(obj.rtti.full_name, out var value);
				value++;
				updates_per_type[obj.rtti.full_name] = value;
			}
			if (profile)
			{
				text = obj.rtti.update_section_name;
				Game.BeginProfileSection(text);
			}
			obj.OnUpdate();
		}
		catch (Exception ex)
		{
			Game.Log("Error OnUpdate(" + obj.ToString() + "): " + ex.ToString(), Game.LogType.Error);
		}
		if (text != null)
		{
			Game.EndProfileSection(text);
		}
	}

	public void StartUpdatesPerTypeStats()
	{
		if (updates_per_type == null)
		{
			updates_per_type = new Dictionary<string, int>();
		}
		else
		{
			updates_per_type.Clear();
		}
	}

	public void DumpUpdatesPerTypeStats()
	{
		if (updates_per_type == null)
		{
			return;
		}
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(updates_per_type.Count);
		foreach (KeyValuePair<string, int> item in updates_per_type)
		{
			list.Add(item);
		}
		list.Sort((KeyValuePair<string, int> a, KeyValuePair<string, int> b) => a.Key.CompareTo(b.Key));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("type,count");
		foreach (KeyValuePair<string, int> item2 in list)
		{
			stringBuilder.AppendLine($"{item2.Key},{item2.Value}");
		}
		File.WriteAllText(System.IO.Path.Combine(Game.GetSavesRootDir(Game.SavesRoot.Root), "updates_per_type.csv"), stringBuilder.ToString());
	}
}
