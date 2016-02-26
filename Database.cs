using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace myXML
{
    class Database
    {
        #region variables
        XmlDocument database = new XmlDocument();
        XmlNode databaseRoot;

        SortedList<string, Table> tables = new SortedList<string, Table>();
        string path = "";
        bool exit = false;
        #endregion
        public Database()
        {

        }
    }
}
