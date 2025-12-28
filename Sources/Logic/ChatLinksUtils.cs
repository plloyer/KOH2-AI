using System;

namespace Logic;

public static class ChatLinksUtils
{
	public static readonly string LinkOpeningTagBeforeID = "<link=\"obj:";

	public static readonly string LinkOpeningTagAfterID = "\">";

	public static readonly string LinkClosingTag = "</link>";

	public static readonly string MarkupOpeningTag = "[_obj_info_]";

	public static readonly string MarkupClosingTag = "[/_obj_info_]";

	public static readonly string MarkupDelimiter = "#";

	public static readonly string CaretOpeningTag = "link";

	public static readonly string CaretClosingTag = "/link";

	public static readonly string NoParseOpeningTag = "<noparse>";

	public static readonly string NoParseClosingTag = "</noparse>";

	public static string GetObjectMarkup(Game game, string uid)
	{
		if (game == null)
		{
			return null;
		}
		if (!int.TryParse(uid, out var result))
		{
			Game.Log("Linked object UID " + uid + " cannot be parsed to int.", Game.LogType.Error);
			return null;
		}
		Object obj = game.FindObjectByUIDVerySlow(result);
		if (obj?.rtti?.ti == null)
		{
			return null;
		}
		string text = obj.rtti.ti.tid.ToString();
		string text2 = obj.GetNid(generateNid: false).ToString();
		return MarkupOpeningTag + text + MarkupDelimiter + text2 + MarkupClosingTag;
	}

