using System;
using System.Collections.Generic;
using System.IO;

namespace AlaunchX;

public class AlaunchXAnswerFile
{
	public class Action
	{
		public string ModulePN = string.Empty;

		public string Path = string.Empty;

		public string Type = string.Empty;
	}

	public string FilePath = string.Empty;

	public int TotalAction;

	public int Done;

	public string Window = string.Empty;

	public string Brand = string.Empty;

	public string Para = string.Empty;

	public List<Action> Actions = new List<Action>();

	public AlaunchXAnswerFile(string filePath)
	{
		FilePath = filePath;
		if (!File.Exists(filePath))
		{
			return;
		}
		string iniKeyValue = Utility.GetIniKeyValue(filePath, "Main", "Action");
		try
		{
			TotalAction = Convert.ToInt32(iniKeyValue);
		}
		catch (Exception)
		{
			TotalAction = 0;
		}
		string iniKeyValue2 = Utility.GetIniKeyValue(filePath, "Main", "Done");
		try
		{
			Done = Convert.ToInt32(iniKeyValue2);
		}
		catch (Exception)
		{
			Done = 0;
		}
		Window = Utility.GetIniKeyValue(filePath, "Main", "Window");
		Brand = Utility.GetIniKeyValue(filePath, "Main", "Brand");
		Para = Utility.GetIniKeyValue(filePath, "Main", "Para");
		if (TotalAction > 0)
		{
			for (int i = 0; i < TotalAction; i++)
			{
				Action item = new Action
				{
					ModulePN = Utility.GetIniKeyValue(filePath, "Action" + (i + 1), "Module"),
					Path = Utility.GetIniKeyValue(filePath, "Action" + (i + 1), "Path"),
					Type = Utility.GetIniKeyValue(filePath, "Action" + (i + 1), "Type")
				};
				Actions.Add(item);
			}
		}
	}
}
