using System.Collections.Generic;

namespace Logic;

public class ChatServer
{
	private Multiplayer multiplayer;

	public ChatServer(Multiplayer multiplayer)
	{
		this.multiplayer = multiplayer;
	}

	public void RouteMessage(string senderPlayerId, string message, Chat.Channel channel, string whisperTargetId = null, string campaignId = null)
	{
		if (multiplayer == null || multiplayer.game == null)
		{
			return;
		}
		List<Multiplayer> list = new List<Multiplayer>();
		bool includeServer = false;
		bool flag = channel == Chat.Channel.Lobby;
		if (flag && campaignId == null)
		{
			Multiplayer.Error("Trying to use lobby chat with null campaign id!");
		}
		switch (channel)
		{
		case Chat.Channel.Team:
		{
			Multiplayer player2 = GetPlayer(senderPlayerId);
			list = GetAllPlayersOnTeam(player2.playerData.team);
			if (multiplayer.playerData != null && multiplayer.playerData.team == player2.playerData.team)
			{
				includeServer = true;
			}
			break;
		}
		case Chat.Channel.All:
		case Chat.Channel.Lobby:
			list = GetPlayers();
			if (!flag)
			{
				includeServer = true;
			}
			break;
		case Chat.Channel.Whisper:
		{
			if (whisperTargetId == null)
			{
				Multiplayer.Error("Sending a whisper chat message with no whisper target!");
				return;
			}
			if (multiplayer.playerData != null && multiplayer.playerData.id == whisperTargetId)
			{
				includeServer = true;
				break;
			}
			Multiplayer player = GetPlayer(whisperTargetId);
			if (player == null)
			{
				multiplayer.NotifyListenersDelayed("chat_add_system_message", "System.Chat.player_left_channel", process_triggers: false, profile: false);
				return;
			}
			list.Add(player);
			break;
		}
		}
		multiplayer.BroadcastChatMessage(list, senderPlayerId, message, channel, includeServer, whisperTargetId, flag, campaignId);
	}

	private List<Multiplayer> GetPlayers()
	{
		List<Multiplayer> list = new List<Multiplayer>();
		List<Multiplayer.PlayerData> all = Multiplayer.CurrentPlayers.GetAll();
		for (int i = 0; i < all.Count; i++)
		{
			if (all[i].owner.type != Multiplayer.Type.Server)
			{
				list.Add(all[i].owner);
			}
		}
		return list;
	}

	private Multiplayer GetPlayer(string playerId)
	{
		if (multiplayer != null && multiplayer.playerData != null && multiplayer.playerData.id == playerId)
		{
			return multiplayer;
		}
		return Multiplayer.CurrentPlayers.GetByGUID(playerId)?.owner;
	}

	private List<Multiplayer> GetAllPlayersOnTeam(int team)
	{
		List<Multiplayer> list = new List<Multiplayer>();
		List<Multiplayer> players = GetPlayers();
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].playerData.team == team)
			{
				list.Add(players[i]);
			}
		}
		return list;
	}
}
