using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace Logic;

public class Coroutine : Coroutine.ICaller
{
	public enum State
	{
		Started,
		Running,
		Suspended,
		Resumed,
		Finished,
		Error
	}

	public interface IResume
	{
		bool Resume(Coroutine coro);
	}

	public interface ICaller
	{
		void OnFinished(Coroutine coro);
	}

	public class WaitForFrames : IResume
	{
		private int frames;

		public WaitForFrames(int frames)
		{
			this.frames = frames;
		}

		public bool Resume(Coroutine coro)
		{
			return --frames <= 0;
		}

		public override string ToString()
		{
			return $"WaitForFrames({frames})";
		}
	}

	public class WaitForMillis : IResume
	{
		private float set_millis;

		private long start_millis;

		public long Millis => Game.prof_timer.ElapsedMilliseconds - start_millis;

		public WaitForMillis(float milliseconds)
		{
			if (Game.prof_timer == null)
			{
				Game.prof_timer = Stopwatch.StartNew();
			}
			start_millis = Game.prof_timer.ElapsedMilliseconds;
			set_millis = milliseconds;
		}

		public bool Resume(Coroutine coro)
		{
			return (float)Millis >= set_millis;
		}

		public override string ToString()
		{
			return $"WaitForMillis({Millis}ms out of {set_millis}ms)";
		}
	}

	public class ExceptionOnResume : IResume
	{
		public bool Resume(Coroutine coro)
		{
			((Coroutine)null).name = "exception";
			return true;
		}

		public override string ToString()
		{
			return "ExceptionOnResume";
		}
	}

	public Value result;

	public string error;

	private IEnumerator func;

	private object resume;

	private bool terminate_on_error;

	private Action<Value, string> on_finish;

	private string _name;

	private static object Lock = new object();

	public static FileWriter coroutinesLogWriter = null;

	private static Coroutine next_to_update = null;

	public State state { get; private set; }

	public ICaller caller { get; private set; }

	public bool Finished => state >= State.Finished;

	public static Value Result
	{
		get
		{
			if (current != null)
			{
				return current.result;
			}
			return Value.Unknown;
		}
	}

	public static string Error => current?.error;

