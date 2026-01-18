using MKlinkGUI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

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
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            // 设置默认路径
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // 筛选文件类型
            openFileDialog.Filter = "所有文件 (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                // 选中文件路径赋值文本框
                EditableFileAddressTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // 1. 替换为文件夹选择对话框（需添加 System.Windows.Forms 引用）
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                // 设置默认路径（和原逻辑一致）
                folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                // 设置对话框描述
                folderDialog.Description = "请选择目标文件夹";

                // 2. 打开文件夹选择对话框
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // 3. 将选中的文件夹路径赋值给文本框
                    EditableFileAddressTextBox.Text = folderDialog.SelectedPath;
                }
            }
        }
    }


}
