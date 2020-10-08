using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace BlockingApp
{
    class Program
    {
        const string TemplateFile = "template.tbl";
        static readonly PlatformID[] WindowsPlatformIDs = new PlatformID[]{
                PlatformID.Win32S,
                PlatformID.Win32Windows,
                PlatformID.Win32NT,
                PlatformID.WinCE};
        static List<string> Template { get; set; }
        static void Main(string[] args)
        {
            if (!File.Exists(TemplateFile))
            {
                if (!CreateTable()) return;
            }
            Console.WriteLine("Please, enter the password");
            var password = Console.ReadLine();
            Free(TemplateFile);
            Template = File.ReadAllLines(TemplateFile).ToList();
            Protect(TemplateFile);
            using (var hashf = SHA256.Create())
            {
                var hashp = hashf.ComputeHash(Encoding.Default.GetBytes(password));
                if (Template[0] == Encoding.Default.GetString(hashp))
                {
                    Console.WriteLine("Access granted");
                }
                else
                {
                    Console.WriteLine("Access denied");
                    return;
                }
            }
            MainMenu();
        }
        static bool CreateTable()
        {
            Console.WriteLine($"File {TemplateFile} is not found in current directory");
            Console.WriteLine("Do you want to create? (y/n)");
            while (true)
            {
                var t = Console.ReadLine().ToLower();
                if (t == "y")
                {
                    try
                    {
                        File.Create(TemplateFile).Dispose();
                        Console.WriteLine($"File {TemplateFile} was created");
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else if (t == "n")
                {
                    Console.WriteLine("Understandable. Have a nice day!");
                    return false;
                }
            }
            Console.WriteLine("Please, enter new password");
            var password = Console.ReadLine();
            Console.WriteLine("Please, enter file names to protect");
            var files = new List<string>();
            while (true)
            {
                var t = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(t)) break;
                files.Add(t);
            }
            using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
            {
                using (var hashf = SHA256.Create())
                    file.WriteLine(Encoding.Default.GetString(hashf.ComputeHash(Encoding.Default.GetBytes(password))));
                file.WriteLine(0);
                foreach (var f in files)
                {
                    file.WriteLine(f);
                }
            }
            Protect(TemplateFile);
            return true;
        }
        static void MainMenu()
        {
            while (true)
            {
                Console.Clear();
                var active = Template[1] == "1";
                PrintMenu(active);
                var t = Console.ReadLine();
                switch (t)
                {
                    case "0":
                        return;
                    case "1":
                        if (active) Deactivate();
                        else Activate();
                        break;
                    case "2":
                        ChangeTable();
                        break;
                    case "3":
                        ChangeTable(true);
                        break;
                    case "4":
                        ChangePassword();
                        break;
                }
            }
        }
        static void PrintMenu(bool active)
        {
            if (active)
            {
                Console.WriteLine($"Protection is active.");
                Console.WriteLine("1. Deactivate protection");
            }
            else
            {
                Console.WriteLine($"Protection is inactive.");
                Console.WriteLine("1. Activate protection");
            }
            Console.WriteLine("2. Change Protection List");
            Console.WriteLine("3. Delete Protection List");
            Console.WriteLine("4. Change password");
            Console.WriteLine("0. Exit");
        }
        static void Activate()
        {
            for (int i = 2; i < Template.Count; i++)
            {
                if (!File.Exists(Template[i])) continue;
                Protect(Template[i]);
            }
            Template[1] = "1";
            Free(TemplateFile);
            using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
            {
                file.WriteLine(Template[0]);
                file.WriteLine(Template[1]);
            }
            Protect(TemplateFile);
        }
        static void Deactivate()
        {
            for (int i = 2; i < Template.Count; i++)
            {
                if (!File.Exists(Template[i])) continue;
                Free(Template[i]);
            }
            Template[1] = "0";
            Free(TemplateFile);
            using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
            {
                file.WriteLine(Template[0]);
                file.WriteLine(Template[1]);
            }
            Protect(TemplateFile);
        }
        static void Protect(string path)
        {
            var t = File.GetAccessControl(path);
            foreach (FileSystemAccessRule r in t.GetAccessRules(true, true, typeof(NTAccount)))
            {
                t.AddAccessRule(
                    new FileSystemAccessRule(r.IdentityReference,
                                             FileSystemRights.FullControl,
                                             AccessControlType.Deny));
            }
            File.SetAccessControl(path, t);
        }
        static void Free(string path)
        {
            var t = File.GetAccessControl(path);
            foreach (FileSystemAccessRule r in t.GetAccessRules(true, true, typeof(NTAccount)))
            {
                t.RemoveAccessRule(r);
            }
            File.SetAccessControl(path, t);
        }
        static void ChangeTable(bool delete = false)
        {
            if (delete)
            {
                File.Delete(TemplateFile);
                Process.Start(Assembly.GetExecutingAssembly().Location);
                Environment.Exit(0);
            }
            var fileCopy = "." + TemplateFile;
            using (var t = File.CreateText(fileCopy))
            {
                for (int i = 2; i < Template.Count; i++)
                {
                    t.WriteLine(Template[i]);
                }
                t.Flush();
            }
            Process p;
            if (WindowsPlatformIDs.Contains(System.Environment.OSVersion.Platform)) p = Process.Start("notepad.exe", fileCopy);
            else p = Process.Start("nano", fileCopy); //вроде так?
            p.WaitForExit();
            if (File.Exists(fileCopy))
            {
                var t = File.ReadLines(fileCopy);
                Template = Template.GetRange(0, 2).Concat(t).ToList();
                Free(TemplateFile);
                File.WriteAllLines(TemplateFile, Template);
                Protect(TemplateFile);
                File.Delete(fileCopy);
            }
        }
        static void ChangePassword()
        {
            Console.WriteLine("Enter old password");
            var password = Console.ReadLine();
            using (var hashf = SHA256.Create())
            {
                var hashp = hashf.ComputeHash(Encoding.Default.GetBytes(password));
                if (Template[0] == Encoding.Default.GetString(hashp))
                {
                    Console.WriteLine("Correct");
                    Console.WriteLine("Enter new password");
                    var newPassword = Console.ReadLine();
                    Template[0] = Encoding.Default.GetString(hashf.ComputeHash(Encoding.Default.GetBytes(newPassword)));
                    Free(TemplateFile);
                    using (var file = new StreamWriter(File.OpenWrite(TemplateFile)))
                    {
                        file.WriteLine(Template[0]);
                    }
                    Protect(TemplateFile);
                }
                else
                {
                    Console.WriteLine("Password is incorrect");
                    Console.ReadLine();
                }
            }
        }
    }
}