	public string name
	{
		get
		{
			if (_name == null)
			{
				_name = func.ToString();
			}
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public static Coroutine current { get; private set; }

	public static int Count { get; private set; }

	public static Coroutine first { get; private set; }

	public static Coroutine last { get; private set; }

	public Coroutine prev { get; private set; }

	public Coroutine next { get; private set; }

	protected Coroutine()
	{
	}

	public static Coroutine Start(string name, IEnumerator func, ICaller caller = null)
	{
		if (!MainThreadUpdates.IsMainThread())
		{
			Game.Log("DELME: Starting coroutine '" + name + "' from thread: " + Thread.CurrentThread.Name, Game.LogType.Warning);
		}
		if (coroutinesLogWriter != null)
		{
			coroutinesLogWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff: ") + "Start " + name);
		}
		Coroutine coroutine = new Coroutine();
		coroutine.name = name;
		coroutine.state = State.Started;
		coroutine.func = func;
		coroutine.caller = caller ?? current;
		coroutine.result = Value.Unknown;
		lock (Lock)
		{
			if (current == null)
			{
				coroutine.prev = last;
				if (last != null)
				{
					last.next = coroutine;
				}
				else
				{
					first = coroutine;
				}
				last = coroutine;
			}
			else
			{
				coroutine.prev = current.prev;
				coroutine.next = current;
				current.prev = coroutine;
				if (coroutine.prev != null)
				{
					coroutine.prev.next = coroutine;
				}
				else
				{
					first = coroutine;
				}
			}
			Count++;
			return coroutine;
		}
	}

	public static Coroutine Start(IEnumerator func)
	{
		return Start(null, func);
	}

	public void Suspend(object resume = null)
	{
		if (Finished)
		{
			return;
		}
		lock (Lock)
		{
			state = State.Suspended;
			this.resume = resume;
			CheckResumeValue();
		}
	}

	public void Resume()
	{
		if (!Finished)
		{
			state = State.Resumed;
			resume = null;
		}
	}

	public static void TerminateOnError(bool terminate_on_error = true)
	{
		if (current != null)
		{
			current.terminate_on_error = terminate_on_error;
		}
	}

	public static void OnFinish(Action<Value, string> on_finish)
	{
		if (current != null)
		{
			current.on_finish = on_finish;
		}
	}

	private void Cleanup(State state)
	{
		if (Finished)
		{
			return;
		}
		if (!MainThreadUpdates.IsMainThread())
		{
			Game.Log("DELME: Destroying coroutine '" + name + "' from thread: " + Thread.CurrentThread.Name, Game.LogType.Warning);
		}
		this.state = state;
		resume = null;
		if (caller != null)
		{
			try
			{
				caller.OnFinished(this);
			}
			catch (Exception ex)
			{
				LogError(ex.ToString());
			}
		}
		if (on_finish != null)
		{
			Coroutine coroutine = current;
			current = this;
			try
			{
				on_finish(result, error);
			}
			catch (Exception ex2)
			{
				LogError(ex2.ToString());
			}
			current = coroutine;
		}
		caller = null;
		lock (Lock)
		{
			if (next_to_update == this)
			{
				next_to_update = next;
			}
			if (prev != null)
			{
				prev.next = next;
			}
			else
			{
				first = next;
			}
			if (next != null)
			{
				next.prev = prev;
			}
			else
			{
				last = prev;
			}
			Coroutine coroutine2 = (next = null);
			prev = coroutine2;
			Count--;
		}
	}

	public static object Return(Value result, string error = null)
	{
		if (current == null)
		{
			return null;
		}
		current.result = result;
		current.error = error;
		if (coroutinesLogWriter != null)
		{
			coroutinesLogWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff: ") + $"Return {current.name} result: {current.result}, error: '{current.error}'");
		}
		if (!current.Finished)
		{
			current.Cleanup(State.Finished);
		}
		return null;
	}

	public void Terminate(string err_msg)
	{
		if (!string.IsNullOrEmpty(err_msg))
		{
			LogError(err_msg);
		}
		if (coroutinesLogWriter != null)
		{
			coroutinesLogWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff: ") + "Terminate " + name + " error: " + err_msg);
		}
		if (!Finished)
		{
			Coroutine coroutine = resume as Coroutine;
			while (coroutine != null)
			{
				Coroutine obj = coroutine.resume as Coroutine;
				coroutine.error = err_msg;
				coroutine.Cleanup(State.Error);
				coroutine = obj;
			}
			error = err_msg;
			Cleanup(State.Error);
		}
	}

	public static void TerminateAll(string err_msg)
	{
		while (first != null)
		{
			first.Terminate(err_msg);
		}
	}

	public static void UpdateAll()
	{
		for (Coroutine coroutine = first; coroutine != null; coroutine = next_to_update)
		{
			next_to_update = coroutine.next;
			current = coroutine;
			string section = coroutine.name;
			Game.BeginProfileSection(section);
			coroutine.Update();
			Game.EndProfileSection(section);
			current = null;
		}
	}

	public static T FindCaller<T>(Coroutine coro) where T : class, ICaller
	{
		while (coro != null)
		{
			if (coro.caller is T val)
			{
				return val;
			}
			coro = coro.caller as Coroutine;
		}
		return null;
	}

	public static T FindCaller<T>() where T : class, ICaller
	{
		return FindCaller<T>(current);
	}

	public void OnFinished(Coroutine coro)
	{
		if (coro == resume)
		{
			state = State.Resumed;
			result = coro.result;
			error = coro.error;
			resume = null;
		}
	}

	private bool ShouldResume()
	{
		object obj = this.resume;
		if (obj != null)
		{
			if (!(obj is IResume resume))
			{
				if (obj is Coroutine coroutine)
				{
					Coroutine coroutine2 = coroutine;
					if (!coroutine2.Finished)
					{
						return false;
					}
					LogError("Calee coroutine finished without notifying caller");
					error = coroutine2.error;
					result = coroutine2.result;
					return true;
				}
				error = $"Unsupported coroutine resume value: {this.resume}";
				LogError(error);
				return true;
			}
			IResume resume2 = resume;
			try
			{
				return resume2.Resume(this);
			}
			catch (Exception ex)
			{
				error = ex.ToString();
				LogError(error);
				return true;
			}
		}
		return false;
	}

	private void CheckResumeValue()
	{
		object obj = this.resume;
		if (obj != null)
		{
			IResume resume;
			if ((resume = obj as IResume) != null)
			{
				return;
			}
			if (!(obj is Coroutine coroutine))
			{
				if (obj is IEnumerator enumerator)
				{
					Coroutine coroutine2 = Start(enumerator);
					coroutine2.caller = this;
					this.resume = coroutine2;
				}
				else
				{
					error = $"Unsupported coroutine resume value: {this.resume}";
					LogError(error);
					this.resume = null;
					state = State.Resumed;
				}
			}
			else
			{
				coroutine.caller = this;
			}
		}
		else
		{
			state = State.Resumed;
		}
	}

	private bool UpdateState()
	{
		switch (state)
		{
		case State.Started:
		case State.Resumed:
			return true;
		case State.Suspended:
			return ShouldResume();
		default:
			Terminate($"Invalid coroutine state: {state}");
			return false;
		}
	}

	private void Update()
	{
		if (!UpdateState())
		{
			return;
		}
		state = State.Running;
		resume = null;
		bool flag;
		try
		{
			if (error != null && terminate_on_error)
			{
				Terminate(error);
				return;
			}
			flag = !func.MoveNext();
			if (Finished)
			{
				return;
			}
			result = Value.Unknown;
			error = null;
		}
		catch (Exception ex)
		{
			Terminate(ex.ToString());
			return;
		}
		if (flag)
		{
			if (!Finished)
			{
				Cleanup(State.Finished);
			}
			return;
		}
		state = State.Suspended;
		try
		{
			resume = func.Current;
		}
		catch (Exception ex2)
		{
			resume = null;
			error = ex2.ToString();
			LogError(error);
		}
		CheckResumeValue();
	}

	public override string ToString()
	{
		string text = (result.is_unknown ? "" : $"[Result: {result}]");
		string text2 = ((error == null || state == State.Error) ? "" : "[Error]");
		Coroutine coroutine = resume as Coroutine;
		string text3 = ((resume == null) ? "" : ((coroutine == null) ? $" (waiting {resume})" : (" (waiting " + coroutine.name + ")")));
		return $"[{state}]{text}{text2} {name}{text3}";
	}

	public void Log(string msg)
	{
		msg = string.Format("{0} {1}: {2}", DateTime.Now.ToString("HH:mm:ss.fff: "), this, msg);
		UnityEngine.Debug.Log(msg);
	}

	public void LogError(string msg)
	{
		msg = string.Format("{0} {1}: Error: {2}", DateTime.Now.ToString("HH:mm:ss.fff: "), this, msg);
		UnityEngine.Debug.LogError(msg);
	}

	public static IEnumerator Test0()
	{
		int i = 0;
		while (Count > 1)
		{
			int num = i + 1;
			i = num;
			if (num > 100)
			{
				break;
			}
			string text = $"Frame {i}, Coroutines ({Count - 1}): ";
			for (Coroutine coroutine = first; coroutine != null; coroutine = coroutine.next)
			{
				if (coroutine != current)
				{
					text += coroutine.ToString();
					if (coroutine != last)
					{
						text += ", ";
					}
				}
			}
			current.Log(text);
			yield return null;
		}
	}

	public static IEnumerator Test1()
	{
		OnFinish(delegate(Value result, string err)
		{
			current.Log($"Finished: {result} ({err})");
		});
		current.Log("started, waiting 1 frame");
		yield return null;
		current.Log("waiting Test2");
		yield return Start("Test2", Test2());
		current.Log("starting Test2");
		Coroutine t2 = Start("Test2", Test2(5));
		yield return new WaitForFrames(2);
		current.Log("killing Test2");
		t2.Terminate("kill");
		yield return null;
		current.Log("waiting Test3");
		yield return Test3();
		if (Error != null)
		{
			current.Log("error");
			yield return null;
		}
		current.Log("waiting ExceptionOnResume");
		yield return new ExceptionOnResume();
		if (Error != null)
		{
			current.Log("error");
			yield return null;
		}
		current.Log("Self Destruct");
		yield return Start("Self Destruct", SelfDestruct(3));
		current.Log("past Self Destruct?!?");
		static IEnumerator SelfDestruct(int frames)
		{
			while (frames > 0)
			{
				current.Log(frames.ToString());
				int num = frames - 1;
				frames = num;
				yield return null;
			}
			(current.caller as Coroutine)?.Terminate("Self Destruct");
			yield return null;
			current.Log("past Self Destruct?!?");
		}
	}

	public static IEnumerator Test2(int frames = 1)
	{
		OnFinish(delegate(Value result, string err)
		{
			current.Log($"Finished: {result} ({err})");
		});
		current.Log($"Test2 started, waiting {frames} frame(s)");
		yield return new WaitForFrames(frames);
		current.Log("Test2 done");
		yield return Return(frames);
	}

	public static IEnumerator Test3()
	{
		current.name = "Test3";
		current.Log("Test3 started, waiting 1 frame");
		yield return null;
		current.Log("Test3 throwing exception");
		((Coroutine)null).name = "exception";
		yield return null;
		current.Log("Test3 after exception?!?");
	}

	public static void Test()
	{
		Start("-------", Test0());
		Start("Test1", Test1());
		while (Count > 0)
		{
			UpdateAll();
		}
		Console.WriteLine("done");
	}
}
