using Talki.Enums;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Talki.Models;

namespace Talki.Sockets
{
    public interface IBaseSocket<TModel> where TModel : class, IMessageModel, new()
    {
        #region Events
        /// <summary>
        /// Raises when there is an exception in the remote client.
        /// </summary>
        event EventHandler<Exception> Error;
        ///<summary>
        /// Raises when the client signs on an exterior system;
        /// </summary>
        event EventHandler<TModel> SignOn;
        /// <summary>
        /// Raises when the client signs ff an exterior system;
        /// </summary>
        event EventHandler<TModel> SignOff;
        /// <summary>
        /// Raises when there is a message is received from the external system.
        /// </summary>
        event EventHandler<TModel> MessageReceived;
        /// <summary>
        /// Raises when there is a message is received from the external system.
        /// </summary>
        event EventHandler<BaseSocket<TModel>> LifeCheck;

        #endregion


        /// <summary>
        /// 
        /// </summary>
        StatusEnum Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        bool IsLifeCheckActivated { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultSignOnMessage"></param>
        /// <param name="defaultSignOffMessage"></param>
        /// <param name="defaultLifeCheckMessage"></param>
        void SetDefaultMessages(TModel defaultSignOnMessage, TModel defaultSignOffMessage, TModel defaultLifeCheckMessage);
        
    }
}