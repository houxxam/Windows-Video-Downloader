using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTube_Downloader
{
    public partial class Form1 : Form
    {
        private string ytDlpPath = Path.Combine(Application.StartupPath, "yt-dlp", "yt-dlp.exe");
        private string ffmpegPath = Path.Combine(Application.StartupPath, "yt-dlp", "ffmpeg.exe"); // Ensure ffmpeg is in the same directory
        private string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

        public Form1()
        {
            InitializeComponent();
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Populate the resolution ComboBox
            comboBoxResolution.Items.Add("1080"); // 1080p
            comboBoxResolution.Items.Add("720");  // 720p
            comboBoxResolution.Items.Add("480");  // 480p
            comboBoxResolution.Items.Add("360");  // 360p
            comboBoxResolution.SelectedIndex = 0; // Default to the first resolution
            lblStatus.Text = "";
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            string videoUrl = txtUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                MessageBox.Show("Please enter a valid video URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                lblStatus.Text = "Downloading...";
                btnDownload.Enabled = false;
                progressBar1.Value = 0; // Reset the progress bar

                // Get the selected resolution from the ComboBox
                string selectedResolution = comboBoxResolution.SelectedItem.ToString();
                string resolutionOption = $"-f \"bestvideo[height<={selectedResolution}]+bestaudio/best\" --merge-output-format mp4";

                // Command for yt-dlp
                string arguments = $"-o \"{Path.Combine(downloadFolder, "%(title)s.%(ext)s")}\" {resolutionOption} --ffmpeg-location \"{ffmpegPath}\" \"{videoUrl}\"";

                // Start yt-dlp as a process
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = new Process { StartInfo = psi };

                // Capture the output asynchronously
                process.OutputDataReceived += (s, ev) => ParseProgress(ev.Data);
                process.ErrorDataReceived += (s, ev) => Console.WriteLine(ev.Data); // Optional: Log error messages

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for the process to finish asynchronously
                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    lblStatus.Text = "Download completed successfully!";
                    lblStatus.ForeColor = Color.Green;

                }
                else
                {
                    lblStatus.Text = "An error occurred during the download.";
                    lblStatus.ForeColor = Color.Red;

                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "An error occurred.";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDownload.Enabled = true;
                txtUrl.Text = "";
            }
        }

        // Method to parse the progress output and update the progress bar
        private void ParseProgress(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                // Look for the download progress percentage (e.g., [download] 24.3% of 500MiB at 1.5MiB/s)
                string[] splitData = data.Split('%');
                if (splitData.Length > 1)
                {
                    string percentageStr = splitData[0].Trim().Split(' ')[1]; // Extract the percentage value
                    if (double.TryParse(percentageStr, out double percentage))
                    {
                        // Update the progress bar safely on the UI thread
                        if (progressBar1.InvokeRequired)
                        {
                            progressBar1.Invoke(new Action(() =>
                            {
                                progressBar1.Value = (int)percentage; // Set the progress bar value
                            }));
                        }
                        else
                        {
                            progressBar1.Value = (int)percentage;
                        }
                    }
                }
            }
        }
    }
}
