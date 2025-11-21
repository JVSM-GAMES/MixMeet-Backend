using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MixMeet.Reservas.Data;
using MixMeet.Reservas.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace MixMeet.Reservas.Controllers
{
    // A API só pode ser acessada por usuários com JWT válido
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservasController(AppDbContext context)
        {
            _context = context;
        }

        // --- Método de Validação de Conflito ---
        private async Task<bool> ExisteConflito(Reserva novaReserva)
        {
            // Normaliza as strings para que "Sala 01", "SaLa01" e "sala 01 " não sejam tratados como diferentes
            string salaNormalizada = novaReserva.Sala.Trim().ToLower();
            string localNormalizado = novaReserva.Local.Trim().ToLower();
            
            var conflitos = await _context.Reservas
                .Where(r => r.Id != novaReserva.Id &&
                            r.Sala.ToLower() == salaNormalizada &&
                            r.Local.ToLower() == localNormalizado && 
                            // Lógica de sobreposição de tempo
                            r.DataHoraInicio < novaReserva.DataHoraFim && 
                            r.DataHoraFim > novaReserva.DataHoraInicio)
                .AnyAsync();

            return conflitos;
        }

        // POST: api/Reservas (C - Create/Criar)
        [HttpPost]
        public async Task<ActionResult<Reserva>> PostReserva(Reserva reserva)
        {
            if (await ExisteConflito(reserva))
            {
                return Conflict(new { mensagem = "Conflito de horários detectado. A sala já está reservada neste período." });
            }
            if (reserva.DataHoraInicio >= reserva.DataHoraFim)
            {
                return BadRequest(new { mensagem = "A data/hora de início deve ser anterior à data/hora de fim." });
            }

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReserva), new { id = reserva.Id }, reserva);
        }

        // GET: api/Reservas (R - Read/Listar)
        // O GET pode ser público se desejado, mas mantemos Autorizado por padrão de segurança
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reserva>>> GetReservas()
        {
            return await _context.Reservas.ToListAsync();
        }
        
        // GET: api/Reservas/5 (R - Read/Buscar por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Reserva>> GetReserva(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) { return NotFound(); }
            return reserva;
        }

        // PUT: api/Reservas/5 (U - Update/Atualizar)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReserva(int id, Reserva reserva)
        {
            if (id != reserva.Id) { return BadRequest(); }

            if (await ExisteConflito(reserva))
            {
                return Conflict(new { mensagem = "Conflito de horários detectado. A sala já está reservada neste período." });
            }

            _context.Entry(reserva).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservaExists(id)) { return NotFound(); } else { throw; }
            }

            return NoContent();
        }

        // DELETE: api/Reservas/5 (D - Delete/Excluir)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReserva(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) { return NotFound(); }

            _context.Reservas.Remove(reserva);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReservaExists(int id)
        {
            return _context.Reservas.Any(e => e.Id == id);
        }
    }
}