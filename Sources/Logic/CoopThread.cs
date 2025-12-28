using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Logic;

public class CoopThread
{
	private IEnumerator coro;

	public int trace_verbosity = -1;

	private CoopThread caller;

	private CoopThread callee;

	private Value call_result;

	private CoopThread prev;

	private CoopThread next;

	private int calls;

	private int slices;

	private int yields;

	private long total_ticks;

	private long self_ticks;

	private long self_overhead_ticks;

	private long total_overhead_ticks;

	private int did_not_yield_for_a_long_time_warrnings;

	private long slice_ticks;

	private long slice_overhead_ticks;

	private bool new_slice;

	public static readonly object Yield = new object();

	private static CoopThread first;

	private static CoopThread last;

	private static Stopwatch timer = Stopwatch.StartNew();

	private static long log_ticks;

	private static long frame_start_ticks;

	private static long tick_start_ticks;

	private static long overhead_start_ticks;

	public static long frame_ticks;

	public static long frame_overhead_ticks;

	public static int frame_count;

	public static int frame_active_threads;

	public static int frame_yields;

	private Dictionary<string, CoopThread> pool;

	private bool pooled;

	public static Action<string, Game.LogType> log_func = Game.Log;

	public string name { get; private set; }

	private long slice_total_ticks => slice_ticks + slice_overhead_ticks;

	public static CoopThread current { get; private set; }

	public bool valid => coro != null;

	public static int Count
	{
		get
		{
			int num = 0;
			for (CoopThread coopThread = first; coopThread != null; coopThread = coopThread.next)
			{
				num++;
			}
			return num;
		}
	}

	public CoopThread root
	{
		get
		{
			CoopThread coopThread = this;
			while (coopThread.caller != null)
			{
				coopThread = coopThread.caller;
			}
			return coopThread;
		}
	}

	public static int current_trace_verbosity
	{
		get
		{
			if (current != null)
			{
				return current.CalcTraceVerbosity();
			}
			return -1;
		}
	}

	public static long TickCount => timer.ElapsedTicks - log_ticks;

	public static float SliceMicros
	{
		get
		{
			if (current != null)
			{
				return TicksToMicros(current.root.slice_total_ticks + TickCount - tick_start_ticks);
			}
			return 0f;
		}
	}

	public static float FrameMicros => TicksToMicros((current == null) ? frame_ticks : (TickCount - frame_start_ticks));

	public static Value Result
	{
		get
		{
			if (current != null)
			{
				return current.call_result;
			}
			return Value.Unknown;
		}
	}

	public static bool IsValid(CoopThread ct)
	{
		return ct?.coro != null;
	}

	public static float TicksToMicros(long ticks)
	{
		return (float)(ticks * 1000000) / (float)Stopwatch.Frequency;
	}

	public static CoopThread Start(string name, IEnumerator coro, int trace_verbosity = 0)
	{
		if (current != null)
		{
			Warning("Started new thread (" + name + ") from another thread, did you mean to Call() it instead?", current);
		}
		if (coro == null && trace_verbosity < 2)
		{
			return null;
		}
		CoopThread coopThread = new CoopThread
		{
			name = name,
			coro = coro,
			trace_verbosity = trace_verbosity,
			calls = 1
		};
		Trace("Started", 2, coopThread);
		if (coro == null)
		{
			return null;
		}
		if (current != null)
		{
			AddAsFirst(coopThread);
		}
		else
		{
			AddAsLast(coopThread);
		}
		return coopThread;
	}

