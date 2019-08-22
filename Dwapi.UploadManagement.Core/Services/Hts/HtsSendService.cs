﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dwapi.ExtractsManagement.Core.Model.Destination.Cbs;
using Dwapi.ExtractsManagement.Core.Model.Destination.Hts.NewHts;
using Dwapi.ExtractsManagement.Core.Notifications;
using Dwapi.SharedKernel.DTOs;
using Dwapi.SharedKernel.Enum;
using Dwapi.SharedKernel.Events;
using Dwapi.SharedKernel.Exchange;
using Dwapi.SharedKernel.Model;
using Dwapi.SharedKernel.Utility;
using Dwapi.UploadManagement.Core.Event.Hts;
using Dwapi.UploadManagement.Core.Exchange.Cbs;
using Dwapi.UploadManagement.Core.Exchange.Hts;
using Dwapi.UploadManagement.Core.Interfaces.Packager.Hts;
using Dwapi.UploadManagement.Core.Interfaces.Services.Hts;
using Dwapi.UploadManagement.Core.Notifications.Hts;
using Newtonsoft.Json;
using Serilog;

namespace Dwapi.UploadManagement.Core.Services.Hts
{
    public class HtsSendService : IHtsSendService
    {
        private readonly string _endPoint;
        private readonly IHtsPackager _packager;

        public HtsSendService(IHtsPackager packager)
        {
            _packager = packager;
            _endPoint = "api/hts/";
        }

        public Task<List<SendManifestResponse>> SendManifestAsync(SendManifestPackageDTO sendTo)
        {
            return SendManifestAsync(sendTo, ManifestMessageBag.Create(_packager.GenerateWithMetrics().ToList()));
        }

        public async Task<List<SendManifestResponse>> SendManifestAsync(SendManifestPackageDTO sendTo, ManifestMessageBag manifestMessage)
        {
            var responses=new List<SendManifestResponse>();

            var client = new HttpClient();

            foreach (var message in manifestMessage.Messages)
            {
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}manifest"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendManifestResponse>();
                        responses.Add(content);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }
            }

            return responses;
        }

        public Task<List<SendMpiResponse>> SendClientsAsync(SendManifestPackageDTO sendTo)
        {
            return SendClientsAsync(sendTo, HtsMessageBag.Create(_packager.GenerateClients().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendClientsAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound=0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}Clients"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.Clients.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent,sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.Clients.Select(x => x.Id).ToList(), SendStatus.Failed,sendTo.ExtractName,error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClients), Common.GetProgress(count,total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClients), Common.GetProgress(count,total),true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }

        public Task<List<SendMpiResponse>> SendClientsLinkagesAsync(SendManifestPackageDTO sendTo)
        {
            return SendClientsLinkagesAsync(sendTo, HtsMessageBag.Create(_packager.GenerateClientLinkage().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendClientsLinkagesAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound=0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}ClientsLinkage"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.ClientLinkage.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent,sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.ClientLinkage.Select(x => x.Id).ToList(), SendStatus.Failed,sendTo.ExtractName,error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClientLinkage), Common.GetProgress(count,total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClientLinkage), Common.GetProgress(count,total),true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }

        public Task<List<SendMpiResponse>> SendClientTestsAsync(SendManifestPackageDTO sendTo)
        {
            return SendClientTestsAsync(sendTo, HtsMessageBag.Create(_packager.GenerateClientTests().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendClientTestsAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound=0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}ClientTests"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.ClientTests.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent,sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.ClientTests.Select(x => x.Id).ToList(), SendStatus.Failed,sendTo.ExtractName,error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClientTests), Common.GetProgress(count,total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClientTests), Common.GetProgress(count,total),true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }

        public Task<List<SendMpiResponse>> SendTestKitsAsync(SendManifestPackageDTO sendTo)
        {
            return SendTestKitsAsync(sendTo, HtsMessageBag.Create(_packager.GenerateTestKits().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendTestKitsAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound = 0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}TestKits"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.TestKits.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent, sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.TestKits.Select(x => x.Id).ToList(), SendStatus.Failed, sendTo.ExtractName, error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsTestKits), Common.GetProgress(count, total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsTestKits), Common.GetProgress(count, total), true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }

        public Task<List<SendMpiResponse>> SendClientTracingAsync(SendManifestPackageDTO sendTo)
        {
            return SendClientTracingAsync(sendTo, HtsMessageBag.Create(_packager.GenerateClientTracing().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendClientTracingAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound = 0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}ClientTracing"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.ClientTracing.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent, sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.ClientTracing.Select(x => x.Id).ToList(), SendStatus.Failed, sendTo.ExtractName, error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClientTracing), Common.GetProgress(count, total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsClientTracing), Common.GetProgress(count, total), true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }

        public Task<List<SendMpiResponse>> SendPartnerTracingAsync(SendManifestPackageDTO sendTo)
        {
            return SendPartnerTracingAsync(sendTo, HtsMessageBag.Create(_packager.GeneratePartnerTracing().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendPartnerTracingAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound = 0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}PartnerTracing"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.PartnerTracing.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent, sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.PartnerTracing.Select(x => x.Id).ToList(), SendStatus.Failed, sendTo.ExtractName, error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsPartnerTracing), Common.GetProgress(count, total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsPartnerTracing), Common.GetProgress(count, total), true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }

        public Task<List<SendMpiResponse>> SendPartnerNotificationServicesAsync(SendManifestPackageDTO sendTo)
        {
            return SendPartnerNotificationServicesAsync(sendTo, HtsMessageBag.Create(_packager.GeneratePartnerNotificationServices().ToList()));
        }

        public async Task<List<SendMpiResponse>> SendPartnerNotificationServicesAsync(SendManifestPackageDTO sendTo, HtsMessageBag messageBag)
        {
            var responses = new List<SendMpiResponse>();

            var client = new HttpClient();
            int sendCound = 0;
            int count = 0;
            int total = messageBag.Messages.Count;
            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sending));

            foreach (var message in messageBag.Messages)
            {
                count++;
                try
                {
                    var msg = JsonConvert.SerializeObject(message);
                    var response = await client.PostAsJsonAsync(sendTo.GetUrl($"{_endPoint.HasToEndsWith("/")}PartnerNotificationServices"), message);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsJsonAsync<SendMpiResponse>();
                        responses.Add(content);

                        var sentIds = message.PartnerNotificationServices.Select(x => x.Id).ToList();
                        sendCound += sentIds.Count;
                        DomainEvents.Dispatch(new HtsExtractSentEvent(sentIds, SendStatus.Sent, sendTo.ExtractName));
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        DomainEvents.Dispatch(new HtsExtractSentEvent(message.PartnerNotificationServices.Select(x => x.Id).ToList(), SendStatus.Failed, sendTo.ExtractName, error));
                        throw new Exception(error);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Send Manifest Error");
                    throw;
                }

                DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsPartnerNotificationServices), Common.GetProgress(count, total))));
            }

            DomainEvents.Dispatch(new HtsSendNotification(new SendProgress(nameof(HtsPartnerNotificationServices), Common.GetProgress(count, total), true)));

            DomainEvents.Dispatch(new HtsStatusNotification(sendTo.ExtractId, ExtractStatus.Sent, sendCound));

            return responses;
        }
    }


}
