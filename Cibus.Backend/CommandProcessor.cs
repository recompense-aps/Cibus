using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cibus.Backend
{
    class CommandProcessor
    {
        private string? prompt;
        private bool verboseErrors = false;
        private Func<string>? input;
        private Func<string, bool>? quit;
        private Action<string>? after;
        private Func<string, bool>? ignore;

        public static CommandProcessor Create() { return new CommandProcessor(); }

        public CommandProcessor WithVerboseErrors()
        {
            verboseErrors = true;
            return this;
        }

        public CommandProcessor Prompt(string message)
        {
            prompt = message;
            return this;
        }

        public CommandProcessor Input(Func<string> handler)
        {
            input = handler;
            return this;
        }

        public CommandProcessor Quit(Func<string, bool> handler)
        {
            quit = handler;
            return this;
        }

        public CommandProcessor After(Action<string> handler)
        {
            after = handler;
            return this;
        }

        public CommandProcessor Ignore(Func<string, bool> handler)
        {
            ignore = handler;
            return this;
        }

        public async Task Process()
        {
            string text = "";
            while (!quit(text))
            {
                if (prompt != null) Console.Write(prompt);
                text = input();

                bool shouldIgnore = ignore?.Invoke(text) ?? false;

                if (!shouldIgnore)
                    await CibusConsole.ProcessCommands(text, verboseErrors);

                after?.Invoke(text);
            }
        }
    }
}
