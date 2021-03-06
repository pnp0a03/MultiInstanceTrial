﻿using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Text;

namespace MultiInstanceTrial
{
    public sealed partial class MainPage : Page
    {
        private const string NAME_MMF = "Local\\hogehoge";
        private const long SIZE_MMF = 10000;
        private MemoryMappedFile Mmf = null;

        public MainPage()
        {
            InitializeComponent();

            Mmf = MemoryMappedFile.CreateOrOpen(NAME_MMF, SIZE_MMF);

            ApplicationView.GetForCurrentView().Title = ((MultiInstanceTrial.App)App.Current).Instance.Key;
            IAmText.Text = "I am " + ((MultiInstanceTrial.App)App.Current).Instance.Key;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationData.Current.DataChanged += Current_DataChanged;
            ApplicationData.Current.SignalDataChanged();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ApplicationData.Current.DataChanged -= Current_DataChanged;
        }

        public static async System.Threading.Tasks.Task CallOnUiThreadAsync(Windows.UI.Core.DispatchedHandler handler) =>
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, handler);

        private async void Current_DataChanged(ApplicationData sender, object args)
        {
            await CallOnUiThreadAsync(async () =>
            {
                await RefreshPage();
            });
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var fop = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SettingsIdentifier = "MultiInstanceTrial",
                FileTypeFilter = { ".jpg", ".png", ".gif" },
            };

            var file = await fop.PickSingleFileAsync();
            if(null != file)
            {
                var dstFile = await file.CopyAsync(ApplicationData.Current.LocalFolder, file.Name, NameCollisionOption.ReplaceExisting);
                if( null != dstFile)
                {
                    ApplicationData.Current.LocalSettings.Values["PictureFileName"] = dstFile.Name;
                    ApplicationData.Current.SignalDataChanged();
                }
            }
        }

        private void UpdateMMFTextArea(string inputText)
        {
            try
            {
                //MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(NAME_MMF, SIZE_MMF));
                string text = "";

                using (MemoryMappedViewStream stream = Mmf.CreateViewStream(0, 0))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    text = reader.ReadString();
                }

                using (MemoryMappedViewStream stream = Mmf.CreateViewStream(0, 0))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((string.IsNullOrEmpty(text) ? "" : text + "\n") + inputText);
                }

                ApplicationData.Current.SignalDataChanged();
            }
            catch (FileNotFoundException)
            {

            }
        }

        private void InputTextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdateMMFTextArea(InputTextBox.Text);
                InputTextBox.Text = "";
            }
        }

        private async Task<bool> RefreshPage()
        {
            // Instance list

            // Clear current instance list
            InstancesListBox.Items.Clear();

            // Enum instances and update the list
            foreach(var instance in AppInstance.GetInstances())
            {
                InstancesListBox.Items.Add(instance.Key);
            }

            // Load image
            if (ApplicationData.Current.LocalSettings.Values["PictureFileName"] is string filename)
            {
                if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(filename) is StorageFile file)
                {
                    using (var stream = await file.OpenReadAsync())
                    {
                        var bi = new BitmapImage();
                        await bi.SetSourceAsync(stream);
                        SampleImage.Source = bi;
                        ImagePlaceholderText.Visibility = Visibility.Collapsed;
                        ImageFileNameText.Text = filename;
                    }
                }
            }

            // Show MMF Content
            //using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(NAME_MMF, SIZE_MMF))
            using (MemoryMappedViewStream stream = Mmf.CreateViewStream(0, 0))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                        MemoryMappedFileText.Text = reader.ReadString();
            }

            return true;
        }
    }
}
