        private async void GetPolylineFeaturelayersOnlyAsync()
        {
            // Check if Active MapView exists
            if (MapView.Active == null)
            {
                return;
            }

            _activeLayers.Clear();
            
            // Get Feature 
            IEnumerable<Layer> layers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(layer => layer.ShapeType.Equals(esriGeometryType.esriGeometryPolyline));

            await QueuedTask.Run(() =>
            {
                foreach(FeatureLayer fLayer in layers)
                {
                    MessageBox.Show(fLayer.Name.ToString() + "is a polyline");                    
                }
            });
        }
