using System;

namespace Logic;

public static class Common
{
	public enum GameBackendType
	{
		Unknown,
		Steam,
		GOG,
		GOGOffline,
		THQNO,
		Origin
	}

	public class RequestId
	{
		private const uint InvalidRequestId = 0u;

		private uint requestId;

		public RequestId(uint requestId)
		{
			this.requestId = requestId;
		}

		public RequestId(RequestId requestId)
		{
			this.requestId = requestId.requestId;
		}

		public static bool operator ==(RequestId lhs, RequestId rhs)
		{
			return lhs.requestId == rhs.requestId;
		}

		public static bool operator !=(RequestId lhs, RequestId rhs)
		{
			return lhs.requestId != rhs.requestId;
		}

		public override bool Equals(object obj)
		{
			RequestId requestId = obj as RequestId;
			if (requestId == null)
			{
				return false;
			}
			return this.requestId.Equals(requestId.requestId);
		}

		public override int GetHashCode()
		{
			return requestId.GetHashCode();
		}

		public bool IsValid()
		{
			return requestId != 0;
		}

		public uint ToUInt()
		{
			return requestId;
		}
	}

	public enum ProjectDocumentResponseState
	{
		HashMatched,
		HashMismatched,
		NotFound,
		NumOf
	}

	public enum ServiceAPIVersionMismatch
	{
		NotChecked,
		UnableToCheck,
		NoMismatch,
		MinorVersionsMismatch,
		MajorVersionsMismatch
	}

	public enum GameLobbyType
	{
		Native,
		Steam
	}

	public class GameLobbyId
	{
		private GameLobbyType typePart;

		private ulong numberPart;

		public GameLobbyId(GameLobbyType typePart, ulong numberPart)
		{
			this.typePart = typePart;
			this.numberPart = numberPart;
		}

		public GameLobbyId(string id)
		{
			string[] array = id.Split('|');
			if (array.Length != 2)
			{
				Error("Invalid game lobby ID format!");
				return;
			}
			if (!Enum.TryParse<GameLobbyType>(array[0], out var result))
			{
				Error("Invalid game lobby ID type part!");
				return;
			}
			typePart = result;
			ulong num;
			try
			{
				num = ulong.Parse(array[1]);
			}
			catch (FormatException ex)
			{
				Error("Invalid game lobby ID number part! " + ex.Message);
				return;
			}
			numberPart = num;
		}

		public GameLobbyId(GameLobbyId gameLobbyId)
		{
			typePart = gameLobbyId.GetTypePart();
			numberPart = gameLobbyId.GetNumberPart();
		}

		public GameLobbyType GetTypePart()
		{
			return typePart;
		}

		public ulong GetNumberPart()
		{
			return numberPart;
		}

		public override string ToString()
		{
			return string.Concat(typePart, "|", numberPart);
		}

		public static bool operator ==(GameLobbyId lhs, GameLobbyId rhs)
		{
			if (lhs.GetTypePart() == rhs.GetTypePart())
			{
				return lhs.GetNumberPart() == rhs.GetNumberPart();
			}
			return false;
		}

		public static bool operator !=(GameLobbyId lhs, GameLobbyId rhs)
		{
			return !(lhs == rhs);
		}

		public static bool operator <(GameLobbyId lhs, GameLobbyId rhs)
		{
			if (lhs.GetNumberPart() >= rhs.GetNumberPart())
			{
				if (lhs.GetNumberPart() == rhs.GetNumberPart())
				{
					return lhs.GetTypePart() < rhs.GetTypePart();
				}
				return false;
			}
			return true;
		}

		public static bool operator >(GameLobbyId lhs, GameLobbyId rhs)
		{
			if (lhs.GetNumberPart() <= rhs.GetNumberPart())
			{
				if (lhs.GetNumberPart() == rhs.GetNumberPart())
				{
					return lhs.GetTypePart() > rhs.GetTypePart();
				}
				return false;
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			GameLobbyId gameLobbyId = obj as GameLobbyId;
			if (gameLobbyId == null)
			{
				return false;
			}
			if (GetTypePart().Equals(gameLobbyId.GetTypePart()))
			{
				return GetNumberPart().Equals(gameLobbyId.GetNumberPart());
			}
			return false;
		}

		public override int GetHashCode()
		{
			return GetTypePart().GetHashCode() + GetNumberPart().GetHashCode();
		}
	}

