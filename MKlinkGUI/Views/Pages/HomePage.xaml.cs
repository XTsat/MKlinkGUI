using MKlinkGUI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Wpf.Ui.Controls;

namespace MKlinkGUI.Views.Pages
{
    public partial class HomePage : INavigableView<HomeViewModel>
    {
        public HomeViewModel ViewModel { get; }

        public HomePage(HomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {

            var button = sender as Wpf.Ui.Controls.Button;
            System.Windows.Controls.TextBox? targetTextBox = null;

            if (button == SourceOpenButton)
            {
                targetTextBox = SourceTextBox;
            }
            else if (button == TargetOpenButton)
            {
                targetTextBox = TargetTextBox;
            }
            if (targetTextBox == null) return;
            // 判断切换按钮状态
            bool isFolderMode = (FindName("FolderToggle") as ToggleSwitch)?.IsChecked == true;

            if (isFolderMode)
            {
                // 文件夹选择逻辑
                using var folderDialog = new FolderBrowserDialog();
                {
                    folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);

                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        targetTextBox.Text = folderDialog.SelectedPath;
                    }
                }
            }
            else
            {
                // 文件选择逻辑
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);

                if (openFileDialog.ShowDialog() == true)
                {
                    targetTextBox.Text = openFileDialog.FileName;
                }
            }
        }

        // 拖动时显示允许放置的光标
        private void TextBox_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        // 获取文件/文件夹路径填入文本框
        private void TextBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // 只获取第一个拖入的文件/文件夹路径
                string path = files[0];
                textBox.Text = path;
            }
        }


    }

}
