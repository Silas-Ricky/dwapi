using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dwapi.ExtractsManagement.Core.Commands.Cbs;
using Dwapi.ExtractsManagement.Core.Commands.Dwh;
using Dwapi.ExtractsManagement.Core.Interfaces.Repository.Cbs;
using Dwapi.ExtractsManagement.Core.Interfaces.Services;
using Dwapi.ExtractsManagement.Core.Model.Destination.Cbs;
using Dwapi.Hubs.Cbs;
using Dwapi.Hubs.Dwh;
using Dwapi.Hubs.Mgs;
using Dwapi.Models;
using Dwapi.SettingsManagement.Core.Application.Metrics.Events;
using Dwapi.SettingsManagement.Core.Model;
using Dwapi.SharedKernel.DTOs;
using Dwapi.SharedKernel.Utility;
using Dwapi.UploadManagement.Core.Interfaces.Services.Cbs;
using Dwapi.UploadManagement.Core.Interfaces.Services.Dwh;
using Dwapi.UploadManagement.Core.Interfaces.Services.Mgs;
using Hangfire;
using Hangfire.States;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace Dwapi.Controller
{
    [Produces("application/json")]
    [Route("api/Mgs")]
    public class MgsController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IMediator _mediator;
        private readonly IExtractStatusService _extractStatusService;
        private readonly IHubContext<MgsActivity> _hubContext;
        private readonly IHubContext<MgsSendActivity> _hubSendContext;
        private readonly IMgsSendService _mgsSendService;

        public MgsController(IMediator mediator, IExtractStatusService extractStatusService,
            IHubContext<MgsActivity> hubContext, IMgsSendService mgsSendService,
            IHubContext<MgsSendActivity> hubSendContext)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _extractStatusService = extractStatusService;
            _mgsSendService = mgsSendService;
            Startup.MgsSendHubContext = _hubSendContext = hubSendContext;
            Startup.MgsHubContext = _hubContext = hubContext;
        }

        [HttpPost("extractAll")]
        public async Task<IActionResult> Load([FromBody] LoadMgsExtracts request)
        {
            if (!ModelState.IsValid) return BadRequest();

            var ver = GetType().Assembly.GetName().Version;
            string version = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            await _mediator.Publish(new ExtractLoaded("MigrationService", version));

            var result = await _mediator.Send(request.LoadMgsFromEmrCommand, HttpContext.RequestAborted);
            return Ok(result);
        }


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

        // POST: api/DwhExtracts/manifest
        [HttpPost("manifest")]
        public async Task<IActionResult> SendManifest([FromBody] SendManifestPackageDTO packageDto)
        {
            if (!packageDto.IsValid())
                return BadRequest();

            var ver = GetType().Assembly.GetName().Version;
            string version = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            await _mediator.Publish(new ExtractSent("HivTestingService", version));

            try
            {
                var result = await _mgsSendService.SendManifestAsync(packageDto);
                return Ok(result);
            }
            catch (Exception e)
            {
                var msg = $"Error sending  Manifest {e.Message}";
                Log.Error(e, msg);
                return StatusCode(500, msg);
            }
        }


      
        [HttpPost("migrations")]
        public IActionResult SendMigrationExtracts([FromBody] SendManifestPackageDTO packageDto)
        {
            if (!packageDto.IsValid())
                return BadRequest();
            try
            {
                _mgsSendService.SendMigrationsAsync(packageDto);
                return Ok();
            }
            catch (Exception e)
            {
                var msg = $"Error sending Extracts {e.Message}";
                Log.Error(e, msg);
                return StatusCode(500, msg);
            }
        }
    }
}
