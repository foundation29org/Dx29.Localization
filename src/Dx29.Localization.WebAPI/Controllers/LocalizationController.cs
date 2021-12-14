using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Dx29.Services;

namespace Dx29.Localization.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class LocalizationController : ControllerBase
    {
        public LocalizationController(LocalizationService localizationService)
        {
            LocalizationService = localizationService;
        }

        public LocalizationService LocalizationService { get; }

        [HttpGet("literals")]
        public async Task<IActionResult> GetLiteralsAsync(string lang = "en")
        {
            try
            {
                var items = await LocalizationService.GetLiteralsAsync(lang);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("literal")]
        public async Task<IActionResult> GetLiteralAsync(string key, string lang = "en")
        {
            try
            {
                var item = await LocalizationService.GetLiteralAsync(lang, key);
                return Ok(item);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("literals")]
        public async Task<IActionResult> SetLiteralsAsync([FromBody] IDictionary<string, string> literals, string lang = "en")
        {
            try
            {
                await LocalizationService.SetLiteralsAsync(lang, literals);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("literal")]
        public async Task<IActionResult> SetLiteralAsync([FromBody] KeyValuePair<string, string> literal, string lang = "en")
        {
            try
            {
                await LocalizationService.SetLiteralAsync(lang, literal);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("register")]
        public async Task<IActionResult> RegisterLiteralAsync([FromBody] KeyValuePair<string, string> literal, string lang = "en")
        {
            try
            {
                var value = await LocalizationService.RegisterLiteralAsync(lang, literal.Key);
                return Ok(value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("literal")]
        public async Task<IActionResult> DeleteLiteralAsync(string key, string lang = "en")
        {
            try
            {
                await LocalizationService.DeleteLiteralAsync(lang, key);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
