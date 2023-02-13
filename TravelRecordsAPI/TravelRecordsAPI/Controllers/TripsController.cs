using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TravelRecordsAPI.Models;

namespace TravelRecordsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly CoreDbContext _context;

        public TripsController(CoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Trips
        [HttpGet, Authorize]
        public async Task<ActionResult<IEnumerable<Trip>>> GetTrips()
        {
            return await _context.Trips.ToListAsync();
        }

        // GET: api/Trips/5
        [HttpGet("{userId}/userTrips"), Authorize]
        public async Task<ActionResult<IEnumerable<Trip>>> GetUsersTrips(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            List<Trip> userTrips = _context.Trips.Where(x => x.UserId == userId).ToList();
            return userTrips;
        }

        // GET: api/Trips/5
        [HttpGet("{id}"), Authorize]
        public async Task<ActionResult<Trip>> GetTrip(int id)
        {
            var trip = await _context.Trips.FindAsync(id);

            if (trip == null)
            {
                return NotFound();
            }

            return trip;
        }

        // PUT: api/Trips/5
        [HttpPut("{id}"), Authorize]
        public async Task<IActionResult> PutTrip(int id, Trip trip)
        {
            if (id != trip.TripId)
            {
                return BadRequest();
            }

            //checking for assigning trip to non existent user
            var user = await _context.Users.FindAsync(trip.UserId);
            if (user == null)
            {
                return BadRequest();
            }
            if (trip.Title == "null" || trip.Title.IsNullOrEmpty())
            {
                return BadRequest("invalid title");
            }
            if (trip.TripDesc == "null" || trip.TripDesc.IsNullOrEmpty())
            {
                return BadRequest("invalid description");
            }

            _context.Entry(trip).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TripExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Trips
        [HttpPost, Authorize]
        public async Task<ActionResult<Trip>> PostTrip(Trip trip)
        {            
            var user = await _context.Users.FindAsync(trip.UserId);

            if (user == null)
            {
                return BadRequest("user not found");
            }

            if(trip.Title=="null"||trip.Title.IsNullOrEmpty())
            {
                return BadRequest("invalid title");
            }
            if (trip.TripDesc == "null" || trip.TripDesc.IsNullOrEmpty())
            {
                return BadRequest("invalid description");
            }
            trip.CreationDate = DateTime.Now;
            trip.TripId = this.GetTripId();
            _context.Trips.Add(trip);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (TripExists(trip.TripId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetTrip", new { id = trip.TripId }, trip);
        }

        
        // DELETE: api/Trips/5
        [HttpDelete("{id}"), Authorize]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.TripId == id);
        }

        private int GetTripId()
        {
            if (_context.Trips.Count()==0)
            {
                return 1;
            }
            int maxTripId = _context.Trips.Max(x => x.TripId);
            maxTripId++;
            return maxTripId;
        }
    }
}
