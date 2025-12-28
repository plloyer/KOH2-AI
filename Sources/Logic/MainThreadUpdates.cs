using System;
using System.Collections.Generic;
using System.Threading;

namespace Logic;

public class MainThreadUpdates
{
	private static object Lock = new object();

	private static List<System.Action> updates = new List<System.Action>();

	private static List<System.Action> updates2 = new List<System.Action>();

	public static Thread main_thread { get; private set; }

	public static void Init()
	{
		if (main_thread != null)
		{
			Game.Log("MainThreadUpdates.Init already called", Game.LogType.Error);
		}
		main_thread = Thread.CurrentThread;
	}

	public static bool IsMainThread()
	{
		return Thread.CurrentThread == main_thread;
	}

	public static bool AssertMainThread(string caller)
	{
		if (Thread.CurrentThread == main_thread)
		{
			return true;
		}
		Game.Log(caller + " called from non-main thread: " + Thread.CurrentThread.Name, Game.LogType.Error);
		return false;
	}

	public static bool Perform(System.Action action)
	{
		if (IsMainThread())
		{
			action();
			return true;
		}
		Schedule(action);
		return false;
	}

	public static void Schedule(System.Action action)
	{
		lock (Lock)
		{
			updates.Add(action);
		}
	}

	public static void Update()
	{
		List<System.Action> list;
		lock (Lock)
		{
			list = updates;
			updates = updates2;
			updates2 = list;
		}
		for (int i = 0; i < list.Count; i++)
		{
			System.Action action = list[i];
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Game.Log(ex.ToString(), Game.LogType.Error);
			}
		}
		list.Clear();
	}

	public static void Test(Multiplayer mp)
	{
		mp?.QueueSS(delegate
		{
			Game.Log("Hello from the QSS thread", Game.LogType.Message);
			Perform(delegate
			{
				Game.Log("Hello from the main thread", Game.LogType.Message);
			});
			Game.Log("Hello again the QSS thread", Game.LogType.Message);
		});
		Game.Log("Pending Hello from the main thread", Game.LogType.Message);
	}
}
