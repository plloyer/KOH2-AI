using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public class RemoteVars : IVars
{
	public delegate void DataChangedCallback(RemoteVars rvars, string key, Value val);

	public interface IListener
	{
		void OnVarChanged(RemoteVars vars, string key, Value old_val, Value new_val);
	}

	public interface IValidator
	{
		bool ValidateVarChange(RemoteVars vars, string key, Value old_val, ref Value new_val);
	}

	public enum DataType
	{
		CampaignData,
		PersistentPlayerData,
		NonPersistentPlayerData,
		AllPersistentData,
		GlobalPersistentPlayerData,
		ChatData
	}

	private struct VarChange
	{
		public string key;

		public Value old_val;

		public Value new_val;

		public VarChange(string key, Value old_val, Value new_val)
		{
			this.key = key;
			this.old_val = old_val;
			this.new_val = new_val;
		}
	}

	public static DataChangedCallback data_changed;

	public Campaign campaign;

	public DataType data_type;

	public int player_idx;

	public Vars vars = new Vars();

	private List<IListener> listeners;

	private List<IValidator> validators;

	public RemoteVars(Campaign campaign, DataType data_type, int player_idx)
	{
		this.campaign = campaign;
		this.data_type = data_type;
		this.player_idx = player_idx;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "name" && data_type == DataType.PersistentPlayerData)
		{
			string playerID = GetPlayerID();
			string playerName = PlayerInfo.GetPlayerName(playerID);
			Game.Log("Resolving player id (" + playerID + ") to name (" + playerName + ") using GetVar(). Use Logic.PlayerInfo.GetPlayerName or Campaign.GetPlayerName instead!", Game.LogType.Warning);
			return playerName;
		}
		return this.vars.GetVar(key, vars, as_value);
	}

	public bool IsPersistent()
	{
		if (data_type != DataType.NonPersistentPlayerData)
		{
			return data_type != DataType.ChatData;
		}
		return false;
	}

	public string GetPlayerID()
	{
		if (data_type == DataType.CampaignData || data_type == DataType.ChatData)
		{
			return null;
		}
		return vars.Get<string>("id");
	}

	public bool IsAuthority()
	{
		if (campaign == null)
		{
			return false;
		}
		if (data_type == DataType.ChatData)
		{
			return false;
		}
		string playerID = GetPlayerID();
		string ownerID = campaign.GetOwnerID();
		string text = campaign.CalcHostID();
		if (data_type == DataType.CampaignData)
		{
			if (campaign.state < Campaign.State.Started)
			{
				return ownerID == THQNORequest.userId;
			}
			return text == THQNORequest.userId;
		}
		if (data_type == DataType.PersistentPlayerData)
		{
			if (playerID == THQNORequest.userId)
			{
				return true;
			}
			if (campaign.state >= Campaign.State.Started)
			{
				return text == THQNORequest.userId;
			}
			return ownerID == THQNORequest.userId;
		}
		return playerID == THQNORequest.userId;
	}

	public int GetVersion()
	{
		if (vars == null)
		{
			return 0;
		}
		return vars.Get("version").Int();
	}

	public void SetVersion(int version, bool send_data_changed = true)
	{
		Set("version", version, send_data_changed);
	}

	public int IncVersion(bool send_data_changed = true)
	{
		if (!IsPersistent())
		{
			if (send_data_changed)
			{
				data_changed?.Invoke(this, "version", 0);
			}
			return 0;
		}
		int version = GetVersion();
		version++;
		SetVersion(version, send_data_changed);
		return version;
	}

	public void Set(string key, Value val, bool send_data_changed = true)
	{
		Value var = GetVar(key);
		if (!(var == val))
		{
			if (val.is_unknown)
			{
				vars.Del(key);
			}
			else
			{
				vars.Set(key, val);
			}
			NotifyVarChanged(key, var, val);
			if (send_data_changed && IsAuthority())
			{
				IncVersion(send_data_changed: false);
				data_changed?.Invoke(this, key, val);
			}
		}
	}

	public void SetAll(Vars new_vars, bool send_data_changed = true)
	{
		if (new_vars == null)
		{
			Log("RemoteVars.SetAll called with null");
			new_vars = new Vars();
		}
		Vars old_vars = vars;
		vars = new_vars.Copy();
		if (listeners != null)
		{
			List<VarChange> changes = new List<VarChange>();
			changes.Add(new VarChange(null, old_vars, new_vars));
			old_vars.EnumerateAll(delegate(string key, Value val)
			{
				Value var = new_vars.GetVar(key);
				if (!(var == val))
				{
					changes.Add(new VarChange(key, val, var));
				}
			});
			new_vars.EnumerateAll(delegate(string key, Value val)
			{
				if (!old_vars.ContainsKey(key) && !(val == Value.Unknown))
				{
					changes.Add(new VarChange(key, Value.Unknown, val));
				}
			});
			foreach (VarChange item in changes)
			{
				NotifyVarChanged(item.key, item.old_val, item.new_val);
			}
		}
		if (send_data_changed && IsAuthority())
		{
			IncVersion(send_data_changed: false);
			data_changed?.Invoke(this, null, new_vars);
		}
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
		listeners?.Remove(listener);
	}

	public void AddValidator(IValidator validator)
	{
		if (validators == null)
		{
			validators = new List<IValidator>();
		}
		validators.Add(validator);
	}

	public void DelValidator(IValidator validator)
	{
		validators?.Remove(validator);
	}

	public void SetOffline(string key, Value val, bool send_data_changed = true)
	{
		if (validators != null)
		{
			Value var = GetVar(key);
			for (int i = 0; i < validators.Count; i++)
			{
				if (!validators[i].ValidateVarChange(this, key, var, ref val))
				{
					return;
				}
			}
		}
		Set(key, val, send_data_changed);
	}

	private void NotifyVarChanged(string key, Value old_val, Value new_val)
	{
		if (listeners != null)
		{
			for (int i = 0; i < listeners.Count; i++)
			{
				listeners[i].OnVarChanged(this, key, old_val, new_val);
			}
		}
	}

	public override string ToString()
	{
		string text = ((campaign == null) ? "null" : campaign.idx.ToString());
		return $"campaign {text} {data_type} [{player_idx}]: {vars}";
	}

	public string Dump()
	{
		return Dump("");
	}

	public string Dump(string ident, bool deep = true)
	{
		return vars.Dump(ident, "\n" + ident, deep);
	}

	public static void Log(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		Debug.Log(msg);
	}

	public static void LogError(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		Debug.LogError(msg);
	}
}
