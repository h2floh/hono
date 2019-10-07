using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Eclipse.Hono.DotNet
{
    public abstract class HonoBackendClient
    {   
        
        // Hono tenantID
        protected string tenantId = "unknown";

        protected ILogger Logger { get; }

        public HonoBackendClient(string tenantId, ILogger logger = null)
        {
            this.tenantId = tenantId;

            if (logger == null)
            {
                this.Logger = new LoggerFactory().CreateLogger(typeof(HonoBackendClient));
            }
            else
            {
                this.Logger = logger;
            }
        }

    }
}
