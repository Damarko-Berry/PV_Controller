using PVLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace PV_Controller
{
    static class SSDP
    {
        static string UniqueID = Guid.NewGuid().ToString();
        public static Dictionary<string, ControllerRef> Clients = new Dictionary<string, ControllerRef>();
        public static CancellationTokenSource cts = new CancellationTokenSource();
        public static async Task Scan()
        {
            SendSsdpAnnouncements(cts.Token);

            ListenForSsdpRequests(cts.Token);
            try
            {
                await Task.Delay(1000 * 60, cts.Token);
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            cts = new CancellationTokenSource();
        }

        static async Task SendSsdpAnnouncements(CancellationToken token)
        {
            string customSsdpNotifyTemplate = $@"M-SEARCH * HTTP/1.1
    HOST: 239.255.255.250:1900
    MAN: ""ssdp:discover""
    MX: 3
    ST: {SSDPTemplates.ClientSchema}";

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            UdpClient client = new UdpClient();

            byte[] customBuffer = Encoding.UTF8.GetBytes(customSsdpNotifyTemplate);
            while (!token.IsCancellationRequested)
            {
                client.Send(customBuffer, customBuffer.Length, endPoint);
                await Task.Delay(1000 * 30); // Send every 30 seconds
            }
        }

        static async Task ListenForSsdpRequests(CancellationToken token)
        {
            UdpClient client = new UdpClient();
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 1900);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(localEndPoint);
            client.JoinMulticastGroup(IPAddress.Parse("239.255.255.250"));

            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result = await client.ReceiveAsync();
                string request = Encoding.UTF8.GetString(result.Buffer);

                if (request.Contains("NOTIFY") | request.Contains($"HTTP/1.1 200 OK"))
                {
                    if (request.Contains($"ST: {SSDPTemplates.ControllerSchema}") | request.Contains($"NT: {SSDPTemplates.ClientSchema}"))
                    {
                        var URL = string.Empty;
                        var lines = request.Split("\r\n");
                        string UID = string.Empty;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains("LOCATION:"))
                            {
                                URL = lines[i].Replace("LOCATION:", string.Empty).Trim();
                            }
                            if (lines[i].Contains("USN"))
                            {
                                UID = lines[i].Split(":")[2].Trim();
                            }
                        }
                        string description = string.Empty;
                        try
                        {
                            using HttpClient Hclient = new HttpClient();
                            var response = await Hclient.GetAsync(URL);
                            response.EnsureSuccessStatusCode(); // Ensure the response is successful
                            description = await response.Content.ReadAsStringAsync();
                            var sr = new XmlSerializer(typeof(ChannelList));
                            TextReader reader = new StringReader(description);
                            if (!Clients.ContainsKey(UID))
                            {
                                var cl = (ChannelList)sr.Deserialize(reader);
                                var addy = URL.Replace("/description.xml", string.Empty).Replace("http://", string.Empty).Split(":");
                                Clients.Add(UID, new(addy[0].Replace("//", string.Empty), int.Parse(addy[1].Replace(":", string.Empty)), cl));
                                cts.Cancel();
                            }
                        }
                        catch (HttpRequestException httpEx)
                        {
                            Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
                            var addy = URL.Replace("/description.xml", string.Empty).Replace("http://", string.Empty).Split(":");
                            string host = addy[0].Replace("//", string.Empty);
                            int port = int.Parse(addy[1].Replace(":", string.Empty));

                            using TcpClient tcpClient = new TcpClient(host, port);
                            using NetworkStream networkStream = tcpClient.GetStream();
                            using StreamWriter streamWriter = new StreamWriter(networkStream) { AutoFlush = true };
                            using StreamReader streamReader = new StreamReader(networkStream);

                            // Send HTTP GET request
                            string requestMessage = $"GET /description.xml HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n";
                            await streamWriter.WriteAsync(requestMessage);

                            // Read HTTP response
                            StringBuilder responseBuilder = new StringBuilder();
                            char[] buffer = new char[1024];
                            int bytesRead;
                            while ((bytesRead = await streamReader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                responseBuilder.Append(buffer, 0, bytesRead);
                                Console.WriteLine(responseBuilder.ToString());
                                if (responseBuilder.ToString().Contains("</ChannelList>"))
                                {
                                    break;
                                }
                            }

                            string responseContent = responseBuilder.ToString();
                            Console.WriteLine(responseContent);

                            // Parse the response content if needed
                            string content = responseContent.Split("<?")[^1];
                            if (!content.Contains("<?"))
                            {
                                content = "<?"+content;
                            }
                            var sr = new XmlSerializer(typeof(ChannelList));
                            TextReader reader = new StringReader(content);
                            if (!Clients.ContainsKey(UID))
                            {
                                var cl = (ChannelList)sr.Deserialize(reader);
                                Clients.Add(UID, new(host, port, cl));
                                cts.Cancel();
                            }
                            tcpClient.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            
                        }
                    }
                }
            }
            Console.WriteLine("timeout");
        }
    }
}
