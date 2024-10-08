﻿using Microsoft.AspNetCore.Authorization;
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
    public class AttractionsController : ControllerBase
    {
        private readonly CoreDbContext _context;

        private readonly int popularityLimit = 10;
        public AttractionsController(CoreDbContext context)
        {
            _context = context;
        }

        [HttpGet,Authorize]
        public async Task<ActionResult<IEnumerable<Attraction>>> GetAllAttractions()
        {
            return await _context.Attractions.ToListAsync();
        }

        [HttpGet("/popularAttractions"), Authorize]
        public async Task<ActionResult<IEnumerable<Attraction>>> GetPopularAttractions()
        {
            return await _context.Attractions.Where(x=>x.Popularity=="HIGH").ToListAsync();
        }

        [HttpGet("{stageId}/allStageAttractions"), Authorize]
        public async Task<ActionResult<IEnumerable<Attraction>>> GetStageAttractions(int stageId)
        {
            var stage = await _context.Stages.FindAsync(stageId);
            if (stage == null)
            {
                return NotFound();
            }

            List<Attraction> attractions = new List<Attraction>();
            var attractionRelations = await _context.HasAttractions.Where(x => x.StageId == stageId).ToListAsync();

            foreach (var relation in attractionRelations)
            {
                Attraction? attraction = await _context.Attractions
                    .Where(x => x.AttractionId == relation.AttractionId)
                    .FirstOrDefaultAsync();

                if (attraction != null)
                {
                    attractions.Add(attraction);
                }
                else
                {
                    return NotFound("Attraction not found.");
                }
            }

            return attractions;
        }

        [HttpGet("{attractionId}"), Authorize]
        public async Task<ActionResult<Attraction>> GetAttraction(int attractionId)
        {
            var attraction = await _context.Attractions.FindAsync(attractionId);
            if (attraction == null)
            {
                return NotFound();
            }
            
            return attraction;
        }


        [HttpPost, Authorize]
        public async Task<ActionResult<Attraction>> PostAttraction(Attraction attraction)
        {
            attraction.AttractionId = GetAttractionId();
            //as default all attractions have low popularity and score==0, higher the score higher the popularity
            attraction.Score = 0;
            if (string.IsNullOrEmpty(attraction.Popularity))
            {
                attraction.Popularity = "LOW";
            }

            if (string.IsNullOrEmpty(attraction.AttractionName) || attraction.AttractionName == "null")
            {
                return BadRequest("invalid attraction name");
            }

            if (string.IsNullOrEmpty(attraction.AttractionDesc) || attraction.AttractionDesc == "null")
            {
                return BadRequest("invalid attraction description");
            }
            _context.Attractions.Add(attraction);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                return StatusCode(500);
            }

            return Ok(attraction);
        }
        [HttpPost("{attractionId}/{stageId}"), Authorize]
        public async Task<ActionResult<HasAttraction>> PostRelation(int attractionId, int stageId)
        {
            var attraction = await _context.Attractions.FindAsync(attractionId);
            if (attraction == null)
            {
                return BadRequest();
            }
            var stage = await _context.Stages.FindAsync(stageId);
            if (stage == null)
            {
                return BadRequest();
            }
            HasAttraction relation = new HasAttraction();
            relation.AttractionId=attractionId;
            relation.StageId = stageId;

            _context.HasAttractions.Add(relation);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500);
            }

            return Ok(relation);
        }

        [HttpDelete("{id}"), Authorize]
        public async Task<IActionResult> DeleteAttraction(int id)
        {
            var attraction = await _context.Attractions.FindAsync(id);
            if (attraction == null)
            {
                return NotFound();
            }

            _context.Attractions.Remove(attraction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{attractionId}/{stageId}"), Authorize]
        public async Task<IActionResult> DeleteRelation(int attractionId,int stageId)
        {
            var attraction = await _context.Attractions.FindAsync(attractionId);
            if (attraction == null)
            {
                return BadRequest();
            }
            var stage = await _context.Stages.FindAsync(stageId);
            if (stage == null)
            {
                return BadRequest();
            }

            var relation = await _context.HasAttractions
                .Where(x => x.StageId == stageId && x.AttractionId == attractionId)
                .FirstOrDefaultAsync();

            if (relation != null)
            {
                _context.HasAttractions.Remove(relation);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Optionally handle the case where no relation is found
                return NotFound("Relation not found.");
            }

            return NoContent();
        }


        [HttpPut("{attractionId}"), Authorize]
        public async Task<IActionResult> PutAttraction(int attractionId, Attraction attraction)
        {
            if (attractionId != attraction.AttractionId)
            {
                return BadRequest();
            }
            if (string.IsNullOrEmpty(attraction.AttractionName) || attraction.AttractionName == "null")
            {
                return BadRequest("invalid attraction name");
            }

            if (string.IsNullOrEmpty(attraction.AttractionDesc) || attraction.AttractionDesc == "null")
            {
                return BadRequest("invalid attraction description");
            }
            attraction.Score+=1;

            if (attraction.Score > this.popularityLimit)
            {
                attraction.Popularity = "HIGH";
            }

            _context.Entry(attraction).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500);
            }

            return NoContent();
        }
        private int GetAttractionId()
        {
            if (_context.Attractions.Count() == 0)
            {
                return 1;
            }
            int maxAttractionId = _context.Attractions.Max(x => x.AttractionId);
            maxAttractionId++;
            return maxAttractionId;
        }
    }
}
