using MKlinkGUI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Wpf.Ui.Controls;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.Versioning;

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

            this.Loaded += (sender, e) =>
            {
                var sourceTextBox = this.FindName("SourceTextBox") as Wpf.Ui.Controls.TextBox;
                var targetTextBox = this.FindName("TargetTextBox") as Wpf.Ui.Controls.TextBox;

                if (sourceTextBox != null)
                {
                    sourceTextBox.PreviewKeyDown += TextBox_KeyDown;
                }

                if (targetTextBox != null)
                {
                    targetTextBox.PreviewKeyDown += TextBox_KeyDown;
                }

                // 检查管理员权限，如果是则隐藏"管理员运行"按钮
                var runAsAdminButton = this.FindName("RunAsAdminButton") as Wpf.Ui.Controls.Button;
                if (runAsAdminButton != null)
                {
                    if (IsAdministrator())
                    {
                        runAsAdminButton.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        runAsAdminButton.Visibility = Visibility.Visible;
                    }
                }
            };
        }

        #region Admin

        private void RunAsAdminButton_Click(object sender, RoutedEventArgs e)
        {
            RunAsAdministrator();
        }

        private void RunAsAdministrator()
        {
            try
            {
                // 检查管理员权限
                if (IsAdministrator())
                {
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "提示",
                        Content = new TextBlock()
                        {
                            Text = "应用程序已经在管理员模式下运行！",
                            TextWrapping = TextWrapping.Wrap
                        },
                        PrimaryButtonText = "确定"
                    };
                    _ = messageBox.ShowDialogAsync(true);
                    return;
                }

                // 获取应用程序路径
                var currentProcess = Process.GetCurrentProcess();
                var mainModule = currentProcess.MainModule;
                if (mainModule == null)
                {
                    throw new InvalidOperationException("无法获取程序路径");
                }
                string exePath = mainModule.FileName;

                // 创建新的进程启动信息
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas" // 请求管理员权限
                };

                // 启动新进程
                Process.Start(startInfo);

                // 关闭当前实例
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = new TextBlock()
                    {
                        Text = $"无法以管理员身份运行应用程序：{ex.Message}",
                        TextWrapping = TextWrapping.Wrap
                    },
                    PrimaryButtonText = "确定"
                };
                _ = messageBox.ShowDialogAsync(true);
            }
        }

        [SupportedOSPlatform("windows")]
        public static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        #endregion Admin

        #region Folder

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
            var textBox = sender as Wpf.Ui.Controls.TextBox;
            if (textBox == null) return;

            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // 获取第一个拖入的文件/文件夹路径
                string path = files[0];
                textBox.Text = path;
            }
        }

        // 处理文本框的键盘事件，支持粘贴功能
        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.V && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                var textBox = sender as Wpf.Ui.Controls.TextBox;
                if (textBox != null)
                {
                    // 处理粘贴操作
                    if (System.Windows.Clipboard.ContainsFileDropList())
                    {
                        // 获取剪贴板文件第一个文件/文件夹路径
                        var fileDropList = System.Windows.Clipboard.GetFileDropList();
                        if (fileDropList.Count > 0)
                        {
                            textBox.Text = fileDropList[0]; // 只取第一个文件/文件夹
                            e.Handled = true;
                        }
                    }
                    else if (System.Windows.Clipboard.ContainsText())
                    {
                        // 获取剪贴板文本
                        textBox.Text = System.Windows.Clipboard.GetText();
                        e.Handled = true;
                    }
                }
            }
        }

        #endregion Folder

        #region Run

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var sourceTextBox = this.FindName("SourceTextBox") as Wpf.Ui.Controls.TextBox;
            var targetTextBox = this.FindName("TargetTextBox") as Wpf.Ui.Controls.TextBox;

            string sourcePath = sourceTextBox?.Text?.Trim() ?? string.Empty;
            string targetPath = targetTextBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = MKlinkGUI.Resources.Localization.Lang.UniError,
                    Content = new TextBlock()
                    {
                        Text = "请填写完整的源路径和目标路径",
                        TextWrapping = TextWrapping.Wrap
                    },
                    SecondaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniClose,
                    IsCloseButtonEnabled = false
                };
                _ = await messageBox.ShowDialogAsync(true);
                return;
            }

            #region RunType

            // 获取选中的链接类型
            string linkType = GetSelectedLinkType();

            await ExecuteMklinkCommand(sourcePath, targetPath, linkType);
        }

        private string GetSelectedLinkType()
        {
            if (((System.Windows.Controls.RadioButton)this.FindName("SymbolicLinkRadio")).IsChecked == true)
            {
                return "/D"; // 符号链接
            }
            else if (((System.Windows.Controls.RadioButton)this.FindName("FolderLinkRadio")).IsChecked == true)
            {
                return "/J"; // 目录链接
            }
            else if (((System.Windows.Controls.RadioButton)this.FindName("HardLinkRadio")).IsChecked == true)
            {
                return "/H"; // 硬链接
            }
            return "/D"; // 默认符号链接
        }

        // 根据链接类型自动设置文件/文件夹模式
        private void UpdateFolderToggleState()
        {
            var folderToggle = FindName("FolderToggle") as ToggleSwitch;
            if (folderToggle == null) return;

            string linkType = GetSelectedLinkType();

            // 目录链接(/J)时，必须是文件夹模式
            if (linkType == "/J")
            {
                folderToggle.IsChecked = true;      // 设置为文件夹模式
                folderToggle.IsEnabled = false;     // 禁用切换
            }
            // 硬链接(/H)时，必须是文件模式
            else if (linkType == "/H")
            {
                folderToggle.IsChecked = false;     // 设置为文件模式
                folderToggle.IsEnabled = false;     // 禁用切换
            }
            // 符号链接(/D)时，允许用户手动切换
            else if (linkType == "/D")
            {
                folderToggle.IsEnabled = true;      // 启用切换
                // 保持当前用户选择的状态，不做改变
            }
        }

        #endregion RunType

        private void LinkTypeChanged(object sender, RoutedEventArgs e)
        {
            UpdateFolderToggleState();
        }

        #region RunTip

        private async Task ExecuteMklinkCommand(string sourcePath, string targetPath, string linkType)
        {
            try
            {
                // 根据链接类型判断源路径应该是文件还是文件夹
                bool sourceShouldBeFile = linkType == "/H"; // 硬链接 只能用于文件
                bool sourceShouldBeDir = linkType == "/J"; // 目录链接 只能用于文件夹
                bool sourceCanBeBoth = linkType == "/D"; // 符号链接 可用于文件或文件夹

                // 验证源路径是否存在及类型是否匹配
                bool sourceExists = false;
                if (sourceShouldBeFile) //硬链接
                {
                    sourceExists = File.Exists(sourcePath);
                }
                else if (sourceShouldBeDir) //目录链接
                {
                    sourceExists = Directory.Exists(sourcePath);
                }
                else if (sourceCanBeBoth) // 符号链接
                {
                    sourceExists = File.Exists(sourcePath) || Directory.Exists(sourcePath);
                }

                if (!sourceExists)
                {
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = MKlinkGUI.Resources.Localization.Lang.UniError,
                        Content = new TextBlock()
                        {
                            Text = $"源路径不存在或类型不匹配：{sourcePath}",
                            TextWrapping = TextWrapping.Wrap
                        },
                        SecondaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniClose,
                        IsCloseButtonEnabled = false
                    };
                    _ = await messageBox.ShowDialogAsync(true);
                    return;
                }

                // 目录链接，如果目标已存在，则在该目录内创建与源目录同名的子目录
                if (linkType == "/J" && Directory.Exists(targetPath))
                {
                    string sourceDirectoryName = Path.GetFileName(sourcePath);
                    targetPath = Path.Combine(targetPath, sourceDirectoryName);
                }

                // 如果目标已存在，询问是否覆盖
                if (File.Exists(targetPath) || Directory.Exists(targetPath))
                {
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = MKlinkGUI.Resources.Localization.Lang.UniDanger,
                        Content = new TextBlock()
                        {
                            Text = $"目标路径已存在：{targetPath}\n是否继续？",
                            TextWrapping = TextWrapping.Wrap
                        },
                        PrimaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniConfirm,
                        SecondaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniCancel,
                        IsCloseButtonEnabled = false
                    };
                    var result = await messageBox.ShowDialogAsync(true);

                    if (result == Wpf.Ui.Controls.MessageBoxResult.Secondary) // 如果取消，则返回
                        return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink {linkType} \"{targetPath}\" \"{sourcePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            var messageBox = new Wpf.Ui.Controls.MessageBox
                            {
                                Title = "成功",
                                Content = new TextBlock()
                                {
                                    Text = $"链接创建成功！\n{output}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                PrimaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniOk,
                                IsCloseButtonEnabled = false
                            };
                            _ = await messageBox.ShowDialogAsync(true);
                        }
                        else
                        {
                            var messageBox = new Wpf.Ui.Controls.MessageBox
                            {
                                Title = MKlinkGUI.Resources.Localization.Lang.UniError,
                                Content = new TextBlock()
                                {
                                    Text = $"链接创建失败！\n错误信息：{error}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                SecondaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniClose,
                                IsCloseButtonEnabled = false
                            };
                            _ = await messageBox.ShowDialogAsync(true);
                        }
                    }
                    else
                    {
                        var messageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = MKlinkGUI.Resources.Localization.Lang.UniError,
                            Content = new TextBlock()
                            {
                                Text = "无法启动进程来执行mklink命令",
                                TextWrapping = TextWrapping.Wrap
                            },
                            SecondaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniClose,
                            IsCloseButtonEnabled = false
                        };
                        _ = await messageBox.ShowDialogAsync(true);
                    }
                }
            }
            catch (Exception ex)
            {
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = MKlinkGUI.Resources.Localization.Lang.UniError,
                    Content = new TextBlock()
                    {
                        Text = $"执行mklink命令时发生异常：{ex.Message}",
                        TextWrapping = TextWrapping.Wrap
                    },
                    SecondaryButtonText = MKlinkGUI.Resources.Localization.Lang.UniClose,
                    IsCloseButtonEnabled = false
                };
                _ = await messageBox.ShowDialogAsync(true);
            }
        }

        #endregion RunTip

        #endregion Run


    }

}
