using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Logic;

public static class THQNO_Wrapper
{
	public delegate void DebugLog(string msg);

	public delegate void ErrorLog(string msg);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void Callback(IntPtr result);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void DebugLogCallback(IntPtr request);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void LogCallback(string message);

	private const string dllLocation = "THQNO_Wrapper.dll";

	public static DebugLog debugLog;

	public static ErrorLog errorLog;

	public static void InitCSLogs(DebugLog debugLogMethod, ErrorLog errorLogMethod)
	{
		debugLog = debugLogMethod;
		errorLog = errorLogMethod;
	}

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void RegisterDebugLogCallback(DebugLogCallback cb);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void Test();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void Init();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern Common.APIResult ConnectToOnlineService(string APIKey, string Path);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void DisconnectFromOnlineService();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint CreateLobby(Callback callback, Common.LobbyType lobbyType, int maxMembers, string name, [MarshalAs(UnmanagedType.U1)] bool joinable);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint JoinLobby(Callback callback, string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint JoinLobbyIdx(Callback callback, int idx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void LeaveLobby(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void LeaveLobbyIdx(uint idx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void Update();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsDefined(string defineId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ShowJoinedLobbies();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void DumpLobby(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint RequestLobbyList(Callback callback, [MarshalAs(UnmanagedType.U1)] bool requestLobbyContent);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint RequestLobbyListWithFilter([MarshalAs(UnmanagedType.U1)] bool requestLobbyContent);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetLobbyByIndex(int idx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint CreateRequestLobbyListFilter();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void DestroyRequestLobbyListFilter(uint filterId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AddRequestLobbyListStringFilter(uint filterId, string keyToMatch, string valueToMatch, Common.LobbyStringFilterType comparisonType, [MarshalAs(UnmanagedType.U1)] bool negate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AddRequestLobbyListIntFilter(uint filterId, string keyToMatch, int valueToMatch, [MarshalAs(UnmanagedType.U1)] bool negate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AddRequestLobbyListNearValueFilter(uint filterId, string keyToMatch, int valueToBeCloseTo, [MarshalAs(UnmanagedType.U1)] bool negate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void AddRequestLobbyListFilterSlotsAvailable(uint filterId, int slotsAvailable, [MarshalAs(UnmanagedType.U1)] bool negate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void AddRequestLobbyListDistanceFilter(uint filterId, Common.LobbyDistanceFilter lobbyDistanceFilter, [MarshalAs(UnmanagedType.U1)] bool negate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void AddRequestLobbyListNewerThanFilter(uint filterId, ulong unixTimestampMicros, [MarshalAs(UnmanagedType.U1)] bool negate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void AddRequestLobbyListResultCountFilter(uint filterId, int maxResults);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint CreateRequestLobbySortMethod(uint maxLobbies, Common.LobbySortMethod sortMethod, string sortDataKey, int sortDataValue);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void DestroyRequestLobbySortMethod(uint sortMethodId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool InviteUserToLobby(string lobbyId, string invitee);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetNumLobbyMembers(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetLobbyMemberByIndex(string lobbyId, int member);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetLobbyName(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetStringDataCStr(string lobbyId, string key, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SetStringData(string lobbyId, string key, string value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetStringDataCount(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetStringDataByIndex(string lobbyId, int lobbyData, StringBuilder key, int keyBufferSize, StringBuilder value, int valueBufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool DeleteLobbyStringData(string lobbyId, string key);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetIntData(string lobbyId, string key);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SetIntData(string lobbyId, string key, int value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetIntDataCount(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool DeleteLobbyIntData(string lobbyId, string key);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetMemberStringDataCStr(string lobbyId, string memberId, string key, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetMemberStringData(string lobbyId, string key, string value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetMemberIntData(string lobbyId, string memberId, string key);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetMemberIntData(string lobbyId, string key, int value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestLobbyData(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetLobbyMemberLimit(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint SetLobbyMemberLimit(Callback callback, string lobbyId, int maxMembers);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern Common.LobbyType GetLobbyType(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint SetLobbyType(Callback callback, string lobbyId, Common.LobbyType lobbyType);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern ulong GetLobbyLastUpdateTime(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint SetLobbyJoinable(Callback callback, string lobbyId, [MarshalAs(UnmanagedType.U1)] bool lobbyJoinable);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetLobbyOwner(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint SetLobbyOwner(Callback callback, string lobbyId, string newOwnerId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreateAutomatchmakingPlayerEntry();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreatePlayerGroupId();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void PlayerGroupIdAddUserId(IntPtr handle, string userId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CopyAutomatchmakingPlayerEntry(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void DestroyAutomatchmakingPlayerEntry(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntrySetConnectionInfo(IntPtr handle, string connectionInfo);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntryAddOption(IntPtr handle, string key, string value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntryAddOptionArray(IntPtr handle, string key, string[] values, int valuesSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntrySetGroupId(IntPtr handle, IntPtr groupId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntryClearOptions(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreateAutomatchmakingQueueInfoRequest();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingQueueInfoRequestAddMatchmakingMode(IntPtr handle, string matchmakingMode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void DestroyAutomatchmakingQueueInfoRequest(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint AutomatchmakingQueueInfoResponseGetNumEntries(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool AutomatchmakingQueueInfoResponseGetMatchmakingModeName(IntPtr handle, uint index, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint AutomatchmakingQueueInfoResponseGetWaitingPlayerCount(IntPtr handle, uint index);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void DestroyAutomatchmakingQueueInfoResponse(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void GetAutomatchmakingQueueInfo(ref IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr AutomatchmakingPlayerEntryGetOptions(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntrySetInitiallyPassedMatchingSeconds(IntPtr handle, int seconds);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint AutomatchmakingPlayerEntryGetInitiallyPassedMatchingSeconds(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntrySetScoreName(IntPtr handle, string scoreName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void AutomatchmakingPlayerEntryGetScoreName(IntPtr handle, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool AutoMatchmakingJoin(string matchmakingMode, ref IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool AutoMatchmakingAbort(string matchmakingMode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetMatchInitDataMatchMakingMode(IntPtr handle, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetMatchInitDataFixedOptions(IntPtr handle, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern int GetMatchInitDataConnectionType(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetMatchInitDataBackendConnectionInfo(IntPtr handle, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint GetMatchInitDataPlayersNum(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetMatchInitDataPlayerUserId(IntPtr handle, uint playerIdx, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr GetMatchInitDataPlayerEntry(IntPtr handle, uint playerIdx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint GetMatchInitDataTeamsNum(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint GetMatchInitDataTeamMembersNum(IntPtr handle, uint teamIdx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetMatchInitDataTeamMemberUserId(IntPtr handle, uint teamIdx, uint playerIdx, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsInMatch(string matchMakingMode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void LeaveActiveMatch(string matchMakingMode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreateMatchResultReport();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CopyMatchResultReport(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern Common.MatchReportOutcome GetMatchResultOutcome(IntPtr handle, string userId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetMatchResultOutcome(IntPtr handle, string userId, Common.MatchReportOutcome outcome);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetMatchResultVariableInt(IntPtr handle, string userId, string key, int value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetMatchResultVariableDouble(IntPtr handle, string userId, string key, double value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void ReportActiveMatchResult(string matchMakingMode, ref IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestActiveMatchResult(string matchMakingMode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestMatchResult(string matchId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreateMatchRecordsRequest();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CopyMatchRecordsRequest(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void DestroyMatchRecordsRequest(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void MatchRecordsRequestAddScoreName(IntPtr handle, string userId, string scoreName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestMatchRecords(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreateMatchScoresRequest();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CopyMatchScoresRequest(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void DestroyMatchScoresRequest(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void MatchScoresRequestAddScoreName(IntPtr handle, string userId, string scoreName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestMatchScores(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SendP2PPacket(string remoteId, IntPtr data, uint dataSize, Common.P2PSendType sendType, uint channel);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsP2PPacketAvailable(out uint messageSize, uint channel);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string ReadP2PPacket(IntPtr dest, uint destSize, out uint outMessageSize, uint channel);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool AcceptP2PSessionWithUser(string remoteId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool CloseP2PSessionWithUser(string remoteId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool CloseP2PChannelWithUser(string remoteId, uint channel);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestP2PAddress(string remoteId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetP2PSessionDetails(string remoteId, out Common.P2PSessionDetails sessionDetails);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint RequestCurrentStats(Callback callback);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint PlatformRequestCurrentStats(Callback callback);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint StoreStats(Callback callback);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.SysInt)]
	public static extern IntPtr CreatePlayerStatsRequest();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void PlayerStatsRequestAddStatName(IntPtr playerStatsRequest, string userId, string statName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint PlayerStatsResponseGetNumEntries(ref IntPtr playerStatsResponse);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint PlayerStatsResponseGetEntryNumStats(ref IntPtr playerStatsResponse, uint entryIdx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern string PlayerStatsResponseGetStatName(ref IntPtr playerStatsResponse, uint entryIdx, uint statIdx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern string PlayerStatsResponseGetUserId(ref IntPtr playerStatsResponse, uint entryIdx);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern int PlayerStatsResponseGetIntStat(ref IntPtr playerStatsResponse, string userId, string statName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern float PlayerStatsResponseGetFloatStat(ref IntPtr playerStatsResponse, string userId, string statName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestPlayerStats(ref IntPtr playerStatsRequest);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RequestControlledStats(ref IntPtr playerStatsRequest);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetIntStat(string name, IntPtr data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetFloatStat(string name, IntPtr data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SetIntStat(string name, int data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SetFloatStat(string name, double data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool UpdateAvgRateStat(string name, double countThisSession, double sessionLength);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformGetIntStat(string name, IntPtr data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformGetFloatStat(string name, IntPtr data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformSetIntStat(string name, int data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformSetFloatStat(string name, double data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformUpdateAvgRateStat(string name, double countThisSession, double sessionLength);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetAchievement(string name, IntPtr achieved);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SetAchievement(string name);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool ClearAchievement(string name);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformGetAchievement(string name, IntPtr achieved);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformSetAchievement(string name);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlatformClearAchievement(string name);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint DownloadLeaderboardAroundRank(string leaderboardName, uint rank, int rangeMin, int rangeMax);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint DownloadLeaderboardAroundUser(string leaderboardName, string userId, int rangeMin, int rangeMax);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint LeaderboardEntry_GetRank(IntPtr leaderboardsEntriesHandle, uint entryIndex);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern double LeaderboardEntry_GetScoreFloat(IntPtr leaderboardsEntriesHandle, uint entryIndex, string scoreName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern string LeaderboardEntry_GetUserId(IntPtr leaderboardsEntriesHandle, uint entryIndex);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint LeaderboardEntry_GetScoresNum(IntPtr leaderboardsEntriesHandle, uint entryIndex);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern string LeaderboardEntry_GetScoreName(IntPtr leaderboardsEntriesHandle, uint entryIndex, uint scoreIndex);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern string LeaderboardEntry_GetAdditionalData(IntPtr leaderboardsEntriesHandle, uint entryIndex, string scoreName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void EnableLogs(LogCallback logFunc);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void DisableLogs();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern Common.ServiceAPIVersionMismatch GetServiceAPIVersionMismatch();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint GetPublicReadDocument(string documentName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint GetPublicReadDocumentNumEntries(IntPtr handle);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern Common.ProjectDocumentResponseState GetPublicReadDocumentState(IntPtr handle, uint index);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetPublicReadDocumentData(IntPtr handle, uint index, StringBuilder nameBuffer, uint nameBufferSize, StringBuilder typeBuffer, uint typeBufferSize, StringBuilder contentBuffer, uint contentBufferSize, StringBuilder hashBuffer, uint hashBufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignedIn();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetOnlineId();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetPlayerName();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RegisterEmailPW(string displayName, string email, string password, [MarshalAs(UnmanagedType.U1)] bool showActivationLinkInEmail, [MarshalAs(UnmanagedType.U1)] bool showActivationCodeInEmail);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RegisterMessengerCodePW(string displayName, string messengerName, string messengerCode, string password, [MarshalAs(UnmanagedType.U1)] bool showActivationLink, [MarshalAs(UnmanagedType.U1)] bool showActivationCode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool RegisterPlatform(Common.PlatformType platformType, string[] args, int iArgs);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignInEmailPW(string email, string password);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignInMessengerCodePW(string messengerName, string messengerCode, string password);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignInPlatform(Common.PlatformType platformType, string[] args, int iArgs);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignInAnonymous();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignInOffline();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SignOut();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool StartPasswordRecovery(string contactIdentifier);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool StartPasswordRecoveryEmail(string email);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool StartPasswordRecoveryPhone(string phone);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool StartPasswordRecoveryMessenger(string messengerName, string messengerCode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool ConfirmPasswordRecovery(string newPassword, string verificationCode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool CheckEmail(string newEmail);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool CheckPhone(string newPhoneNumberE123);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool VerifyEmail(string verificationCode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool VerifyPhone(string verificationCode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool UpdateEmail(string newEmail, [MarshalAs(UnmanagedType.U1)] bool showActivationLinkInEmail, [MarshalAs(UnmanagedType.U1)] bool showActivationCodeInEmail);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool UpdatePhone(string newPhoneNumberE123);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool ResetPasswordEmail(string email, string password, string newPassword);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool ResetPasswordPhone(string phoneNumberE123, string password, string newPassword);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool DeleteAccountEmail(string email, string password);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool DeleteAccountPhone(string phoneNumberE123, string password);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool DeleteAccountPlatform(Common.PlatformType platformType, string[] args, int iArgs);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint GetUserPlatformInformation(Callback callback, string userId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint PlayerDataSetCustomData(Callback callback, string data);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint PlayerDataGetCustomData(Callback callback, string userId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint PlayerDataGetPersonalDataList(Callback callback, string[] userIds, uint length);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string PlayerDataGetPersonalDataListUserId(IntPtr handle, uint index);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool PlayerDataGetPersonalDataListPlayerName(IntPtr handle, uint index, StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint PlayerDataGetPersonalData(Callback callback, string userId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint PlayerDataSetPersonalPlayerName(Callback callback, string playerName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint PlayerProfileDelete(Callback callback, string profileName);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint PlayerProfileGetNames(Callback callback);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void JoinRoom(ulong roomId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void LeaveRoom(ulong roomId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern void SendRoomMessage(uint id, string message);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool HasCurrentAppEntitlement();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool HasAppEntitlement(string appId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsDlcInstalled(string appId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool GetSteamBetaName(StringBuilder buffer, uint bufferSize);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetPresenceLobby(string lobbyId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ClearPresenceLobby();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetPresenceText(string presenceText, string presenceDetailText);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern Common.PlatformType GetPlatformType();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetPlatformPlayerName();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetGameLanguage();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string GetUniquePlayerId();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint RequestAuthSessionTicket(Callback callback);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void SetOverlayNotificationPosition(Common.OverlayNotificationPosition notificationPosition);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ActivateGameOverlay(Common.OverlayMode mode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ActivateInviteOverlay();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void ActivateGameOverlayToUser(Common.UserOverlayMode mode, string userId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void ActivateGameOverlayToWebPage(string url);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ActivateGameOverlayToStore(string entitlementId, Common.StoreOverlayMode storeMode);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void EnableEpicAccountControl(bool showNotification, int notificationDisplayTimeout);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void DisableEpicAccountControl();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsVoiceChatSupported();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void StartVoiceRecording();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void StopVoiceRecording();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern Common.VoiceResult GetAvailableVoice(IntPtr _pCompressed, IntPtr _pUncompressed, uint _iUncompressedVoiceDesiredSampleRate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern Common.VoiceResult GetVoice([MarshalAs(UnmanagedType.U1)] bool wantCompressed, IntPtr destBuffer, uint destBufferSize, uint bytesWritten, [MarshalAs(UnmanagedType.U1)] bool wantUncompressed, IntPtr uncompressedDestBuffer, uint uncompressedDestBufferSize, IntPtr uncompressBytesWritten, uint uncompressedVoiceDesiredSampleRate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern Common.VoiceResult DecompressVoice(IntPtr compressedPtr, uint compressed, IntPtr destBuffer, uint destBufferSize, IntPtr bytesWritten, uint desiredSampleRate);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern void SetMiniDumpDescription(string descsription);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void WriteMiniDump(uint structuredExceptionCode, IntPtr exceptionInfo, uint buildId);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void DownloadItem(ulong publishedFileID, [MarshalAs(UnmanagedType.U1)] bool highPriority);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint GetNumSubscribedItems();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint GetSubscribedItems(IntPtr publishedFileId, uint maxEntries);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint GetItemState(ulong publishedFileID);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern bool GetItemInstallInfo(ulong publishedFileID, IntPtr sizeOnDisk, StringBuilder folder, uint folderSize, IntPtr timeStamp);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool SetRichPresence(string key, string value);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void ClearRichPresence();

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void THQNORegisterCallback(Callback callback, Common.CallbackType callbackType);

	[DllImport("THQNO_Wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void THQNOUnregisterCallback(Common.CallbackType callbackType);
}
