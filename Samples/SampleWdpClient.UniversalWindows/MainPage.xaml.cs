﻿using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Tools.WindowsDevicePortal;
using static Microsoft.Tools.WindowsDevicePortal.DevicePortal;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;

namespace SampleWdpClient.UniversalWindows
{
    /// <summary>
    /// The main page of the application.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// The device portal to which we are connecting.
        /// </summary>
        private DevicePortal portal;

        private List<DevicePortal> listDevices = new List<DevicePortal>();

        private Certificate certificate;

        /// <summary>
        /// The main page constructor.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.EnableDeviceControls(false);
        }

        /// <summary>
        /// TextChanged handler for the address text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Address_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConnectButton();
        }

        /// <summary>
        /// TextChanged handler for the package path textbox
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void PackagePath_Changed(object sender, TextChangedEventArgs e)
        {
            EnableInstallButton();
        }
        /// <summary>
        /// If specified in the UI, clears the test output display, otherwise does nothing.
        /// </summary>
        private void ClearOutput()
        {
            bool clearOutput = this.clearOutput.IsChecked.HasValue ? this.clearOutput.IsChecked.Value : false;
            if (clearOutput)
            {
                this.commandOutput.Text = string.Empty;
            }
        }

        /// <summary>
        /// Click handler for the connectToDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void ConnectToDevice_Click(object sender, RoutedEventArgs e)
        {
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            this.ClearOutput();

            bool allowUntrusted = this.allowUntrustedCheckbox.IsChecked.Value;

            portal = new DevicePortal(
                new DefaultDevicePortalConnection(
                    this.address.Text,
                    this.username.Text.Length == 0 ? "a" : this.username.Text,
                    this.password.Password.Length == 0 ? "a" : this.password.Password));

            StringBuilder sb = new StringBuilder();
            Task connectTask = new Task(
                async () =>
                {
                    sb.Append(this.MarshalGetCommandOutput());
                    sb.AppendLine("Connecting...");
                    this.MarshalUpdateCommandOutput(sb.ToString());
                    portal.ConnectionStatus += (portal, connectArgs) =>
                    {
                        if (connectArgs.Status == DeviceConnectionStatus.Connected)
                        {
                            sb.Append("Connected to: ");
                            sb.AppendLine(portal.Address);
                            sb.Append("OS version: ");
                            sb.AppendLine(portal.OperatingSystemVersion);
                            sb.Append("Device family: ");
                            sb.AppendLine(portal.DeviceFamily);
                            sb.Append("Platform: ");
                            sb.AppendLine(String.Format("{0} ({1})",
                                portal.PlatformName,
                                portal.Platform.ToString()));
                        }
                        else if (connectArgs.Status == DeviceConnectionStatus.Failed)
                        {
                            sb.AppendLine("Failed to connect to the device.");
                            sb.AppendLine(connectArgs.Message);
                        }
                    };

                    try
                    {
                        // If the user wants to allow untrusted connections, make a call to GetRootDeviceCertificate
                        // with acceptUntrustedCerts set to true. This will enable untrusted connections for the
                        // remainder of this session.
                        if (allowUntrusted)
                        {
                            this.certificate = await portal.GetRootDeviceCertificateAsync(true);
                        }
                        await portal.ConnectAsync(manualCertificate: this.certificate);
                    }
                    catch (Exception exception)
                    {
                        sb.AppendLine(exception.Message);
                    }

                    this.MarshalUpdateCommandOutput(sb.ToString());
                });

            Task continuationTask = connectTask.ContinueWith(
                (t) =>
                {
                    this.MarshalEnableDeviceControls(true);
                    this.MarshalEnableConnectionControls(true);
                });

            connectTask.Start();
        }

        /// <summary>
        /// Enables or disables the Connect button based on the current state of the
        /// Address, User name and Password fields.
        /// </summary>
        private void EnableConnectButton()
        {
            bool enable = (!string.IsNullOrWhiteSpace(this.address.Text) /*&&
                        !string.IsNullOrWhiteSpace(this.username.Text) &&
                        !string.IsNullOrWhiteSpace(this.password.Password)*/);

            this.connectToDevice.IsEnabled = enable;
            this.AddDevice.IsEnabled = enable;
        }

        /// <summary>
        /// Enables or disables the Install button based on the current state of the
        /// package path field.
        /// </summary>
        private void EnableInstallButton()
        {
            bool enable = (!string.IsNullOrWhiteSpace(this.packagePath.Text));

            this.installAppButton.IsEnabled = enable;
        }
        /// <summary>
        /// Sets the IsEnabled property appropriately for the connection controls.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void EnableConnectionControls(bool enable)
        {
            this.address.IsEnabled = enable;
            this.username.IsEnabled = enable;
            this.password.IsEnabled = enable;

            this.connectToDevice.IsEnabled = enable;
        }

        /// <summary>
        /// Sets the IsEnabled property appropriately for the device command controls.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void EnableDeviceControls(bool enable)
        {
            this.rebootDevice.IsEnabled = enable;
            this.shutdownDevice.IsEnabled = enable;
        }

        /// <summary>
        /// Executes the EnabledConnectionControls method on the UI thread.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void MarshalEnableConnectionControls(bool enable)
        {
            Task t = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.EnableConnectionControls(enable);
                }).AsTask();
            t.Wait();
        }

        /// <summary>
        /// Executes the EnabledDeviceControls method on the UI thread.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void MarshalEnableDeviceControls(bool enable)
        {
            Task t = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.EnableDeviceControls(enable);
                }).AsTask();
            t.Wait();
        }

        /// <summary>
        /// Executes the fetching of the text displayed in the command output UI element on the UI thread.
        /// </summary>
        /// <returns>The contents of the command output UI element.</returns>
        private string MarshalGetCommandOutput()
        {
            string output = string.Empty;

            Task t = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    output = this.commandOutput.Text;
                }).AsTask();
            t.Wait();

            return output;
        }

        /// <summary>
        /// Executes the update of the text displayed in the command output UI element ont he UI thread.
        /// </summary>
        /// <param name="output">The text to display in the command output UI element.</param>
        private void MarshalUpdateCommandOutput(string output)
        {
            Task t = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.commandOutput.Text = output;
                }).AsTask();
            t.Wait();
        }

        /// <summary>
        /// PasswordChanged handler for the password text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            EnableConnectButton();
        }

        /// <summary>
        /// Click handler for the rebootDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void RebootDevice_Click(object sender, RoutedEventArgs e)
        {
            bool reenableDeviceControls = false;

            this.ClearOutput();
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();
            Task rebootTask = new Task(
                async () =>
                {
                    sb.Append(this.MarshalGetCommandOutput());
                    sb.AppendLine("Rebooting the device");
                    this.MarshalUpdateCommandOutput(sb.ToString());

                    try
                    {
                        await portal.RebootAsync();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("Failed to reboot the device.");
                        sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
                        reenableDeviceControls = true;
                    }
                });

            Task continuationTask = rebootTask.ContinueWith(
                (t) =>
                {
                    this.MarshalUpdateCommandOutput(sb.ToString());
                    this.MarshalEnableDeviceControls(reenableDeviceControls);
                    this.MarshalEnableConnectionControls(true);
                });

            rebootTask.Start();
        }

        /// <summary>
        /// Click handler for the shutdownDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void ShutdownDevice_Click(object sender, RoutedEventArgs e)
        {
            bool reenableDeviceControls = false;

            this.ClearOutput();
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();
            Task shutdownTask = new Task(
                async () =>
                {
                    sb.Append(this.MarshalGetCommandOutput());
                    sb.AppendLine("Shutting down the device");
                    this.MarshalUpdateCommandOutput(sb.ToString());

                    try
                    {
                        await portal.ShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("Failed to shut down the device.");
                        sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
                        reenableDeviceControls = true;
                    }
                });

            Task continuationTask = shutdownTask.ContinueWith(
                (t) =>
                {
                    this.MarshalUpdateCommandOutput(sb.ToString());
                    this.MarshalEnableDeviceControls(reenableDeviceControls);
                    this.MarshalEnableConnectionControls(true);
                });

            shutdownTask.Start();
        }

        /// <summary>
        /// TextChanged handler for the username text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConnectButton();
        }

        /// <summary>
        /// Loads a cert file for cert validation.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void LoadCertificate_Click(object sender, RoutedEventArgs e)
        {
            await LoadCertificate();
        }

        /// <summary>
        /// Loads a certificates asynchronously (runs on the UI thread).
        /// </summary>
        /// <returns></returns>
        private async Task LoadCertificate()
        {
            try
            {
                FileOpenPicker filePicker = new FileOpenPicker();
                filePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                filePicker.FileTypeFilter.Add(".cer");

                StorageFile file = await filePicker.PickSingleFileAsync();

                if (file != null)
                {
                    IBuffer cerBlob = await FileIO.ReadBufferAsync(file);

                    if (cerBlob != null)
                    {
                        certificate = new Certificate(cerBlob);
                    }
                }
            }
            catch (Exception exception)
            {
                this.commandOutput.Text = "Failed to get cert file: " + exception.Message;
            }
        }

        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            listDevices.Add(portal);
            ShowAddedDevices();
        }

        private void ShowAddedDevices()
        {
            DeviceOne.Text = "";
            AddDeviceBorder.Visibility = Visibility.Visible;
            foreach (DevicePortal device in listDevices)
            {
                DeviceOne.Text += device.Address;
                DeviceOne.Text += "\n";
            }
        }

        private void InstallApp_Click(object sender, RoutedEventArgs e)
        {
            this.ClearOutput();

            foreach (DevicePortal device in listDevices)
            {
                Task getTask = device.InstallApplicationAsync("Rubber Duck", packagePath.Text, null);
            }
        }

        private async void BrowseToFile(object sender, RoutedEventArgs e)
        {
            try
            {
                FileOpenPicker filePicker = new FileOpenPicker();
                filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                filePicker.FileTypeFilter.Add(".appx");
                filePicker.FileTypeFilter.Add(".appxbundle");
                StorageFile file = await filePicker.PickSingleFileAsync();

                Windows.ApplicationModel.Package package = Windows.ApplicationModel.Package.Current;
                Windows.Storage.StorageFolder installedLocation = package.InstalledLocation;

                String output = String.Format("Installed Location: {0}", installedLocation.Path);

                if (file != null)
                {
                    packagePath.Text = file.Path;
                }
            }
            catch (Exception exception)
            {
                this.commandOutput.Text = "Failed to get app package file: " + exception.Message;
            }
        }
    }
}