using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_fixing_areas
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
        {
            if (commandData.Application == null)
                return Result.Cancelled;

            
            TaskDialog dialog = new TaskDialog("Выбор элементов");
            dialog.MainInstruction = "Выберите, где искать элементы:";
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "На активном виде");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Во всем проекте");
            TaskDialogResult result = dialog.Show();
            if (result != TaskDialogResult.Cancel)
            {
                MainWindow window = new MainWindow(commandData, result);
                window.ShowDialog();
            }
         
            return Result.Succeeded;
        }
    }
}
