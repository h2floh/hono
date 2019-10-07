using System;
using System.Collections.Generic;
using System.Text;

namespace HonoIntegrationTest
{
    class TestResult
    {
        public string Name { get; set; }
        public int AmountSuccess { get; set; }
        public int AmountFail { get; set; }

        public string SuccessString()
        {
            if (AmountSuccess > 0 && AmountFail == 0)
            {
                return "success";
            }
            else
            {
                return "failed";
            }
        }

        public bool Success()
        {
            return (AmountSuccess > 0 && AmountFail == 0);
        }
    }
}
