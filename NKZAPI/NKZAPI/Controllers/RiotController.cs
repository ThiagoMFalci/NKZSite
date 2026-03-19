
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Services.RiotService;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/riot")]
    public class RiotController : ControllerBase
    {
        private readonly IRiotService _riotService;
        public RiotController(IRiotService riotService)
        {
            _riotService = riotService;
        }

        // endpoint de diagnóstico (pode ser protegido com [Authorize] se quiser)
        [HttpGet("validate-key")]
        public async Task<ActionResult<string>> ValidateKey([FromQuery] string region = "br1")
        {
            try
            {
                var result = await _riotService.ValidateApiKeyAsync(region);
                return Ok(new { success = true, diagnostic = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}