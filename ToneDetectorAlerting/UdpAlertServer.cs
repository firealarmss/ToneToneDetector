using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ToneDetectorAlerting
{
    public class UdpAlertServer
    {
        private UdpClient _udpServer;
        private readonly int _port;
        private readonly string _authToken;
        private readonly List<Peer> _connectedPeers;
        private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(15);

        public UdpAlertServer(int port, string authToken)
        {
            _port = port;
            _authToken = authToken;
            _udpServer = new UdpClient(port);
            _connectedPeers = new List<Peer>();
        }

        public async Task StartAsync()
        {
            var pingTimeoutTask = CheckPingTimeoutsAsync();

            while (true)
            {
                var receivedResults = await _udpServer.ReceiveAsync();
                var message = Encoding.UTF8.GetString(receivedResults.Buffer);

                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;
                var opcode = root.GetProperty("opcode").GetString();

                if (Enum.TryParse(opcode, out Opcode receivedOpcode))
                {
                    switch (receivedOpcode)
                    {
                        case Opcode.AUTH:
                            var nodeId = root.GetProperty("nodeId").GetString();
                            var receivedHash = root.GetProperty("hash").GetString();
                            if (receivedHash == ComputeSha256Hash(_authToken))
                            {
                                if (_connectedPeers.Any(p => p.NodeId == nodeId))
                                {
                                    var response = new { opcode = Opcode.AUTH_DUPE_NODE.ToString() };
                                    var responseData = JsonSerializer.Serialize(response);
                                    await _udpServer.SendAsync(Encoding.UTF8.GetBytes(responseData), responseData.Length, receivedResults.RemoteEndPoint);
                                }
                                else
                                {
                                    _connectedPeers.Add(new Peer
                                    {
                                        NodeId = nodeId,
                                        EndPoint = receivedResults.RemoteEndPoint,
                                        LastPingResponse = DateTime.UtcNow
                                    });
                                    var response = new { opcode = Opcode.AUTH_OK.ToString() };
                                    var responseData = JsonSerializer.Serialize(response);
                                    await _udpServer.SendAsync(Encoding.UTF8.GetBytes(responseData), responseData.Length, receivedResults.RemoteEndPoint);
                                }
                            }
                            else
                            {
                                var response = new { opcode = Opcode.AUTH_FAIL.ToString() };
                                var responseData = JsonSerializer.Serialize(response);
                                await _udpServer.SendAsync(Encoding.UTF8.GetBytes(responseData), responseData.Length, receivedResults.RemoteEndPoint);
                            }
                            break;

                        case Opcode.PING:
                            var sequenceNumber = root.GetProperty("sequenceNumber").GetInt32();
                            var pongResponse = new { opcode = Opcode.PONG.ToString(), sequenceNumber };
                            var pongData = JsonSerializer.Serialize(pongResponse);
                            await _udpServer.SendAsync(Encoding.UTF8.GetBytes(pongData), pongData.Length, receivedResults.RemoteEndPoint);

                            var peer = _connectedPeers.FirstOrDefault(p => p.EndPoint.Equals(receivedResults.RemoteEndPoint));
                            if (peer != null)
                            {
                                peer.LastPingResponse = DateTime.UtcNow;
                            }
                            break;
                    }
                }
            }
        }

        public async Task SendToneReportAsync(double frequencyA, double frequencyB)
        {
            foreach (var peer in _connectedPeers)
            {
                var report = new { opcode = Opcode.TONE_REPORT.ToString(), frequencyA, frequencyB };
                var data = JsonSerializer.Serialize(report);
                await _udpServer.SendAsync(Encoding.UTF8.GetBytes(data), data.Length, peer.EndPoint);
            }
        }

        public void Stop()
        {
            _udpServer.Close();
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private async Task CheckPingTimeoutsAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                var now = DateTime.UtcNow;
                var timedOutPeers = _connectedPeers.Where(p => now - p.LastPingResponse > _pingTimeout).ToList();

                foreach (var peer in timedOutPeers)
                {
                    Console.WriteLine($"Peer {peer.NodeId} timed out.");
                    _connectedPeers.Remove(peer);
                }
            }
        }
    }
}
