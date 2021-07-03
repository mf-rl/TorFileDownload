using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Net.Sockets;
using System.Configuration;
using System.Collections.Generic;
using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;

namespace TorFileDownload
{
    public class MainProcess
    {
        string GetInput(string messageTitle, string inputRequired, string currentValue)
        {
            Console.WriteLine(messageTitle);
            return (
                (string.IsNullOrEmpty(inputRequired) ? Path.GetFullPath(Console.ReadLine()) :
                    (Console.ReadLine() == inputRequired ? inputRequired : currentValue)
                )
            );
        }
        public void Execute()
        {
            string destination = ConfigurationManager.AppSettings[Constants.DESTINATION_PATH];
            string base_uri = ConfigurationManager.AppSettings[Constants.BASE_URI];
            void writeLines(List<string> messageList)
            {
                messageList.ForEach(message => { Console.WriteLine(message); });
            }
            Functional.ExecuteAction(
                () =>
                {
                    writeLines(new List<string> {
                            string.Concat(Message.DEFAULT_PATH, destination)
                        });
                    destination = GetInput(Message.SET_CUSTOM_PATH, string.Empty, string.Empty);
                },
                (Exception) =>
                {
                    writeLines(new List<string> {
                            Message.ERROR_SET_CUSTOM_PATH,
                            string.Concat(Message.DEFAULT_PATH, destination)
                        });
                }
                );
            Functional.ExecuteAction(
                () =>
                {
                    base_uri = GetInput(Message.SET_CUSTOM_URI, string.Empty, string.Empty);
                },
                (Exception) =>
                {
                    writeLines(new List<string> {
                            Message.ERROR_SET_CUSTOM_URI,
                            string.Concat(Message.DEFAULT_URI, base_uri)
                        });
                }
                );
            StartProcess(
                () =>
                {
                    Console.WriteLine(OpenTorConnection(
                    () =>
                    {
                        ExecuteDownload(base_uri, destination);
                    }
                ));
                });
        }
        private string OpenTorConnection(Action executeAction)
        {
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Connect(new IPEndPoint(IPAddress.Parse(Constants.DEFAULT_HOST), 9151));
            server.Send(Encoding.ASCII.GetBytes("AUTHENTICATE \"" + Constants.AUTH_PASSWORD + "\"" + Environment.NewLine));
            _ = server.Receive(new byte[1024]);
            server.Send(Encoding.ASCII.GetBytes("SIGNAL NEWNYM" + Environment.NewLine));
            byte[] data = new byte[1024];
            if (!Encoding.ASCII.GetString(data, 0, server.Receive(data)).Contains("250"))
            {
                server.Shutdown(SocketShutdown.Both);
                server.Close();
                return Message.UNABLE_SIGNAL_USER_TO_SERVER;
            }
            else
                Console.WriteLine(Message.SIGNAL_SUCCESS);
            executeAction();
            server.Shutdown(SocketShutdown.Both);
            server.Close();
            return Message.PROCESS_ENDED_SUCCESSFULLY;
        }
        private void ExecuteDownload(string base_uri, string destination)
        {
            int file_index = 0;
            string reset = Constants.RETRY_INPUT;
            string retry = Constants.RETRY_INPUT;
            do
            {
                file_index = reset.Equals(Constants.RETRY_INPUT) ? 0 : file_index;
                int tries = 0;
                if (File.ReadLines(ConfigurationManager.AppSettings[Constants.BATCH_ORIGIN_FILE]).ElementAtOrDefault(0) == null)
                {
                    Console.WriteLine("No files to download.");
                    return;
                }
                var uri_d = File.ReadLines(ConfigurationManager.AppSettings[Constants.BATCH_ORIGIN_FILE])
                    .ToList().Where(f => f.StartsWith("http"));
                string first_uri = uri_d.FirstOrDefault();
                string directory_dest = first_uri.Replace(base_uri, "").Replace(Path.GetFileName(first_uri), "").Replace("/", "_");
                directory_dest = Path.Combine(destination, directory_dest);
                if (!Directory.Exists(directory_dest)) Directory.CreateDirectory(directory_dest);
                uri_d.ToList().ForEach(p =>
                {
                    if (tries < 3)
                    {
                        Console.WriteLine("Requesting page: " + p);
                        file_index++;
                        string uri_file_name = Path.GetFileName(p);
                        string fileName = string.Concat(Path.GetFileName(file_index.ToString().PadLeft(3, '0')), Path.GetExtension(uri_file_name).ToLower());
                        string destinationT = Path.Combine(directory_dest, fileName);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p);
                        request.Proxy = new SocksWebProxy(new ProxyConfig(IPAddress.Loopback, 8181, IPAddress.Loopback, 9150, ProxyConfig.SocksVersion.Five));
                        request.KeepAlive = false;
                        try
                        {
                            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            {
                                using (Stream responseStream = response.GetResponseStream())
                                {
                                    Image.FromStream(responseStream).Save(destinationT);
                                }
                                Console.WriteLine("Saved file: " + destinationT);
                            }
                        }
                        catch
                        {
                            tries++;
                            if (tries >= 3)
                            {
                                Console.WriteLine("Process ended due to maximum failed requests.");
                                return;
                            }
                        }
                    }
                });
                retry = GetInput(Message.CANCEL_CONDITION, Constants.NO, retry);
                retry = GetInput(Message.RESET_CONDITION, Constants.NO, reset);
            } while (retry.Equals(Constants.RETRY_INPUT));
        }
        private void StartProcess(Action executeAction)
        {
            using (Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Tor\\tor.exe",
                    Arguments = "-f .\\torrc",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = "Tor\\"
                }
            })
            {
                process.Start();
                executeAction();
                process.Kill();
            }
        }
    }
}
