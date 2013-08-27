using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace GreenQloud.UI.Setup
{
    public partial class Login
    {
        public Login()
        {
            Console.WriteLine("Wellcome to QloudSync");
            bool isConnected = false;
            do {
                Console.WriteLine("Before continue, put your username:");
                string username = Console.ReadLine();
                Console.WriteLine("Password:");
                string password = ReadPassword();
                try
                {
                    QloudSync.Repository.S3Connection.Authenticate(username, password);
                    Credential.Username = username;
                    Program.Controller.SyncStart();
                }
                catch (WebException)
                {
                    Console.WriteLine("An error ocurred while trying authenticate. Please, try again.");
                }
            } while (!isConnected);
        }

        public static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }
    }
}