	public static CoopThread Call(string name, IEnumerator coro, int trace_verbosity = -1)
	{
		if (current == null)
		{
			Error("Attempting to call a thread (" + name + ") outside a thread, use Start() instead!");
			return null;
		}
		if (coro == null)
		{
			if (trace_verbosity < 0)
			{
				_ = current_trace_verbosity;
			}
			if (trace_verbosity < 2)
			{
				return null;
			}
		}
		CoopThread coopThread = current.CreateFromPool(name);
		coopThread.coro = coro;
		if (trace_verbosity >= 0)
		{
			coopThread.trace_verbosity = trace_verbosity;
		}
		coopThread.caller = current;
		current.callee = coopThread;
		current.call_result = Value.Unknown;
		coopThread.calls++;
		Trace("Started", 2, coopThread);
		if (coro == null)
		{
			current.callee = null;
			return null;
		}
		if (current != null)
		{
			AddAsFirst(coopThread);
		}
		else
		{
			AddAsLast(coopThread);
		}
		return coopThread;
	}

	private CoopThread CreateFromPool(string name)
	{
		if (pool == null)
		{
			return new CoopThread
			{
				name = name
			};
		}
		if (!pool.TryGetValue(name, out var value))
		{
			return new CoopThread
			{
				name = name
			};
		}
		if (value.coro != null)
		{
			Error("Pooled thread is already taken", value);
		}
		value.call_result = Value.Unknown;
		return value;
	}

	private void ReturnToPool(CoopThread ct)
	{
		if (ct.coro != null)
		{
			Error("Valid thread returned to pool", ct);
		}
		else if (!ct.pooled)
		{
			ct.pooled = true;
			if (pool == null)
			{
				pool = new Dictionary<string, CoopThread>();
			}
			pool.Add(ct.name, ct);
		}
	}

	public void Stop(Value call_result = default(Value))
	{
		if (callee != null)
		{
			callee.Stop();
		}
		else
		{
			RemoveFromList(this);
		}
		this.call_result = call_result;
		if (caller != null)
		{
			caller.call_result = call_result;
		}
		coro = null;
		callee = null;
		Trace("Stopped", 2, this);
	}

	public static object Return(Value call_result = default(Value))
	{
		if (current == null)
		{
			Error("Attempting to Return outside of a thread.");
			return null;
		}
		current.Stop(call_result);
		return null;
	}

	public static void Shutdown()
	{
		first = (last = null);
		frame_ticks = 0L;
		frame_overhead_ticks = 0L;
		frame_active_threads = 0;
		frame_count = 0;
		frame_yields = 0;
	}

	public static bool UpdateAll(float time_quota = 1f)
	{
		long num = (long)(time_quota * (float)Stopwatch.Frequency / 1000f);
		frame_start_ticks = (overhead_start_ticks = TickCount);
		frame_ticks = 0L;
		frame_overhead_ticks = 0L;
		frame_active_threads = 0;
		frame_count++;
		frame_yields = 0;
		if (!UpdateCurrent(new_slice: false, "Activated (new frame)"))
		{
			return false;
		}
		CoopThread coopThread = current.root;
		while (true)
		{
			current.AddOverhead();
			tick_start_ticks = overhead_start_ticks;
			Game.BeginProfileSection(current.name);
			bool flag = !current.Tick();
			Game.EndProfileSection(current.name);
			overhead_start_ticks = TickCount;
			frame_ticks = overhead_start_ticks - frame_start_ticks;
			long num2 = overhead_start_ticks - tick_start_ticks;
			if (num2 >= num && current_trace_verbosity > 0 && current.did_not_yield_for_a_long_time_warrnings++ % 100 == 0)
			{
				Warning($"Did not yield for {TicksToMicros(num2):N0}us", current);
			}
			long num3 = current.AddTicks(num2);
			frame_yields++;
			if (current.coro == null)
			{
				bool flag2;
				string activate_reason;
				if (current.caller != null)
				{
					current.caller.callee = null;
					AddAsFirst(current.caller);
					flag2 = false;
					activate_reason = "Activated (resume from Call)";
					current.caller.ReturnToPool(current);
				}
				else
				{
					if (first.root == coopThread)
					{
						break;
					}
					flag2 = true;
					activate_reason = "Activated (previous thread finished)";
				}
				if (!CheckFrameLimit(num) || !UpdateCurrent(flag2, activate_reason))
				{
					break;
				}
				continue;
			}
			if (current.callee != null)
			{
				RemoveFromList(current);
				current = first;
				flag = false;
			}
			if (flag || num3 >= num)
			{
				current.root.new_slice = true;
				if (first == last)
				{
					break;
				}
				RemoveFromList(current);
				AddAsLast(current);
				if (!CheckFrameLimit(num) || first.root == coopThread || !UpdateCurrent(new_slice: true, "Activated (thread switch)"))
				{
					break;
				}
			}
			else if (!CheckFrameLimit(num))
			{
				break;
			}
		}
		if (current != null)
		{
			current.AddOverhead();
			Trace("Deactivated (end of frame)", 3, current.root);
			current = null;
		}
		frame_ticks = TickCount - frame_start_ticks;
		return first != null;
	}

