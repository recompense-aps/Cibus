using System;
namespace Cibus.Backend
{
	class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MainAsync().GetAwaiter().GetResult();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadKey();
        }

        static async Task MainAsync()
        {
            CibusConsole.Init();

            await CommandProcessor
                .Create()
                .WithVerboseErrors()
                .Prompt("Cibus> ")
                .Input(() => Console.ReadLine()?.Trim() ?? "")
                .Quit(input => input == "quit")
                .After(input => Console.WriteLine())
                .Process();
        }
    }
}
