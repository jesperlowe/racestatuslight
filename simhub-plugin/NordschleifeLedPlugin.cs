using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;

namespace User.NordschleifeLedPlugin
{
    /// <summary>
    /// SimHub plugin that converts session mode + lap progress into a compact ASCII command for an Arduino.
    ///
    /// The implementation intentionally uses only the core plugin lifecycle from SimHub's SDK examples:
    /// Init(), DataUpdate(), and End(). Telemetry access is defensive because property names vary between
    /// games such as RaceRoom, Assetto Corsa, and SimHub versions.
    /// </summary>
    [PluginName("Nordschleife LED Lamp")]
    [PluginDescription("Sends Nordschleife session mode and active section commands to an Arduino over serial.")]
    [PluginAuthor("OpenAI")]
    public class NordschleifeLedPlugin : IPlugin, IDataPlugin
    {
        private const int DefaultSections = 12;
        private const int DefaultBaudRate = 115200;
        private const int DefaultUpdateIntervalMs = 100;
        private const int ReconnectIntervalMs = 1000;
        private const string PluginName = "Nordschleife LED Lamp";

        private static readonly string[] SessionPropertyCandidates =
        {
            "SessionTypeName",
            "SessionName",
            "SessionType",
            "SessionPhase",
            "GameSession",
            "CurrentSessionType",
            "SessionInfo.SessionType",
            "SessionInfo.SessionName",
            "PlayerRawData.SessionType",
            "DataCorePlugin.GameData.SessionTypeName",
            "DataCorePlugin.GameData.SessionName",
            "DataCorePlugin.GameData.SessionType"
        };

        private static readonly string[] ProgressPropertyCandidates =
        {
            "LapProgress",
            "TrackProgress",
            "TrackPosition",
            "TrackPositionPercent",
            "NormalizedPosition",
            "NormalizedTrackPosition",
            "NormalizedLapPosition",
            "CurrentLapProgress",
            "CurrentLapPercent",
            "LapDistancePercent",
            "PlayerRawData.LapDistancePercent",
            "PlayerRawData.LapProgress",
            "DataCorePlugin.GameData.LapProgress",
            "DataCorePlugin.GameData.TrackPositionPercent"
        };

        private static readonly string[] SectorPropertyCandidates =
        {
            "SectorIndex",
            "CurrentSectorIndex",
            "CurrentSector",
            "PlayerRawData.SectorIndex",
            "PlayerRawData.CurrentSectorIndex",
            "DataCorePlugin.GameData.CurrentSector",
            "DataCorePlugin.GameData.CurrentSectorIndex"
        };

        private SerialPort _serialPort;
        private DateTime _lastUpdateUtc = DateTime.MinValue;
        private DateTime _lastReconnectAttemptUtc = DateTime.MinValue;
        private string _lastMessage = string.Empty;
        private string _lastDebug = "Not started";
        private Settings _settings = new Settings();
        private string _settingsPath;

        public string Name => PluginName;
        public string Author => "OpenAI";
        public string Version => "1.0.0";
        public string LastSentMessage => _lastMessage;
        public string LastDebug => _lastDebug;

        public void Init(PluginManager pluginManager)
        {
            _settingsPath = BuildSettingsPath();
            _settings = LoadSettings(_settingsPath);
            NormalizeSettings(_settings);

            TrySetPluginProperty(pluginManager, "LastSentMessage", "");
            TrySetPluginProperty(pluginManager, "LastDebug", "Initialized; waiting for game data");

            Log(pluginManager, $"Initialized. Settings file: {_settingsPath}");
            TryOpenSerial(pluginManager, force: true);
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            try
            {
                if (!IsUpdateDue())
                {
                    return;
                }

                EnsureSerialConnected(pluginManager);

                string sessionText = ReadFirstString(pluginManager, data, SessionPropertyCandidates, out string sessionSource);
                char mode = MapSessionToMode(sessionText);

                int section = ResolveSection(pluginManager, data, _settings.LedSections, out string sectionSource);
                string message = $"{mode},{section}";

                _lastDebug = $"mode={mode} session='{sessionText ?? "<missing>"}' from {sessionSource}; section={section} from {sectionSource}; port={_settings.ComPort}";
                TrySetPluginProperty(pluginManager, "LastDebug", _lastDebug);

                if (_settings.ForceUpdate || !string.Equals(message, _lastMessage, StringComparison.Ordinal))
                {
                    SendMessage(pluginManager, message);
                }
            }
            catch (Exception ex)
            {
                // Never let missing telemetry or serial errors crash SimHub's data update loop.
                _lastDebug = "DataUpdate ignored error: " + ex.Message;
                TrySetPluginProperty(pluginManager, "LastDebug", _lastDebug);
                Log(pluginManager, _lastDebug);
                CloseSerial();
            }
        }

