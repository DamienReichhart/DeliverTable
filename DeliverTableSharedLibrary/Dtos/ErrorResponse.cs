using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliverTableSharedLibrary.Dtos
{
    public class ErrorResponse
    {
        public string Error { get; set;} = "";
        public int Status {get; set;} = 500;
    }
}