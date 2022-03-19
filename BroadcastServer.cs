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
        UdpClient _listener;
        IPEndPoint _ep;

        CancellationTokenSource _cts;

        byte[] _buffer = new byte[0];

        byte[] _pingSend = new byte[1] { 1 };
        byte[] _pingResp = new byte[1] { 2 };
        bool _ping = false;

        int _FPS = 0;
        int _FPS_count = 0;
        int _FPS_tick = Environment.TickCount;

        internal async Task StartChannel(string channel, int port)
        {
            _cts = new CancellationTokenSource();
            _listener = new(port);

            _ep = new(IPAddress.Any, port);

            int threads = 5;

            Task[] task = new Task[threads];
            for (int i = 0; i < task.Length; i++)
            {
                task[i] = TransmitterChannelAsync(channel, i, 1000);
            }

            await Task.WhenAll(task);
        }

        internal async Task TransmitterChannelAsync(string channel, int thread, int bufferLength)
        {
            Array.Resize(ref _buffer, _buffer.Length + bufferLength);

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (true)
                    {
                        _buffer = _listener.Receive(ref _ep);


                        CalcFPS();

                        if (_buffer.Length == 1)
                        {
                            if (_buffer[0] == 1) // Ping Send
                            {
                                await _listener.SendAsync(_pingResp, _ep, _cts.Token);
                            }
                            else if (_buffer[0] == 2) // Ping Receive
                            {
                                _ping = true;
                            }
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {

                }
            });
        }

        private void CalcFPS()
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

