        /// <summary>
        /// Update specific columns of the attribute table of feature classes
        /// </summary>
        /// <param name="gdbPath">Geodatabase Path</param>
        /// <param name="fcName">Name of Feature Class to update</param>
        /// <param name="searchFieldName">Column to search</param>
        /// <param name="searchKey">Search Key [short]</param>
        /// <param name="updateFieldName">Column to update</param>
        /// <param name="updateDictionary">Update Information</param>
        /// <returns></returns>
        private async Task UpdateRows(
            string gdbPath, string fcName, string searchFieldName, uint searchKey, string updateFieldName, Dictionary<uint, string> updateDictionary)
        {

            string message = string.Empty;
            EditOperation editOp = new EditOperation();

            await QueuedTask.Run(() =>
            {

                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbPath))))
                using (FeatureClass fcClass = gdb.OpenDataset<FeatureClass>(fcName))
                {
                    // Check if feature class is already open in Active Map // 
                    FeatureLayer lyr;
                    if (MapView.Active.Map.FindLayers(fcName).Count == 0)
                    {
                        lyr = LayerFactory.Instance.CreateFeatureLayer(fcClass, MapView.Active.Map, 0);
                    }
                    else
                    {
                        lyr = MapView.Active.Map.FindLayers(fcName).First() as FeatureLayer;
                    }
                    // Prepare Query Statement // 
                    var filter = new QueryFilter()
                    {
                        WhereClause = $"{searchFieldName}  = {searchKey}"
                    };
                    // Get list of object ID of all rows that meet the Query statement //
                    var oids = new List<long>();
                    using (var rc = lyr.Search(filter))
                    {
                        while (rc.MoveNext())
                        {
                            using (var record = rc.Current)
                            {
                                oids.Add(record.GetObjectID());
                            }
                        }
                    }

                    // Create edit Operation //
                    var modifyOp = new EditOperation()
                    {
                        Name = $"Update {updateFieldName}"
                    };

                    // Load features into inspector and update field //
                    var descInsp = new ArcGIS.Desktop.Editing.Attributes.Inspector();
                    descInsp.Load(lyr, oids);
                    descInsp[updateFieldName] = updateDictionary[searchKey];                    

                    // Modify and execute //
                    modifyOp.Modify(descInsp);
                    modifyOp.Execute();

                }
            });
            
        }


// AND DON'T FORGET TO SAVE EDITS

            // Save all edits // CALL THIS AFTER ALL EDITS ARE PERFORMED. 
            if (Project.Current.HasEdits)
                await Project.Current.SaveEditsAsync();
