        /// <summary>
        /// Example to create list of feature classes and tables
        /// </summary>
        private async void SearchSoilGDBPath()         ///////////// select gdb 
        {
            if (_executeQuery)
                return;

            ArcGIS.Desktop.Catalog.OpenItemDialog dlg = new ArcGIS.Desktop.Catalog.OpenItemDialog()
            {
                Title = "Select Soil Database",
                Filter = ArcGIS.Desktop.Catalog.ItemFilters.geodatabases,
                MultiSelect = false
            };

            bool? ok = dlg.ShowDialog();

            if(ok == true)
            {
                SoilGDBPath = dlg.Items.ToList().First().Path;
            }

            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Soil database is: " + SoilGDBPath +"\nIf not updated: " + _soilGDBPath);

            // Get a list of tables contained in SoilGDBPath
            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                using (ArcGIS.Core.Data.Geodatabase gdb = new ArcGIS.Core.Data.Geodatabase(
                    new ArcGIS.Core.Data.FileGeodatabaseConnectionPath(new Uri(SoilGDBPath, UriKind.Absolute))))
                {
                    IReadOnlyList<ArcGIS.Core.Data.Definition> fcList = gdb.GetDefinitions<ArcGIS.Core.Data.FeatureClassDefinition>();
                    IReadOnlyList<ArcGIS.Core.Data.Definition> tblList = gdb.GetDefinitions<ArcGIS.Core.Data.TableDefinition>();

                    lock (_theLock)
                    {
                        foreach(var fcDef in fcList)
                        {
                            _soilTables.Add(TableString(fcDef as ArcGIS.Core.Data.TableDefinition));
                        }

                        foreach (var fcDef in tblList)
                        {
                            _soilTables.Add(TableString(fcDef as ArcGIS.Core.Data.TableDefinition));
                        }
                    }

                }                
                
            });            
        }
