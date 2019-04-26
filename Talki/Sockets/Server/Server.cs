using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Talki.Sockets.Client;
using Talki.Enums;
using System.Net;
using Talki.Models;

namespace Talki.Sockets.Server
{
    public class Server<TModel> : TcpListener, IBaseSocket<TModel> where TModel : class, IMessageModel, new()
    {
        #region Properties

        /// <summary>
        /// The Port that is used to listen to incoming connections.
        /// </summary>
        public bool IsStarted { get; private set; }
        /// <summary>
        /// List of currently connected receivers.
        /// </summary>
        public static List<BaseSocket<TModel>> Receivers { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public object State { get; private set; }

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
        /// 
        /// </summary>
        public StatusEnum Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsLifeCheckActivated { get; set; }

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

        /// <summary>
        /// Raises when a new client is connected.
        /// </summary>
        public event EventHandler<BaseSocket<TModel>> ClientConnected;
        #endregion

        #region Constructors
        static Server()
        {
            Receivers = new List<BaseSocket<TModel>>();
        }

        /// <summary>
        /// Initialise a new instance of a Server tcpListner
        /// </summary>
        /// <param name="localEP"></param>
        public Server(IPEndPoint localEP):base(localEP)
        {

        }

        /// <summary>
        /// Initialise a new instance of a Server tcpListner
        /// </summary>
        /// <param name="localAddress"></param>
        /// <param name="port"></param>
        public Server(IPAddress localAddress, int port):base(localAddress, port)
        {

        }

        #endregion

        #region Public Methods
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
        

        /// <summary>
        /// Start Listening for incoming connections.
        /// </summary>
        public void StartListener()
        {
            if (!IsStarted)
            {
                State = this;

                Start();
                Status = StatusEnum.Connected;
                //Start Async pattern for accepting new connections
                WaitForConnection();
                IsStarted = true;
            }
        }
        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        public void StopListener(TModel signoffmsg)
        {
            if (IsStarted)
            {
                Stop();
                IsStarted = false;
                
            }
        }

        #endregion

        #region Incoming Connections Methods

        private void WaitForConnection()
        {
            BeginAcceptTcpClient(new AsyncCallback(NewClientConnectionHandler), State);
        }

        private void NewClientConnectionHandler(IAsyncResult ar)
        {
            try
            {
                lock (Receivers)
                {
                    Client.Client<TModel> newClient = new Client.Client<TModel>();
                    newClient.SetDefaultMessages(DefaultSignOnMessage, DefaultSignOffMessage, DefaultLifeCheckMessage);

                    newClient.BuildBridge(EndAcceptTcpClient(ar), IsLifeCheckActivated);

                    SetClientHandlers(ref newClient);

                    Receivers.Add(newClient);
                    OnClientConnected(newClient);
                }

                WaitForConnection();
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void SetClientHandlers(ref Client.Client<TModel> newClient)
        {
            newClient.SignOff += delegate (object sender, TModel m)
            {
                OnSignOff(m, sender);
            };

            newClient.SignOn += delegate (object sender, TModel m)
            {
                OnSignOn(m, sender);
            };

            newClient.MessageReceived += delegate (object sender, TModel m)
            {
                OnMessageReceived(m, sender);
            };

            newClient.LifeCheck += delegate (object sender, BaseSocket<TModel> m)
            {
                OnLifeCheck(m, sender);
            };

            newClient.Error += delegate (object sender, Exception ex)
            {
                OnError(ex, sender);
            };
        }


        #endregion

        #region Virtuals
        protected virtual void OnSignOn(TModel msg, object sender = null)
            =>
                SignOn?.Invoke(sender ?? this, msg);
        protected virtual void OnSignOff(TModel msg, object sender = null)
        {
            SignOff?.Invoke(sender ?? this, msg);
        }
        protected virtual void OnMessageReceived(TModel msg, object sender = null)
            =>
                MessageReceived?.Invoke(sender ?? this, msg);
        protected virtual void OnError(Exception ex, object sender = null)
            =>
                Error?.Invoke(sender ?? this, ex);
        protected virtual void OnLifeCheck(BaseSocket<TModel> client, object sender = null)
            =>
                LifeCheck?.Invoke(sender ?? this, client);

        protected virtual void OnClientConnected(BaseSocket<TModel> client)
            =>
                ClientConnected?.Invoke(this, client);
        #endregion
    }
}