	private static bool CheckFrameLimit(long max_ticks)
	{
		frame_ticks = TickCount - frame_start_ticks;
		if (frame_ticks >= max_ticks)
		{
			return false;
		}
		return true;
	}

	private static bool UpdateCurrent(bool new_slice, string activate_reason)
	{
		CoopThread coopThread = current?.root;
		CoopThread coopThread2 = first?.root;
		bool flag = coopThread != coopThread2 && coopThread2 != null;
		if (current != null)
		{
			current.AddOverhead();
			if (flag)
			{
				Trace("Deactivated", 3, coopThread);
			}
			else
			{
				Trace("Deactivated", 4, current);
			}
		}
		current = first;
		if (current == null)
		{
			return false;
		}
		coopThread = coopThread2;
		if (flag)
		{
			frame_active_threads++;
		}
		if (new_slice || coopThread.new_slice)
		{
			coopThread.new_slice = false;
			coopThread.slices++;
			coopThread.slice_ticks = 0L;
			coopThread.slice_overhead_ticks = 0L;
		}
		if (flag)
		{
			Trace(activate_reason, 3, coopThread);
		}
		else
		{
			Trace(activate_reason, 4, current);
		}
		return true;
	}

	private long AddTicks(long ticks)
	{
		self_ticks += ticks;
		CoopThread coopThread = this;
		while (true)
		{
			coopThread.total_ticks += ticks;
			if (coopThread.caller == null)
			{
				break;
			}
			coopThread = coopThread.caller;
		}
		coopThread.slice_ticks += ticks;
		return coopThread.slice_total_ticks;
	}

	private void AddOverhead()
	{
		long tickCount = TickCount;
		long num = tickCount - overhead_start_ticks;
		overhead_start_ticks = tickCount;
		self_overhead_ticks += num;
		frame_overhead_ticks += num;
		frame_ticks = overhead_start_ticks - frame_start_ticks;
		CoopThread coopThread = this;
		while (true)
		{
			coopThread.total_overhead_ticks += num;
			if (coopThread.caller == null)
			{
				break;
			}
			coopThread = coopThread.caller;
		}
		coopThread.slice_overhead_ticks += num;
	}

	public bool Tick()
	{
		if (coro == null)
		{
			Error("Attempting to Tick() a stopped thread", this);
			return false;
		}
		if (callee != null)
		{
			Warning("Attempting to Tick() a waiting thread", this);
			return callee.Tick();
		}
		try
		{
			yields++;
			Trace("Tick", 4, this);
			if (!coro.MoveNext())
			{
				if (coro != null)
				{
					Stop();
				}
				return false;
			}
			if (coro == null)
			{
				return false;
			}
			object obj = coro.Current;
			if (obj == null)
			{
				return true;
			}
			if (obj is CoopThread coopThread)
			{
				coopThread.caller = this;
				callee = coopThread;
				return false;
			}
			if (obj == Yield)
			{
				return false;
			}
			Error($"Invalid yield return value: {obj}", this);
			return false;
		}
		catch (Exception ex)
		{
			Error(ex.ToString(), this);
			Stop();
			return false;
		}
	}

	private static void AddAsFirst(CoopThread ct)
	{
		ct.prev = null;
		ct.next = first;
		if (first != null)
		{
			first.prev = ct;
		}
		else
		{
			last = ct;
		}
		first = ct;
	}

