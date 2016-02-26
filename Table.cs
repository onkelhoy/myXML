using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace myXML
{
    class Table
    {
        private string name;
        private string path;
        XmlDocument main = new XmlDocument();
        XmlDocument structure = new XmlDocument();

        public Table(string path, string name)
        {
            try {
                this.path = path;
                this.name = name;
                main.Load(path + "/" + name + "/main.xml");
                //get file data check if 0.. then add to backup if backup isnt 0
                structure.Load(path + "/" + name + "/structure.xml");
            }
            catch (Exception e) {
                throw e;
            }
        }
        private XmlNode getStructure() { return structure.SelectSingleNode("root/structure"); }

        static public void Create(string path, string name)
        {
            try {
                string p = path + "/" + name;
                if (File.Exists(p + "/structure.cml"))
                {
                    throw new IOException("Table already exist");
                }
                Directory.CreateDirectory(p);
                XmlDocument st = new XmlDocument();
                XmlDocument m = new XmlDocument();

                st.AppendChild(st.CreateXmlDeclaration("1.0", "utf-8", null));
                m.AppendChild(m.CreateXmlDeclaration("1.0", "utf-8", null));

                XmlElement s = st.CreateElement("root");
                s.AppendChild(st.CreateElement("slots"));//store deleted autoindex value!
                s.AppendChild(st.CreateElement("structure"));
                s.SetAttribute("index", "0");
                st.AppendChild(s);

                m.AppendChild(m.CreateElement("root"));
                
                st.Save(p + "/structure.xml");
                m.Save(p + "/backup.xml");
                m.Save(p + "/main.xml");
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public string toString() { return name; }


        private void Save(int index)
        {
            switch (index)
            {
                case 0:
                    try
                    {
                        main.Save(path + "/" + name + "/main.xml");
                        main.Save(path + "/" + name + "/backup.xml");
                    } catch { }
                    break;
                case 1:
                    structure.Save(path + "/" + name + "/structure.xml");
                    break;
            }
        }
        public void saveAll()
        {
            main.Save(path + "/" + name + "/main.xml");
            main.Save(path + "/" + name + "/backup.xml");
            structure.Save(path + "/" + name + "/structure.xml");
        }
        public void dump() { }

        /*select method*/
        public XmlNodeList select(string command)
        {//title,name,id WHERE title like 'hej' and name = 'poop'

            string[] whereSplit = command.Split(new string[] { "where" }, StringSplitOptions.None);
            string[] selects = whereSplit[0].Split(new string[] { "," }, StringSplitOptions.None);
            string searchCommand = "//r[";//the main select
            string fullCommand = "";//to the selectives!
            string[] and = { };
            if (whereSplit.Length == 2)
            {
                and = whereSplit[1].Split(new string[] { "and" }, StringSplitOptions.None);
            
                for (int i = 0; i < and.Length; i++)
                {//will run atleast once even if no ands..
                    string[] or = and[i].Split(new string[] { "or" }, StringSplitOptions.None);
                    if (or.Length != 1)
                    {
                        for (int j = 0; j < or.Length; j++)
                        {
                            string val = selectOperation(or[j]);
                            if (j != or.Length - 1) { val += " or "; }
                            else if (i != and.Length - 1) { val += " and "; }
                            searchCommand += val;
                        }
                    }
                    else //if(and.Length != 1)
                    {
                        string val = selectOperation(and[i]);
                        if (i != and.Length - 1) { val += " and "; }
                        searchCommand += val;
                    }
                }
                searchCommand += "]";
            }
            else
            {
                searchCommand = "//r";
            }

            if (selects.Length != 1)
            {
                for (int i = 0; i < selects.Length; i++)
                {
                    if (selects[i] != "") { fullCommand += searchCommand + "/" + selects[i].Replace(" ", "") + ((i != selects.Length - 1) ? "|" : ""); }
                }
            }
            else
            {
                fullCommand = searchCommand;//get r so it can be chunks
            }
            //Console.WriteLine("full: "+fullCommand);
            //Console.WriteLine("cmd: "+searchCommand);
            XmlNodeList list = main.SelectNodes(fullCommand);
            return list;
        }//add /* at the end if select.lenght == 0
        private string selectOperation(string operation)
        {
            string newOperation = "";
            Console.WriteLine(operation);
            string[] like = operation.Split(new string[] { "like" }, StringSplitOptions.None);//speciell
            if(like.Length == 2)
            {
                newOperation = like[0] + "[contains(text()," + like[1] + ")]";
            }
            else
            {//could be title='hej', title>'hel', title div 'hej', title + 'hej'
                //check all the names that exist in structure
                XmlNodeList strucFields = structure.SelectNodes("//structure/*");
                //Console.WriteLine(strucFields.Count);
                foreach(XmlNode field in strucFields)
                {
                    try
                    {
                        string right = operation.Split(new string[] { field.Name }, 2, StringSplitOptions.None)[1];
                        newOperation = field.Name + "[text()" + right + "]";
                        break;
                    } catch { }//no need to throw anything.. 
                }
            }

            return newOperation;
        }

        /*update method*/
        public void update(string command)
        {

        }

        /*insert method*/
        public void addStructureRow(string[] data)
        {
            XmlElement row = structure.CreateElement(data[0]);
            //row.SetAttribute("name", data[0]);
            row.SetAttribute("type", data[1]);
            row.SetAttribute("length", data[2]);
            row.SetAttribute("start", data[3]);
            row.SetAttribute("unique", data[4]);
            row.SetAttribute("auto", data[5]);

            structure.SelectSingleNode("root/structure").AppendChild(row);
            Save(1);
        }
        public void insertRow(string[] fields, string[] values)
        {
            XmlNode s = getStructure();
            XmlNode collection = main.CreateElement("r");

            XmlNode root = structure.SelectSingleNode("root");
            string open = structure.SelectSingleNode("root/slots").InnerText;
            bool structChanged = false;//same index for entire book
            bool changeIndex = false;
            bool usedIndex = false;

            XmlElement row = main.CreateElement("r");

            #region index
            string addIndex = "";
            int index = -1;
            try
            {
                index = Convert.ToInt32(root.Attributes["index"].Value);//database should only have 0-100000000 indexes or what it now can be
                string[] v = { };
                if (open != "")
                {
                    v = open.Split(new char[] { ',' }, 2);
                }
                if (v.Length != 0)
                {
                    addIndex = v[0];
                    structure.SelectSingleNode("root/slots").InnerText = v[1];
                }
                else
                {
                    usedIndex = true;
                    addIndex = index.ToString();
                    root.Attributes["index"].Value = (index + 1).ToString();
                }
            }
            catch { throw new IndexOutOfRangeException("database has reached its index ammout"); }
            #endregion

            for (int i = 0; i < fields.Length; i++)
            {
                #region field loop
                XmlNode srow = s.SelectSingleNode(fields[i]);
                #region inserted field
                if (srow == null)
                {
                    throw new ArgumentException("structure of table <" + name + "> does not contain field - " + fields[i]);
                }
                if (srow.Attributes[0].Value == "number")
                {
                    try
                    {
                        Convert.ToDouble(values[i]);
                    }
                    catch { throw new ArgumentException("field is number type value is not [field,value] - [" + fields[i] + "," + values[i] + "]"); }
                }
                if (srow.Attributes[1].Value != "-1")
                {
                    int length = Convert.ToInt32(srow.Attributes[1].Value);
                    if (values[i].Length >= length) { throw new ArgumentException("value is too long [field,value] - [" + fields[i] + "," + values[i] + "]"); }
                }
                string val = ((values[i] != "") ? values[i] : srow.Attributes[2].Value);
                if (srow.Attributes[4].Value == "1")
                {//check what index to add
                    structChanged = true;
                    if (usedIndex) { changeIndex = true; }
                    val += addIndex;
                }
                if (srow.Attributes[3].Value == "1")
                {//check so no othe value exist!
                    if (select("* where " + fields[i] + "=" + val).Count > 0)
                    {
                        throw new ArgumentException("There already exist field with same value [field,value]: [" + fields[i] + "," + val + "]");
                    }
                }
                #endregion

                XmlElement rowVal = main.CreateElement(fields[i]);
                if (val != "")
                    rowVal.InnerText = val;
                row.AppendChild(rowVal);
                #endregion
            }

            for (int i = 0; i < s.ChildNodes.Count; i++)
            {
                #region structure loop
                if (row.SelectSingleNode("/" + s.ChildNodes[i].Name + "") == null)
                {
                    string val = "";
                    XmlElement rowVal = main.CreateElement(s.ChildNodes[i].Name);
                    if (s.ChildNodes[i].Attributes[4].Value == "1")
                    {
                        if (usedIndex)
                        {
                            structChanged = true;
                            changeIndex = true;
                        }
                        val += addIndex;
                    }
                    val += s.ChildNodes[i].Attributes[2].Value;
                    if (val != "")
                        rowVal.InnerText = val;
                    row.AppendChild(rowVal);
                }
                #endregion
            }
            main.DocumentElement.AppendChild(row);
            Save(0);
            if (structChanged)
            {
                if (changeIndex)
                {
                    structure.DocumentElement.Attributes["index"].Value = (index + 1).ToString();
                }
                Save(1);
            }
        }

        /*delete method*/
        public void delete(string command)
        {

        }
    }
}
