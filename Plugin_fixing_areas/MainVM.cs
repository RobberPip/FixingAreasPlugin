using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Plugin_fixing_areas
{
    public class MainVM : INotifyPropertyChanged
    {
        ExternalCommandData commandData;
        public MainVM(ExternalCommandData _commandData, TaskDialogResult result)
        {
            commandData = _commandData;
            GetAllParameters(result);
        }
        private ObservableCollection<Parameter> _areas = new ObservableCollection<Parameter>();
        public ObservableCollection<Parameter> areas
        {
            get { return _areas; }
            set
            {
                _areas = value;
            }
        }
        private ObservableCollection<Parameter> _addParameters = new ObservableCollection<Parameter>();
        public ObservableCollection<Parameter> addParameters
        {
            get { return _addParameters; }
            set
            {
                _addParameters = value;
            }
        }
        private FilteredElementCollector collector;
        private Document doc;
        private ForgeTypeId typeArea = new ForgeTypeId();
        private Parameter flagRetrun { get; set; }
        private bool _returnIsEnable = false;
        public bool returnIsEnable
        {
            get { return _returnIsEnable; }
            set
            {
                _returnIsEnable = value;
                OnPropertyChanged(nameof(returnIsEnable));
            }
        }
        public ObservableCollection<Parameter> selectedValuesList = new ObservableCollection<Parameter>();
        private List<Parameter> listRetrun = new List<Parameter>();

        public void GetAllParameters(TaskDialogResult result)
        {
            doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            View activeView = uidoc.ActiveView;
            if (result == TaskDialogResult.CommandLink1)
            {
                collector = new FilteredElementCollector(doc, activeView.Id)
                    .WhereElementIsNotElementType()
                    .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_Rooms));
            }
            else if (result == TaskDialogResult.CommandLink2)
            {
                collector = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_Rooms));
            }
            else
            {

                return;
            }
            typeArea.TypeId = "autodesk.spec.aec:area-2.0.0";
            if (collector.GetElementCount() != 0)
            {
                foreach (Element element in collector)
                {
                    foreach (Parameter parameter in element.Parameters)
                    {
                        if (parameter.Definition.GetDataType() == typeArea && !areas.Any(item => item.Definition.Name == parameter.Definition.Name) && !parameter.Definition.Name.StartsWith("CONST"))
                        {
                            areas.Add(parameter);
                        }
                        flagRetrun = element.LookupParameter($"{prefix} " + parameter.Definition.Name);
                        if (flagRetrun != null && parameter.IsReadOnly == false)
                        {
                            returnIsEnable = true;
                            listRetrun.Add(parameter);
                        }
                    }
                }
            }
            return;
        }

        public void AddParameterListView(Parameter nameValue)
        {

            bool isValueExists = false;

            foreach (var listViewItem in addParameters)
            {

                if (listViewItem.Definition.Name == nameValue.Definition.Name)
                {
                    isValueExists = true;
                    break;
                }

            }
            if (isValueExists)
            {
                MessageBox.Show($"Значение \"{nameValue}\" уже существует в списке!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                addParameters.Add(nameValue);
            }
        }
        private string CreateExternalSharedParamFile(string sharedParameterFile)
        {
            string tempFolder = System.IO.Path.GetTempPath();
            string fullPath = System.IO.Path.Combine(tempFolder, sharedParameterFile);
            System.IO.FileStream fileStream = System.IO.File.Create(fullPath);

            fileStream.Close();
            return fullPath;
        }
        private void DeleteExternalSharedParamFile(string sharedParameterFile)
        {
            string tempFolder = System.IO.Path.GetTempPath();
            string fullPath = System.IO.Path.Combine(tempFolder, sharedParameterFile);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
            else
            {
                Console.WriteLine("Файл не найден: " + fullPath);
            }
        }
        private const string prefix = "CONST";
        public event EventHandler SaveRequest;
        private void RaiseSaveRequest()
        {
            SaveRequest?.Invoke(this, EventArgs.Empty);
        }
        public void CreateShareParamater(DefinitionFile file)
        {

            var bindingMap = doc.ParameterBindings;
            CategorySet myCategories = commandData.Application.Application.Create.NewCategorySet();

            Category myCategory = Category.GetCategory(commandData.Application.ActiveUIDocument.Document, BuiltInCategory.OST_Rooms);
            myCategories.Insert(myCategory);

            foreach (DefinitionGroup defGroup in file.Groups)
            {
                if (defGroup.Name == prefix)
                {
                    foreach (ExternalDefinition definition in defGroup.Definitions.Cast<ExternalDefinition>())
                    {
                        foreach (Parameter parameter in addParameters)
                        {
                            if (($"{prefix} " + parameter.Definition.Name) == definition.Name)
                            {
                                bindingMap.Insert(definition, new InstanceBinding(myCategories), BuiltInParameterGroup.PG_AREA);

                                var shar = new FilteredElementCollector(doc).OfClass(typeof(SharedParameterElement)).ToElements().Cast<SharedParameterElement>().Where(e => e.Name.Equals(definition.Name));
                                foreach (var item in shar)
                                {
                                    if (item.GetDefinition().Name == definition.Name)
                                    {
                                        item.GetDefinition().SetAllowVaryBetweenGroups(doc, true);
                                    }
                                }
                            }
                        }
                    }

                }

            }
        }

        public void CreateFop(Parameter parameter, string groupName, DefinitionFile file)
        {


            var difinitionName = $"{prefix} {parameter.Definition.Name}";
            var newGroup = file.Groups.get_Item(groupName);
            var sharedParameter = newGroup.Definitions.get_Item(difinitionName);
            if (sharedParameter == null)
            {
                var defenition = newGroup.Definitions.Create(new ExternalDefinitionCreationOptions(difinitionName, typeArea));
                var categorySet = commandData.Application.Application.Create.NewCategorySet();
                categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_Rooms));
                var binding = commandData.Application.Application.Create.NewInstanceBinding(categorySet);
                doc.ParameterBindings.Insert(defenition, binding);
            }

        }
        public void SaveParameters(bool? _fileCheck)
        {
            RaiseSaveRequest();
            using (Transaction transaction = new Transaction(doc, "Fixing values"))
            {
                transaction.Start();
                string fileName = "ФОП";
                string originalFile = "";
                var fileFlag = true;

                {

                    if (_fileCheck == false)
                    {
                        fileFlag = false;
                        fileName = CreateExternalSharedParamFile(fileName);
                        originalFile = doc.Application.SharedParametersFilename;
                        doc.Application.SharedParametersFilename = fileName;
                    }

                    DefinitionFile file = doc?.Application?.OpenSharedParameterFile();
                    if (file == null)
                    {

                        fileFlag = false;
                        fileName = CreateExternalSharedParamFile(fileName);
                        doc.Application.SharedParametersFilename = fileName;
                        file = doc.Application.OpenSharedParameterFile();

                    }
                    var groupName = prefix;

                    if (!file.Groups.Any(g => g.Name == groupName))
                    {
                        file.Groups.Create(groupName);
                    }
                    if (collector.GetElementCount() != 0)
                    {
                        foreach (Element element in collector)
                        {
                            foreach (Parameter parameter in addParameters)
                            {
                                if (!parameter.Definition.Name.StartsWith(prefix))
                                {
                                    Parameter param = element.LookupParameter($"{prefix} " + parameter.Definition.Name);

                                    if (param == null)
                                    {
                                        CreateFop(parameter, groupName, file);
                                    }
                                }
                            }
                            CreateShareParamater(file);
                            AddValue(element);


                        }
                    }
                    if (!fileFlag)
                    {
                        DeleteExternalSharedParamFile(fileName);
                        doc.Application.SharedParametersFilename = originalFile;
                    }

                }
                transaction.Commit();
            }

        }
        public void AddValue(Element element)
        {

            foreach (Parameter parameter in element.Parameters)
            {
                if (parameter.Definition.Name.StartsWith(prefix))
                {
                    Parameter target = element.LookupParameter(parameter.Definition.Name);
                    Parameter source = element.LookupParameter(parameter.Definition.Name.Replace(prefix, "").Trim());
                    if (target != null && source != null)
                    {
                        target.SetValueString(source.AsValueString());
                    }
                }
            }

        }
        public void RetrurnParameters()
        {
            using (Transaction transaction = new Transaction(doc, "Return parameters"))
            {
                transaction.Start();
                if (collector.GetElementCount() != 0)
                {
                    foreach (Element element in collector)
                    {
                        foreach (var item in listRetrun)
                        {
                            foreach (Parameter parameter in element.Parameters)
                            {
                                {
                                    if (parameter.Definition.Name == item.Definition.Name)
                                    {
                                        Parameter target = element.LookupParameter(parameter.Definition.Name);
                                        Parameter source = element.LookupParameter($"{prefix} " + parameter.Definition.Name);
                                        target.SetValueString(source.AsValueString());
                                    }

                                }
                            }
                        }
                    }
                }
                transaction.Commit();
            }

        }
        public void RemoveAttributes()
        {
            for (int i = selectedValuesList.Count - 1; i >= 0; i--)
            {
                var item = selectedValuesList[i];
                selectedValuesList.Remove(item);
                addParameters.Remove(item);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