	public enum UserIdType
	{
		Registered,
		Anonymous,
		Steam,
		Gog,
		Origin,
		Discord,
		Offline,
		NumOf
	}

	public class UserId
	{
		private UserIdType typePart;

		private ulong numberPart;

		public UserId(UserIdType typePart, ulong numberPart)
		{
			this.typePart = typePart;
			this.numberPart = numberPart;
		}

		public UserId(string id)
		{
			string[] array = id.Split('|');
			if (array.Length != 2)
			{
				Error("Invalid user ID format!");
				return;
			}
			if (!Enum.TryParse<UserIdType>(array[0], out var result))
			{
				Error("Invalid user ID type part!");
				return;
			}
			typePart = result;
			ulong num;
			try
			{
				num = ulong.Parse(array[1]);
			}
			catch (FormatException ex)
			{
				Error("Invalid user ID number part! " + ex.Message);
				return;
			}
			numberPart = num;
		}

		public UserId(UserId userId)
		{
			typePart = userId.GetTypePart();
			numberPart = userId.GetNumberPart();
		}

		public UserIdType GetTypePart()
		{
			return typePart;
		}

		public ulong GetNumberPart()
		{
			return numberPart;
		}

		public override string ToString()
		{
			return string.Concat(typePart, "|", numberPart);
		}

		public static bool operator ==(UserId lhs, UserId rhs)
		{
			if (lhs.GetTypePart() == rhs.GetTypePart())
			{
				return lhs.GetNumberPart() == rhs.GetNumberPart();
			}
			return false;
		}

		public static bool operator !=(UserId lhs, UserId rhs)
		{
			return !(lhs == rhs);
		}

		public static bool operator <(UserId lhs, UserId rhs)
		{
			if (lhs.GetNumberPart() >= rhs.GetNumberPart())
			{
				if (lhs.GetNumberPart() == rhs.GetNumberPart())
				{
					return lhs.GetTypePart() < rhs.GetTypePart();
				}
				return false;
			}
			return true;
		}

		public static bool operator >(UserId lhs, UserId rhs)
		{
			if (lhs.GetNumberPart() <= rhs.GetNumberPart())
			{
				if (lhs.GetNumberPart() == rhs.GetNumberPart())
				{
					return lhs.GetTypePart() > rhs.GetTypePart();
				}
				return false;
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			UserId userId = obj as UserId;
			if (userId == null)
			{
				return false;
			}
			if (GetTypePart().Equals(userId.GetTypePart()))
			{
				return GetNumberPart().Equals(userId.GetNumberPart());
			}
			return false;
		}

		public override int GetHashCode()
		{
			return GetTypePart().GetHashCode() + GetNumberPart().GetHashCode();
		}
	}

