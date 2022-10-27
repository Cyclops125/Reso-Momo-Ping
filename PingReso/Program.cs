using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mail;
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using System.Net.Http;
using System.IO;
using System;
using System.Net.NetworkInformation;
using PingReso;

namespace PingReso
{
    public class Program
    {
        public static AppSettings appSettings { get; set; }

        public static AppSettings ReadAppSettings()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                + @"appsettings.json");

            using (StreamReader sr = new StreamReader(path))
            {
                var json = sr.ReadToEnd();
                appSettings = JsonConvert.DeserializeObject<AppSettings>(json);

            }
            return appSettings;
        }

        static int SleepsAfterPingAllDomain()
        {           
            int sleeps = ReadAppSettings().SleepsAfterPingAllDomain;
            return sleeps;
        }

        static int SleepBetweenPings()
        {            
            int sleeps = ReadAppSettings().SleepBetweenPings;
            return sleeps;
        }


        static void SendEmail(string ErrorLink, DateTime time)
        {
            string ReceiveMail = ReadAppSettings().ReceiveEmail;
            string SendEmail = ReadAppSettings().SendEmail;
            string SendEmailPassword = ReadAppSettings().SendEmailPassword;

            string Body = "Server down at link: ";

            if (String.IsNullOrEmpty(ErrorLink))
                return;

            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(ReceiveMail);
                mail.From = new MailAddress(SendEmail);
                mail.Subject = "Server Down";
                mail.Body = Body + ErrorLink + " | " + time;

                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com"; //Or Your SMTP Server Address
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(SendEmail, SendEmailPassword);
                smtp.Port = 587;

                //Or your Smtp Email ID and Password
                smtp.EnableSsl = true;
                smtp.Send(mail);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Error message will be sent to your email");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void CreateLog(List<string> ErrorList)
        {
            string logFolderName = "logs/";
            if (!Directory.Exists(logFolderName))
            {
                Directory.CreateDirectory(logFolderName);
            }
            string logFileName = "";
            DateTime now = DateTime.Now;
            logFileName = String.Format("{0}_{1}_{2}_log.txt", now.Day, now.Month, now.Year);
            string fullFileLog = Path.Combine(logFolderName, logFileName);

            using (StreamWriter sw = new StreamWriter(fullFileLog))
            {
                foreach (var err in ErrorList)//save error to 1 file
                {
                    sw.WriteLine(String.Format("Error occurs at: {0}", now));
                    sw.WriteLine(String.Format("Error: An exception occurred during a Ping request."));
                    sw.WriteLine(String.Format("Error URL: {0}", err));
                    sw.WriteLine();
                }
            }
        }

        static void Ping()
        {

            Ping p = new Ping();
            bool check = true;
            int MaxErrorCount = ReadAppSettings().MaxErrorCount;

            //Read Json file
            string domains = ReadAppSettings().Domains;       
            string[] convertArrayToSplit = domains.Split(",");//Convert to array to split
            List<string> domain = new List<string>(convertArrayToSplit);//Convert to list     
            Dictionary<string, int> DomainHashmap = domain.Distinct().ToDictionary(x => x, x => 0);//Convert to hashmap

            List<string> ErrorList = new List<string>();//list of error server
            for (; ; )
            {
                foreach (var domainItem in DomainHashmap)
                {
                    while (check)
                    {
                        if (DomainHashmap[domainItem.Key] < MaxErrorCount)
                        {
                            try
                            {
                                PingReply rep = p.Send(domainItem.Key, 1000);
                                if (rep.Status.ToString() == "Success")
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;

                                    Console.WriteLine("Reply from: " + rep.Address + "Bytes=" + rep.Buffer.Length + " Time=" +
                                        rep.RoundtripTime + " TTL=" + rep.Options.Ttl + " Routers=" + (128 - rep.Options.Ttl) + " Status=" +
                                        rep.Status + " Server" + domainItem);//Print normal server

                                }
                                check = false;
                                Thread.Sleep(SleepBetweenPings());
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;

                                DomainHashmap[domainItem.Key] = domainItem.Value + 1;//Error Server Count + 1
                                Console.WriteLine("Error:{0}, {1}", domainItem.Key, DomainHashmap[domainItem.Key]);
                                if (DomainHashmap[domainItem.Key] == MaxErrorCount)
                                {
                                    Console.WriteLine("Error at server: {0}", domainItem.Key);
                                    ErrorList.Add(domainItem.Key);//add error server to list for send mail                                                                               
                                    CreateLog(ErrorList);

                                    DateTime Time = DateAndTime.Now;
                                    string URL = domainItem.Key; //error at link                                        
                                    SendEmail(URL, Time);//send mail
                                }
                                Thread.Sleep(SleepBetweenPings());
                                check = false;
                            }
                        }
                        else
                        {
                            check = false;
                        }
                    }

                    check = true;
                }
                Thread.Sleep(SleepsAfterPingAllDomain());               
            }

        }
        static void Main(string[] args)
        {          
            Ping();
        }
    }
}





