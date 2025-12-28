using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Logic;

public class Checksum
{
	private static SHA1Managed sha = null;

	public static Encoding encoding = Encoding.UTF8;

	public static bool enabled = true;

	public static void ResetChecksum()
	{
		if (enabled)
		{
			BeginChecksum();
		}
	}

	public static void BeginChecksum()
	{
		if (enabled)
		{
			if (sha != null)
			{
				sha.Dispose();
			}
			sha = new SHA1Managed();
		}
	}

	public static void FeedChecksum(byte[] bytes)
	{
		if (enabled && sha != null)
		{
			sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
		}
	}

	public static void FeedChecksum(string input)
	{
		if (enabled && sha != null)
		{
			byte[] bytes = encoding.GetBytes(input);
			sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
		}
	}

	public static void FeedChecksum(FileInfo file)
	{
		if (!enabled || sha == null)
		{
			return;
		}
		try
		{
			FileStream fileStream = file.OpenRead();
			int num = (int)fileStream.Length;
			byte[] array = new byte[num];
			fileStream.Read(array, 0, num);
			fileStream.Close();
			sha.TransformBlock(array, 0, array.Length, array, 0);
		}
		catch (IOException ex)
		{
			Game.Log("I/O Exception: " + ex.Message, Game.LogType.Message);
		}
		catch (UnauthorizedAccessException ex2)
		{
			Game.Log("Access Exception: " + ex2.Message, Game.LogType.Message);
		}
	}

	public static void FeedChecksum(DirectoryInfo directory, SearchOption searchOption)
	{
		if (!enabled || sha == null)
		{
			return;
		}
		try
		{
			FileInfo[] files = directory.GetFiles("*", searchOption);
			Array.Sort(files, (FileInfo x, FileInfo y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
			for (int num = 0; num < files.Length; num++)
			{
				FeedChecksum(files[num]);
			}
		}
		catch (IOException ex)
		{
			Game.Log("I/O Exception: " + ex.Message, Game.LogType.Message);
		}
		catch (UnauthorizedAccessException ex2)
		{
			Game.Log("Access Exception: " + ex2.Message, Game.LogType.Message);
		}
	}

	public static byte[] EndChecksum()
	{
		if (!enabled)
		{
			return null;
		}
		if (sha == null)
		{
			return null;
		}
		byte[] array = new byte[0];
		sha.TransformFinalBlock(array, 0, array.Length);
		byte[] hash = sha.Hash;
		sha.Dispose();
		sha = null;
		return hash;
	}

	public static byte[] GetSingleHash(FileInfo file)
	{
		if (!enabled)
		{
			return null;
		}
		byte[] result = new byte[0];
		using (SHA256 sHA = SHA256.Create())
		{
			try
			{
				FileStream fileStream = file.Open(FileMode.Open);
				result = sHA.ComputeHash(fileStream);
				fileStream.Close();
			}
			catch (IOException ex)
			{
				Game.Log("I/O Exception: " + ex.Message, Game.LogType.Message);
			}
			catch (UnauthorizedAccessException ex2)
			{
				Game.Log("Access Exception: " + ex2.Message, Game.LogType.Message);
			}
		}
		return result;
	}

	public static void LogHash(string name, byte[] hash)
	{
		if (!enabled)
		{
			return;
		}
		string text = name;
		text = text + hash.Length + "B ";
		for (int i = 0; i < hash.Length; i++)
		{
			text += $"{hash[i]:X2}";
			if (i % 4 == 3)
			{
				text += " ";
			}
		}
		Game.Log(text, Game.LogType.Message);
	}

	public static string GetHashString(byte[] hash)
	{
		string text = "";
		for (int i = 0; i < hash.Length; i++)
		{
			text += $"{hash[i]:X2}";
		}
		return text;
	}
}