	public enum APIResult
	{
		Fail = 0,
		Success = 1,
		NotAuthenticated = -1,
		ServiceUnavaliable = -2,
		HeartBeatFailure = -3,
		InvalidUTF8Argument = -4,
		InvalidASCIIArgument = -5,
		InvalidUserIdArgument = -6,
		InvalidLobbyIdArgument = -7,
		InvalidPointerArgument = -8,
		Init_ConfigurationError = -101,
		Init_RequiredPluginNotLoaded = -102,
		Init_AlreadyInitialized = -103,
		Init_RestartRequired = -104,
		SignIn_AlreadySignedIn = -201,
		SignIn_AuthenticationFailed = -202,
		SignIn_InvalidTrustedMessengerUserCode = -203,
		SignIn_UnknownPlatform = -204,
		SignIn_UnableToGenerateSteamAuthSessionTicket = -205,
		SignIn_UnableToGenerateEAOriginAccessToken = -206,
		SignIn_UnableToGenerateGoGEncryptedAppTicket = -207,
		SignIn_MissingArguments = -208,
		SignIn_MissingSonyAuthType = -209,
		SignIn_MissingSonyAuthorizationCode = -210,
		SignIn_MissingSonyContext = -211,
		SignIn_MissingSonyIdToken = -212,
		SignIn_MissingXBLToken = -214,
		SignIn_MissingXBLRelyingParty = -214,
		SignIn_InvalidAuthIdentifiers = -215,
		SignIn_MissingEpicIdToken = -216,
		SignIn_InvalidSteamAppId = -217,
		Register_InsufficientRegistrationData = -301,
		Register_PasswordTooShort = -302,
		Register_PasswordTooLong = -303,
		Register_PasswordTooWeakUseMoreCharacters = -304,
		Register_PasswordTooWeakUseMoreVariation = -305,
		Register_PasswordHasInvalidCharacters = -306,
		Register_PasswordHasInvalidASCIICharacters = -307,
		Register_PasswordHasInvalidUTF8Characters = -308,
		Register_InvalidPassword = -309,
		Register_InvalidEmailAddressProvided = -310,
		Register_NoAccountIdentifiersFound = -311,
		Register_SomeAccountIdentifiersAlreadyPresent = -312,
		Register_UnableToPutAccountLinkToDatabase = -313,
		Register_UnableToAddActivatedAccountToDatabase = -314,
		Register_UnknownPlatform = -315,
		Register_UnableToGenerateSteamAuthSessionTicket = -316,
		Register_UnableToGenerateEAOriginAccessToken = -317,
		Register_UnableToGenerateGoGEncryptedAppTicket = -318,
		Register_InvalidTrustedMessengerUserCode = -319,
		Register_MissingArguments = -320,
		Register_MissingSonyAuthType = -321,
		Register_MissingSonyAuthorizationCode = -322,
		Register_MissingSonyContext = -323,
		Register_MissingSonyIdToken = -324,
		Register_MissingXBLToken = -325,
		Register_MissingXBLRelyingParty = -326,
		Register_InvalidAuthIdentifiers = -327,
		Register_MissingEpicIdToken = -328,
		Register_InvalidSteamAppId = -329,
		UpdateContact_NoAccountIdentifiersFound = -401,
		UpdateContact_SomeAccountIdentifiersAlreadyPresent = -402,
		UpdateContact_AuthenticationFailed = -403,
		UpdateContact_MissingNewContactOrPassword = -404,
		Account_InvalidUserSession = -501,
		Account_AuthenticationFailed = -502,
		Account_UnhandledContactType = -503,
		Account_MissingVerificationCode = -504,
		Account_UnableToUpdateVerificationCode = -505,
		Account_NoNewContactIdentifierProvided = -506,
		Account_UnableToDeleteAccount = -507,
		Account_UnableToUpdateActivationTries = -508,
		Account_InvalidVerificationCode = -509,
		Account_UnableToUpdateContactIdentifier = -510,
		Account_UnableToPutAccountLinkToDatabase = -511,
		Account_UnableToGetNonactivatedAccount = -512,
		Account_UnableToPutActivatedAccount = -513,
		Account_UnableToDeleteNonactivatedAccount = -514,
		DeleteAccount_AuthenticationFailed = -601,
		DeleteAccount_InvalidTrustedMessengerUserCode = -602,
		DeleteAccount_InvalidAuthIdentifiers = -603,
		DeleteAccount_UnableToGenerateSteamAuthSessionTicket = -604,
		DeleteAccount_UnableToGenerateEAOriginAccessToken = -605,
		DeleteAccount_UnableToGenerateGoGEncryptedAppTicket = -606,
		DeleteAccount_MissingArguments = -607,
		DeleteAccount_MissingSonyAuthType = -608,
		DeleteAccount_MissingSonyAuthorizationCode = -609,
		DeleteAccount_MissingSonyContext = -610,
		DeleteAccount_MissingSonyIdToken = -611,
		DeleteAccount_MissingXBLToken = -612,
		DeleteAccount_MissingXBLRelyingParty = -613,
		DeleteAccount_UnknownPlatform = -614,
		DeleteAccount_InvalidSteamAppId = -615,
		PasswordRecovery_InsufficientContactData = -701,
		PasswordRecovery_UserIdNotFound = -702,
		PasswordRecovery_PasswordTooShort = -703,
		PasswordRecovery_PasswordTooLong = -704,
		PasswordRecovery_PasswordTooWeakUseMoreCharacters = -705,
		PasswordRecovery_PasswordTooWeakUseMoreVariation = -706,
		PasswordRecovery_PasswordHasInvalidASCIICharacters = -707,
		PasswordRecovery_PasswordHasInvalidUTF8Characters = -708,
		PasswordRecovery_InvalidPassword = -709,
		PasswordRecovery_AuthenticationFailed = -710,
		PasswordRecovery_InvalidTrustedMessengerUserCode = -711,
		ResetPassword_PasswordTooShort = -801,
		ResetPassword_PasswordTooLong = -802,
		ResetPassword_PasswordTooWeakUseMoreCharacters = -803,
		ResetPassword_PasswordTooWeakUseMoreVariation = -804,
		ResetPassword_PasswordHasInvalidASCIICharacters = -805,
		ResetPassword_PasswordHasInvalidUTF8Characters = -806,
		ResetPassword_InvalidPassword = -807,
		ResetPassword_AuthenticationFailed = -808,
		ResetPassword_InvalidTrustedMessengerUserCode = -809,
		PhoneNotFound = -901,
		PhoneFound = -902,
		EmailNotFound = -903,
		EmailFound = -904,
		CheckContact_MissingContactData = -1001,
		CreateLobby_LobbyLimitReached = -1101,
		JoinLobby_LobbyIsNotJoinable = -1201,
		JoinLobby_MemberAlreadyJoined = -1202,
		JoinLobby_LobbyIsFull = -1203,
		JoinLobby_UnableToJoinLobby = -1204,
		JoinLobby_LobbyNotFound = -1205,
		SetLobbyJoinable_LobbyNotFound = -1301,
		SetLobbyJoinable_OnlyLobbyOwnerCanSetLobbyJoinable = -1302,
		SetLobbyType_LobbyNotFound = -1401,
		SetLobbyType_OnlyLobbyOwnerCanSetType = -1402,
		SetLobbyMemberLimit_LobbyNotFound = -1501,
		SetLobbyMemberLimit_OnlyLobbyOwnerCanSetMemberLimit = -1502,
		RequestLobbyData_LobbyNotFound = -1601,
		SetLobbyOwner_LobbyNotFound = -1701,
		SetLobbyOwner_OnlyLobbyOwnerCanSetOwner = -1702,
		SetLobbyOwner_OnlyMembersCanBecomeLobbyOwners = -1703,
		ChatRoomNotFound = -1801,
		ChatRoomFull = -1802,
		AutoMatchmakingJoin_MatchingModeNotFound = -1901,
		AutoMatchmakingJoin_AlreadyJoinedMatchmakingQueue = -1902,
		AutoMatchmakingJoin_UnableToReadInitPlayerEntry = -1903,
		AutoMatchmakingJoin_MatchmakingAborted = -1904,
		AutoMatchmakingJoin_NoMatchFound = -1905,
		AutoMatchmakingJoin_MissingConnectionInfoInMatchInitPlayerEntry = -1906,
		AutoMatchmakingJoin_PlayerAlreadyInMatch = -1907,
		AutoMatchmakingJoin_UnableToReadMatchId = -1908,
		AutoMatchmakingJoin_UnableToReadMatchInitData = -1909,
		AutoMatchmakingJoin_Acknowledge_Failure = -1910,
		AutoMatchmakingJoin_ScoreNameNotFound = -1911,
		AutoMatchmakingJoin_UnsupportedTeamId = -1912,
		AutoMatchmakingJoin_MatchmakingModeDoesntSupportTeamSize = -1913,
		AutoMatchmakingJoin_MatchmakingModeDoesntSupportPlayersWithoutTeam = -1914,
		AutoMatchmakingJoin_MatchmakingModeDoesntSupportTeams = -1915,
		AutoMatchmakingJoin_NoActivePlayerProfileSet = -1916,
		Match_NotFound = -2001,
		MatchRecords_UnableToDeserialize = -2002,
		MatchRecords_InvalidMatchRecordsRequestDocument = -2003,
		MatchScores_UnableToDeserialize = -2004,
		MatchScores_InvalidMatchScoresRequestDocument = -2005,
		Match_P2P_InvalidMatchResultReportDocumentFormat = -2006,
		Match_P2P_UnableToGetActivePlayerProfile = -2007,
		Match_P2P_MatchNotFound = -2008,
		Match_P2P_PlayerNotInMatch = -2009,
		Profile_ProfileContentExceedsMaxAllowedSize = -2101,
		Profile_ProfilesLimitReached = -2102,
		Profile_UnableToGetProjectPermissions = -2103,
		Profile_ProfileWithThatNameAlreadyExists = -2104,
		Profile_ProfileWithThatNameDoesntExist = -2105,
		Profile_CantDeleteActivePlayerProfile = -2106,
		Profile_CantDeleteDefaultProfile = -2107,
		Profile_InvalidPlayerId = -2108,
		Profile_UnregisteredPlayerId = -2109,
		Profile_UnableToGetActivePlayerSession = -2110,
		Profile_MatchScoresNotFound = -2111,
		Profile_NoActivePlayerProfileSet = -2112,
		Profile_InvalidProfileName = -2113,
		Profile_PlayerDataNotFound = -2114,
		Profile_InvalidPlayerDataRequestDocument = -2115,
		Profile_InvalidPlayerDataMultiDocument = -2116,
		Profile_DataExceedsSizeLimit = -2117,
		Profile_ProfileUpdateErrors = -2118,
		Stats_NoActivePlayerProfileSet = -2201,
		Stats_PlayerStatNotFound = -2202,
		Stats_UnableToUpdatePlayerStats = -2203,
		Stats_InvalidPlayerIdFormat = -2204,
		Stats_UserIsNotRegistered = -2205,
		Stats_UnableToGetActivePlayerProfile = -2206,
		Stats_MissingPlayerProfileName = -2207,
		Stats_UnableToGetPlayerStats = -2208,
		Stats_InvalidStatsRequestDocumentFormat = -2209,
		Stats_UnableToGetControlledPlayerStatsDefinitionDocument = -2210,
		Leaderboard_LeaderboardNotFound = -2301,
		Leaderboard_LeaderboardEmpty = -2302,
		Leaderboard_FocusPlayerEntryNotFound = -2303,
		Leaderboard_InvalidRangeSpecified = -2304,
		Leaderboard_NoLeaderboardsDefined = -2305,
		Leaderboard_FocusModeNotImplemented = -2306,
		Project_PublicReadDocument_UnableToReadResponse = -2401,
		InvalidResultCode = -32768
	}

