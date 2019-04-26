using System.Collections.Generic;
using System.Net.Sockets;
using Talki.Models;

namespace Talki.Sockets.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class Client<TModel> : BaseSocket<TModel> where TModel : class, IMessageModel, new()
    {
        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        public Client():base()
        {

        }
        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        /// <param name="family"></param>
        public Client(AddressFamily family):base(family) { }

        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        /// <param name="localEP"></param>
        public Client(System.Net.IPEndPoint localEP) : base(localEP) { }

        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public Client(string hostname, int port):base(hostname, port) { }

    }
}
