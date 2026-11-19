using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace NVMe_Secure_Erase
{
    public partial class MainWindow : Window
    {
        // ========================================================================
        // SECURITY & NETWORK CONFIGURATION
        // ========================================================================
        private const string DownloadUrl = "https://github.com/austinstiefel/NVMe-Secure-Erase-Utility/releases/download/v1.0.0-beta.1/NVMe_Secure_Erase-v1.0.0-BETA_1.zip";
        private const string HashUrl = "https://raw.githubusercontent.com/austinstiefel/NVMe-Secure-Erase-Utility/refs/heads/main/hash.txt";
        // ========================================================================

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowPortableMode_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "WARNING: Windows Portable Mode Limitations\n\n" +
                "Because Windows blocks low-level hardware commands, this portable mode uses " +
                "Windows PowerShell to clear partitions and unallocate space.\n\n" +
                "It DOES NOT reliably trigger the hardware-level cryptographic sanitize. " +
                "For maximum security, please use the Bootable USB option instead.",
                "Security Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                MenuFrame.Visibility = Visibility.Hidden;
                PortableFrame.Visibility = Visibility.Visible;
                _ = StartAsyncScan();
            }
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            PortableFrame.Visibility = Visibility.Hidden;
            MenuFrame.Visibility = Visibility.Visible;
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow
            {
                Owner = this // Centers the about window over the main app
            };
            about.ShowDialog();
        }

        private async Task StartAsyncScan()
        {
            DriveListBox.Items.Clear();
            DriveListBox.Items.Add("Scanning for NVMe/SSD drives...");
            DriveListBox.IsEnabled = false;

            string drivesJson = await Task.Run(() => GetDrivesViaPowerShell());

            DriveListBox.Items.Clear();
            DriveListBox.IsEnabled = true;

            if (string.IsNullOrWhiteSpace(drivesJson))
            {
                DriveListBox.Items.Add("No compatible SSD/NVMe drives found.");
                return;
            }

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(drivesJson))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement element in root.EnumerateArray())
                        {
                            AddDriveToList(element);
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        AddDriveToList(root);
                    }
                }
            }
            catch
            {
                DriveListBox.Items.Add("No compatible SSD/NVMe drives found.");
            }
        }

        private void AddDriveToList(JsonElement element)
        {
            string deviceId = element.GetProperty("DeviceId").GetString() ?? "";
            string model = element.GetProperty("Model").ValueKind != JsonValueKind.Null ? element.GetProperty("Model").GetString() ?? "" : "";
            string friendlyName = element.GetProperty("FriendlyName").ValueKind != JsonValueKind.Null ? element.GetProperty("FriendlyName").GetString() ?? "" : "";

            string displayName = !string.IsNullOrEmpty(model) ? model : friendlyName;
            DriveListBox.Items.Add($"Disk {deviceId}: {displayName}");
        }

        private string GetDrivesViaPowerShell()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"Get-PhysicalDisk | Where-Object MediaType -eq 'SSD' | Select-Object DeviceId, Model, FriendlyName | ConvertTo-Json\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process == null) return ""; // Safety check

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }
            }
            catch
            {
                return "";
            }
        }

        private void WipeDrive_Click(object sender, RoutedEventArgs e)
        {
            string selected = DriveListBox.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(selected) || selected.Contains("Scanning") || selected.Contains("No compatible"))
            {
                MessageBox.Show("Please select a valid drive.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string diskId = selected.Split(':')[0].Replace("Disk ", "");
            var confirm = MessageBox.Show($"PERMANENTLY ERASE {selected}?\n\nThis will destroy all data and partitions.",
                                          "DANGER", MessageBoxButton.YesNo, MessageBoxImage.Hand);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"Clear-Disk -Number {diskId} -RemoveData -RemoveOEM -Confirm:$false\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process? process = Process.Start(psi))
                    {
                        if (process == null)
                        {
                            MessageBox.Show("Failed to launch PowerShell!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return; // Safety Check
                        }
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            MessageBox.Show($"{selected} has been wiped and unallocated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            _ = StartAsyncScan();
                        }
                        else
                        {
                            MessageBox.Show("Failed to wipe. Drive may be in use by Windows or locked.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExtractIso_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save Bootable ISO",
                Filter = "ISO files (*.iso)|*.iso",
                FileName = "NVMe_Secure_Erase.iso"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadStatusText.Visibility = Visibility.Visible;
                DownloadProgressBar.Value = 0;
                DownloadProgressBar.IsIndeterminate = false;

                string tempZipPath = saveFileDialog.FileName + ".temp.zip";
                bool downloadSuccess = false;

                try
                {
                    DownloadStatusText.Text = "Downloading ZIP archive...";
                    using (HttpClient client = new HttpClient())
                    {
                        using (HttpResponseMessage response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            long? totalBytes = response.Content.Headers.ContentLength;

                            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                            using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                            {
                                byte[] buffer = new byte[81920];
                                long totalRead = 0;
                                int read;

                                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;

                                    if (totalBytes.HasValue)
                                    {
                                        double percentage = Math.Round((double)totalRead / totalBytes.Value * 100, 1);
                                        DownloadProgressBar.Value = percentage;
                                        DownloadStatusText.Text = $"Downloading ZIP: {percentage}%";
                                    }
                                }
                            }
                        }
                    }
                    downloadSuccess = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Download failed.\n\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                }

                if (downloadSuccess)
                {
                    try
                    {
                        DownloadProgressBar.IsIndeterminate = true;
                        DownloadStatusText.Text = "Fetching security manifest...";

                        // 1. Fetch the expected hash dynamically from GitHub
                        string expectedZipHash = "";
                        using (HttpClient client = new HttpClient())
                        {
                            expectedZipHash = await client.GetStringAsync(HashUrl);
                            expectedZipHash = expectedZipHash.Trim(); // Removes any invisible newlines or spaces
                        }

                        // 2. Verify the downloaded ZIP against the dynamic hash
                        DownloadStatusText.Text = "Verifying security hash...";
                        bool isHashValid = await VerifyFileHashAsync(tempZipPath, expectedZipHash);

                        if (!isHashValid)
                        {
                            File.Delete(tempZipPath);
                            MessageBox.Show("SECURITY ALERT: The downloaded file's cryptographic hash does not match the live security manifest. The file has been automatically deleted.", "Verification Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                            DownloadProgressBar.Visibility = Visibility.Hidden;
                            DownloadStatusText.Visibility = Visibility.Hidden;
                            return;
                        }

                        DownloadStatusText.Text = "Extracting ISO...";
                        await Task.Run(() =>
                        {
                            using (ZipArchive archive = ZipFile.OpenRead(tempZipPath))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    if (entry.FullName.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                                    {
                                        entry.ExtractToFile(saveFileDialog.FileName, overwrite: true);
                                        break;
                                    }
                                }
                            }
                        });

                        if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                        MessageBox.Show($"ISO successfully downloaded, verified, and extracted!\nSaved to: {saveFileDialog.FileName}\n\nNEXT STEPS:\n1. Download a flashing tool like Rufus.\n2. Flash this ISO to a USB drive.\n3. Boot from the USB.", "Secure Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during extraction or verification:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                    }
                }

                DownloadProgressBar.Visibility = Visibility.Hidden;
                DownloadStatusText.Visibility = Visibility.Hidden;
            }
        }

        private async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hashBytes = await sha256.ComputeHashAsync(fileStream);
                string actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return actualHash == expectedHash.ToLowerInvariant();
            }
        }
    }
}