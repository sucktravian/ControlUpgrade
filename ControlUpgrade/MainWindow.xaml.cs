/*
 * This file is part of ControlUpgrade.
 *
 * ControlUpgrade is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ControlUpgrade is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ControlUpgrade. If not, see <https://www.gnu.org/licenses/>.
 *
 * Copyright (C) 2025 Sucktravian
 */

using System;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;


namespace ControlUpgrade
{
    public partial class MainWindow : Window
    {
        private const string RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

        public MainWindow()
        {
            InitializeComponent();
            themeToggleButton.Content = "☀️"; // Light mode by default
            isDarkMode = false;
            ShowRegistryInfo();

            if (!IsRunningAsAdmin())
            {
                MessageBox.Show("This application must be run as Administrator.", "Insufficient Privileges", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }


            Title = Properties.Resources.Title;
            lockButton.Content = Properties.Resources.LockButton;
            unlockButton.Content = Properties.Resources.UnlockButton;
            upgradeGroupBox.Header =Properties.Resources.UpgradeGroupHeader;
            registryTab.Header = Properties.Resources.RegistryTabHeader;
            registryKeyLabel.Text = Properties.Resources.RegistryKeyLabel;
            openRegistryEditorButton.Content = Properties.Resources.OpenRegistryEditorButton;


            statusLabel.Text = IsUpgradeLocked
                ? Properties.Resources.StatusLocked
                : Properties.Resources.StatusUnlocked;



            LoadTheme("Light");
            DisplayCurrentWindowsVersion();
            UpdateStatusLabel();
        }

        private bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void LoadTheme(string theme)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}Theme.xaml", UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        private bool isDarkMode = false;

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            LoadTheme(isDarkMode ? "Dark" : "Light");
            themeToggleButton.Content = isDarkMode ? "🌙" : "☀️";
        }

        private void OpenRegistryEditor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string keyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit",
                    "LastKey",
                    keyPath);

                System.Diagnostics.Process.Start("regedit.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open Registry Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void LockUpgrade_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentVersion = GetCurrentWindowsVersion();
                if (string.IsNullOrEmpty(currentVersion))
                {
                    MessageBox.Show("Unable to determine the current Windows version.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        key.SetValue("ProductVersion", "Windows 11", RegistryValueKind.String);
                        key.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                        key.SetValue("TargetReleaseVersionInfo", currentVersion, RegistryValueKind.String);
                    }
                }

                MessageBox.Show($"Upgrade locked to {currentVersion}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowRegistryInfo();
                UpdateStatusLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error locking upgrade: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UnlockUpgrade_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("ProductVersion", false);
                        key.DeleteValue("TargetReleaseVersion", false);
                        key.DeleteValue("TargetReleaseVersionInfo", false);
                    }
                }

                MessageBox.Show("Upgrade unlocked.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowRegistryInfo();
                UpdateStatusLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error unlocking upgrade: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetCurrentWindowsVersion()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    object? displayVersion = key.GetValue("DisplayVersion");
                    if (displayVersion != null)
                    {
                        return displayVersion.ToString()!; // Use null-forgiving operator to suppress CS8600  
                    }
                }
            }
            catch
            {
                // Handle exceptions if necessary  
            }

            return string.Empty; // Return a non-null default value to avoid nullability issues  
        }
        private void DisplayCurrentWindowsVersion()
        {
            string version = GetCurrentWindowsVersion();
            versionTextBlock.Text = $"Windows Version: {version ?? "Unknown"}";
        }

        private void ShowRegistryInfo()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                if (key == null)
                {
                    registryInfoTextBox.Text = Properties.Resources.RegistryKeyNotFound;
                    return;
                }

                var sb = new System.Text.StringBuilder();
                foreach (var valueName in key.GetValueNames())
                {
                    object? val = key.GetValue(valueName);
                    sb.AppendLine($"{valueName} = {val}");
                }

                if (sb.Length == 0)
                    sb.AppendLine(Properties.Resources.NoRegistryValues);

                registryInfoTextBox.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                registryInfoTextBox.Text = string.Format(Properties.Resources.ErrorReadingRegistry, ex.Message);
            }
        }



        private static bool IsUpgradeLocked
        {
            get
            {
                using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                if (key == null)
                    return false;

                object? targetReleaseVersion = key.GetValue("TargetReleaseVersion");
                object? targetReleaseVersionInfo = key.GetValue("TargetReleaseVersionInfo");

                return targetReleaseVersion != null && targetReleaseVersionInfo != null;
            }
        }

        private void UpdateStatusLabel()
        {
            statusLabel.Text = IsUpgradeLocked
                ? ControlUpgrade.Properties.Resources.StatusLocked
                : ControlUpgrade.Properties.Resources.StatusUnlocked;
        }



    }
}
