using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Logic;

public class ModManager
{
	public enum ModState
	{
		MissingMod,
		NotActive,
		ActiveMod
	}

	public const string BaseChecksumFileName = "base_checksum.txt";

	private List<Mod> _mods = new List<Mod>();

	private readonly List<Mod> _activeMods = new List<Mod>();

	public System.Action OnActiveModsChanged;

	private System.Action _onLoadModsStarted;

	private System.Action _onLoadModsChanged;

	private System.Action _onLoadModsCompleted;

	private static ModManager _instance = null;

	public readonly string BaseChecksum;

	public const string EmptyChecksum = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

	public bool BaseChecksumValid;

	public readonly string ProjectPath;

	public static List<Mod.Conflicts> mod_conflicts = new List<Mod.Conflicts>();

	private static readonly List<string> NonBinaryFileExtensions = new List<string> { ".def", ".csv", ".wiki" };

	public static Mod LoadingMod = null;

	private ModManager()
	{
		THQNORequest.Connect();
		BaseChecksum = ReadChecksumFromFile(Application.dataPath + "/../base_checksum.txt");
		ProjectPath = Application.dataPath + "/../";
	}

	public static ModManager Get(bool forceCreate = false)
	{
		if (!Game.IsPlaying())
		{
			return null;
		}
		if (_instance != null)
		{
			return _instance;
		}
		if (!forceCreate)
		{
			return null;
		}
		_instance = new ModManager();
		return _instance;
	}

	private Mod LoadMod(Mod.ModInfo modInfo)
	{
		if (!modInfo.Path.Exists)
		{
			return null;
		}
		Mod mod = new Mod(modInfo);
		byte[] checksumFromDefs = GetChecksumFromDefs(mod.base_path, mod.binary_file_changes);
		mod.checksum = Checksum.GetHashString(checksumFromDefs);
		mod.LoadChanges();
		return mod;
	}

