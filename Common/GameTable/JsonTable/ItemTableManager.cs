using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.GameTable.JsonTable
{
	public class ItemTableManager : IGameTableLoad
	{
		Dictionary<int, ItemTable> ItemTables = null;
		public class ItemTable
		{
			public int Id { get; set; }
			public ItemType ItemType { get; set; }
			public int ItemId { get; set; }
		}
		
		public bool LoadTable(string json)
		{
			ItemTables = new Dictionary<int, ItemTable>();

			ItemTable[] items = Newtonsoft.Json.JsonConvert.DeserializeObject<ItemTable[]>(json);
			foreach(var item in items)
			{
				ItemTable itemTable = null;
				ItemTables.TryGetValue(item.Id, out itemTable);
				if(itemTable == null)
				{
					itemTable = new ItemTable();
					ItemTables.Add(item.Id, item);
				}
			}

			return true;
		}
		public bool AfterLoad()
		{
			return true;
		}

		public ItemTable GetItemTable(int itemId)
		{
			ItemTable _itemTable = null;
			ItemTables.TryGetValue(itemId, out _itemTable);
			return _itemTable;
		}
	}
}
