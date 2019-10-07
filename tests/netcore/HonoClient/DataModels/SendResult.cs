using System;
using System.Collections.Generic;
using System.Text;

namespace Eclipse.Hono.DotNet
{
    public class SendResult
    {
        public SendResult(int resultCode, string resultCodeStr, string resultMessage)
        {
            ResultCode = resultCode;
            ResultCodeString = resultCodeStr;
            ResultMessage = resultMessage;
        }

        public int ResultCode { get; }

        public string ResultCodeString { get; }
        public string ResultMessage { get; }
    }
}