	private static bool HasModFolderName(DirectoryInfo dir)
	{
		if (!dir.Name.Equals("defs", StringComparison.OrdinalIgnoreCase) && !dir.Name.Equals("maps", StringComparison.OrdinalIgnoreCase))
		{
			return dir.Name.Equals("texts", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public string GetActiveModsSaveString(bool ignore_texts_only = true)
	{
		string text = "";
		foreach (Mod activeMod in _activeMods)
		{
			if (!ignore_texts_only || !activeMod.IsTextsOnly)
			{
				if (text != "")
				{
					text += ";";
				}
				text += activeMod.GetSaveString();
			}
		}
		return text;
	}

	public bool HasActiveMods(bool ignore_texts_only = true)
	{
		if (!ignore_texts_only)
		{
			return _activeMods.Count > 0;
		}
		for (int i = 0; i < _activeMods.Count; i++)
		{
			if (!_activeMods[i].IsTextsOnly)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsVanillaGame()
	{
		if (!HasActiveMods())
		{
			return BaseChecksumValid;
		}
		return false;
	}

	public bool IsGameModified()
	{
		return !IsVanillaGame();
	}

	private static bool AreCompatibleMods(Mod mod1, Mod mod2)
	{
		bool flag = mod1 == null || string.IsNullOrEmpty(mod1.checksum);
		bool flag2 = mod2 == null || string.IsNullOrEmpty(mod2.checksum);
		if (flag || flag2)
		{
			return flag == flag2;
		}
		return mod1.checksum.Equals(mod2.checksum);
	}

	public bool HasCompatibleActiveMod(Mod m)
	{
		foreach (Mod activeMod in _activeMods)
		{
			if (AreCompatibleMods(activeMod, m))
			{
				return true;
			}
		}
		return false;
	}

	public List<Mod> GetAllMods()
	{
		return _mods;
	}

	public static string GetModdedAssetPath(string org_path, bool allow_unmodded_path = false)
	{
		if (string.IsNullOrEmpty(org_path))
		{
			return null;
		}
		string result = org_path;
		ModManager modManager = Get();
		if (modManager == null)
		{
			if (!allow_unmodded_path)
			{
				return null;
			}
			return result;
		}
		if (modManager._activeMods.Count == 0)
		{
			if (!allow_unmodded_path)
			{
				return null;
			}
			return result;
		}
		if (org_path.StartsWith("assets/", StringComparison.InvariantCultureIgnoreCase))
		{
			org_path = org_path.Substring(7);
		}
		if (org_path.StartsWith("UMA/", StringComparison.InvariantCultureIgnoreCase))
		{
			org_path = org_path.Substring(4);
		}
		for (int i = 0; i < modManager._activeMods.Count; i++)
		{
			string text = System.IO.Path.Combine(modManager._activeMods[i].base_path, org_path);
			if (File.Exists(text))
			{
				return text;
			}
		}
		if (!allow_unmodded_path)
		{
			return null;
		}
		return result;
	}

	public Mod.Conflicts DetectModConflicts(Mod mod)
	{
		Mod.Conflicts conflicts = null;
		for (int i = 0; i < _activeMods.Count; i++)
		{
			Mod mod2 = _activeMods[i];
			if (mod2 == mod)
			{
				continue;
			}
			Mod.ModAndModConflicts modAndModConflicts = mod.DetectConflicts(mod2);
			if (modAndModConflicts != null)
			{
				if (conflicts == null)
				{
					conflicts = new Mod.Conflicts
					{
						mod = mod
					};
				}
				conflicts.per_mod.Add(modAndModConflicts);
			}
		}
		if (conflicts == null)
		{
			return null;
		}
		mod_conflicts.Add(conflicts);
		conflicts.Log();
		return conflicts;
	}

	public void LoadModList()
	{
		using (Game.Profile("LoadModsList", log: true))
		{
			_onLoadModsStarted?.Invoke();
			if (_mods == null)
			{
				_mods = new List<Mod>();
			}
			_mods.Clear();
			List<Mod.ModInfo> allModInfos = GetAllModInfos();
			if (allModInfos.Count == 0)
			{
				_onLoadModsCompleted?.Invoke();
				return;
			}
			foreach (Mod.ModInfo item in allModInfos)
			{
				LoadModInternal(item);
			}
			ResolveActiveMods();
			_onLoadModsCompleted?.Invoke();
			OnActiveModsChanged?.Invoke();
		}
	}

	private void ResolveActiveMods()
	{
		for (int i = 0; i < _activeMods.Count; i++)
		{
			Mod mod = _activeMods[i];
			Mod modByDirectoryCode = GetModByDirectoryCode(mod.GetDirectoryCode());
			if (modByDirectoryCode != mod)
			{
				if (modByDirectoryCode == null)
				{
					_activeMods.RemoveAt(i);
					i--;
				}
				else
				{
					_activeMods[i] = modByDirectoryCode;
				}
			}
		}
	}

	public static string GetModDisplayName(DirectoryInfo path)
	{
		if (!path.Exists)
		{
			return "";
		}
		FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(path.FullName, "mod_name.txt"));
		string text = "";
		if (fileInfo.Exists)
		{
			try
			{
				text = File.ReadAllText(fileInfo.FullName);
			}
			catch
			{
			}
		}
		if (text == "")
		{
			text = path.Name;
		}
		return text;
	}

	private void LoadModInternal(Mod.ModInfo modInfo)
	{
		Mod mod = LoadMod(modInfo);
		if (mod == null)
		{
			return;
		}
		if (mod.WorkshopId != 0L)
		{
			foreach (Mod mod2 in _mods)
			{
				if (mod.WorkshopId == mod2.WorkshopId)
				{
					return;
				}
			}
		}
		_mods.Add(mod);
		_onLoadModsChanged?.Invoke();
	}

	public bool IsValid(Mod mod)
	{
		return mod?.Exists ?? false;
	}

	public List<Mod> GetActiveMods()
	{
		return _activeMods;
	}

	private void AddActiveMod(Mod mod)
	{
		_activeMods.Add(mod);
		OnActiveModsChanged?.Invoke();
	}

	public void DeactivateMod(Mod mod)
	{
		if (_activeMods.Remove(mod))
		{
			OnActiveModsChanged?.Invoke();
		}
	}

	public void ClearActiveMods()
	{
		if (_activeMods != null && _activeMods.Count > 0)
		{
			_activeMods.Clear();
			OnActiveModsChanged?.Invoke();
		}
	}

	private static int GetNumSubscribedItems()
	{
		THQNORequest numSubscribedItems = THQNORequest.GetNumSubscribedItems();
		if (numSubscribedItems.error != null)
		{
			return 0;
		}
		return numSubscribedItems.result.Int();
	}

	private static List<ulong> GetSubscribedItems()
	{
		THQNORequest subscribedItems = THQNORequest.GetSubscribedItems();
		if (subscribedItems.error != null)
		{
			return new List<ulong>();
		}
		return subscribedItems.result.obj_val as List<ulong>;
	}

	private static List<Mod.ModInfo> GetLocalModInfos(string directory)
	{
		List<Mod.ModInfo> list = new List<Mod.ModInfo>();
		DirectoryInfo directoryInfo = new DirectoryInfo(directory);
		if (!directoryInfo.Exists)
		{
			return list;
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		for (int i = 0; i < directories.Length; i++)
		{
			DirectoryInfo directoryInfo2;
			if (!(directoryInfo2 = directories[i]).Exists)
			{
				continue;
			}
			Mod.ModInfo item = new Mod.ModInfo
			{
				Path = directoryInfo2,
				WorkshopId = 0uL
			};
			string path = System.IO.Path.Combine(directoryInfo2.FullName, "mod_workshop_id.txt");
			if (File.Exists(path))
			{
				try
				{
					ulong workshopId = ulong.Parse(File.ReadAllText(path));
					item.WorkshopId = workshopId;
				}
				catch
				{
				}
			}
			list.Add(item);
		}
		return list;
	}

	private static List<Mod.ModInfo> GetWorkshopModInfos()
	{
		List<Mod.ModInfo> list = new List<Mod.ModInfo>();
		try
		{
			THQNORequest.Connect();
			List<ulong> subscribedItems = GetSubscribedItems();
			if (subscribedItems == null || subscribedItems.Count == 0)
			{
				return list;
			}
			for (int i = 0; i < subscribedItems.Count; i++)
			{
				try
				{
					if ((THQNORequest.GetItemState(subscribedItems[i]).result.Int() & 4) == 0)
					{
						continue;
					}
					string text = THQNORequest.GetItemInstallInfo(subscribedItems[i]).result.String();
					if (!string.IsNullOrEmpty(text))
					{
						DirectoryInfo directoryInfo = new DirectoryInfo(text);
						if (directoryInfo.Exists)
						{
							Mod.ModInfo item = new Mod.ModInfo
							{
								Path = directoryInfo,
								WorkshopId = subscribedItems[i]
							};
							list.Add(item);
						}
					}
				}
				catch (Exception arg)
				{
					Game.Log($"Error getting workshop item {i} info: {arg}", Game.LogType.Error);
				}
			}
		}
		catch (Exception arg2)
		{
			Game.Log($"Error getting workshop mods info: {arg2}", Game.LogType.Error);
		}
		return list;
	}

	private static List<Mod.ModInfo> GetAllModInfos()
	{
		List<Mod.ModInfo> localModInfos = GetLocalModInfos(GetModsDir());
		localModInfos.Sort(ModCompareOrdinal);
		List<Mod.ModInfo> workshopModInfos = GetWorkshopModInfos();
		workshopModInfos.Sort(ModCompareOrdinal);
		localModInfos.AddRange(workshopModInfos);
		return localModInfos;
		static int ModCompareOrdinal(Mod.ModInfo m1, Mod.ModInfo m2)
		{
			return string.CompareOrdinal(m1.Path.Name, m2.Path.Name);
		}
	}

	public bool IsActiveMod(Mod mod)
	{
		return _activeMods.Contains(mod);
	}

	public bool ActivateMod(Mod mod)
	{
		if (!IsValid(mod))
		{
			return false;
		}
		if (IsActiveMod(mod))
		{
			return false;
		}
		if (DetectModConflicts(mod) != null)
		{
			return false;
		}
		AddActiveMod(mod);
		return true;
	}

	public string GetActiveModsString()
	{
		string text = "";
		for (int i = 0; i < _activeMods.Count; i++)
		{
			Mod mod = _activeMods[i];
			if (i > 0)
			{
				text += ";";
			}
			text += mod.GetDirectoryCode();
		}
		return text;
	}

	public Mod GetModByDirectoryCode(string dirCode)
	{
		foreach (Mod mod in _mods)
		{
			if (string.Equals(dirCode, mod.GetDirectoryCode(), StringComparison.Ordinal))
			{
				return mod;
			}
		}
		return null;
	}

	public Mod GetModBySaveString(string str)
	{
		int num = str.IndexOf('/');
		if (num < 0)
		{
			return null;
		}
		string text = str.Substring(0, num);
		string text2 = str.Substring(num + 1);
		Mod mod = null;
		foreach (Mod mod2 in _mods)
		{
			if (!(mod2.checksum != text2))
			{
				if (mod2.DisplayName == text)
				{
					return mod2;
				}
				if (mod == null)
				{
					mod = mod2;
				}
			}
		}
		return mod;
	}

	public ModState GetModState(string modId)
	{
		Mod modByDirectoryCode = GetModByDirectoryCode(modId);
		if (modByDirectoryCode == null)
		{
			return ModState.MissingMod;
		}
		if (IsActiveMod(modByDirectoryCode))
		{
			return ModState.ActiveMod;
		}
		return ModState.NotActive;
	}

	public string CalculateFilesChecksumHex()
	{
		return Checksum.GetHashString(GetChecksumFromDefs(ProjectPath));
	}

	public bool VerifyFilesChecksum()
	{
		string text = CalculateFilesChecksumHex();
		return BaseChecksum == text;
	}

	public void UpdateBaseChecksumValidity()
	{
		BaseChecksumValid = VerifyFilesChecksum();
	}

	public static string GetModsDir()
	{
		return Game.PersistentDataPath() + "/Mods";
	}

	private static string ReadChecksumFromFile(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		FileStream fileStream = File.OpenRead(path);
		StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8);
		string result = streamReader.ReadToEnd();
		streamReader.Close();
		fileStream.Close();
		return result;
	}

	public static byte[] GetChecksumFromDefs(string baseFolder, Dictionary<string, string> binary_file_checksums = null)
	{
		List<FileInfo> modFiles = GetModFiles(baseFolder, includeTexts: false);
		modFiles.Sort((FileInfo x, FileInfo y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
		return GetChecksum(baseFolder, modFiles, binary_file_checksums);
	}

	private static List<FileInfo> GetModFiles(string baseFolder, bool includeTexts = true)
	{
		List<FileInfo> files = new List<FileInfo>();
		GetFiles(System.IO.Path.Combine(baseFolder, "defs"));
		GetFiles(System.IO.Path.Combine(baseFolder, "maps"));
		if (includeTexts)
		{
			GetFiles(System.IO.Path.Combine(baseFolder, "texts"));
		}
		GetFiles(System.IO.Path.Combine(baseFolder, "Portraits"));
		return files;
		void GetFiles(string path)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (directoryInfo.Exists)
			{
				FileInfo[] files2 = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
				files.AddRange(files2);
			}
		}
	}

	public static bool IsBinaryFile(FileInfo file)
	{
		if (file == null)
		{
			return false;
		}
		string extension = file.Extension;
		for (int i = 0; i < NonBinaryFileExtensions.Count; i++)
		{
			if (NonBinaryFileExtensions[i].Equals(extension, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	private static byte[] GetChecksum(string baseFolder, List<FileInfo> files, Dictionary<string, string> binary_file_checksums = null)
	{
		if (binary_file_checksums != null)
		{
			baseFolder = new DirectoryInfo(baseFolder).FullName;
			if (baseFolder[baseFolder.Length - 1] != System.IO.Path.DirectorySeparatorChar)
			{
				baseFolder += System.IO.Path.DirectorySeparatorChar;
			}
		}
		LocalChecksum localChecksum = null;
		LocalChecksum localChecksum2 = new LocalChecksum();
		localChecksum2.BeginChecksum();
		foreach (FileInfo file in files)
		{
			if (file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (binary_file_checksums == null || !IsBinaryFile(file))
			{
				localChecksum2.FeedChecksum(file);
				continue;
			}
			if (localChecksum == null)
			{
				localChecksum = new LocalChecksum();
			}
			localChecksum.BeginChecksum();
			localChecksum.FeedChecksum(file);
			byte[] array = localChecksum.EndChecksum();
			localChecksum2.FeedChecksum(array);
			string hashString = Checksum.GetHashString(array);
			string text = file.FullName;
			if (text.StartsWith(baseFolder, StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(baseFolder.Length);
			}
			binary_file_checksums.Add(text, hashString);
		}
		return localChecksum2.EndChecksum();
	}

	public static bool IsLoadingMod()
	{
		return LoadingMod != null;
	}

	public static bool IsPureDelete(DT.Field f)
	{
		if (f == null)
		{
			return true;
		}
		if (f.type != "delete")
		{
			return false;
		}
		if (!string.IsNullOrEmpty(f.base_path))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(f.value_str))
		{
			return false;
		}
		if (f.children != null && f.children.Count > 0)
		{
			return false;
		}
		return true;
	}
}
