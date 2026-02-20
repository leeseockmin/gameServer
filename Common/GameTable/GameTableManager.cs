using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GameServer.GameTable.JsonTable;

namespace GameServer.GameTable
{
    public class GameTableManager
	{
		public GameTableManager()
		{
		}

		private Dictionary<string, IGameTableLoad> TableInfo = new Dictionary<string, IGameTableLoad>();
		public ItemTableManager ItemTableManager = new ItemTableManager();

		// 초기 TableLoad
		public void SetTable()
		{
			TableInfo.Add("ItemTable", ItemTableManager);
		}

		public bool Init(string path)
		{
			SetTable();
			int failCount = 0;
			var pOption = new ParallelOptions();

			Parallel.ForEach(TableInfo, pOption, table =>
			{
				if (GameFileLoad.JsonFileLoad(path, table.Key, table.Value) == false)
				{
					++failCount;
				}
			});
			if (failCount > 0)
			{
				// 에러 처리
			}
			failCount = 0;

			Parallel.ForEach(TableInfo, pOption, table =>
			{
				if (table.Value.AfterLoad() == false)
				{
					++failCount;
				}
			});
			if (failCount > 0)
			{
				// 에러 처리
			}
			return true;
		}
	}
}
