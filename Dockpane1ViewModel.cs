using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Windows.Forms;
using DockPaneTest.Common;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;


namespace DockPaneTest
{
    internal class Dockpane1ViewModel : DockPane, System.ComponentModel.INotifyPropertyChanged
    {   
        private const string _dockPaneID = "DockPaneTest_Dockpane1";
        private string _gdbPath = "";

        private ICommand _browseGDB = null;
        private ICommand _openGDB = null;
        private bool _executeQuery = false;

        protected Dockpane1ViewModel()
        {
            Initialize();  // Synchronization of Observable Collections
            GetFeatureClassCurrentGDB();
        }

        /// <summary>
        /// Synchronization of Observable Collections created on the UI thread at the begining of the program
        /// </summary>
        private void Initialize()
        {
            System.Windows.Data.BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;
        }
        
        /// <summary>
        /// List to store all feature classes and tables in the GDB 
        /// This ObservableCollection is created on UI thread,meaning that it can only 
        /// be modified on the UI thread and not from other threads (See "Thread Affinity")
        /// In order to update objects in the UI thread, we need to put the delegate on the 
        /// UI dispatcher or with WPF 4.5 use BindingOperations.EnableCollectionSynchronization
        /// Solution at: https://pragmaticdevs.wordpress.com/2015/08/25/modifying-observablecollection-from-worker-threads-in-wpf/
        /// Problem at: https://stackoverflow.com/questions/18331723/this-type-of-collectionview-does-not-support-changes-to-its-sourcecollection-fro
        /// </summary>
        private System.Collections.ObjectModel.ObservableCollection<string> _tables = new System.Collections.ObjectModel.ObservableCollection<string>();
        private static readonly object _theLock = new object();

        void BindingOperations_CollectionRegistering(object sender, System.Windows.Data.CollectionRegisteringEventArgs e)
        {
            // Register all the Collections
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_tables, _theLock);

            // Unregister - We only need this event once
            System.Windows.Data.BindingOperations.CollectionRegistering -= BindingOperations_CollectionRegistering;
        }

        /// <summary>
        /// Tables property binds the _tables <List> to the client
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<string> Tables
        {
            get
            {
                return _tables;
            }
        }

        public string GDBPath
        {
            get
            {
                return _gdbPath;
            }
            set
            {
                _gdbPath = value;
                OnPropertyChanged();  // Informs the client of change in GDBPath 
            }
        }

        #region Progress
        public bool IAmBusy
        {
            get
            {
                return _executeQuery;
            }
        }

        #endregion Progress


        #region Commands

        /// <summary>
        /// Update the list of feature class on the ComboTextBox
        /// </summary>
        public ICommand UpdateListCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    GetFeatureClassCurrentGDB();
                }, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICommand OpenSearchFolder
        {
            get
            {
                return new RelayCommand(() =>
                {
                    YouHavePressedAButton();                    
                }, true);
            }
        }

        public ICommand BrowseForGDBCommand
        {
            get
            {
                if (_browseGDB == null)
                    _browseGDB = new RelayCommand(BrowseForGDB);
                    return _browseGDB;
            }
        }

        public ICommand OpenGDBCommand
        {
            get
            {
                if (_openGDB == null)
                    _openGDB = new RelayCommand(OpenGDB);
                return _openGDB;
            }
        }

        private void YouHavePressedAButton()
        {
            _tables.Clear();
            OnPropertyChanged();
            if (_executeQuery)
            {
                _executeQuery = false;
            }
            else
            {
                _executeQuery = true;
            }            
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("You pressed a button: " + _executeQuery.ToString());
        }

        private void BrowseForGDB()
        {
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Status: " + _executeQuery.ToString());
            if (_executeQuery) // If something is going on 
                return;
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "Please select a Geodatabase (.gdb)";                                
                dlg.SelectedPath = GDBPath;
                dlg.ShowNewFolderButton = false;
                System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                if(result == System.Windows.Forms.DialogResult.OK)
                {
                    if (dlg.SelectedPath.Contains("gdb")) 
                    {
                        GDBPath = dlg.SelectedPath;                        
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please select a GDB");
                        return;
                    }                    
                }

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Selected Path: " + GDBPath.ToString());
            }
        }


        private void OpenGDB()
        {
            if (_executeQuery)
                return;
            if (_gdbPath == "")
                return;
            _executeQuery = true;

            System.Windows.Data.BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

            ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                using (ArcGIS.Core.Data.Geodatabase gdb = new ArcGIS.Core.Data.Geodatabase(
                    new ArcGIS.Core.Data.FileGeodatabaseConnectionPath(new Uri(_gdbPath, UriKind.Absolute))))
                {
                    IReadOnlyList<ArcGIS.Core.Data.Definition> fcList = gdb.GetDefinitions<ArcGIS.Core.Data.FeatureClassDefinition>();
                    IReadOnlyList<ArcGIS.Core.Data.Definition> tables = gdb.GetDefinitions<ArcGIS.Core.Data.TableDefinition>();

                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Total number of feature classes: " + fcList.Count.ToString() +
                    "\nFirst Feature is: " + FeatureString(fcList[0] as ArcGIS.Core.Data.FeatureClassDefinition));

                    lock (_theLock)
                    {
                        // Add all features to _tables list
                        foreach (var fcDef in fcList)
                        {
                            _tables.Add(TableString(fcDef as ArcGIS.Core.Data.TableDefinition)); // Feature class table is read as TableDefinition and NOT FeatureClassDefinition
                        }
                        // Add all tables to _tables list
                        foreach (var tableDef in tables)
                        {
                            _tables.Add(TableString(tableDef as ArcGIS.Core.Data.TableDefinition));
                            // ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("TableString: " + (TableString(tableDef as ArcGIS.Core.Data.TableDefinition)));
                        }
                    }
                }                
            });
        }

        private void GetFeatureClassCurrentGDB()
        {
            // Check if active MapView exists
            if(ArcGIS.Desktop.Mapping.MapView.Active.Map == null)
            {
                return;
            }

            _tables.Clear();

            var aMap = ArcGIS.Desktop.Mapping.MapView.Active.Map;

            List< ArcGIS.Desktop.Mapping.FeatureLayer> fcList = aMap.GetLayersAsFlattenedList().OfType<ArcGIS.Desktop.Mapping.FeatureLayer>().ToList();

            foreach(var item in fcList)
            {
                _tables.Add(item.ToString());
            }
        }


        private string FeatureString(ArcGIS.Core.Data.FeatureClassDefinition fcDef)
        {
            string alias = fcDef.GetAliasName();
            return string.Format("{0} ({1})", !string.IsNullOrEmpty(alias) ? alias : fcDef.GetName(), fcDef.GetName());
        }
        private string TableString(ArcGIS.Core.Data.TableDefinition table)
        {
            string alias = table.GetAliasName();
            return string.Format("{0} ({1})", alias == null ? alias : table.GetName(), table.GetName()); 
        }


        #endregion Commands

        #region Delegate Checking Property Changes   
        /// <summary>
        /// NOTE: Windows.Forms also has PropertyChangedEventHandler and PropertyChangedEventArgs. Thus, we have 
        /// avoid such confusion and point towards the appropriate class. 
        /// Notes: https://stackoverflow.com/questions/1725554/wpf-simple-textbox-data-binding
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = delegate { };

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propName = "")
        {
            PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propName));
        }
        #endregion Delegate Checking Property Changes




        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();            
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Generate Soil Report";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class Dockpane1_ShowButton : Button
    {
        protected override void OnClick()
        {
            Dockpane1ViewModel.Show();            
        }
    }
}
