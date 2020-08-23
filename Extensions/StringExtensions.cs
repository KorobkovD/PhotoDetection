using System.Diagnostics;

namespace PhotoDetection.Extensions
{
    /// <summary>
    /// Расширения для строк
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Выполнение bash-команды
        /// </summary>
        /// <param name="cmd">Команда для выполнения</param>
        public static void Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}
