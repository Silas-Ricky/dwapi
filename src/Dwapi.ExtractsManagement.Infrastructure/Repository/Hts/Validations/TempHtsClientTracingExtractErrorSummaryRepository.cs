﻿using Dwapi.ExtractsManagement.Core.Interfaces.Repository.Hts;
using Dwapi.ExtractsManagement.Core.Model.Source.Hts.NewHts;
using Dwapi.ExtractsManagement.Infrastructure.Repository.Hts.Validations.Base;

namespace Dwapi.ExtractsManagement.Infrastructure.Repository.Hts.Validations
{ 
    public class TempHtsClientTracingExtractErrorSummaryRepository : TempHTSExtractErrorSummaryRepository<TempHtsClientTracingErrorSummary>, ITempHtsClientTracingErrorSummaryRepository
    {
        public TempHtsClientTracingExtractErrorSummaryRepository(ExtractsContext context) : base(context)
        {
        }
    }
}
