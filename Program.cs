using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PhotoDetection
{
    internal class Program
    {
        public static readonly string FAQ =
            $"How to:{Environment.NewLine}" +
            $"\tFirst argument sets directory for photos{Environment.NewLine}" +
            $"\tSecond argument sets pin number for PIR sensor (12 by default){Environment.NewLine}" +
            $"\tThord argument sets pin number for LED (10 by default){Environment.NewLine}" +
            $"\tForth argument sets PIR sensor delay time for in milliseconds (1000 by default){Environment.NewLine}" +
            $"{Environment.NewLine}\tTo use default value type dash symbol as appropriate parameter{Environment.NewLine}";

        private static int GreenLedPin = 10;
        private static int PirPin = 12;
        private static int DelayInMilliseconds = 1000;
        private static bool KeepRunning = true;
        private static GpioController gpioController = new GpioController();

        private static readonly int IterationForPhoto = 5;
        private static string ShotCommandFormat = "fswebcam -r 640*480 -> {0}";
        private static string ShotFileNameFormat = "photobox_{0}.jpg";
        private static string PhotoDirectory = "/home/dkor/photobox";

        /// <summary>
        /// Проверка запроса помощи
        /// </summary>
        private static bool HelpRequired(string param)
        {
            return param == "-h" || param == "--help" || param == "/?";
        }

        /// <summary>
        /// Обработка аргументов приложения
        /// </summary>
        private static bool HandleArguments(string[] args)
        {
            var handleResult = true;

            if (args.Length > 0)
            {
                if (HelpRequired(args[0]))
                {
                    Console.WriteLine(FAQ);
                    handleResult = false;
                }

                if (args[0] != "-")
                {
                    PhotoDirectory = args[0];
                }

                if (int.TryParse(args[0], out var pirPin))
                {
                    PirPin = pirPin;
                }

                if (args.Length > 1 && int.TryParse(args[1], out var ledPin))
                {
                    GreenLedPin = ledPin;
                }

                if (args.Length > 2 && int.TryParse(args[2], out var delayTime))
                {
                    DelayInMilliseconds = delayTime;
                }
            }

            return handleResult;
        }

        /// <summary>
        /// Метод фотографирования
        /// </summary>
        private static void TakePhoto()
        {
            var fileName = string.Format(ShotFileNameFormat, $"{DateTime.Now:ddMMyyyyHHmmss}");
            var fullFileName = Path.Combine(PhotoDirectory, fileName);
            var startInfo = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{string.Format(ShotCommandFormat, fullFileName)}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = new Process()
            {
                StartInfo = startInfo,
            };
            process.Start();
            process.WaitForExit();
        }

        private static void Main(string[] args)
        {
            if (HandleArguments(args))
            {
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    KeepRunning = false;
                };

                if (!Directory.Exists(PhotoDirectory))
                {
                    Directory.CreateDirectory(PhotoDirectory);
                }

                gpioController.OpenPin(GreenLedPin, PinMode.Output);
                gpioController.OpenPin(PirPin, PinMode.Input);

                var currentMotionDetectedIteration = 0;

                while (KeepRunning)
                {
                    var motionStatus = gpioController.Read(PirPin);
                    if (motionStatus == PinValue.Low)
                    {
                        Console.WriteLine("All clear here...");
                        gpioController.Write(GreenLedPin, PinValue.Low);
                        currentMotionDetectedIteration = 0;
                    }
                    else
                    {
                        Console.WriteLine("Motion detected! Taking a photo...");
                        gpioController.Write(GreenLedPin, PinValue.High);
                        currentMotionDetectedIteration++;
                        // Делаем фото на каждое пятое срабатывание датчика
                        if (IterationForPhoto % currentMotionDetectedIteration == 0)
                        {
                            TakePhoto();
                        }
                    }
                    Thread.Sleep(DelayInMilliseconds);
                }

                gpioController.Write(GreenLedPin, PinValue.Low);
                gpioController.ClosePin(GreenLedPin);
                gpioController.ClosePin(PirPin);
                Console.WriteLine($"{Environment.NewLine}Exited");
            }
        }
    }
}
