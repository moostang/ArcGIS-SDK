namespace MyNamespace
{   
    /// <summary>
    /// Dockpane that shows list of layers in active Map
    /// </summary>
    internal class MyDockPaneViewModel : DockPane, System.ComponentModel.INotifyPropertyChanged
    {
        private ArcGIS.Core.Events.SubscriptionToken _eventToken = null;
        private ArcGIS.Core.Events.SubscriptionToken _eventToken2 = null;
        
        private System.Collections.ObjectModel.ObservableCollection<string> _tables = new System.Collections.ObjectModel.ObservableCollection<string>();
        private static readonly object _theLock = new object();        

        protected void Initialize()
        {
            System.Windows.Data.BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;            
        }

        private void OnMapViewIntialized(ArcGIS.Desktop.Mapping.Events.MapViewEventArgs args)
        {
            if (_eventToken != null)
            {
                ArcGIS.Desktop.Mapping.Events.MapViewInitializedEvent.Unsubscribe(_eventToken);
                _eventToken = null;
            }
            GetActiveMapLayerGDBPathAsync();
        }

        private void OnActiveMapViewChanged(ArcGIS.Desktop.Mapping.Events.ActiveMapViewChangedEventArgs args)
        {
            GetActiveMapLayerGDBPathAsync();
            return;
        }
        
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

        private async void GetActiveMapLayerGDBPathAsync()
        {
            // Check if active MapView exists
            if (ArcGIS.Desktop.Mapping.MapView.Active == null)
            {
                return;
            }

            _tables.Clear();

            IEnumerable<ArcGIS.Desktop.Mapping.Layer> layers = ArcGIS.Desktop.Mapping.MapView.Active.Map.Layers.Where(layer => layer is ArcGIS.Desktop.Mapping.FeatureLayer);

            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                foreach (ArcGIS.Desktop.Mapping.FeatureLayer featureLayer in layers)
                {
                    try  // TRY/CATCH TO CHECK FOR LAYERS WITH LOST CONNECTIONS (DELETED/CHANGED SOURCES) //
                    {
                        using (ArcGIS.Core.Data.Table table = featureLayer.GetTable())
                        using (ArcGIS.Core.Data.Datastore datastore = table.GetDatastore())
                        {
                            if (datastore is ArcGIS.Core.Data.UnknownDatastore)
                            {
                                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Unknow datastore for: " + featureLayer.Name);
                            }
                            ArcGIS.Core.Data.Geodatabase gdb = datastore as ArcGIS.Core.Data.Geodatabase;
                            _tables.Add(gdb.GetPath().ToString() + "/" + featureLayer.Name.ToString());
                        }
                    }
                    catch
                    {
                        return;
                    }
                }                
            }); 
        }
        
    internal class MyDockPane_ShowButton : Button
    {
        protected override void OnClick()
        {
            MyDockPaneViewModel.Show();            
        }
    }
}        
