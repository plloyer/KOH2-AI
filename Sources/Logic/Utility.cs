using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Logic;

public class THQNORequest : Coroutine.IResume
{
	public delegate void OnLeaveLobbyCallbackDelegate(string lobbyId, string memberChangedId, string memberIdMakingChange, Common.MemberLeftReason memberLeftReason);

	public delegate void OnSignOutResponseCallback();

	public delegate void OnCallbackMessage(string msg, object param);

	public enum EItemState
	{
		None = 0,
		Subscribed = 1,
		LegacyItem = 2,
		Installed = 4,
		NeedsUpdate = 8,
		Downloading = 0x10,
		DownloadPending = 0x20
	}

	public static class Utility
	{
		private static byte[] encrypt_key = new byte[16]
		{
			25, 197, 154, 211, 240, 223, 182, 197, 205, 16,
			147, 190, 55, 126, 152, 47
		};

		private static byte[] encrypt_iv = new byte[16]
		{
			101, 56, 27, 187, 183, 147, 214, 82, 84, 133,
			193, 167, 118, 147, 253, 18
		};

		public static string Encrypt(string plain_text)
		{
			if (string.IsNullOrEmpty(plain_text))
			{
				return plain_text;
			}
			byte[] bytes = Encoding.ASCII.GetBytes(plain_text);
			ICryptoTransform transform = Aes.Create().CreateEncryptor(encrypt_key, encrypt_iv);
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			cryptoStream.Write(bytes, 0, bytes.Length);
			cryptoStream.Close();
			return Convert.ToBase64String(memoryStream.ToArray());
		}

		public static string Decrypt(string encrypted_text)
		{
			if (string.IsNullOrEmpty(encrypted_text))
			{
				return encrypted_text;
			}
			byte[] array = Convert.FromBase64String(encrypted_text);
			ICryptoTransform transform = Aes.Create().CreateDecryptor(encrypt_key, encrypt_iv);
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			cryptoStream.Write(array, 0, array.Length);
			cryptoStream.Close();
			byte[] bytes = memoryStream.ToArray();
			return Encoding.ASCII.GetString(bytes);
		}

