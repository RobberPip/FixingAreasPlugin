using Autodesk.Revit.UI;
using System.Windows;
using Autodesk.Revit.DB;
using System;
using System.Windows.Controls;
using System.Reflection;

namespace Plugin_fixing_areas
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainVM mainVM;
        
        public MainWindow(ExternalCommandData commandData, TaskDialogResult result)
        {
            mainVM = new MainVM(commandData,result);
            DataContext = mainVM;
            var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var directory = System.IO.Path.GetDirectoryName(executingAssembly.Location);
            var dllPath = System.IO.Path.Combine(directory, "HandyControl.dll");
            Assembly.LoadFrom(dllPath);
            InitializeComponent();
            
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        { 
            ((MainVM)DataContext).AddParameterListView((Parameter)parameterComboBox.SelectedItem);
            ControlMarker();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            bool? _fileCheck = fileCheckBox.IsChecked;
            ((MainVM)DataContext).SaveParameters(_fileCheck);
            Close();
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            ((MainVM)DataContext).RetrurnParameters();
            Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ((MainVM)DataContext).RemoveAttributes();
            ControlMarker();
        }
        private void selectedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
                mainVM.selectedValuesList.Clear();
                foreach (Parameter selectedItem in parameterListView.SelectedItems)
                {
                    mainVM.selectedValuesList.Add(selectedItem);
                }
            ControlMarker();
        }
        private void ControlMarker()
        {
            delete.IsEnabled = parameterListView.SelectedItems.Count > 0? true: false;
            add.IsEnabled = parameterComboBox.SelectedItem != null ? true: false;
            save.IsEnabled = parameterListView.Items.Count != 0 ? true: false;
        }

        private void parameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ControlMarker();
        }
    }
}
