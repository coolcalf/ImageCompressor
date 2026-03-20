using System;
using System.Windows;
using Microsoft.Win32;

namespace ImageCompressor
{
    public partial class App : Application
    {
        private const int Net48ReleaseKey = 528040;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                if (!IsNetFramework48OrLaterInstalled())
                {
                    MessageBox.Show(
                        ".NET Framework 4.8 未安装或未启用。\n\n请先在这台 Windows 10 电脑上安装/启用 .NET Framework 4.8 后再运行本程序。",
                        "运行环境检查",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Shutdown();
                    return;
                }

                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "程序启动失败。\n\n" + ex,
                    "启动错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

        private static bool IsNetFramework48OrLaterInstalled()
        {
            const string subKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subKey))
            {
                var releaseValue = ndpKey != null ? ndpKey.GetValue("Release") as int? : null;
                return releaseValue.HasValue && releaseValue.Value >= Net48ReleaseKey;
            }
        }
    }
}
