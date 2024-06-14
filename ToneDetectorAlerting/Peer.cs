using System;
using System.Net;

namespace ToneDetectorAlerting
{
    public class Peer
    {
        public string NodeId { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastPingResponse { get; set; }
    }
}