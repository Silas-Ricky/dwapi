using System;
using System.Linq;
using System.Threading.Tasks;
using Dwapi.ExtractsManagement.Core.Commands.Cbs;
using Dwapi.ExtractsManagement.Core.Interfaces.Packager.Cbs;
using Dwapi.ExtractsManagement.Core.Interfaces.Repository.Cbs;
using Dwapi.ExtractsManagement.Core.Interfaces.Services;
using Dwapi.ExtractsManagement.Core.Model.Destination.Cbs;
using Dwapi.Hubs.Dwh;
using Dwapi.SettingsManagement.Core.Model;
using Dwapi.SharedKernel.DTOs;
using Dwapi.SharedKernel.Model;
using Dwapi.SharedKernel.Utility;
using Dwapi.UploadManagement.Core.Exchange.Cbs;
using Dwapi.UploadManagement.Core.Interfaces.Services.Cbs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace Dwapi.Controller
{
    [Produces("application/json")]
    [Route("api/Cbs")]
    public class CbsController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IMediator _mediator;
        private readonly IExtractStatusService _extractStatusService;
        private readonly IHubContext<CbsActivity> _hubContext;
        private readonly IMasterPatientIndexRepository _masterPatientIndexRepository;
        private readonly ICbsSendService _cbsSendService;
        private readonly ICbsPackager _cbsPackager;

        public CbsController(IMediator mediator, IExtractStatusService extractStatusService,
            IHubContext<CbsActivity> hubContext, IMasterPatientIndexRepository masterPatientIndexRepository,
            ICbsSendService cbsSendService, ICbsPackager cbsPackager)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _extractStatusService = extractStatusService;
            _masterPatientIndexRepository = masterPatientIndexRepository;
            _cbsSendService = cbsSendService;
            _cbsPackager = cbsPackager;
            Startup.CbsHubContext = _hubContext = hubContext;
        }


        [HttpPost("extract")]
        public async Task<IActionResult> Load([FromBody] ExtractMasterPatientIndex request)
        {
            if (!ModelState.IsValid) return BadRequest();
            var result = await _mediator.Send(request, HttpContext.RequestAborted);
            return Ok(result);
        }


        // GET: api/DwhExtracts/status/id
        [HttpGet("status/{id}")]
        public IActionResult GetStatus(Guid id)
        {
            if (id.IsNullOrEmpty())
                return BadRequest();
            try
            {
                var eventExtract = _extractStatusService.GetStatus(id);
                if (null == eventExtract)
                    return NotFound();

                return Ok(eventExtract);
            }
            catch (Exception e)
            {
                var msg = $"Error loading {nameof(Extract)}(s)";
                Log.Error(msg);
                Log.Error($"{e}");
                return StatusCode(500, msg);
            }
        }

        [HttpGet("count")]
        public IActionResult GetExtractCount()
        {
            try
            {
                var count = _masterPatientIndexRepository.GetView().Select(x => x.Id).Count();
                return Ok(count);
            }
            catch (Exception e)
            {
                var msg = $"Error loading {nameof(Extract)}(s)";
                Log.Error(msg);
                Log.Error($"{e}");
                return StatusCode(500, msg);
            }
        }

        [HttpGet]
        public IActionResult GetExtracts()
        {
            try
            {
                var eventExtract = _masterPatientIndexRepository.GetView().ToList().OrderBy(x => x.sxdmPKValueDoB);
                return Ok(eventExtract);
            }
            catch (Exception e)
            {
                var msg = $"Error loading {nameof(Extract)}(s)";
                Log.Error(msg);
                Log.Error($"{e}");
                return StatusCode(500, msg);
            }
        }

        // POST: api/Cbs/manifest
        [HttpPost("manifest")]
        public async Task<IActionResult> SendManifest([FromBody] SendManifestPackageDTO packageDTO)
        {
            if (!packageDTO.IsValid())
                return BadRequest();
            try
            {

                var manifests = _cbsPackager.Generate().ToList();
                await _cbsSendService.SendManifestAsync(packageDTO, ManifestMessageBag.Create(manifests));
                return Ok();
            }
            catch (Exception e)
            {
                var msg = $"Error sending {nameof(MasterPatientIndex)} Manifest {e.Message}";
                Log.Error(e,msg);
                return StatusCode(500, msg);
            }
        }


        // POST: api/Cbs/manifest
        [HttpPost("mpi")]
        public async Task<IActionResult> SendMpi([FromBody] SendManifestPackageDTO packageDTO)
        {
            if (!packageDTO.IsValid())
                return BadRequest();
            try
            {

                var mpi = _cbsPackager.GenerateMpi().ToList();
                await _cbsSendService.SendMpiAsync(packageDTO, MpiMessageBag.Create(mpi));
                return Ok();
            }
            catch (Exception e)
            {
                var msg = $"Error sending {nameof(MasterPatientIndex)} {e.Message}";
                Log.Error(e, msg);
                return StatusCode(500, msg);
            }
        }
    }
}
