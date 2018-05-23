﻿using System;
using System.Collections.Generic;
using System.Text;
using Dwapi.SharedKernel.Events;
using Dwapi.SharedKernel.Model;
using MediatR;

namespace Dwapi.ExtractsManagement.Core.Notifications
{
  public  class ExtractActivityNotification : IDomainEvent
    {
        public DwhProgress Progress { get; set; }

        public ExtractActivityNotification(DwhProgress progress)
        {
            Progress = progress;
        }
    }
}