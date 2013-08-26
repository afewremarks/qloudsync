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
                string password = Console.ReadLine();
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
    }
}
