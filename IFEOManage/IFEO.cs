﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using IFEOGlobal;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace IFEOManage
{

    /// <summary>
    /// IFEOItem
    /// </summary>
    public class IFEOItem
    {

        /// <summary>
        /// Gets or sets the PE file's name.
        /// </summary>
        /// <value>
        /// The PE file's name.
        /// </value>
        public string PEName { get; set; } = "";

        /// <summary>
        /// Gets or sets the remark.
        /// </summary>
        /// <value>
        /// The remark.
        /// </value>
        public string Remark { get; set; } = "";


        /// <summary>
        /// Gets remark for display.
        /// </summary>
        /// <value>
        /// The remark.
        /// </value>
        public string DisplayRemark
        {
            get
            {
                string Extra = GetExtraRemarkString();
                if (Extra != "")
                {
                    Extra += "; ";
                }
                return Extra + Remark;
            }
        }


        /// <summary>
        /// Gets or sets the debugger.
        /// </summary>
        /// <value>
        /// The debugger.
        /// </value>
        public string Debugger { get; set; } = "";
        /// <summary>
        /// Gets or sets the RegKey.
        /// </summary>
        /// <value>
        /// The RegKey.
        /// </value>
        public RegistryKey RegKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [manage by this].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [manage by this]; otherwise, <c>false</c>.
        /// </value>
        public bool ManageByThis { get; set; } = true;

        /// <summary>
        /// Gets or sets the running method
        /// </summary>
        /// <value>
        /// The running method
        /// </value>
        public RunMethod RunMethod { get; set; } = RunMethod.Close;

        private string _IFEOPath;
        /// <summary>
        /// Gets the IFEOManage path.
        /// </summary>
        /// <value>
        /// The ifeo path.
        /// </value>
        public string IFEOPath
        {
            get
            {
                return _IFEOPath;
            }
            set
            {
                if (_IFEOPath == value) return;
                if (value == "")
                {
                    _IFEOPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                }
                else
                {
                    _IFEOPath = value;
                }
            }
        }

        private string GetExtraRemarkString()
        {
            List<string> Ret = new List<string>();
            if (!ManageByThis)
            {
                string DebuggerFile = "";
                if (Debugger.Contains("\""))
                {
                    DebuggerFile = DebuggerFile.Split('"')[1];
                }
                else if (Debugger.Contains(" "))
                {
                    DebuggerFile = Debugger.Split(' ')[0];
                }
                else
                {
                    DebuggerFile = Debugger;
                }
                if (!System.IO.File.Exists(DebuggerFile))
                {
                    Ret.Add((string)Application.Current.FindResource("cfmNotFound"));
                }
            }
            else
            {
                switch (RunMethod)
                {
                    case RunMethod.Close:
                        Ret.Add((string)Application.Current.FindResource("cfmExitDirectly"));
                        break;
                    case RunMethod.Popup:
                        Ret.Add((string)Application.Current.FindResource("cfmPopup"));
                        break;
                }
            }


            return String.Join("; ", Ret);
        }

        public override string ToString()
        {
            StringBuilder Builder = new StringBuilder();
            Builder.Append("PEName = " + PEName + ", ");
            Builder.Append("Remark = " + Remark + ", ");
            Builder.Append("Debugger = " + Debugger + ", ");
            Builder.Append("ManageByThis = " + ManageByThis + ", ");
            Builder.Append("RunMethod = " + RunMethod + ", ");
            Builder.Append("IFEOPath = " + IFEOPath + "");
            return Builder.ToString();
        }
    }

    /// <summary>
    /// IFEOInstance
    /// </summary>
    public class IFEOInstance
    {
        private static IFEOInstance instance = new IFEOInstance();
        private IFEOInstance()
        {
            Console.WriteLine("fuck");
        }
        public static IFEOInstance Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// The items
        /// </summary>
        public ObservableCollection<IFEOItem> Items = new ObservableCollection<IFEOItem>();


        private object GetValue(RegistryKey RegKey, string Key)
        {
            object Value = RegKey.GetValue(Key);
            return Value == null ? "" : Value.ToString();
        }

        private void _FormatDebuggerStringAndUpdate(IFEOItem Debugger)
        {
            if (Debugger.ManageByThis)
            {
                // Auto Update
                if (Debugger.Debugger != Global.IFEOExecution)
                {
                    Log.WriteLine("Refresh Debugger(" + Debugger.PEName + ") From (" + Debugger.Debugger + ") To (" + Global.IFEOExecution + ")");
                    Debugger.RegKey.SetValue("Debugger", Global.IFEOExecution);
                    Debugger.RegKey.SetValue("IFEOManage_Path", Global.IFEORunPath);
                }
                Debugger.ManageByThis = true;
                Debugger.Debugger = (string)Application.Current.FindResource("cfmManagedByThis");

            }
        }

        private RegistryKey IFEOKey = null;

        /// <summary>
        /// Loads IFEO items
        /// </summary>
        /// <returns>The state of load</returns>
        public bool Load()
        {
            Log.WriteLine("Load registry");
            IFEOKey = Registry.LocalMachine.OpenSubKey(Global.IFEORegPath, true);
            IFEOItem TempIFEO;
            if (IFEOKey != null)
            {
                Items.Clear();
                foreach (string keyName in IFEOKey.GetSubKeyNames())
                {
                    RegistryKey SubKey = IFEOKey.OpenSubKey(keyName, true);
                    if (SubKey != null)
                    {
                        if (SubKey.GetValueNames().ToList().ConvertAll(d => d.ToLower()).IndexOf("debugger") > -1)
                        {
                            TempIFEO = new IFEOItem();
                            TempIFEO.ManageByThis = ((string)GetValue(SubKey, "IFEOManage_Manage") == "True");
                            TempIFEO.RegKey = SubKey;
                            TempIFEO.IFEOPath = (string)GetValue(SubKey, "IFEOManage_Path");
                            TempIFEO.PEName = SubKey.Name.Split('\\').Last();
                            TempIFEO.Debugger = SubKey.GetValue("Debugger").ToString();
                            TempIFEO.Remark = (string)GetValue(SubKey, "IFEOManage_Remark");

                            string RunMethodString = (string)GetValue(SubKey, "IFEOManage_RunMethod");
                            if (RunMethodString != "")
                            {
                                TempIFEO.RunMethod = (RunMethod)Enum.Parse(TempIFEO.RunMethod.GetType(), RunMethodString);
                            }

                            _FormatDebuggerStringAndUpdate(TempIFEO);
                            Items.Add(TempIFEO);
                            Log.WriteLine("Load " + keyName + " successfully: " + TempIFEO.ToString());
                        }
                        else
                        {
                            //Log.WriteLine(keyName + " is not a vaild key.");
                        }

                    }
                    else
                    {
                        Log.WriteLine("Error when loading Sub Key(" + keyName + ")");
                        Log.MessageBoxError((string)Application.Current.FindResource("cfmOpenSubKeyError") + "\n" + keyName);
                    }
                }
            }
            else
            {
                Log.WriteLine("Cannot open registry");
                Log.MessageBoxError((string)Application.Current.FindResource("cfmCannotOpenRegistry"));
                return false;
            }
            return true;
        }



        /// <summary>
        /// Deletes IFEO items from both class and registry
        /// </summary>
        /// <param name="DeleteList">The deletion list.</param>
        /// <returns>The state of deletion</returns>
        public bool Delete(List<IFEOItem> DeleteList)
        {

            if (IFEOKey != null)
            {
                DeleteList.ForEach(delegate (IFEOItem Item)
                {
                    try
                    {
                        IFEOKey.DeleteSubKeyTree(Item.PEName);
                    }
                    catch (Exception Ex)
                    {
                        Log.WriteLine("Error when deleting item: " + Item.PEName + "\n\n" + Ex.ToString());
                        Log.MessageBoxError((string)Application.Current.FindResource("cfmDeleteError") + "\n" + Item.PEName + "\n\n" + Ex.ToString());
                    }
                });
            }
            Load();
            return true;

        }
        /// <summary>
        /// Deletes IFEO items from both class and registry
        /// </summary>
        /// <param name="DeleteIDList">The deletion identifier list.</param>
        /// <returns>The state of deletion</returns>
        public bool Delete(List<int> DeleteIDList)
        {
            if (IFEOKey != null)
            {
                DeleteIDList.ForEach(delegate (int Item)
                {
                    try
                    {
                        IFEOKey.DeleteSubKeyTree(Items[Item].PEName);
                    }
                    catch (Exception Ex)
                    {
                        Log.WriteLine("Error when deleting item: " + Items[Item].PEName + "\n\n" + Ex.ToString());
                        Log.MessageBoxError((string)Application.Current.FindResource("cfmDeleteError") + "\n" + Items[Item].PEName + "\n\n" + Ex.ToString());
                    }
                });
            }
            Load();
            return true;
        }

        /// <summary>
        /// Save IFEO Item to Registry
        /// </summary>
        /// <param name="Item">The item.</param>
        /// <returns>
        /// true
        /// </returns>
        public bool Save(IFEOItem Item)
        {

            Log.WriteLine("Start Saving Key");
            IFEOKey = Registry.LocalMachine.OpenSubKey(Global.IFEORegPath, true);
            try
            {
                Item.RegKey = IFEOKey.CreateSubKey(Item.PEName);
                Log.WriteLine("Create Sub Key(" + Item.PEName + ") Successfully.");
            }
            catch (Exception Ex)
            {
                Log.WriteLine("Error when creating Sub Key(" + Item.PEName + "): " + Ex.ToString());
                Log.MessageBoxError((string)Application.Current.FindResource("cfmOpenSubKeyError") + "\n" + Item.PEName + "\n\n" + Ex.ToString());
                return false;
            }

            try
            {
                if (Item.Debugger == "")
                {
                    Item.RegKey.DeleteValue("Debugger");
                }
                else
                {
                    Item.RegKey.SetValue("Debugger", Item.Debugger);
                }
            }

            catch (Exception Ex)
            {
                Log.WriteLine("Error when creating Debugger: " + Ex.ToString());
                Log.MessageBoxError((string)Application.Current.FindResource("cfmCreateDebuggerError") + "\n" + Item.PEName + "\n\n" + Ex.ToString());
                return false;
            }

            Item.RegKey.SetValue("IFEOManage_Path", Global.IFEORunPath);
            Item.RegKey.SetValue("IFEOManage_Remark", Item.Remark);
            Item.RegKey.SetValue("IFEOManage_Manage", Item.ManageByThis);
            Item.RegKey.SetValue("IFEOManage_RunMethod", Item.RunMethod);
            Item.RegKey.Close();
            Log.WriteLine("Saved key: " + Item.ToString());
            Load();
            return true;
        }

        /// <summary>
        /// Exports the specified items.
        /// </summary>
        /// <param name="Items">The items.</param>
        /// <param name="OutputFile">The output file.</param>
        public void Export(List<IFEOItem> Items, string OutputFile)
        {
            List<string> RegKey = new List<string>();
            RegKey.Add("Windows Registry Editor Version 5.00");
            Items.ForEach(delegate (IFEOItem Item)
            {
                string RegFileName = "Temp_" + Item.PEName + ".reg";
                ProcessStartInfo Start = new ProcessStartInfo();
                Start.FileName = "regedit.exe";
                Start.Arguments = "/e " + "\"" + RegFileName + "\" " + " \"HKEY_LOCAL_MACHINE\\" + Global.IFEORegPath + Item.PEName + "\"";
                Process RegProcess = Process.Start(Start);
                RegProcess.WaitForExit();

                RegKey.Add(File.ReadAllText(RegFileName).Replace("Windows Registry Editor Version 5.00", ""));
                File.Delete(RegFileName);
            });
            File.WriteAllText(OutputFile, string.Join(Environment.NewLine, RegKey));
        }
    }
}
