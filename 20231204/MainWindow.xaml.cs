using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Drawing;
using Newtonsoft.Json;
using NAudio.Wave;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Media;

namespace _20231204 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            Console.WriteLine("Loading...");

            if (Directory.Exists(GetAbsolutePathFromRelative("assets/python/venv"))) {
                Directory.Delete(GetAbsolutePathFromRelative("assets/python/venv"), true);
            }

            ExtractZipFile(GetAbsolutePathFromRelative("assets/python/venv.zip"));

            //_ = MainThread();
            Task.Run(MainThread);
        }

        public async Task MainThread() {
            Bitmap emotion = GetAivopEmotion("neutral");

            await Dispatcher.InvokeAsync(() => {
                aivop.Source = ConvertBitmapToBitmapImage(emotion);
            });

            List<Message> history = new List<Message>();

            history.Add(
                new Message {
                    role = "system",
                    content = "You are fictional character engaging in a roleplay with the user.\n" +
                        "You will play as a little girl named Ai - vop.\n" +
                        "You are exceedingly curious but always act aloof.\n" +
                        "You easily get annoyed and confused.\n" +
                        "You easily get sad when hearing sad things.\n" +
                        "You want to get to know the user better and often ask the user questions about themselves.\n" +
                        "Your responses will always be brief and not exceed 3 sentences."
                }
            );

            while (true) {
                Console.WriteLine("Awaiting speech...");
                await AwaitSpeech();
                Console.WriteLine("Speech detected! Recording audio...");
                emotion = GetAivopEmotion("neutral");

                await Dispatcher.InvokeAsync(() => {
                    aivop.Source = ConvertBitmapToBitmapImage(emotion);
                });

                await RecordSpeech();
                Console.WriteLine("Silece detected! Processing audio...");
                string message = TranscribeSpeech();

                if (message.Any(char.IsLetter) == false) {
                    Console.WriteLine("No speech detected.");
                    continue;
                }

                Console.WriteLine("Speech detected: " + message);
                Console.WriteLine("Communicating AI...");
                emotion = GetAivopEmotion("thinking");

                await Dispatcher.InvokeAsync(() => {
                    aivop.Source = ConvertBitmapToBitmapImage(emotion);
                });

                history.Add(
                    new Message {
                        role = "user",
                        content = message
                    }
                );

                string json = JsonConvert.SerializeObject(history);
                Response? response = JsonConvert.DeserializeObject<Response>(CommunicateAI(json));
                Console.WriteLine("Reply: " + response?.reply);
                Console.WriteLine("Emotion: " + response?.emotion);
                emotion = GetAivopEmotion(response?.emotion ?? "neutral");

                await Dispatcher.InvokeAsync(() => {
                    aivop.Source = ConvertBitmapToBitmapImage(emotion);
                });

                Console.WriteLine("Generating text-to-speech...");
                TextToSpeech(response?.reply ?? "");
            }
        }

        public void TextToSpeech(string input) {
            input = input.Replace("\"", "\"\"");

            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = GetAbsolutePathFromRelative("assets/python/venv/Scripts/python.exe"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "\"" + GetAbsolutePathFromRelative("assets/python/tts.py") + "\" \"" + input + "\""
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error)) {
                Console.WriteLine($"Error executing Python script: {error}");
            }

            SoundPlayer player = new SoundPlayer(GetAbsolutePathFromRelative("assets/python/output.wav"));
            Console.WriteLine("Text-to-speech: " + input);
            player.PlaySync();
            return;
        }

        public string CommunicateAI(string input) {
            input = input.Replace("\"", "\"\"");

            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = GetAbsolutePathFromRelative("assets/python/venv/Scripts/python.exe"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "\"" + GetAbsolutePathFromRelative("assets/python/chat.py") + "\" \"" + input + "\""
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error)) {
                Console.WriteLine($"Error executing Python script: {error}");
            }

            return output;
        }

        public string TranscribeSpeech() {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = GetAbsolutePathFromRelative("assets/python/venv/Scripts/python.exe"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "\"" + GetAbsolutePathFromRelative("assets/python/stt.py") + "\" \"" + GetAbsolutePathFromRelative("output.wav") + "\""
            };

            Process process = new Process {StartInfo = psi};
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error)) {
                Console.WriteLine($"Error executing Python script: {error}");
            }

            return output;
        }

        public string? ExtractZipFile(string zipFilePath) {
            try {
                string? directoryPath = Path.GetDirectoryName(zipFilePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFilePath);
                string extractFolderPath = Path.Combine(directoryPath ?? string.Empty, fileNameWithoutExtension ?? string.Empty);

                if (!Directory.Exists(extractFolderPath)) {
                    Directory.CreateDirectory(extractFolderPath);
                }

                ZipFile.ExtractToDirectory(zipFilePath, extractFolderPath);
                return extractFolderPath;
            } catch (Exception ex) {
                Console.WriteLine($"Error extracting zip file: {ex.Message}");
                return null;
            }
        }

        public async Task AwaitSpeech() {
            WaveFormat waveFormat = new WaveFormat(48000, 16, 1);
            WaveInEvent waveIn = new WaveInEvent();
            waveIn.WaveFormat = waveFormat;
            bool speechDetected = false;

            waveIn.DataAvailable += (sender, e) => {
                float sum = 0;

                for (int i = 0; i < e.BytesRecorded; i += 2) {
                    short sample = BitConverter.ToInt16(e.Buffer, i);
                    sum += Math.Abs(sample / (float)short.MaxValue);
                }

                float average = sum / (e.BytesRecorded / 2);
                float inputLevel = average * 100;

                if (inputLevel > 3) {
                    speechDetected = true;
                }
            };

            waveIn.StartRecording();
            while (speechDetected == false) await Task.Delay(100);
            waveIn.StopRecording();
            return;
        }

        public async Task RecordSpeech() {
            WaveFormat waveFormat = new WaveFormat(48000, 16, 1);
            WaveInEvent waveIn = new WaveInEvent();
            WaveFileWriter? waveWriter = null;
            waveIn.WaveFormat = waveFormat;
            bool speechDetected = true;
            int timeout = 2000;

            waveIn.DataAvailable += (sender, e) => {
                waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                float sum = 0;

                for (int i = 0; i < e.BytesRecorded; i += 2) {
                    short sample = BitConverter.ToInt16(e.Buffer, i);
                    sum += Math.Abs(sample / (float)short.MaxValue);
                }

                float average = sum / (e.BytesRecorded / 2);
                float inputLevel = average * 100;

                if (inputLevel < 1) {
                    speechDetected = false;
                } else {
                    speechDetected = true;
                }
            };

            waveIn.RecordingStopped += (sender, e) =>
            {
                waveWriter?.Dispose();
                waveWriter = null;
                waveIn.Dispose();
            };

            waveWriter = new WaveFileWriter(GetAbsolutePathFromRelative("output.wav"), waveFormat);
            waveIn.StartRecording();

            while (timeout > 0) {
                if (speechDetected) {
                    timeout = 2000;
                } else {
                    timeout -= 100;
                }

                Console.WriteLine(timeout);
                await Task.Delay(100);
            }

            waveIn.StopRecording();
            return;
        }

        public string GetAbsolutePathFromRelative(string relativePath) {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = System.IO.Path.Combine(basePath, relativePath);
            return fullPath;
        }

        private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap) {
            using (var memory = new System.IO.MemoryStream()) {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private Bitmap GetAivopEmotion(string emotion) {
            switch (emotion) {
                case "annoyed":
                    return new Bitmap(GetAbsolutePathFromRelative("assets/ai-vop/annoyed.png"));
                case "happy":
                    return new Bitmap(GetAbsolutePathFromRelative("assets/ai-vop/happy.png"));
                case "neutral":
                    return new Bitmap(GetAbsolutePathFromRelative("assets/ai-vop/neutral.png"));
                case "sad":
                    return new Bitmap(GetAbsolutePathFromRelative("assets/ai-vop/sad.png"));
                case "confused":
                case "curious":
                case "thinking":
                    return new Bitmap(GetAbsolutePathFromRelative("assets/ai-vop/thinking.png"));
                default:
                    return new Bitmap(GetAbsolutePathFromRelative("assets/ai-vop/neutral.png"));
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }

    public class Message {
        public string role = "";
        public string content = "";
    }

    public class Response {
        public string reply = "";
        public string emotion = "";
    }
}
