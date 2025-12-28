using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Logic;

public class LocalChecksum
{
	private SHA1Managed sha;

	public static Encoding encoding = Encoding.UTF8;

	public static bool enabled = true;

	public void ResetChecksum()
	{
		if (enabled)
		{
			BeginChecksum();
		}
	}

	public void BeginChecksum()
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

	public void FeedChecksum(byte[] bytes)
	{
		if (enabled && sha != null)
		{
			sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
		}
	}

	public void FeedChecksum(string input)
	{
		if (enabled && sha != null)
		{
			byte[] bytes = encoding.GetBytes(input);
			sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
		}
	}

	public void FeedChecksum(FileInfo file)
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

	public void FeedChecksum(DirectoryInfo directory, SearchOption searchOption)
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

	public byte[] EndChecksum()
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
}
