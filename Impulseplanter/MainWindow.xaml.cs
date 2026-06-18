using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Impulseplanter
{
    public partial class MainWindow : Window
    {
        private string selectedImagePath = string.Empty;
        private string selectedAudioPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            // Wire up buttons
            GenerateButton.Click += GenerateButton_Click;
            CloseButton.Click += CloseButton_Click;
            SelectImageButton.Click += SelectImageButton_Click;
            SelectAudioButton.Click += SelectAudioButton_Click;

            WidthBox.TextChanged += (s, e) => UpdatePreview();
            HeightBox.TextChanged += (s, e) => UpdatePreview();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e) // Drag window when click pressing anywhere
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) // Close window
        {
            Close();
        }

        // Select Image
        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter =
                    "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tif;*.tiff;*.tga;*.ico;*.ppm;*.pgm;*.pbm;*.pnm;*.dds;*.exr;*.hdr;*.jxl;*.heic;*.heif;*.avif|" +
                    "All Files|*.*",
                Title = "Select an image"
            };

            if (ofd.ShowDialog() == true)
            {
                selectedImagePath = ofd.FileName;
                UpdatePreview();
                SelectImageButtonText.Text = Path.GetFileName(selectedImagePath);
            }
        }

        // Select Audio
        private void SelectAudioButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter =
                    "Audio Files|*.mp3;*.wav;*.aac;*.m4a;*.flac;*.ogg;*.opus;*.wma;*.aiff;*.aif;*.alac;*.ape;*.mp2;*.mp1;*.ac3;*.eac3;*.amr;*.au;*.ra;*.rm;*.mid;*.midi;*.mka;*.caf;*.tta;*.wv;*.spx;*.dts;*.dsf;*.dff;*.tak;*.mpc;*.pcm;*.snd;*.3gp;*.webm|" +
                    "All Files|*.*",
                Title = "Select an audio file"
            };

            if (ofd.ShowDialog() == true)
            {
                selectedAudioPath = ofd.FileName;
                SelectAudioButtonText.Text = Path.GetFileName(selectedAudioPath);
            }
        }

        private bool TryGetResolution(out int width, out int height) // Parse resolution
        {
            width = 0;
            height = 0;

            if (!int.TryParse(WidthBox.Text, out width) || width <= 0)
                return false;

            if (!int.TryParse(HeightBox.Text, out height) || height <= 0)
                return false;

            return true;
        }

        private void UpdatePreview()
        {
            if (string.IsNullOrEmpty(selectedImagePath))
                return;

            try
            {
                if (!TryGetResolution(out int width, out int height))
                    return;

                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new System.Uri(selectedImagePath);

                // Resize while decoding
                bitmap.DecodePixelWidth = width;
                bitmap.DecodePixelHeight = height;

                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // Assign to preview
                PreviewImage.Source = bitmap;
            }
            catch
            {
                MessageBox.Show("Failed to load preview image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e) // Generate video button
        {
            if (string.IsNullOrEmpty(selectedImagePath))
            {
                MessageBox.Show("Please select an image!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(selectedAudioPath))
            {
                MessageBox.Show("Please select an audio file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!TryGetResolution(out int width, out int height))
            {
                MessageBox.Show("Invalid resolution!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string resolution = $"{width}x{height}";
            string audioFolder = Path.GetDirectoryName(selectedAudioPath);
            string audioNameWithoutExt = Path.GetFileNameWithoutExtension(selectedAudioPath);

            var sfd = new SaveFileDialog
            {
                Title = "Save video as",
                Filter = "MP4 Video|*.mp4",
                InitialDirectory = audioFolder,
                FileName = audioNameWithoutExt + ".mp4",
                DefaultExt = ".mp4"
            };

            if (sfd.ShowDialog() != true)
                return;

            string outputPath = sfd.FileName;
            string vfFilter;

            if (StretchCheckBox.IsChecked == true)
            {
                vfFilter = $"scale={width}:{height},setsar=1";
            }
            else
            {
                vfFilter = $"scale={width}:{height}:force_original_aspect_ratio=decrease," +
                           $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2,setsar=1";
            }

            string args = $"-loop 1 -r 1 -i \"{selectedImagePath}\" -i \"{selectedAudioPath}\" " +
                          $"-vf \"{vfFilter}\" -c:v libx264 -tune stillimage -c:a aac -shortest \"{outputPath}\"";

            try
            {
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = false, // show console window
                    };

                    using var ffmpeg = Process.Start(psi);

                    // Optionally, read output asynchronously
                    //ffmpeg.OutputDataReceived += (s, e) => { if (e.Data != null) Debug.WriteLine(e.Data); };
                    //ffmpeg.ErrorDataReceived += (s, e) => { if (e.Data != null) Debug.WriteLine(e.Data); };

                    //ffmpeg.BeginOutputReadLine();
                    //ffmpeg.BeginErrorReadLine();

                    ffmpeg.WaitForExit();
                });

                MessageBox.Show($"Video generated: {outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Failed to run FFmpeg. Make sure it is installed and added to PATH.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}