        public void End(PluginManager pluginManager)
        {
            CloseSerial();
            SaveSettings(_settingsPath, _settings);
            Log(pluginManager, "Stopped");
        }

        private bool IsUpdateDue()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastUpdateUtc).TotalMilliseconds < _settings.UpdateIntervalMs)
            {
                return false;
            }

            _lastUpdateUtc = now;
            return true;
        }

        private void SendMessage(PluginManager pluginManager, string message)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                Log(pluginManager, "Serial port is not open; message not sent: " + message);
                return;
            }

            _serialPort.Write(message + "\n");
            _lastMessage = message;
            TrySetPluginProperty(pluginManager, "LastSentMessage", message);
            Log(pluginManager, "Sent: " + message);
        }

        private void EnsureSerialConnected(PluginManager pluginManager)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                return;
            }

            TryOpenSerial(pluginManager, force: false);
        }

        private void TryOpenSerial(PluginManager pluginManager, bool force)
        {
            if (string.IsNullOrWhiteSpace(_settings.ComPort))
            {
                _lastDebug = "COM port is empty; edit the settings JSON or SimHub plugin settings before use";
                TrySetPluginProperty(pluginManager, "LastDebug", _lastDebug);
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (!force && (now - _lastReconnectAttemptUtc).TotalMilliseconds < ReconnectIntervalMs)
            {
                return;
            }

            _lastReconnectAttemptUtc = now;
            CloseSerial();

            try
            {
                _serialPort = new SerialPort(_settings.ComPort, _settings.BaudRate)
                {
                    Encoding = Encoding.ASCII,
                    NewLine = "\n",
                    ReadTimeout = 50,
                    WriteTimeout = 50,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.Open();
                Log(pluginManager, $"Opened {_settings.ComPort} at {_settings.BaudRate} baud");
            }
            catch (Exception ex)
            {
                _lastDebug = $"Could not open {_settings.ComPort}: {ex.Message}";
                TrySetPluginProperty(pluginManager, "LastDebug", _lastDebug);
                Log(pluginManager, _lastDebug);
                CloseSerial();
            }
        }

        private void CloseSerial()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }

                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
            catch
            {
                _serialPort = null;
            }
        }

        private int ResolveSection(PluginManager pluginManager, GameData data, int ledSections, out string source)
        {
            if (TryReadFirstDouble(pluginManager, data, ProgressPropertyCandidates, out double progress, out source))
            {
                double normalized = NormalizeProgress(progress);
                int section = (int)Math.Floor(normalized * ledSections);
                return Clamp(section, 0, ledSections - 1);
            }

            if (TryReadFirstDouble(pluginManager, data, SectorPropertyCandidates, out double sector, out source))
            {
                int sectorIndex = ConvertSectorToZeroBasedIndex(sector);
                int approxSection = (int)Math.Floor(sectorIndex * (ledSections / 3.0));
                source += " (sector fallback)";
                return Clamp(approxSection, 0, ledSections - 1);
            }

            source = "fallback section 0";
            return 0;
        }

        private static double NormalizeProgress(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            // Some games expose progress as 0..1, others as 0..100 percent.
            if (value > 1.0 && value <= 100.0)
            {
                value /= 100.0;
            }

            return Math.Max(0.0, Math.Min(0.999999, value));
        }

        private static int ConvertSectorToZeroBasedIndex(double sector)
        {
            int rounded = (int)Math.Floor(sector);
            if (rounded >= 1 && rounded <= 3)
            {
                return rounded - 1;
            }

            return Clamp(rounded, 0, 2);
        }

        private static char MapSessionToMode(string sessionText)
        {
            if (string.IsNullOrWhiteSpace(sessionText))
            {
                return 'P';
            }

            string lower = sessionText.ToLowerInvariant();
            if (lower.Contains("race"))
            {
                return 'R';
            }

            if (lower.Contains("qual") || lower.Contains("qualification"))
            {
                return 'Q';
            }

            return 'P';
        }

        private string ReadFirstString(PluginManager pluginManager, GameData data, IEnumerable<string> candidates, out string source)
        {
            foreach (string candidate in candidates)
            {
                if (TryGetValue(pluginManager, data, candidate, out object value) && value != null)
                {
                    string text = Convert.ToString(value, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        source = candidate;
                        return text;
                    }
                }
            }

            source = "fallback Practice";
            return null;
        }

        private bool TryReadFirstDouble(PluginManager pluginManager, GameData data, IEnumerable<string> candidates, out double number, out string source)
        {
            foreach (string candidate in candidates)
            {
                if (TryGetValue(pluginManager, data, candidate, out object value) && TryConvertDouble(value, out number))
                {
                    source = candidate;
                    return true;
                }
            }

            number = 0;
            source = "missing";
            return false;
        }

        private bool TryGetValue(PluginManager pluginManager, GameData data, string path, out object value)
        {
            value = null;

            if (TryGetByReflection(data, path, out value))
            {
                return true;
            }

            if (TryGetPluginManagerProperty(pluginManager, path, out value))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetByReflection(object root, string path, out object value)
        {
            value = null;
            object current = root;

            foreach (string segment in path.Split('.'))
            {
                if (current == null)
                {
                    return false;
                }

                Type type = current.GetType();
                PropertyInfo property = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    current = property.GetValue(current, null);
                    continue;
                }

                FieldInfo field = type.GetField(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    current = field.GetValue(current);
                    continue;
                }

                return false;
            }

            value = current;
            return value != null;
        }

        private static bool TryGetPluginManagerProperty(PluginManager pluginManager, string propertyName, out object value)
        {
            value = null;
            if (pluginManager == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            try
            {
                MethodInfo method = pluginManager.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "GetPropertyValue" && m.GetParameters().Length == 1);
                if (method == null)
                {
                    return false;
                }

                value = method.Invoke(pluginManager, new object[] { propertyName });
                return value != null;
            }
            catch
            {
                return false;
            }
        }

        private static void TrySetPluginProperty(PluginManager pluginManager, string propertyName, object value)
        {
            if (pluginManager == null)
            {
                return;
            }

            try
            {
                MethodInfo method = pluginManager.GetType().GetMethods()
                    .Where(m => m.Name == "SetPropertyValue")
                    .OrderBy(m => m.GetParameters().Length)
                    .FirstOrDefault();
                if (method == null)
                {
                    return;
                }

                int parameterCount = method.GetParameters().Length;
                if (parameterCount == 2)
                {
                    method.Invoke(pluginManager, new[] { propertyName, value });
                }
                else if (parameterCount == 3)
                {
                    method.Invoke(pluginManager, new[] { propertyName, typeof(NordschleifeLedPlugin), value });
                }
            }
            catch
            {
                // Debug property publishing is optional; ignore if this SimHub build exposes a different API.
            }
        }

        private static bool TryConvertDouble(object value, out double number)
        {
            if (value == null)
            {
                number = 0;
                return false;
            }

            if (value is double d)
            {
                number = d;
                return true;
            }

            if (value is float f)
            {
                number = f;
                return true;
            }

            if (value is int i)
            {
                number = i;
                return true;
            }

            if (value is long l)
            {
                number = l;
                return true;
            }

            return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private void Log(PluginManager pluginManager, string message)
        {
            string line = $"[{PluginName}] {message}";
            try
            {
                MethodInfo method = pluginManager?.GetType().GetMethods()
                    .FirstOrDefault(m => (m.Name == "Log" || m.Name == "AddLog" || m.Name == "Info") && m.GetParameters().Length == 1);
                if (method != null)
                {
                    method.Invoke(pluginManager, new object[] { line });
                    return;
                }
            }
            catch
            {
                // Fall back to Debug output below.
            }

            System.Diagnostics.Debug.WriteLine(line);
        }

        private static Settings LoadSettings(string path)
        {
            Settings settings = new Settings();

            try
            {
                if (File.Exists(path))
                {
                    foreach (string rawLine in File.ReadAllLines(path))
                    {
                        string line = rawLine.Trim().TrimEnd(',');
                        string[] parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length != 2)
                        {
                            continue;
                        }

                        string key = parts[0].Trim().Trim('\"');
                        string textValue = parts[1].Trim().Trim('\"');

                        if (key.Equals(nameof(Settings.ComPort), StringComparison.OrdinalIgnoreCase))
                        {
                            settings.ComPort = textValue;
                        }
                        else if (key.Equals(nameof(Settings.BaudRate), StringComparison.OrdinalIgnoreCase) && int.TryParse(textValue, out int baudRate))
                        {
                            settings.BaudRate = baudRate;
                        }
                        else if (key.Equals(nameof(Settings.LedSections), StringComparison.OrdinalIgnoreCase) && int.TryParse(textValue, out int ledSections))
                        {
                            settings.LedSections = ledSections;
                        }
                        else if (key.Equals(nameof(Settings.UpdateIntervalMs), StringComparison.OrdinalIgnoreCase) && int.TryParse(textValue, out int updateIntervalMs))
                        {
                            settings.UpdateIntervalMs = updateIntervalMs;
                        }
                        else if (key.Equals(nameof(Settings.ForceUpdate), StringComparison.OrdinalIgnoreCase) && bool.TryParse(textValue, out bool forceUpdate))
                        {
                            settings.ForceUpdate = forceUpdate;
                        }
                    }
                }
                else
                {
                    SaveSettings(path, settings);
                }
            }
            catch
            {
                // Use defaults if settings are malformed.
            }

            return settings;
        }

        private static void SaveSettings(string path, Settings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json =
                    "{\n" +
                    $"  \"{nameof(Settings.ComPort)}\": \"{settings.ComPort}\",\n" +
                    $"  \"{nameof(Settings.BaudRate)}\": {settings.BaudRate},\n" +
                    $"  \"{nameof(Settings.LedSections)}\": {settings.LedSections},\n" +
                    $"  \"{nameof(Settings.UpdateIntervalMs)}\": {settings.UpdateIntervalMs},\n" +
                    $"  \"{nameof(Settings.ForceUpdate)}\": {settings.ForceUpdate.ToString().ToLowerInvariant()}\n" +
                    "}\n";
                File.WriteAllText(path, json);
            }
            catch
            {
                // SimHub may run under restricted folders; settings persistence is helpful but not fatal.
            }
        }

        private static string BuildSettingsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "SimHub", "NordschleifeLedPlugin", "settings.json");
        }

        private static void NormalizeSettings(Settings settings)
        {
            settings.BaudRate = settings.BaudRate <= 0 ? DefaultBaudRate : settings.BaudRate;
            settings.LedSections = Clamp(settings.LedSections <= 0 ? DefaultSections : settings.LedSections, 1, 64);
            settings.UpdateIntervalMs = Clamp(settings.UpdateIntervalMs <= 0 ? DefaultUpdateIntervalMs : settings.UpdateIntervalMs, 10, 5000);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        public class Settings
        {
            public string ComPort { get; set; } = "COM3";
            public int BaudRate { get; set; } = DefaultBaudRate;
            public int LedSections { get; set; } = DefaultSections;
            public int UpdateIntervalMs { get; set; } = DefaultUpdateIntervalMs;
            public bool ForceUpdate { get; set; } = false;
        }
    }
}
