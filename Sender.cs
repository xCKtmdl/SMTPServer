using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Common.SMTP;
namespace SMTPServer
{
    public class Sender
    {
        static Boolean sendMail(string server, string from, string to, List<string> lines, SMTPMessage message)
        {
            try
            {

                TcpClient client = new TcpClient(server, 25);
                NetworkStream stream = client.GetStream();
                System.IO.StreamReader reader = new System.IO.StreamReader(stream);
                System.IO.StreamWriter writer = new System.IO.StreamWriter(stream);
                message.Data.Add(reader.ReadLine());
                OutputLine(writer, message, "HELO " + System.Net.Dns.GetHostName());
                message.Data.Add(reader.ReadLine());
                OutputLine(writer, message, from);
                message.Data.Add(reader.ReadLine());
                OutputLine(writer, message, to);
                message.Data.Add(reader.ReadLine());
                OutputLine(writer, message, "DATA");
                message.Data.Add(reader.ReadLine());
                foreach (var line in lines)
                    OutputLine(writer, message, line);
                message.Data.Add(reader.ReadLine());
                OutputLine(writer, message, "QUIT");
                string quitMessage = reader.ReadLine();
                message.Data.Add(quitMessage);
                return quitMessage.StartsWith("221");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                message.Data.Add(ex.ToString());
                return false;
            }
        }
        private static void OutputLine(StreamWriter writer, SMTPMessage log, string p)
        {
            writer.WriteLine(p);
            log.Data.Add(p);
            writer.Flush();
        }

        internal static void Send(string externalMail, List<string> Data, string MAIL_FROM)
        {
            Common.DNS.DnsMx dns = new Common.DNS.DnsMx();
            SMTPMessage message = new SMTPMessage();
            message.From = "SMTP administrator";
            message.To.Add(MAIL_FROM);
            bool sent = false;
            try
            {
                string domain = externalMail.Split('@')[1];
                string[] servers = dns.resolve(domain);
                foreach (var serverName in servers)
                {
                    if (!sendMail(serverName, MAIL_FROM, externalMail, Data, message))
                        continue;
                    sent = true;
                    break;
                }
            }
            catch (Exception e)
            {
                message.Data.Add("Error: " + e.ToString());
                message.Data.Add("Message: " + e.Message);
                message.Data.Add("Source: " + e.Source);
                message.Data.Add("StackTrace: " + e.StackTrace);
                sent = false;
            }
            if (!sent)
                message.Save();
        }
    }
}