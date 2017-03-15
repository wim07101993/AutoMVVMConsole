
using AutoConsole;

namespace TestConsoleApp
{
    internal static class Program
    {
        private static void Main()
        {
            var person = new PersonVM { Name = "Bart", Length = 1.80, Weight = 83.2 };
            var console = new ConsoleBase { DataContext = person };
            console.AskQuestion();
        }
    }
}
