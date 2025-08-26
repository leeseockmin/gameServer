using GameServer.GameTable.JsonTable;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
							throw new Exception();
						}
					}
					catch(Exception ex)
					{
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
						if(string.IsNullOrEmpty(jsonString) == true)
						{
							throw new Exception(jsonString);
						}

						return JsonConvert.DeserializeObject<T>(jsonString);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }

                }
            }
			return null;

        }
	}
}
