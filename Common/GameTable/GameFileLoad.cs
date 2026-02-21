using GameServer.GameTable.JsonTable;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GameServer.GameTable
{
	public class GameFileLoad
	{
		public static bool JsonFileLoad(string path, string fileName, IGameTableLoad loader)
		{
			bool isSuccess = true;
			var jsonFilePath = System.IO.Path.Combine("../" + path, fileName + ".json");
			using (var fs = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read))
			{
				using (StreamReader reader = new StreamReader(fs))
				{
					try
					{
						if (loader.LoadTable(reader.ReadToEnd()) == false)
						{
							throw new Exception($"[GameFileLoad] LoadTable 실패: {fileName}");
						}
					}
					catch (Exception ex)
					{
						Trace.TraceError($"[GameFileLoad] JsonFileLoad 실패 - 파일: {fileName}, 오류: {ex}");
						isSuccess = false;
					}
				}
			}
			return isSuccess;
		}

		public static T JsonFileLoad<T>(string path, string fileName) where T : class
		{
			var jsonFilePath = System.IO.Path.Combine("../" + path, fileName + ".json");
			using (var fs = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read))
			{
				using (StreamReader reader = new StreamReader(fs))
				{
					try
					{
						var jsonString = reader.ReadToEnd();
						if (string.IsNullOrEmpty(jsonString) == true)
						{
							throw new Exception($"[GameFileLoad] 빈 JSON 파일: {fileName}");
						}

						return JsonConvert.DeserializeObject<T>(jsonString);
					}
					catch (Exception ex)
					{
						Trace.TraceError($"[GameFileLoad] JsonFileLoad<T> 실패 - 파일: {fileName}, 오류: {ex}");
					}
				}
			}
			return null;
		}
	}
}