	public static Object GetObjectFromMarkup(Game game, string objectInfo)
	{
		Serialization.ObjectType result;
		int result2;
		try
		{
			int num = objectInfo.IndexOf(MarkupDelimiter);
			if (num <= -1)
			{
				Game.Log(objectInfo + " is invalid markup for object information", Game.LogType.Error);
				return null;
			}
			string text = objectInfo.Substring(0, num);
			string text2 = objectInfo.Substring(num + MarkupDelimiter.Length);
			if (!Enum.TryParse<Serialization.ObjectType>(text, out result))
			{
				Game.Log("Object tid " + text + " cannot be parsed to ObjectType.", Game.LogType.Error);
				return null;
			}
			if (!int.TryParse(text2, out result2))
			{
				Game.Log("Object nid " + text2 + " cannot be parsed to int.", Game.LogType.Error);
				return null;
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
			return null;
		}
		return game.multiplayer.objects.Get(result, result2);
	}

	public static string ConvertLinksToMarkup(Game game, string message)
	{
		if (game == null)
		{
			Game.Log("Game is null", Game.LogType.Error);
			return null;
		}
		int num = 0;
		try
		{
			while (num > -1)
			{
				num = message.IndexOf(LinkOpeningTagBeforeID);
				if (num != -1)
				{
					int matchingClosingTagIndex = GetMatchingClosingTagIndex(message, num);
					string uIDFromLink = GetUIDFromLink(message.Substring(num, matchingClosingTagIndex + LinkClosingTag.Length - num));
					if (uIDFromLink != null)
					{
						string objectMarkup = GetObjectMarkup(game, uIDFromLink);
						if (objectMarkup != null)
						{
							message = message.Substring(0, num) + objectMarkup + message.Substring(matchingClosingTagIndex + LinkClosingTag.Length);
							continue;
						}
						break;
					}
					break;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return message;
	}

	public static string GetUIDFromLink(string link)
	{
		string result = string.Empty;
		try
		{
			int num = link.IndexOf(LinkOpeningTagBeforeID);
			int num2 = link.IndexOf(LinkOpeningTagAfterID);
			if (num < 0 || num2 < num)
			{
				return null;
			}
			result = link.Substring(num + LinkOpeningTagBeforeID.Length, num2 - (num + LinkOpeningTagBeforeID.Length));
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return result;
	}

	public static string HandleInvalidLinks(string message, int currentStringPosition, out int stringPosition)
	{
		return HandleInvalidClosingLinkTags(HandleInvalidOpeningLinkTags(message), currentStringPosition, out stringPosition);
	}

	public static string HandleInvalidOpeningLinkTags(string message)
	{
		try
		{
			string value = LinkOpeningTagBeforeID.Substring(1, LinkOpeningTagBeforeID.Length - 1);
			int num = 0;
			int startIndex = 0;
			while (num > -1)
			{
				num = message.IndexOf(value, startIndex);
				if (num != -1)
				{
					if (num > 0 && message.Length > num - 1 && message[num - 1] == '<')
					{
						startIndex = num + 1;
						continue;
					}
					int closingTagIndex = message.IndexOf(LinkClosingTag, num);
					closingTagIndex = GetMatchingClosingTagIndex(message, num, closingTagIndex);
					message = message.Substring(0, num) + message.Substring(closingTagIndex + LinkClosingTag.Length);
					continue;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return message;
	}

	public static string HandleInvalidClosingLinkTags(string message, int currentStringPosition, out int stringPosition)
	{
		stringPosition = currentStringPosition;
		try
		{
			string text = LinkClosingTag.Substring(0, LinkClosingTag.Length - 1);
			int num = 0;
			int startIndex = message.Length;
			while (num > -1 && !string.IsNullOrEmpty(message))
			{
				num = message.LastIndexOf(text, startIndex);
				if (num != -1)
				{
					int matchingOpeningTagIndex = GetMatchingOpeningTagIndex(message, num);
					if (num > 0 && num + text.Length < message.Length && message[num + text.Length] == '>')
					{
						startIndex = matchingOpeningTagIndex;
						continue;
					}
					message = ((num + LinkClosingTag.Length >= message.Length) ? message.Substring(0, matchingOpeningTagIndex) : (message.Substring(0, matchingOpeningTagIndex) + message.Substring(num + text.Length)));
					startIndex = matchingOpeningTagIndex;
					stringPosition = matchingOpeningTagIndex;
					continue;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return message;
	}

	public static int GetMatchingOpeningTagIndex(string message, int closingTagIndex)
	{
		int num = 0;
		try
		{
			num = message.LastIndexOf(LinkOpeningTagBeforeID, closingTagIndex);
			int num3;
			do
			{
				if (closingTagIndex < num)
				{
					Game.Log($"Error while processing link tags. Opening tag index: {num}, closing tag index: {closingTagIndex}", Game.LogType.Error);
					return -1;
				}
				int num2 = message.IndexOf(LinkOpeningTagAfterID, num);
				if (!AreOpeningAndClosingLinkTagsEqualInNumber(message.Substring(num2, closingTagIndex - num2), LinkOpeningTagBeforeID, LinkClosingTag))
				{
					num3 = num;
					num = message.LastIndexOf(LinkOpeningTagBeforeID, num);
					continue;
				}
				break;
			}
			while (num3 != num);
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return num;
	}

	public static int GetMatchingClosingTagIndex(string message, int openingTagIndex, int closingTagIndex = -1)
	{
		try
		{
			if (closingTagIndex == -1)
			{
				closingTagIndex = message.IndexOf(LinkClosingTag, openingTagIndex);
			}
			int num = message.IndexOf(LinkOpeningTagBeforeID, openingTagIndex + LinkOpeningTagBeforeID.Length, closingTagIndex - (openingTagIndex + LinkOpeningTagBeforeID.Length));
			while (num > -1)
			{
				closingTagIndex = message.IndexOf(LinkClosingTag, closingTagIndex + LinkClosingTag.Length);
				if (num + LinkOpeningTagBeforeID.Length + (closingTagIndex - num) <= message.Length)
				{
					num = message.IndexOf(LinkOpeningTagBeforeID, num + LinkOpeningTagBeforeID.Length, closingTagIndex - num);
					continue;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
			closingTagIndex = 0;
		}
		return closingTagIndex;
	}

	public static int GetOuterOpeningTagIndex(string message, string openingTag, string closingTag, int position)
	{
		if (position > message.Length)
		{
			Game.Log($"Position {position} is not within the message with length: {message.Length}", Game.LogType.Error);
			return -1;
		}
		int result = 0;
		int num = 0;
		int num2 = -1;
		while (++num2 < position)
		{
			if (message[num2] != '>')
			{
				continue;
			}
			try
			{
				if (num2 - openingTag.Length > 0 && message.Substring(num2 - openingTag.Length + 1, openingTag.Length) == openingTag)
				{
					if (num == 0)
					{
						result = message.LastIndexOf(LinkOpeningTagBeforeID, num2);
					}
					num++;
				}
				else if (num2 - closingTag.Length > 0 && message.Substring(num2 - closingTag.Length + 1, closingTag.Length) == closingTag)
				{
					num--;
				}
			}
			catch (Exception ex)
			{
				Game.Log(ex.ToString(), Game.LogType.Error);
			}
		}
		return result;
	}

	public static bool AreOpeningAndClosingLinkTagsEqualInNumber(string message, string openingTag, string closingTag)
	{
		int num = CountOfTagsInMessage(message, openingTag);
		int num2 = CountOfTagsInMessage(message, closingTag);
		return num == num2;
	}

	public static int CountOfTagsInMessage(string message, string tag)
	{
		int num = 0;
		int startIndex = 0;
		try
		{
			while ((startIndex = message.IndexOf(tag, startIndex)) != -1)
			{
				startIndex += tag.Length;
				num++;
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return num;
	}

	public static int HandleCaretPosition(string message, int caretPosition)
	{
		if (string.IsNullOrEmpty(message))
		{
			return 0;
		}
		try
		{
			if (caretPosition - 1 >= 0 && caretPosition - 1 < message.Length && caretPosition + CaretOpeningTag.Length < message.Length && message[caretPosition - 1] == '<' && message.Substring(caretPosition, CaretOpeningTag.Length) == CaretOpeningTag)
			{
				return GetMatchingClosingTagIndex(message, caretPosition - 1) + LinkClosingTag.Length;
			}
			if (caretPosition >= 0 && caretPosition < message.Length && caretPosition - CaretClosingTag.Length > 0 && message[caretPosition] == '>' && message.Substring(caretPosition - CaretClosingTag.Length, CaretClosingTag.Length) == CaretClosingTag)
			{
				return GetMatchingOpeningTagIndex(message, caretPosition);
			}
			if (caretPosition > 0 && caretPosition < message.Length)
			{
				if (caretPosition + LinkClosingTag.Length < message.Length && message.Substring(caretPosition, LinkClosingTag.Length) == LinkClosingTag)
				{
					return caretPosition + LinkClosingTag.Length;
				}
				if (!AreOpeningAndClosingLinkTagsEqualInNumber(message.Substring(0, caretPosition), LinkOpeningTagBeforeID, LinkClosingTag))
				{
					return GetOuterOpeningTagIndex(message, LinkOpeningTagAfterID, LinkClosingTag, caretPosition);
				}
			}
		}
		catch (Exception ex)
		{
			Game.Log(ex.ToString(), Game.LogType.Error);
		}
		return caretPosition;
	}

	public static string ColorizeLinks(string message, string colorOpeningTag)
	{
		message = message.Replace(LinkOpeningTagAfterID, LinkOpeningTagAfterID + colorOpeningTag);
		message = message.Replace(LinkClosingTag, "</color>" + LinkClosingTag);
		return message;
	}
}
