using System;
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

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.GetForCurrentView().Title = ((MultiInstanceTrial.App)App.Current).Instance.Key;
            tbIAm.Text = "I am " + ((MultiInstanceTrial.App)App.Current).Instance.Key;
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

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.SignalDataChanged();
        }

        private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
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
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(NAME_MMF, SIZE_MMF))
                {
                    string text = "";

                    using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, 0))
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        text = reader.ReadString();
                    }

                    using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, 0))
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write((string.IsNullOrEmpty(text) ? "" : text + "\n") + inputText);
                    }

                    ApplicationData.Current.SignalDataChanged();
                }
            }
            catch (FileNotFoundException)
            {

            }
        }

        private void TextBoxInput_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdateMMFTextArea(TextBoxInput.Text);
                TextBoxInput.Text = "";
            }
        }

        private async Task<bool> RefreshPage()
        {
            // Instance list

            // Clear current instance list
            LbInstances.Items.Clear();

            // Enum instances and update the list
            foreach(var instance in AppInstance.GetInstances())
            {
                LbInstances.Items.Add(instance.Key);
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
                        ImgSample.Source = bi;
                        TbHeader.Visibility = Visibility.Collapsed;
                        tbImageFileName.Text = filename;
                    }
                }
            }

            // Show MMF Content
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(NAME_MMF, SIZE_MMF))
            using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, 0))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                        TbMMF.Text = reader.ReadString();
            }

            return true;
        }
    }
}
