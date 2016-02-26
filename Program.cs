using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace myXML
{
    class Program
    {
        #region variables
        static XmlDocument database = new XmlDocument();
        static XmlNode databaseRoot;

        static SortedList<string, Table> tables = new SortedList<string, Table>();
        static string path = "";
        static bool exit = false;
        #endregion

        static void Main(string[] args)
        {
            try
            {
                init();
            } catch (Exception e) { Console.WriteLine(e.Message); }
            listener();
        }

        #region core
        static void init()
        {
            Console.Write("Point to (folder) -> ");
            path = Console.ReadLine();
            if (!File.Exists(path + "/tables.xml"))
            {
                try
                {
                    Console.WriteLine("creating starting point..");
                    Directory.CreateDirectory(path);

                    database.AppendChild(database.CreateXmlDeclaration("1.0", "utf-8", null));
                    databaseRoot = database.CreateElement("tables");
                    database.AppendChild(databaseRoot);
                    database.Save(path + "/tables.xml");
                } catch { throw new EntryPointNotFoundException("this is not a valid location"); }
            }
            else
            {
                database.Load(path + "/tables.xml");
                databaseRoot = database.SelectSingleNode("tables");
                //try catch
                XmlNodeList nodelist = databaseRoot.SelectNodes("table");
                foreach (XmlNode node in nodelist)
                {
                    try {
                        tables.Add(node.InnerText, new Table(path, node.InnerText));
                    } catch (Exception e) { Console.WriteLine(e.Message); }
                }
            }
        }
        static void listener()
        {
            while (!exit)
            {//heart
                Console.Write("  #: ");
                string input = Console.ReadLine();
                if (!Commands(input))
                {
                    basicCommands(input);
                }
            }
        }
        #endregion

        #region rows
        static void select(string input)
        {//SELECT * FROM 'hej' WHERE...
            string[] fromSplit = input.Split(new string[] { "from" }, StringSplitOptions.None);
            if(fromSplit.Length != 2) { throw new ArgumentException("command do not follow the propper format - FROM"); }
            string[] whereSplit = fromSplit[1].Split(new string[] { "where" }, StringSplitOptions.None);
            //if(whereSplit.Length != 2) { throw new ArgumentException("command do not follow the propper format - WHERE"); }

            string name = whereSplit[0];
            name = name.Replace(" ", "");
            name = name.Replace("'", "");
            string selects = fromSplit[0];
            selects = selects.Replace("select", "");
            selects = selects.Replace(" ", "");//in case

            try
            {
                XmlNodeList list = tables[name].select(selects + ((whereSplit.Length > 1) ? "where" + whereSplit[1]: ""));
                if (list.Count == 0)
                {
                    throw new NullReferenceException("can't find any elements");
                }
                else
                {
                    Console.WriteLine();
                    string jsonText = "[\n";
                    for (int i = 0; i < list.Count; i++)
                    {
                        XmlNode node = list[i];
                        if (node.HasChildNodes)
                        {
                            jsonText += "\n\t[\n";
                            for(int j = 0; j < node.ChildNodes.Count; j++)
                            {
                                jsonText += "\t\t{\""+node.ChildNodes[j].LocalName + "\": \"" + node.ChildNodes[j].InnerText+ "\"}" + ((i != node.ChildNodes.Count - 1) ? ",\n" : "\n");
                            }
                            jsonText += "\t]\n";
                        }
                        else jsonText += "\t{" + node.Name + ": " + node.InnerText + "}\n";
                    }
                    jsonText += "]";
                    Console.WriteLine(jsonText);
                }
            } catch(Exception e) { throw e; }
        }
        static void update(string input)
        {//UPDATE `users` SET name='' WHERE 1

        }
        static void remove(string input)
        {
            Console.WriteLine("remove");
        }
        static void insert(string input)
        {
            #region checks
            //insert structure to 'table-name' (name, type, length, start, unique, auto) VALUES ('name of row'),('number,text,varchar'),('length of row'),('nothing/something'),('yes/no'),('yes/no')
            //insert into 'table-name' (row1, row2, row3..) VALUES (value1, value2, value3..)

            string[] arr = input.Split(' ');
            string tableName = "";
            string[] inputFields, inputValues;
            bool _structure = false;

            if (arr[1].Equals("into"))
            {//row
                tableName = arr[2].Replace("\'", "");
            }
            else if(arr[1].Equals("structure"))
            {//structure
                tableName = arr[3].Replace("\'", "");
                _structure = true;
            }
            try
            {
                tables[tableName].dump();
            }
            catch
            {
                throw new NullReferenceException("table not found");
            }
            #endregion
            inputFields = input.Split('(')[1].Split(')')[0].Replace(" ", "").Split(',');
            inputValues = input.Split('(')[2].Split(')')[0].Split(',');
            
            for(int i = 0; i < inputValues.Length; i++)
            {
                try
                {
                    inputValues[i] = inputValues[i].Split('\'')[1];
                } catch { throw new ArgumentException("Values need to be wrapped in 'value' (quotes)"); }
            }

            if(inputValues.Length != inputFields.Length)
            {
                throw new IndexOutOfRangeException("The values dont match the fields");
            }
            #region insert structure

            if (_structure)
            {//fields can only be name,type,length,start,unique,start

                string[] insertData = { "", "varchar", "-1", "", "0", "0" };
                #region loop
                for (int i = 0; i < inputFields.Length; i++)
                {
                    switch (inputFields[i])
                    {
                        case "name":
                            insertData[0] = inputValues[i];
                            break;
                        case "type":
                            if (inputValues[i] != "number" && inputValues[i] != "text" && inputValues[i] != "varchar")
                            {
                                throw new ArgumentException("only number,varchar,text allowed as type - " + inputValues[i]);
                            }
                            insertData[1] = inputValues[i];
                            break;
                        case "length":
                            try { Convert.ToDouble(inputValues[i]); }
                            catch { throw new ArgumentException("length can only be a number"); }
                            insertData[2] = inputValues[i];
                            break;
                        case "start":
                            insertData[3] = inputValues[i];
                            break;
                        case "unique":
                            if(inputValues[i] != "0" && inputValues[i] != "1")
                            {
                                throw new ArgumentException("unique can only be set to 1 or 0");
                            }
                            insertData[4] = inputValues[i];
                            break;
                        case "auto":
                            if (inputValues[i] != "0" && inputValues[i] != "1")
                            {
                                throw new ArgumentException("auto can only be set to 1 or 0");
                            }
                            insertData[5] = inputValues[i];
                            break;
                        case "space":
                            if (inputValues[i] != "0" && inputValues[i] != "1")
                            {
                                throw new ArgumentException("space can only be set to 1 or 0");
                            }
                            insertData[6] = inputValues[i];
                            break;
                    }
                }
                #endregion
                if (insertData[0].Equals(""))
                {
                    throw new ArgumentException("name-field must be defiened");
                }
                if (insertData[1].Equals("text"))
                {
                    insertData[2] = "-1";
                }
                try
                {
                    tables[tableName].addStructureRow(insertData);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            #endregion
            #region insert row
            else
            {
                //lets say structure -> id, name, username
                //data cant have other values then these
                //data also must have aleast one value (those who arent set goes by the start value and auto if so)
                if(inputFields.Length <= 0)
                {
                    throw new IndexOutOfRangeException("row must contain atleast one value");
                }
                try
                {
                    tables[tableName].insertRow(inputFields, inputValues);
                } catch(Exception e) { Console.WriteLine(e.Message); }
            }
            #endregion

        }
        #endregion

        #region table
        static void create(string input)
        {
            //create table 'hej'
            string[] data = input.Split(' ');
            if (data.Length == 3)
            {
                if (data[1].Equals("table"))
                {//later if it should be other (idk..)
                    string name = data[2].Replace("\'", "");
                    try
                    {
                        if (databaseRoot.SelectSingleNode(name) != null)
                        {//this should not be happend.. but in case
                            throw new Exception("This database already exist");
                        }
                        Table.Create(path, name);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    try
                    {

                        tables.Add(name, new Table(path, name));
                        XmlElement table = database.CreateElement("table");
                        table.InnerText = name;
                        databaseRoot.AppendChild(table);
                        database.Save(path + "/tables.xml");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error table could't be created!");
                        Console.Write("  # show to see error: ");
                        string ans = Console.ReadLine();
                        if (ans.ToLower().Equals("show"))
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Wrong input -> create type 'name'");
            }
        }
        static void drop(string input)
        {
            //drop table name
            string[] data = input.Split(' ');
            if (data.Length == 3)
            {
                if (data[1].Equals("table"))
                {//later if it should be other (idk..)
                    string name = data[2].Replace("\'", "");
                    try
                    {
                        XmlNode del = database.SelectSingleNode("//table[text()='" + name + "']");
                        databaseRoot.RemoveChild(del);
                        database.Save(path + "/tables.xml");
                        try
                        {
                            tables.Remove(name);
                        }
                        catch { }
                    }
                    catch
                    {
                        Console.WriteLine("No table named " + name + " was found in the database");
                    }
                    try
                    {
                        Directory.Delete(path + "/" + name, true);
                    }
                    catch
                    {
                        Console.WriteLine("No files belonging to " + name + " was found");
                    }
                }
            }
            else
            {
                Console.WriteLine("Wrong input -> drop type 'name'");
            }
        }

        #endregion

        #region commands
        static void basicCommands(string input)
        {
            if (input.Equals("help"))
            {//print out the commands
                Console.WriteLine(">>HELP");
                Console.WriteLine("\thelp-name\t- see specific help");
                Console.WriteLine("\tclear\t- clears the terminal");
                Console.WriteLine("\tlist\t- list the tables");
                Console.WriteLine("\tCREATE\t- creates table");
                Console.WriteLine("\tDROP\t- deletes table");

                Console.WriteLine("\tSELECT\t- selects row in table");
                Console.WriteLine("\tUPDATE\t- updates row/strucutre in table");
                Console.WriteLine("\tINSERT\t- insert  row/strucutre in table");
                Console.WriteLine("\tDELETE\t- deletes row/strucutre in table");
            }
            else if (input.Contains("help-"))
            {
                //save all first
                string type = input.Split('-')[1];
                switch (type.ToLower())
                {
                    case "create":
                        Console.WriteLine(">>help <CREATE>");
                        Console.WriteLine("\tcommand: CREATE table <table-name>");
                        break;
                    case "drop":
                        Console.WriteLine(">>help <DROP>");
                        Console.WriteLine("\tcommand: DROP table <table-name>");
                        break;
                    case "select":
                        Console.WriteLine(">>help <SELECT>");
                        Console.WriteLine("\tcommand: SELECT 'selectives' FROM <tableName> WHERE condition='value'");
                        break;
                    case "update":
                        break;
                    case "insert":
                        Console.WriteLine(">>help <INSERT>");
                        Console.WriteLine("\ninsert structure to 'table-name' (name, type, length, start, unique, auto, space) VALUES ('name of row'),('number,text,varchar'),('length of row'),('nothing/something'),(1/0),(1/0),(1/0)\n");
                        Console.WriteLine("insert into 'table-name' (row1, row2, row3..) VALUES (value1, value2, value3..)\n");
                        break;
                    case "delete":
                        break;
                }
            }
            else if (input.Equals("list"))
            {//list the tables
                foreach (XmlElement t in databaseRoot)
                {
                    Console.WriteLine("\t" + t.InnerText);
                }
            }
            else if (input.Equals("clear"))
            {//clear terminal
                Console.Clear();
            }
            else if (input.Equals("end"))
            {
                //save all first
                XmlNodeList tbls = databaseRoot.SelectNodes("tables/table");
                bool wait = false;
                foreach (XmlNode table in tbls)
                {
                    try
                    {
                        tables[table.InnerText].saveAll();
                    }
                    catch { Console.WriteLine("failed to save table - " + table.InnerText); wait = true; }
                }
                if (wait)
                {
                    Console.ReadLine();
                }
                exit = true;
            }
            else
            {
                Console.WriteLine("\t" + input + " is not a valid input - see help for information");
            }
        }
        static bool Commands(string input)
        {
            bool ok = true;
            input = input.ToLower();
            string val = input.Split(' ')[0].ToLower();
            if (val.Equals("create")) { try { create(input); } catch (Exception e) { Console.WriteLine(e.Message); } }
            else if (val.Equals("drop")) { drop(input); }
            //else if (val.Equals("adds")) { adds(input); }
            //else if (val.Equals("rems")) { rems(input); }

            else if (val.Equals("select")) { try { select(input.ToLower()); } catch (Exception e) { Console.WriteLine(e.Message); } }
            else if (val.Equals("update")) { update(input); }
            else if (val.Equals("remove")) { remove(input); }
            else if (val.Equals("insert")) { try { insert(input); } catch (Exception e) { Console.WriteLine(e.Message); } }
            else { ok = false; }
            return ok;
        }
        #endregion

        #region helpfunctions
        static bool searchArray(string[] arr, string search)
        {
            foreach(string s in arr)
            {
                if (s.Equals(search)) { return true; }
            }
            return false;
        }
        #endregion
    }
}
