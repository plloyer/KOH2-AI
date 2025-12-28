using System;
using System.Runtime.InteropServices;

namespace Logic;

public static class THQNO_Callbacks
{
	public static void RegisterCallbacks()
	{
		THQNO_Wrapper.THQNORegisterCallback(OnlineServicesSignOutCallback, Common.CallbackType.OnlineServicesSignOut);
		THQNO_Wrapper.THQNORegisterCallback(RegisterResultCallback, Common.CallbackType.RegisterResult);
		THQNO_Wrapper.THQNORegisterCallback(RegisterPlatformResultCallback, Common.CallbackType.RegisterPlatformResult);
		THQNO_Wrapper.THQNORegisterCallback(SignInResultCallback, Common.CallbackType.SignInResult);
		THQNO_Wrapper.THQNORegisterCallback(CheckEmailResultCallback, Common.CallbackType.CheckEmailResult);
		THQNO_Wrapper.THQNORegisterCallback(CheckPhoneResultCallback, Common.CallbackType.CheckPhoneResult);
		THQNO_Wrapper.THQNORegisterCallback(UpdatePhoneResultCallback, Common.CallbackType.UpdatePhoneResult);
		THQNO_Wrapper.THQNORegisterCallback(UpdateEmailResultCallback, Common.CallbackType.UpdateEmailResult);
		THQNO_Wrapper.THQNORegisterCallback(VerifyPhoneResultCallback, Common.CallbackType.VerifyPhoneResult);
		THQNO_Wrapper.THQNORegisterCallback(VerifyEmailResultCallback, Common.CallbackType.VerifyEmailResult);
		THQNO_Wrapper.THQNORegisterCallback(ResetPasswordResultCallback, Common.CallbackType.ResetPasswordResult);
		THQNO_Wrapper.THQNORegisterCallback(DeleteAccountResultCallback, Common.CallbackType.DeleteAccountResult);
		THQNO_Wrapper.THQNORegisterCallback(PasswordRecoveryStartResultCallback, Common.CallbackType.PasswordRecoveryStartResult);
		THQNO_Wrapper.THQNORegisterCallback(PasswordRecoveryResultCallback, Common.CallbackType.PasswordRecoveryResult);
		THQNO_Wrapper.THQNORegisterCallback(LobbyLeaveCallback, Common.CallbackType.LobbyLeave);
		THQNO_Wrapper.THQNORegisterCallback(LobbyInviteCallback, Common.CallbackType.LobbyInvite);
		THQNO_Wrapper.THQNORegisterCallback(LobbyDataUpdateCallback, Common.CallbackType.LobbyDataUpdate);
		THQNO_Wrapper.THQNORegisterCallback(LobbyMemberDataUpdateCallback, Common.CallbackType.LobbyMemberDataUpdate);
		THQNO_Wrapper.THQNORegisterCallback(ChatRoomJoinResultCallback, Common.CallbackType.ChatRoomJoinResult);
		THQNO_Wrapper.THQNORegisterCallback(ChatRoomLeftCallback, Common.CallbackType.ChatRoomLeft);
		THQNO_Wrapper.THQNORegisterCallback(ChatRoomMessageCallback, Common.CallbackType.ChatRoomMessage);
		THQNO_Wrapper.THQNORegisterCallback(P2PSessionConnectFailCallback, Common.CallbackType.P2PSessionConnectFail);
		THQNO_Wrapper.THQNORegisterCallback(P2PSessionRequestCallback, Common.CallbackType.P2PSessionRequest);
		THQNO_Wrapper.THQNORegisterCallback(P2PAddressUpdateCallback, Common.CallbackType.P2PAddressUpdate);
		THQNO_Wrapper.THQNORegisterCallback(AutoMatchmakingJoinResultCallback, Common.CallbackType.AutoMatchmakingJoinResult);
		THQNO_Wrapper.THQNORegisterCallback(ReportActiveMatchResultCallback, Common.CallbackType.ReportActiveMatchResult);
		THQNO_Wrapper.THQNORegisterCallback(RequestActiveMatchResultCallback, Common.CallbackType.RequestActiveMatchResult);
		THQNO_Wrapper.THQNORegisterCallback(ActiveMatchHeartbeatCallback, Common.CallbackType.RequestActiveMatchHeartbeatResult);
		THQNO_Wrapper.THQNORegisterCallback(AuthSessionTicketResultCallback, Common.CallbackType.AuthSessionTicketResult);
		THQNO_Wrapper.THQNORegisterCallback(DownloadItemResultCallback, Common.CallbackType.DownloadItemResult);
		THQNO_Wrapper.THQNORegisterCallback(PlayerStatsResultCallback, Common.CallbackType.PlayerStats);
		THQNO_Wrapper.THQNORegisterCallback(ControlledStatsResultCallback, Common.CallbackType.ControlledStats);
		THQNO_Wrapper.THQNORegisterCallback(DownloadLeaderboardResultCallback, Common.CallbackType.LeaderboardDownloaded);
		THQNO_Wrapper.THQNORegisterCallback(PlayerDataGetCustomDataResponseResult, Common.CallbackType.PlayerDataGetCustomDataResponseResult);
		THQNO_Wrapper.THQNORegisterCallback(PublicReadDocumentResult, Common.CallbackType.Project_PublicReadDocument);
		THQNO_Wrapper.THQNORegisterCallback(GetAutoMatchmakingQueueInfo, Common.CallbackType.GetAutoMatchmakingQueueInfo);
	}

