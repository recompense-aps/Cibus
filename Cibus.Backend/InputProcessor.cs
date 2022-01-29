using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Cibus.Backend
{
    public class InputProcessor
    {
        public static (bool shouldNotRun, InputProcessor processor) Process(string commandName, string raw)
        {
            var processor = new InputProcessor(raw);

            bool shouldNotRun = processor.Parts.First() != commandName;

            return (shouldNotRun, processor);
        }

        public string[] Parts { get; private set; }
        public string[] Arguments { get; private set; }
        public string CommandName { get; private set; }

        public InputProcessor(string raw)
        {
            Parts = raw.Split(' ');
            Arguments = Parts.Skip(1).ToArray();
            CommandName = Parts[0];
        }

        public T? GetArg<T>(int index) where T : class
        {
            if (index > Arguments.Length - 1) return null;
            return Arguments[index] as T;
        }

        public int? GetArgInt(int index)
        {
            if (index > Arguments.Length - 1) return null;
            return int.Parse(Arguments[index]);
        }

        public long? GetArgLong(int index)
        {
            if (index > Arguments.Length - 1) return null;
            return long.Parse(Arguments[index]);
        }

        public bool Help(string description, params (string arg, string desc, bool optional)[] options)
        {
            if (Arguments.Contains("-h") || Arguments.Contains("-help"))
            {
                CibusConsole.Log($"-{CommandName}-", ConsoleColor.Green);
                CibusConsole.Log(description);

                foreach(var item in options)
                {
                    CibusConsole.Log($"-{item.arg}\t{(item.optional ? "" : "Required.")} {item.desc}");
                }

                return false;
            }

            return true;
        }

        public ProcessedSwitch Switch(string switchId)
        {
            int index = Arguments.ToList().IndexOf("-" + switchId);

            if (index >= 0 && index + 1 < Arguments.Length)
                return new ProcessedSwitch(Arguments[index + 1]);

            return new ProcessedSwitch(null, false);
        }
    }

    public class ProcessedSwitch
    {
        public ProcessedSwitch(object value, bool switchPresent = true) { SwitchValue = value; SwitchPresent = switchPresent; }
        public bool SwitchPresent { get; private set; }
        public object? SwitchValue { get; private set; }
        public object? DefaultValue { get; private set; }
        public object? ComputedValue => SwitchValue ?? DefaultValue ?? throw new Exception("Tried to get computed value, but only nulls found");
        public int Int => int.Parse(ComputedValue.ToString());
        public long Long => long.Parse(ComputedValue.ToString());
        public float Float => float.Parse(ComputedValue.ToString());
        public double Double => double.Parse(ComputedValue.ToString());
        public decimal Decimal => decimal.Parse(ComputedValue.ToString());
        public string String => ComputedValue.ToString();
        public string File => ReadFile(ComputedValue.ToString());
        public bool Bool => SwitchPresent && String != "false";
        public T Get<T>() where T:class
        {
            return SwitchValue as T;
        }
        public ProcessedSwitch Default(object value)
        {
            DefaultValue = value;
            return this;
        }

        private string ReadFile(string path)
        {
            StreamReader r = new StreamReader(path);
            string contents = r.ReadToEnd();
            r.Close();
            return contents;
        }
    }
}
