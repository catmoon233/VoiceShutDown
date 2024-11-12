using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Diagnostics;
using System.Threading;

class Program
{
    private static List<(TimeSpan, TimeSpan)> _allowedShutdownTimes = new List<(TimeSpan, TimeSpan)>
    {
        (new TimeSpan(23, 0, 0), new TimeSpan(23, 59, 59)) // 23:00 - 23:59
        // 添加更多时间段
    };

    private static bool _forceShutdownPending = false;
    private static Timer _forceShutdownTimer;

    static void Main(string[] args)
    {
        using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine())
        {
            Choices commands = new Choices();
            commands.Add(new string[] { "关机", "强制关机", "添加时间段", "取消关机", "确定关机" });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(commands);

            Grammar g = new Grammar(gb);
            recognizer.LoadGrammar(g);

            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            recognizer.SetInputToDefaultAudioDevice();

            Console.WriteLine("语音识别启动，等待指令...");
            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            Console.ReadLine();
        }
    }

    private static void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        Console.WriteLine($"识别到指令: {e.Result.Text}");
        switch (e.Result.Text)
        {
            case "关机":
                HandleShutdown();
                break;
            case "强制关机":
                HandleForceShutdownRequest();
                break;
            case "确定关机":
                ConfirmForceShutdown();
                break;
            case "添加时间段":
                AddAllowedTimePeriod(new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)); // 示例时间段
                break;
            case "取消关机":
                CancelShutdown();
                break;
        }
    }

    private static void HandleShutdown()
    {
        TimeSpan currentTime = DateTime.Now.TimeOfDay;
        foreach (var period in _allowedShutdownTimes)
        {
            if (currentTime >= period.Item1 && currentTime <= period.Item2)
            {
                Console.WriteLine("系统将在5秒后关机...");
                Process.Start("shutdown", "/s /t 5");
                return;
            }
        }
        Console.WriteLine("当前时间不在允许的关机时间段内。");
    }

    private static void HandleForceShutdownRequest()
    {
        _forceShutdownPending = true;
        Console.WriteLine("请在5秒内确认强制关机，通过说 '确定关机'。");

        _forceShutdownTimer = new Timer((state) =>
        {
            _forceShutdownPending = false;
            Console.WriteLine("强制关机确认超时。");
        }, null, 5000, Timeout.Infinite);
    }

    private static void ConfirmForceShutdown()
    {
        if (_forceShutdownPending)
        {
            Console.WriteLine("系统将在5秒后强制关机...");
            Process.Start("shutdown", "/s /t 5 /f");
            _forceShutdownPending = false;
            _forceShutdownTimer?.Dispose();
        }
        else
        {
            Console.WriteLine("没有待确认的强制关机请求或已超时。");
        }
    }

    private static void AddAllowedTimePeriod(TimeSpan start, TimeSpan end)
    {
        _allowedShutdownTimes.Add((start, end));
        Console.WriteLine($"添加允许关机时间段: {start} - {end}");
    }

    private static void CancelShutdown()
    {
        Console.WriteLine("取消关机...");
        Process.Start("shutdown", "/a");
    }
}