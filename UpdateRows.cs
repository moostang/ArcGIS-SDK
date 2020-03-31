/// <summary>
        /// Update specific columns of the attribute table of feature classes. 
        /// </summary>
        /// <param name="selectedKey">Field Value to query</param>
        /// <param name="gdbPath">Path to the root GDB of the feature class. Even if fc is inside Dataset, give path to GDB </param>
        /// <param name="fc">Name of feature class</param>
        /// <returns></returns>
        private async Task<bool> UpdateRows(string gdbPath, string fc, uint queryValue)
        {   
            EditOperation editOp = new EditOperation();

            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbPath))))
                using (FeatureClass fcClass = gdb.OpenDataset<FeatureClass>(fc))
                {
                    FeatureLayer lyr; 
                    string fcName = fcClass.GetName();                    
                    
                    // Check if fc is already open in activeMap. Otherwise, if you call 
                    // calling this module for the same feature class several times, then it will 
                    // open up several layers of the same feature class
                    if (MapView.Active.Map.FindLayers(fcName).Count == 0)
                    {
                        lyr = LayerFactory.Instance.CreateFeatureLayer(fcClass, MapView.Active.Map, 0);
                    }
                    else
                    {
                        lyr = MapView.Active.Map.FindLayers(fcName).First() as FeatureLayer;
                    }
                    
                    // Create Query //
                    var filter = new QueryFilter()
                    {
                        WhereClause = $"{queryField}  = {queryValue}"
                    };                   

                    var ObjIDs = new List<long>();
                    using (var rc = lyr.Search(filter))
                    {
                        while (rc.MoveNext())
                        {
                            using (var record = rc.Current)
                            {
                                ObjIDs.Add(record.GetObjectID());
                            }
                        }
                    }

                    // Create edit Operation //
                    var modifyOp = new EditOperation()
                    {
                        Name = "Update Description"
                    };

                    // Load features into inspector and update field //
                    var descInsp = new ArcGIS.Desktop.Editing.Attributes.Inspector();                    
                    descInsp.Load(lyr, ObjIDs);
                    descInsp["DESCRIPTION"] = DictDESCRIPTION_POLYID[selectedKey];

                    // Modify and execute //
                    modifyOp.Modify(descInsp);
                    modifyOp.Execute();

                }
            });

            return true;
        }
        