	public enum CallbackType
	{
		OnlineServicesSignOut = 0,
		RegisterResult = 100,
		RegisterPlatformResult = 101,
		SignInResult = 102,
		CheckEmailResult = 103,
		CheckPhoneResult = 104,
		UpdatePhoneResult = 105,
		UpdateEmailResult = 106,
		VerifyPhoneResult = 107,
		VerifyEmailResult = 108,
		ResetPasswordResult = 109,
		DeleteAccountResult = 110,
		PasswordRecoveryStartResult = 111,
		PasswordRecoveryResult = 112,
		LobbyListReceived = 200,
		LobbyCreated = 201,
		LobbyEnter = 202,
		LobbyLeave = 203,
		LobbyInvite = 204,
		LobbyDataUpdate = 205,
		LobbyMemberDataUpdate = 206,
		LobbyMessage = 207,
		SetLobbyOwnerResult = 208,
		SetLobbyJoinable = 209,
		SetLobbyType = 210,
		SetLobbyMemberLimit = 211,
		ChatRoomJoinResult = 300,
		ChatRoomLeft = 301,
		ChatRoomJoined = 302,
		ChatRoomMessage = 303,
		P2PSessionConnectFail = 400,
		P2PSessionRequest = 401,
		P2PAddressUpdate = 402,
		AchievementSet = 500,
		UserStatsReceived = 501,
		UserStatsStored = 502,
		PlayerStats = 503,
		ControlledStats = 504,
		LeaderboardFindResult = 505,
		LeaderboardStoreResult = 506,
		LeaderboardDownloaded = 507,
		AutoMatchmakingJoinResult = 600,
		AutoMatchmakingUpdate = 601,
		ReportActiveMatchResult = 602,
		RequestActiveMatchResult = 603,
		RequestActiveMatchHeartbeatResult = 604,
		RequestMatchRecords = 605,
		RequestMatchScores = 606,
		PlayerDataGetCustomDataResponseResult = 700,
		GetPlayerCustomDataResponseResult = 701,
		PlayerDataGetGameSettingsResult = 702,
		PlayerDataGetMatchDataResult = 703,
		playerdatagetpersonaldataresult = 704,
		PlayerDataGetPersonalDataListResult = 705,
		PlayerDataSetCustomDataResult = 706,
		PlayerDataSetGameSettingsResult = 707,
		PlayerDataSetPersonalPlayerNameResult = 708,
		PlayerProfileDeletedResult = 709,
		PlayerProfileSetActiveResult = 710,
		GetUserPlatformInformationResult = 711,
		DownloadItemResult = 800,
		Project_PublicReadDocument = 900,
		AuthSessionTicketResult = 1000,
		RequestFriendListResult = 1100,
		UserRichPresenceUpdate = 1101,
		GameOverlayStatusUpdate = 1200,
		GetAutoMatchmakingQueueInfo = 1300,
		GameServerSignInResult = 1400
	}

