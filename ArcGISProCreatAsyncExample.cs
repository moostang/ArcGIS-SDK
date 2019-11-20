using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;


/* CREATE NEW PROJECT WITH SPECIFIED NAME
 * ======================================
 * This will create a new Project named "NewProject.aprx" with and 
 * path "C:\GIS\DATA\NewProject\NewProject.aprx"
 */
CreateProjectSettings projectSettings  = new CreateProjectSettings()
{
    Name = "NewProject",
    LocationPath = @"C:\GIS\Data",                
};

var project = await Project.CreateAsync(projectSettings);
MessageBox.Show("New project has been created at " + project.Path);
