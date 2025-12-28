namespace Logic;

public class Chat
{
	public enum Channel
	{
		All = 1,
		Team = 2,
		Whisper = 3,
		Lobby = 4,
		NONE = 0
	}

	private Multiplayer multiplayer;

	public Chat(Multiplayer multiplayer)
	{
		this.multiplayer = multiplayer;
	}

	public void SendInGameChatMessage(Channel channel, string message, string whisperTargetId)
	{
		if (multiplayer.type == Multiplayer.Type.ServerClient)
		{
			Multiplayer.Error("Attempting to call Chat.SendChatMessage() from multiplayer of type " + multiplayer.type);
			return;
		}
		string campaignId = multiplayer?.game?.campaign?.id;
		if (multiplayer.type == Multiplayer.Type.Server)
		{
			multiplayer.chatServer.RouteMessage(multiplayer.playerData.id, message, channel, whisperTargetId, campaignId);
		}
		else
		{
			multiplayer.SendChatMessage(multiplayer.playerData.id, message, channel, whisperTargetId, campaignId);
		}
	}

	public void ReceivedChatMessage(Campaign campaign, string playerId, string message, Channel channel, string whisperTargetId = null)
	{
		Vars messageVars = GetMessageVars(campaign, playerId, message, channel, whisperTargetId);
		multiplayer.NotifyListenersDelayed("chat_message_received", messageVars, process_triggers: false, profile: false);
	}

	public static Vars GetMessageVars(string campaignId, string playerId, string message, Channel channel, string whisperTargetName)
	{
		Vars vars = new Vars();
		vars.Set("player_id", playerId);
		vars.Set("message", message);
		vars.Set("channel", channel);
		vars.Set("whisper_target_name", whisperTargetName);
		vars.Set("campaign_id", campaignId);
		return vars;
	}

	public static Vars GetMessageVars(Campaign campaign, string playerId, string message, Channel channel, string whisperTargetName)
	{
		Vars messageVars = GetMessageVars(campaign?.id, playerId, message, channel, whisperTargetName);
		ResolveCampaignVars(messageVars, campaign, playerId);
		return messageVars;
	}

	public static void ResolveCampaignVars(Vars messageVars, Campaign campaign, string playerId = null)
	{
		if (campaign != null)
		{
			if (playerId == null)
			{
				playerId = messageVars.Get<string>("player_id");
			}
			int playerIndex = campaign.GetPlayerIndex(playerId);
			Value val = Value.Unknown;
			string val2 = string.Empty;
			if (playerIndex >= 0 && playerIndex < campaign.playerDataPersistent.Length)
			{
				val = campaign.GetPlayerName(playerIndex);
				val2 = campaign.GetKingdomName(playerIndex);
			}
			messageVars.Set("player_name", val);
			messageVars.Set("kingdom_name", val2);
		}
	}

	public static void WriteChatMessage(Serialization.IWriter msg_writer, string senderPlayerId, string message, Channel channel, string whisperTargetId, string campaignId)
	{
		msg_writer.WriteStr(senderPlayerId, "player_id");
		msg_writer.WriteRawStr(message, "message");
		msg_writer.WriteByte((byte)channel, "channel");
		switch (channel)
		{
		case Channel.Whisper:
			if (string.IsNullOrEmpty(whisperTargetId))
			{
				Multiplayer.Error("Sending lobby chat message without whisper target id!");
			}
			msg_writer.WriteStr(whisperTargetId, "whipser_target_id");
			break;
		case Channel.Lobby:
			if (string.IsNullOrEmpty(campaignId))
			{
				Multiplayer.Error("Sending lobby chat message without campaign id!");
			}
			msg_writer.WriteStr(campaignId, "campaign_id");
			break;
		}
	}

	public static void ReadChatMessage(Serialization.IReader reader, out string senderPlayerId, out string message, out Channel channel, out string whisperTargetId, out string campaignId)
	{
		senderPlayerId = reader.ReadStr("player_id");
		message = reader.ReadRawStr("message");
		channel = (Channel)reader.ReadByte("channel");
		if (channel == Channel.Whisper)
		{
			whisperTargetId = reader.ReadStr("whipser_target_id");
			if (string.IsNullOrEmpty(whisperTargetId))
			{
				Multiplayer.Error("Received whisper without target id");
			}
		}
		else
		{
			whisperTargetId = null;
		}
		if (channel == Channel.Lobby)
		{
			campaignId = reader.ReadStr("campaign_id");
			if (string.IsNullOrEmpty(campaignId))
			{
				Multiplayer.Error("Received lobby chat without campaign id");
			}
		}
		else
		{
			campaignId = null;
		}
	}
}
