using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore.Model
{
    public class Respond<T>
    {
        public string Type { get; set; }

        public string Message { get; set; }

        public bool HasError { get; set; }

        public T Context { get; set; }

        public int Count { get; set; }

        public Respond()
        {
            Message = "OK";
        }

        public Respond(T context)
        {
            Message = "OK";
            Context = context;
        }

        public Respond(string msg)
        {
            Message = msg;
        }

        public Respond(string msg, T context)
        {
            Message = msg;
            Context = context;
        }

        public Respond(Exception t)
        {
            Message = t.Message;
            Type = t.GetType().Name;
            HasError = true;
        }
    }
}
