
using HasoVM.Core;
using System;

namespace HasoVM.CLI
{
    class Program
    {

        static string ASCII = @"
██╗  ██╗ █████╗ ███████╗ ██████╗ ██╗   ██╗███╗   ███╗
██║  ██║██╔══██╗██╔════╝██╔═══██╗██║   ██║████╗ ████║
███████║███████║███████╗██║   ██║██║   ██║██╔████╔██║
██╔══██║██╔══██║╚════██║██║   ██║╚██╗ ██╔╝██║╚██╔╝██║
██║  ██║██║  ██║███████║╚██████╔╝ ╚████╔╝ ██║ ╚═╝ ██║
╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝ ╚═════╝   ╚═══╝  ╚═╝     ╚═╝
                                                     
";
        [STAThread]
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(ASCII);       
            Context.Main(args);
        }
    }
}
