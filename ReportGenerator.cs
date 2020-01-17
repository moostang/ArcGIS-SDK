namespace ReportGenerator.Common
{
    class Generator
    {
        static string fcPipelinePath  = "temp_pipeline";
        static string routeID = "ROUTEID";


        /// <summary>
        /// Coordinate Priority 
        /// See reference at https://pro.arcgis.com/en/pro-app/tool-reference/linear-referencing/create-routes.htm
        /// </summary>
        public static IEnumerable<string> CoordinatePriority = new string[]
        {
            "UPPER_LEFT ",
            "LOWER_LEFT",
            "UPPER_RIGHT",
            "LOWER_RIGHT"
        };

        public static IEnumerable<string> SourceMapDatabase = new string[]
        {
            "Agricultural Database",
            "Surficial Geology",
            "Bedrock Geology",
            "Other"
        };

        /// <summary>
        /// Create Pipeline Route from input pipeline centerline
        /// </summary>
        /// <param name="fcPath"></param>
        /// <returns></returns>
        public static async Task<ArcGIS.Desktop.Core.Geoprocessing.IGPResult> CreatePipelineRoute(string fcPath)
        {

            // Check if input pipeline has "ROUTEID" field // Active Map must hvae fcPath
            var output = await QueuedTask.Run(() =>
            {
                var lyr = ArcGIS.Desktop.Mapping.MapView.Active.Map.FindLayers(fcPath).First() as ArcGIS.Desktop.Mapping.FeatureLayer;
                var gdb = lyr.GetTable().GetDatastore() as ArcGIS.Core.Data.Geodatabase;

                Uri path = gdb.GetPath();
                string fullPath = Path.Combine(path.LocalPath, lyr.GetTable().GetName().ToString());

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("GDB is: " + path);
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("path is: " + fullPath);

                fcPath = fullPath;

                Uri uri = new Uri(path.ToString());
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("database uri here: " + uri);                

                // Check if input pipline layer has a "ROUTEID" field
                using (ArcGIS.Core.Data.Geodatabase fileGeodatabase = new ArcGIS.Core.Data.Geodatabase(new ArcGIS.Core.Data.FileGeodatabaseConnectionPath(uri)))
                using (ArcGIS.Core.Data.FeatureClass featureClass = fileGeodatabase.OpenDataset<ArcGIS.Core.Data.FeatureClass>(lyr.Name))
                {
                    ArcGIS.Core.Data.FeatureClassDefinition lyrDefinition = featureClass.GetDefinition();
                    IReadOnlyList<ArcGIS.Core.Data.Field> fields = lyrDefinition.GetFields();
                    List<string> fieldsList = new List<string>();
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Checking fields");
                    foreach (var field in fields)
                    {
                        fieldsList.Add(field.Name);
                    }
                    
                    return fieldsList;
                }               

            });
        }
    }
}
