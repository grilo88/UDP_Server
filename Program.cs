using Broadcast.Server;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

BroadcastServer server = new ();
await server.StartChannel("teste", 11000, false);  

    