	public enum LobbyType
	{
		Private,
		FriendsOnly,
		Public,
		Invisible
	}

	[Flags]
	public enum LobbySortMethod
	{
		SortByAvailability = 2,
		SortByClosestToValue = 4,
		SortByLastUpdateTime = 8,
		Ascending = 0x10
	}

	public enum LobbyStringFilterType
	{
		Exact,
		Lean
	}

	public enum LobbyDistanceFilter
	{
		Close,
		Far,
		Worldwide
	}

	public enum LobbyDataSetMode
	{
		Merge,
		Override,
		Remove
	}

	public enum MemberLeftReason
	{
		Left,
		Disconnected
	}

	public enum P2PSendType
	{
		Unreliable,
		UnreliableSequenced,
		Reliable,
		ReliableOrdered,
		ReliableSequenced
	}

	public enum P2PConnectionError
	{
		None,
		Timeout,
		Max
	}

	public enum P2PSessionState
	{
		Connected,
		Connecting,
		Disconnected
	}

	public struct P2PSessionDetails
	{
		private P2PSessionState sessionState;

		private uint externalIP;

		private ushort externalPort;
	}

	public enum ChatRoomUserLeftReason
	{
		Left,
		Disconnected,
		Kicked,
		Banned
	}

	public enum VoiceResult
	{
		OK,
		NotInitialized,
		NotRecording,
		NoData,
		BufferTooSmall,
		DataCorrupted,
		Restricted,
		UnsupportedCodec,
		ReceiverOutOfDate,
		ReceiverDidNotAnswer
	}

