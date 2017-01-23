﻿/// <author>
/// Shawn Quereshi
/// </author>
namespace EliteUi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using EliteServiceReference;
    using GamepadManagement;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;

    // my additions
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.IO;
    using System.Diagnostics;

    /// <summary>
    /// The non-generated part of the main application.
    /// </summary>
    /// <seealso cref="Windows.UI.Xaml.Controls.Page" />
    /// <seealso cref="Windows.UI.Xaml.Markup.IComponentConnector" />
    /// <seealso cref="Windows.UI.Xaml.Markup.IComponentConnector2" />
    public sealed partial class MainPage : Page
    {
        // variables
        Windows.Storage.StorageFolder configFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        bool VibrationEnabled { get; set; } = false;

        /// <summary>
        /// The service URI
        /// </summary>
        private const string EliteServiceUri = "http://localhost:8642/EliteService";

        /// <summary>
        /// The poll rate for the gamepad
        /// </summary>
        private const int PollRateMs = 8;

        /// <summary>
        /// The timer synchronization lock
        /// </summary>
        private object timerLock = new object();

        /// <summary>
        /// The timer
        /// </summary>
        private Timer timer;

        /// <summary>
        /// The service client
        /// </summary>
        private EliteServiceClient service = null;

        /// <summary>
        /// The last timestamp of the gamepad
        /// </summary>
        private ulong lastTimestamp = 0;

        /// <summary>
        /// The gamepad
        /// </summary>
        private EliteGamepadAdapter gamepad = null;

        #region app state properties

        /// <summary>
        /// The lock for click processing
        /// </summary>
        private object clickLock = new object();

        /// <summary>
        /// The buttons which currently have mappings
        /// </summary>
        private IDictionary<GamepadButtons, VirtualKey> assignedButtons = new Dictionary<GamepadButtons, VirtualKey>();
        private IDictionary<GamepadButtons, VirtualKey> assignedModButtons = new Dictionary<GamepadButtons, VirtualKey>();

        /// <summary>
        /// The currently pressed
        /// </summary>
        private HashSet<GamepadButtons> buttonsDown = new HashSet<GamepadButtons>();

        /// <summary>
        /// Whether a key is currently being assigned
        /// </summary>
        private bool assigning = false;

        /// <summary>
        /// The gamepad button being assigned
        /// </summary>
        private GamepadButtons assignedGamepadButton = GamepadButtons.None;

        /// <summary>
        /// The last UI button clicked ("being assigned")
        /// </summary>
        private Button assignedButton = null;

        /// <summary>
        /// Whether clearing is being deferred (used for assigning spacebar)
        /// </summary>
        private bool deferringClear = false;

        #endregion

        #region View Helpers

        private string measurementId { get; set; } = "No readings yet.";
        private string measurementValue { get; set; } = "";
        private Color serviceStatusColor = Colors.Yellow;
        private string serviceStatus = "Not connected to helper service.";

        /// <summary>
        /// Updates the UI bindings on the main thread
        /// </summary>
        private void UpdateBindings()
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { Bindings.Update(); }).AsTask();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the application.
        /// </summary>
        private void Initialize()
        {
            this.InitializeWindow();
            this.InitializeTimer();
            this.InitializeEvents();
        }

        /// <summary>
        /// Initializes the application.
        /// </summary>
        private async void InitializeWindow()
        {
            ApplicationView.PreferredLaunchViewSize = new Size { Height = 580 , Width = 570 };
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.GetForCurrentView().TryResizeView(new Size { Height = 580, Width = 570 });
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size { Height = 580, Width = 570 });

            // Fill dropdown lists for modifiers
            comboBoxMod1.ItemsSource = Enum.GetNames(typeof(VirtualKey));
            comboBoxMod2.ItemsSource = Enum.GetNames(typeof(VirtualKey));
            comboBoxMod3.ItemsSource = Enum.GetNames(typeof(VirtualKey));
            comboBoxMod4.ItemsSource = Enum.GetNames(typeof(VirtualKey));

            // Check config
            await configPresent();
            await PushKeysToUI();
            await populateProfiles();
            comboBoxProfile.SelectedIndex = 0;
        }

        /// <summary>
        /// Checks if config file is present and handles each case
        /// </summary>
        private async Task configPresent()
        {
            try
            {
                Windows.Storage.StorageFile configFile = await configFolder.GetFileAsync("Default.bin");

                // read
                var ibuffer = await Windows.Storage.FileIO.ReadBufferAsync(configFile);
                byte[] buffer = ibuffer.ToArray();

                // Get and set assignedButtons
                assignedButtons[GamepadButtons.Aux1] = (VirtualKey)BitConverter.ToUInt16(buffer, 0);
                assignedButtons[GamepadButtons.Aux2] = (VirtualKey)BitConverter.ToUInt16(buffer, 2);
                assignedButtons[GamepadButtons.Aux3] = (VirtualKey)BitConverter.ToUInt16(buffer, 4);
                assignedButtons[GamepadButtons.Aux4] = (VirtualKey)BitConverter.ToUInt16(buffer, 6);

                // Get and set assignedModButtons
                assignedModButtons[GamepadButtons.Aux1] = (VirtualKey)BitConverter.ToUInt16(buffer, 8);
                assignedModButtons[GamepadButtons.Aux2] = (VirtualKey)BitConverter.ToUInt16(buffer, 10);
                assignedModButtons[GamepadButtons.Aux3] = (VirtualKey)BitConverter.ToUInt16(buffer, 12);
                assignedModButtons[GamepadButtons.Aux4] = (VirtualKey)BitConverter.ToUInt16(buffer, 14);

                // Get and set vibration
                if (buffer[16] == 1)
                {
                    VibrationEnabled = true;
                }
                else
                {
                    VibrationEnabled = false;
                }
            }
            catch
            {
                // create empty config
                Windows.Storage.StorageFile create = await configFolder.CreateFileAsync("Default.bin", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                byte[] buffer = new byte[17];
                var ibuffer = buffer.AsBuffer();
                await Windows.Storage.FileIO.WriteBufferAsync(create, ibuffer);

                // assignedButtons
                assignedButtons[GamepadButtons.Aux1] = VirtualKey.None;
                assignedButtons[GamepadButtons.Aux2] = VirtualKey.None;
                assignedButtons[GamepadButtons.Aux3] = VirtualKey.None;
                assignedButtons[GamepadButtons.Aux4] = VirtualKey.None;

                // assignedModButtons
                assignedModButtons[GamepadButtons.Aux1] = VirtualKey.None;
                assignedModButtons[GamepadButtons.Aux2] = VirtualKey.None;
                assignedModButtons[GamepadButtons.Aux3] = VirtualKey.None;
                assignedModButtons[GamepadButtons.Aux4] = VirtualKey.None;
            }
        }

        /// <summary>
        /// Populates the profile combobox.
        /// </summary>
        /// 
        private async Task populateProfiles()
        {
            var profiles = Directory.EnumerateFiles(configFolder.Path, "*.bin", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension);
            comboBoxProfile.ItemsSource = profiles;
        }

        /// <summary>
        /// Initializes the timer for polling.
        /// </summary>
        private void InitializeTimer()
        {
            this.timer = new Timer(new TimerCallback(this.TimerCallback), this, 0, PollRateMs);
        }

        /// <summary>
        /// Initializes any events being used that aren't implicitly initialized.
        /// </summary>
        private void InitializeEvents()
        {
            // If we want to intercept keys before they hit buttons so we can use Spacebar
            CoreWindow.GetForCurrentThread().KeyDown += (o, e) =>
            {
                if (e.VirtualKey == VirtualKey.Space)
                {
                    this.OnKeyDown(e.VirtualKey, false);
                }
            };
        }

        #endregion

        #region Connections

        /// <summary>
        /// Ensures the service is initialized
        /// </summary>
        private void EnsureServiceInitialized()
        {
            if (this.service == null)
            {
                this.service = new EliteServiceClient(EliteServiceClient.EndpointConfiguration.NetHttpBinding_IEliteService, EliteServiceUri);
            }
        }

        #endregion

        #region Background controller polling

        /// <summary>
        /// The function to do processing for the timer
        /// </summary>
        /// <param name="state">The object holding the state to be used by the timer..</param>
        private void TimerCallback(object state)
        {
            if (!Monitor.TryEnter(this.timerLock))
            {
                return;
            }
            
            try
            {
                if ((this.gamepad == null && !EliteGamepadAdapter.TryCreate(out this.gamepad)) || !this.gamepad.IsReady)
                {
                    return;
                }
                
                var reading = this.gamepad.GetCurrentUnmappedReading();
                if (this.ShouldSkipGamepadProcessing(reading))
                {
                    return;
                }

                WriteGamepadReadings(reading);
                ProcessGamepadReadings(reading);
                UpdateVibrations(reading);
            }
            catch (Exception)
            {
                // Do nothing, gamepad could be uninitialized
            }
            finally
            {
                Monitor.Exit(this.timerLock);
            }
        }

        /// <summary>
        /// Updates vibration motors based on trigger readings
        /// </summary>
        /// <param name="reading">The reading.</param>
        private void UpdateVibrations(GamepadReading reading)
        {
            Windows.Gaming.Input.Gamepad gamepad = Windows.Gaming.Input.Gamepad.Gamepads.FirstOrDefault();
            if (gamepad == default(Windows.Gaming.Input.Gamepad))
            {
                return;
            }
            Windows.Gaming.Input.GamepadVibration vibration;
            EliteGamepadConfiguration config = this.gamepad.gamepad.GetConfiguration(this.gamepad.CurrentSlotId);

            vibration.LeftMotor = 0.0;
            vibration.LeftTrigger = 0.0;
            vibration.RightMotor = 0.0;
            vibration.RightTrigger = 0.0;

            // Set vibration
            if (reading.LeftTrigger != 0.0)
            {
                if (VibrationEnabled == true)
                {
                    double value = normalizeTriggerValue(reading.LeftTrigger, config.LeftTrigger.Min / 10, config.LeftTrigger.Max / 10);
                    vibration.LeftTrigger = value * config.ScaleFactorLeftTriggerMotor / 100.0f;
                    gamepad.Vibration = vibration;
                }
            }
            else
            {
                vibration.LeftTrigger = 0.0;
                gamepad.Vibration = vibration;
            }

            if (reading.RightTrigger != 0.0)
            {
                if (VibrationEnabled == true)
                {
                    double value = normalizeTriggerValue(reading.RightTrigger, config.RightTrigger.Min / 10, config.RightTrigger.Max / 10);
                    vibration.RightTrigger = value * config.ScaleFactorRightTriggerMotor / 100.0f;
                    gamepad.Vibration = vibration;
                }
            }
            else
            {
                vibration.RightTrigger = 0.0;
                gamepad.Vibration = vibration;
            }
        }

        /// <summary>
        /// Whether gamepad processing should be skipped due to the current and previous reading being the same.
        /// </summary>
        /// <param name="reading">The reading.</param>
        /// <returns>True if the reading is new and should be processed; otherwise, false.</returns>
        private bool ShouldSkipGamepadProcessing(GamepadReading reading)
        {
            var newTimestamp = reading.Timestamp;
            if (this.lastTimestamp == reading.Timestamp)
            {
                return true;
            }

            this.lastTimestamp = newTimestamp;
            return false;
        }

        /// <summary>
        /// Prints the gamepad readings to the UI
        /// </summary>
        /// <param name="reading">The reading.</param>
        private void WriteGamepadReadings(GamepadReading reading)
        {
            var propertyStringBuilder = new StringBuilder();
            var valueStringBuilder = new StringBuilder();

            valueStringBuilder.AppendLine(this.gamepad.CurrentSlotId.ToString());
            propertyStringBuilder.AppendLine(string.Format("Slot:"));

            valueStringBuilder.AppendLine(reading.Buttons.ToString());
            propertyStringBuilder.AppendLine("Buttons:");

            valueStringBuilder.AppendLine(reading.LeftTrigger.ToString());
            propertyStringBuilder.AppendLine("LeftTrigger:");

            valueStringBuilder.AppendLine(reading.RightTrigger.ToString());
            propertyStringBuilder.AppendLine("RightTrigger:");

            valueStringBuilder.AppendLine(reading.LeftThumbstickX.ToString());
            propertyStringBuilder.AppendLine("LeftThumbstickX:");

            valueStringBuilder.AppendLine(reading.LeftThumbstickY.ToString());
            propertyStringBuilder.AppendLine("LeftThumbstickY:");

            valueStringBuilder.AppendLine(reading.RightThumbstickX.ToString());
            propertyStringBuilder.AppendLine("RightThumbstickX:");

            valueStringBuilder.AppendLine(reading.RightThumbstickY.ToString());
            propertyStringBuilder.AppendLine("RightThumbstickY:");
                        
            EliteGamepadConfiguration config = this.gamepad.gamepad.GetConfiguration(this.gamepad.CurrentSlotId);

            // Vibration values
            valueStringBuilder.AppendLine(config.LeftTrigger.Min.ToString());
            propertyStringBuilder.AppendLine("LeftTriggerMin:");

            valueStringBuilder.AppendLine(config.LeftTrigger.Max.ToString());
            propertyStringBuilder.AppendLine("LeftTriggerMax:");

            valueStringBuilder.AppendLine(config.RightTrigger.Min.ToString());
            propertyStringBuilder.AppendLine("RightTriggerMin:");

            valueStringBuilder.AppendLine(config.RightTrigger.Max.ToString());
            propertyStringBuilder.AppendLine("RightTriggerMax:");

            double value = normalizeTriggerValue(reading.LeftTrigger, config.LeftTrigger.Min / 10, config.LeftTrigger.Max / 10);
            valueStringBuilder.AppendLine(value.ToString());
            propertyStringBuilder.AppendLine("VibrationValueLeft");

            value = normalizeTriggerValue(reading.RightTrigger, config.RightTrigger.Min / 10, config.RightTrigger.Max / 10);
            valueStringBuilder.AppendLine(value.ToString());
            propertyStringBuilder.AppendLine("VibrationValueRight");

            // set strings used in UI
            measurementId = propertyStringBuilder.ToString();
            measurementValue = valueStringBuilder.ToString();
            UpdateBindings();
        }

        /// <summary>
        /// Processes the gamepad readings for sending the assigned key.
        /// </summary>
        /// <param name="reading">The reading.</param>
        private void ProcessGamepadReadings(GamepadReading reading)
        {
            var values = (GamepadButtons[])Enum.GetValues(typeof(GamepadButtons));

            var tasks = new HashSet<Task>();
            foreach (var value in values.Where(v => (reading.Buttons & v) != 0))
            {
                VirtualKey key;
                VirtualKey modKey;
                if (this.assignedButtons.TryGetValue(value, out key) && this.assignedModButtons.TryGetValue(value, out modKey) && !this.buttonsDown.Contains(value))
                {
                    this.buttonsDown.Add(value);
                    this.SendKeyDown(key, modKey);
                }

                switch (reading.Buttons & value)
                {
                    case GamepadButtons.Aux1:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux1_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x10, 0x7C, 0x10)); }).AsTask());
                        break;
                    case GamepadButtons.Aux2:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux2_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x10, 0x7C, 0x10)); }).AsTask());
                        break;
                    case GamepadButtons.Aux3:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux3_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x10, 0x7C, 0x10)); }).AsTask());
                        break;
                    case GamepadButtons.Aux4:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux4_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x10, 0x7C, 0x10)); }).AsTask());
                        break;
                    default:
                        break;
                }
            }

            foreach (var value in values.Where(v => (reading.Buttons & v) == 0))
            {
                if (this.buttonsDown.Contains(value))
                {
                    VirtualKey key;
                    VirtualKey modKey;
                    if (this.assignedButtons.TryGetValue(value, out key) && this.assignedModButtons.TryGetValue(value, out modKey))
                    {
                        this.buttonsDown.Add(value);
                        this.SendKeyUp(key, modKey);
                    }

                    this.buttonsDown.Remove(value);
                }

                if (this.buttonsDown.Contains(value))
                {
                    this.buttonsDown.Remove(value);
                }

                switch (value)
                {
                    case GamepadButtons.Aux1:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux1_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A)); }).AsTask());
                        break;
                    case GamepadButtons.Aux2:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux2_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A)); }).AsTask());
                        break;
                    case GamepadButtons.Aux3:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux3_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A)); }).AsTask());
                        break;
                    case GamepadButtons.Aux4:
                        tasks.Add(Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { aux4_identifier.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A)); }).AsTask());
                        break;
                    default:
                        break;
                }
            }

            // Wait for UI to update
            Task.WaitAll(tasks.ToArray<Task>());
        }

        /// <summary>
        /// Sends a key down signal.
        /// </summary>
        /// <param name="key">The key.</param>
        private async void SendKeyDown(VirtualKey key, VirtualKey modKey)
        {
            this.EnsureServiceInitialized();
            try
            {
                await this.service.SendKeyDownAsync((ushort)key, (ushort)modKey);
                this.serviceStatusColor = Colors.Green;
                this.serviceStatus = "Connected to helper service.";

            }
            catch (Exception ex)
            {
                this.serviceStatusColor = Colors.Red;
                this.serviceStatus = "Failed to send key down.\n" + ex.Message;
            } finally
            {
                UpdateBindings();
            }
        }

        /// <summary>
        /// Sends a key up signal.
        /// </summary>
        /// <param name="key">The key.</param>
        private async void SendKeyUp(VirtualKey key, VirtualKey modKey)
        {
            this.EnsureServiceInitialized();
            try
            {
                await this.service.SendKeyUpAsync((ushort)key, (ushort)modKey);
                this.serviceStatusColor = Colors.Green;
                this.serviceStatus = "Connected to helper service.";

            } catch(Exception ex)
            {
                this.serviceStatusColor = Colors.Red;
                this.serviceStatus = "Failed to send key up.\n" + ex.Message;
            } finally
            {
                UpdateBindings();
            }
        }

        #endregion

        #region UI events and handling

        // Push assigned keys
        private async Task PushKeysToUI()
        {
            /*assignedButtons[GamepadButtons.Aux1] = VirtualKey.None;
            assignedButtons[GamepadButtons.Aux2] = VirtualKey.None;
            assignedButtons[GamepadButtons.Aux3] = VirtualKey.Y;
            assignedButtons[GamepadButtons.Aux4] = VirtualKey.F12;

            assignedModButtons[GamepadButtons.Aux1] = VirtualKey.None;
            assignedModButtons[GamepadButtons.Aux2] = VirtualKey.None;
            assignedModButtons[GamepadButtons.Aux3] = VirtualKey.Menu;
            assignedModButtons[GamepadButtons.Aux4] = VirtualKey.None;*/

            aux1_button.Content = assignedButtons[GamepadButtons.Aux1].ToString();
            aux2_button.Content = assignedButtons[GamepadButtons.Aux2].ToString();
            aux3_button.Content = assignedButtons[GamepadButtons.Aux3].ToString();
            aux4_button.Content = assignedButtons[GamepadButtons.Aux4].ToString();

            comboBoxMod1.SelectedItem = Enum.GetName(typeof(VirtualKey), assignedModButtons[GamepadButtons.Aux1]);
            comboBoxMod2.SelectedItem = Enum.GetName(typeof(VirtualKey), assignedModButtons[GamepadButtons.Aux2]);
            comboBoxMod3.SelectedItem = Enum.GetName(typeof(VirtualKey), assignedModButtons[GamepadButtons.Aux3]);
            comboBoxMod4.SelectedItem = Enum.GetName(typeof(VirtualKey), assignedModButtons[GamepadButtons.Aux4]);
        }

        // Apply a modifier
        private void modifier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            var selected = (VirtualKey)Enum.Parse(typeof(VirtualKey), (combo.SelectedItem).ToString());
            switch (combo.Tag.ToString())
            {
                case "1": assignedModButtons[GamepadButtons.Aux1] = selected; break;
                case "2": assignedModButtons[GamepadButtons.Aux2] = selected; break;
                case "3": assignedModButtons[GamepadButtons.Aux3] = selected; break;
                case "4": assignedModButtons[GamepadButtons.Aux4] = selected; break;

            }
        }

        // Toggle vibration
        private void ToggleVibration(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            VibrationEnabled = !VibrationEnabled;
        }

        // Save config
        private async void buttonSave_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Windows.Storage.StorageFile configFile = await configFolder.CreateFileAsync(comboBoxProfile.SelectedItem + ".bin", Windows.Storage.CreationCollisionOption.ReplaceExisting);

            // Create content buffer
            byte[] buffer = new byte[17];
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux1]), 0, buffer, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux2]), 0, buffer, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux3]), 0, buffer, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux4]), 0, buffer, 6, 2);

            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux1]), 0, buffer, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux2]), 0, buffer, 10, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux3]), 0, buffer, 12, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux4]), 0, buffer, 14, 2);

            Buffer.SetByte(buffer, 16, Convert.ToByte(VibrationEnabled));

            // Write buffer to file
            var ibuffer = buffer.AsBuffer();
            await Windows.Storage.FileIO.WriteBufferAsync(configFile, ibuffer);
        }

        // Reload config
        private async void buttonReload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            /*Windows.Storage.StorageFile configFile = await configFolder.GetFileAsync("Default.bin");
            var ibuffer = await Windows.Storage.FileIO.ReadBufferAsync(configFile);
            byte[] buffer = ibuffer.ToArray();*/

            string profile_path = configFolder.Path + "\\" + comboBoxProfile.SelectedItem + ".bin";

            // read
            byte[] buffer = File.ReadAllBytes(profile_path);

            // Get and set assignedButtons
            assignedButtons[GamepadButtons.Aux1] = (VirtualKey)BitConverter.ToUInt16(buffer, 0);
            assignedButtons[GamepadButtons.Aux2] = (VirtualKey)BitConverter.ToUInt16(buffer, 2);
            assignedButtons[GamepadButtons.Aux3] = (VirtualKey)BitConverter.ToUInt16(buffer, 4);
            assignedButtons[GamepadButtons.Aux4] = (VirtualKey)BitConverter.ToUInt16(buffer, 6);

            // Get and set assignedModButtons
            assignedModButtons[GamepadButtons.Aux1] = (VirtualKey)BitConverter.ToUInt16(buffer, 8);
            assignedModButtons[GamepadButtons.Aux2] = (VirtualKey)BitConverter.ToUInt16(buffer, 10);
            assignedModButtons[GamepadButtons.Aux3] = (VirtualKey)BitConverter.ToUInt16(buffer, 12);
            assignedModButtons[GamepadButtons.Aux4] = (VirtualKey)BitConverter.ToUInt16(buffer, 14);

            // Get and set vibration
            if (buffer[16] == 1)
            {
                VibrationEnabled = true;
            }
            else
            {
                VibrationEnabled = false;
            }

            await PushKeysToUI();
        }

        // Load selected profile
        private void buttonLoadProfile_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string profile_path = configFolder.Path + "\\" + comboBoxProfile.SelectedItem + ".bin";

            // read
            byte[] buffer = File.ReadAllBytes(profile_path);

            // Get and set assignedButtons
            assignedButtons[GamepadButtons.Aux1] = (VirtualKey)BitConverter.ToUInt16(buffer, 0);
            assignedButtons[GamepadButtons.Aux2] = (VirtualKey)BitConverter.ToUInt16(buffer, 2);
            assignedButtons[GamepadButtons.Aux3] = (VirtualKey)BitConverter.ToUInt16(buffer, 4);
            assignedButtons[GamepadButtons.Aux4] = (VirtualKey)BitConverter.ToUInt16(buffer, 6);

            // Get and set assignedModButtons
            assignedModButtons[GamepadButtons.Aux1] = (VirtualKey)BitConverter.ToUInt16(buffer, 8);
            assignedModButtons[GamepadButtons.Aux2] = (VirtualKey)BitConverter.ToUInt16(buffer, 10);
            assignedModButtons[GamepadButtons.Aux3] = (VirtualKey)BitConverter.ToUInt16(buffer, 12);
            assignedModButtons[GamepadButtons.Aux4] = (VirtualKey)BitConverter.ToUInt16(buffer, 14);

            // Get and set vibration
            if (buffer[16] == 1)
            {
                VibrationEnabled = true;
            }
            else
            {
                VibrationEnabled = false;
            }

            PushKeysToUI();
        }

        // Save current profile
        private async void buttonSaveProfile_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Get name
            var name_box = new TextBox { Width = 200, Height = 25 };

            var dialog = new ContentDialog()
            {
                Title = "Profile Name",
                MaxWidth = this.ActualWidth,
                Content = name_box,
                PrimaryButtonText = "Confirm"
            };

            await dialog.ShowAsync();

            var profile_name = ((TextBox)dialog.Content).Text;

            // Save to file
            Windows.Storage.StorageFile configFile = await configFolder.CreateFileAsync(profile_name + ".bin", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            
            // Create content buffer
            byte[] buffer = new byte[17];
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux1]), 0, buffer, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux2]), 0, buffer, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux3]), 0, buffer, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedButtons[GamepadButtons.Aux4]), 0, buffer, 6, 2);

            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux1]), 0, buffer, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux2]), 0, buffer, 10, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux3]), 0, buffer, 12, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)assignedModButtons[GamepadButtons.Aux4]), 0, buffer, 14, 2);

            Buffer.SetByte(buffer, 16, Convert.ToByte(VibrationEnabled));

            // Write buffer to file
            var ibuffer = buffer.AsBuffer();
            await Windows.Storage.FileIO.WriteBufferAsync(configFile, ibuffer);
        }

        // Delete selected profile
        private void buttonDeleteProfile_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string profile_path = configFolder.Path + "\\" + comboBoxProfile.SelectedItem + ".bin";
            File.Delete(profile_path);
            populateProfiles();
            comboBoxProfile.SelectedIndex = 0;
        }

        // Workaround for populating the profile combobox after creating a new profile
        private void comboBoxProfile_populate(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var prev = comboBoxProfile.SelectedItem;
            populateProfiles();
            comboBoxProfile.SelectedItem = prev;
        }

        // Normalize vibration values
        private double normalizeTriggerValue(double value, int min, int max)
        {
            return (value * 100 - min) / (max - min);
        }

        /// <summary>
        /// Handles the Click event of the aux1_button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void aux1_button_Click(object sender, RoutedEventArgs e)
        {
            lock (this.clickLock)
            {
                this.ProcessButtonClick(e.OriginalSource as Button, GamepadButtons.Aux1);
            }
        }

        /// <summary>
        /// Handles the Click event of the aux2_button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void aux2_button_Click(object sender, RoutedEventArgs e)
        {
            lock (this.clickLock)
            {
                this.ProcessButtonClick(e.OriginalSource as Button, GamepadButtons.Aux2);
            }
        }

        /// <summary>
        /// Handles the Click event of the aux3_button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void aux3_button_Click(object sender, RoutedEventArgs e)
        {
            lock (this.clickLock)
            {
                this.ProcessButtonClick(e.OriginalSource as Button, GamepadButtons.Aux3);
            }
        }

        /// <summary>
        /// Handles the Click event of the aux4_button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void aux4_button_Click(object sender, RoutedEventArgs e)
        {
            lock (this.clickLock)
            {
                this.ProcessButtonClick(e.OriginalSource as Button, GamepadButtons.Aux4);
            }
        }

        /// <summary>
        /// Raised from the the <see cref="E:KeyDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="KeyRoutedEventArgs"/> instance containing the event data.</param>
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);
            this.OnKeyDown(e.Key);
        }

        /// <summary>
        /// Called when [key down].
        /// </summary>
        /// <param name="key">The key.</param>
        private void OnKeyDown(VirtualKey key)
        {
            this.OnKeyDown(key, true);
        }

        /// <summary>
        /// Called when [key down].
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="reset">if set to <c>true</c> [reset].</param>
        private void OnKeyDown(VirtualKey key, bool reset)
        {
            if (!this.assigning)
            {
                return;
            }

            lock (this.clickLock)
            {
                if (this.assigning)
                {
                    this.assignedButtons[this.assignedGamepadButton] = key;
                    this.assignedButton.Content = key.ToString();

                    // Workaround that certain characters get captured twice 
                    this.deferringClear = !reset;
                    if (reset)
                    {
                        this.EnableButtons();
                        this.ClearAssignmentVariables();
                    }
                }
            }
        }

        /// <summary>
        /// Processes a button click.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="gamepadButton">The gamepad button associated with the UI button.</param>
        private void ProcessButtonClick(Button button, GamepadButtons gamepadButton)
        {
            if (!this.assigning)
            {
                this.DisableButtonsExcept(button);
                this.SetAssignmentVariables(button, gamepadButton);
            }
            else
            {
                if (!this.deferringClear)
                {
                    this.assignedButtons[this.assignedGamepadButton] = VirtualKey.None;
                    this.assignedButton.Content = "None";
                }

                this.ClearAssignmentVariables();
                this.EnableButtons();
                this.deferringClear = false;
            }
        }

        /// <summary>
        /// Clears assignment variables after an assignment or canceled assignment.
        /// </summary>
        private void ClearAssignmentVariables()
        {
            this.assigning = false;
            this.assignedGamepadButton = GamepadButtons.None;
            this.assignedButton = null;
        }

        /// <summary>
        /// Sets the assignment variables to begin button assignment.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="gamepadButton">The gamepad button.</param>
        private void SetAssignmentVariables(Button button, GamepadButtons gamepadButton)
        {
            this.assigning = true;
            this.assignedGamepadButton = gamepadButton;
            this.assignedButton = button;
        }

        /// <summary>
        /// Enables the UI buttons.
        /// </summary>
        private void EnableButtons()
        {
            var toEnable = new HashSet<Button>() { aux1_button, aux2_button, aux3_button, aux4_button };
            foreach (var enabled in toEnable)
            {
                enabled.IsEnabled = true;
            }
        }

        /// <summary>
        /// Disables the UI buttons except the specified button.
        /// </summary>
        /// <param name="button">The button.</param>
        private void DisableButtonsExcept(Button button)
        {
            var toDisable = new HashSet<Button>() { aux1_button, aux2_button, aux3_button, aux4_button };
            toDisable.Remove(button);

            foreach (var enabled in toDisable)
            {
                enabled.IsEnabled = false;
            }
        }
        #endregion
    }
}
