using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MixMeet.Reservas.Data;
using MixMeet.Reservas.Models;
using System.Security.Claims;

namespace MixMeet.Reservas.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users/me
        // Verifica se o usuário já tem cadastro
        [HttpGet("me")]
        public async Task<ActionResult<User>> GetMe()
        {
            // Obtém o telefone do Claim 'sub' ou 'nameidentifier' do JWT
            var phoneNumber = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(phoneNumber)) return Unauthorized();

            var user = await _context.Users.FindAsync(phoneNumber);

            if (user == null)
            {
                return NotFound(); // Retorna 404 se não tiver nickname cadastrado
            }

            return user;
        }

        // POST: api/users/nickname
        // Define ou atualiza o nickname
        [HttpPost("nickname")]
        public async Task<ActionResult<User>> SetNickname([FromBody] UserDto dto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(phoneNumber)) return Unauthorized();

            var user = await _context.Users.FindAsync(phoneNumber);

            if (user == null)
            {
                // Cria novo usuário
                user = new User { PhoneNumber = phoneNumber, Nickname = dto.Nickname };
                _context.Users.Add(user);
            }
            else
            {
                // Atualiza existente
                user.Nickname = dto.Nickname;
            }

            await _context.SaveChangesAsync();
            return Ok(user);
        }
    }

    // DTO simples para receber o JSON
    public class UserDto {
        public string Nickname { get; set; }
    }
}