		public static string GetCredentialsFilePath()
		{
			string text = "\\thqno_credentials.txt";
			string savesRootDir = Game.GetSavesRootDir(Game.SavesRoot.Root);
			string[] array = savesRootDir.Split('\\', '/');
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == "Saves2")
				{
					text = "\\thqno_credentials2.txt";
					break;
				}
			}
			return savesRootDir + "\\.." + text;
		}

		public static void WriteTHQNOCredentials(string email, string password)
		{
			WriteTHQNOEMail(email);
			WriteTHQNOPassword(password);
		}

		public static void WriteTHQNOEMail(string email)
		{
			WriteTHQNOData(email, 0, "mail: ");
		}

		public static void WriteTHQNOPassword(string password)
		{
			password = Encrypt(password);
			WriteTHQNOData(password, 1, "pass: ");
		}

		public static void WriteTHQNODisplayName(string displayName)
		{
			WriteTHQNOData(displayName, 2, "name: ");
		}

		private static string GetTHQNOData(string[] data, int lineIndex, string prefix)
		{
			if (data == null || data.Length <= lineIndex)
			{
				return null;
			}
			string text = data[lineIndex];
			if (string.IsNullOrEmpty(prefix))
			{
				return text;
			}
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (!text.StartsWith(prefix, StringComparison.Ordinal))
			{
				return null;
			}
			return text.Substring(prefix.Length);
		}

		public static void ReadTHQNOCredentials(out string email, out string password)
		{
			email = ReadTHQNOEmail();
			password = ReadTHQNOPassword();
		}

		public static string ReadTHQNOEmail()
		{
			string[] data = ReadTHQNOData();
			string tHQNOData = GetTHQNOData(data, 0, "mail: ");
			if (!string.IsNullOrEmpty(tHQNOData))
			{
				return tHQNOData;
			}
			tHQNOData = GetTHQNOData(data, 0, null);
			if (string.IsNullOrEmpty(tHQNOData))
			{
				return null;
			}
			WriteTHQNOEMail(tHQNOData);
			return tHQNOData;
		}

		public static string ReadTHQNOPassword()
		{
			string[] data = ReadTHQNOData();
			string tHQNOData = GetTHQNOData(data, 1, "pass: ");
			if (!string.IsNullOrEmpty(tHQNOData))
			{
				return Decrypt(tHQNOData);
			}
			tHQNOData = GetTHQNOData(data, 1, null);
			if (string.IsNullOrEmpty(tHQNOData))
			{
				return null;
			}
			WriteTHQNOPassword(tHQNOData);
			return tHQNOData;
		}

		public static string ReadTHQNODisplayName()
		{
			string[] data = ReadTHQNOData();
			string tHQNOData = GetTHQNOData(data, 2, "name: ");
			if (!string.IsNullOrEmpty(tHQNOData))
			{
				return tHQNOData;
			}
			tHQNOData = GetTHQNOData(data, 2, null);
			if (string.IsNullOrEmpty(tHQNOData))
			{
				return null;
			}
			WriteTHQNODisplayName(tHQNOData);
			return tHQNOData;
		}

		public static string ReadTHQNOCredentialsDefaultMultiplayerMap()
		{
			string[] array = ReadTHQNOData();
			if (array == null || array.Length < 4)
			{
				return null;
			}
			return array[3];
		}

		private static void WriteTHQNOData(string data, int lineIndex, string prefix)
		{
			string[] array = ReadTHQNOData();
			string[] array2 = new string[(array == null || array.Length < 4) ? 4 : array.Length];
			if (array != null)
			{
				Array.Copy(array, 0, array2, 0, array.Length);
			}
			if (!string.IsNullOrEmpty(prefix))
			{
				data = prefix + data;
			}
			array2[lineIndex] = data;
			try
			{
				File.WriteAllLines(GetCredentialsFilePath(), array2);
			}
			catch (Exception arg)
			{
				Multiplayer.Error($"WriteTHQNOData error: {arg}");
			}
		}

		private static string[] ReadTHQNOData()
		{
			string[] array = null;
			string credentialsFilePath = GetCredentialsFilePath();
			if (!File.Exists(credentialsFilePath))
			{
				return null;
			}
			try
			{
				return File.ReadAllLines(credentialsFilePath);
			}
			catch (Exception arg)
			{
				Multiplayer.Error($"ReadTHQNOData error: {arg}");
				return null;
			}
		}
	}

	public static bool loadingScreenShown = false;

	public static bool enabled = true;

	public static bool devIgnoreP2P = false;

	public static object LockP2P = new object();

	public static Common.PlatformType platformType = Common.PlatformType.THQNO;

	public static string lobbyIdToJoin = string.Empty;

	public static OnLeaveLobbyCallbackDelegate on_leave_lobby_callback = null;

	public static OnSignOutResponseCallback on_sign_out_response = null;

	public uint req_id;

	public int timeout;

	public Stopwatch timer;

	public Value result = Value.Unknown;

	public string error;

	public bool completed;

	public static OnCallbackMessage onCallbackMessage;

	public static bool initted = false;

	public static bool connected = false;

	public static bool signed_in = false;

	public static string CHECK_ONLINE_SERVICES_USER_ID = "2_0_270986";

	public static string userId = string.Empty;

	public static string playerName = string.Empty;

	public static bool networkingAvailable = false;

	public static string currentlyEnteredTHQNOLobbyId = string.Empty;

	public static bool user_stats_received = false;

	public static Common.APIResult lastConnectToOnlineServiceResult = Common.APIResult.InvalidResultCode;

	private static List<Multiplayer> multiplayers = new List<Multiplayer>();

	private static float last_update_time = 0f;

	public static float update_interval = 0f;

	private static bool incStatInProgress = false;

	private static readonly object MultiplayersLock = new object();

	private static Dictionary<uint, THQNORequest> pending = new Dictionary<uint, THQNORequest>();

	private static THQNORequest signin_req = null;

	private static THQNORequest signout_req = null;

	private static THQNORequest register_req = null;

	private static THQNORequest requestlobbydata_req = null;

	private static THQNORequest requestp2paddress_req = null;

	private static THQNORequest requestcurrentstats_req = null;

	private static THQNORequest storestats_req = null;

	private static THQNORequest startpasswordrecovery_req = null;

	private static THQNORequest confirmpasswordrecovery_req = null;

	private static THQNORequest checkemail_req = null;

	private static THQNORequest checkphone_req = null;

	private static THQNORequest verifyemail_req = null;

	private static THQNORequest verifyphone_req = null;

	private static THQNORequest updateemail_req = null;

	private static THQNORequest updatephone_req = null;

	private static THQNORequest resetpassword_req = null;

	private static THQNORequest deleteaccount_req = null;

	private static THQNORequest joinroom_req = null;

	private static THQNORequest leaveroom_req = null;

	private static THQNORequest sendroommessage_req = null;

	private static THQNORequest downloaditem_req = null;

	private static THQNORequest getpublicdocumentread_req = null;

	private static char[] new_line_characters = new char[2] { '\r', '\n' };

	public THQNORequest(uint req_id, int timeout_ms = 10000)
	{
		this.req_id = req_id;
		timeout = timeout_ms;
		if (timeout_ms > 0)
		{
			timer = Stopwatch.StartNew();
		}
		if (req_id != 0)
		{
			pending.Add(req_id, this);
		}
	}

	public bool Resume(Coroutine coro)
	{
		if (timeout > 0 && timer.ElapsedMilliseconds > timeout)
		{
			Completed(Value.Unknown, "timeout");
		}
		if (!completed)
		{
			return false;
		}
		coro.result = result;
		coro.error = error;
		return true;
	}

	public static void UpdateAll()
	{
		if (!enabled)
		{
			return;
		}
		float realtimeSinceStartup = UnityEngine.Time.realtimeSinceStartup;
		if (!(realtimeSinceStartup - last_update_time < update_interval))
		{
			last_update_time = realtimeSinceStartup;
			if (initted)
			{
				THQNO_Wrapper.Update();
			}
		}
	}

	public static THQNORequest Connect()
	{
		if (!enabled)
		{
			return null;
		}
		try
		{
			if (!initted)
			{
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log("THQNO_Wrapper.InitLogs", 2);
				}
				THQNO_Wrapper.InitCSLogs(Log, LogError);
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log("THQNO_Wrapper.Init", 2);
				}
				THQNO_Wrapper.Init();
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log("THQNO_Wrapper.RegisterDebugLogCallback", 2);
				}
				THQNO_Wrapper.RegisterDebugLogCallback(OnDebugLogCallback);
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log("THQNO_Wrapper.RegisterCallbacks", 2);
				}
				THQNO_Callbacks.RegisterCallbacks();
				initted = true;
			}
			if (!connected)
			{
				string text = "";
				string text2 = THQNOConfigPath();
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log("THQNO_Wrapper.ConnectToOnlineService(" + text + ", " + text2 + ")", 2);
				}
				Common.APIResult aPIResult = THQNO_Wrapper.ConnectToOnlineService(text, text2);
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log($"Connect result: {aPIResult}", 2);
				}
				lastConnectToOnlineServiceResult = aPIResult;
				if (aPIResult == Common.APIResult.Success)
				{
					connected = true;
				}
			}
		}
		catch (Exception ex)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(ex.ToString(), 2);
			}
		}
		return null;
	}

	public static THQNORequest Disconnect()
	{
		if (!enabled)
		{
			return null;
		}
		if (connected)
		{
			connected = false;
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.DisconnectFromOnlineService", 2);
			}
			THQNO_Wrapper.DisconnectFromOnlineService();
			signed_in = false;
		}
		if (initted)
		{
			initted = false;
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.UnregisterCallbacks", 2);
			}
			THQNO_Callbacks.UnregisterCallbacks();
		}
		user_stats_received = false;
		return null;
	}

	public static THQNORequest CheckOnlineServices()
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("Checking online services...", 2);
		}
		if (!signed_in)
		{
			string msg = "CheckOnlineServices() called while THQNORequest.signed_in is false";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			return null;
		}
		return PlayerDataGetPersonalData(CHECK_ONLINE_SERVICES_USER_ID, log: false);
	}

	public static THQNORequest RegisterEmailPW(string displayName, string email, string password, bool showActivationLinkInEmail, bool showActivationCodeInEmail)
	{
		if (register_req != null)
		{
			return register_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (signed_in)
		{
			RegisterResultCD registerResultCD = new RegisterResultCD
			{
				result = Common.APIResult.Success
			};
			tHQNORequest.Completed(new Value(registerResultCD));
		}
		else
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.RegisterEmailPW(" + email + ", ...)", 2);
			}
			register_req = tHQNORequest;
			THQNO_Wrapper.RegisterEmailPW(displayName, email, password, showActivationLinkInEmail, showActivationCodeInEmail);
		}
		return tHQNORequest;
	}

	public static THQNORequest RegisterPlatform()
	{
		if (register_req != null)
		{
			return register_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (signed_in)
		{
			RegisterResultCD registerResultCD = new RegisterResultCD
			{
				result = Common.APIResult.Success
			};
			tHQNORequest.Completed(new Value(registerResultCD));
		}
		else
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.RegisterPlatform", 2);
			}
			register_req = tHQNORequest;
			THQNO_Wrapper.RegisterPlatform(platformType, null, 0);
		}
		return tHQNORequest;
	}

	public static void OnRegisterResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnRegisterResponse", 2);
		}
		if (register_req != null)
		{
			Value value = new Value(res);
			register_req.Completed(value);
			register_req = null;
		}
	}

	public static THQNORequest SignInEmailPW(string email, string password)
	{
		if (signin_req != null)
		{
			return signin_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (signed_in)
		{
			SignInResultCD signInResultCD = new SignInResultCD
			{
				result = Common.APIResult.Success,
				userId = userId,
				networkingAvaliable = networkingAvailable
			};
			tHQNORequest.Completed(new Value(signInResultCD));
		}
		else
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.SignInEmailPW(" + email + ", *****)", 2);
			}
			signin_req = tHQNORequest;
			THQNO_Wrapper.SignInEmailPW(email, password);
		}
		return tHQNORequest;
	}

	public static THQNORequest SignInPlatform()
	{
		if (signin_req != null)
		{
			return signin_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (signed_in)
		{
			SignInResultCD signInResultCD = new SignInResultCD
			{
				result = Common.APIResult.Success,
				userId = userId,
				networkingAvaliable = networkingAvailable
			};
			tHQNORequest.Completed(new Value(signInResultCD));
		}
		else
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log($"THQNO_Wrapper.SignInPlatform({platformType})", 2);
			}
			signin_req = tHQNORequest;
			THQNO_Wrapper.SignInPlatform(platformType, null, 0);
		}
		return tHQNORequest;
	}

	public static void OnSignInResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnSignInResponse", 2);
		}
		if (signin_req != null)
		{
			Value value = new Value(res);
			signin_req.Completed(value);
			signin_req = null;
			SignInResultCD signInResultCD = (SignInResultCD)value.obj_val;
			if (signInResultCD.result == Common.APIResult.Success || signInResultCD.result == Common.APIResult.SignIn_AlreadySignedIn)
			{
				signed_in = true;
			}
			else
			{
				signed_in = false;
			}
		}
	}

	public static THQNORequest SignOut()
	{
		if (signout_req != null)
		{
			return signout_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (!signed_in)
		{
			OnlineServicesSignOutCD onlineServicesSignOutCD = new OnlineServicesSignOutCD
			{
				reason = Common.APIResult.Success
			};
			tHQNORequest.Completed(new Value(onlineServicesSignOutCD));
		}
		else
		{
			signout_req = tHQNORequest;
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.SignOut", 2);
			}
			THQNO_Wrapper.SignOut();
		}
		return tHQNORequest;
	}

	public static void OnSignOutResponse(object res)
	{
		signed_in = false;
		playerName = string.Empty;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnSignOutResponse", 2);
		}
		on_sign_out_response?.Invoke();
		if (signout_req != null)
		{
			Value value = new Value(res);
			signout_req.Completed(value);
			signout_req = null;
			string text = "Sign out occurred during request execution";
			KeyValuePair<uint, THQNORequest>[] array = pending.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Value.Completed(Value.Null, text);
			}
			pending.Clear();
		}
	}

	public static THQNORequest CreateLobby(Common.LobbyType lobbyType, int maxMembers, string name, bool joinable)
	{
		if (!enabled)
		{
			return new THQNORequest(0u);
		}
		if (!connected)
		{
			return new THQNORequest(0u);
		}
		if (name == null)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNORequest.CreateLobby name is null!", 2);
			}
			return new THQNORequest(0u);
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.CreateLobby", 2);
		}
		return new THQNORequest(THQNO_Wrapper.CreateLobby(THQNO_Callbacks.LobbyCreatedCallback, lobbyType, maxMembers, name, joinable));
	}

	public static THQNORequest JoinLobby(string lobbyId)
	{
		if (!enabled)
		{
			return new THQNORequest(0u);
		}
		if (!connected)
		{
			return new THQNORequest(0u);
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.JoinLobby " + lobbyId, 2);
		}
		return new THQNORequest(THQNO_Wrapper.JoinLobby(THQNO_Callbacks.LobbyEnterCallback, lobbyId));
	}

	public static THQNORequest LeaveCurrentLobby()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.LeaveLobby(" + currentlyEnteredTHQNOLobbyId + ")", 2);
		}
		THQNO_Wrapper.LeaveLobby(currentlyEnteredTHQNOLobbyId);
		currentlyEnteredTHQNOLobbyId = string.Empty;
		return tHQNORequest;
	}

	public static void OnLeaveLobbyCallback(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnLeaveLobbyCallback", 2);
		}
		if (res == null)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("OnLeaveLobbyCallback res is null", 2);
			}
		}
		else
		{
			LobbyLeaveCD lobbyLeaveCD = (LobbyLeaveCD)res;
			on_leave_lobby_callback?.Invoke(lobbyLeaveCD.lobbyId, lobbyLeaveCD.memberChangedId, lobbyLeaveCD.memberIdMakingChange, lobbyLeaveCD.reason);
		}
	}

	public static THQNORequest RequestLobbyList(bool requestLobbyContent)
	{
		if (!enabled)
		{
			return new THQNORequest(0u);
		}
		if (!connected)
		{
			return new THQNORequest(0u);
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"THQNO_Wrapper.RequestLobbyList requestLobbyContent: {requestLobbyContent}", 2);
		}
		return new THQNORequest(THQNO_Wrapper.RequestLobbyList(THQNO_Callbacks.LobbyListReceivedCallback, requestLobbyContent));
	}

	public static THQNORequest InviteUserToLobby(string lobbyId, string invitee)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.InviteUserToLobby(" + lobbyId + ", " + invitee + ")", 2);
		}
		THQNO_Wrapper.InviteUserToLobby(lobbyId, invitee);
		return tHQNORequest;
	}

	public static void OnInviteUserToLobbyResponse(object res)
	{
		LobbyInviteCD lobbyInviteCD = (LobbyInviteCD)new Value(res).obj_val;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"OnInviteUserToLobbyResponse: userId: {lobbyInviteCD.userId} lobbyId: {lobbyInviteCD.lobbyId} spectate: {lobbyInviteCD.spectate}", 2);
		}
		lobbyIdToJoin = lobbyInviteCD.lobbyId;
		if (onCallbackMessage != null)
		{
			onCallbackMessage("on_invite_response", null);
		}
	}

	public static THQNORequest GetNumLobbyMembers(string lobbyId)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetNumLobbyMembers(" + lobbyId + ")", 2);
		}
		int numLobbyMembers = THQNO_Wrapper.GetNumLobbyMembers(lobbyId);
		tHQNORequest.Completed(numLobbyMembers);
		return tHQNORequest;
	}

	public static IEnumerator GetNumLobbyMembersCoro(string lobbyId)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return GetNumLobbyMembers(lobbyId);
		int val = Coroutine.Result;
		yield return Coroutine.Return(new Value(val));
	}

	public static THQNORequest GetLobbyStringData(string lobbyId, string key, StringBuilder buffer, uint bufferSize)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetStringDataCStr", 2);
		}
		THQNO_Wrapper.GetStringDataCStr(lobbyId, key, buffer, bufferSize);
		return tHQNORequest;
	}

	public static THQNORequest SetLobbyStringData(string lobbyId, string key, string value)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.SetStringData(" + lobbyId + ", " + key + ", " + value + ")", 2);
		}
		bool flag = THQNO_Wrapper.SetStringData(lobbyId, key, value);
		tHQNORequest.Completed(flag);
		return tHQNORequest;
	}

	public static THQNORequest GetLobbyIntData(string lobbyId, string key)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetIntData(" + lobbyId + ", " + key + ")", 2);
		}
		int intData = THQNO_Wrapper.GetIntData(lobbyId, key);
		tHQNORequest.Completed(intData);
		return tHQNORequest;
	}

	public static THQNORequest SetLobbyIntData(string lobbyId, string key, int value)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"THQNO_Wrapper.SetIntData({lobbyId}, {key}, {value})", 2);
		}
		bool flag = THQNO_Wrapper.SetIntData(lobbyId, key, value);
		tHQNORequest.Completed(flag);
		return tHQNORequest;
	}

	public static THQNORequest RequestLobbyData(string lobbyId)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (requestlobbydata_req != null)
		{
			tHQNORequest.error = "Another RequestLobbyData in progress";
			return tHQNORequest;
		}
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.RequestLobbyData(" + lobbyId + ")", 2);
		}
		requestlobbydata_req = tHQNORequest;
		if (!THQNO_Wrapper.RequestLobbyData(lobbyId))
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("RequestLobbyData returned false.", 2);
			}
			LobbyDataUpdateCD lobbyDataUpdateCD = new LobbyDataUpdateCD
			{
				result = Common.APIResult.Fail
			};
			Value value = new Value(lobbyDataUpdateCD);
			requestlobbydata_req.Completed(value, "unsucessful request");
			requestlobbydata_req = null;
		}
		return tHQNORequest;
	}

	public static void OnRequestLobbyData(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnRequestLobbyData()", 2);
		}
		if (requestlobbydata_req != null)
		{
			Value value = new Value(res);
			requestlobbydata_req.Completed(value);
			requestlobbydata_req = null;
		}
	}

	public static THQNORequest GetLobbyType(string lobbyId)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetLobbyType(" + lobbyId + ")", 2);
		}
		Common.LobbyType lobbyType = THQNO_Wrapper.GetLobbyType(lobbyId);
		tHQNORequest.Completed(new Value(lobbyType));
		return tHQNORequest;
	}

	public static THQNORequest SetLobbyType(string lobbyId, Common.LobbyType lobbyType)
	{
		if (!enabled)
		{
			return new THQNORequest(0u);
		}
		if (!connected)
		{
			return new THQNORequest(0u);
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.SetLobbyType", 2);
		}
		return new THQNORequest(THQNO_Wrapper.SetLobbyType(THQNO_Callbacks.SetLobbyTypeCallback, lobbyId, lobbyType));
	}

	public static THQNORequest GetLobbyOwner(string lobbyId)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetLobbyOwner(" + lobbyId + ")", 2);
		}
		string lobbyOwner = THQNO_Wrapper.GetLobbyOwner(lobbyId);
		tHQNORequest.Completed(lobbyOwner);
		return tHQNORequest;
	}

	public static THQNORequest SendP2PPacket(string remoteId, IntPtr data, uint dataSize, Common.P2PSendType sendType, uint channel)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (devIgnoreP2P)
		{
			return tHQNORequest;
		}
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2) && channel == 0)
		{
			Multiplayer.Log($"THQNO_Wrapper.SendP2PPacket to {remoteId}:{channel} ({dataSize} bytes)", 2);
		}
		THQNO_Wrapper.SendP2PPacket(remoteId, data, dataSize, sendType, channel);
		return tHQNORequest;
	}

	public static void OnP2PSessionConnectFailResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnP2PSessionConnectFailResponse", 2);
		}
		P2PSessionConnectFailCD p2PSessionConnectFailCD = (P2PSessionConnectFailCD)res;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"THQNO_Wrapper.OnP2PSessionConnectFail: {p2PSessionConnectFailCD.remoteID}, error: {p2PSessionConnectFailCD.P2PSessionError}", 2);
		}
	}

	public static THQNORequest IsP2PPacketAvailable(out uint messageSize, uint channel)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled || !connected)
		{
			messageSize = 0u;
			return tHQNORequest;
		}
		bool flag = THQNO_Wrapper.IsP2PPacketAvailable(out messageSize, channel);
		tHQNORequest.Completed(flag);
		return tHQNORequest;
	}

	public static string ReadP2PPacket(IntPtr dest, uint destSize, out uint messageSize, uint channel)
	{
		if (!enabled || !connected)
		{
			messageSize = 0u;
			return null;
		}
		string arg;
		using (Game.Profile("ReadP2PPacket itself", log: true, 100f))
		{
			arg = THQNO_Wrapper.ReadP2PPacket(dest, destSize, out messageSize, channel);
		}
		if (Multiplayer.LogEnabled(2) && channel == 0)
		{
			Multiplayer.Log($"THQNO_Wrapper.ReadP2PPacket on channel {channel} from {arg} ({messageSize} bytes)", 2);
		}
		return arg;
	}

	public static THQNORequest CloseP2PChannelWithUser(string remoteId, uint channel)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"THQNO_Wrapper.CloseP2PChannelWithUser({remoteId}, {channel})", 2);
		}
		bool flag = THQNO_Wrapper.CloseP2PChannelWithUser(remoteId, channel);
		tHQNORequest.Completed(flag);
		return tHQNORequest;
	}

	public static void OnRequestP2PAddressResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnRequestP2PAddressResponse", 2);
		}
		if (requestp2paddress_req != null)
		{
			Value value = new Value(res);
			requestp2paddress_req.Completed(value);
			requestp2paddress_req = null;
		}
	}

	public static THQNORequest RequestCurrentStats()
	{
		if (requestcurrentstats_req != null)
		{
			return requestcurrentstats_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.RequestCurrentStats", 2);
		}
		requestcurrentstats_req = tHQNORequest;
		THQNO_Wrapper.RequestCurrentStats(THQNO_Callbacks.UserStatsReceivedCallback);
		return tHQNORequest;
	}

	public static void OnRequestCurrentStatsResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnRequestCurrentStatsResponse", 2);
		}
		if (requestcurrentstats_req != null)
		{
			Value value = new Value(res);
			requestcurrentstats_req.Completed(value);
			requestcurrentstats_req = null;
		}
	}

	public static THQNORequest StoreStats()
	{
		if (storestats_req != null)
		{
			return storestats_req;
		}
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (!user_stats_received)
		{
			UserStatsStoredCD userStatsStoredCD = new UserStatsStoredCD
			{
				result = Common.APIResult.Fail
			};
			tHQNORequest.Completed(new Value(userStatsStoredCD));
		}
		else
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.StoreStats", 2);
			}
			storestats_req = tHQNORequest;
			THQNO_Wrapper.StoreStats(THQNO_Callbacks.UserStatsStoredCallback);
		}
		return tHQNORequest;
	}

	public static void OnStoreStatsResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnStoreStatsResponse", 2);
		}
		if (storestats_req != null)
		{
			Value value = new Value(res);
			storestats_req.Completed(value);
			storestats_req = null;
		}
	}

	public static THQNORequest GetIntStat(string name)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (user_stats_received)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.GetIntStat(" + name + ", ...)", 2);
			}
			if (THQNO_Wrapper.GetIntStat(name, intPtr))
			{
				int val = Marshal.ReadInt32(intPtr);
				Marshal.FreeHGlobal(intPtr);
				tHQNORequest.Completed(new Value(val));
				return tHQNORequest;
			}
			Marshal.FreeHGlobal(intPtr);
			tHQNORequest.Completed(new Value(-1), "Unsuccessful get");
			return tHQNORequest;
		}
		tHQNORequest.Completed(new Value(-1), "User stats need to be received first");
		return tHQNORequest;
	}

	public static THQNORequest SetIntStat(string name, int data)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (user_stats_received)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.GetIntStat(" + name + ", ...)", 2);
			}
			bool val = THQNO_Wrapper.SetIntStat(name, data);
			tHQNORequest.Completed(new Value(val));
			return tHQNORequest;
		}
		tHQNORequest.Completed(new Value(val: false), "User stats need to be received first");
		return tHQNORequest;
	}

	public static THQNORequest GetAchievement(string name)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (user_stats_received)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.GetAchievement(" + name + ", ...)", 2);
			}
			if (THQNO_Wrapper.GetAchievement(name, intPtr))
			{
				bool val = Convert.ToBoolean(Marshal.ReadByte(intPtr));
				Marshal.FreeHGlobal(intPtr);
				tHQNORequest.Completed(new Value(val));
				return tHQNORequest;
			}
			Marshal.FreeHGlobal(intPtr);
			tHQNORequest.Completed(new Value(-1), "Unsuccessful get");
			return tHQNORequest;
		}
		tHQNORequest.Completed(new Value(-1), "User stats need to be received first");
		return tHQNORequest;
	}

	public static THQNORequest SetAchievement(string name)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (user_stats_received)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.SetAchievement(" + name + ")", 2);
			}
			bool val = THQNO_Wrapper.SetAchievement(name);
			tHQNORequest.Completed(new Value(val));
			return tHQNORequest;
		}
		tHQNORequest.Completed(new Value(val: false), "User stats need to be received first");
		return tHQNORequest;
	}

	public static THQNORequest ClearAchievement(string name)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (user_stats_received)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.ClearAchievement(" + name + ")", 2);
			}
			bool val = THQNO_Wrapper.ClearAchievement(name);
			tHQNORequest.Completed(new Value(val));
			return tHQNORequest;
		}
		tHQNORequest.Completed(new Value(val: false), "User stats need to be received first");
		return tHQNORequest;
	}

	public static THQNORequest GetPublicReadDocument(string documentName)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (getpublicdocumentread_req != null)
		{
			tHQNORequest.error = "Another GetPublicReadDocument in progress";
			return tHQNORequest;
		}
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetPublicReadDocument", 2);
		}
		getpublicdocumentread_req = tHQNORequest;
		THQNO_Wrapper.GetPublicReadDocument(documentName);
		return tHQNORequest;
	}

	public static void OnGetPublicReadDocumentResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnGetPublicReadDocumentResponse", 2);
		}
		if (getpublicdocumentread_req == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder(1024);
		StringBuilder stringBuilder2 = new StringBuilder(1024);
		StringBuilder stringBuilder3 = new StringBuilder(1024);
		StringBuilder stringBuilder4 = new StringBuilder(1024);
		GetPublicReadDocumentCD getPublicReadDocumentCD = (GetPublicReadDocumentCD)res;
		if (getPublicReadDocumentCD.result == Common.APIResult.Success)
		{
			uint publicReadDocumentNumEntries = THQNO_Wrapper.GetPublicReadDocumentNumEntries(getPublicReadDocumentCD.docResponse);
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log($"OnGetPublicReadDocumentResponse num entries: {publicReadDocumentNumEntries}", 2);
			}
			for (uint num = 0u; num < publicReadDocumentNumEntries; num++)
			{
				Common.ProjectDocumentResponseState publicReadDocumentState = THQNO_Wrapper.GetPublicReadDocumentState(getPublicReadDocumentCD.docResponse, num);
				if (publicReadDocumentState != Common.ProjectDocumentResponseState.NotFound)
				{
					THQNO_Wrapper.GetPublicReadDocumentData(getPublicReadDocumentCD.docResponse, num, stringBuilder, (uint)stringBuilder.Capacity, stringBuilder2, (uint)stringBuilder2.Capacity, stringBuilder3, (uint)stringBuilder3.Capacity, stringBuilder4, (uint)stringBuilder4.Capacity);
					continue;
				}
				string msg = $"OnGetPublicReadDocumentResponse, public read document state: {publicReadDocumentState}";
				if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log(msg, 2);
				}
				LogError(msg);
			}
		}
		else
		{
			string msg2 = $"OnGetPublicReadDocumentResponse result: {getPublicReadDocumentCD.result}";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg2, 2);
			}
			LogError(msg2);
		}
		if (stringBuilder3 == null)
		{
			LogError("OnGetPublicReadDocumentResponse string builder contentSb is null");
		}
		string text = stringBuilder3?.ToString();
		if (string.IsNullOrEmpty(text))
		{
			LogError("OnGetPublicReadDocumentResponse result_string is empty");
		}
		Value value = new Value(text);
		getpublicdocumentread_req.Completed(value);
		getpublicdocumentread_req = null;
	}

	public static THQNORequest StartPasswordRecovery(string contactIdentifier)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (startpasswordrecovery_req != null)
		{
			tHQNORequest.error = "Another StartPasswordRecovery in progress";
			return tHQNORequest;
		}
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.StartPasswordRecovery", 2);
		}
		startpasswordrecovery_req = tHQNORequest;
		THQNO_Wrapper.StartPasswordRecovery(contactIdentifier);
		return tHQNORequest;
	}

	public static void OnStartPasswordRecoveryResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnStartPasswordRecoveryResponse", 2);
		}
		if (startpasswordrecovery_req != null)
		{
			Value value = new Value(res);
			startpasswordrecovery_req.Completed(value);
			startpasswordrecovery_req = null;
		}
	}

	public static THQNORequest ConfirmPasswordRecovery(string newPassword, string verificationCode)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (confirmpasswordrecovery_req != null)
		{
			tHQNORequest.error = "Another ConfirmPasswordRecovery in progress";
			return tHQNORequest;
		}
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.ConfirmPasswordRecovery", 2);
		}
		confirmpasswordrecovery_req = tHQNORequest;
		THQNO_Wrapper.ConfirmPasswordRecovery(newPassword, verificationCode);
		return tHQNORequest;
	}

	public static void OnConfirmPasswordRecoveryResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnConfirmPasswordRecoveryResponse", 2);
		}
		if (confirmpasswordrecovery_req != null)
		{
			Value value = new Value(res);
			confirmpasswordrecovery_req.Completed(value);
			confirmpasswordrecovery_req = null;
		}
	}

	public static void OnCheckEmailResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnCheckEmailResponse", 2);
		}
		if (checkemail_req != null)
		{
			Value value = new Value(res);
			checkemail_req.Completed(value);
			checkemail_req = null;
		}
	}

	public static void OnCheckPhoneResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnCheckPhoneResponse", 2);
		}
		if (checkphone_req != null)
		{
			Value value = new Value(res);
			checkphone_req.Completed(value);
			checkphone_req = null;
		}
	}

	public static void OnVerifyEmailResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnVerifyEmailResponse", 2);
		}
		if (verifyemail_req != null)
		{
			Value value = new Value(res);
			verifyemail_req.Completed(value);
			verifyemail_req = null;
		}
	}

	public static void OnVerifyPhoneResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnVerifyPhoneResponsee", 2);
		}
		if (verifyphone_req != null)
		{
			Value value = new Value(res);
			verifyphone_req.Completed(value);
			verifyphone_req = null;
		}
	}

	public static void OnUpdateEmailResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnUpdateEmailResponse", 2);
		}
		if (updateemail_req != null)
		{
			Value value = new Value(res);
			updateemail_req.Completed(value);
			updateemail_req = null;
		}
	}

	public static void OnUpdatePhoneResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnUpdatePhoneResponse", 2);
		}
		if (updatephone_req != null)
		{
			Value value = new Value(res);
			updatephone_req.Completed(value);
			updatephone_req = null;
		}
	}

	public static void OnResetPasswordResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnResetPasswordResponse", 2);
		}
		if (resetpassword_req != null)
		{
			Value value = new Value(res);
			resetpassword_req.Completed(value);
			resetpassword_req = null;
		}
	}

	public static void OnDeleteAccountResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnDeleteAccountResponse", 2);
		}
		if (deleteaccount_req != null)
		{
			Value value = new Value(res);
			deleteaccount_req.Completed(value);
			deleteaccount_req = null;
		}
	}

	public static IEnumerator CanPlayerBeInvitedCoro(string userId)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetUserPlatformInformationCoro", 2);
		}
		yield return GetUserPlatformInformation(userId);
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "GetUserPlatformInformationCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		GetUserPlatformInformationResultCD getUserPlatformInformationResultCD = (GetUserPlatformInformationResultCD)Coroutine.Result.obj_val;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"GetUserPlatformInformationCoro: Primary platform: {getUserPlatformInformationResultCD.primaryPlatform}, canBeInvited: {getUserPlatformInformationResultCD.canBeInvited}", 2);
		}
		if (getUserPlatformInformationResultCD.result == Common.APIResult.Success)
		{
			yield return Coroutine.Return(new Value(getUserPlatformInformationResultCD.canBeInvited));
		}
	}

	public static THQNORequest GetUserPlatformInformation(string userId)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetUserPlatformInformation", 2);
		}
		return new THQNORequest(THQNO_Wrapper.GetUserPlatformInformation(THQNO_Callbacks.UserPlatformInformationResult, userId));
	}

	public static THQNORequest PlayerDataSetCustomData(string data)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.PlayerDataSetCustomData", 2);
		}
		return new THQNORequest(THQNO_Wrapper.PlayerDataSetCustomData(THQNO_Callbacks.PlayerDataSetCustomDataResultCallback, data));
	}

	public static THQNORequest PlayerDataGetCustomData(string userId, bool log = true)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2) && log)
		{
			Multiplayer.Log("THQNO_Wrapper.PlayerDataGetCustomData", 2);
		}
		return new THQNORequest(THQNO_Wrapper.PlayerDataGetCustomData(THQNO_Callbacks.PlayerDataGetCustomDataResultCallback, userId));
	}

	public static THQNORequest PlayerDataGetPersonalData(string userId, bool log = true)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2) && log)
		{
			Multiplayer.Log("THQNO_Wrapper.PlayerDataGetPersonalData", 2);
		}
		return new THQNORequest(THQNO_Wrapper.PlayerDataGetPersonalData(THQNO_Callbacks.PlayerDataGetPersonalDataResultCallback, userId));
	}

	public static THQNORequest PlayerDataSetPersonalPlayerName(string playerName)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.PlayerDataSetPersonalPlayerName", 2);
		}
		return new THQNORequest(THQNO_Wrapper.PlayerDataSetPersonalPlayerName(THQNO_Callbacks.PlayerDataSetPersonalPlayerNameResultCallback, playerName));
	}

	public static void OnJoinRoomResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnJoinRoomResponse", 2);
		}
		if (joinroom_req != null)
		{
			Value value = new Value(res);
			joinroom_req.Completed(value);
			joinroom_req = null;
		}
	}

	public static void OnLeaveRoomResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnLeaveRoomResponse", 2);
		}
		if (leaveroom_req != null)
		{
			Value value = new Value(res);
			leaveroom_req.Completed(value);
			leaveroom_req = null;
		}
	}

	public static void OnChatRoomMessageResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnChatRoomMessageResponse", 2);
		}
		if (sendroommessage_req != null)
		{
			Value value = new Value(res);
			sendroommessage_req.Completed(value);
			sendroommessage_req = null;
		}
	}

	public static THQNORequest GetSteamBetaName(StringBuilder buffer, uint bufferSize)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!initted)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetSteamBetaName", 2);
		}
		bool steamBetaName = THQNO_Wrapper.GetSteamBetaName(buffer, bufferSize);
		tHQNORequest.Completed(steamBetaName);
		return tHQNORequest;
	}

	public static THQNORequest GetPlatformType()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetPlatformType", 2);
		}
		Common.PlatformType platformType = THQNO_Wrapper.GetPlatformType();
		tHQNORequest.Completed((int)platformType);
		return tHQNORequest;
	}

	public static THQNORequest GetPlatformPlayerName()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		try
		{
			if (!enabled)
			{
				return tHQNORequest;
			}
			if (!connected)
			{
				return tHQNORequest;
			}
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log("THQNO_Wrapper.GetPlatformPlayerName", 2);
			}
			string platformPlayerName = THQNO_Wrapper.GetPlatformPlayerName();
			tHQNORequest.Completed(platformPlayerName);
		}
		catch (Exception ex)
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(ex.ToString(), 2);
			}
		}
		return tHQNORequest;
	}

	public static THQNORequest SetOverlayNotificationPosition(Common.OverlayNotificationPosition notificationPosition)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.SetOverlayNotificationPosition", 2);
		}
		THQNO_Wrapper.SetOverlayNotificationPosition(notificationPosition);
		return tHQNORequest;
	}

	public static THQNORequest ActivateGameOverlay(Common.OverlayMode mode)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.ActivateGameOverlay", 2);
		}
		THQNO_Wrapper.ActivateGameOverlay(mode);
		return tHQNORequest;
	}

	public static THQNORequest ActivateInviteOverlay()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.ActivateInviteOverlay", 2);
		}
		THQNO_Wrapper.ActivateInviteOverlay();
		return tHQNORequest;
	}

	public static THQNORequest ActivateGameOverlayToUser(Common.UserOverlayMode mode, string userId)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.ActivateGameOverlayToUser", 2);
		}
		THQNO_Wrapper.ActivateGameOverlayToUser(mode, userId);
		return tHQNORequest;
	}

	public static THQNORequest ActivateGameOverlayToWebPage(string url)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.ActivateGameOverlayToWebPage", 2);
		}
		THQNO_Wrapper.ActivateGameOverlayToWebPage(url);
		return tHQNORequest;
	}

	public static void OnDownloadItemResponse(object res)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnDownloadItemResponse", 2);
		}
		if (downloaditem_req != null)
		{
			Value value = new Value(res);
			downloaditem_req.Completed(value);
			downloaditem_req = null;
		}
	}

	public static THQNORequest GetNumSubscribedItems()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetNumSubscribedItems", 2);
		}
		uint numSubscribedItems = THQNO_Wrapper.GetNumSubscribedItems();
		tHQNORequest.Completed(numSubscribedItems);
		return tHQNORequest;
	}

	public static THQNORequest GetSubscribedItems()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetSubscribedItems", 2);
		}
		int num = 1000;
		long[] array = new long[num];
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(array[0]) * array.Length);
		int subscribedItems = (int)THQNO_Wrapper.GetSubscribedItems(intPtr, (uint)num);
		Marshal.Copy(intPtr, array, 0, subscribedItems);
		List<ulong> list = new List<ulong>(subscribedItems);
		for (int i = 0; i < subscribedItems; i++)
		{
			list.Add((ulong)array[i]);
		}
		Marshal.FreeHGlobal(intPtr);
		tHQNORequest.Completed(new Value(list));
		return tHQNORequest;
	}

	public static THQNORequest GetItemState(ulong publishedFileID)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetItemState", 2);
		}
		uint itemState = THQNO_Wrapper.GetItemState(publishedFileID);
		tHQNORequest.Completed(itemState);
		return tHQNORequest;
	}

	public static THQNORequest GetItemInstallInfo(ulong publishedFileID)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetItemInstallInfo", 2);
		}
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ulong)));
		StringBuilder stringBuilder = new StringBuilder(128);
		IntPtr intPtr2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)));
		bool itemInstallInfo = THQNO_Wrapper.GetItemInstallInfo(publishedFileID, intPtr, stringBuilder, 128u, intPtr2);
		tHQNORequest.Completed(itemInstallInfo ? stringBuilder.ToString() : string.Empty);
		Marshal.FreeHGlobal(intPtr);
		Marshal.FreeHGlobal(intPtr2);
		return tHQNORequest;
	}

	public static THQNORequest GetFriendCount()
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetFriendCount", 2);
		}
		uint num = 0u;
		Multiplayer.Error("GetFriendCount not implemented!");
		tHQNORequest.Completed(num);
		return tHQNORequest;
	}

	public static THQNORequest GetFriendByIndex(uint index)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetFriendByIndex", 2);
		}
		string text = "";
		Multiplayer.Error("GetFriendByIndex not implemented!");
		tHQNORequest.Completed(text);
		return tHQNORequest;
	}

	public static THQNORequest GetFriendPersonaName(string userId, StringBuilder output, uint outputBufferSize)
	{
		THQNORequest tHQNORequest = new THQNORequest(0u);
		if (!enabled)
		{
			return tHQNORequest;
		}
		if (!connected)
		{
			return tHQNORequest;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO_Wrapper.GetFriendPersonaName", 2);
		}
		bool flag = false;
		Multiplayer.Error("GetFriendPersonaName not implemented!");
		tHQNORequest.Completed(flag);
		return tHQNORequest;
	}

	public static IEnumerator ConnectCoro()
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return Connect();
		if (connected)
		{
			THQNORequest req = GetPlatformType();
			yield return req;
			platformType = (Common.PlatformType)(int)req.result;
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log($"Platform Type: {platformType}", 2);
			}
		}
	}

	public static IEnumerator RegisterEmailPWCoro(string email, string password, bool showActivationLinkInEmail, bool showActivationCodeInEmail)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return RegisterEmailPW("", email, password, showActivationLinkInEmail, showActivationCodeInEmail);
		RegisterResultCD registerResultCD = (RegisterResultCD)Coroutine.Result.obj_val;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"RegisterEmailPWCoro Result: {registerResultCD.result}", 2);
		}
		yield return Coroutine.Return(Coroutine.Result);
	}

	public static IEnumerator SignInWithCredentialsFile()
	{
		Utility.ReadTHQNOCredentials(out var email, out var password);
		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			yield return Coroutine.Return(Value.Unknown, "Error reading credentials from file");
		}
		yield return SignInEmailPWCoro(email, password);
		yield return Coroutine.Return(Coroutine.Result, Coroutine.Error);
	}

	public static IEnumerator SignInEmailPWCoro(string email, string password)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return SignInEmailPW(email, password);
		SignInResultCD signInResultCD = (SignInResultCD)Coroutine.Result.obj_val;
		Value value = Coroutine.Result;
		if (signInResultCD.result == Common.APIResult.Success)
		{
			userId = signInResultCD.userId;
			networkingAvailable = signInResultCD.networkingAvaliable;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"SignInEmailPWCoro Result: {signInResultCD.result}, User id: {signInResultCD.userId}, Networking available: {signInResultCD.networkingAvaliable}", 2);
		}
		yield return Coroutine.Return(value);
	}

	public static IEnumerator SignInPlatformCoro()
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return SignInPlatform();
		SignInResultCD signInResultCD = (SignInResultCD)Coroutine.Result.obj_val;
		if (signInResultCD.result == Common.APIResult.Success)
		{
			userId = signInResultCD.userId;
			networkingAvailable = signInResultCD.networkingAvaliable;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"SignInPlatformCoro Result: {signInResultCD.result}, User id: {signInResultCD.userId}, Networking available: {signInResultCD.networkingAvaliable}", 2);
		}
	}

	public static IEnumerator SignOutCoro()
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return SignOut();
		Value value = Coroutine.Result;
		yield return Coroutine.Return(value);
	}

	public static IEnumerator CreateLobbyCoro(Common.LobbyType lobbyType, int maxPlayersCount, string name, bool isJoinable)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return CreateLobby(lobbyType, maxPlayersCount, name, isJoinable);
		LobbyCreatedCD lobbyCreatedCD = (LobbyCreatedCD)Coroutine.Result.obj_val;
		if (lobbyCreatedCD.result == Common.APIResult.Success)
		{
			currentlyEnteredTHQNOLobbyId = lobbyCreatedCD.lobbyId;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"CreateLobbyCoro Result: {lobbyCreatedCD.result}, Lobby id: {lobbyCreatedCD.lobbyId}", 2);
		}
	}

	public static IEnumerator JoinLobbyCoro(string lobby_id)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return JoinLobby(lobby_id);
		LobbyEnterCD lobbyEnterCD = (LobbyEnterCD)Coroutine.Result.obj_val;
		if (lobbyEnterCD.result == Common.APIResult.Success)
		{
			currentlyEnteredTHQNOLobbyId = lobbyEnterCD.lobbyId;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"JoinLobbyCoro Result: {lobbyEnterCD.result}, Lobby id: {lobbyEnterCD.lobbyId}", 2);
		}
	}

	public static IEnumerator LeaveCurrentLobbyCoro()
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return LeaveCurrentLobby();
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "LeaveCurrentLobbyCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		LobbyLeaveCD lobbyLeaveCD = (LobbyLeaveCD)Coroutine.Result.obj_val;
		currentlyEnteredTHQNOLobbyId = string.Empty;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"LeaveCurrentLobbyCoro: Reason: {lobbyLeaveCD.reason}, Lobby id: {lobbyLeaveCD.lobbyId}, Member changed id: {lobbyLeaveCD.memberChangedId}, Member making change: {lobbyLeaveCD.memberIdMakingChange}", 2);
		}
	}

	public static IEnumerator RequestLobbyListCoro(bool requestLobbyContent)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return RequestLobbyList(requestLobbyContent);
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "RequestLobbyListCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		LobbyListReceivedCD lobbyListReceivedCD = (LobbyListReceivedCD)Coroutine.Result.obj_val;
		string[] array = lobbyListReceivedCD.lobbyIds.Split(';');
		if (lobbyListReceivedCD.result == Common.APIResult.Success)
		{
			List<string> list = new List<string>();
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
			array = list.ToArray();
		}
		yield return Coroutine.Return(new Value(array));
	}

	public static IEnumerator RequestLobbyDataCoro(string lobbyId)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return RequestLobbyData(lobbyId);
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "RequestLobbyDataCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		LobbyDataUpdateCD lobbyDataUpdateCD = (LobbyDataUpdateCD)Coroutine.Result.obj_val;
		if (Multiplayer.LogEnabled(2) && lobbyDataUpdateCD.result == Common.APIResult.Success)
		{
			Multiplayer.Log($"RequestLobbyDataCoro Result: {lobbyDataUpdateCD.result}, Lobby Id: {lobbyDataUpdateCD.lobbyId}", 2);
		}
		yield return Coroutine.Return(new Value(lobbyDataUpdateCD));
	}

	public static IEnumerator GetLobbyOwnerIdCoro(string lobbyId)
	{
		Coroutine.TerminateOnError();
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return RequestLobbyDataCoro(lobbyId);
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "GetLobbyOwnerIdCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		if (((LobbyDataUpdateCD)Coroutine.Result.obj_val).result == Common.APIResult.Success)
		{
			yield return GetLobbyOwner(lobbyId);
			string val = (string)Coroutine.Result.obj_val;
			yield return Coroutine.Return(new Value(val));
		}
	}

	public static IEnumerator PlayerDataGetCustomDataCoro(string user_id)
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return PlayerDataGetCustomData(user_id);
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "PlayerDataGetCustomDataCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		PlayerDataGetCustomDataResultCD playerDataGetCustomDataResultCD = (PlayerDataGetCustomDataResultCD)Coroutine.Result.obj_val;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"PlayerDataGetCustomDataCoro: User id: {playerDataGetCustomDataResultCD.userId}, Player data length: {playerDataGetCustomDataResultCD.customDataLength}", 2);
		}
		if (playerDataGetCustomDataResultCD.result == Common.APIResult.Success)
		{
			yield return Coroutine.Return(new Value(playerDataGetCustomDataResultCD.customData));
		}
	}

	public static IEnumerator GetOwnPlayerNameCoro()
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		if (platformType != Common.PlatformType.THQNO && platformType != Common.PlatformType.NoDRM)
		{
			THQNORequest req = GetPlatformPlayerName();
			yield return req;
			string text = req.result.String();
			if (string.IsNullOrEmpty(text))
			{
				text = platformType.ToString() + " " + userId;
			}
			yield return Coroutine.Return(text);
		}
		yield return PlayerDataGetPersonalData(userId);
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "GetOwnPlayerNameCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		PlayerDataGetPersonalDataResultCD playerDataGetPersonalDataResultCD = (PlayerDataGetPersonalDataResultCD)Coroutine.Result.obj_val;
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"GetOwnPlayerNameCoro: User id: {playerDataGetPersonalDataResultCD.userId}, Player name: {playerDataGetPersonalDataResultCD.playerName}, Player name length: {playerDataGetPersonalDataResultCD.playerNameLength}", 2);
		}
		if (playerDataGetPersonalDataResultCD.result == Common.APIResult.Success)
		{
			yield return Coroutine.Return(new Value(playerDataGetPersonalDataResultCD.playerName));
		}
	}

	public static IEnumerator RequestCurrentStatsCoro()
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return RequestCurrentStats();
		if (Coroutine.Result.obj_val == null)
		{
			string msg = "RequestCurrentStatsCoro: Coroutine.Result.obj_val is null";
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log(msg, 2);
			}
			yield return Coroutine.Return(default(Value), msg);
		}
		if (((UserStatsReceivedCD)Coroutine.Result.obj_val).result == Common.APIResult.Success)
		{
			user_stats_received = true;
		}
	}

	public static IEnumerator StoreStatsCoro()
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		yield return StoreStats();
	}

	public static IEnumerator SetStatCoro(string name, int val)
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		if (!user_stats_received)
		{
			yield return RequestCurrentStatsCoro();
		}
		THQNORequest req = SetIntStat(name, val);
		yield return req;
		if (req.error != null)
		{
			Multiplayer.Warning("SetIntStat " + name + " unsuccessful! Error: " + req.error);
		}
		else
		{
			yield return StoreStatsCoro();
		}
		yield return Coroutine.Return(req.result, req.error);
	}

	public static IEnumerator IncStatCoro(string name, int val)
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		if (!user_stats_received)
		{
			yield return RequestCurrentStatsCoro();
		}
		while (incStatInProgress)
		{
			yield return null;
		}
		incStatInProgress = true;
		THQNORequest req = GetIntStat(name);
		yield return req;
		if (req.error != null)
		{
			Multiplayer.Warning("GetIntStat " + name + " unsuccessful! Error: " + req.error);
		}
		int int_val = req.result.int_val;
		req = SetIntStat(name, int_val + val);
		yield return req;
		if (req.error != null)
		{
			Multiplayer.Warning("SetIntStat " + name + " unsuccessful! Error: " + req.error);
		}
		else
		{
			yield return StoreStatsCoro();
		}
		incStatInProgress = false;
		yield return Coroutine.Return(req.result, req.error);
	}

	public static IEnumerator SetAchievementCoro(string name)
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		if (!user_stats_received)
		{
			yield return RequestCurrentStatsCoro();
		}
		THQNORequest req = SetAchievement(name);
		yield return req;
		if (req.error != null)
		{
			Multiplayer.Warning("SetAchievement " + name + " unsuccessful! Error: " + req.error);
		}
		else
		{
			yield return StoreStatsCoro();
		}
		yield return Coroutine.Return(req.result, req.error);
	}

	public static IEnumerator ClearAchievementCoro(string name)
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		if (!user_stats_received)
		{
			yield return RequestCurrentStatsCoro();
		}
		THQNORequest req = ClearAchievement(name);
		yield return req;
		if (req.error != null)
		{
			Multiplayer.Warning("ClearAchievement " + name + " unsuccessful! Error: " + req.error);
		}
		else
		{
			yield return StoreStatsCoro();
		}
		yield return Coroutine.Return(req.result, req.error);
	}

	public static IEnumerator CheckAchievementCoro(string name)
	{
		if (!enabled)
		{
			yield return Coroutine.Return(default(Value), "THQNO disabled!");
		}
		if (!user_stats_received)
		{
			yield return RequestCurrentStatsCoro();
		}
		THQNORequest req = GetAchievement(name);
		yield return req;
		if (req.error != null)
		{
			Multiplayer.Warning("GetAchievement " + name + " unsuccessful! Error: " + req.error);
		}
		else if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"CheckAchievementCoro: {name} = {req.result.Bool()}", 2);
		}
		yield return Coroutine.Return(req.result, req.error);
	}

	public static void RegisterMultiplayer(Multiplayer multiplayer)
	{
		lock (MultiplayersLock)
		{
			if (multiplayer != null && multiplayers != null)
			{
				if (!multiplayers.Contains(multiplayer))
				{
					multiplayers.Add(multiplayer);
				}
				else
				{
					Multiplayer.Error($"{multiplayer} is already registered to THQNORequest");
				}
			}
		}
	}

	public static void UnregisterMultiplayer(Multiplayer multiplayer)
	{
		lock (MultiplayersLock)
		{
			if (multiplayer != null && multiplayers != null)
			{
				if (multiplayers.Contains(multiplayer))
				{
					multiplayers.Remove(multiplayer);
				}
				else
				{
					Multiplayer.Error($"{multiplayer} is not registered to THQNORequest");
				}
			}
		}
	}

	public static List<Multiplayer> GetAllMultiplayers()
	{
		return multiplayers;
	}

	private void Completed(Value result, string error = null)
	{
		if (req_id != 0)
		{
			pending.Remove(req_id);
		}
		this.result = result;
		this.error = error;
		completed = true;
	}

	public static void OnResponse(uint req_id, object result, string error = null)
	{
		if (pending.TryGetValue(req_id, out var value))
		{
			value.Completed(new Value(result), error);
		}
	}

	public static string THQNOConfigPath()
	{
		return Application.dataPath + "\\StreamingAssets\\THQNO";
	}

	public static void OnDebugLogCallback(IntPtr request)
	{
		if (Multiplayer.LogEnabled(2))
		{
			string text = Marshal.PtrToStringAnsi(request);
			int num = text.IndexOfAny(new_line_characters);
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			Multiplayer.Log("THQNODBG: " + text, 2);
		}
	}

	public static void Log(string msg)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("THQNO: " + msg, 2);
		}
	}

	public static void LogError(string msg)
	{
		Multiplayer.Error("THQNO: " + msg);
	}
}
