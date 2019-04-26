using Talki.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Talki.Models;

namespace Talki.Sockets
{
    /// <summary>
    /// Base Socket Manipulation class
    /// </summary>
    /// <typeparam name="TModel">The type of message the socket is going to handle e.g (string, object, AClass)</typeparam>
    public abstract class BaseSocket<TModel> : TcpClient,IBaseSocket<TModel> where TModel : class, IMessageModel, new()
    {
        private bool bridgeMethodsSet;
        #region Properties
        /// <summary>
        /// Receiving Thread
        /// </summary>
        protected Thread receivingThread { get; set; }
        /// <summary>
        /// Sending Thread
        /// </summary>
        protected Thread sendingThread { get; set; }
        /// <summary>
        /// LifeCheck Thread
        /// </summary>
        protected Thread lifeCheckThread { get; set; }
        /// <summary>
        /// Default SignOff Message to send to external system
        /// </summary>
        public TModel DefaultSignOffMessage { get; protected set; }
        /// <summary>
        /// Default LifeCheck Message to send to external system
        /// </summary>
        public TModel DefaultLifeCheckMessage { get; protected set; }

        /// <summary>
        /// Default sign on message send to external system
        /// </summary>
        public TModel DefaultSignOnMessage { get; protected set; }

        /// <summary>
        /// The TcpClient that is encapsulated by this client instance.
        /// </summary>
        //public TcpClient TcpClient { get; set; }
        /// <summary>
        /// The ip/domain address of the remote server.
        /// </summary>
        public String Address { get; protected set; }
        /// <summary>
        /// The Port that is used to connect to the remote server.
        /// </summary>
        public int Port { get; protected set; }
        /// <summary>
        /// The status of the client.
        /// </summary>
        public StatusEnum Status { get; set; }
        /// <summary>
        /// Indicates whether the system checks if the external system is alive.
        /// </summary>
        public bool IsLifeCheckActivated { get; set; }
        /// <summary>
        /// List containing all messages that is waiting to be delivered to the remote client/server
        /// </summary>
        public static List<TModel> MessageQueue { get; protected set; }

        /// <summary>
        /// Indicates whether client can perform operations.
        /// </summary>
        public bool CanOperate { get; set; } = false;
        #endregion

        #region Events
        /// <summary>
        /// Raises when there is an exception in the remote client.
        /// </summary>
        public event EventHandler<Exception> Error;
        /// Raises when the client signs on an exterior system;
        /// </summary>
        public event EventHandler<TModel> SignOn;
        /// <summary>
        /// Raises when the client signs ff an exterior system;
        /// </summary>
        public event EventHandler<TModel> SignOff;
        /// <summary>
        /// Raises when there is a message is received from the external system.
        /// </summary>
        public event EventHandler<TModel> MessageReceived;
        /// <summary>
        /// Raises when there is a message is received from the external system.
        /// </summary>
        public event EventHandler<BaseSocket<TModel>> LifeCheck;

        #endregion

        #region Constructors

        static BaseSocket()
        {
            MessageQueue = new List<TModel>();
        }
        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        public BaseSocket():base()
        {
            
        }
        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        /// <param name="family"></param>
        public BaseSocket(AddressFamily family):base(family) {       }

        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        /// <param name="localEP"></param>
        public BaseSocket(IPEndPoint localEP) : base(localEP) {      }

        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public BaseSocket(string hostname, int port):base(hostname, port) {  }

        #endregion

        /// <summary>
        /// Sets default signOn, signOff and LifeCheck Message
        /// </summary>
        /// <param name="defaultSignOffMessage"></param>
        /// <param name="defaultLifeCheckMessage"></param>
        /// <param name="defaultSignOnMessage"></param>
        public void SetDefaultMessages(TModel defaultSignOnMessage, TModel defaultSignOffMessage, TModel defaultLifeCheckMessage)
        {
            DefaultSignOnMessage = defaultSignOnMessage; 
            DefaultSignOffMessage = defaultSignOffMessage;
            DefaultLifeCheckMessage = defaultLifeCheckMessage;
        }

