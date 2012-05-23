using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Parch {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public GameArchive archive;
        public List<FileRecord> fileList;
        String filterstring = "";
        List<GameArchive> ArchivePlugins;
        Boolean preservePaths = true;
        public MainWindow() {
            InitializeComponent();
            Window.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            String[] filePaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll");
            ArchivePlugins = new List<GameArchive>();
            GameArchive tmpplugin;
            String alltypes = "All Supported|";
            foreach (String filename in filePaths) {
                try {
                    tmpplugin = loadPlugin(filename);
                    if (tmpplugin.addFileType()) {
                        alltypes += tmpplugin.getFileExtensions() + ";";
                        filterstring += "|" + tmpplugin.getFileType() + "|" + tmpplugin.getFileExtensions();
                    }
                    ArchivePlugins.Add(tmpplugin);
                }
                catch (Exception b) {
#if DEBUG
                    MessageBox.Show(b.Message);
#endif
                }
            }
            filterstring = alltypes + filterstring + "|All Files|*.*";
        }

        private void Open_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = filterstring;
            dlg.InitialDirectory = Properties.Settings.Default.lastOpenPath;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true) {
                if (archive != null) {
                    archive.close();
                    archive = null;
                    dataGrid1.ItemsSource = new List<FileRecord>();
                    Window.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                }
                FileStream OpenFile = File.Open(dlg.FileName, FileMode.Open);

                foreach (GameArchive plugin in ArchivePlugins)
                    if (plugin.LoadFile(OpenFile)) {
                        archive = plugin;
                        break;
                    }

                if (archive != null) {
                    fileList = new List<FileRecord>();
                    for (int i = 0; i < archive.numFiles; i++)
                        fileList.Add(new FileRecord(i, archive.getFileName(i), archive.getFileSize(i)));
                    dataGrid1.ItemsSource = fileList;
                    Properties.Settings.Default.lastOpenPath = dlg.FileName.Substring(0, dlg.FileName.LastIndexOf('\\'));
                    Properties.Settings.Default.Save();
                    Window.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " - " + dlg.FileName.Substring(dlg.FileName.LastIndexOf('\\') + 1);
                }
                else {
                    MessageBox.Show("Unrecognized File Type", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Extract_Click(object sender, RoutedEventArgs e) {
            if (dataGrid1.SelectedItems.Count == 1) {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = "";
                dlg.FileName = ((FileRecord)dataGrid1.SelectedItem).Name;
                Nullable<bool> result = dlg.ShowDialog();

                if (result == true) {
                    BinaryWriter outputFile = new BinaryWriter(File.Open(dlg.FileName, FileMode.CreateNew));
                    outputFile.Write(archive.getFile(((FileRecord)dataGrid1.SelectedItem).ID));
                    outputFile.Close();
                }
            }
            else {
                Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                dialog.Description = "Please select a folder.";
                dialog.UseDescriptionForTitle = true;
                Nullable<bool> result = dialog.ShowDialog();
                if (result == true) {
                    BinaryWriter writer;
                    string[] tempPath;
                    string tempName;
                    foreach (FileRecord rec in dataGrid1.SelectedItems) {
                        tempName = rec.Name;
                        if (preservePaths) {
                            tempPath = rec.Name.Split('\\');
                            Directory.CreateDirectory(dialog.SelectedPath + "\\" + String.Join("\\", tempPath, 0, tempPath.Length - 1));
                        }
                        else {
                            tempName = tempName.Substring(tempName.LastIndexOf("\\"));
                        }
                        writer = new BinaryWriter(File.Open(dialog.SelectedPath + "\\" + tempName, FileMode.CreateNew));
                        writer.Write(archive.getFile(rec.ID));
                        writer.Close();
                    }
                }
            }
        }
        public static GameArchive loadPlugin(String path) {
            Assembly a = Assembly.LoadFile(path);
            Type pluginType = null;

            foreach (Type type in a.GetTypes())
                if (type.GetInterface("GameArchive") != null)
                    pluginType = type;

            if (pluginType == null)
                throw new Exception("GameArchive not found!");


            GameArchive plugin = (GameArchive)Activator.CreateInstance(pluginType);
            return plugin;
        }
        private void dataGrid1_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            if ((archive == null) || (archive.numFiles < 0))
                e.Handled = true;
        }

        private void toolBar1_Loaded(object sender, RoutedEventArgs e) {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null) {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e) {
            preservePaths = ((MenuItem)sender).IsChecked;
        }
    }
}
