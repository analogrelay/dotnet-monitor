using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SampleWebApp.Controllers
{
    [ApiController]
    [Route("api/values")]
    public class ValuesController: Controller
    {
        public ActionResult<IEnumerable<int>> Get()
        {
            return new [] { 1, 2, 3, 4, 5 };
        }
    }
}
