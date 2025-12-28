using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Logic;

public class PlayerInfo
{
	public enum OnlineState
	{
		Unknown,
		Offline,
		Online
	}

	public string id;

	private string _name;

	public OnlineState online_state;

	public RemoteVars persistent_global_vars = new RemoteVars(null, RemoteVars.DataType.GlobalPersistentPlayerData, -1);

	public static Dictionary<string, PlayerInfo> registry = new Dictionary<string, PlayerInfo>();

	public static Action<string, string> on_player_name_changed = null;

	private static bool loading_names_cache = false;

	public string name
	{
		get
		{
			return _name;
		}
		set
		{
			if (!(value == _name))
			{
				_name = value;
				if (!string.IsNullOrEmpty(_name))
				{
					SaveNamesCache();
					on_player_name_changed?.Invoke(id, name);
				}
			}
		}
	}

	public bool online => online_state == OnlineState.Online;

	public override string ToString()
	{
		return $"[{id}] {name}: {online_state}";
	}

	public string Dump()
	{
		return Dump("");
	}

	public string Dump(string ident, bool deep = true)
	{
		return $"{this}\n{ident}{persistent_global_vars.Dump(ident, deep)}";
	}

	public static PlayerInfo Get(string player_id, bool create)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			return null;
		}
		if (registry.TryGetValue(player_id, out var value))
		{
			return value;
		}
		if (!create)
		{
			return null;
		}
		value = new PlayerInfo();
		value.id = player_id;
		value.persistent_global_vars.Set("id", player_id, send_data_changed: false);
		registry.Add(player_id, value);
		return value;
	}

	public static string GetPlayerName(string player_id)
	{
		return Get(player_id, create: false)?.name;
	}

	public static void LoadNamesCache()
	{
		string text = Game.GetSavesRootDir(Game.SavesRoot.Multi) + "/player_names.txt";
		loading_names_cache = true;
		try
		{
			string[] array = File.ReadAllLines(text, Encoding.UTF8);
			for (int i = 0; i < array.Length; i += 2)
			{
				string text2 = array[i];
				string value = array[i + 1];
				if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(value))
				{
					PlayerInfo playerInfo = Get(text2, create: true);
					if (string.IsNullOrEmpty(playerInfo.name))
					{
						playerInfo.name = value;
					}
				}
			}
		}
		catch (FileNotFoundException)
		{
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (Exception ex3)
		{
			Game.Log("Error reading " + text + ": " + ex3.Message, Game.LogType.Error);
		}
		loading_names_cache = false;
	}

	public static void SaveNamesCache()
	{
		if (loading_names_cache)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder(1024);
		foreach (KeyValuePair<string, PlayerInfo> item in registry)
		{
			string key = item.Key;
			string value = item.Value?.name;
			if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
			{
				stringBuilder.AppendLine(key);
				stringBuilder.AppendLine(value);
			}
		}
		string contents = stringBuilder.ToString();
		File.WriteAllText(Game.GetSavesRootDir(Game.SavesRoot.Multi) + "/player_names.txt", contents, Encoding.UTF8);
	}
}
