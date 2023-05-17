// kcp client logic abstracted into a class.
// for use in Mirror, DOTSNET, testing, etc.
using System;
using System.Net;
using System.Net.Sockets;

namespace kcp2k
{
    public class KcpClient
    {
        // kcp
        // public so that bandwidth statistics can be accessed from the outside
        public KcpPeer peer;

        // IO
        protected Socket socket;
        public EndPoint remoteEndPoint;

        // raw receive buffer always needs to be of 'MTU' size, even if
        // MaxMessageSize is larger. kcp always sends in MTU segments and having
        // a buffer smaller than MTU would silently drop excess data.
        // => we need the MTU to fit channel + message!
        // => protected because someone may overwrite RawReceive but still wants
        //    to reuse the buffer.
        protected readonly byte[] rawReceiveBuffer = new byte[Kcp.MTU_DEF];

        // events
        public Action OnConnected;
        public Action<ArraySegment<byte>, KcpChannel> OnData;
        public Action OnDisconnected;
        // error callback instead of logging.
        // allows libraries to show popups etc.
        // (string instead of Exception for ease of use and to avoid user panic)
        public Action<ErrorCode, string> OnError;

        // state
        public bool connected;

        public KcpClient(Action OnConnected,
                         Action<ArraySegment<byte>,
                         KcpChannel> OnData,
                         Action OnDisconnected,
                         Action<ErrorCode, string> OnError)
        {
            this.OnConnected = OnConnected;
            this.OnData = OnData;
            this.OnDisconnected = OnDisconnected;
            this.OnError = OnError;
        }

        public void Connect(string address,
                            ushort port,
                            bool noDelay,
                            uint interval,
                            int fastResend = 0,
                            bool congestionWindow = true,
                            uint sendWindowSize = Kcp.WND_SND,
                            uint receiveWindowSize = Kcp.WND_RCV,
                            int timeout = KcpPeer.DEFAULT_TIMEOUT,
                            uint maxRetransmits = Kcp.DEADLINK,
                            bool maximizeSendReceiveBuffersToOSLimit = false)
        {
            if (connected)
            {
                Log.Warning("[KCP] client already connected!");
                return;
            }

            // create fresh peer for each new session
            peer = new KcpPeer(RawSend, noDelay, interval, fastResend, congestionWindow, sendWindowSize, receiveWindowSize, timeout, maxRetransmits);

            // setup events
            peer.OnAuthenticated = () =>
            {
                Log.Info($"[KCP] OnClientConnected");
                connected = true;
                OnConnected();
            };
            peer.OnData = (message, channel) =>
            {
                //Log.Debug($"KCP: OnClientData({BitConverter.ToString(message.Array, message.Offset, message.Count)})");
                OnData(message, channel);
            };
            peer.OnDisconnected = () =>
            {
                Log.Info($"[KCP] OnClientDisconnected");
                connected = false;
                peer = null;
                socket?.Close();
                socket = null;
                remoteEndPoint = null;
                OnDisconnected();
            };
            peer.OnError = (error, reason) =>
            {
                OnError(error, reason);
            };

            Log.Info($"[KCP] Client connecting to {address}:{port}");

            // try resolve host name
            if (!Common.ResolveHostname(address, out IPAddress[] addresses))
            {
                // pass error to user callback. no need to log it manually.
                peer.OnError(ErrorCode.DnsResolve, $"[KCP] Failed to resolve host: {address}");
                peer.OnDisconnected();
                return;
            }

            // create socket
            remoteEndPoint = new IPEndPoint(addresses[0], port);
            socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // configure buffer sizes:
            // if connections drop under heavy load, increase to OS limit.
            // if still not enough, increase the OS limit.
            if (maximizeSendReceiveBuffersToOSLimit)
            {
                Common.MaximizeSocketBuffers(socket);
            }
            // otherwise still log the defaults for info.
            else Log.Info($"[KCP] Client RecvBuf = {socket.ReceiveBufferSize} SendBuf = {socket.SendBufferSize}. If connections drop under heavy load, enable {nameof(maximizeSendReceiveBuffersToOSLimit)} to increase it to OS limit. If they still drop, increase the OS limit.");

            // bind to endpoint so we can use send/recv instead of sendto/recvfrom.
            socket.Connect(remoteEndPoint);

            // client should send handshake to server as very first message
            peer.SendHandshake();

            RawReceive();
        }

        // io - input.
        // virtual so it may be modified for relays, etc.
        protected virtual void RawReceive()
        {
            if (socket == null) return;

            try
            {
                while (socket.Poll(0, SelectMode.SelectRead))
                {
                    // ReceiveFrom allocates. we used bound Receive.
                    // returns amount of bytes written into buffer.
                    // throws SocketException if datagram was larger than buffer.
                    // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receive?view=net-6.0
                    int msgLength = socket.Receive(rawReceiveBuffer);

                    //Log.Debug($"KCP: client raw recv {msgLength} bytes = {BitConverter.ToString(buffer, 0, msgLength)}");
                    peer.RawInput(rawReceiveBuffer, 0, msgLength);
                }
            }
            // this is fine, the socket might have been closed in the other end
            catch (SocketException ex)
            {
                // the other end closing the connection is not an 'error'.
                // but connections should never just end silently.
                // at least log a message for easier debugging.
                Log.Info($"[KCP] ClientConnection: looks like the other end has closed the connection. This is fine: {ex}");
                peer.Disconnect();
            }
        }

        // io - output.
        // virtual so it may be modified for relays, etc.
        protected virtual void RawSend(ArraySegment<byte> data)
        {
            socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
        }

        public void Send(ArraySegment<byte> segment, KcpChannel channel)
        {
            if (!connected)
            {
                Log.Warning("[KCP] Can't send because client not connected!");
                return;
            }

            peer.SendData(segment, channel);
        }

        public void Disconnect()
        {
            // only if connected
            // otherwise we end up in a deadlock because of an open Mirror bug:
            // https://github.com/vis2k/Mirror/issues/2353
            if (!connected) return;

            // call Disconnect and let the connection handle it.
            // DO NOT set it to null yet. it needs to be updated a few more
            // times first. let the connection handle it!
            peer?.Disconnect();
        }

        // process incoming messages. should be called before updating the world.
        public void TickIncoming()
        {
            // recv on socket first, then process incoming
            // (even if we didn't receive anything. need to tick ping etc.)
            // (connection is null if not active)
            if (peer != null)
            {
                RawReceive();
                peer.TickIncoming();
            }
        }

        // process outgoing messages. should be called after updating the world.
        public void TickOutgoing()
        {
            // process outgoing
            // (connection is null if not active)
            peer?.TickOutgoing();
        }

        // process incoming and outgoing for convenience
        // => ideally call ProcessIncoming() before updating the world and
        //    ProcessOutgoing() after updating the world for minimum latency
        public void Tick()
        {
            TickIncoming();
            TickOutgoing();
        }
    }
}
