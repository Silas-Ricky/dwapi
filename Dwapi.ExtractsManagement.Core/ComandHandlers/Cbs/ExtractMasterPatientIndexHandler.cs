﻿using System.Threading;
using System.Threading.Tasks;
using Dwapi.ExtractsManagement.Core.Commands;
using Dwapi.ExtractsManagement.Core.Commands.Cbs;
using Dwapi.ExtractsManagement.Core.Interfaces.Extratcors;
using Dwapi.ExtractsManagement.Core.Interfaces.Extratcors.Cbs;
using Dwapi.ExtractsManagement.Core.Interfaces.Loaders;
using Dwapi.ExtractsManagement.Core.Interfaces.Utilities;
using Dwapi.ExtractsManagement.Core.Interfaces.Validators;
using Dwapi.ExtractsManagement.Core.Model.Destination.Cbs;
using Dwapi.ExtractsManagement.Core.Model.Destination.Dwh;
using Dwapi.ExtractsManagement.Core.Model.Source.Dwh;
using Dwapi.ExtractsManagement.Core.Notifications;
using Dwapi.SharedKernel.Events;
using Dwapi.SharedKernel.Model;
using MediatR;

namespace Dwapi.ExtractsManagement.Core.ComandHandlers.Cbs
{
    public class ExtractMasterPatientIndexHandler : IRequestHandler<ExtractMasterPatientIndex,bool>
    {
        private readonly IMasterPatientIndexSourceExtractor _patientSourceExtractor;
        private readonly IPatientValidator _patientValidator;
        private readonly IPatientLoader _patientLoader;
        private readonly IClearExtracts _clearExtracts;

        public ExtractMasterPatientIndexHandler(IMasterPatientIndexSourceExtractor patientSourceExtractor, IPatientValidator patientValidator, IPatientLoader patientLoader, IClearExtracts clearExtracts)
        {
            _patientSourceExtractor = patientSourceExtractor;
            _patientValidator = patientValidator;
            _patientLoader = patientLoader;
            _clearExtracts = clearExtracts;
        }

        public async Task<bool> Handle(ExtractMasterPatientIndex request, CancellationToken cancellationToken)
        {
            //clear
          //  int count = await _clearExtracts.Clear();

            //Extract
            int found = await _patientSourceExtractor.Extract(request.Extract, request.DatabaseProtocol);

            //Validate
          //  await _patientValidator.Validate();

            /*
            //notify rejected
            int rejected = await _patientValidator.GetRejectedCount();
            DomainEvents.Dispatch(
                new ExtractActivityNotification(new DwhProgress(
                    nameof(MasterPatientIndex),
                    "loaded",
                    found, 0, rejected, 0, 0)));

            //Load
            int loaded = await _patientLoader.Load();

            //notify loaded
            DomainEvents.Dispatch(
                new ExtractActivityNotification(new DwhProgress(
                    nameof(MasterPatientIndex),
                    "loaded",
                    found, loaded, rejected, 0, 0)));
                    */
            return true;
        }
    }
}