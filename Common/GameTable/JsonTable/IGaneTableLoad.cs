using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.GameTable.JsonTable
{
    public interface IGaneTableLoad
    {
        public bool LoadTable(string json);
        public bool AfterLoad();
    }
}
