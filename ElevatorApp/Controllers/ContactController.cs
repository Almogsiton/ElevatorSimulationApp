using Microsoft.AspNetCore.Mvc;
using ElevatorApp.DataAccess.Helpers;

namespace ElevatorApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetContactInfo()
        {
            return Ok(new
            {
                email = Constants.ContactEmail,
                phone = Constants.ContactPhone
            });
        }
    }
}
