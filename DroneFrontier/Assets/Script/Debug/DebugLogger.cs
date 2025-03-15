using System;
using System.IO;
using System.Text;
using UnityEngine;

public class DebugLogger
{
    private const string LOG_FOLODER = "Log";
    private const string LOG_FILE_NAME = "debug.log";
    private static readonly Encoding LOG_ENCODING = Encoding.UTF8;
    private static object _lock = new object();

    public static void OutLog(string message)
    {
        lock (_lock)
        {
            if (!Directory.Exists(LOG_FOLODER))
            {
                Directory.CreateDirectory(LOG_FOLODER);
            }

            using (StreamWriter writer = new StreamWriter(Path.Combine(LOG_FOLODER, LOG_FILE_NAME), true, LOG_ENCODING))
            {
                string msg = $"{DateTime.Now} {message}";
                writer.WriteLine(msg);
                Debug.Log(msg);
            }
        }
    }
}
