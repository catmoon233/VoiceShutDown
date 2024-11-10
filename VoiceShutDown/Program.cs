using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Recognition;

class Program
{
    static void Main(string[] args)
    {
        // 创建一个识别引擎
        var recognizer = new SpeechRecognitionEngine();

        // 设置识别引擎的语言
        recognizer.SetInputToDefaultAudioDevice();
        recognizer.LoadGrammar(new DictationGrammar());

        // 定义命令列表
        List<string> shutdownCommands = new List<string> { "关机", "强制关机", "取消关机" };
        string confirmCommand = "确定关机";

        // 定义时间段
        List<TimeSpan[]> timeRanges = new List<TimeSpan[]>
        {
            new TimeSpan[] { new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0) },
            new TimeSpan[] { new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0) },
            new TimeSpan[] { new TimeSpan(21, 0, 0), new TimeSpan(22, 0, 0) },
            new TimeSpan[] { new TimeSpan(17, 0, 0), new TimeSpan(18, 0, 0) }
            // 可以继续添加更多时间段
        };

        // 添加命令识别事件
        recognizer.SpeechRecognized += (s, e) =>
        {
            if (shutdownCommands.Contains(e.Result.Text))
            {
                Console.WriteLine("命令收到: " + e.Result.Text);
                switch (e.Result.Text)
                {
                    case "关机":
                        ScheduleShutdown(timeRanges, 5); // 5秒后关机
                        break;
                    case "强制关机":
                        Console.WriteLine("请说'确定关机'来确认操作。");
                        DateTime startTime = DateTime.Now;
                        while ((DateTime.Now - startTime).TotalSeconds < 5)
                        {
                            if (recognizer.Recognize() != null && recognizer.Recognize().Text == confirmCommand)
                            {
                                Console.WriteLine("正在强制关机...");
                                Process.Start("shutdown", "/s /t 0");
                                return;
                            }
                        }
                        Console.WriteLine("超时未确认，取消操作。");
                        break;
                    case "取消关机":
                        CancelShutdown();
                        break;
                }
            }
        };

        // 启动识别
        recognizer.RecognizeAsync(RecognizeMode.Multiple);

        // 阻塞主线程以保持程序运行
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }

    static void ScheduleShutdown(List<TimeSpan[]> timeRanges, int delaySeconds)
    {
        // 获取当前时间
        DateTime now = DateTime.Now;

        // 检查当前时间是否在任何一个时间段内
        foreach (var range in timeRanges)
        {
            if (now.TimeOfDay >= range[0] && now.TimeOfDay < range[1])
            {
                Console.WriteLine($"计划在 {range[0]:hh\\:mm} 至 {range[1]:hh\\:mm} 之间关机...");

                // 设置关机时间为5秒后
                Process.Start("shutdown", $"/s /t {delaySeconds}");
                return;
            }
        }

        Console.WriteLine("不在指定时间内，不执行关机操作。");
    }

    static void CancelShutdown()
    {
        try
        {
            Process.Start("shutdown", "/a");
            Console.WriteLine("关机已取消。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"取消关机失败: {ex.Message}");
        }
    }
}