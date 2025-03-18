using System;
using System.Windows;
using Python.Runtime;

namespace LiveSubtitlesApp
{
    public partial class MainWindow : Window
    {
        private dynamic _recognizer;
        private bool _isRecognizing;

        public MainWindow()
        {
            InitializeComponent();
            InitializeVosk();
        }

        // Инициализация Vosk
        private void InitializeVosk()
        {
            // Укажите путь к Python и модели Vosk
            string pythonPath = @"C:\Python39"; // Замените на ваш путь к Python
            string modelPath = @"vosk-model-ru-0.42"; // Укажите путь к новой модели

            Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath);
            PythonEngine.Initialize();

            using (Py.GIL()) // Глобальная блокировка интерпретатора Python
            {
                dynamic vosk = Py.Import("vosk");
                dynamic model = vosk.Model(modelPath);
                _recognizer = vosk.KaldiRecognizer(model, 16000.0);
            }
        }

        // Обработчик нажатия на кнопку "Start"
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _isRecognizing = true;
            StartRecognition();
            SubtitlesTextBlock.Text = "Слушаю...";
        }

        // Обработчик нажатия на кнопку "Stop"
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isRecognizing = false;
            SubtitlesTextBlock.Text = "Распознавание остановлено.";
        }

        // Запуск распознавания
        private void StartRecognition()
        {
            using (Py.GIL())
            {
                dynamic pyaudio = Py.Import("pyaudio");
                dynamic audio = pyaudio.PyAudio();
                dynamic stream = audio.open(
                    format: pyaudio.paInt16,
                    channels: 1,
                    rate: 16000,
                    input: true,
                    frames_per_buffer: 8192
                );

                while (_isRecognizing)
                {
                    byte[] data = stream.read(8192);
                    if (_recognizer.AcceptWaveform(data))
                    {
                        dynamic result = _recognizer.Result();
                        string text = result["text"];
                        Dispatcher.Invoke(() => SubtitlesTextBlock.Text = text);
                    }
                }

                stream.stop_stream();
                stream.close();
                audio.terminate();
            }
        }
    }
}