	public static void UnregisterCallbacks()
	{
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.OnlineServicesSignOut);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.RegisterResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.RegisterPlatformResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.SignInResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.CheckEmailResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.CheckPhoneResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.UpdatePhoneResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.UpdateEmailResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.VerifyPhoneResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.VerifyEmailResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.ResetPasswordResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.DeleteAccountResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.PasswordRecoveryStartResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.PasswordRecoveryResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.LobbyLeave);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.LobbyInvite);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.LobbyDataUpdate);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.LobbyMemberDataUpdate);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.ChatRoomJoinResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.ChatRoomLeft);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.ChatRoomMessage);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.P2PSessionConnectFail);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.P2PSessionRequest);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.P2PAddressUpdate);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.AutoMatchmakingJoinResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.ReportActiveMatchResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.RequestActiveMatchResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.RequestActiveMatchHeartbeatResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.AuthSessionTicketResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.DownloadItemResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.PlayerStats);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.ControlledStats);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.LeaderboardDownloaded);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.PlayerDataGetCustomDataResponseResult);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.Project_PublicReadDocument);
		THQNO_Wrapper.THQNOUnregisterCallback(Common.CallbackType.GetAutoMatchmakingQueueInfo);
	}

	public static void OnlineServicesSignOutCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("OnlineServicesSignOutCallback", 2);
		}
		THQNORequest.OnSignOutResponse(Marshal.PtrToStructure<OnlineServicesSignOutCD>(param));
	}

	public static void RegisterResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("RegisterResultCallback", 2);
		}
		THQNORequest.OnRegisterResponse(Marshal.PtrToStructure<RegisterResultCD>(param));
	}

	public static void RegisterPlatformResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("RegisterPlatformResultCallback", 2);
		}
		THQNORequest.OnRegisterResponse(Marshal.PtrToStructure<RegisterPlatformResultCD>(param));
	}

	public static void SignInResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("SignInResultCallback", 2);
		}
		THQNORequest.OnSignInResponse(Marshal.PtrToStructure<SignInResultCD>(param));
	}

	public static void PublicReadDocumentResult(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PublicReadDocumentResult", 2);
		}
		THQNORequest.OnGetPublicReadDocumentResponse(Marshal.PtrToStructure<GetPublicReadDocumentCD>(param));
	}

	public static void CheckEmailResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("CheckEmailResultCallback", 2);
		}
		THQNORequest.OnCheckEmailResponse(Marshal.PtrToStructure<CheckEmailResultCD>(param));
	}

	public static void CheckPhoneResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("CheckPhoneResultCallback", 2);
		}
		THQNORequest.OnCheckPhoneResponse(Marshal.PtrToStructure<CheckPhoneResultCD>(param));
	}

	public static void UpdatePhoneResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("UpdatePhoneResultCallback", 2);
		}
		THQNORequest.OnUpdatePhoneResponse(Marshal.PtrToStructure<UpdatePhoneResultCD>(param));
	}

	public static void UpdateEmailResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("UpdateEmailResultCallback", 2);
		}
		THQNORequest.OnUpdateEmailResponse(Marshal.PtrToStructure<UpdateEmailResultCD>(param));
	}

	public static void VerifyPhoneResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("VerifyPhoneResultCallback", 2);
		}
		THQNORequest.OnVerifyPhoneResponse(Marshal.PtrToStructure<VerifyPhoneResultCD>(param));
	}

	public static void VerifyEmailResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("VerifyEmailResultCallback", 2);
		}
		THQNORequest.OnVerifyEmailResponse(Marshal.PtrToStructure<VerifyEmailResultCD>(param));
	}

	public static void ResetPasswordResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ResetPasswordResultCallback", 2);
		}
		THQNORequest.OnResetPasswordResponse(Marshal.PtrToStructure<ResetPasswordResultCD>(param));
	}

	public static void DeleteAccountResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("DeleteAccountResultCallback", 2);
		}
		THQNORequest.OnDeleteAccountResponse(Marshal.PtrToStructure<DeleteAccountResultCD>(param));
	}

	public static void PasswordRecoveryStartResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PasswordRecoveryStartResultCallback", 2);
		}
		THQNORequest.OnStartPasswordRecoveryResponse(Marshal.PtrToStructure<PasswordRecoveryStartResultCD>(param));
	}

	public static void PasswordRecoveryResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PasswordRecoveryResultCallback", 2);
		}
		THQNORequest.OnConfirmPasswordRecoveryResponse(Marshal.PtrToStructure<PasswordRecoveryResultCD>(param));
	}

	public static void LobbyListReceivedCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyListReceivedCallback", 2);
		}
		LobbyListReceivedCD lobbyListReceivedCD = Marshal.PtrToStructure<LobbyListReceivedCD>(param);
		THQNORequest.OnResponse(lobbyListReceivedCD.requestId, lobbyListReceivedCD);
	}

	public static void LobbyCreatedCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyCreatedCallback", 2);
		}
		LobbyCreatedCD lobbyCreatedCD = Marshal.PtrToStructure<LobbyCreatedCD>(param);
		THQNORequest.OnResponse(lobbyCreatedCD.requestId, lobbyCreatedCD);
	}

	public static void LobbyEnterCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyEnterCallback", 2);
		}
		LobbyEnterCD lobbyEnterCD = Marshal.PtrToStructure<LobbyEnterCD>(param);
		THQNORequest.OnResponse(lobbyEnterCD.requestId, lobbyEnterCD);
	}

	public static void LobbyLeaveCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyLeaveCallback", 2);
		}
		THQNORequest.OnLeaveLobbyCallback(Marshal.PtrToStructure<LobbyLeaveCD>(param));
	}

	public static void LobbyInviteCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyInviteCallback", 2);
		}
		THQNORequest.OnInviteUserToLobbyResponse(Marshal.PtrToStructure<LobbyInviteCD>(param));
	}

	public static void LobbyDataUpdateCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyDataUpdateCallback", 2);
		}
		THQNORequest.OnRequestLobbyData(Marshal.PtrToStructure<LobbyDataUpdateCD>(param));
	}

	public static void LobbyMemberDataUpdateCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("LobbyMemberDataUpdateCallback", 2);
		}
		Marshal.PtrToStructure<LobbyMemberDataUpdateCD>(param);
	}

	public static void SetLobbyOwnerResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("SetLobbyOwnerResultCallback", 2);
		}
		SetLobbyOwnerResultCD setLobbyOwnerResultCD = Marshal.PtrToStructure<SetLobbyOwnerResultCD>(param);
		THQNORequest.OnResponse(setLobbyOwnerResultCD.requestId, setLobbyOwnerResultCD);
	}

	public static void SetLobbyJoinableCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("SetLobbyJoinableCallback", 2);
		}
		SetLobbyJoinableResultCD setLobbyJoinableResultCD = Marshal.PtrToStructure<SetLobbyJoinableResultCD>(param);
		THQNORequest.OnResponse(setLobbyJoinableResultCD.requestId, setLobbyJoinableResultCD);
	}

	public static void SetLobbyTypeCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("SetLobbyTypeCallback", 2);
		}
		SetLobbyTypeResultCD setLobbyTypeResultCD = Marshal.PtrToStructure<SetLobbyTypeResultCD>(param);
		THQNORequest.OnResponse(setLobbyTypeResultCD.requestId, setLobbyTypeResultCD);
	}

	public static void SetLobbyMemberLimitCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("SetLobbyMemberLimitCallback", 2);
		}
		SetLobbyMemberLimitResultCD setLobbyMemberLimitResultCD = Marshal.PtrToStructure<SetLobbyMemberLimitResultCD>(param);
		THQNORequest.OnResponse(setLobbyMemberLimitResultCD.requestId, setLobbyMemberLimitResultCD);
	}

	public static void ChatRoomJoinResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ChatRoomJoinResultCallback", 2);
		}
		THQNORequest.OnJoinRoomResponse(Marshal.PtrToStructure<ChatRoomJoinResultCD>(param));
	}

	public static void ChatRoomLeftCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ChatRoomLeftCallback", 2);
		}
		THQNORequest.OnLeaveRoomResponse(Marshal.PtrToStructure<ChatRoomLeftCD>(param));
	}

	public static void ChatRoomMessageCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ChatRoomMessageCallback", 2);
		}
		THQNORequest.OnChatRoomMessageResponse(Marshal.PtrToStructure<ChatRoomMessageCD>(param));
	}

	public static void P2PSessionConnectFailCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("P2PSessionConnectFailCallback", 2);
		}
		THQNORequest.OnP2PSessionConnectFailResponse(Marshal.PtrToStructure<P2PSessionConnectFailCD>(param));
	}

	public static void P2PSessionRequestCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("P2PSessionRequestCallback", 2);
		}
		THQNO_Wrapper.AcceptP2PSessionWithUser(Marshal.PtrToStructure<P2PSessionRequestCD>(param).remoteID);
	}

	public static void P2PAddressUpdateCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("P2PAddressUpdateCallback", 2);
		}
		THQNORequest.OnRequestP2PAddressResponse(Marshal.PtrToStructure<P2PAddressUpdateCD>(param));
	}

	public static void UserStatsReceivedCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("UserStatsReceivedCallback", 2);
		}
		THQNORequest.OnRequestCurrentStatsResponse(Marshal.PtrToStructure<UserStatsReceivedCD>(param));
	}

	public static void UserStatsStoredCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("UserStatsStoredCallback", 2);
		}
		THQNORequest.OnStoreStatsResponse(Marshal.PtrToStructure<UserStatsStoredCD>(param));
	}

	public static void AutoMatchmakingJoinResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("AutoMatchmakingJoinResultCallback", 2);
		}
		Marshal.PtrToStructure<AutomatchmakingJoinResultCD>(param);
	}

	public static void ReportActiveMatchResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ReportActiveMatchResultCallback", 2);
		}
		Marshal.PtrToStructure<ReportActiveMatchResultCD>(param);
	}

	public static void RequestActiveMatchResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("RequestActiveMatchResultCallback", 2);
		}
		Marshal.PtrToStructure<RequestActiveMatchResultCD>(param);
	}

	public static void ActiveMatchHeartbeatCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ActiveMatchHeartbeatCallback", 2);
		}
		Marshal.PtrToStructure<RequestActiveMatchHeartbeatResultCD>(param);
	}

	public static void PlayerDataGetCustomDataResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerDataGetCustomDataResultCallback", 2);
		}
		PlayerDataGetCustomDataResultCD playerDataGetCustomDataResultCD = Marshal.PtrToStructure<PlayerDataGetCustomDataResultCD>(param);
		THQNORequest.OnResponse(playerDataGetCustomDataResultCD.requestId, playerDataGetCustomDataResultCD);
	}

	public static void PlayerDataGetCustomDataResponseResult(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerDataGetCustomDataResponseResult", 2);
		}
		Console.WriteLine("C# PlayerDataGetCustomDataResponseResult called with result " + Marshal.PtrToStructure<PlayerDataGetCustomDataResponseResultCD>(param).result);
	}

	public static void PlayerDataGetPersonalDataResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerDataGetPersonalDataResultCallback", 2);
		}
		PlayerDataGetPersonalDataResultCD playerDataGetPersonalDataResultCD = Marshal.PtrToStructure<PlayerDataGetPersonalDataResultCD>(param);
		THQNORequest.OnResponse(playerDataGetPersonalDataResultCD.requestId, playerDataGetPersonalDataResultCD);
	}

	public static void PlayerDataSetCustomDataResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerDataSetCustomDataResultCallback", 2);
		}
		PlayerDataSetCustomDataResultCD playerDataSetCustomDataResultCD = Marshal.PtrToStructure<PlayerDataSetCustomDataResultCD>(param);
		THQNORequest.OnResponse(playerDataSetCustomDataResultCD.requestId, playerDataSetCustomDataResultCD);
	}

	public static void PlayerDataSetPersonalPlayerNameResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerDataSetPersonalPlayerNameResultCallback", 2);
		}
		PlayerDataSetPersonalPlayerNameResultCD playerDataSetPersonalPlayerNameResultCD = Marshal.PtrToStructure<PlayerDataSetPersonalPlayerNameResultCD>(param);
		THQNORequest.OnResponse(playerDataSetPersonalPlayerNameResultCD.requestId, playerDataSetPersonalPlayerNameResultCD);
	}

	public static void PlayerProfileDeletedResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerProfileDeletedResultCallback", 2);
		}
		PlayerProfileDeleteResultCD playerProfileDeleteResultCD = Marshal.PtrToStructure<PlayerProfileDeleteResultCD>(param);
		THQNORequest.OnResponse(playerProfileDeleteResultCD.requestId, playerProfileDeleteResultCD);
	}

	public static void UserPlatformInformationResult(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerProfileDeletedResultCallback", 2);
		}
		GetUserPlatformInformationResultCD getUserPlatformInformationResultCD = Marshal.PtrToStructure<GetUserPlatformInformationResultCD>(param);
		THQNORequest.OnResponse(getUserPlatformInformationResultCD.requestId, getUserPlatformInformationResultCD);
	}

	public static void AuthSessionTicketResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("AuthSessionTicketResult", 2);
		}
		AuthSessionTicketResultCD authSessionTicketResultCD = Marshal.PtrToStructure<AuthSessionTicketResultCD>(param);
		THQNORequest.OnResponse(authSessionTicketResultCD.requestId, authSessionTicketResultCD);
	}

	public static void DownloadItemResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("DownloadItemResultCallback", 2);
		}
		THQNORequest.OnDownloadItemResponse(Marshal.PtrToStructure<DownloadItemResultCD>(param));
	}

	public static void PlayerStatsResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("PlayerStatsResultCallback", 2);
		}
		Marshal.PtrToStructure<PlayerStatsResultCD>(param);
	}

	public static void ControlledStatsResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("ControlledStatsResultCallback", 2);
		}
		Marshal.PtrToStructure<ControlledStatsResultCD>(param);
	}

	public static void DownloadLeaderboardResultCallback(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("DownloadLeaderboardResultCallback", 2);
		}
		Marshal.PtrToStructure<LeaderboardDownloadedCD>(param);
	}

	private static void GetAutoMatchmakingQueueInfo(IntPtr param)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log("GetAutoMatchmakingQueueInfo", 2);
		}
		Marshal.PtrToStructure<AutomatchmakingQueueInfoCD>(param);
	}
}