        #region Methods
        /// <summary>
        /// Connect to a remote server.
        /// (The client will not be able to perform any operations until it is loged in and validated).
        /// </summary>
        /// <param name="address">The server ip/domain address.</param>
        /// <param name="port">The server port.</param>
        /// <param name="activateLifeCheck">Indicates whether the system checks if the external system is alive.</param>
        /// <param name="context">The params context that could be pass to the socket thread handlers.</param>
        public virtual void BuildBridge(string address, int port, bool activateLifeCheck = false, object context = null)
        {
            Address = address;
            Port = port;

            Connect(address, port);

            ReceiveBufferSize = 1024;
            SendBufferSize = 1024;

            SetBridgeMethods(activateLifeCheck, context);

            Status = StatusEnum.Connected;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="activateLifeCheck"></param>
        /// <param name="context"></param>
        public virtual void BuildBridge(TcpClient client, bool activateLifeCheck = false, object context = null)
        {
            Clone(client);
            var ipendpoint = Client.LocalEndPoint as IPEndPoint;
            Address = ipendpoint.Address.ToString();
            Port = ipendpoint.Port;

            SetBridgeMethods(activateLifeCheck, context);

            Status = StatusEnum.Connected;
            SendTimeout = 10000;
            ReceiveTimeout = 10000;
        }

        protected void Clone(TcpClient client)
        {
            base.Client = client.Client;
            base.NoDelay = client.NoDelay;
            base.ReceiveBufferSize = client.ReceiveBufferSize;
            //base.ReceiveTimeout = client.ReceiveTimeout;
            base.SendBufferSize = client.SendBufferSize;
            //base.SendTimeout = client.SendTimeout;
        }

        private void SetBridgeMethods(bool activateLifeCheck, object context)
        {

            if (!bridgeMethodsSet)
            {
                sendingThread = new Thread(new ThreadStart(() => Sending(context ?? this)));
                sendingThread.IsBackground = true;
                sendingThread.Start();

                receivingThread = new Thread(new ThreadStart(() => Receiving(context ?? this)));
                receivingThread.IsBackground = true;
                receivingThread.Start();

                if (activateLifeCheck)
                {
                    lifeCheckThread = new Thread(new ThreadStart(() => LifeChecking(context ?? this)));
                    lifeCheckThread.IsBackground = true;
                    lifeCheckThread.Start();
                    IsLifeCheckActivated = activateLifeCheck;
                }
                bridgeMethodsSet = true;
            }
        }

        /// <summary>
        /// SignOn to the external system.
        /// </summary>
        /// <param name="signOnMsg"></param>
        public virtual void SendConnectionMessage(TModel signOnMsg)
            =>
                SendMessage(signOnMsg);

        /// <summary>
        /// SignOff from the external system.
        /// </summary>
        public virtual void SendDisconnectMessage(TModel signoffmsg)
        {
            MessageQueue.Clear();
            try
            {
                SendMessage(signoffmsg);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            Thread.Sleep(1000);
            Status = StatusEnum.Disconnected;
            try
            {
                Client.Disconnect(false);
                Close();
                OnSignOff(signoffmsg);
            }
            catch (Exception ex)
            {
                OnError(new Exception("Host has disconnected!", ex));
            }
        }

        /// <summary>
        /// Send a text message to the remote client.
        /// </summary>
        /// <param name="message"></param>
        public virtual void SendMessage(TModel message)
        {
            if(message != null)
                MessageQueue.Add(message);

        }
        #endregion

        #region Threading Methods

        /// <summary>
        /// Send threading method.
        /// </summary>
        /// <param name="context"></param>
        public virtual void Sending(object context)
        {
            while (Status != StatusEnum.Disconnected && Connected)
            {
                if (MessageQueue.Count > 0)
                {
                    TModel m = MessageQueue[0];
                    
                    try
                    {
                        if (m != null)
                        {
                            var str = GetStream();
                            var msg = m.GetBytes();

                            str.Write(msg, 0, msg.Count());
                        }
                    }
                    catch(Exception ex)
                    {
                        OnError(ex);
                        //SendDisconnectMessage(DefaultSignOffMessage);
                    }

                    MessageQueue.Remove(m);
                }

                Thread.Sleep(30);
            }
        }

        /// <summary>
        /// Receive threading method.
        /// </summary>
        /// <param name="context"></param>
        public virtual void Receiving(object context)
        {
            while (Status != StatusEnum.Disconnected && Connected)
            {
                if (Available > 0 )
                {
                    try
                    {
                        NetworkStream ms = GetStream();
                        
                        var msg = ReadToEnd(ms);
                        //string msg = Encoding.ASCII.GetString(res);

                        OnMessageReceived(GetModel(msg));
                    }
                    catch (Exception e)
                    {
                        Exception ex = new Exception(e.ToString());
                        OnError(ex);
                        //Debug.WriteLine(ex.Message);
                    }
                }

                Thread.Sleep(30);
            }
        }

        private TModel GetModel(byte[] msg)
        {
            var m = new TModel();
            m.Load(msg);

            return m;
        }

        private byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                int lengthOfLength = 4;
                byte[] l = new byte[lengthOfLength];

                stream.Read(l, 0, lengthOfLength);

                string length = Encoding.ASCII.GetString(l);
                int actuLength = int.Parse(length) + lengthOfLength;

                byte[] readBuffer = new byte[actuLength];

                Array.Copy(l, 0, readBuffer, 0, lengthOfLength);

                int totalBytesRead = lengthOfLength;

                int bytesRead = 0;

                while (totalBytesRead < actuLength && (bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                }
                return readBuffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        /// <summary>
        /// LifeCheck threading method.
        /// </summary>
        /// <param name="context"></param>
        public virtual void LifeChecking(object context)
        {
            while (Status != StatusEnum.Disconnected && IsLifeCheckActivated)
            {

                try
                {
                    if (DefaultLifeCheckMessage != null)
                    {
                        var str = GetStream();
                        var m = DefaultLifeCheckMessage as byte[];
                        str.Write(m.ToArray(), 0, m.Count());

                        OnLifeCheck(context as BaseSocket<TModel>);
                    }
                       
                }
                catch
                {
                    SendDisconnectMessage(DefaultSignOffMessage);
                }
                
                Thread.Sleep(60000);
            }
        }
        #endregion


        #region Virtuals
        protected virtual void OnSignOn(TModel msg, object sender = null)
            =>
                SignOn?.Invoke(sender ?? this, msg);
        protected virtual void OnSignOff(TModel msg, object sender = null)
            =>
                SignOff?.Invoke(sender ?? this, msg);
        protected virtual void OnMessageReceived(TModel msg, object sender = null)
            =>
                MessageReceived?.Invoke(sender ?? this, msg);
        protected virtual void OnError(Exception ex, object sender = null)
            =>
                Error?.Invoke(sender ?? this, ex);
        protected virtual void OnLifeCheck(BaseSocket<TModel> client, object sender = null)
            =>
                LifeCheck?.Invoke(sender ?? this, client);
        
        #endregion
    }
}
