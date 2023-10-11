using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace NinjaTools.Pages.Helpers
{
	public static class Serialization
	{
		public static string FilePath { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NinjaTools", "SaveState.json"); }

		public static void Deserialize(AppViewModel vm)
		{
			if (File.Exists(FilePath))
			{
				using (StreamReader file = new StreamReader(FilePath))
				using (JsonReader reader = new JsonTextReader(file))
				{
					JsonSerializer serializer = new JsonSerializer();
					List<List<string>> paths = serializer.Deserialize<List<List<string>>>(reader);

					if (paths == null)
						return;

					foreach (List<string> path in paths)
					{
						if (Path.GetExtension(path[0]) == string.Empty)
							Helpers.TabHelpers.LoadTab(vm, Directory.GetFiles(path[0]));
						else
							Helpers.TabHelpers.LoadTab(vm, path.ToArray());
					}
				}
			}
		}

		public static void Serialize(AppViewModel vm)
		{
			List<List<string>> paths = new List<List<string>>();

			foreach (IScreen tab in vm.Items)
			{
				List<string> tabItems = new List<string>();
				if (tab is DocumentContainerViewModel)
					tabItems.Add((tab as DocumentContainerViewModel).TreeViewPaths[0].Name);
				else
					tabItems.Add((tab as Interfaces.IStatusBarText).Path);
				paths.Add(tabItems);
			}

			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

			using (StreamWriter file = File.CreateText(FilePath))
			{
				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(file, paths);
			}
		}
	}
}