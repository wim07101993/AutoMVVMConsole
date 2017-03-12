
using AutoConsole;

namespace TestConsoleApp
{
    internal static class Program
    {
        private static void Main()
        {
            var person = new PersonVM { Name = "Bart", Length = 5.42, Weight = 54 };
            var console = new ConsoleBase { DataContext = person };
            console.AskQuestion();
        }
    }
}