	public enum PlatformType
	{
		THQNO,
		Steam,
		GOG,
		Origin,
		Discord,
		EpicStore,
		WinStore,
		NoDRM,
		PSN,
		XBL,
		Luna,
		NumOf
	}

	public enum OverlayNotificationPosition
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public enum StoreOverlayMode
	{
		None,
		AddToCart,
		AddToCartAndShow
	}

	public enum OverlayMode
	{
		Default,
		Friends,
		Settings,
		Stats,
		Achievements,
		Players,
		Community,
		SteamOfficialgamegroup
	}

	public enum UserOverlayMode
	{
		Default,
		UserId,
		Stats,
		Achievements,
		Chat,
		AddFriend,
		RemoveFriend,
		AcceptFriendRequest,
		IgnoreFriendRequest,
		JoinTrade
	}

	[Flags]
	public enum ItemState
	{
		None = 0,
		Subscribed = 1,
		LegacyItem = 2,
		Installed = 4,
		NeedsUpdate = 8,
		Downloading = 0x10,
		DownloadPending = 0x20
	}

	private enum PersonaState
	{
		Offline,
		Online,
		Busy,
		Away
	}

	public enum MatchReportOutcome
	{
		Win,
		Lose,
		Draw
	}

	public enum MatchState
	{
		NotFound,
		Running,
		Over,
		Closed
	}

	public enum MatchResultState
	{
		Unfinished,
		Closed_Invalid,
		Closed_Conculsive,
		Closed_Inconclusive,
		Unknown
	}

	public static void Log(string msg)
	{
		THQNO_Wrapper.debugLog?.Invoke(msg);
	}

	public static void Error(string msg)
	{
		THQNO_Wrapper.errorLog?.Invoke(msg);
	}
}
