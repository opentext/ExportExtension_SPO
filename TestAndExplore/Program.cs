using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAndExplore
{
    class Program
    {
        public static string server = "10.50.60.70";

        static void Main(string[] args)
        {
            DirTest();
            //GetLibraries();
            //Logon();
            //ReadSomeFolder();
            Console.Read();
        }

        static void DirTest()
        {
            DirectoryInfo di = new DirectoryInfo(@"D:\");
            var x = di.GetDirectories();
        }

        static void GetLibraries()
        {
            List<string> libraries = new List<string>();
            // Read the libraries
            foreach (string lib in libraries)
                Console.WriteLine(lib);
        }
    }
}
