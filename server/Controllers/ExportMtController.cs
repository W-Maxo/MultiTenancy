using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenancy.Data;

namespace MultiTenancy
{
    public partial class ExportMtController : ExportController
    {
        private readonly MtContext context;
        private readonly MtService service;
        public ExportMtController(MtContext context, MtService service)
        {
            this.service = service;
            this.context = context;
        }

    }
}
