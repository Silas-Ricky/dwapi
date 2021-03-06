﻿using System;
using System.Collections.Generic;
using Dwapi.ExtractsManagement.Core.Model.Destination.Hts.NewHts;
using Dwapi.SharedKernel.Interfaces;
using Dwapi.SharedKernel.Model;

namespace Dwapi.ExtractsManagement.Core.Interfaces.Repository.Hts
{
    public interface IHtsClientsExtractRepository : IRepository<HtsClients, Guid>
    {
        bool BatchInsert(IEnumerable<HtsClients> extracts);
        IEnumerable<HtsClients> GetView();
        void UpdateSendStatus(List<SentItem> sentItems);
    }


}
