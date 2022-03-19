using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Broadcast.Server
{
    internal class BroadcastServer
    {
        UdpClient _udpListener;
        TcpListener _tcpListener;

        IPEndPoint _ep;

        CancellationTokenSource _cts;

        byte[] _buffer = new byte[0];

        byte[] _pingSend = new byte[1] { 1 };
        byte[] _pingResp = new byte[1] { 2 };
        bool _ping = false;

        int _FPS = 0;
        int _FPS_count = 0;
        int _FPS_tick = Environment.TickCount;

        bool _udp = false;
        bool _tcp = false;

        internal async Task StartChannel(string channel, int port, bool udp = false)
        {
            _udp = udp;
            _tcp = !udp;

            _cts = new CancellationTokenSource();
            _ep = new(IPAddress.Any, port);

            if (udp)
                _udpListener = new(port);
            else if (_tcp)
            {
                _tcpListener = new TcpListener(_ep.Address, port);
                _tcpListener.Start();
            }
            else
                throw new NotImplementedException();

            int threads = 20;

            Task[] task = new Task[threads];
            for (int i = 0; i < task.Length; i++)
            {
                if (_udp)
                    task[i] = UDPTransmitterChannelAsync(channel, i, 1000);
                else if (_tcp)
                    task[i] = TCPTransmitterChannelAsync(channel, i, 1000);
            }

            await Task.WhenAll(task);
        }

        internal async Task UDPTransmitterChannelAsync(string channel, int thread, int bufferLength)
        {
            Array.Resize(ref _buffer, _buffer.Length + bufferLength);

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (true)
                    {
                        _buffer = _udpListener.Receive(ref _ep);

                        //CalcFPSEvents();
                        CalcFPSBytes(_buffer.Length);

                        #region Ping
                        if (_buffer.Length == 1)
                        {
                            if (_buffer[0] == 1) // Ping Send
                            {
                                if (_udp)
                                {
                                    await _udpListener.SendAsync(_pingResp, _ep, _cts.Token);
                                }
                                else
                                {

                                }
                                
                            }
                            else if (_buffer[0] == 2) // Ping Receive
                            {
                                _ping = true;
                            }
                            continue;
                        }
                        #endregion
                    }
                }
                catch (Exception e)
                {

                }
            });
        }

        internal async Task TCPTransmitterChannelAsync(string channel, int thread, int bufferLength)
        {
            Array.Resize(ref _buffer, _buffer.Length + bufferLength);

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (true)
                    {
                        TcpClient client = await _tcpListener.AcceptTcpClientAsync(_cts.Token);
                        NetworkStream stream = client.GetStream();
                        int i;

                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(_buffer, 0, _buffer.Length)) != 0)
                        {
                            // Send back a response.
                            //stream.Write(msg, 0, msg.Length);

                            //CalcFPSEvents();
                            CalcFPSBytes(i);

                            #region Ping
                            if (_buffer.Length == 1)
                            {
                                if (_buffer[0] == 1) // Ping Send
                                {
                                    await _udpListener.SendAsync(_pingResp, _ep, _cts.Token);
                                }
                                else if (_buffer[0] == 2) // Ping Receive
                                {
                                    _ping = true;
                                }
                                continue;
                            }
                            #endregion
                        }

                        // Shutdown and end connection
                        client.Close();
                    }
                }
                catch (Exception e)
                {

                }
            });
        }

        public static class FileSizeFormatter
        {
            // Load all suffixes in an array  
            static readonly string[] suffixes =
            { "Bytes", "KB", "MB", "GB", "TB", "PB" };
            public static string FormatSize(Int64 bytes)
            {
                int counter = 0;
                decimal number = (decimal)bytes;
                while (Math.Round(number / 1024) >= 1)
                {
                    number = number / 1024;
                    counter++;
                }
                return string.Format("{0:n1}{1}", number, suffixes[counter]);
            }
        }

        private void CalcFPSBytes(int count)
        {
            if (Environment.TickCount - _FPS_tick >= 1000)
            {
                _FPS = _FPS_count;
                _FPS_count = 0;
                _FPS_tick = Environment.TickCount;

                string size = FileSizeFormatter.FormatSize(_FPS);

                Debug.Print($"Size: {size}");
                Console.WriteLine($"Size: {size}");
            }
            //Interlocked.And(ref _FPS_count, count);
            _FPS_count += count;
        }

        private void CalcFPSEvents()
        {
            if (Environment.TickCount - _FPS_tick >= 1000)
            {
                _FPS = _FPS_count;
                _FPS_count = 0;
                _FPS_tick = Environment.TickCount;
                Debug.Print($"FPS: {_FPS}");
                Console.WriteLine($"FPS: {_FPS}");
            }
            Interlocked.Increment(ref _FPS_count);
        }
    }
}

