using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Logic;

public class FileWriter
{
	public delegate void ErrorLog(string message);

	private StreamWriter file;

	public string pathToFile;

	private FileMode fileMode;

	private bool writeFromThread;

	private bool abortThread;

	private Dictionary<string, bool> messageQueue = new Dictionary<string, bool>();

	private object Lock = new object();

	private ErrorLog errorLog;

	public FileWriter(string pathToFile, FileMode fileMode, bool writeFromThread = false, ErrorLog errorLog = null, bool includeHeader = false)
	{
		this.pathToFile = pathToFile;
		this.fileMode = fileMode;
		this.errorLog = errorLog;
		if (includeHeader)
		{
			WriteLine("");
			WriteLine("");
			WriteLine("Logging session " + DateTime.Now.ToString("hh:mm:ss - dd/MM/yy"));
		}
	}

	public void WriteLine(string message)
	{
		lock (Lock)
		{
			if (writeFromThread)
			{
				messageQueue.Add(message, value: true);
			}
			else
			{
				WriteLineToFile(message);
			}
		}
	}

	public void Write(string message)
	{
		lock (Lock)
		{
			if (writeFromThread)
			{
				messageQueue.Add(message, value: false);
			}
			else
			{
				WriteToFile(message);
			}
		}
	}

	private void WriteToFile(string message)
	{
		try
		{
			using (file = new StreamWriter(new FileStream(pathToFile, fileMode, FileAccess.Write)))
			{
				file.Write(message);
			}
		}
		catch (Exception ex)
		{
			string text = "Error in FileWriter: " + ex.ToString();
			if (errorLog != null)
			{
				errorLog(text);
			}
			else
			{
				Console.WriteLine(text);
			}
		}
	}

	private void WriteLineToFile(string message)
	{
		try
		{
			using (file = new StreamWriter(new FileStream(pathToFile, fileMode, FileAccess.Write)))
			{
				file.WriteLine(message);
			}
		}
		catch (Exception ex)
		{
			string text = "Error in FileWriter: " + ex.ToString();
			if (errorLog != null)
			{
				errorLog(text);
			}
			else
			{
				Console.WriteLine(text);
			}
		}
	}

	private void WriteToFileThread()
	{
		while (!abortThread)
		{
			Dictionary<string, bool> dictionary = null;
			lock (Lock)
			{
				if (messageQueue.Count > 0)
				{
					dictionary = messageQueue;
					messageQueue = new Dictionary<string, bool>();
				}
			}
			if (dictionary != null && dictionary.Count > 0)
			{
				foreach (KeyValuePair<string, bool> item in dictionary)
				{
					if (item.Value)
					{
						WriteLineToFile(item.Key);
					}
					else
					{
						WriteToFile(item.Key);
					}
				}
			}
			Thread.Sleep(1000);
		}
	}

	public void Dispose()
	{
		if (writeFromThread)
		{
			abortThread = true;
		}
	}

	public void Log(string message)
	{
		WriteLine(DateTime.Now.ToString("hh:mm:ss ") + message);
	}
}