	private static void AddAsLast(CoopThread ct)
	{
		ct.next = null;
		ct.prev = last;
		if (last != null)
		{
			last.next = ct;
		}
		else
		{
			first = ct;
		}
		last = ct;
	}

	private static void RemoveFromList(CoopThread ct)
	{
		if (ct.prev != null)
		{
			ct.prev.next = ct.next;
		}
		else
		{
			first = ct.next;
		}
		if (ct.next != null)
		{
			ct.next.prev = ct.prev;
		}
		else
		{
			last = ct.prev;
		}
		ct.prev = (ct.next = null);
	}

	public static CoopThread Find(string name, bool recursive = false, bool partial_name = false)
	{
		for (CoopThread coopThread = first; coopThread != null; coopThread = coopThread.next)
		{
			CoopThread coopThread2 = coopThread.root;
			if (partial_name)
			{
				if (coopThread2.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return coopThread2;
				}
			}
			else if (coopThread2.name == name)
			{
				return coopThread2;
			}
			if (recursive)
			{
				CoopThread coopThread3 = coopThread2.FindChild(name, recursive, partial_name);
				if (coopThread3 != null)
				{
					return coopThread3;
				}
			}
		}
		return null;
	}

	public CoopThread FindChild(string name, bool recursive = false, bool partial_name = false)
	{
		if (pool == null)
		{
			return null;
		}
		if (pool.TryGetValue(name, out var value))
		{
			return value;
		}
		if (!partial_name && !recursive)
		{
			return null;
		}
		foreach (KeyValuePair<string, CoopThread> item in pool)
		{
			value = item.Value;
			if (partial_name)
			{
				if (value.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return value;
				}
			}
			else if (value.name == name)
			{
				return value;
			}
			if (recursive)
			{
				CoopThread coopThread = value.FindChild(name, recursive, partial_name);
				if (coopThread != null)
				{
					return coopThread;
				}
			}
		}
		return null;
	}

	public int CalcTraceVerbosity()
	{
		for (CoopThread coopThread = this; coopThread != null; coopThread = coopThread.caller)
		{
			if (coopThread.trace_verbosity >= 0)
			{
				return coopThread.trace_verbosity;
			}
		}
		return 0;
	}

	public static void Trace(string message, int verbosity_level = 1, CoopThread ct = null)
	{
		if (log_func == null)
		{
			return;
		}
		if (ct == null)
		{
			ct = current;
			if (ct == null)
			{
				return;
			}
		}
		if (ct.CalcTraceVerbosity() >= verbosity_level)
		{
			long tickCount = TickCount;
			float num = TicksToMicros(frame_ticks);
			float num2 = TicksToMicros(ct.root.slice_ticks + ct.root.slice_overhead_ticks);
			log_func($"[FT: {num:N0}us] [ST: {num2:N0}us] [{ct}]: {message}", Game.LogType.Message);
			log_ticks += TickCount - tickCount;
		}
	}

	public static void Log(string message, CoopThread ct = null)
	{
		if (log_func != null)
		{
			long tickCount = TickCount;
			string text = ct?.ToString() ?? current?.ToString() ?? "CoopThread";
			log_func("[" + text + "]: " + message, Game.LogType.Message);
			log_ticks += TickCount - tickCount;
		}
	}

	public static void Warning(string message, CoopThread ct = null)
	{
		if (log_func != null)
		{
			long tickCount = TickCount;
			string text = ct?.ToString() ?? current?.ToString() ?? "CoopThread";
			log_func("[" + text + "]: " + message, Game.LogType.Warning);
			log_ticks += TickCount - tickCount;
		}
	}

	public static void Error(string message, CoopThread ct = null)
	{
		if (log_func != null)
		{
			long tickCount = TickCount;
			string text = ct?.ToString() ?? current?.ToString() ?? "CoopThread";
			log_func("[" + text + "]: " + message, Game.LogType.Error);
			log_ticks += TickCount - tickCount;
		}
	}

