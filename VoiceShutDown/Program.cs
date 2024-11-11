using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Threading;

class Program
{
    private static SpeechRecognitionEngine recognizer;
    private static Dictionary<string, Tuple<DateTime, DateTime>> timeSlots = new Dictionary<string, Tuple<DateTime, DateTime>>();
    private static bool shutdownConfirmed = false;
    private static DateTime? shutdownTime = null;

    static void Main(string[] args)
    {
        // Initialize speech recognizer
        recognizer = new SpeechRecognitionEngine();
        recognizer.SetInputToDefaultAudioDevice();

        // Add grammar
        Choices commands = new Choices("关机", "强制关机", "确定关机", "取消关机");
        Grammar grammar = new Grammar(new GrammarBuilder(commands));
        recognizer.LoadGrammar(grammar);

        recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
        recognizer.RecognizeAsync(RecognizeMode.Multiple);

        // Define time slots
        timeSlots.Add("Slot1", new Tuple<DateTime, DateTime>(new DateTime(2024, 11, 11, 23, 0, 0), new DateTime(2024, 11, 11, 23, 59, 59)));
        timeSlots.Add("Slot2", new Tuple<DateTime, DateTime>(new DateTime(2024, 11, 11, 9, 0, 0), new DateTime(2024, 11, 11, 10, 0, 0)));

        Console.WriteLine("Listening for commands...");
        Console.ReadLine();
    }

    private static void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        string command = e.Result.Text;

        if (command == "关机" && IsWithinTimeSlot())
        {
            Console.WriteLine("Detected '关机', please confirm within 5 seconds...");
            shutdownTime = DateTime.Now.AddSeconds(5);
            shutdownConfirmed = false;

            Timer timer = new Timer(CheckShutdownConfirmation, null, 5000, Timeout.Infinite);
        }
        else if (command == "强制关机")
        {
            Console.WriteLine("Detected '强制关机', please say '确定关机' to confirm.");
            shutdownConfirmed = false;
        }
        else if (command == "确定关机")
        {
            if (shutdownTime != null && DateTime.Now <= shutdownTime)
            {
                Console.WriteLine("Shutdown confirmed, the system will shut down now.");
                ExecuteShutdown();
            }
            else
            {
                Console.WriteLine("Shutdown confirmation timeout or not within valid window.");
            }
        }
        else if (command == "取消关机")
        {
            Console.WriteLine("Shutdown cancelled.");
            shutdownConfirmed = false;
            shutdownTime = null;
        }
    }

    private static bool IsWithinTimeSlot()
    {
        DateTime now = DateTime.Now;
        foreach (var timeSlot in timeSlots.Values)
        {
            if (now >= timeSlot.Item1 && now <= timeSlot.Item2)
                return true;
        }
        return false;
    }

    private static void CheckShutdownConfirmation(object state)
    {
        if (!shutdownConfirmed && shutdownTime.HasValue && DateTime.Now > shutdownTime.Value)
        {
            Console.WriteLine("Shutdown confirmation failed, no action taken.");
            shutdownTime = null;
        }
    }

    private static void ExecuteShutdown()
    {
        Console.WriteLine("The system will shut down...");
        // System shutdown command (e.g., PowerShell or shutdown.exe)
        System.Diagnostics.Process.Start("shutdown", "/s /f /t 0");
    }
}