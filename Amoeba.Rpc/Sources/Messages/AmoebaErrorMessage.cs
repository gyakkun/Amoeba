using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba.Rpc
{
    [DataContract(Name = nameof(AmoebaErrorMessage))]
    public class AmoebaErrorMessage
    {
        public AmoebaErrorMessage(string type, string message, string stackTrace)
        {
            this.Type = type;
            this.Message = message;
            this.StackTrace = stackTrace;
        }

        [DataMember(Name = nameof(Type))]
        public string Type { get; private set; }

        [DataMember(Name = nameof(Message))]
        public string Message { get; private set; }

        [DataMember(Name = nameof(StackTrace))]
        public string StackTrace { get; private set; }
    }
}
