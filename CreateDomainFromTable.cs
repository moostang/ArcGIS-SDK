        /// <summary>
        /// Create Domain table with 'Code' and 'Description' from a list of descriptions. 
        /// </summary>
        /// <param name="currentGDB">Geodatabase path</param>
        /// <param name="domainTable">Name of Domain Table</param>
        /// <param name="domainDescriptionList">List of Description (string) to be coded</param>
        /// <param name="descriptionLength">Length of Description</param>
        public static async Task CreateDomainFromTable(string currentGDB, string domainTable, List<string> domainDescriptionList, short descriptionLength)
        {
            #region Create new domain from list of Component ID ["CMP_ID"]
            // Create new empty domain table //                        
            var argsCreateTable = gp.MakeValueArray(currentGDB, domainTable);
            await gp.ExecuteToolAsync("management.CreateTable", argsCreateTable);

            // Add Fields //
            string statement = $"Code SHORT # # # #;Description Text # {descriptionLength} # # ";
            var argsAddFields = gp.MakeValueArray(domainTable, statement);
            await gp.ExecuteToolAsync("management.AddFields", argsAddFields);

            #region Populate Domain Table
            string message = string.Empty;
            bool creationResult = false;
            EditOperation editOp = new EditOperation();

            await QueuedTask.Run(() =>
            {
                // Query table // 
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(currentGDB))))
                using (Table table = gdb.OpenDataset<Table>(domainTable))
                {
                    #region Add Standalone Table
                    // Add queried table to Active Map's Table of Contents // 
                    if (MapView.Active != null)
                    {
                        // Check to see if queried table exist in Table of Contents //
                        IReadOnlyList<StandaloneTable> tables = MapView.Active.Map.FindStandaloneTables(domainTable);
                        if (tables.Count() == 0)
                        {
                            // Add Table to Table of Contents //
                            IStandaloneTableFactory tableFactory = StandaloneTableFactory.Instance;
                            tableFactory.CreateStandaloneTable(table, MapView.Active.Map);
                        }
                    }

                    #endregion Add Standalone Table

                    editOp.Callback(context =>
                    {
                        for (int x = 0; x < domainDescriptionList.Count(); x++)
                        {
                            TableDefinition tableDefinition = table.GetDefinition();
                            int codeIndex = tableDefinition.FindField("Code");

                            using (RowBuffer rb = table.CreateRowBuffer())
                            {
                                rb[codeIndex] = Convert.ToUInt16(x);
                                rb["Description"] = Convert.ToInt32(domainDescriptionList[x]);

                                using (Row row = table.CreateRow(rb))
                                {
                                    context.Invalidate(row);
                                }
                            }
                        }
                    }, table);

                    try
                    {
                        creationResult = editOp.Execute();
                        if (!creationResult) message = editOp.ErrorMessage;
                    }
                    catch (GeodatabaseException exObj)
                    {
                        message = exObj.Message;
                    }

                }
            });

            if (!string.IsNullOrEmpty(message))
                MessageBox.Show(message);

            // Save all edits // Always save before add domain to tables. 
            if (Project.Current.HasEdits)
                await Project.Current.SaveEditsAsync();

            #endregion Populate Domain Table

            // Table to Domain //
            string domainName = domainTable;
            var argsTableToDomain = gp.MakeValueArray(domainTable, "Code", "Description", currentGDB, domainName, "Description", "REPLACE");
            var outputTableToDomain = await gp.ExecuteToolAsync("management.TableToDomain", argsTableToDomain);
            #endregion  Create new domain from list of Component ID ["CMP_ID"]
        }