	public override string ToString()
	{
		string text = name ?? "CoopThread";
		if (callee != null)
		{
			text = text + " (waiting " + callee.name + ")";
		}
		if (coro == null)
		{
			text += $" (finished, result: {call_result})";
		}
		return text;
	}

	public static string FrameStatsText()
	{
		float num = TicksToMicros(frame_ticks);
		float num2 = TicksToMicros(frame_overhead_ticks);
		float num3 = ((num <= 0f) ? 0f : (num2 * 100f / num));
		return string.Concat(string.Concat(string.Concat($"Frame {frame_count}" + $", Threads: {frame_active_threads}/{Count}", $", Yields: {frame_yields}"), $", Time: {num:N0}us"), $", Overhead: {num2:N0}us ({num3:N0}%)");
	}

	public string ProfileLine(int ident = 0)
	{
		string text = new string(' ', ident);
		float num = ((calls == 0) ? 0f : TicksToMicros(self_ticks / calls));
		float num2 = ((calls == 0) ? 0f : TicksToMicros(total_ticks / calls));
		float num3 = ((total_ticks == 0L) ? 0 : (total_overhead_ticks * 100 / total_ticks));
		return $"{text}{name}: Calls: {calls}, Yields: {yields}, Self: {num:N0}us, Total: {num2:N0}us, Overhead: {num3}%";
	}

	public string ProfileText(int ident = 0, string new_line = "\n")
	{
		string text = ProfileLine(ident);
		if (pool == null)
		{
			return text;
		}
		foreach (KeyValuePair<string, CoopThread> item in pool)
		{
			CoopThread value = item.Value;
			text = text + new_line + value.ProfileText(ident + 4, new_line);
		}
		return text;
	}

	public static string AllProfileText()
	{
		string text = "";
		for (CoopThread coopThread = first; coopThread != null; coopThread = coopThread.next)
		{
			if (text != "")
			{
				text += "\n";
			}
			text += coopThread.root.ProfileText();
		}
		return text;
	}

	public static void Test()
	{
		int num = 3;
		int inner_trace_level = 0;
		int log_level = 4;
		Start("Thread1", Thread(), num);
		Start("Thread2", Thread2(), num);
		Start("Thread3", Thread3(), num);
		Game.Log("------------------------------------------------- " + FrameStatsText(), Game.LogType.Message);
		for (int i = 0; i < 10; i++)
		{
			if (!UpdateAll())
			{
				break;
			}
			Game.Log("------------------------------------------------- " + FrameStatsText(), Game.LogType.Message);
		}
		Game.Log(AllProfileText(), Game.LogType.Message);
		Shutdown();
		IEnumerator Sub(Value ret_val)
		{
			Trace("NOP0", log_level);
			yield return null;
			Trace("NOP1", log_level);
			yield return null;
			Trace("NOP2", log_level);
			yield return null;
			Trace("Done", log_level);
			yield return Return(ret_val);
		}
		static IEnumerator Sub2()
		{
			yield break;
		}
		IEnumerator Thread()
		{
			Trace("NOP0", log_level);
			yield return null;
			int i2 = 0;
			int sum = 0;
			while (true)
			{
				int num2 = i2 + 1;
				i2 = num2;
				Trace("Calling Sub1", log_level);
				yield return Call("Thread1.Sub1", Sub(i2), inner_trace_level);
				sum += Result.Int();
				Trace($"Result: {Result}, Sum: {sum}", log_level);
				yield return null;
				Trace("Calling Sub2", log_level);
				yield return Call("Thread1.Sub2", Sub2(), inner_trace_level);
			}
		}
		IEnumerator Thread2()
		{
			int i2 = 0;
			while (i2 < 3)
			{
				Trace($"NOP{i2}", log_level);
				yield return null;
				Trace($"Yield{i2}", log_level);
				yield return Yield;
				int num2 = i2 + 1;
				i2 = num2;
			}
		}
		IEnumerator Thread3()
		{
			Trace("Done", log_level);
			yield return Return("Done");
		}
	}
}
