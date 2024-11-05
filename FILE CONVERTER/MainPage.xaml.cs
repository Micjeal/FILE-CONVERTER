using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Popups;
using Windows.Storage.Provider;

namespace FILE_CONVERTER
{
    public sealed partial class MainPage : Page
    {
        private IReadOnlyList<StorageFile> files;
        private StorageFolder outputFolder;

        public object StorageLibraryChangeStatus { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                // Attempt to access the DocumentsLibrary and create the output folder
                outputFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync(
                    "ConvertedFiles", CreationCollisionOption.OpenIfExists);
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorDialog("Permission Error", "Access to the Documents library is required to create the output folder. Please check your app permissions.");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Failed to create output folder", ex.Message);
            }
        }


        private async void selectbtn(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".txt");
                picker.FileTypeFilter.Add(".docx");
                picker.FileTypeFilter.Add(".pdf");

                files = await picker.PickMultipleFilesAsync();

                if (files != null && files.Count > 0)
                {
                    statusTb.Text = $"{files.Count} files selected";
                    ProgresBAR.Maximum = files.Count;
                    ProgresBAR.Value = 0;
                    ProgresBAR.Visibility = Visibility.Visible;
                    convertbtn.IsEnabled = true;
                }
                else
                {
                    statusTb.Text = "No files selected";
                    convertbtn.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("File Selection Error", ex.Message);
            }
        }

        private async void Convertbutton_click(object sender, RoutedEventArgs e)
        {
            if (formatcb.SelectedItem == null)
            {
                statusTb.Text = "Please choose format";
                return;
            }

            if (files == null || files.Count == 0)
            {
                statusTb.Text = "No files selected";
                return;
            }

            convertbtn.IsEnabled = false;
            selectfiles.IsEnabled = false;
            formatcb.IsEnabled = false;

            try
            {
                var format = (formatcb.SelectedItem as ComboBoxItem)?.Content.ToString();
                int fileIndex = 1;

                foreach (var file in files)
                {
                    statusTb.Text = $"Converting file {fileIndex} of {files.Count}";
                    await ConvertFile(file, format);
                    ProgresBAR.Value = fileIndex;
                    fileIndex++;
                }

                statusTb.Text = "Conversion complete! Files saved in Documents/ConvertedFiles";
                await ShowSuccessDialog();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Conversion Error", ex.Message);
            }
            finally
            {
                convertbtn.IsEnabled = true;
                selectfiles.IsEnabled = true;
                formatcb.IsEnabled = true;
            }
        }

        private async Task ConvertFile(StorageFile sourceFile, string format)
        {
            if (format == null) return;

            string newFileName = Path.GetFileNameWithoutExtension(sourceFile.Name) +
                               "." + format.ToLower();

            StorageFile destinationFile = await outputFolder.CreateFileAsync(
                newFileName, CreationCollisionOption.GenerateUniqueName);

            if (format.ToUpper() == "PDF")
            {
                await ConvertToPdf(sourceFile, destinationFile);
            }
            else if (format.ToUpper() == "DOCX")
            {
                await ConvertToDocx(sourceFile, destinationFile);
            }
        }

        private async Task ConvertToPdf(StorageFile sourceFile, StorageFile destinationFile)
        {
            // Read the source file
            string content = await FileIO.ReadTextAsync(sourceFile);

            // Create PDF using Windows Runtime APIs
            using (IRandomAccessStream stream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Here you would typically use a PDF library
                // For demonstration, we're just writing text content
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    using (DataWriter writer = new DataWriter(outputStream))
                    {
                        writer.WriteString(content);
                        await writer.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
            }
        }

        private async Task ConvertToDocx(StorageFile sourceFile, StorageFile destinationFile)
        {
            // Read the source file
            string content = await FileIO.ReadTextAsync(sourceFile);

            // Create DOCX using Windows Runtime APIs
            using (IRandomAccessStream stream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    using (DataWriter writer = new DataWriter(outputStream))
                    {
                        writer.WriteString(content);
                        await writer.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new MessageDialog($"{message}", title);
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessDialog()
        {
            var dialog = new MessageDialog("Files have been converted successfully!", "Success");
            await dialog.ShowAsync();
        }
    }
}