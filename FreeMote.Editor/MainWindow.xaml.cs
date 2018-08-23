using System.Windows.Input;
using FreeMote.Editor.Models;
using FreeMote.Plugins;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace FreeMote.Editor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private PsbJsonViewModel _model = new PsbJsonViewModel();
        public MainWindow()
        {
            _model.WindowDispatcher = Dispatcher;
            DataContext = _model;
            InitializeComponent();
            FreeMount.Init();
        }

        private void OpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title = "Select a PSB",
                Multiselect = false,
                Filter = "PSB Json(*.json)|*.json|PSB File(*.psb)|*.psb;*.mmo;|All Files|*.*",
            };

            if (dialog.ShowDialog() == true)
            {
                _model.Load(dialog.FileName);
            }
        }
    }
}
