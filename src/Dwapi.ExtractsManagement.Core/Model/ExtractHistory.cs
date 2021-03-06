﻿using System;
using Dwapi.SharedKernel.Enum;
using Dwapi.SharedKernel.Model;

namespace Dwapi.ExtractsManagement.Core.Model
{
    public class ExtractHistory : Entity<Guid>
    {
        public ExtractStatus Status { get; set; } = ExtractStatus.Idle;
        public DateTime? StatusDate { get; set; }
        public int? Stats { get; set; }
        public string StatusInfo { get; set; }
        public Guid ExtractId { get; set; }

        public ExtractHistory()
        {
        }
        public ExtractHistory(ExtractStatus status, Guid extractId)
        {
            Status = status;
            StatusDate = DateTime.Now;
            ExtractId = extractId;
        }
        public ExtractHistory(ExtractStatus status, int? stats, string statusInfo,Guid extractId)
        {
            Status = status;
            StatusDate=DateTime.Now;
            Stats = stats;
            StatusInfo = statusInfo;
            ExtractId = extractId;
        }
        
        public override string ToString()
        {
            var dateInfo = StatusDate.HasValue ? $"[{StatusDate:F}]" : string.Empty;
            var statsInfo=Stats.HasValue? $"[{Stats}]" : string.Empty;
            return $"{Status} {statsInfo} {dateInfo} {StatusInfo}";
        }

        public bool IsStarted()
        {
            return Status != ExtractStatus.Idle;
        }
        public bool IsSending()
        {
            return Status == ExtractStatus.Sending;
        }
        public bool IsSent()
        {
            return Status == ExtractStatus.Sent;
        }
    }
}